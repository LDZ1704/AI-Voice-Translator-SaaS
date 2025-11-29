using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

namespace AI_Voice_Translator_SaaS.Services
{
    public class AwsS3StorageService : IStorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;

        public AwsS3StorageService(IAmazonS3 s3Client, IConfiguration configuration)
        {
            _s3Client = s3Client;
            _bucketName = configuration["AWS:BucketName"];
        }

        public async Task<string> UploadFileAsync(IFormFile file, string folder)
        {
            var key = $"{folder}/{Guid.NewGuid()}_{file.FileName}";

            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = file.OpenReadStream(),
                Key = key,
                BucketName = _bucketName,
                ContentType = file.ContentType,
                CannedACL = S3CannedACL.Private
            };

            var transferUtility = new TransferUtility(_s3Client);
            await transferUtility.UploadAsync(uploadRequest);

            return key;
        }

        public async Task<bool> DeleteFileAsync(string fileUrl)
        {
            try
            {
                var deleteRequest = new DeleteObjectRequest
                {
                    BucketName = _bucketName,
                    Key = fileUrl
                };

                await _s3Client.DeleteObjectAsync(deleteRequest);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<Stream> DownloadFileAsync(string fileUrl)
        {
            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = fileUrl
            };

            var response = await _s3Client.GetObjectAsync(request);
            return response.ResponseStream;
        }

        public async Task<bool> FileExistsAsync(string fileUrl)
        {
            try
            {
                var request = new GetObjectMetadataRequest
                {
                    BucketName = _bucketName,
                    Key = fileUrl
                };

                await _s3Client.GetObjectMetadataAsync(request);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public string GetFileUrl(string fileName)
        {
            return $"https://{_bucketName}.s3.amazonaws.com/{fileName}";
        }
    }
}