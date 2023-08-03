USE [OpenWorldServer]
GO
/****** Object:  StoredProcedure [dbo].[AddCharacter]    Script Date: 7/30/2023 1:16:43 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- EXEC [AddCharacter] '452faa8c-0fac-4784-9d56-ada346ebec03',

ALTER PROCEDURE [dbo].[AddCharacter]
(
	@CustomerGUID uniqueidentifier,
	@UserSessionGUID uniqueidentifier,
	@CharacterName nvarchar(50),
	@ClassName varchar(50),
	@ErrorMessage varchar(50) OUTPUT
)
AS
BEGIN

	DECLARE @OutputTable TABLE( 
		ErrorMessage varchar(100),
		CharacterName varchar(50),
		ClassName varchar(50),
		StartingMapName varchar(50),
		X float,
		Y float,
		Z float,
		RX float,
		RY float,
		RZ float,
		TeamNumber int,
		Gender int
	); 

	DECLARE @SupportUnicode bit
	SELECT @SupportUnicode=SupportUnicode FROM Customers C WHERE C.CustomerGUID=@CustomerGUID

	--Remove multiple spaces
	SET @CharacterName = LTRIM(RTRIM(@CharacterName))
	SELECT @CharacterName = replace(replace(replace(@CharacterName,' ','<>'),'><',''),'<>',' ')

	DECLARE @InvalidCharacters int
	SET @InvalidCharacters = (SELECT PatIndex('%[^a-z0-9 ]%', @CharacterName))

	IF (@InvalidCharacters > 0 AND @SupportUnicode=0)
	BEGIN
		SET @ErrorMessage = 'Character Name can only contain letters, numbers, and spaces'

		RETURN
	END

	DECLARE @UserGUID uniqueidentifier

	SELECT @UserGUID=US.UserGUID FROM UserSessions US WHERE US.CustomerGUID=@CustomerGUID AND US.UserSessionGUID=@UserSessionGUID

	IF (@UserGUID IS NOT NULL)
	BEGIN

		DECLARE @ClassID int

		SELECT @ClassID=ClassID FROM Class WHERE CustomerGUID=@CustomerGUID AND ClassName=@ClassName

		IF (@ClassID > 0)
		BEGIN

			DECLARE @CountOfCharNamesFound int = 0
			SELECT @CountOfCharNamesFound = ISNULL(COUNT(*),0) FROM Characters C WHERE C.CustomerGUID=@CustomerGUID AND C.CharName=@CharacterName

			IF (@CountOfCharNamesFound < 1)
			BEGIN

				DECLARE @CharacterID int

				INSERT INTO Characters (CustomerGUID, ClassID, UserGUID, CharName, MapName, X, Y, Z, ServerIP, LastActivity, RX, RY, RZ, TeamNumber, Gender, Description)
				OUTPUT '', @CharacterName, @ClassName, INSERTED.MapName, INSERTED.X, INSERTED.Y, INSERTED.Z,  INSERTED.RX, INSERTED.RY, INSERTED.RZ,
					 INSERTED.TeamNumber, INSERTED.Gender
					INTO @OutputTable
				SELECT @CustomerGUID, @ClassID, @UserGUID, @CharacterName, StartingMapName, X, Y, Z, '', GETDATE(), RX, RY, RZ, TeamNumber, Gender, Description 
				FROM Class 
				WHERE ClassID=@ClassID
				AND CustomerGUID=@CustomerGUID

				SET @CharacterID = SCOPE_IDENTITY()

				DECLARE @CountOfClassInventory int

				
				BEGIN
					INSERT INTO CharInventory (CustomerGUID, CharacterID, InventoryName, InventorySize)
					VALUES (@CustomerGUID, @CharacterID, 'Bag', 16)
				END
				

				SET @ErrorMessage = ''

			END
			ELSE
			BEGIN
				INSERT INTO @OutputTable
				VALUES ('Invalid Character Name', '','','',0,0,0,0,0,0,0,0)
				SET @ErrorMessage = 'Invalid Character Name'
			END	
		END	
		ELSE
		BEGIN
			INSERT INTO @OutputTable
			VALUES ('Invalid Class Name','','','',0,0,0,0,0,0,0,0)
			SET @ErrorMessage = 'Invalid Class Name'
		END
	END
	ELSE
	BEGIN
		INSERT INTO @OutputTable
		VALUES ('Invalid User Session','','','',0,0,0,0,0,0,0,0)
		SET @ErrorMessage = 'Invalid User Session'
	END

	SELECT TOP 1 * FROM @OutputTable

END