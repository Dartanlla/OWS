using System.Collections.Generic;
using System.Threading.Tasks;
using OWSData.Models.Composites;

namespace OWSData.Repositories.Interfaces
{
    /// <summary>
    /// ConsoleSettings (Postgres only): switches the staff console owns. Get reads through a 10 s cache, so the public
    /// endpoints that check a switch on every call do not query the database each time.
    /// </summary>
    public interface IConsoleSettingsRepository
    {
        Task<IReadOnlyDictionary<string, string>> GetAll();
        Task<IEnumerable<ConsoleSetting>> GetAllRows();

        /// <summary>An empty value deletes the setting.</summary>
        Task Set(string key, string value, string updatedBy);
    }
}
