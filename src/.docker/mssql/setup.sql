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