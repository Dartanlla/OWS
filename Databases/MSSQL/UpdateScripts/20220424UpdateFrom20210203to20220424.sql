USE OpenWorldServer
GO

UPDATE OWSVersion SET OWSDBVersion='20220424'
GO

SELECT OWSDBVersion FROM OWSVersion
GO

ALTER TABLE WorldServers
ADD ZoneServerGUID uniqueidentifier NULL
GO



