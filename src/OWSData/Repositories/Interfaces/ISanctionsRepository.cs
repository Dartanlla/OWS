using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OWSData.Models.Composites;

namespace OWSData.Repositories.Interfaces
{
    /// <summary>
    /// AccountSanctions (Postgres only): bans, mutes, warnings and notes, on the account.
    /// </summary>
    public interface ISanctionsRepository
    {
        Task<ActiveSanctions> GetActive(Guid customerGUID, Guid userGUID);
        Task<IEnumerable<Sanction>> GetForUser(Guid customerGUID, Guid userGUID);
        Task<Sanction> GetById(Guid customerGUID, Guid userGUID, long sanctionID);
        Task<IEnumerable<Sanction>> GetRecent(Guid customerGUID, bool activeOnly, int limit);
        Task<Guid?> GetUserForCharacter(Guid customerGUID, string characterName);

        /// <summary>A new ban or mute revokes the account's open one of the same kind, so one is active at a time.</summary>
        Task<long> Add(Guid customerGUID, Guid userGUID, string kind, string reason, string characterName, DateTime? expiresAtUtc,
            string issuedBy, string issuedFrom);

        /// <summary>Revokes the account's open sanctions of this kind. Returns how many were open.</summary>
        Task<int> RevokeActive(Guid customerGUID, Guid userGUID, string kind, string revokedBy);
        Task<bool> Revoke(Guid customerGUID, long sanctionID, string revokedBy);
    }
}
