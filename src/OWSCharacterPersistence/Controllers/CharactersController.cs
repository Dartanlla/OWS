using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using OWSCharacterPersistence.Requests.Characters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OWSData.Models.Tables;
using OWSData.Models.Composites;

namespace OWSCharacterPersistence.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CharactersController : Controller
    {
        private readonly ICharactersRepository _charactersRepository;
        private readonly IJoinTicketsRepository _joinTicketsRepository;
        private readonly ICharacterOwnersRepository _ownersRepository;
        private readonly IHeaderCustomerGUID _customerGuid;

        public CharactersController(ICharactersRepository charactersRepository,
            IJoinTicketsRepository joinTicketsRepository,
            ICharacterOwnersRepository ownersRepository,
            IHeaderCustomerGUID customerGuid)
        {
            _charactersRepository = charactersRepository;
            _joinTicketsRepository = joinTicketsRepository;
            _ownersRepository = ownersRepository;
            _customerGuid = customerGuid;
        }

        // Every save names its character; once the character has an owner, only the owner's token may write it.
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            OnActionExecuting(context);
            if (context.Result != null)
            {
                return;
            }

            foreach (object argument in context.ActionArguments.Values)
            {
                if (argument is IOwnedCharacterWrite write && !String.IsNullOrEmpty(write.OwnedCharacterName)
                    && await _ownersRepository.IsStaleOwnerWrite(_customerGuid.CustomerGUID, write.OwnedCharacterName, write.OwnerToken))
                {
                    context.Result = new OkObjectResult(new StaleOwnerResult());
                    return;
                }
            }

            OnActionExecuted(await next());
        }

        // Server-to-server gate for the persistence API (mirrors PlayerShopsController). These
        // endpoints take a client-supplied CharName and write gold/inventory/stats with no
        // ownership binding, so on the shared-CustomerGUID model any caller could act as any
        // character. Gated by OWS_REQUIRE_CHARACTER_WRITE_KEY (default off) because the current UE
        // persistence calls send only X-CustomerGUID, not the service key — turning it on before
        // the client sends X-Samsara-Service-Key would reject all persistence traffic. When off,
        // behaviour is unchanged (middleware CustomerGUID check only).
        private const string ServiceKeyHeader = "X-Samsara-Service-Key";
        private static readonly string ConfiguredServiceKey =
            Environment.GetEnvironmentVariable("OWS_SAMSARA_SERVICE_KEY");
        private static readonly bool RequireServiceKey =
            string.Equals(Environment.GetEnvironmentVariable("OWS_REQUIRE_CHARACTER_WRITE_KEY"), "true", StringComparison.OrdinalIgnoreCase);

        // True when these endpoints are writing gold/inventory with no credential beyond the
        // X-CustomerGUID header, which is not a secret. Startup logs this so the posture is visible
        // rather than inferred from an env var nobody set. See docs/mvp-hardening-spec.md item 4.
        public static bool IsUnauthenticated => !RequireServiceKey;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!ServiceKeyOk())
            {
                context.Result = new UnauthorizedResult();
                return;
            }
            base.OnActionExecuting(context);
        }

        private bool ServiceKeyOk()
        {
            // Nothing configured to verify against. Fail closed when the gate is required, so a
            // missing env var on a deploy surfaces as 401s instead of silently opening the door.
            if (string.IsNullOrEmpty(ConfiguredServiceKey))
            {
                return !RequireServiceKey;
            }

            bool headerPresent = Request.Headers.TryGetValue(ServiceKeyHeader, out var provided)
                   && !string.IsNullOrEmpty(provided.ToString());

            if (headerPresent
                && System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                       System.Text.Encoding.UTF8.GetBytes(provided.ToString()),
                       System.Text.Encoding.UTF8.GetBytes(ConfiguredServiceKey)))
            {
                return true;
            }

            // Key configured but not matched. A caller that presented a *wrong* key is rejected
            // whatever the toggle says: accepting it would let a misconfigured zone server look
            // healthy right up until the toggle flips. A caller that presented none — including a
            // proxy that injects the header empty, which is absence, not a wrong key — is still
            // allowed while the gate is opt-in.
            return !RequireServiceKey && !headerPresent;
        }

        [HttpPost]
        [Route("GetSaveDataByName")]
        [Produces(typeof(CharacterSaveData))]
        public async Task<IActionResult> GetSaveDataByName([FromBody] GetSaveDataByNameRequest request)
        {
            request.SetData(_charactersRepository, _customerGuid);
            return await request.Handle();
        }

        [HttpPost]
        [Route("GetStatsByName")]
        [Produces(typeof(GetCharStatsByCharName))]
        public async Task<IActionResult> GetStatsByName([FromBody] GetStatsByNameRequest request)
        {
            request.SetData(_charactersRepository, _customerGuid);
            return await request.Handle();
        }

        [HttpPost]
        [Route("GetQuestsByName")]
        [Produces(typeof(GetCharQuestsByCharName))]
        public async Task<IActionResult> GetQuestsByName([FromBody] GetQuestsByNameRequest request)
        {
            request.SetData(_charactersRepository, _customerGuid);
            return await request.Handle();
        }

        [HttpPost]
        [Route("GetInventoryByName")]
        [Produces(typeof(GetCharInventoryByCharName))]
        public async Task<IActionResult> GetInventoryByName([FromBody] GetInventoryByNameRequest request)
        {
            request.SetData(_charactersRepository, _customerGuid);
            return await request.Handle();
        }

        [HttpPost]
        [Route("GetByName")]
        [Produces(typeof(GetCharByCharName))]
        public async Task<IActionResult> GetByName([FromBody] GetByNameRequest request)
        {
            request.SetData(_charactersRepository, _customerGuid);
            return await request.Handle();
        }

        // The zone server learns who joins from this reply, so the service key is required whatever
        // OWS_REQUIRE_CHARACTER_WRITE_KEY says.
        [HttpPost]
        [Route("RedeemJoinTicket")]
        [Produces(typeof(RedeemJoinTicketResult))]
        public async Task<IActionResult> RedeemJoinTicket([FromBody] RedeemJoinTicketRequest request)
        {
            if (!SamsaraServiceKey.IsValid(Request)) return Unauthorized();
            request.SetData(_joinTicketsRepository, _customerGuid);
            return await request.Handle();
        }

        [HttpPost]
        [Route("ClaimCharacter")]
        [Produces(typeof(ClaimCharacterResult))]
        public async Task<IActionResult> ClaimCharacter([FromBody] ClaimCharacterRequest request)
        {
            if (!SamsaraServiceKey.IsValid(Request)) return Unauthorized();
            request.SetData(_ownersRepository, _customerGuid);
            return await request.Handle();
        }

        [HttpPost]
        [Route("CancelJoinTicket")]
        [Produces(typeof(CancelJoinTicketResult))]
        public async Task<IActionResult> CancelJoinTicket([FromBody] CancelJoinTicketRequest request)
        {
            if (!SamsaraServiceKey.IsValid(Request)) return Unauthorized();
            request.SetData(_ownersRepository, _customerGuid);
            return await request.Handle();
        }

        [HttpPost]
        [Route("CheckOwners")]
        [Produces(typeof(CheckOwnersResult))]
        public async Task<IActionResult> CheckOwners([FromBody] CheckOwnersRequest request)
        {
            if (!SamsaraServiceKey.IsValid(Request)) return Unauthorized();
            request.SetData(_ownersRepository, _customerGuid);
            return await request.Handle();
        }

        [HttpPost]
        [Route("UpdateAllPlayerPositions")]
        [Produces(typeof(SuccessAndErrorMessage))]
        public async Task<SuccessAndErrorMessage> UpdateAllPlayerPositions([FromBody] UpdateAllPlayerPositionsRequest request)
        {
            request.SetData(_charactersRepository, _ownersRepository, _customerGuid);
            return await request.Handle();
        }

        [HttpPost]
        [Route("UpdateCharacterData")]
        [Produces(typeof(SuccessAndErrorMessage))]
        public async Task<SuccessAndErrorMessage> UpdateCharacterData([FromBody] UpdateCharacterDataRequest request)
        {
            request.SetData(_charactersRepository, _customerGuid);
            return await request.Handle();
        }

        [HttpPost]
        [Route("UpdateCharacterStats")]
        [Produces(typeof(SuccessAndErrorMessage))]
        public async Task<SuccessAndErrorMessage> UpdateCharacterStats([FromBody] UpdateCharacterStatsRequest request)
        {
            request.SetData(_charactersRepository, _customerGuid);
            return await request.Handle();
        }

        [HttpPost]
        [Route("UpdateCharacterQuests")]
        [Produces(typeof(SuccessAndErrorMessage))]
        public async Task<SuccessAndErrorMessage> UpdateCharacterQuests([FromBody] UpdateCharacterQuestsRequest request)
        {
            request.SetData(_charactersRepository, _customerGuid);
            return await request.Handle();
        }

        [HttpPost]
        [Route("UpdateCharacterInventory")]
        [Produces(typeof(UpdateCharacterInventoryResponse))]
        public async Task<UpdateCharacterInventoryResponse> UpdateCharacterInventory([FromBody] UpdateCharacterInventoryRequest request)
        {
            request.SetData(_charactersRepository, _customerGuid);
            return await request.Handle();
        }

        [HttpPost]
        [Route("UpdateCharacterCurrency")]
        [Produces(typeof(UpdateCharacterCurrencyResponse))]
        public async Task<UpdateCharacterCurrencyResponse> UpdateCharacterCurrency([FromBody] UpdateCharacterCurrencyRequest request)
        {
            request.SetData(_charactersRepository, _customerGuid);
            return await request.Handle();
        }

        [HttpPost]
        [Route("UpdateCharacterClass")]
        [Produces(typeof(SuccessAndErrorMessage))]
        public async Task<SuccessAndErrorMessage> UpdateCharacterClass([FromBody] UpdateCharacterClassRequest request)
        {
            request.SetData(_charactersRepository, _customerGuid);
            return await request.Handle();
        }

        [HttpPost]
        [Route("UpdateCharacterAbilities")]
        [Produces(typeof(SuccessAndErrorMessage))]
        public async Task<SuccessAndErrorMessage> UpdateCharacterAbilities([FromBody] UpdateCharacterAbilitiesRequest request)
        {
            request.SetData(_charactersRepository, _customerGuid);
            return await request.Handle();
        }

        [HttpPost]
        [Route("GetCustomData")]
        [Produces(typeof(CustomCharacterDataRows))]
        public async Task<CustomCharacterDataRows> GetCustomData([FromBody] GetCustomDataRequest request)
        {
            request.SetData(_charactersRepository, _customerGuid);
            return await request.Handle();
        }

        [HttpPost]
        [Route("AddOrUpdateCustomData")]
        public async Task AddOrUpdateCustomData([FromBody] AddOrUpdateCustomDataRequest request)
        {
            request.SetData(_charactersRepository, _customerGuid);
            await request.Handle();

            return;
        }

        [HttpPost]
        [Route("PlayerLogout")]
        [Produces(typeof(SuccessAndErrorMessage))]
        public async Task<SuccessAndErrorMessage> PlayerLogout([FromBody] PlayerLogoutRequest request)
        {
            request.SetData(_charactersRepository, _customerGuid);
            return await request.Handle();
        }

        [HttpPost]
        [Route("AddQuestListToDatabase")]
        [Produces(typeof(SuccessAndErrorMessage))]
        public async Task<SuccessAndErrorMessage> AddQuestListToDatabase([FromBody] AddQuestListToDatabaseRequest request)
        {
            request.SetData(_charactersRepository, _customerGuid);
            return await request.Handle();
        }

        [HttpPost]
        [Route("GetQuestsFromDatabase")]
        [Produces(typeof(GetQuestsFromDb))]
        public async Task<IActionResult> GetQuestsFromDatabase(GetQuestsFromDatabaseRequest request)
        {
            request.SetData(_charactersRepository, _customerGuid);
            return await request.Handle();
        }

        [HttpPost]
        [Route("GetAbilitiesByName")]
        [Produces(typeof(IEnumerable<CharacterAbilityDto>))]
        public async Task<IActionResult> GetAbilitiesByName([FromBody] GetAbilitiesByNameRequest request)
        {
            request.SetData(_charactersRepository, _customerGuid);
            return await request.Handle();
        }
    }
}
