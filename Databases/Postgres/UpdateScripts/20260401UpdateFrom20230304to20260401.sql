CREATE TABLE ContentFeatures (
    ContentFeatureId  SERIAL PRIMARY KEY,
    CustomerGuid      UUID NOT NULL,
    ContentFeatureJson TEXT NOT NULL,
    CreatedBy         UUID NOT NULL,
    CreatedAt         TIMESTAMP NOT NULL,
    ModifiedAt        TIMESTAMP NOT NULL,
    ArchivedAt        TIMESTAMP NULL,
    ContentPackageId  INT NULL
);

CREATE TABLE ContentPackages (
    ContentPackageId      SERIAL PRIMARY KEY,
    CustomerGuid          UUID NOT NULL,
    AggregatedContentJson TEXT NOT NULL,
    VersionNumber         INT NOT NULL,
    CreatedAt             TIMESTAMP NOT NULL,
    ModifiedAt            TIMESTAMP NOT NULL,
    ArchivedAt            TIMESTAMP NULL,
    DeployAt              TIMESTAMP NULL
);

ALTER TABLE ContentFeatures
    ADD CONSTRAINT fk_contentfeature_package
    FOREIGN KEY (ContentPackageId) REFERENCES ContentPackages(ContentPackageId);