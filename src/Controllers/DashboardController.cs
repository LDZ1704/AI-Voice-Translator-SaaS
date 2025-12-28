using AI_Voice_Translator_SaaS.Interfaces;
using AI_Voice_Translator_SaaS.Jobs;
using AI_Voice_Translator_SaaS.Models.ViewModels;
using AI_Voice_Translator_SaaS.Repositories;
using AI_Voice_Translator_SaaS.Services;
using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace AI_Voice_Translator_SaaS.Controllers
{
    public class DashboardController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DashboardController> _logger;
        private readonly IAuditService _auditService;

        public DashboardController(IUnitOfWork unitOfWork, ILogger<DashboardController> logger, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _auditService = auditService;
        }

        // GET: /Dashboard/Index
        [HttpGet]
        public async Task<IActionResult> Index(string search = "", string status = "", int page = 1)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = Guid.Parse(userIdStr);

            var (audioFiles, totalCount) = await _unitOfWork.AudioFiles
                .GetPagedByUserIdAsync(userId, page, 10, status);

            if (!string.IsNullOrEmpty(search))
            {
                audioFiles = audioFiles
                    .Where(f => f.FileName.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var statsTask = CalculateStatsAsync(userId);
            var stats = await statsTask;

            var totalPages = (int)Math.Ceiling(totalCount / 10.0);

            var viewModel = new DashboardViewModel
            {
                Stats = stats,
                AudioFiles = audioFiles.Select(f => new AudioFileDto
                {
                    Id = f.Id,
                    FileName = f.FileName,
                    Status = f.Status,
                    UploadedAt = f.UploadedAt,
                    DurationSeconds = f.DurationSeconds ?? 0,
                    FileSizeBytes = f.FileSizeBytes
                }).ToList(),
                CurrentPage = page,
                TotalPages = totalPages,
                SearchQuery = search,
                StatusFilter = status
            };

            return View(viewModel);
        }

        // GET: /Dashboard/Details/{id}
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToAction("Login", "Account");
            }

            var audioFile = await _unitOfWork.AudioFiles.GetByIdWithRelatedAsync(id);
            if (audioFile == null || audioFile.UserId.ToString() != userIdStr)
            {
                return NotFound();
            }

            var viewModel = new AudioDetailsViewModel
            {
                AudioFile = audioFile,
                Transcript = audioFile.Transcripts,
                Translation = audioFile.Transcripts?.Translations?.FirstOrDefault(),
                Output = audioFile.Transcripts?.Translations?.FirstOrDefault()?.Outputs
            };

            return View(viewModel);
        }

        // POST: /Dashboard/RateTranslation
        [HttpPost]
        public async Task<IActionResult> RateTranslation(Guid translationId, int rating)
        {
            try
            {
                if (rating < 1 || rating > 5)
                {
                    return Json(new { success = false, message = "Đánh giá không hợp lệ" });
                }

                var translation = await _unitOfWork.Translations.GetByIdAsync(translationId);
                if (translation == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy bản dịch" });
                }

                translation.UserRating = rating;
                _unitOfWork.Translations.Update(translation);
                await _unitOfWork.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rating translation");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: /Dashboard/DownloadOutput/{id}
        [HttpGet]
        public async Task<IActionResult> DownloadOutput(Guid id)
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdStr))
                {
                    return RedirectToAction("Login", "Account");
                }

                var userId = Guid.Parse(userIdStr);
                var output = await _unitOfWork.Outputs.GetByIdAsync(id);
                if (output == null)
                {
                    return NotFound();
                }

                output.DownloadCount++;
                _unitOfWork.Outputs.Update(output);
                await _unitOfWork.SaveChangesAsync();
                await _auditService.LogAsync(userId, "Download", $"Downloaded output file: {output.OutputFileUrl}");

                var storageService = HttpContext.RequestServices.GetService<IStorageService>();
                var stream = await storageService.DownloadFileAsync(output.OutputFileUrl);

                return File(stream, "audio/mpeg", $"translated_{DateTime.Now:yyyyMMddHHmmss}.mp3");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading output");
                return BadRequest("Không thể tải file");
            }
        }

        // GET: /Dashboard/GetAudioSasUrl
        [HttpGet]
        public IActionResult GetAudioSasUrl(string fileUrl)
        {
            try
            {
                var storageService = HttpContext.RequestServices.GetService<IStorageService>() as AzureBlobStorageService;
                var sasUrl = storageService?.GenerateSasUrl(fileUrl, 1);

                return Redirect(sasUrl ?? fileUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating SAS URL");
                return BadRequest("Không thể tạo link tải");
            }
        }

        // POST: /Dashboard/RetryProcessing
        [HttpPost]
        public async Task<IActionResult> RetryProcessing(Guid id)
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdStr))
                {
                    return Json(new { success = false, message = "Chưa đăng nhập" });
                }

                var audioFile = await _unitOfWork.AudioFiles.GetByIdAsync(id);
                if (audioFile == null || audioFile.UserId.ToString() != userIdStr)
                {
                    return Json(new { success = false, message = "Không tìm thấy file" });
                }

                audioFile.Status = "Pending";
                _unitOfWork.AudioFiles.Update(audioFile);
                await _unitOfWork.SaveChangesAsync();

                BackgroundJob.Enqueue<ProcessAudioJob>(job => job.ProcessAsync(audioFile.Id, "vi"));

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying processing");
                return Json(new { success = false, message = ex.Message });
            }
        }

        //Helper Functions
        private async Task<DashboardStatsDto> CalculateStatsAsync(Guid userId)
        {
            var allFiles = await _unitOfWork.AudioFiles.GetByUserIdAsync(userId);

            return new DashboardStatsDto
            {
                TotalFiles = allFiles.Count(),
                CompletedFiles = allFiles.Count(f => f.Status == "Completed"),
                ProcessingFiles = allFiles.Count(f => f.Status == "Processing"),
                FailedFiles = allFiles.Count(f => f.Status == "Failed"),
                TotalMinutes = (int)(allFiles.Sum(f => f.DurationSeconds) / 60)
            };
        }
    }
}