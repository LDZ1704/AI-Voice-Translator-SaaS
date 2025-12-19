using AI_Voice_Translator_SaaS.Interfaces;
using AI_Voice_Translator_SaaS.Models.ViewModels;
using AI_Voice_Translator_SaaS.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI_Voice_Translator_SaaS.Controllers
{
    public class AdminController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AdminController> _logger;
        private readonly IAuditService _auditService;

        public AdminController(IUnitOfWork unitOfWork, ILogger<AdminController> logger, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _auditService = auditService;
        }

        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return role == "Admin";
        }

        // GET: /Admin/Dashboard
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var viewModel = new AdminDashboardViewModel
            {
                Statistics = await GetSystemStatistics(),
                RecentUploads = await GetRecentUploads(10),
                RecentUsers = await GetRecentUsers(10)
            };

            return View(viewModel);
        }

        // GET: /Admin/Users
        [HttpGet]
        public async Task<IActionResult> Users(string search = "", int page = 1)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var allUsers = await _unitOfWork.Users.GetAllAsync();

            if (!string.IsNullOrEmpty(search))
            {
                allUsers = allUsers.Where(u =>
                    u.Email.Contains(search, StringComparison.OrdinalIgnoreCase) || u.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            int pageSize = 20;
            var totalPages = (int)Math.Ceiling(allUsers.Count() / (double)pageSize);
            var pagedUsers = allUsers
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    DisplayName = u.DisplayName,
                    Role = u.Role,
                    SubscriptionTier = u.SubscriptionTier,
                    SubscriptionExpiryDate = u.SubscriptionExpiryDate,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt
                })
                .ToList();

            var viewModel = new AdminUsersViewModel
            {
                Users = pagedUsers,
                CurrentPage = page,
                TotalPages = totalPages,
                SearchQuery = search
            };

            return View(viewModel);
        }

        // GET: /Admin/Uploads
        [HttpGet]
        public async Task<IActionResult> Uploads(string status = "All", int page = 1)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var allUploads = await _unitOfWork.AudioFiles.GetAllAsync();

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                allUploads = allUploads.Where(u => u.Status == status).ToList();
            }

            int pageSize = 20;
            var totalPages = (int)Math.Ceiling(allUploads.Count() / (double)pageSize);
            var pagedUploads = allUploads
                .OrderByDescending(u => u.UploadedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new AdminAudioFileDto
                {
                    Id = u.Id,
                    UserId = u.UserId,
                    FileName = u.FileName,
                    Status = u.Status,
                    FileSizeBytes = u.FileSizeBytes,
                    DurationSeconds = u.DurationSeconds ?? 0,
                    UploadedAt = u.UploadedAt
                })
                .ToList();

            var viewModel = new AdminUploadsViewModel
            {
                Uploads = pagedUploads,
                CurrentPage = page,
                TotalPages = totalPages,
                StatusFilter = status
            };

            return View(viewModel);
        }

        // GET: /Admin/Logs
        [HttpGet]
        public async Task<IActionResult> Logs(string actionFilter = "All", int page = 1)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var allLogsEnumerable = await _unitOfWork.AuditLogs.GetAllAsync();
                var allLogsList = allLogsEnumerable?.ToList() ?? new List<Models.AuditLog>();
                
                _logger.LogInformation($"Loaded {allLogsList.Count} logs from database");

                if (actionFilter == "Logs" || string.IsNullOrWhiteSpace(actionFilter))
                {
                    actionFilter = "All";
                }

                if (!string.IsNullOrEmpty(actionFilter) && actionFilter != "All")
                {
                    allLogsList = allLogsList.Where(l => l.Action == actionFilter).ToList();
                    _logger.LogInformation($"Filtered to {allLogsList.Count} logs for action: {actionFilter}");
                }
                else
                {
                    _logger.LogInformation($"No filter applied, showing all {allLogsList.Count} logs");
                }

                var totalLogs = allLogsList.Count;
                var loginCount = allLogsList.Count(l => l.Action == "Login");
                var registerCount = allLogsList.Count(l => l.Action == "Register");
                var uploadCount = allLogsList.Count(l => l.Action == "Upload");
                var downloadCount = allLogsList.Count(l => l.Action == "Download");

                int pageSize = 10;
                var totalPages = totalLogs > 0 ? (int)Math.Ceiling(totalLogs / (double)pageSize) : 1;
                var pagedLogs = allLogsList
                    .OrderByDescending(l => l.Timestamp)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(l => new AuditLogDto
                    {
                        Id = l.Id,
                        UserId = l.UserId,
                        Action = l.Action ?? string.Empty,
                        IpAddress = l.IpAddress ?? string.Empty,
                        Timestamp = l.Timestamp
                    })
                    .ToList();

                _logger.LogInformation($"Paged logs: {pagedLogs.Count} logs for page {page} of {totalPages}");

                var viewModel = new AdminLogsViewModel
                {
                    Logs = pagedLogs ?? new List<AuditLogDto>(),
                    CurrentPage = page,
                    TotalPages = totalPages,
                    ActionFilter = actionFilter ?? "All",
                    TotalLogs = totalLogs,
                    LoginCount = loginCount,
                    RegisterCount = registerCount,
                    UploadCount = uploadCount,
                    DownloadCount = downloadCount
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading logs");
                var errorViewModel = new AdminLogsViewModel
                {
                    Logs = new List<AuditLogDto>(),
                    CurrentPage = 1,
                    TotalPages = 1,
                    ActionFilter = actionFilter ?? "All",
                    TotalLogs = 0,
                    LoginCount = 0,
                    RegisterCount = 0,
                    UploadCount = 0,
                    DownloadCount = 0
                };
                return View(errorViewModel);
            }
        }

        // POST: /Admin/ToggleUserStatus
        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(Guid userId)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                user.IsActive = !user.IsActive;
                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = user.IsActive ? "User activated" : "User deactivated",
                    isActive = user.IsActive
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user status");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: /Admin/DeleteUser
        [HttpPost]
        public async Task<IActionResult> DeleteUser(Guid userId)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                var adminUserIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(adminUserIdStr))
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                var adminUserId = Guid.Parse(adminUserIdStr);
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                var userEmail = user.Email;
                // Soft-delete: mark as inactive instead of hard delete
                user.IsActive = false;
                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();
                await _auditService.LogAsync(adminUserId, "Delete", $"Admin deleted user: {userEmail} (UserId: {userId})");

                return Json(new { success = true, message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: /Admin/DeleteAudioFile
        [HttpPost]
        public async Task<IActionResult> DeleteAudioFile(Guid audioFileId)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                var adminUserIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(adminUserIdStr))
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                var adminUserId = Guid.Parse(adminUserIdStr);
                var audioFile = await _unitOfWork.AudioFiles.GetByIdAsync(audioFileId);
                if (audioFile == null)
                {
                    return Json(new { success = false, message = "File not found" });
                }

                var fileName = audioFile.FileName;
                var storageService = HttpContext.RequestServices.GetService<IStorageService>();
                await storageService.DeleteFileAsync(audioFile.OriginalFileUrl);

                _unitOfWork.AudioFiles.Remove(audioFile);
                await _unitOfWork.SaveChangesAsync();
                await _auditService.LogAsync(adminUserId, "Delete", $"Admin deleted audio file: {fileName} (FileId: {audioFileId})");

                return Json(new { success = true, message = "File deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting audio file");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: /Admin/ClearOldLogs
        [HttpPost]
        public async Task<IActionResult> ClearOldLogs()
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                var allLogs = await _unitOfWork.AuditLogs.GetAllAsync();
                var oldLogs = allLogs.Where(l => l.Timestamp < DateTime.UtcNow.AddDays(-30)).ToList();

                foreach (var log in oldLogs)
                {
                    _unitOfWork.AuditLogs.Remove(log);
                }

                await _unitOfWork.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Cleared {oldLogs.Count} old logs"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing old logs");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Helper Methods
        private async Task<SystemStatisticsDto> GetSystemStatistics()
        {
            var users = await _unitOfWork.Users.GetAllAsync();
            var audioFiles = await _unitOfWork.AudioFiles.GetAllAsync();
            var translations = await _unitOfWork.Translations.GetAllAsync();

            return new SystemStatisticsDto
            {
                TotalUsers = users.Count(),
                ActiveUsers = users.Count(u => u.IsActive),
                TotalUploads = audioFiles.Count(),
                CompletedUploads = audioFiles.Count(a => a.Status == "Completed"),
                ProcessingUploads = audioFiles.Count(a => a.Status == "Processing"),
                FailedUploads = audioFiles.Count(a => a.Status == "Failed"),
                TotalTranslations = translations.Count(),
                TotalStorageUsedMB = audioFiles.Sum(a => a.FileSizeBytes) / (1024.0 * 1024),
                TotalProcessingMinutes = (int)(audioFiles.Sum(a => a.DurationSeconds) / 60),
                AverageRating = translations.Where(t => t.UserRating.HasValue).Average(t => (decimal?)t.UserRating) ?? 0
            };
        }

        private async Task<List<AdminAudioFileDto>> GetRecentUploads(int count)
        {
            var uploads = await _unitOfWork.AudioFiles.GetAllAsync();
            return uploads
                .OrderByDescending(u => u.UploadedAt)
                .Take(count)
                .Select(u => new AdminAudioFileDto
                {
                    Id = u.Id,
                    UserId = u.UserId,
                    FileName = u.FileName,
                    Status = u.Status,
                    FileSizeBytes = u.FileSizeBytes,
                    DurationSeconds = u.DurationSeconds ?? 0,
                    UploadedAt = u.UploadedAt
                })
                .ToList();
        }

        private async Task<List<UserDto>> GetRecentUsers(int count)
        {
            var users = await _unitOfWork.Users.GetAllAsync();
            return users
                .OrderByDescending(u => u.CreatedAt)
                .Take(count)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    DisplayName = u.DisplayName,
                    Role = u.Role,
                    SubscriptionTier = u.SubscriptionTier,
                    SubscriptionExpiryDate = u.SubscriptionExpiryDate,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt
                })
                .ToList();
        }
    }
}