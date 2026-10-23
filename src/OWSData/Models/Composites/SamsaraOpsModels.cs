using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace OWSData.Models.Composites
{
    /// <summary>A maintenance window the console scheduled (ConsoleSettings "maintenance"). Times are UTC.</summary>
    public class MaintenanceWindow
    {
        public DateTime StartsAt { get; set; }
        public string Message { get; set; }

        public bool Started(DateTime utcNow) => utcNow >= StartsAt;

        public string PlayerMessage() => string.IsNullOrWhiteSpace(Message)
            ? "The servers are closed for maintenance. Try again later."
            : Message.Trim();
    }

    /// <summary>An EXP and drop rate event (ConsoleSettings "rate_event"). Multipliers are 1 to 5. Times are UTC.</summary>
    public class RateEvent
    {
        public const double MinMultiplier = 1;
        public const double MaxMultiplier = 5;

        public string Name { get; set; }
        public double Exp { get; set; } = 1;
        public double Drop { get; set; } = 1;
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }

        public bool InForce(DateTime utcNow) => utcNow >= StartsAt && utcNow < EndsAt;
    }

    /// <summary>Reads the structured console settings. A value that does not parse counts as not set.</summary>
    public static class ConsoleSettingValues
    {
        private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

        public static MaintenanceWindow Maintenance(IReadOnlyDictionary<string, string> settings)
        {
            MaintenanceWindow window = Read<MaintenanceWindow>(settings, ConsoleSettingKeys.Maintenance);
            return window == null || window.StartsAt == default ? null : Utc(window);
        }

        public static RateEvent Rates(IReadOnlyDictionary<string, string> settings)
        {
            RateEvent rates = Read<RateEvent>(settings, ConsoleSettingKeys.RateEvent);
            if (rates == null || rates.EndsAt <= rates.StartsAt) return null;
            rates.StartsAt = DateTime.SpecifyKind(rates.StartsAt.ToUniversalTime(), DateTimeKind.Utc);
            rates.EndsAt = DateTime.SpecifyKind(rates.EndsAt.ToUniversalTime(), DateTimeKind.Utc);
            rates.Exp = Math.Clamp(rates.Exp, RateEvent.MinMultiplier, RateEvent.MaxMultiplier);
            rates.Drop = Math.Clamp(rates.Drop, RateEvent.MinMultiplier, RateEvent.MaxMultiplier);
            return rates;
        }

        /// <summary>0 when no cap is set.</summary>
        public static int PlayerCap(IReadOnlyDictionary<string, string> settings)
        {
            return settings.TryGetValue(ConsoleSettingKeys.PlayerCap, out string value)
                && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int cap) && cap > 0 ? cap : 0;
        }

        public static string Write<T>(T value) => JsonSerializer.Serialize(value);

        private static T Read<T>(IReadOnlyDictionary<string, string> settings, string key) where T : class
        {
            if (!settings.TryGetValue(key, out string value) || string.IsNullOrWhiteSpace(value)) return null;
            try
            {
                return JsonSerializer.Deserialize<T>(value, Json);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static MaintenanceWindow Utc(MaintenanceWindow window)
        {
            window.StartsAt = DateTime.SpecifyKind(window.StartsAt.ToUniversalTime(), DateTimeKind.Utc);
            return window;
        }
    }

    public class PlayerReport
    {
        public long ReportID { get; set; }
        public DateTime CreateDate { get; set; }
        public string ReporterName { get; set; }
        public Guid? ReporterUserGUID { get; set; }
        public string TargetName { get; set; }
        public Guid? TargetUserGUID { get; set; }
        public string Reason { get; set; }
        public string MapName { get; set; }
        public int? MapInstanceID { get; set; }
        public string Details { get; set; }
        public string ChatExcerpt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string ResolvedBy { get; set; }
        public string ResolutionNote { get; set; }
        /// <summary>Reports against the target in the last 30 days, this one included.</summary>
        public int TargetReports30d { get; set; }
        /// <summary>Reports the reporter filed in the last 30 days, this one included.</summary>
        public int ReporterReports30d { get; set; }
    }

    public class NewPlayerReport
    {
        public string ReporterName { get; set; }
        public string TargetName { get; set; }
        public string Reason { get; set; }
        public string MapName { get; set; }
        public int? MapInstanceID { get; set; }
        public string Details { get; set; }
    }

    public class LivePosition
    {
        public string CharacterName { get; set; }
        public int MapInstanceID { get; set; }
        public string MapName { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int Level { get; set; }
    }

    public class LivePositionIn
    {
        public string CharacterName { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }
}
