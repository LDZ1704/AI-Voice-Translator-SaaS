namespace AI_Voice_Translator_SaaS.Helpers
{
    public static class FileValidator
    {
        private static readonly string[] AllowedExtensions = { ".mp3", ".wav", ".m4a" };
        private static readonly string[] AllowedMimeTypes =
        {
            "audio/mpeg",
            "audio/wav",
            "audio/x-wav",
            "audio/mp4",
            "audio/x-m4a"
        };

        private const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20MB

        public static (bool IsValid, string ErrorMessage) ValidateAudioFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return (false, "File không được để trống");
            }

            // Check file size
            if (file.Length > MaxFileSizeBytes)
            {
                return (false, $"File quá lớn. Tối đa {MaxFileSizeBytes / (1024 * 1024)}MB");
            }

            // Check extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                return (false, $"Chỉ hỗ trợ file: {string.Join(", ", AllowedExtensions)}");
            }

            // Check MIME type
            if (!AllowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                return (false, "Định dạng file không hợp lệ");
            }

            return (true, string.Empty);
        }

        public static string GetSafeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var safeName = string.Join("_", fileName.Split(invalidChars));
            return safeName;
        }
    }
}