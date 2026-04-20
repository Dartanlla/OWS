using System;

namespace OWSData.Models.Content
{
    public class ContentFeature
    {
        public int ContentFeatureId { get; set; }
        public Guid CustomerGuid { get; set; }
        public string ContentFeatureJson { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
        public DateTime? ArchivedAt { get; set; }
        public int? ContentPackageId { get; set; }
    }
}
