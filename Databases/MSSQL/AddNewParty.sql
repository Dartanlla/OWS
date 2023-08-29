USE [OpenWorldServer]
GO
/****** Object:  StoredProcedure [dbo].[AddCharacter]    Script Date: 8/7/2023 5:30:23 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- EXEC [AddCharacter] '452faa8c-0fac-4784-9d56-ada346ebec03',
ALTER PROCEDURE [dbo].[AddNewParty]
(
	@CustomerGUID uniqueidentifier
)
AS
BEGIN
	DECLARE @PartyGUID uniqueidentifier

	SET @PartyGUID = NEWID();

	INSERT INTO dbo.Party (CustomerGUID, PartyGUID, bRaidingParty)
	VALUES (@CustomerGUID, @PartyGUID, 0);
			
	SELECT TOP 1 * FROM dbo.Party order by PartyID DESC;

END