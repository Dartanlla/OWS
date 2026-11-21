using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OWSData.Repositories.Interfaces;
using OWSInstanceManagement.Services;
using OWSShared.Options;

namespace OWSInstanceManagement.Controllers
{
    [ApiController]
    [Route("api/Instance")]
    public class ConnectionTicketsController : ControllerBase
    {
        private readonly IInstanceManagementRepository _instances;
        private readonly ILogger<ConnectionTicketsController> _logger;

        public ConnectionTicketsController(ILogger<ConnectionTicketsController> logger, IInstanceManagementRepository instances)
        {
            _logger = logger;
            _instances = instances;
        }

        [HttpPost("CheckReadyAssignment")]
        [HttpPost("GetReadyMapInstances")]
        [HttpPost("AddMapInstance")]
        public async Task<IActionResult> InstanceOperation([FromBody] MapInstanceOperation request)
        {
            var action = Request.Path.Value.TrimEnd('/').Split('/')[^1];
            var result = await MapInstanceOperations.ExecuteAsync(action, request,
                Request.Headers[ServerCredentials.LauncherHeader], Request.Headers[ServerCredentials.AssignmentHeader], _instances);
            return StatusCode(result.Status, result.Payload);
        }

        /// <summary>
        /// Allows an authenticated Edge service to inspect and then atomically claim
        /// an issued connection ticket. This is not a client-facing endpoint.
        /// </summary>
        [HttpPost]
        [Route("RedeemExternalConnectionTicket")]
        [Produces(typeof(RedeemExternalConnectionTicketResponse))]
        public async Task<IActionResult> RedeemExternalConnectionTicket(
            [FromBody] RedeemExternalConnectionTicketRequest request,
            [FromServices] ITicketRepository ticketRepository = null)
        {
            Response.Headers["Cache-Control"] = "no-store";
            if (!Request.IsHttps ||
                !int.TryParse(Environment.GetEnvironmentVariable("OWS_EDGE_PRIVATE_HTTPS_PORT"), out var privatePort) ||
                privatePort <= 0 || HttpContext.Connection.LocalPort != privatePort)
                return NotFound();

            if (!ExternalConnectionTicketRedemption.IsValid(request))
            {
                return BadRequest(
                    "Ticket, a positive ServerId, and ClaimantId when claiming are required.");
            }
            if (ticketRepository == null)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    "The connection ticket store is not configured.");
            }

            try
            {
                var hash = ServerCredentials.Hash(Request.Headers[ServerCredentials.CredentialHeader]);
                var tenant = hash == null ? null : await _instances.AuthenticateActiveEdgeServer(request.ServerId, hash);
                if (!tenant.HasValue)
                    return Unauthorized("Invalid Edge server credential.");

                var ticket = await ExternalConnectionTicketRedemption.ExecuteAsync(
                    ticketRepository, request, tenant.Value);
                return ticket == null
                    ? NotFound("The ticket is unavailable, expired, assigned to another server, or already claimed.")
                    : Ok(ExternalConnectionTicketRedemption.ToResponse(ticket));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception,
                    "Could not {Operation} connection ticket for ServerId {ServerId}.",
                    request.Claim ? "claim" : "inspect", request.ServerId);
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    "The connection ticket store is unavailable.");
            }
        }

    }
}
