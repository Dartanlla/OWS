CREATE OR ALTER PROCEDURE dbo.AddCharacter
    @CustomerGUID uniqueidentifier,
    @UserSessionGUID uniqueidentifier,
    @CharacterName nvarchar(50),
    @ClassName varchar(50),
    @ErrorMessage varchar(50) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @ErrorMessage = NULL;

    DECLARE @UserGUID uniqueidentifier, @ClassID int, @CharacterID int;
    SELECT @UserGUID = UserGUID FROM UserSessions
    WHERE CustomerGUID = @CustomerGUID AND UserSessionGUID = @UserSessionGUID;
    SELECT @ClassID = ClassID FROM Class
    WHERE CustomerGUID = @CustomerGUID AND ClassName = @ClassName;
    SET @CharacterName = LTRIM(RTRIM(@CharacterName));
    WHILE CHARINDEX('  ', @CharacterName) > 0
        SET @CharacterName = REPLACE(@CharacterName, '  ', ' ');

    IF EXISTS (SELECT 1 FROM Customers WHERE CustomerGUID = @CustomerGUID AND SupportUnicode = 0)
       AND @CharacterName COLLATE Latin1_General_BIN2 LIKE '%[^a-zA-Z0-9 ]%'
        SET @ErrorMessage = 'Character Name can only contain letters, numbers, and spaces';
    ELSE IF @UserGUID IS NULL
        SET @ErrorMessage = 'Invalid User Session';
    ELSE IF @ClassID IS NULL
        SET @ErrorMessage = 'Invalid Class Name';
    ELSE IF EXISTS (SELECT 1 FROM Characters WHERE CustomerGUID = @CustomerGUID AND CharName = @CharacterName)
        SET @ErrorMessage = 'Invalid Character Name';

    IF @ErrorMessage IS NOT NULL
    BEGIN
        SELECT @ErrorMessage AS ErrorMessage;
        RETURN;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;
        INSERT INTO Characters (CustomerGUID, ClassID, UserGUID, CharName, MapName, X, Y, Z, ServerIP, LastActivity, RX, RY, RZ)
        SELECT @CustomerGUID, ClassID, @UserGUID, @CharacterName, StartingMapName, X, Y, Z, '', GETDATE(), RX, RY, RZ
        FROM Class WHERE CustomerGUID = @CustomerGUID AND ClassID = @ClassID;
        SET @CharacterID = SCOPE_IDENTITY();

        IF EXISTS (SELECT 1 FROM ClassInventory WHERE CustomerGUID = @CustomerGUID AND ClassID = @ClassID)
            INSERT INTO CharInventory (CustomerGUID, CharacterID, InventoryName, InventorySize, InventoryWidth, InventoryHeight)
            SELECT @CustomerGUID, @CharacterID, InventoryName, InventorySize, InventoryWidth, InventoryHeight
            FROM ClassInventory WHERE CustomerGUID = @CustomerGUID AND ClassID = @ClassID;
        ELSE
            INSERT INTO CharInventory (CustomerGUID, CharacterID, InventoryName, InventorySize, InventoryWidth, InventoryHeight)
            VALUES (@CustomerGUID, @CharacterID, 'Bag', 16, 4, 4);
        COMMIT TRANSACTION;

        SELECT @ErrorMessage AS ErrorMessage, CharName AS CharacterName, @ClassName AS ClassName,
               MapName AS StartingMapName, X, Y, Z, RX, RY, RZ
        FROM Characters WHERE CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

ALTER PROCEDURE [dbo].[AddNewCustomer]
(
	@CustomerName varchar(50),
	@FirstName varchar(50),
	@LastName varchar(50),
	@Email nvarchar(256),
	@Password nvarchar(256),
	@CustomerGUID uniqueidentifier = NULL
)
AS
BEGIN

BEGIN TRY
BEGIN TRANSACTION

    IF @CustomerGUID IS NULL
BEGIN
	    SET @CustomerGUID = NEWID()
END

   IF EXISTS(SELECT 1 FROM Customers WHERE CustomerGUID = @CustomerGUID)
BEGIN
      RAISERROR('Customer with specified GUID already exists.', 16, 10);
END

INSERT INTO Customers (CustomerGUID, CustomerName, CustomerEmail, CustomerPhone, CustomerNotes, EnableDebugLogging)
VALUES (@CustomerGUID, @CustomerName, @Email, '', '', 1)

    INSERT INTO WorldSettings (CustomerGUID, StartTime)
SELECT CustomerGUID, convert(float, datediff(dd, '1970-01-01', GETUTCDATE()))*24*3600 + datediff(ss, dateadd(dd, datediff(dd, 0, GETUTCDATE()), 0), GETUTCDATE())
FROM Customers C
WHERE C.CustomerGUID=@CustomerGUID

DECLARE @UserGUID uniqueidentifier
	EXEC [AddUser] @CustomerGUID, @FirstName, @LastName, @Email, @Password, 'Developer', @UserGUID OUTPUT

	INSERT INTO Maps (CustomerGUID, MapName, ZoneName, MapData, Width, Height)
	VALUES (@CustomerGUID, 'ThirdPersonExampleMap','ThirdPersonExampleMap', NULL, 1, 1)
	INSERT INTO Maps (CustomerGUID, MapName, ZoneName, MapData, Width, Height)
	VALUES (@CustomerGUID, 'Map2','Map2', NULL, 1, 1)
	INSERT INTO Maps (CustomerGUID, MapName, ZoneName, MapData, Width, Height)
	VALUES (@CustomerGUID, 'DungeonMap','DungeonMap', NULL, 1, 1)
	INSERT INTO Maps (CustomerGUID, MapName, ZoneName, MapData, Width, Height)
	VALUES (@CustomerGUID, 'FourZoneMap','Zone1', NULL, 1, 1)
	INSERT INTO Maps (CustomerGUID, MapName, ZoneName, MapData, Width, Height)
	VALUES (@CustomerGUID, 'FourZoneMap','Zone2', NULL, 1, 1)
	

	DECLARE @ClassID int

	INSERT INTO Class (CustomerGUID, ClassName, StartingMapName, X, Y, Z, Perception, Acrobatics, Climb, Stealth, RX, RY, RZ, Spirit, Magic, TeamNumber, Thirst, Hunger, Gold, Score, CharacterLevel, Gender, XP, HitDie, Wounds, Size, weight, MaxHealth, Health, HealthRegenRate, MaxMana, Mana, ManaRegenRate, MaxEnergy, Energy, EnergyRegenRate, MaxFatigue, Fatigue, FatigueRegenRate, MaxStamina, Stamina, StaminaRegenRate, MaxEndurance, Endurance, EnduranceRegenRate, Strength, Dexterity, Constitution, Intellect, Wisdom, Charisma, Agility, Fortitude, Reflex, Willpower, BaseAttack, BaseAttackBonus, AttackPower, AttackSpeed, CritChance, CritMultiplier, Haste, SpellPower, SpellPenetration, Defense, Dodge, Parry, Avoidance, Versatility, Multishot, Initiative, NaturalArmor, PhysicalArmor, BonusArmor, ForceArmor, MagicArmor, Resistance, ReloadSpeed, Range, Speed, Silver, Copper, FreeCurrency, PremiumCurrency, Fame, Alignment, Description)
	VALUES (@CustomerGUID,'MaleWarrior','ThirdPersonExampleMap',0,0,250,0,0,0,0,0,0,0,0,0,1,0,0,0,0,1,1,0,10,0,1,0,100,50,1,100,0,1,100,0,5,100,0,1,0,0,0,0,0,0,10,10,10,10,10,10,0,1,1,1,5,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,'')

SELECT @ClassID = SCOPE_IDENTITY()

DECLARE @CharacterName varchar(50)
	SET @CharacterName='Test'

	DECLARE @CharacterID int

	INSERT INTO Characters (CustomerGUID, ClassID, UserGUID, CharName, MapName, X, Y, Z, ServerIP, LastActivity, RX, RY, RZ)
SELECT @CustomerGUID, @ClassID, @UserGUID, @CharacterName, StartingMapName, X, Y, Z, '', GETDATE(), RX, RY, RZ
            FROM Class
WHERE ClassID=@ClassID

SELECT @CharacterID = SCOPE_IDENTITY()

    INSERT INTO CharInventory (CustomerGUID, CharacterID, InventoryName, InventorySize)
VALUES (@CustomerGUID,@CharacterID,'Bag',16)

    COMMIT TRAN -- Transaction Success!
END TRY
BEGIN CATCH
IF @@TRANCOUNT > 0
        ROLLBACK TRAN --RollBack in case of Error

    DECLARE @ErrorMessage NVARCHAR(4000);
    DECLARE @ErrorSeverity INT;
    DECLARE @ErrorState INT;


SELECT
        @ErrorMessage=ERROR_MESSAGE(),
        @ErrorSeverity=ERROR_SEVERITY(),
        @ErrorState=ERROR_STATE();

RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH

END
GO

DECLARE @sql nvarchar(max) = N'';
SELECT @sql = @sql + N'ALTER TABLE dbo.Characters DROP CONSTRAINT ' + QUOTENAME(dc.name) + N';'
FROM sys.default_constraints dc
JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Characters')
  AND c.name IN ('Email', 'IsAdmin', 'IsModerator', 'Perception', 'Acrobatics', 'Climb', 'Stealth', 'Spirit', 'Magic', 'TeamNumber', 'Thirst', 'Hunger', 'Gold', 'Score', 'CharacterLevel', 'Gender', 'Xp', 'HitDie', 'Wounds', 'Size', 'Weight', 'MaxHealth', 'Health', 'HealthRegenRate', 'MaxMana', 'Mana', 'ManaRegenRate', 'MaxEnergy', 'Energy', 'EnergyRegenRate', 'MaxFatigue', 'Fatigue', 'FatigueRegenRate', 'MaxStamina', 'Stamina', 'StaminaRegenRate', 'MaxEndurance', 'Endurance', 'EnduranceRegenRate', 'Strength', 'Dexterity', 'Constitution', 'Intellect', 'Wisdom', 'Charisma', 'Agility', 'Fortitude', 'Reflex', 'Willpower', 'BaseAttack', 'BaseAttackBonus', 'AttackPower', 'AttackSpeed', 'CritChance', 'CritMultiplier', 'Haste', 'SpellPower', 'SpellPenetration', 'Defense', 'Dodge', 'Parry', 'Avoidance', 'Versatility', 'Multishot', 'Initiative', 'NaturalArmor', 'PhysicalArmor', 'BonusArmor', 'ForceArmor', 'MagicArmor', 'Resistance', 'ReloadSpeed', 'Range', 'Speed', 'Silver', 'Copper', 'FreeCurrency', 'PremiumCurrency', 'Fame', 'Alignment', 'Description');
EXEC sp_executesql @sql;
IF COL_LENGTH('dbo.Characters', 'Email') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Email];
IF COL_LENGTH('dbo.Characters', 'IsAdmin') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [IsAdmin];
IF COL_LENGTH('dbo.Characters', 'IsModerator') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [IsModerator];
IF COL_LENGTH('dbo.Characters', 'Perception') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Perception];
IF COL_LENGTH('dbo.Characters', 'Acrobatics') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Acrobatics];
IF COL_LENGTH('dbo.Characters', 'Climb') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Climb];
IF COL_LENGTH('dbo.Characters', 'Stealth') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Stealth];
IF COL_LENGTH('dbo.Characters', 'Spirit') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Spirit];
IF COL_LENGTH('dbo.Characters', 'Magic') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Magic];
IF COL_LENGTH('dbo.Characters', 'TeamNumber') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [TeamNumber];
IF COL_LENGTH('dbo.Characters', 'Thirst') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Thirst];
IF COL_LENGTH('dbo.Characters', 'Hunger') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Hunger];
IF COL_LENGTH('dbo.Characters', 'Gold') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Gold];
IF COL_LENGTH('dbo.Characters', 'Score') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Score];
IF COL_LENGTH('dbo.Characters', 'CharacterLevel') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [CharacterLevel];
IF COL_LENGTH('dbo.Characters', 'Gender') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Gender];
IF COL_LENGTH('dbo.Characters', 'Xp') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Xp];
IF COL_LENGTH('dbo.Characters', 'HitDie') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [HitDie];
IF COL_LENGTH('dbo.Characters', 'Wounds') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Wounds];
IF COL_LENGTH('dbo.Characters', 'Size') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Size];
IF COL_LENGTH('dbo.Characters', 'Weight') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Weight];
IF COL_LENGTH('dbo.Characters', 'MaxHealth') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [MaxHealth];
IF COL_LENGTH('dbo.Characters', 'Health') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Health];
IF COL_LENGTH('dbo.Characters', 'HealthRegenRate') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [HealthRegenRate];
IF COL_LENGTH('dbo.Characters', 'MaxMana') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [MaxMana];
IF COL_LENGTH('dbo.Characters', 'Mana') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Mana];
IF COL_LENGTH('dbo.Characters', 'ManaRegenRate') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [ManaRegenRate];
IF COL_LENGTH('dbo.Characters', 'MaxEnergy') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [MaxEnergy];
IF COL_LENGTH('dbo.Characters', 'Energy') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Energy];
IF COL_LENGTH('dbo.Characters', 'EnergyRegenRate') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [EnergyRegenRate];
IF COL_LENGTH('dbo.Characters', 'MaxFatigue') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [MaxFatigue];
IF COL_LENGTH('dbo.Characters', 'Fatigue') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Fatigue];
IF COL_LENGTH('dbo.Characters', 'FatigueRegenRate') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [FatigueRegenRate];
IF COL_LENGTH('dbo.Characters', 'MaxStamina') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [MaxStamina];
IF COL_LENGTH('dbo.Characters', 'Stamina') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Stamina];
IF COL_LENGTH('dbo.Characters', 'StaminaRegenRate') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [StaminaRegenRate];
IF COL_LENGTH('dbo.Characters', 'MaxEndurance') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [MaxEndurance];
IF COL_LENGTH('dbo.Characters', 'Endurance') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Endurance];
IF COL_LENGTH('dbo.Characters', 'EnduranceRegenRate') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [EnduranceRegenRate];
IF COL_LENGTH('dbo.Characters', 'Strength') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Strength];
IF COL_LENGTH('dbo.Characters', 'Dexterity') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Dexterity];
IF COL_LENGTH('dbo.Characters', 'Constitution') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Constitution];
IF COL_LENGTH('dbo.Characters', 'Intellect') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Intellect];
IF COL_LENGTH('dbo.Characters', 'Wisdom') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Wisdom];
IF COL_LENGTH('dbo.Characters', 'Charisma') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Charisma];
IF COL_LENGTH('dbo.Characters', 'Agility') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Agility];
IF COL_LENGTH('dbo.Characters', 'Fortitude') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Fortitude];
IF COL_LENGTH('dbo.Characters', 'Reflex') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Reflex];
IF COL_LENGTH('dbo.Characters', 'Willpower') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Willpower];
IF COL_LENGTH('dbo.Characters', 'BaseAttack') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [BaseAttack];
IF COL_LENGTH('dbo.Characters', 'BaseAttackBonus') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [BaseAttackBonus];
IF COL_LENGTH('dbo.Characters', 'AttackPower') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [AttackPower];
IF COL_LENGTH('dbo.Characters', 'AttackSpeed') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [AttackSpeed];
IF COL_LENGTH('dbo.Characters', 'CritChance') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [CritChance];
IF COL_LENGTH('dbo.Characters', 'CritMultiplier') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [CritMultiplier];
IF COL_LENGTH('dbo.Characters', 'Haste') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Haste];
IF COL_LENGTH('dbo.Characters', 'SpellPower') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [SpellPower];
IF COL_LENGTH('dbo.Characters', 'SpellPenetration') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [SpellPenetration];
IF COL_LENGTH('dbo.Characters', 'Defense') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Defense];
IF COL_LENGTH('dbo.Characters', 'Dodge') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Dodge];
IF COL_LENGTH('dbo.Characters', 'Parry') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Parry];
IF COL_LENGTH('dbo.Characters', 'Avoidance') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Avoidance];
IF COL_LENGTH('dbo.Characters', 'Versatility') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Versatility];
IF COL_LENGTH('dbo.Characters', 'Multishot') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Multishot];
IF COL_LENGTH('dbo.Characters', 'Initiative') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Initiative];
IF COL_LENGTH('dbo.Characters', 'NaturalArmor') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [NaturalArmor];
IF COL_LENGTH('dbo.Characters', 'PhysicalArmor') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [PhysicalArmor];
IF COL_LENGTH('dbo.Characters', 'BonusArmor') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [BonusArmor];
IF COL_LENGTH('dbo.Characters', 'ForceArmor') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [ForceArmor];
IF COL_LENGTH('dbo.Characters', 'MagicArmor') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [MagicArmor];
IF COL_LENGTH('dbo.Characters', 'Resistance') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Resistance];
IF COL_LENGTH('dbo.Characters', 'ReloadSpeed') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [ReloadSpeed];
IF COL_LENGTH('dbo.Characters', 'Range') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Range];
IF COL_LENGTH('dbo.Characters', 'Speed') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Speed];
IF COL_LENGTH('dbo.Characters', 'Silver') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Silver];
IF COL_LENGTH('dbo.Characters', 'Copper') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Copper];
IF COL_LENGTH('dbo.Characters', 'FreeCurrency') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [FreeCurrency];
IF COL_LENGTH('dbo.Characters', 'PremiumCurrency') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [PremiumCurrency];
IF COL_LENGTH('dbo.Characters', 'Fame') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Fame];
IF COL_LENGTH('dbo.Characters', 'Alignment') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Alignment];
IF COL_LENGTH('dbo.Characters', 'Description') IS NOT NULL ALTER TABLE dbo.Characters DROP COLUMN [Description];
UPDATE OWSVersion SET OWSDBVersion = '20261029';
GO
