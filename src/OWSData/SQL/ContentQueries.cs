namespace OWSData.SQL
{
    public static class ContentQueries
    {
        public static readonly string GetContentFeature = @"
            SELECT ContentFeatureId, CustomerGuid, ContentFeatureJson, CreatedBy, CreatedAt, ModifiedAt, ArchivedAt, ContentPackageId
            FROM ContentFeatures
            WHERE CustomerGuid = @CustomerGuid::UUID
            AND ContentFeatureId = @ContentFeatureId";

        public static readonly string InsertContentFeature = @"
            INSERT INTO ContentFeatures (CustomerGuid, ContentFeatureJson, CreatedBy, CreatedAt, ModifiedAt, ArchivedAt, ContentPackageId)
            VALUES (@CustomerGuid::UUID, @ContentFeatureJson, @CreatedBy::UUID, @CreatedAt, @ModifiedAt, @ArchivedAt, @ContentPackageId)
            RETURNING ContentFeatureId";

        public static readonly string UpdateContentFeature = @"
            UPDATE ContentFeatures
            SET ContentFeatureJson = @ContentFeatureJson,
                ModifiedAt = @ModifiedAt,
                ArchivedAt = @ArchivedAt,
                ContentPackageId = @ContentPackageId
            WHERE CustomerGuid = @CustomerGuid::UUID
            AND ContentFeatureId = @ContentFeatureId";

        public static readonly string GetContentPackage = @"
            SELECT ContentPackageId, CustomerGuid, AggregatedContentJson, VersionNumber, CreatedAt, ModifiedAt, ArchivedAt, DeployAt
            FROM ContentPackages
            WHERE CustomerGuid = @CustomerGuid::UUID
            AND ContentPackageId = @ContentPackageId";

        public static readonly string InsertContentPackage = @"
            INSERT INTO ContentPackages (CustomerGuid, AggregatedContentJson, VersionNumber, CreatedAt, ModifiedAt, ArchivedAt, DeployAt)
            VALUES (@CustomerGuid::UUID, @AggregatedContentJson, @VersionNumber, @CreatedAt, @ModifiedAt, @ArchivedAt, @DeployAt)
            RETURNING ContentPackageId";

        public static readonly string UpdateContentPackage = @"
            UPDATE ContentPackages
            SET AggregatedContentJson = @AggregatedContentJson,
                VersionNumber = @VersionNumber,
                ModifiedAt = @ModifiedAt,
                ArchivedAt = @ArchivedAt,
                DeployAt = @DeployAt
            WHERE CustomerGuid = @CustomerGuid::UUID
            AND ContentPackageId = @ContentPackageId";
    }
}
