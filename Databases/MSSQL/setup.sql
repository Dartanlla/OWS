USE master
GO

RESTORE DATABASE OpenWorldServer
FROM DISK =  N'/user/config/20210203OpenWorldServer.bak'
WITH FILE = 1,
     MOVE N'd20'
     TO  N'/var/opt/mssql/data/OpenWorldServer.mdf',
     MOVE N'd20_log'
     TO  N'/var/opt/mssql/data/OpenWorldServer_log.ldf',
     NOUNLOAD,
     STATS = 5;
GO