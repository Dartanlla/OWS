using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using OWSData.Repositories.Interfaces;
using OWSShared.Options;
using System.IO;
using System.IO.Compression;
using System.Text;
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

            var compressed = new MemoryStream();
            using (var gzip = new GZipStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
            {
                var bytes = Encoding.UTF8.GetBytes(contentJson);
                await gzip.WriteAsync(bytes, 0, bytes.Length);
            }
            compressed.Position = 0;

            var request = new PutObjectRequest
            {
                BucketName = _options.BucketName,
                Key = zoneName,
                InputStream = compressed,
                ContentType = "application/json",
                Headers = { ContentEncoding = "gzip" }
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
                Stream readStream = response.Headers.ContentEncoding == "gzip"
                    ? new GZipStream(response.ResponseStream, CompressionMode.Decompress)
                    : response.ResponseStream;
                using var reader = new StreamReader(readStream, Encoding.UTF8);
                return await reader.ReadToEndAsync();
            }
            catch (AmazonS3Exception ex) when (ex.ErrorCode == "NoSuchKey")
            {
                return null;
            }
        }
    }
}
