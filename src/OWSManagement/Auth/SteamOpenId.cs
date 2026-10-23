using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OWSManagement.Auth
{
    /// <summary>
    /// Steam sign-in (OpenID 2.0). Steam sends the browser back with a signed assertion; check_authentication asks Steam
    /// whether that assertion is genuine, so no Web API key or OpenID library is needed.
    /// </summary>
    public static class SteamOpenId
    {
        public const string Endpoint = "https://steamcommunity.com/openid/login";
        private const string Namespace = "http://specs.openid.net/auth/2.0";
        private const string IdentifierSelect = "http://specs.openid.net/auth/2.0/identifier_select";
        private static readonly Regex ClaimedIdPattern = new(@"^https://steamcommunity\.com/openid/id/(\d{17})$", RegexOptions.Compiled);
        private static readonly TimeSpan MaxNonceAge = TimeSpan.FromMinutes(5);

        public static string BuildLoginUrl(string returnTo, string realm)
        {
            var query = new Dictionary<string, string>
            {
                ["openid.ns"] = Namespace,
                ["openid.mode"] = "checkid_setup",
                ["openid.return_to"] = returnTo,
                ["openid.realm"] = realm,
                ["openid.identity"] = IdentifierSelect,
                ["openid.claimed_id"] = IdentifierSelect
            };
            return Endpoint + "?" + string.Join("&", query.Select(pair => Uri.EscapeDataString(pair.Key) + "=" + Uri.EscapeDataString(pair.Value)));
        }

        /// <summary>
        /// Checks everything in the assertion that does not need Steam: mode, endpoint, return URL, nonce age and the
        /// claimed ID's form. Returns the 17-digit Steam ID, or null with the reason.
        /// </summary>
        public static string ReadSteamId(IReadOnlyDictionary<string, string> response, string expectedReturnTo, DateTime utcNow, out string error)
        {
            error = null;
            string Get(string key) => response.TryGetValue(key, out string value) ? value : null;

            if (Get("openid.mode") != "id_res") { error = "not_signed_in"; return null; }
            if (Get("openid.ns") != Namespace) { error = "bad_namespace"; return null; }
            if (Get("openid.op_endpoint") != Endpoint) { error = "bad_endpoint"; return null; }
            if (Get("openid.return_to") != expectedReturnTo) { error = "bad_return_to"; return null; }

            string claimedId = Get("openid.claimed_id");
            Match match = claimedId == null ? Match.Empty : ClaimedIdPattern.Match(claimedId);
            if (!match.Success || Get("openid.identity") != claimedId) { error = "bad_claimed_id"; return null; }

            // response_nonce starts with its UTC creation time, e.g. 2026-10-22T10:00:00Z...
            string nonce = Get("openid.response_nonce");
            if (nonce == null || nonce.Length < 20 || !DateTime.TryParseExact(nonce.Substring(0, 20), "yyyy-MM-dd'T'HH:mm:ss'Z'",
                    CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime issued)
                || (utcNow - issued).Duration() > MaxNonceAge)
            {
                error = "stale_nonce";
                return null;
            }

            string[] signed = (Get("openid.signed") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (string field in new[] { "op_endpoint", "claimed_id", "identity", "return_to", "response_nonce" })
            {
                if (!signed.Contains(field)) { error = "unsigned_" + field; return null; }
            }

            return match.Groups[1].Value;
        }

        /// <summary>Asks Steam whether it issued this assertion.</summary>
        public static async Task<bool> VerifyWithSteam(HttpClient http, IReadOnlyDictionary<string, string> response)
        {
            var form = response
                .Where(pair => pair.Key.StartsWith("openid.", StringComparison.Ordinal))
                .ToDictionary(pair => pair.Key, pair => pair.Value);
            form["openid.mode"] = "check_authentication";

            using HttpResponseMessage reply = await http.PostAsync(Endpoint, new FormUrlEncodedContent(form));
            if (!reply.IsSuccessStatusCode) return false;

            string body = await reply.Content.ReadAsStringAsync();
            return body.Split('\n').Any(line => line.Trim() == "is_valid:true");
        }
    }
}
