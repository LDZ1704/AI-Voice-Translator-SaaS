using AI_Voice_Translator_SaaS.Interfaces;

namespace AI_Voice_Translator_SaaS.Services
{
    public class LocalStorageService : IStorageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly string _uploadPath;

        public LocalStorageService(IWebHostEnvironment environment)
        {
            _environment = environment;
            _uploadPath = Path.Combine(_environment.WebRootPath, "uploads");

            if (!Directory.Exists(_uploadPath))
            {
                Directory.CreateDirectory(_uploadPath);
            }
        }

        public async Task<string> UploadFileAsync(IFormFile file, string folder)
        {
            var folderPath = Path.Combine(_uploadPath, folder);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/{folder}/{fileName}";
        }

        public Task<bool> DeleteFileAsync(string fileUrl)
        {
            var filePath = Path.Combine(_environment.WebRootPath, fileUrl.TrimStart('/'));

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public Task<Stream> DownloadFileAsync(string fileUrl)
        {
            var filePath = Path.Combine(_environment.WebRootPath, fileUrl.TrimStart('/'));

            if (File.Exists(filePath))
            {
                return Task.FromResult<Stream>(File.OpenRead(filePath));
            }

            throw new FileNotFoundException("File not found", fileUrl);
        }

        public Task<bool> FileExistsAsync(string fileUrl)
        {
            var filePath = Path.Combine(_environment.WebRootPath, fileUrl.TrimStart('/'));
            return Task.FromResult(File.Exists(filePath));
        }

        public string GetFileUrl(string fileName)
        {
            return $"/uploads/{fileName}";
        }
    }
}