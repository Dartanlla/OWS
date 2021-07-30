USE master
GO

IF DB_ID('OpenWorldServer') IS NULL
BEGIN
    RESTORE DATABASE OpenWorldServer
    FROM DISK =  N'/var/opt/mssql/backups/20210203OpenWorldServer.bak' WITH REPLACE,
        MOVE N'd20' TO N'/var/opt/mssql/data/OpenWorldServer.mdf',
        MOVE N'd20_log' TO  N'/var/opt/mssql/data/OpenWorldServer_log.ldf',
    NOUNLOAD,
    STATS = 5;
END
GO