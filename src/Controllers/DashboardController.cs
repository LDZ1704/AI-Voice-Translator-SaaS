using AI_Voice_Translator_SaaS.Interfaces;
using AI_Voice_Translator_SaaS.Models.ViewModels;
using AI_Voice_Translator_SaaS.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI_Voice_Translator_SaaS.Controllers
{
    public class DashboardController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(IUnitOfWork unitOfWork, ILogger<DashboardController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        //GET: /Dashboard
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập";
                return RedirectToAction("Login", "Account");
            }

            var userId = Guid.Parse(userIdStr);

            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var audioFiles = await _unitOfWork.AudioFiles.GetByUserIdAsync(userId);

                var stats = new DashboardStatsDto
                {
                    TotalUploads = audioFiles.Count(),
                    CompletedTranslations = audioFiles.Count(a => a.Status == "Completed"),
                    PendingTranslations = audioFiles.Count(a => a.Status == "Pending" || a.Status == "Processing"),
                    FailedTranslations = audioFiles.Count(a => a.Status == "Failed"),
                    TotalStorageUsed = audioFiles.Sum(a => a.FileSizeBytes),
                    TotalMinutesProcessed = audioFiles.Where(a => a.DurationSeconds.HasValue).Sum(a => a.DurationSeconds.Value) / 60
                };

                var recentFiles = audioFiles
                    .OrderByDescending(a => a.UploadedAt)
                    .Take(10)
                    .Select(a => new AudioFileDto
                    {
                        Id = a.Id,
                        FileName = a.FileName,
                        FileSizeBytes = a.FileSizeBytes,
                        DurationSeconds = a.DurationSeconds,
                        Status = a.Status,
                        UploadedAt = a.UploadedAt
                    })
                    .ToList();

                var viewModel = new DashboardViewModel
                {
                    CurrentUser = user,
                    Stats = stats,
                    RecentFiles = recentFiles
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                TempData["ErrorMessage"] = "Lỗi khi tải dashboard";
                return RedirectToAction("Index", "Home");
            }
        }

        //GET: /Dashboard/History
        [HttpGet]
        public async Task<IActionResult> History()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = Guid.Parse(userIdStr);
            var audioFiles = await _unitOfWork.AudioFiles.GetByUserIdAsync(userId);

            var files = audioFiles
                .OrderByDescending(a => a.UploadedAt)
                .Select(a => new AudioFileDto
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    FileSizeBytes = a.FileSizeBytes,
                    DurationSeconds = a.DurationSeconds,
                    Status = a.Status,
                    UploadedAt = a.UploadedAt
                })
                .ToList();

            return View(files);
        }

        //GET: /Dashboard/Details/{id}
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = Guid.Parse(userIdStr);
            var audioFile = await _unitOfWork.AudioFiles.GetWithTranscriptAsync(id);

            if (audioFile == null || audioFile.UserId != userId)
            {
                return NotFound();
            }

            var dto = new AudioFileDto
            {
                Id = audioFile.Id,
                FileName = audioFile.FileName,
                FileSizeBytes = audioFile.FileSizeBytes,
                DurationSeconds = audioFile.DurationSeconds,
                Status = audioFile.Status,
                UploadedAt = audioFile.UploadedAt
            };

            if (audioFile.Transcripts != null)
            {
                dto.Transcript = new TranscriptDto
                {
                    Id = audioFile.Transcripts.Id,
                    OriginalText = audioFile.Transcripts.OriginalText,
                    DetectedLanguage = audioFile.Transcripts.DetectedLanguage,
                    Confidence = audioFile.Transcripts.Confidence
                };

                dto.Translations = audioFile.Transcripts.Translations.Select(t => new TranslationDto
                {
                    Id = t.Id,
                    TargetLanguage = t.TargetLanguage,
                    TranslatedText = t.TranslatedText,
                    Output = t.Outputs != null ? new OutputDto
                    {
                        Id = t.Outputs.Id,
                        OutputFileUrl = t.Outputs.OutputFileUrl,
                        VoiceType = t.Outputs.VoiceType,
                        DownloadCount = t.Outputs.DownloadCount
                    } : null
                }).ToList();
            }

            return View(dto);
        }

        //GET: /Dashboard/Stats
        [HttpGet]
        public async Task<IActionResult> GetStats()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return Unauthorized();
            }

            var userId = Guid.Parse(userIdStr);
            var audioFiles = await _unitOfWork.AudioFiles.GetByUserIdAsync(userId);

            var stats = new
            {
                totalUploads = audioFiles.Count(),
                completed = audioFiles.Count(a => a.Status == "Completed"),
                pending = audioFiles.Count(a => a.Status == "Pending" || a.Status == "Processing"),
                failed = audioFiles.Count(a => a.Status == "Failed"),
                storageUsed = audioFiles.Sum(a => a.FileSizeBytes)
            };

            return Json(stats);
        }
    }
}