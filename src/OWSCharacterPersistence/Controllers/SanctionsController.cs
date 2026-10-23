using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;

namespace OWSCharacterPersistence.Controllers
{
    /// <summary>
    /// Account bans, mutes and warnings for the in-game staff commands and the zone server's login check. A character
    /// name resolves to its account, so a sanction covers every character on it. Service key only.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class SanctionsController : Controller
    {
        private readonly ISanctionsRepository _sanctions;
        private readonly IHeaderCustomerGUID _customerGuid;

        public SanctionsController(ISanctionsRepository sanctions, IHeaderCustomerGUID customerGuid)
        {
            _sanctions = sanctions;
            _customerGuid = customerGuid;
        }

        public class CharacterRequest
        {
            public string CharacterName { get; set; }
        }

        public class AddRequest
        {
            public string CharacterName { get; set; }
            public string Kind { get; set; }
            public string Reason { get; set; }

            /// <summary>Ban and mute only; 0 or less is permanent.</summary>
            public int Minutes { get; set; }

            public string IssuedBy { get; set; }
        }

        public class RevokeRequest
        {
            public string CharacterName { get; set; }
            public string Kind { get; set; }
            public string RevokedBy { get; set; }
        }

        public class ActiveSanctionView
        {
            public string Reason { get; set; }
            public DateTime? ExpiresAt { get; set; }
            public string IssuedBy { get; set; }
            public string Message { get; set; }
        }

        [HttpPost]
        [Route("Active")]
        public async Task<IActionResult> Active([FromBody] CharacterRequest request)
        {
            if (!SamsaraServiceKey.IsValid(Request)) return Unauthorized();

            Guid? userGUID = await _sanctions.GetUserForCharacter(_customerGuid.CustomerGUID, request?.CharacterName);
            if (!userGUID.HasValue) return Ok(new { Found = false });

            ActiveSanctions active = await _sanctions.GetActive(_customerGuid.CustomerGUID, userGUID.Value);
            return Ok(new { Found = true, Ban = View(active.Ban), Mute = View(active.Mute) });
        }

        [HttpPost]
        [Route("Add")]
        public async Task<IActionResult> Add([FromBody] AddRequest request)
        {
            if (!SamsaraServiceKey.IsValid(Request)) return Unauthorized();

            string kind = request?.Kind?.Trim().ToLowerInvariant();
            if (kind != SanctionKinds.Ban && kind != SanctionKinds.Mute && kind != SanctionKinds.Warn)
            {
                return Ok(new SuccessAndErrorMessage { Success = false, ErrorMessage = "unknown_kind" });
            }

            Guid? userGUID = await _sanctions.GetUserForCharacter(_customerGuid.CustomerGUID, request.CharacterName);
            if (!userGUID.HasValue)
            {
                return Ok(new SuccessAndErrorMessage { Success = false, ErrorMessage = "unknown_character" });
            }

            DateTime? expiresAt = request.Minutes > 0 ? DateTime.UtcNow.AddMinutes(request.Minutes) : null;
            await _sanctions.Add(_customerGuid.CustomerGUID, userGUID.Value, kind, request.Reason, request.CharacterName, expiresAt,
                string.IsNullOrWhiteSpace(request.IssuedBy) ? "zone" : request.IssuedBy, "zone");
            return Ok(new SuccessAndErrorMessage { Success = true, ErrorMessage = "" });
        }

        [HttpPost]
        [Route("Revoke")]
        public async Task<IActionResult> Revoke([FromBody] RevokeRequest request)
        {
            if (!SamsaraServiceKey.IsValid(Request)) return Unauthorized();

            string kind = request?.Kind?.Trim().ToLowerInvariant();
            if (kind != SanctionKinds.Ban && kind != SanctionKinds.Mute)
            {
                return Ok(new SuccessAndErrorMessage { Success = false, ErrorMessage = "unknown_kind" });
            }

            Guid? userGUID = await _sanctions.GetUserForCharacter(_customerGuid.CustomerGUID, request.CharacterName);
            if (!userGUID.HasValue)
            {
                return Ok(new SuccessAndErrorMessage { Success = false, ErrorMessage = "unknown_character" });
            }

            int revoked = await _sanctions.RevokeActive(_customerGuid.CustomerGUID, userGUID.Value, kind,
                string.IsNullOrWhiteSpace(request.RevokedBy) ? "zone" : request.RevokedBy);
            return Ok(new SuccessAndErrorMessage { Success = revoked > 0, ErrorMessage = revoked > 0 ? "" : "not_active" });
        }

        private static ActiveSanctionView View(Sanction sanction)
        {
            return sanction == null ? null : new ActiveSanctionView
            {
                Reason = sanction.Reason,
                ExpiresAt = sanction.ExpiresAt,
                IssuedBy = sanction.IssuedBy,
                Message = sanction.PlayerMessage()
            };
        }
    }
}
