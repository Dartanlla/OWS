using System;
using System.Text.Json;

namespace OWSContent.DTOs
{
    public class AddOrUpdateContentFeatureDTO
    {
        public int ContentFeatureId { get; set; }
        public Guid CustomerGuid { get; set; }
        public JsonElement ContentFeatureJson { get; set; }
        public Guid CreatedBy { get; set; }
        public int? ContentPackageId { get; set; }
    }
}
