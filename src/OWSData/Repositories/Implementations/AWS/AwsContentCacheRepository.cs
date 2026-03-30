using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using OWSData.Repositories.Interfaces;
using OWSShared.Options;
using System.IO;
using System.Threading.Tasks;

namespace OWSData.Repositories.Implementations.AWS
{
    public class AwsContentCacheRepository : IContentCacheRepository
    {
        private readonly ContentCacheOptions _options;
        private readonly AmazonS3Client _s3Client;
        private bool _bucketInitialized = false;

        public AwsContentCacheRepository(IOptions<ContentCacheOptions> options)
        {
            _options = options.Value;
            var config = new AmazonS3Config
            {
                ServiceURL = _options.Endpoint,
                ForcePathStyle = true
            };
            _s3Client = new AmazonS3Client(_options.AccessKey, _options.SecretKey, config);
        }

        private async Task EnsureBucketAsync()
        {
            if (_bucketInitialized) return;

            try
            {
                await _s3Client.PutBucketAsync(_options.BucketName);
            }
            catch (AmazonS3Exception ex) when (ex.ErrorCode == "BucketAlreadyOwnedByYou")
            {
                // Bucket already exists, this is fine
            }

            _bucketInitialized = true;
        }

        public async Task AddOrUpdateContentCacheAsync(string zoneName, string contentJson)
        {
            await EnsureBucketAsync();

            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream, leaveOpen: true);
            await writer.WriteAsync(contentJson);
            await writer.FlushAsync();
            stream.Position = 0;

            var request = new PutObjectRequest
            {
                BucketName = _options.BucketName,
                Key = zoneName,
                InputStream = stream,
                ContentType = "application/json"
            };

            await _s3Client.PutObjectAsync(request);
        }

        public async Task<string> GetContentCacheAsync(string zoneName)
        {
            await EnsureBucketAsync();

            try
            {
                var request = new GetObjectRequest
                {
                    BucketName = _options.BucketName,
                    Key = zoneName
                };

                using var response = await _s3Client.GetObjectAsync(request);
                using var reader = new StreamReader(response.ResponseStream);
                return await reader.ReadToEndAsync();
            }
            catch (AmazonS3Exception ex) when (ex.ErrorCode == "NoSuchKey")
            {
                return null;
            }
        }
    }
}
