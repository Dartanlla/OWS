using System;
using System.Collections.Generic;
using System.Text.Json;

namespace OWSData.Models.Composites
{
    /// <summary>One AccountSanctions row. Active is computed: a ban or mute that is not revoked and not expired.</summary>
    public class Sanction
    {
        public long SanctionID { get; set; }
        public Guid UserGUID { get; set; }
        public string Kind { get; set; }
        public string Reason { get; set; }
        public string CharacterName { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string IssuedBy { get; set; }
        public string IssuedFrom { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string RevokedBy { get; set; }
        public bool Active { get; set; }
        public string AccountName { get; set; }

        /// <summary>The text the player sees (login refusal, kick, chat). Times are UTC.</summary>
        public string PlayerMessage()
        {
            string until = ExpiresAt.HasValue ? $" until {DateTime.SpecifyKind(ExpiresAt.Value, DateTimeKind.Utc):yyyy-MM-dd HH:mm} UTC" : "";
            string reason = string.IsNullOrWhiteSpace(Reason) ? "" : $" Reason: {Reason.Trim()}";
            return Kind == SanctionKinds.Mute
                ? $"You are muted{until}.{reason}"
                : $"This account is banned{until}.{reason}";
        }
    }

    public class ActiveSanctions
    {
        public Sanction Ban { get; set; }
        public Sanction Mute { get; set; }
    }

    public static class SanctionKinds
    {
        public const string Ban = "ban";
        public const string Mute = "mute";
        public const string Warn = "warn";
        public const string Note = "note";

        public static readonly IReadOnlyList<string> All = new[] { Ban, Mute, Warn, Note };

        public static bool HasDuration(string kind) => kind == Ban || kind == Mute;
    }

    public class ZoneCommand
    {
        public long CommandID { get; set; }
        public int? MapInstanceID { get; set; }
        public string CharacterName { get; set; }
        public string Command { get; set; }
        public string Args { get; set; }
        public DateTime CreateDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public int? DeliveredToMapInstanceID { get; set; }
        public int DeliveryAttempt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Result { get; set; }
        public string ResultDetail { get; set; }
    }

    public static class ZoneCommandNames
    {
        public const string Kick = "kick";
        public const string Warn = "warn";
        public const string Mute = "mute";
        public const string Unmute = "unmute";
        public const string Announce = "announce";
        public const string Unstuck = "unstuck";
        public const string GiveItem = "give_item";
        public const string GiveJuri = "give_juri";
    }

    public class ZoneCommandResult
    {
        public long CommandId { get; set; }
        public int Attempt { get; set; }
        public string Result { get; set; }
        public string Detail { get; set; }
    }

    /// <summary>
    /// One SSEVENT object from a zone server. Every event carries id, event, ts and character; the rest of its fields
    /// depend on the event and are stored as they arrive.
    /// </summary>
    public class GameEventIn
    {
        public Guid EventGUID { get; set; }
        public DateTime EventTime { get; set; }
        public string Event { get; set; }
        public string CharacterName { get; set; }
        public string MapName { get; set; }
        public string Fields { get; set; }

        /// <summary>Reads one SSEVENT object; null when it lacks a usable id, event or ts.</summary>
        public static GameEventIn FromJson(JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Object) return null;
            if (!element.TryGetProperty("id", out JsonElement id) || !Guid.TryParse(id.GetString(), out Guid eventGuid)) return null;
            if (!element.TryGetProperty("event", out JsonElement name) || string.IsNullOrWhiteSpace(name.GetString())) return null;
            if (!element.TryGetProperty("ts", out JsonElement ts) || !DateTime.TryParse(ts.GetString(), null,
                    System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out DateTime eventTime)) return null;

            return new GameEventIn
            {
                EventGUID = eventGuid,
                EventTime = DateTime.SpecifyKind(eventTime, DateTimeKind.Utc),
                Event = Truncate(name.GetString(), 32),
                CharacterName = Truncate(element.TryGetProperty("character", out JsonElement character) && character.ValueKind == JsonValueKind.String ? character.GetString() : null, 50),
                MapName = Truncate(element.TryGetProperty("map", out JsonElement map) && map.ValueKind == JsonValueKind.String ? map.GetString() : null, 50),
                Fields = element.GetRawText()
            };
        }

        private static string Truncate(string value, int maxLength)
        {
            return string.IsNullOrEmpty(value) ? null : value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }

    public class GameEventRow
    {
        public long GameEventID { get; set; }
        public DateTime EventTime { get; set; }
        public int? MapInstanceID { get; set; }
        public string MapName { get; set; }
        public string CharacterName { get; set; }
        public string Event { get; set; }
        public string Fields { get; set; }
    }

    public class ChatLogEntry
    {
        public long ChatLogID { get; set; }
        public Guid CustomerGUID { get; set; }
        public DateTime CreateDate { get; set; }
        public string Channel { get; set; }
        public string SenderName { get; set; }
        public Guid? SenderUserGUID { get; set; }
        public string TargetName { get; set; }
        public string Message { get; set; }
        public bool Dropped { get; set; }
    }

    public static class ConsoleSettingKeys
    {
        public const string Motd = "motd";
        public const string LoginsOpen = "logins_open";
        public const string RegionStatusPrefix = "region_status:";
        public const string Maintenance = "maintenance";
        public const string RateEvent = "rate_event";
        public const string PlayerCap = "player_cap";
        public const string AlertsCursorPrefix = "alerts_cursor:";
    }

    public class ConsoleSetting
    {
        public string SettingKey { get; set; }
        public string SettingValue { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
    }
}
