using System;

namespace OWSData.Models.Content
{
    public class ContentPackage
    {
        public int ContentPackageId { get; set; }
        public Guid CustomerGuid { get; set; }
        public string AggregatedContentJson { get; set; }
        public int VersionNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
        public DateTime? ArchivedAt { get; set; }
        public DateTime? DeployAt { get; set; }
    }
}
