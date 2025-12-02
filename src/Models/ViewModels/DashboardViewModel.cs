namespace AI_Voice_Translator_SaaS.Models.ViewModels
{
    public class DashboardViewModel
    {
        public User CurrentUser { get; set; }
        public DashboardStatsDto Stats { get; set; }
        public List<AudioFileDto> RecentFiles { get; set; }
    }

    public class DashboardStatsDto
    {
        public int TotalUploads { get; set; }
        public int CompletedTranslations { get; set; }
        public int PendingTranslations { get; set; }
        public int FailedTranslations { get; set; }
        public long TotalStorageUsed { get; set; }
        public int TotalMinutesProcessed { get; set; }
    }

    public class AudioFileDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; }
        public long FileSizeBytes { get; set; }
        public string FileSizeMB => (FileSizeBytes / (1024.0 * 1024.0)).ToString("F2");
        public int? DurationSeconds { get; set; }
        public string Status { get; set; }
        public DateTime UploadedAt { get; set; }
        public string UploadedAtFormatted => UploadedAt.ToString("dd/MM/yyyy HH:mm");
        public TranscriptDto Transcript { get; set; }
        public List<TranslationDto> Translations { get; set; }
    }

    public class TranscriptDto
    {
        public Guid Id { get; set; }
        public string OriginalText { get; set; }
        public string DetectedLanguage { get; set; }
        public decimal? Confidence { get; set; }
    }

    public class TranslationDto
    {
        public Guid Id { get; set; }
        public string TargetLanguage { get; set; }
        public string TranslatedText { get; set; }
        public OutputDto Output { get; set; }
    }

    public class OutputDto
    {
        public Guid Id { get; set; }
        public string OutputFileUrl { get; set; }
        public string VoiceType { get; set; }
        public int DownloadCount { get; set; }
    }
}