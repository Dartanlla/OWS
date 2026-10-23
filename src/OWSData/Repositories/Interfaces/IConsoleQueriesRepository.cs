using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OWSData.Models.Composites;

namespace OWSData.Repositories.Interfaces
{
    /// <summary>Read-only queries for the staff console (Postgres only). Every list is capped.</summary>
    public interface IConsoleQueriesRepository
    {
        Task<ConsoleOverview> GetOverview(Guid customerGUID);
        Task<IEnumerable<ConsoleAccountRow>> SearchAccounts(Guid customerGUID, string search, int limit);
        Task<IEnumerable<ConsoleCharacterRow>> SearchCharacters(Guid customerGUID, string search, int limit);
        Task<ConsoleAccountRow> GetAccount(Guid customerGUID, Guid userGUID);
        Task<IEnumerable<ConsoleCharacterRow>> GetCharactersForAccount(Guid customerGUID, Guid userGUID);
        Task<IEnumerable<AuditEntry>> GetLoginsForAccount(Guid customerGUID, Guid userGUID, int limit);
        Task<ConsoleCharacterDetail> GetCharacter(Guid customerGUID, string characterName);
        Task<IEnumerable<AuditEntry>> GetAuditForCharacter(Guid customerGUID, string characterName, int limit);
        Task<ConsoleDistribution> GetDistribution(Guid customerGUID);
        Task<ConsoleEconomy> GetEconomy(Guid customerGUID);
        Task<IEnumerable<ConsoleMigration>> GetMigrations(int limit);
        Task<IEnumerable<AuditEntry>> GetAudit(Guid customerGUID, string source, string action, string search, int limit);

        Task<ConsoleStaffUser> FindUserBySteamId(Guid customerGUID, string steamId);
        Task<ConsoleStaffUser> GetUser(Guid customerGUID, Guid userGUID);
        Task<IEnumerable<string>> GetOnlineCharacterNames(Guid customerGUID, Guid userGUID);
        Task<IEnumerable<Guid>> GetCustomerGUIDs();
    }
}
