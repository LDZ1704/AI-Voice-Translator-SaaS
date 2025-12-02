using System.ComponentModel.DataAnnotations;

namespace AI_Voice_Translator_SaaS.Models.ViewModels
{
    public class AudioUploadViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn file")]
        public IFormFile File { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngôn ngữ nguồn")]
        public string SourceLanguage { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngôn ngữ đích")]
        public string TargetLanguage { get; set; }
    }

    public class AudioUploadResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public Guid? AudioFileId { get; set; }
        public string FileName { get; set; }
        public long FileSizeBytes { get; set; }
    }
}