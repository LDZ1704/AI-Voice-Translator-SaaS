namespace AI_Voice_Translator_SaaS.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public SystemStatisticsDto Statistics { get; set; }
        public List<AdminAudioFileDto> RecentUploads { get; set; }
        public List<UserDto> RecentUsers { get; set; }
    }

    public class SystemStatisticsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalUploads { get; set; }
        public int CompletedUploads { get; set; }
        public int ProcessingUploads { get; set; }
        public int FailedUploads { get; set; }
        public int TotalTranslations { get; set; }
        public double TotalStorageUsedMB { get; set; }
        public int TotalProcessingMinutes { get; set; }
        public decimal AverageRating { get; set; }
    }

    public class AdminUsersViewModel
    {
        public List<UserDto> Users { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string SearchQuery { get; set; }
    }

    public class AdminUploadsViewModel
    {
        public List<AdminAudioFileDto> Uploads { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string StatusFilter { get; set; }
    }

    public class AdminLogsViewModel
    {
        public List<AuditLogDto> Logs { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string ActionFilter { get; set; }
        public int TotalLogs { get; set; }
        public int LoginCount { get; set; }
        public int RegisterCount { get; set; }
        public int UploadCount { get; set; }
        public int DownloadCount { get; set; }
    }

    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public string Role { get; set; }
        public string SubscriptionTier { get; set; }
        public DateTime? SubscriptionExpiryDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    public class AdminAudioFileDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string FileName { get; set; }
        public string Status { get; set; }
        public long FileSizeBytes { get; set; }
        public int DurationSeconds { get; set; }
        public DateTime UploadedAt { get; set; }
        public string FormattedSize => FileSizeBytes < 1024 * 1024 ? $"{FileSizeBytes / 1024:N0} KB" : $"{FileSizeBytes / (1024.0 * 1024):N2} MB";
        public string FormattedDuration => DurationSeconds > 0 ? $"{DurationSeconds / 60}m {DurationSeconds % 60}s" : "Chưa xác định";
    }

    public class AuditLogDto
    {
        public int Id { get; set; }
        public Guid? UserId { get; set; }
        public string Action { get; set; }
        public string IpAddress { get; set; }
        public DateTime Timestamp { get; set; }
    }
}