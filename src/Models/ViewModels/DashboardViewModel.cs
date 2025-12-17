namespace AI_Voice_Translator_SaaS.Models.ViewModels
{
    public class DashboardViewModel
    {
        public DashboardStatsDto Stats { get; set; }
        public List<AudioFileDto> AudioFiles { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string SearchQuery { get; set; }
        public string StatusFilter { get; set; }
    }

    public class DashboardStatsDto
    {
        public int TotalFiles { get; set; }
        public int CompletedFiles { get; set; }
        public int ProcessingFiles { get; set; }
        public int FailedFiles { get; set; }
        public int TotalMinutes { get; set; }
    }

    public class AudioFileDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; }
        public string Status { get; set; }
        public DateTime UploadedAt { get; set; }
        public int DurationSeconds { get; set; }
        public long FileSizeBytes { get; set; }

        public string FormattedDuration => DurationSeconds > 0 ? $"{DurationSeconds / 60}m {DurationSeconds % 60}s" : "0s";
        public string FormattedSize => FileSizeBytes < 1024 * 1024 ? $"{FileSizeBytes / 1024:N0} KB" : $"{FileSizeBytes / (1024.0 * 1024):N2} MB";
    }

    public class AudioDetailsViewModel
    {
        public AudioFile AudioFile { get; set; }
        public Transcript Transcript { get; set; }
        public Translation Translation { get; set; }
        public Output Output { get; set; }
    }
}