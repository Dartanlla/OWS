using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;
using OWSData.Models.Content;
using OWSData.Repositories.Interfaces;
using OWSData.SQL;
using OWSShared.Options;
using System;
using System.Data;
using System.Threading.Tasks;

namespace OWSData.Repositories.Implementations.Postgres
{
    public class ContentRepository : IContentRepository
    {
        private readonly IOptions<StorageOptions> _storageOptions;

        public ContentRepository(IOptions<StorageOptions> storageOptions)
        {
            _storageOptions = storageOptions;
        }

        private IDbConnection Connection => new NpgsqlConnection(_storageOptions.Value.OWSDBConnectionString);

        public async Task<ContentFeature> GetContentFeatureAsync(Guid customerGuid, int contentFeatureId)
        {
            using var connection = (NpgsqlConnection)Connection;
            var parameters = new { CustomerGuid = customerGuid, ContentFeatureId = contentFeatureId };
            return await connection.QueryFirstOrDefaultAsync<ContentFeature>(ContentQueries.GetContentFeature, parameters, commandType: CommandType.Text);
        }

        public async Task<int> AddOrUpdateContentFeatureAsync(Guid customerGuid, ContentFeature contentFeature)
        {
            using var connection = (NpgsqlConnection)Connection;
            try
            {
                var parameters = new { CustomerGuid = customerGuid, contentFeature.ContentFeatureId };
                var existing = await connection.QueryFirstOrDefaultAsync<ContentFeature>(ContentQueries.GetContentFeature, parameters, commandType: CommandType.Text);

                if (existing != null)
                {
                    var updateParams = new
                    {
                        CustomerGuid = customerGuid,
                        contentFeature.ContentFeatureId,
                        contentFeature.ContentFeatureJson,
                        ModifiedAt = DateTime.UtcNow,
                        contentFeature.ArchivedAt,
                        contentFeature.ContentPackageId
                    };
                    await connection.ExecuteAsync(ContentQueries.UpdateContentFeature, updateParams, commandType: CommandType.Text);
                    return contentFeature.ContentFeatureId;
                }
                else
                {
                    var insertParams = new
                    {
                        CustomerGuid = customerGuid,
                        contentFeature.ContentFeatureJson,
                        contentFeature.CreatedBy,
                        CreatedAt = DateTime.UtcNow,
                        ModifiedAt = DateTime.UtcNow,
                        contentFeature.ArchivedAt,
                        contentFeature.ContentPackageId
                    };
                    return await connection.ExecuteScalarAsync<int>(ContentQueries.InsertContentFeature, insertParams, commandType: CommandType.Text);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AddOrUpdateContentFeatureAsync Error: {ex.Message}");
                throw;
            }
        }

        public async Task<ContentPackage> GetContentPackageAsync(Guid customerGuid, int contentPackageId)
        {
            using var connection = (NpgsqlConnection)Connection;
            var parameters = new { CustomerGuid = customerGuid, ContentPackageId = contentPackageId };
            return await connection.QueryFirstOrDefaultAsync<ContentPackage>(ContentQueries.GetContentPackage, parameters, commandType: CommandType.Text);
        }

        public async Task<int> AddOrUpdateContentPackageAsync(Guid customerGuid, ContentPackage contentPackage)
        {
            using var connection = (NpgsqlConnection)Connection;
            try
            {
                var parameters = new { CustomerGuid = customerGuid, contentPackage.ContentPackageId };
                var existing = await connection.QueryFirstOrDefaultAsync<ContentPackage>(ContentQueries.GetContentPackage, parameters, commandType: CommandType.Text);

                if (existing != null)
                {
                    var updateParams = new
                    {
                        CustomerGuid = customerGuid,
                        contentPackage.ContentPackageId,
                        contentPackage.AggregatedContentJson,
                        contentPackage.VersionNumber,
                        ModifiedAt = DateTime.UtcNow,
                        contentPackage.ArchivedAt,
                        contentPackage.DeployAt
                    };
                    await connection.ExecuteAsync(ContentQueries.UpdateContentPackage, updateParams, commandType: CommandType.Text);
                    return contentPackage.ContentPackageId;
                }
                else
                {
                    var insertParams = new
                    {
                        CustomerGuid = customerGuid,
                        contentPackage.AggregatedContentJson,
                        contentPackage.VersionNumber,
                        CreatedAt = DateTime.UtcNow,
                        ModifiedAt = DateTime.UtcNow,
                        contentPackage.ArchivedAt,
                        contentPackage.DeployAt
                    };
                    return await connection.ExecuteScalarAsync<int>(ContentQueries.InsertContentPackage, insertParams, commandType: CommandType.Text);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AddOrUpdateContentPackageAsync Error: {ex.Message}");
                throw;
            }
        }
    }
}
