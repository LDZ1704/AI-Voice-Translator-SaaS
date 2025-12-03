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

        public AudioController(IStorageService storageService, IUnitOfWork unitOfWork, ILogger<AudioController> logger)
        {
            _storageService = storageService;
            _unitOfWork = unitOfWork;
            _logger = logger;
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

                var audioFile = new AudioFile
                {
                    UserId = userId,
                    FileName = file.FileName,
                    OriginalFileUrl = fileUrl,
                    FileSizeBytes = file.Length,
                    DurationSeconds = FileValidator.GetAudioDuration(file),
                    Status = "Pending",
                    UploadedAt = DateTime.UtcNow
                };

                await _unitOfWork.AudioFiles.AddAsync(audioFile);
                await _unitOfWork.SaveChangesAsync();

                await LogAuditAsync(userId, "Upload", $"Uploaded file: {file.FileName}");

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
                var audioFile = await _unitOfWork.AudioFiles.GetByIdAsync(id);
                if (audioFile == null)
                {
                    return NotFound();
                }

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

                var audioFile = await _unitOfWork.AudioFiles.GetByIdAsync(id);
                if (audioFile == null)
                {
                    return NotFound();
                }

                if (audioFile.UserId.ToString() != userIdStr)
                {
                    return Forbid();
                }

                await _storageService.DeleteFileAsync(audioFile.OriginalFileUrl);

                _unitOfWork.AudioFiles.Remove(audioFile);
                await _unitOfWork.SaveChangesAsync();

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

        private async Task LogAuditAsync(Guid userId, string action, string details = null)
        {
            var log = new AuditLog
            {
                UserId = userId,
                Action = action,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                Timestamp = DateTime.UtcNow
            };
            await _unitOfWork.AuditLogs.AddAsync(log);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}