using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OWSData.Repositories.Interfaces;
using OWSInstanceManagement.Services;
using OWSShared.Options;

namespace OWSInstanceManagement.Controllers
{
    [ApiController]
    [Route("api/Instance")]
    public class EdgeServersController : ControllerBase
    {
        private readonly IInstanceManagementRepository _repository;

        public EdgeServersController(IInstanceManagementRepository repository)
        {
            _repository = repository;
        }

        [HttpPost("AddEdgeServer")]
        [HttpPost("GetEdgeServer")]
        [HttpPost("GetEdgeServers")]
        [HttpPost("UpdateEdgeServer")]
        [HttpPost("DeleteEdgeServer")]
        [HttpPost("ProvisionEdgeServerCredential")]
        [HttpPost("SetEdgeServerReady")]
        [HttpPost("RevokeEdgeServerCredential")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> ManageEdgeServer([FromBody] EdgeServerRequest request)
        {
            var path = Request.Path.Value.TrimEnd('/');
            if (!EdgeServerOperations.TryParseOperation(path[(path.LastIndexOf('/') + 1)..], out var operation))
                return NotFound();

            var (status, payload) = await EdgeServerOperations.ExecuteAsync(operation, request,
                Request.Headers[ServerCredentials.LauncherHeader], Request.Headers[ServerCredentials.CredentialHeader], _repository);
            return StatusCode(status, payload);
        }
    }
}
