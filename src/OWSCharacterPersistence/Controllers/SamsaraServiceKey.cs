using Microsoft.AspNetCore.Http;
using OWSShared;

namespace OWSCharacterPersistence.Controllers
{
    /// <summary>
    /// The X-Samsara-Service-Key check (<see cref="SamsaraServiceKeyCheck"/>) for a controller's request.
    /// </summary>
    internal static class SamsaraServiceKey
    {
        public static bool IsValid(HttpRequest request)
        {
            request.Headers.TryGetValue(SamsaraServiceKeyCheck.Header, out var provided);
            return SamsaraServiceKeyCheck.IsValid(provided.ToString());
        }
    }
}
