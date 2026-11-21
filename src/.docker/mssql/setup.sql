USE master
GO

IF DB_ID('OpenWorldServer') IS NULL
BEGIN
    RESTORE DATABASE OpenWorldServer
    FROM DISK =  N'/user/config/backups/OpenWorldServerEmpty.bak' WITH REPLACE,
        MOVE N'OpenWorldServer' TO N'/var/opt/mssql/data/OpenWorldServer.mdf',
        MOVE N'OpenWorldServer_log' TO  N'/var/opt/mssql/data/OpenWorldServer_log.ldf',
    NOUNLOAD,
    STATS = 5;
END
GO

USE OpenWorldServer
GO

IF OBJECT_ID('dbo.EdgeServers', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.EdgeServers
    (
        EdgeServerID INT IDENTITY(1,1) CONSTRAINT PK_EdgeServers PRIMARY KEY,
        CustomerGUID UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Customers (CustomerGUID),
        Host VARCHAR(253) NOT NULL,
        Port INT NOT NULL CHECK (Port BETWEEN 1 AND 65535),
        IsActive BIT NOT NULL DEFAULT 0,
        ServerCredentialHash VARCHAR(64) NULL,
        UpdatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_EdgeServers_Endpoint UNIQUE (CustomerGUID, Host, Port)
    );
END;
GO
