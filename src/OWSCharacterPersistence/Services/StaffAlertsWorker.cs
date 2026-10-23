using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OWSData.Models.Composites;
using OWSData.Repositories.Implementations.Postgres;
using OWSShared.Options;
using Serilog;

namespace OWSCharacterPersistence.Services
{
    /// <summary>
    /// Posts staff alerts to Discord (OWS_STAFF_DISCORD_WEBHOOK_URL) once a minute: new player reports, bans and
    /// unbans, staff gifts, unexpected zone exits, failing saves, newly seen duplicate items, and maintenance and rate
    /// events starting and ending. Cursors live in ConsoleSettings, so a restart does not post anything twice.
    /// Without a webhook it does nothing.
    /// </summary>
    public class StaffAlertsWorker : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);
        private static readonly string[] RowKinds = { "reports", "bans", "unbans", "gifts", "zone_exits" };
        private const int FailedSavesThreshold = 5;
        private const int MaxMessageLength = 1900;

        private readonly string _webhookUrl;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly StaffAlertsRepository _alerts;
        private readonly ConsoleSettingsRepository _settings;

        public StaffAlertsWorker(IConfiguration configuration, IHttpClientFactory httpClientFactory, IOptions<StorageOptions> storageOptions)
        {
            _webhookUrl = configuration["OWS_STAFF_DISCORD_WEBHOOK_URL"];
            _httpClientFactory = httpClientFactory;
            _alerts = new StaffAlertsRepository(storageOptions);
            _settings = new ConsoleSettingsRepository(storageOptions);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (string.IsNullOrWhiteSpace(_webhookUrl))
            {
                Log.Information("staff_alerts_off: OWS_STAFF_DISCORD_WEBHOOK_URL is not set");
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Check();
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "staff_alerts_failed");
                }
                await Task.Delay(Interval, stoppingToken);
            }
        }

        private async Task Check()
        {
            Dictionary<string, string> settings = new(await _settings.GetAll(), StringComparer.OrdinalIgnoreCase);
            List<string> lines = new();
            Dictionary<string, string> cursors = new();

            foreach (string kind in RowKinds)
            {
                string key = ConsoleSettingKeys.AlertsCursorPrefix + kind;
                if (!settings.TryGetValue(key, out string stored) || !long.TryParse(stored, out long after))
                {
                    cursors[key] = (await _alerts.CurrentCursor(kind)).ToString(CultureInfo.InvariantCulture);
                    continue;
                }
                List<StaffAlertsRepository.AlertRow> rows = await _alerts.Read(kind, after);
                if (rows.Count == 0) continue;
                lines.AddRange(rows.Select(r => r.Text));
                cursors[key] = rows.Max(r => r.Cursor).ToString(CultureInfo.InvariantCulture);
            }

            DateTime now = DateTime.UtcNow;
            (int failed, string endpoints) = await _alerts.FailedSaves();
            string failedKey = ConsoleSettingKeys.AlertsCursorPrefix + "failed_saves";
            if (failed > FailedSavesThreshold && !RecentlySent(settings, failedKey, now, TimeSpan.FromMinutes(30)))
            {
                lines.Add($"Saves failing: {failed} in the last 10 minutes ({endpoints})");
                cursors[failedKey] = now.ToString("O", CultureInfo.InvariantCulture);
            }

            string duplicatesKey = ConsoleSettingKeys.AlertsCursorPrefix + "duplicates";
            List<(string InstanceId, string Holders)> duplicates = await _alerts.Duplicates();
            HashSet<string> known = settings.TryGetValue(duplicatesKey, out string knownList)
                ? knownList.Split(',', StringSplitOptions.RemoveEmptyEntries).Where(id => id != "-").ToHashSet()
                : null;
            if (known != null)
            {
                lines.AddRange(duplicates.Where(d => !known.Contains(d.InstanceId))
                    .Select(d => $"Duplicate item: one item ID is held by {d.Holders}"));
            }
            string duplicateList = string.Join(",", duplicates.Select(d => d.InstanceId));
            if (known == null || !known.SetEquals(duplicates.Select(d => d.InstanceId)))
            {
                cursors[duplicatesKey] = duplicateList == "" ? "-" : duplicateList;
            }

            AddTransition(lines, cursors, settings, "maintenance", MaintenanceState(settings, now));
            AddTransition(lines, cursors, settings, "rate_event", RateState(settings, now));

            if (lines.Count > 0)
            {
                await Post(lines);
            }
            foreach ((string key, string value) in cursors)
            {
                await _settings.Set(key, value, "alerts");
            }
        }

        /// <summary>Posts the state's line when the state ("" for none) differs from the one last stored.</summary>
        private static void AddTransition(List<string> lines, Dictionary<string, string> cursors, Dictionary<string, string> settings,
            string kind, (string State, string Line) current)
        {
            string key = ConsoleSettingKeys.AlertsCursorPrefix + kind;
            string stored = settings.TryGetValue(key, out string value) ? value : "";
            if (stored == current.State) return;
            lines.Add(current.Line);
            cursors[key] = current.State;
        }

        private static (string State, string Line) MaintenanceState(IReadOnlyDictionary<string, string> settings, DateTime now)
        {
            MaintenanceWindow window = ConsoleSettingValues.Maintenance(settings);
            if (window == null || !window.Started(now)) return ("", "Maintenance ended.");
            return ($"started:{window.StartsAt:O}", $"Maintenance started: {window.PlayerMessage()}");
        }

        private static (string State, string Line) RateState(IReadOnlyDictionary<string, string> settings, DateTime now)
        {
            RateEvent rates = ConsoleSettingValues.Rates(settings);
            if (rates == null || !rates.InForce(now)) return ("", "Rate event ended.");
            return ($"started:{rates.StartsAt:O}",
                $"Rate event started: {rates.Name} (EXP x{rates.Exp:0.##}, drops x{rates.Drop:0.##}) until {rates.EndsAt:yyyy-MM-dd HH:mm} UTC");
        }

        private static bool RecentlySent(IReadOnlyDictionary<string, string> settings, string key, DateTime now, TimeSpan window)
        {
            return settings.TryGetValue(key, out string value)
                && DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime sentAt)
                && now - sentAt.ToUniversalTime() < window;
        }

        private async Task Post(List<string> lines)
        {
            StringBuilder content = new();
            int shown = 0;
            foreach (string line in lines)
            {
                string escaped = EscapeMarkdown(line);
                if (content.Length + escaped.Length + 1 > MaxMessageLength) break;
                content.AppendLine(escaped);
                shown++;
            }
            if (shown < lines.Count)
            {
                content.Append($"... and {lines.Count - shown} more. See the staff console.");
            }

            using HttpResponseMessage response = await _httpClientFactory.CreateClient("Discord").PostAsJsonAsync(_webhookUrl, new
            {
                content = content.ToString(),
                allowed_mentions = new { parse = Array.Empty<string>() }
            });
            if (!response.IsSuccessStatusCode)
            {
                Log.Warning("staff_alerts_post_failed {Status}", (int)response.StatusCode);
            }
        }

        private static string EscapeMarkdown(string text)
        {
            StringBuilder escaped = new(text.Length);
            foreach (char c in text)
            {
                if ("\\*_~|`>#-".IndexOf(c) >= 0) escaped.Append('\\');
                escaped.Append(c == '\n' || c == '\r' ? ' ' : c);
            }
            return escaped.ToString();
        }
    }
}
