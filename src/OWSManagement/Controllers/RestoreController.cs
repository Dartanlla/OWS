using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OWSData.Models.Composites;
using OWSData.Repositories.Implementations.Postgres;
using OWSData.Repositories.Interfaces;
using OWSManagement.Auth;
using OWSShared.Options;

namespace OWSManagement.Controllers
{
    /// <summary>
    /// Restore a character from an hourly backup. The host's staging loader (scripts/hetzner/staging) loads the chosen
    /// dump into a staging database when the console writes request.json, and reports through status.json. Admin only.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = ConsoleAuth.Admin)]
    public class RestoreController : Controller
    {
        private static readonly Regex DumpName = new(@"^openworldserver-[0-9A-Za-z\-]+\.dump$", RegexOptions.Compiled);

        private readonly ManagementOptions _options;
        private readonly CharacterRestoreRepository _restore;
        private readonly IZoneOpsRepository _zoneOps;
        private readonly ConsoleCustomer _customer;

        public RestoreController(IOptions<ManagementOptions> options, IOptions<StorageOptions> storage, IZoneOpsRepository zoneOps, ConsoleCustomer customer)
        {
            _options = options.Value;
            // The compose file always passes a connection string; without the installer's password restore stays off.
            _restore = string.IsNullOrWhiteSpace(_options.StagingConnectionString)
                || string.IsNullOrEmpty(new Npgsql.NpgsqlConnectionStringBuilder(_options.StagingConnectionString).Password)
                ? null
                : new CharacterRestoreRepository(storage.Value.OWSDBConnectionString, _options.StagingConnectionString);
            _zoneOps = zoneOps;
            _customer = customer;
        }

        [HttpGet]
        [Route("")]
        public IActionResult Get()
        {
            List<object> backups = new();
            if (Directory.Exists(_options.BackupDir))
            {
                backups.AddRange(new DirectoryInfo(_options.BackupDir).GetFiles("*.dump")
                    .OrderByDescending(f => f.LastWriteTimeUtc).Take(72)
                    .Select(f => (object)new { f.Name, Bytes = f.Length, WrittenAt = f.LastWriteTimeUtc }));
            }
            return Ok(new { Enabled = _restore != null, Backups = backups, Status = ReadStatus() });
        }

        public class StageInput
        {
            public string Dump { get; set; }
        }

        [HttpPost]
        [Route("Stage")]
        public async Task<IActionResult> Stage([FromBody] StageInput input)
        {
            string dump = input?.Dump?.Trim() ?? "";
            if (_restore == null || !DumpName.IsMatch(dump) || !System.IO.File.Exists(Path.Combine(_options.BackupDir, dump)))
            {
                return BadRequest(new SuccessAndErrorMessage { Success = false, ErrorMessage = "Pick a backup from the list." });
            }
            Directory.CreateDirectory(_options.StagingDir);
            string request = JsonSerializer.Serialize(new { dump, requestedBy = ConsoleAuth.Actor(User), at = DateTime.UtcNow });
            await System.IO.File.WriteAllTextAsync(Path.Combine(_options.StagingDir, "request.json"), request);
            return Ok(new SuccessAndErrorMessage { Success = true, ErrorMessage = "" });
        }

        [HttpGet]
        [Route("Character/{name}")]
        public async Task<IActionResult> Compare(string name)
        {
            if (!StagingReady(out IActionResult refusal)) return refusal;
            CharacterRestoreRepository.Snapshot live = await _restore.GetSnapshot(false, _customer.CustomerGUID, name);
            CharacterRestoreRepository.Snapshot backup = await _restore.GetSnapshot(true, _customer.CustomerGUID, name);
            if (live == null) return NotFound();
            return Ok(new { Live = live, Backup = backup, Status = ReadStatus() });
        }

        public class RestoreInput
        {
            /// <summary>The character's name, typed again.</summary>
            public string Confirm { get; set; }
            /// <summary>The backup the caller compared (the backup snapshot's dump); restore refuses any other.</summary>
            public string Dump { get; set; }
        }

        [HttpPost]
        [Route("Character/{name}")]
        public async Task<IActionResult> Restore(string name, [FromBody] RestoreInput input)
        {
            if (!StagingReady(out IActionResult refusal)) return refusal;
            if (!string.Equals(input?.Confirm?.Trim(), name?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new SuccessAndErrorMessage { Success = false, ErrorMessage = "Type the character's name to confirm." });
            }

            string error = await _restore.Restore(_customer.CustomerGUID, name, input.Dump);
            if (error != null)
            {
                string message = error switch
                {
                    "online" => "The character is in the world. Kick them, wait a minute, then restore.",
                    "not_in_backup" => "The backup has no character with this name.",
                    "different_character" => "This name belongs to a different character than in the backup.",
                    "staging_changed" => "Another backup is loaded or loading now. Compare again before you restore.",
                    _ => "No character has this name."
                };
                return Ok(new SuccessAndErrorMessage { Success = false, ErrorMessage = message });
            }

            await _zoneOps.AddEvents(_customer.CustomerGUID, 0, new[]
            {
                new GameEventIn
                {
                    EventGUID = Guid.NewGuid(),
                    EventTime = DateTime.UtcNow,
                    Event = "restored",
                    CharacterName = name.Trim(),
                    Fields = JsonSerializer.Serialize(new { backup = input.Dump, by = ConsoleAuth.Actor(User) })
                }
            });
            return Ok(new SuccessAndErrorMessage { Success = true, ErrorMessage = "" });
        }

        private bool StagingReady(out IActionResult refusal)
        {
            refusal = null;
            if (_restore == null)
            {
                refusal = BadRequest(new SuccessAndErrorMessage { Success = false, ErrorMessage = "Restore is not set up on this server." });
                return false;
            }
            JsonElement? status = ReadStatus();
            if (status?.TryGetProperty("state", out JsonElement state) != true || state.GetString() != "ready")
            {
                refusal = Conflict(new SuccessAndErrorMessage { Success = false, ErrorMessage = "Load a backup first." });
                return false;
            }
            return true;
        }

        /// <summary>status.json as the loader wrote it: state (loading, ready, failed, stopped), dump, readyAt, stopsAt, error.</summary>
        private JsonElement? ReadStatus()
        {
            string path = Path.Combine(_options.StagingDir, "status.json");
            try
            {
                if (!System.IO.File.Exists(path)) return null;
                using JsonDocument document = JsonDocument.Parse(System.IO.File.ReadAllText(path));
                return document.RootElement.Clone();
            }
            catch (Exception ex) when (ex is IOException or JsonException)
            {
                return null;
            }
        }
    }
}
