using OWSData.Models.Content;
using System;
using System.Threading.Tasks;

namespace OWSData.Repositories.Interfaces
{
    public interface IContentRepository
    {
        Task<ContentFeature> GetContentFeatureAsync(Guid customerGuid, int contentFeatureId);
        Task<int> AddOrUpdateContentFeatureAsync(Guid customerGuid, ContentFeature contentFeature);
        Task<ContentPackage> GetContentPackageAsync(Guid customerGuid, int contentPackageId);
        Task<int> AddOrUpdateContentPackageAsync(Guid customerGuid, ContentPackage contentPackage);
    }
}
