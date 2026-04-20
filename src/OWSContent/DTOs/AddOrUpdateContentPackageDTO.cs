using System;
using System.Text.Json;

namespace OWSContent.DTOs
{
    public class AddOrUpdateContentPackageDTO
    {
        public int ContentPackageId { get; set; }
        public Guid CustomerGuid { get; set; }
        public JsonElement AggregatedContentJson { get; set; }
        public int VersionNumber { get; set; }
        public DateTime? DeployAt { get; set; }
    }
}
