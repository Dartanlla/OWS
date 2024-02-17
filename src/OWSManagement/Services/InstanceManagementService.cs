using OWSData.Models.Tables;
using OWSData.Repositories.Interfaces;

namespace OWSManagement.Services
{
    public interface IInstanceManagementService
    {
        Task<IEnumerable<WorldServers>> GetWorldServersAsync();
    }
    public class InstanceManagementService : IInstanceManagementService
    {
        private readonly Guid _customerGuid;
        private readonly IInstanceManagementRepository _instanceManagementRepository;

        public InstanceManagementService(IInstanceManagementRepository instanceManagementRepository, IConfiguration config)
        {
            _customerGuid = new Guid(config["OWSManagementOptions:OWSAPIKey"]);
            _instanceManagementRepository = instanceManagementRepository;
        }
        public async Task<IEnumerable<WorldServers>> GetWorldServersAsync()
        {
            return await _instanceManagementRepository.GetActiveWorldServersByLoad(_customerGuid);
        }
    }
}
