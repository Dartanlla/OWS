IF OBJECT_ID('dbo.EdgeServers', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.EdgeServers
    (
        EdgeServerID INT IDENTITY(1,1) CONSTRAINT PK_EdgeServers PRIMARY KEY,
        CustomerGUID UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Customers (CustomerGUID),
        Host VARCHAR(253) NOT NULL,
        Port INT NOT NULL CHECK (Port BETWEEN 1 AND 65535),
        IsActive BIT NOT NULL DEFAULT 0,
        UpdatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_EdgeServers_Endpoint UNIQUE (CustomerGUID, Host, Port)
    );
END;
