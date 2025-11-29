namespace AI_Voice_Translator_SaaS.Services
{
    public interface IStorageService
    {
        Task<string> UploadFileAsync(IFormFile file, string folder);
        Task<bool> DeleteFileAsync(string fileUrl);
        Task<Stream> DownloadFileAsync(string fileUrl);
        Task<bool> FileExistsAsync(string fileUrl);
        string GetFileUrl(string fileName);
    }
}