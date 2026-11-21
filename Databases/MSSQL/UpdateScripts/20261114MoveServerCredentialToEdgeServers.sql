SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('dbo.EdgeServers', 'ServerCredentialHash') IS NULL
    ALTER TABLE dbo.EdgeServers ADD ServerCredentialHash VARCHAR(64) NULL;

IF COL_LENGTH('dbo.MapInstances', 'ServerCredentialHash') IS NOT NULL
    ALTER TABLE dbo.MapInstances DROP COLUMN ServerCredentialHash;

COMMIT TRANSACTION;
