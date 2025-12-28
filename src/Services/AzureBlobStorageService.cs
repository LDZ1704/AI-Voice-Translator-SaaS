using AI_Voice_Translator_SaaS.Interfaces;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace AI_Voice_Translator_SaaS.Services
{
    public class AzureBlobStorageService : IStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _audioContainer;
        private readonly string _outputContainer;
        private readonly ILogger<AzureBlobStorageService> _logger;

        public AzureBlobStorageService(IConfiguration configuration, ILogger<AzureBlobStorageService> logger)
        {
            var connectionString = configuration["Azure:BlobConnectionString"];
            _blobServiceClient = new BlobServiceClient(connectionString);
            _audioContainer = configuration["Azure:AudioContainer"];
            _outputContainer = configuration["Azure:OutputContainer"];
            _logger = logger;
        }

        public async Task<string> UploadFileAsync(IFormFile file, string folder)
        {
            try
            {
                var containerName = folder == "audio" ? _audioContainer : _outputContainer;
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

                await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

                var blobName = $"{Guid.NewGuid()}_{file.FileName}";
                var blobClient = containerClient.GetBlobClient(blobName);

                var blobHttpHeaders = new BlobHttpHeaders
                {
                    ContentType = file.ContentType
                };

                await blobClient.UploadAsync(file.OpenReadStream(), new BlobUploadOptions { HttpHeaders = blobHttpHeaders });

                _logger.LogInformation($"Uploaded file to Azure Blob: {blobName}");

                return $"{containerName}/{blobName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading to Azure Blob");
                throw;
            }
        }

        public async Task<bool> DeleteFileAsync(string fileUrl)
        {
            try
            {
                string containerName;
                string blobName;

                if (fileUrl.Contains("/") && !fileUrl.StartsWith("http"))
                {
                    var parts = fileUrl.Split('/', 2);
                    containerName = parts[0];
                    blobName = parts[1];
                }
                else if (fileUrl.StartsWith("http"))
                {
                    var uri = new Uri(fileUrl);
                    blobName = uri.Segments[^1];
                    containerName = uri.Segments[^2].TrimEnd('/');
                }
                else
                {
                    _logger.LogWarning($"Invalid file URL format: {fileUrl}");
                    return false;
                }

                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                var result = await blobClient.DeleteIfExistsAsync();
                return result.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting from Azure Blob");
                return false;
            }
        }

        public async Task<Stream> DownloadFileAsync(string fileUrl)
        {
            try
            {
                string containerName;
                string blobName;

                if (fileUrl.Contains("/") && !fileUrl.StartsWith("http"))
                {
                    var parts = fileUrl.Split('/', 2);
                    containerName = parts[0];
                    blobName = parts[1];
                }
                else if (fileUrl.StartsWith("http"))
                {
                    var uri = new Uri(fileUrl);
                    blobName = uri.Segments[^1];
                    containerName = uri.Segments[^2].TrimEnd('/');
                }
                else
                {
                    throw new ArgumentException($"Invalid file URL format: {fileUrl}");
                }

                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                var response = await blobClient.DownloadAsync();

                var memoryStream = new MemoryStream();
                await response.Value.Content.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                return memoryStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading from Azure Blob");
                throw;
            }
        }

        public async Task<bool> FileExistsAsync(string fileUrl)
        {
            try
            {
                string containerName;
                string blobName;

                if (fileUrl.Contains("/") && !fileUrl.StartsWith("http"))
                {
                    var parts = fileUrl.Split('/', 2);
                    containerName = parts[0];
                    blobName = parts[1];
                }
                else if (fileUrl.StartsWith("http"))
                {
                    var uri = new Uri(fileUrl);
                    blobName = uri.Segments[^1];
                    containerName = uri.Segments[^2].TrimEnd('/');
                }
                else
                {
                    return false;
                }

                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                return await blobClient.ExistsAsync();
            }
            catch
            {
                return false;
            }
        }

        public string GetFileUrl(string fileName)
        {
            return $"{_audioContainer}/{fileName}";
        }

        public string GenerateSasUrl(string fileUrl, int expiryHours = 1)
        {
            var parts = fileUrl.Split('/');
            var containerName = parts[0];
            var blobName = parts[1];

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var sasBuilder = new Azure.Storage.Sas.BlobSasBuilder
            {
                BlobContainerName = containerName,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(expiryHours)
            };
            sasBuilder.SetPermissions(Azure.Storage.Sas.BlobSasPermissions.Read);

            return blobClient.GenerateSasUri(sasBuilder).ToString();
        }
    }
}