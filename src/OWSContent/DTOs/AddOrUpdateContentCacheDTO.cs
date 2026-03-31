using System.Text.Json;

namespace OWSContent.DTOs
{
    public class AddOrUpdateContentCacheDTO
    {
        public string ZoneName { get; set; }
        public JsonElement ContentJson { get; set; }
    }
}
