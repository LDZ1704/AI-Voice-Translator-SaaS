using AI_Voice_Translator_SaaS.Helpers;
using AI_Voice_Translator_SaaS.Interfaces;
using AI_Voice_Translator_SaaS.Jobs;
using AI_Voice_Translator_SaaS.Models;
using AI_Voice_Translator_SaaS.Models.ViewModels;
using AI_Voice_Translator_SaaS.Repositories;
using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace AI_Voice_Translator_SaaS.Controllers
{
    public class AudioController : Controller
    {
        private readonly IStorageService _storageService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AudioController> _logger;
        private readonly IAuditService _auditService;
        private readonly IAudioDurationService _durationService;

        public AudioController(IStorageService storageService, IUnitOfWork unitOfWork, ILogger<AudioController> logger, IAuditService auditService, IAudioDurationService durationService)
        {
            _storageService = storageService;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _auditService = auditService;
            _durationService = durationService;
        }

        //GET: /Audio/Upload
        [HttpGet]
        public IActionResult Upload()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để tải file";
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        //POST: /Audio/Upload
        [HttpPost]
        [RequestSizeLimit(20 * 1024 * 1024)] // 20MB
        public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] string sourceLanguage, [FromForm] string targetLanguage)
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdStr))
                {
                    return Json(new AudioUploadResponseDto
                    {
                        Success = false,
                        Message = "Vui lòng đăng nhập"
                    });
                }

                var userId = Guid.Parse(userIdStr);

                var validation = FileValidator.ValidateAudioFile(file);
                if (!validation.IsValid)
                {
                    return Json(new AudioUploadResponseDto
                    {
                        Success = false,
                        Message = validation.ErrorMessage
                    });
                }

                var fileUrl = await _storageService.UploadFileAsync(file, "audio");
                var duration = await _durationService.GetDurationAsync(file);

                var audioFile = new AudioFile
                {
                    UserId = userId,
                    FileName = file.FileName,
                    OriginalFileUrl = fileUrl,
                    FileSizeBytes = file.Length,
                    DurationSeconds = duration > 0 ? duration : null,
                    Status = "Pending",
                    UploadedAt = DateTime.UtcNow
                };

                await _unitOfWork.AudioFiles.AddAsync(audioFile);
                await _unitOfWork.SaveChangesAsync();

                await _auditService.LogAsync(userId, "Upload", $"Uploaded file: {file.FileName}");

                BackgroundJob.Enqueue<ProcessAudioJob>(job =>
                    job.ProcessAsync(audioFile.Id, targetLanguage));

                _logger.LogInformation($"User {userId} uploaded file {file.FileName}. Job enqueued.");

                return Json(new AudioUploadResponseDto
                {
                    Success = true,
                    Message = "Tải lên thành công. Đang xử lý...",
                    AudioFileId = audioFile.Id,
                    FileName = audioFile.FileName,
                    FileSizeBytes = audioFile.FileSizeBytes
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                return Json(new AudioUploadResponseDto
                {
                    Success = false,
                    Message = "Lỗi khi tải file: " + ex.Message
                });
            }
        }

        //GET: /Audio/Download/{id}
        [HttpGet]
        public async Task<IActionResult> Download(Guid id)
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdStr))
                {
                    return Unauthorized();
                }

                var userId = Guid.Parse(userIdStr);
                var audioFile = await _unitOfWork.AudioFiles.GetByIdAsync(id);
                if (audioFile == null)
                {
                    return NotFound();
                }

                await _auditService.LogAsync(userId, "Download", $"Downloaded audio file: {audioFile.FileName}");

                var stream = await _storageService.DownloadFileAsync(audioFile.OriginalFileUrl);
                return File(stream, "audio/mpeg", audioFile.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file");
                return BadRequest("Không thể tải file");
            }
        }

        //DELETE: /Audio/Delete/{id}
        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdStr))
                {
                    return Unauthorized();
                }

                var userId = Guid.Parse(userIdStr);
                var audioFile = await _unitOfWork.AudioFiles.GetByIdAsync(id);
                if (audioFile == null)
                {
                    return NotFound();
                }

                if (audioFile.UserId.ToString() != userIdStr)
                {
                    return Forbid();
                }

                var fileName = audioFile.FileName;
                await _storageService.DeleteFileAsync(audioFile.OriginalFileUrl);

                _unitOfWork.AudioFiles.Remove(audioFile);
                await _unitOfWork.SaveChangesAsync();

                await _auditService.LogAsync(userId, "Delete", $"Deleted audio file: {fileName}");

                return Json(new { success = true, message = "Đã xóa file" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file");
                return Json(new { success = false, message = "Lỗi khi xóa file" });
            }
        }

        //GET: /Audio/GetStatus/{id}
        [HttpGet]
        public async Task<IActionResult> GetStatus(Guid id)
        {
            var audioFile = await _unitOfWork.AudioFiles.GetByIdAsync(id);
            if (audioFile == null)
            {
                return NotFound();
            }

            return Json(new
            {
                status = audioFile.Status,
                fileName = audioFile.FileName
            });
        }

        //GET: /Audio/Processing/{id}
        [HttpGet]
        public async Task<IActionResult> Processing(Guid id)
        {
            var audioFile = await _unitOfWork.AudioFiles.GetByIdAsync(id);
            if (audioFile == null)
            {
                return NotFound();
            }

            var dto = new AudioFileDto
            {
                Id = audioFile.Id,
                FileName = audioFile.FileName,
                Status = audioFile.Status
            };

            return View(dto);
        }

        [HttpGet]
        public async Task<IActionResult> GetDetailedStatus(Guid id)
        {
            var audioFile = await _unitOfWork.AudioFiles.GetByIdAsync(id);
            if (audioFile == null)
            {
                return NotFound();
            }

            var progress = 0;
            var message = "";

            switch (audioFile.Status)
            {
                case "Pending":
                    progress = 10;
                    message = "Đang chờ xử lý...";
                    break;
                case "Processing":
                    var transcript = await _unitOfWork.Transcripts.GetAllAsync().ContinueWith(t => t.Result.FirstOrDefault(tr => tr.AudioFileId == id));
                    if (transcript != null)
                    {
                        progress = 60;
                        message = "Đang dịch văn bản...";
                    }
                    else
                    {
                        progress = 30;
                        message = "Đang nhận dạng giọng nói...";
                    }
                    break;
                case "Completed":
                    progress = 100;
                    message = "Hoàn thành!";
                    break;
                case "Failed":
                    progress = 0;
                    message = "Xử lý thất bại";
                    break;
            }

            return Json(new
            {
                status = audioFile.Status,
                progress = progress,
                message = message
            });
        }

    }
}