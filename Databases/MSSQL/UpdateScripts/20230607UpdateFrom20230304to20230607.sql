USE OpenWorldServer
GO

UPDATE OWSVersion SET OWSDBVersion='20230607'
GO

SELECT OWSDBVersion FROM OWSVersion
GO

ALTER PROCEDURE [dbo].[AddNewCustomer]
(
	@CustomerName varchar(50),
	@Email nvarchar(256),
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
    WHERE C.CustomerGUID = @CustomerGUID

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
