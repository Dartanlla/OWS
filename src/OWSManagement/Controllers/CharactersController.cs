using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSManagement.DTOs;
using OWSManagement.Requests.Characters;
using OWSShared.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OWSManagement.Controllers
{
    /// <summary>
    /// Character administration for the management console.
    /// </summary>
    /// <remarks>
    /// These endpoints read and write across every user in the customer. They are gated by
    /// the X-CustomerGUID header alone, which is a tenant identifier rather than a
    /// credential, so this service must stay bound to localhost and be reached over an SSH
    /// tunnel until a real admin credential exists.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class CharactersController : Controller
    {
        private readonly IHeaderCustomerGUID _customerGuid;
        private readonly ICharactersRepository _charactersRepository;

        public CharactersController(IHeaderCustomerGUID customerGuid, ICharactersRepository charactersRepository)
        {
            _customerGuid = customerGuid;
            _charactersRepository = charactersRepository;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (_customerGuid.CustomerGUID == Guid.Empty)
            {
                context.Result = new UnauthorizedResult();
            }
        }

        /// <summary>
        /// Search Characters
        /// </summary>
        /// <remarks>
        /// Finds characters in this CustomerGUID whose name or owning email contains the
        /// search text. An empty search returns the first page of all characters. Capped at
        /// 200 rows.
        /// </remarks>
        [HttpGet]
        [Route("")]
        [Produces(typeof(IEnumerable<AdminCharacterSummary>))]
        public async Task<IEnumerable<AdminCharacterSummary>> Search([FromQuery] string search)
        {
            SearchCharactersRequest request = new SearchCharactersRequest(_customerGuid.CustomerGUID, search, _charactersRepository);

            return await request.Handle();
        }

        /// <summary>
        /// Get Characters for a User
        /// </summary>
        /// <remarks>
        /// Gets every character owned by the given UserGUID.
        /// </remarks>
        [HttpGet]
        [Route("ForUser/{userGuid:guid}")]
        [Produces(typeof(IEnumerable<AdminCharacterSummary>))]
        public async Task<IEnumerable<AdminCharacterSummary>> GetForUser(Guid userGuid)
        {
            GetCharactersForUserRequest request = new GetCharactersForUserRequest(_customerGuid.CustomerGUID, userGuid, _charactersRepository);

            return await request.Handle();
        }

        /// <summary>
        /// Set the per-character flags
        /// </summary>
        /// <remarks>
        /// Sets Characters.IsAdmin, Characters.IsModerator and
        /// Characters.IsInternalNetworkTestUser. Any flag omitted from the body is left
        /// unchanged, so one can be toggled without knowing the others.
        ///
        /// IsAdmin and IsModerator are read by the client from GetCharByCharName at connect,
        /// so they take effect on that character's next login.
        /// IsInternalNetworkTestUser is read server-side on every connect: it makes
        /// GetServerToConnectTo hand back 127.0.0.1 instead of the zone server's real IP.
        /// </remarks>
        [Authorize(Policy = Auth.ConsoleAuth.Admin)]
        [HttpPut]
        [Route("Flags")]
        [Produces(typeof(SuccessAndErrorMessage))]
        public async Task<SuccessAndErrorMessage> SetFlags([FromBody] SetCharacterFlagsDTO dto)
        {
            SetCharacterFlagsRequest request = new SetCharacterFlagsRequest(_customerGuid.CustomerGUID, dto, _charactersRepository);

            return await request.Handle();
        }
    }
}
