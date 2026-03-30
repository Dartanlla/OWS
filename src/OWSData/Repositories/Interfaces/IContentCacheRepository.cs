using System.Threading.Tasks;

namespace OWSData.Repositories.Interfaces
{
    public interface IContentCacheRepository
    {
        Task AddOrUpdateContentCacheAsync(string zoneName, string contentJson);
        Task<string> GetContentCacheAsync(string zoneName);
    }
}
