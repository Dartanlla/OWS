DROP DATABASE IF EXISTS openworldserver;

CREATE DATABASE IF NOT EXISTS openworldserver CHARACTER SET utf8mb4 COLLATE 'utf8mb4_unicode_ci';

USE openworldserver;

CREATE TABLE DebugLog
(
    DebugLogID   INT AUTO_INCREMENT,
    DebugDate    TIMESTAMP NULL,
    DebugDesc    TEXT      NULL,
    CustomerGUID CHAR(36)  NULL,
    CONSTRAINT PK_DebugLog
        PRIMARY KEY (DebugLogID)
);

CREATE TABLE Customers
(
    CustomerGUID       CHAR(36)                  NOT NULL,
    CustomerName       VARCHAR(50)               NOT NULL,
    CustomerEmail      VARCHAR(255)              NOT NULL,
    CustomerPhone      VARCHAR(20)               NULL,
    CustomerNotes      TEXT                      NULL,
    EnableDebugLogging BOOLEAN     DEFAULT FALSE NOT NULL,
    EnableAutoLoopBack BOOLEAN     DEFAULT TRUE  NOT NULL,
    DeveloperPaid      BOOLEAN     DEFAULT FALSE NOT NULL,
    PublisherPaidDate  TIMESTAMP,
    StripeCustomerID   VARCHAR(50) DEFAULT ''    NOT NULL,
    FreeTrialStarted   TIMESTAMP,
    SupportUnicode     BOOLEAN     DEFAULT FALSE NOT NULL,
    CreateDate         TIMESTAMP   DEFAULT NOW() NOT NULL,
    NoPortForwarding   BOOLEAN     DEFAULT FALSE NOT NULL,
    CONSTRAINT
        PK_Customers
        PRIMARY KEY (CustomerGUID)
);

CREATE TABLE AbilityTypes
(
    AbilityTypeID   INT AUTO_INCREMENT,
    AbilityTypeName VARCHAR(50) NOT NULL,
    CustomerGUID    CHAR(36)    NOT NULL,
    CONSTRAINT PK_AbilityTypes
        PRIMARY KEY (AbilityTypeID, CustomerGUID)
);

CREATE TABLE Abilities
(
    CustomerGUID             CHAR(36)                NOT NULL,
    AbilityID                INT AUTO_INCREMENT,
    AbilityName              VARCHAR(50)             NOT NULL,
    AbilityTypeID            INT                     NOT NULL,
    TextureToUseForIcon      VARCHAR(200) DEFAULT '' NOT NULL,
    Class                    INT                     NULL,
    Race                     INT                     NULL,
    AbilityCustomJSON        TEXT                    NULL,
    GameplayAbilityClassName VARCHAR(200) DEFAULT '' NOT NULL,
    CONSTRAINT PK_Abilities
        PRIMARY KEY (AbilityID, CustomerGUID),
    CONSTRAINT FK_Abilities_AbilityTypes
        FOREIGN KEY (AbilityTypeID, CustomerGUID) REFERENCES AbilityTypes (AbilityTypeID, CustomerGUID)
);

CREATE TABLE AreaOfInterestTypes
(
    AreaOfInterestTypeID   INT AUTO_INCREMENT,
    AreaOfInterestTypeDesc VARCHAR(50) NOT NULL,
    CONSTRAINT PK_AreaOfInterestTypes
        PRIMARY KEY (AreaOfInterestTypeID)
);

CREATE TABLE AreasOfInterest
(
    CustomerGUID        CHAR(36)    NOT NULL,
    AreasOfInterestGUID CHAR(36)    NOT NULL,
    MapZoneID           INT         NOT NULL,
    AreaOfInterestName  VARCHAR(50) NOT NULL,
    Radius              FLOAT       NOT NULL,
    AreaOfInterestType  INT         NOT NULL,
    X                   FLOAT,
    Y                   FLOAT,
    Z                   FLOAT,
    RX                  FLOAT,
    RY                  FLOAT,
    RZ                  FLOAT,
    CustomData          TEXT,
    CONSTRAINT PK_AreasOfInterest
        PRIMARY KEY (CustomerGUID, AreasOfInterestGUID)
);

CREATE TABLE Users
(
    UserGUID     CHAR(36)                NOT NULL,
    CustomerGUID CHAR(36)                NOT NULL,
    FirstName    VARCHAR(50)             NOT NULL,
    LastName     VARCHAR(50)             NOT NULL,
    Email        VARCHAR(255)            NOT NULL,
    PasswordHash VARCHAR(128)            NOT NULL,
    Salt         VARCHAR(50)             NOT NULL,
    CreateDate   TIMESTAMP DEFAULT NOW() NOT NULL,
    LastAccess   TIMESTAMP DEFAULT NOW() NOT NULL,
    Role         VARCHAR(10)             NOT NULL,
    CONSTRAINT PK_Users
        PRIMARY KEY (UserGUID),
    CONSTRAINT AK_User
        UNIQUE (CustomerGUID, Email, Role)
);

CREATE TABLE Characters
(
    CustomerGUID              CHAR(36)                   NOT NULL,
    CharacterID               INT AUTO_INCREMENT,
    UserGUID                  CHAR(36)                   NULL,
    Email                     VARCHAR(256)               NOT NULL,
    CharName                  VARCHAR(50)                NOT NULL,
    MapName                   VARCHAR(50)                NULL,
    X                         FLOAT                      NOT NULL,
    Y                         FLOAT                      NOT NULL,
    Z                         FLOAT                      NOT NULL,
    Perception                FLOAT                      NOT NULL,
    Acrobatics                FLOAT                      NOT NULL,
    Climb                     FLOAT                      NOT NULL,
    Stealth                   FLOAT                      NOT NULL,
    ServerIP                  VARCHAR(50)                NULL,
    LastActivity              TIMESTAMP    DEFAULT NOW() NOT NULL,
    RX                        FLOAT        DEFAULT 0     NOT NULL,
    RY                        FLOAT        DEFAULT 0     NOT NULL,
    RZ                        FLOAT        DEFAULT 0     NOT NULL,
    Spirit                    FLOAT        DEFAULT 0     NOT NULL,
    Magic                     FLOAT        DEFAULT 0     NOT NULL,
    TeamNumber                INT          DEFAULT 0     NOT NULL,
    Thirst                    FLOAT        DEFAULT 0     NOT NULL,
    Hunger                    FLOAT        DEFAULT 0     NOT NULL,
    Gold                      INT          DEFAULT 0     NOT NULL,
    Score                     INT          DEFAULT 0     NOT NULL,
    CharacterLevel            SMALLINT     DEFAULT 0     NOT NULL,
    Gender                    SMALLINT     DEFAULT 0     NOT NULL,
    XP                        INT          DEFAULT 0     NOT NULL,
    HitDie                    SMALLINT     DEFAULT 0     NOT NULL,
    Wounds                    FLOAT        DEFAULT 0     NOT NULL,
    Size                      SMALLINT     DEFAULT 0     NOT NULL,
    Weight                    FLOAT        DEFAULT 0     NOT NULL,
    MaxHealth                 FLOAT        DEFAULT 0     NOT NULL,
    Health                    FLOAT        DEFAULT 0     NOT NULL,
    HealthRegenRate           FLOAT        DEFAULT 0     NOT NULL,
    MaxMana                   FLOAT        DEFAULT 0     NOT NULL,
    Mana                      FLOAT        DEFAULT 0     NOT NULL,
    ManaRegenRate             FLOAT        DEFAULT 0     NOT NULL,
    MaxEnergy                 FLOAT        DEFAULT 0     NOT NULL,
    Energy                    FLOAT        DEFAULT 0     NOT NULL,
    EnergyRegenRate           FLOAT        DEFAULT 0     NOT NULL,
    MaxFatigue                FLOAT        DEFAULT 0     NOT NULL,
    Fatigue                   FLOAT        DEFAULT 0     NOT NULL,
    FatigueRegenRate          FLOAT        DEFAULT 0     NOT NULL,
    MaxStamina                FLOAT        DEFAULT 0     NOT NULL,
    Stamina                   FLOAT        DEFAULT 0     NOT NULL,
    StaminaRegenRate          FLOAT        DEFAULT 0     NOT NULL,
    MaxEndurance              FLOAT        DEFAULT 0     NOT NULL,
    Endurance                 FLOAT        DEFAULT 0     NOT NULL,
    EnduranceRegenRate        FLOAT        DEFAULT 0     NOT NULL,
    Strength                  FLOAT        DEFAULT 0     NOT NULL,
    Dexterity                 FLOAT        DEFAULT 0     NOT NULL,
    Constitution              FLOAT        DEFAULT 0     NOT NULL,
    Intellect                 FLOAT        DEFAULT 0     NOT NULL,
    Wisdom                    FLOAT        DEFAULT 0     NOT NULL,
    Charisma                  FLOAT        DEFAULT 0     NOT NULL,
    Agility                   FLOAT        DEFAULT 0     NOT NULL,
    Fortitude                 FLOAT        DEFAULT 0     NOT NULL,
    Reflex                    FLOAT        DEFAULT 0     NOT NULL,
    Willpower                 FLOAT        DEFAULT 0     NOT NULL,
    BaseAttack                FLOAT        DEFAULT 0     NOT NULL,
    BaseAttackBonus           FLOAT        DEFAULT 0     NOT NULL,
    AttackPower               FLOAT        DEFAULT 0     NOT NULL,
    AttackSpeed               FLOAT        DEFAULT 0     NOT NULL,
    CritChance                FLOAT        DEFAULT 0     NOT NULL,
    CritMultiplier            FLOAT        DEFAULT 0     NOT NULL,
    Haste                     FLOAT        DEFAULT 0     NOT NULL,
    SpellPower                FLOAT        DEFAULT 0     NOT NULL,
    SpellPenetration          FLOAT        DEFAULT 0     NOT NULL,
    Defense                   FLOAT        DEFAULT 0     NOT NULL,
    Dodge                     FLOAT        DEFAULT 0     NOT NULL,
    Parry                     FLOAT        DEFAULT 0     NOT NULL,
    Avoidance                 FLOAT        DEFAULT 0     NOT NULL,
    Versatility               FLOAT        DEFAULT 0     NOT NULL,
    Multishot                 FLOAT        DEFAULT 0     NOT NULL,
    Initiative                FLOAT        DEFAULT 0     NOT NULL,
    NaturalArmor              FLOAT        DEFAULT 0     NOT NULL,
    PhysicalArmor             FLOAT        DEFAULT 0     NOT NULL,
    BonusArmor                FLOAT        DEFAULT 0     NOT NULL,
    ForceArmor                FLOAT        DEFAULT 0     NOT NULL,
    MagicArmor                FLOAT        DEFAULT 0     NOT NULL,
    Resistance                FLOAT        DEFAULT 0     NOT NULL,
    ReloadSpeed               FLOAT        DEFAULT 0     NOT NULL,
    `Range`                   FLOAT        DEFAULT 0     NOT NULL,
    Speed                     FLOAT        DEFAULT 0     NOT NULL,
    Silver                    INT          DEFAULT 0     NOT NULL,
    Copper                    INT          DEFAULT 0     NOT NULL,
    FreeCurrency              INT          DEFAULT 0     NOT NULL,
    PremiumCurrency           INT          DEFAULT 0     NOT NULL,
    Fame                      FLOAT        DEFAULT 0     NOT NULL,
    Alignment                 FLOAT        DEFAULT 0     NOT NULL,
    Description               TEXT                       NULL,
    DefaultPawnClassPath      VARCHAR(200) DEFAULT ''    NOT NULL,
    IsInternalNetworkTestUser BOOLEAN      DEFAULT FALSE NOT NULL,
    ClassID                   INT                        NOT NULL,
    BaseMesh                  VARCHAR(100)               NULL,
    IsAdmin                   BOOLEAN      DEFAULT FALSE NOT NULL,
    IsModerator               BOOLEAN      DEFAULT FALSE NOT NULL,
    CreateDate                TIMESTAMP    DEFAULT NOW() NOT NULL,
    CONSTRAINT PK_Chars
        PRIMARY KEY (CharacterID, CustomerGUID),
    CONSTRAINT FK_Characters_UserGUID
        FOREIGN KEY (UserGUID) REFERENCES Users (UserGUID)
);


CREATE TABLE CharHasAbilities
(
    CustomerGUID               CHAR(36)      NOT NULL,
    CharHasAbilitiesID         INT AUTO_INCREMENT,
    CharacterID                INT           NOT NULL,
    AbilityID                  INT           NOT NULL,
    AbilityLevel               INT DEFAULT 1 NOT NULL,
    CharHasAbilitiesCustomJSON TEXT          NULL,
    CONSTRAINT PK_CharHasAbilities
        PRIMARY KEY (CharHasAbilitiesID, CustomerGUID),
    CONSTRAINT FK_CharHasAbilities_CharacterID
        FOREIGN KEY (CharacterID, CustomerGUID) REFERENCES Characters (CharacterID, CustomerGUID)
);

CREATE TABLE CharAbilityBars
(
    CustomerGUID              CHAR(36)               NOT NULL,
    CharAbilityBarID          INT AUTO_INCREMENT,
    CharacterID               INT                    NOT NULL,
    AbilityBarName            VARCHAR(50) DEFAULT '' NOT NULL,
    CharAbilityBarsCustomJSON TEXT                   NULL,
    MaxNumberOfSlots          INT         DEFAULT 1  NOT NULL,
    NumberOfUnlockedSlots     INT         DEFAULT 1  NOT NULL,
    CONSTRAINT PK_CharAbilityBars
        PRIMARY KEY (CharAbilityBarID, CustomerGUID)
);

CREATE TABLE CharAbilityBarAbilities
(
    CustomerGUID                      CHAR(36)      NOT NULL,
    CharAbilityBarAbilityID           INT AUTO_INCREMENT,
    CharAbilityBarID                  INT           NOT NULL,
    CharHasAbilitiesID                INT           NOT NULL,
    CharAbilityBarAbilitiesCustomJSON TEXT          NULL,
    InSlotNumber                      INT DEFAULT 1 NOT NULL,
    CONSTRAINT PK_CharAbilityBarAbilities
        PRIMARY KEY (CharAbilityBarAbilityID, CustomerGUID),
    CONSTRAINT FK_CharAbilityBarAbilities_CharAbilityBarID
        FOREIGN KEY (CharAbilityBarID, CustomerGUID) REFERENCES CharAbilityBars (CharAbilityBarID, CustomerGUID),
    CONSTRAINT FK_CharAbilityBarAbilities_CharHasAbilities
        FOREIGN KEY (CharHasAbilitiesID, CustomerGUID) REFERENCES CharHasAbilities (CharHasAbilitiesID, CustomerGUID)
);

CREATE TABLE CharHasItems
(
    CustomerGUID  CHAR(36)              NOT NULL,
    CharacterID   INT                   NOT NULL,
    CharHasItemID INT AUTO_INCREMENT,
    ItemID        INT                   NOT NULL,
    Quantity      INT     DEFAULT 1     NOT NULL,
    Equipped      BOOLEAN DEFAULT FALSE NOT NULL,
    CONSTRAINT PK_CharHasItems
        PRIMARY KEY (CharHasItemID, CharacterID, CustomerGUID),
    CONSTRAINT FK_CharHasItems_CharacterID
        FOREIGN KEY (CharacterID, CustomerGUID) REFERENCES Characters (CharacterID, CustomerGUID)
);

CREATE TABLE CharInventory
(
    CustomerGUID    CHAR(36)      NOT NULL,
    CharacterID     INT           NOT NULL,
    CharInventoryID INT AUTO_INCREMENT,
    InventoryName   VARCHAR(50)   NOT NULL,
    InventorySize   INT           NOT NULL,
    InventoryWidth  INT DEFAULT 1 NOT NULL,
    InventoryHeight INT DEFAULT 1 NOT NULL,
    CONSTRAINT PK_CharInventory
        PRIMARY KEY (CharInventoryID, CharacterID, CustomerGUID)
);

CREATE TABLE CharInventoryItems
(
    CustomerGUID          CHAR(36)        NOT NULL,
    CharInventoryID       INT             NOT NULL,
    CharInventoryItemID   INT AUTO_INCREMENT,
    ItemID                INT             NOT NULL,
    InSlotNumber          INT             NOT NULL,
    Quantity              INT             NOT NULL,
    NumberOfUsesLeft      INT DEFAULT 0   NOT NULL,
    `Condition`           INT DEFAULT 100 NOT NULL,
    CharInventoryItemGUID CHAR(36)        NOT NULL,
    CustomData            TEXT            NULL,
    CONSTRAINT PK_CharInventoryItems
        PRIMARY KEY (CharInventoryItemID, CharInventoryID, CustomerGUID)
);

CREATE TABLE CharOnMapInstance
(
    CustomerGUID  CHAR(36) NOT NULL,
    CharacterID   INT      NOT NULL,
    MapInstanceID INT      NOT NULL,
    CONSTRAINT PK_CharOnMapInstance
        PRIMARY KEY (CharacterID, MapInstanceID, CustomerGUID)
);

CREATE TABLE ChatGroups
(
    CustomerGUID  CHAR(36)    NOT NULL,
    ChatGroupID   INT AUTO_INCREMENT,
    ChatGroupName VARCHAR(50) NOT NULL,
    CONSTRAINT PK_ChatGroups
        PRIMARY KEY (ChatGroupID, CustomerGUID)
);

CREATE TABLE ChatGroupUsers
(
    CustomerGUID CHAR(36) NOT NULL,
    ChatGroupID  INT      NOT NULL,
    CharacterID  INT      NOT NULL,
    CONSTRAINT PK_ChatGroupUsers
        PRIMARY KEY (ChatGroupID, CharacterID, CustomerGUID)
);

CREATE TABLE ChatMessages
(
    CustomerGUID  CHAR(36)                NOT NULL,
    ChatMessageID INT AUTO_INCREMENT,
    SentByCharID  INT                     NOT NULL,
    SentToCharID  INT                     NULL,
    ChatGroupID   INT                     NULL,
    ChatMessage   TEXT                    NOT NULL,
    MessageDate   TIMESTAMP DEFAULT NOW() NOT NULL,
    CONSTRAINT PK_ChatMessages
        PRIMARY KEY (ChatMessageID, CustomerGUID)
);



CREATE TABLE Class
(
    CustomerGUID       CHAR(36)               NOT NULL,
    ClassID            INT AUTO_INCREMENT,
    ClassName          VARCHAR(50) DEFAULT '' NOT NULL,
    StartingMapName    VARCHAR(50)            NOT NULL,
    X                  FLOAT                  NOT NULL,
    Y                  FLOAT                  NOT NULL,
    Z                  FLOAT                  NOT NULL,
    Perception         FLOAT                  NOT NULL,
    Acrobatics         FLOAT                  NOT NULL,
    Climb              FLOAT                  NOT NULL,
    Stealth            FLOAT                  NOT NULL,
    RX                 FLOAT                  NOT NULL,
    RY                 FLOAT                  NOT NULL,
    RZ                 FLOAT                  NOT NULL,
    Spirit             FLOAT                  NOT NULL,
    Magic              FLOAT                  NOT NULL,
    TeamNumber         INT                    NOT NULL,
    Thirst             FLOAT                  NOT NULL,
    Hunger             FLOAT                  NOT NULL,
    Gold               INT                    NOT NULL,
    Score              INT                    NOT NULL,
    CharacterLevel     SMALLINT               NOT NULL,
    Gender             SMALLINT               NOT NULL,
    XP                 INT                    NOT NULL,
    HitDie             SMALLINT               NOT NULL,
    Wounds             FLOAT                  NOT NULL,
    Size               SMALLINT               NOT NULL,
    Weight             FLOAT                  NOT NULL,
    MaxHealth          FLOAT                  NOT NULL,
    Health             FLOAT                  NOT NULL,
    HealthRegenRate    FLOAT                  NOT NULL,
    MaxMana            FLOAT                  NOT NULL,
    Mana               FLOAT                  NOT NULL,
    ManaRegenRate      FLOAT                  NOT NULL,
    MaxEnergy          FLOAT                  NOT NULL,
    Energy             FLOAT                  NOT NULL,
    EnergyRegenRate    FLOAT                  NOT NULL,
    MaxFatigue         FLOAT                  NOT NULL,
    Fatigue            FLOAT                  NOT NULL,
    FatigueRegenRate   FLOAT                  NOT NULL,
    MaxStamina         FLOAT                  NOT NULL,
    Stamina            FLOAT                  NOT NULL,
    StaminaRegenRate   FLOAT                  NOT NULL,
    MaxEndurance       FLOAT                  NOT NULL,
    Endurance          FLOAT                  NOT NULL,
    EnduranceRegenRate FLOAT                  NOT NULL,
    Strength           FLOAT                  NOT NULL,
    Dexterity          FLOAT                  NOT NULL,
    Constitution       FLOAT                  NOT NULL,
    Intellect          FLOAT                  NOT NULL,
    Wisdom             FLOAT                  NOT NULL,
    Charisma           FLOAT                  NOT NULL,
    Agility            FLOAT                  NOT NULL,
    Fortitude          FLOAT                  NOT NULL,
    Reflex             FLOAT                  NOT NULL,
    Willpower          FLOAT                  NOT NULL,
    BaseAttack         FLOAT                  NOT NULL,
    BaseAttackBonus    FLOAT                  NOT NULL,
    AttackPower        FLOAT                  NOT NULL,
    AttackSpeed        FLOAT                  NOT NULL,
    CritChance         FLOAT                  NOT NULL,
    CritMultiplier     FLOAT                  NOT NULL,
    Haste              FLOAT                  NOT NULL,
    SpellPower         FLOAT                  NOT NULL,
    SpellPenetration   FLOAT                  NOT NULL,
    Defense            FLOAT                  NOT NULL,
    Dodge              FLOAT                  NOT NULL,
    Parry              FLOAT                  NOT NULL,
    Avoidance          FLOAT                  NOT NULL,
    Versatility        FLOAT                  NOT NULL,
    Multishot          FLOAT                  NOT NULL,
    Initiative         FLOAT                  NOT NULL,
    NaturalArmor       FLOAT                  NOT NULL,
    PhysicalArmor      FLOAT                  NOT NULL,
    BonusArmor         FLOAT                  NOT NULL,
    ForceArmor         FLOAT                  NOT NULL,
    MagicArmor         FLOAT                  NOT NULL,
    Resistance         FLOAT                  NOT NULL,
    ReloadSpeed        FLOAT                  NOT NULL,
    `Range`            FLOAT                  NOT NULL,
    Speed              FLOAT                  NOT NULL,
    Silver             INT                    NOT NULL,
    Copper             INT                    NOT NULL,
    FreeCurrency       INT                    NOT NULL,
    PremiumCurrency    INT                    NOT NULL,
    Fame               FLOAT                  NOT NULL,
    Alignment          FLOAT                  NOT NULL,
    Description        TEXT                   NULL,
    CONSTRAINT PK_Class
        PRIMARY KEY (ClassID, CustomerGUID)
);

CREATE TABLE ClassInventory
(
    ClassInventoryID INT AUTO_INCREMENT,
    CustomerGUID     CHAR(36)    NOT NULL,
    ClassID          INT         NOT NULL,
    InventoryName    VARCHAR(50) NOT NULL,
    InventorySize    INT         NOT NULL,
    InventoryWidth   INT         NOT NULL,
    InventoryHeight  INT         NOT NULL,
    CONSTRAINT PK_ClassInventory
        PRIMARY KEY (ClassInventoryID)
);

CREATE TABLE CustomCharacterData
(
    CustomerGUID          CHAR(36)    NOT NULL,
    CustomCharacterDataID INT AUTO_INCREMENT,
    CharacterID           INT         NOT NULL,
    CustomFieldName       VARCHAR(50) NOT NULL,
    FieldValue            TEXT        NOT NULL,
    CONSTRAINT PK_CustomCharacterData
        PRIMARY KEY (CustomCharacterDataID, CustomerGUID),
    CONSTRAINT FK_CustomCharacterData_CharID
        FOREIGN KEY (CharacterID, CustomerGUID) REFERENCES Characters (CharacterID, CustomerGUID)
);

CREATE TABLE GlobalData
(
    CustomerGUID    CHAR(36)    NOT NULL,
    GlobalDataKey   VARCHAR(50) NOT NULL,
    GlobalDataValue TEXT        NOT NULL,
    CONSTRAINT PK_GlobalData
        PRIMARY KEY (GlobalDataKey, CustomerGUID)
);

CREATE TABLE Items
(
    CustomerGUID         CHAR(36)                     NOT NULL,
    ItemID               INT AUTO_INCREMENT,
    ItemTypeID           INT                          NOT NULL,
    ItemName             VARCHAR(50)                  NOT NULL,
    ItemWeight           DECIMAL(18, 2) DEFAULT 0     NOT NULL,
    ItemCanStack         BOOLEAN        DEFAULT FALSE NOT NULL,
    ItemStackSize        SMALLINT       DEFAULT 0     NOT NULL,
    ItemIsUsable         BOOLEAN        DEFAULT FALSE NOT NULL,
    ItemIsConsumedOnUse  BOOLEAN        DEFAULT TRUE  NOT NULL,
    CustomData           VARCHAR(2000)  DEFAULT ''    NOT NULL,
    DefaultNumberOfUses  INT            DEFAULT 0     NOT NULL,
    ItemValue            INT            DEFAULT 0     NOT NULL,
    ItemMesh             VARCHAR(200)   DEFAULT ''    NOT NULL,
    MeshToUseForPickup   VARCHAR(200)   DEFAULT ''    NOT NULL,
    TextureToUseForIcon  VARCHAR(200)   DEFAULT ''    NOT NULL,
    PremiumCurrencyPrice INT            DEFAULT 0     NOT NULL,
    FreeCurrencyPrice    INT            DEFAULT 0     NOT NULL,
    ItemTier             INT            DEFAULT 0     NOT NULL,
    ItemDescription      TEXT           DEFAULT ''    NOT NULL,
    ItemCode             VARCHAR(50)    DEFAULT ''    NOT NULL,
    ItemDuration         INT            DEFAULT 0     NOT NULL,
    CanBeDropped         BOOLEAN        DEFAULT TRUE  NOT NULL,
    CanBeDestroyed       BOOLEAN        DEFAULT FALSE NOT NULL,
    WeaponActorClass     VARCHAR(200)   DEFAULT ''    NOT NULL,
    StaticMesh           VARCHAR(200)   DEFAULT ''    NOT NULL,
    SkeletalMesh         VARCHAR(200)   DEFAULT ''    NOT NULL,
    ItemQuality          SMALLINT       DEFAULT 0     NOT NULL,
    IconSlotWidth        INT            DEFAULT 1     NOT NULL,
    IconSlotHeight       INT            DEFAULT 1     NOT NULL,
    ItemMeshID           INT            DEFAULT 0     NOT NULL,
    CONSTRAINT PK_Items
        PRIMARY KEY (ItemID, CustomerGUID)
);

CREATE TABLE ItemTypes
(
    CustomerGUID      CHAR(36)           NOT NULL,
    ItemTypeID        INT AUTO_INCREMENT,
    ItemTypeDesc      VARCHAR(50)        NOT NULL,
    UserItemType      SMALLINT DEFAULT 0 NOT NULL,
    EquipmentType     SMALLINT DEFAULT 0 NOT NULL,
    ItemQuality       SMALLINT DEFAULT 0 NOT NULL,
    EquipmentSlotType SMALLINT DEFAULT 0 NOT NULL,
    CONSTRAINT PK_ItemTypes
        PRIMARY KEY (ItemTypeID, CustomerGUID)
);

CREATE TABLE MapInstances
(
    CustomerGUID            CHAR(36)                NOT NULL,
    MapInstanceID           INT AUTO_INCREMENT,
    WorldServerID           INT                     NOT NULL,
    MapID                   INT                     NOT NULL,
    Port                    INT                     NOT NULL,
    Status                  INT       DEFAULT 0     NOT NULL,
    PlayerGroupID           INT                     NULL,
    NumberOfReportedPlayers INT       DEFAULT 0     NOT NULL,
    LastUpdateFromServer    TIMESTAMP               NULL,
    LastServerEmptyDate     TIMESTAMP               NULL,
    CreateDate              TIMESTAMP DEFAULT NOW() NOT NULL,
    CONSTRAINT PK_MapInstances
        PRIMARY KEY (MapInstanceID, CustomerGUID)
);

CREATE TABLE Maps
(
    CustomerGUID                CHAR(36)                NOT NULL,
    MapID                       INT AUTO_INCREMENT,
    MapName                     VARCHAR(50)             NOT NULL,
    MapData                     LONGBLOB                NULL,
    Width                       SMALLINT                NOT NULL,
    Height                      SMALLINT                NOT NULL,
    ZoneName                    VARCHAR(50)             NOT NULL,
    WorldCompContainsFilter     VARCHAR(100) DEFAULT '' NOT NULL,
    WorldCompListFilter         VARCHAR(200) DEFAULT '' NOT NULL,
    MapMode                     INT          DEFAULT 1  NOT NULL,
    SoftPlayerCap               INT          DEFAULT 60 NOT NULL,
    HardPlayerCap               INT          DEFAULT 80 NOT NULL,
    MinutesToShutdownAfterEmpty INT          DEFAULT 1  NOT NULL,
    CONSTRAINT PK_Maps
        PRIMARY KEY (MapID, CustomerGUID)
);

CREATE TABLE OWSVersion
(
    OWSDBVersion VARCHAR(10) NULL
);

CREATE TABLE PlayerGroupTypes
(
    PlayerGroupTypeID   INT         NOT NULL,
    PlayerGroupTypeDesc VARCHAR(50) NOT NULL,
    CONSTRAINT PK_PlayerGroupTypes
        PRIMARY KEY (PlayerGroupTypeID)
);

CREATE TABLE PlayerGroup
(
    PlayerGroupID     INT AUTO_INCREMENT,
    CustomerGUID      CHAR(36)      NOT NULL,
    PlayerGroupName   VARCHAR(50)   NOT NULL,
    PlayerGroupTypeID INT           NOT NULL,
    ReadyState        INT DEFAULT 0 NOT NULL,
    CreateDate        TIMESTAMP     NULL,
    CONSTRAINT PK_PlayerGroup
        PRIMARY KEY (PlayerGroupID, CustomerGUID),
    CONSTRAINT FK_PlayerGroup_PlayerGroupType
        FOREIGN KEY (PlayerGroupTypeID) REFERENCES PlayerGroupTypes (PlayerGroupTypeID)
);

CREATE TABLE PlayerGroupCharacters
(
    PlayerGroupID INT                     NOT NULL,
    CustomerGUID  CHAR(36)                NOT NULL,
    CharacterID   INT                     NOT NULL,
    DateAdded     TIMESTAMP DEFAULT NOW() NOT NULL,
    TeamNumber    INT       DEFAULT 0     NOT NULL,
    CONSTRAINT PK_PlayerGroupCharacters
        PRIMARY KEY (PlayerGroupID, CharacterID, CustomerGUID)
);

CREATE TABLE Races
(
    CustomerGUID CHAR(36)    NOT NULL,
    RaceID       INT AUTO_INCREMENT,
    RaceName     VARCHAR(50) NOT NULL,
    CONSTRAINT PK_Races
        PRIMARY KEY (RaceID, CustomerGUID)
);

CREATE TABLE UserSessions
(
    CustomerGUID          CHAR(36)    NOT NULL,
    UserSessionGUID       CHAR(36)    NOT NULL,
    UserGUID              CHAR(36)    NOT NULL,
    LoginDate             TIMESTAMP   NOT NULL,
    SelectedCharacterName VARCHAR(50) NULL,
    CONSTRAINT PK_UserSessions
        PRIMARY KEY (UserSessionGUID, CustomerGUID),
    CONSTRAINT FK_UserSessions_UserGUID
        FOREIGN KEY (UserGUID) REFERENCES Users (UserGUID)
);

CREATE TABLE UsersInQueue
(
    CustomerGUID     CHAR(36)      NOT NULL,
    UserGUID         CHAR(36)      NOT NULL,
    QueueName        VARCHAR(20)   NOT NULL,
    JoinDT           TIMESTAMP     NOT NULL,
    MatchMakingScore INT DEFAULT 0 NOT NULL,
    CONSTRAINT PK_UsersInQueue
        PRIMARY KEY (UserGUID, QueueName, CustomerGUID)
);

CREATE TABLE WorldServers
(
    CustomerGUID            CHAR(36)                 NOT NULL,
    WorldServerID           INT AUTO_INCREMENT,
    ServerIP                VARCHAR(50)              NOT NULL,
    MaxNumberOfInstances    INT                      NOT NULL,
    ActiveStartTime         TIMESTAMP                NULL,
    Port                    INT         DEFAULT 8181 NOT NULL,
    ServerStatus            SMALLINT    DEFAULT 0    NOT NULL,
    InternalServerIP        VARCHAR(50) DEFAULT ''   NOT NULL,
    StartingMapInstancePort INT         DEFAULT 7778 NOT NULL,
    CONSTRAINT PK_WorldServers
        PRIMARY KEY (WorldServerID, CustomerGUID)
);

CREATE TABLE WorldSettings
(
    CustomerGUID    CHAR(36) NOT NULL,
    WorldSettingsID INT AUTO_INCREMENT,
    StartTime       BIGINT   NOT NULL,
    CONSTRAINT PK_WorldSettings
        PRIMARY KEY (WorldSettingsID, CustomerGUID)
);

CREATE VIEW vRandNumber AS
SELECT RAND() AS RandNumber;

DELIMITER $$

CREATE OR REPLACE FUNCTION AbilityMod(AbilityCore INT)
    RETURNS INT
    DETERMINISTIC
    RETURN (SELECT FLOOR((AbilityCore - 10) / 2));

CREATE OR REPLACE FUNCTION CalcXFromTile(Tile INT, MapWidth SMALLINT)
    RETURNS INT
    DETERMINISTIC
    RETURN (SELECT Tile % MapWidth);

CREATE OR REPLACE FUNCTION CalcYFromTile(Tile INT, MapWidth SMALLINT)
    RETURNS INT
    DETERMINISTIC
    RETURN (SELECT FLOOR(Tile / MapWidth));

CREATE OR REPLACE FUNCTION OffsetIntoMap(X INT, Y INT, MapWidth SMALLINT)
    RETURNS INT
    DETERMINISTIC
    RETURN (SELECT (Y - 1) * MapWidth + X);

CREATE OR REPLACE FUNCTION RollDice(NumberOfDice INT, MaxDiceValue INT)
    RETURNS INT
    NOT DETERMINISTIC
    READS SQL DATA
BEGIN
    DECLARE _TotalValue, _CurrentDiceRoll INT;
    SET _TotalValue = 0;
    SET _CurrentDiceRoll = 0;
    WHILE (_CurrentDiceRoll < NumberOfDice)
        DO
            SELECT _TotalValue + CAST(FLOOR((MaxDiceValue) * RandNumber) AS INTEGER) + 1
            INTO _TotalValue
            FROM vRandNumber;
            SET _CurrentDiceRoll = _CurrentDiceRoll + 1;
        END WHILE;
    RETURN _TotalValue;
END;

CREATE OR REPLACE FUNCTION CalculateDistanceBetweenTwoTiles(Tile1 INT, Tile2 INT, MapWidth SMALLINT)
    RETURNS INT
    DETERMINISTIC
    READS SQL DATA
BEGIN
    DECLARE _Distance,_Tile1X,_Tile1Y,_Tile2X,_Tile2Y INT;
    DECLARE _DiffX, _DiffY FLOAT;
    SELECT 0, 0, 0, 0, 0 INTO _Distance,_Tile1X,_Tile1Y,_Tile2X,_Tile2Y;
    SELECT 0.0, 0.0 INTO _DiffX, _DiffY;

    SELECT CalcXFromTile(Tile1, MapWidth) INTO _Tile1X;
    SELECT CalcYFromTile(Tile1, MapWidth) INTO _Tile1Y;
    SELECT CalcXFromTile(Tile2, MapWidth) INTO _Tile2X;
    SELECT CalcYFromTile(Tile2, MapWidth) INTO _Tile2Y;

    IF (_Tile1X > _Tile2X) THEN
        IF (_Tile1Y > _Tile2Y) THEN
            SET _DiffX = _Tile1X - _Tile2X;
            SET _DiffY = _Tile1Y - _Tile2Y;
        ELSE
            SET _DiffX = _Tile1X - _Tile2X;
            SET _DiffY = _Tile2Y - _Tile1Y;
        END IF;
    ELSE
        IF (_Tile1Y > _Tile2Y) THEN
            SET _DiffX = _Tile2X - _Tile1X;
            SET _DiffY = _Tile1Y - _Tile2Y;
        ELSE
            SET _DiffX = _Tile2X - _Tile1X;
            SET _DiffY = _Tile2Y - _Tile1Y;
        END IF;
    END IF;

    IF (_DiffX > _DiffY) THEN
        SET _Distance = (FLOOR(_DiffY * 1.5) + (_DiffX - _DiffY));
    ELSE
        SET _Distance = (FLOOR(_DiffY * 1.5) + (_DiffY - _DiffX));
    END IF;

    RETURN _Distance * 5;
END;

CREATE OR REPLACE PROCEDURE fSplit(DelimitedString VARCHAR(8000), Delimiter VARCHAR(100))
BEGIN
    DECLARE _Index, _Start, _DelSize INT;
    SET _Index = 0;
    SET _Start = 0;
    SET _DelSize = 0;

    DROP TEMPORARY TABLE IF EXISTS tmp_fSplit;
    CREATE TEMPORARY TABLE tmp_fSplit
    (
        ElementID INT AUTO_INCREMENT,
        ELEMENT   VARCHAR(1000),
        CONSTRAINT PK_tmp_fSplit
            PRIMARY KEY (ElementID)
    ) ENGINE = MEMORY;

    SET _DelSize = LENGTH(Delimiter);
    _loop:
    WHILE LENGTH(DelimitedString) > 0
        DO
            SET _Index = POSITION(Delimiter IN DelimitedString);
            IF _Index = 0 THEN
                INSERT INTO tmp_fSplit (ELEMENT) VALUES (TRIM(BOTH FROM DelimitedString));
                LEAVE _loop;
            ELSE
                INSERT INTO tmp_fSplit (ELEMENT) VALUES (TRIM(BOTH FROM SUBSTRING(DelimitedString, 1, (_Index - 1))));
                SET _Start = _Index + _DelSize;
                SET DelimitedString = SUBSTRING(DelimitedString, _Start, (LENGTH(DelimitedString) - _Start + 1));
            END IF;
        END WHILE _loop;
    SELECT * FROM tmp_fSplit;
    DROP TEMPORARY TABLE IF EXISTS tmp_fSplit;
END;

CREATE OR REPLACE PROCEDURE GetPointsOnLine(Tile1 INT, Tile2 INT, MapWidth SMALLINT)
BEGIN
    DECLARE _Tile1X, _Tile1Y, _Tile2X, _Tile2Y, _Temp, _DeltaX, _DeltaY, _Error, _Y, _YStep, _X, _Tile INT;
    DECLARE _SwapXY BOOLEAN;
    SET _Tile1X = 0;
    SET _Tile2X = 0;
    SET _Tile2Y = 0;
    SET _Temp = 0;
    SET _DeltaX = 0;
    SET _DeltaY = 0;
    SET _Error = 0;
    SET _Y = 0;
    SET _YStep = 0;
    SET _X = 0;
    SET _Tile = 0;
    SET _SwapXY = FALSE;

    DROP TEMPORARY TABLE IF EXISTS tmp_GetPointsOnLine;
    CREATE TEMPORARY TABLE tmp_GetPointsOnLine
    (
        MapOrder INT
    ) ENGINE = MEMORY;

    SELECT CalcXFromTile(Tile1 - 1, MapWidth) INTO _Tile1X;
    SELECT CalcYFromTile(Tile1 - 1, MapWidth) + 1 INTO _Tile1Y;
    SELECT CalcXFromTile(Tile2 - 1, MapWidth) INTO _Tile2X;
    SELECT CalcYFromTile(Tile2 - 1, MapWidth) + 1 INTO _Tile2Y;

    IF (ABS(_Tile2Y - _Tile1Y) > ABS(_Tile2X - _Tile1X)) THEN
        SET _SwapXY = TRUE;
    END IF;

    IF (_SwapXY = TRUE) THEN
        -- swap x and y
        SET _Temp = _Tile1X;
        SET _Tile1X = _Tile1Y;
        SET _Tile1Y = _Temp; -- swap x0 and y0
        SET _Temp = _Tile2X;
        SET _Tile2X = _Tile2Y;
        SET _Tile2Y = _Temp; -- swap x1 and y1
    END IF;

    IF (_Tile1X > _Tile2X) THEN
        -- make sure x0 < x1
        SET _Temp = _Tile1X;
        SET _Tile1X = _Tile2X;
        SET _Tile2X = _Temp; -- swap x0 and x1
        SET _Temp = _Tile1Y;
        SET _Tile1Y = _Tile2Y;
        SET _Tile2Y = _Temp; -- swap y0 and y1
    END IF;

    SET _DeltaX = _Tile2X - _Tile1X;
    SET _DeltaY = FLOOR(ABS(_Tile2Y - _Tile1Y));
    SET _Error = FLOOR(_DeltaX / 2.0);
    SET _Y = _Tile1Y;

    IF (_Tile1Y < _Tile2Y) THEN
        SET _YStep = 1;
    ELSE
        SET _YStep = -1;
    END IF;

    IF (_SwapXY = TRUE) THEN
        -- Y / X
        SET _X = _Tile1X;
        WHILE _X < (_Tile2X + 1)
            DO
                SET _Tile = OffsetIntoMap(_Y + 1, _X, MapWidth);
                SET _X = _X + 1;

                INSERT INTO tmp_GetPointsOnLine (MapOrder) VALUES (_Tile);

                SET _Error = _Error - _DeltaY;
                IF _Error < 0 THEN
                    SET _Y = _Y + _YStep;
                    SET _Error = _Error + _DeltaX;
                END IF;
            END WHILE;
    ELSE
        -- X / Y
        SET _X = _Tile1X;
        WHILE _X < (_Tile2X + 1)
            DO
                SET _X = _X + 1;
                SET _Tile = OffsetIntoMap(_X, _Y, MapWidth);
                INSERT INTO tmp_GetPointsOnLine (MapOrder) VALUES (_Tile);

                SET _Error = _Error - _DeltaY;
                IF _Error < 0 THEN
                    SET _Y = _Y + _YStep;
                    SET _Error = _Error + _DeltaX;
                END IF;
            END WHILE;
    END IF;

    SELECT * FROM tmp_GetPointsOnLine;
    DROP TEMPORARY TABLE IF EXISTS tmp_GetPointsOnLine;
END;

CREATE OR REPLACE PROCEDURE GetPointsOnVisionLine(Tile1 INT, Tile2 INT, MapWidth SMALLINT)
BEGIN
    DECLARE _Tile1X, _Tile1Y, _Tile2X, _Tile2Y, _Error, _Y, _YStep, _X, _Tile, _I, _XStep, _ErrorPrev, _DDY, _DDX, _DX, _DY INT;

    SET _Tile1X = 0;
    SET _Tile1Y = 0;
    SET _Tile2X = 0;
    SET _Tile2Y = 0;
    SET _Error = 0;
    SET _Y = 0;
    SET _YStep = 0;
    SET _X = 0;
    SET _Tile = 0;
    SET _XStep = 0;
    SET _ErrorPrev = 0;
    SET _DDY = 0;
    SET _DDX = 0;
    SET _DX = 0;
    SET _DY = 0;

    DROP TEMPORARY TABLE IF EXISTS tmp_GetPointsOnVisionLine;
    CREATE TEMPORARY TABLE tmp_GetPointsOnVisionLine
    (
        MapOrder INT,
        FromCode INT
    ) ENGINE = MEMORY;

    SELECT CalcXFromTile(Tile1, MapWidth) INTO _Tile1X;
    SELECT CalcYFromTile(Tile1 - 1, MapWidth) + 1 INTO _Tile1Y;
    SELECT CalcXFromTile(Tile2, MapWidth) INTO _Tile2X;
    SELECT CalcYFromTile(Tile2 - 1, MapWidth) + 1 INTO _Tile2Y;

    SET _Y = _Tile1Y;
    SET _X = _Tile1X;
    SET _DX = _Tile2X - _Tile1X;
    SET _DY = _Tile2Y - _Tile1Y;

    -- POINT (y1, x1);  // first point
    SET _Tile = OffsetIntoMap(_Tile1X, _Tile1Y, MapWidth);
    INSERT INTO tmp_GetPointsOnVisionLine (MapOrder, FromCode) VALUES (_Tile, 0);

    -- NB the last point can't be here, because of its previous point (which has to be verified)
    IF (_DY < 0) THEN
        SET _YStep = -1;
        SET _DY = -_DY;
    ELSE
        SET _YStep = 1;
    END IF;

    IF (_DX < 0) THEN
        SET _XStep = -1;
        SET _DX = -_DX;
    ELSE
        SET _XStep = 1;
    END IF;

    SET _DDY = 2 * _DY; -- work with double values for full precision
    SET _DDX = 2 * _DX;

    IF (_DDX >= _DDY) THEN -- first octant (0 <= slope <= 1)
    -- compulsory initialization (even for errorprev, needed when dx==dy)
        SET _ErrorPrev = _DX;-- start in the middle of the square
        SET _Error = _DX;
        SET _I = 0;
        -- for (i=0 ; i < dx ; i++){  -- do not use the first point (already done)
        WHILE _I < _DX
            DO
                SET _X = _X + _XStep;
                SET _Error = _Error + _DDY;

                IF _Error > _DDX THEN -- increment y if AFTER the middle ( > )
                    SET _Y = _Y + _YStep;
                    SET _Error = _Error - _DDX;
                    -- three cases (octant == right->right-top for directions below):
                    IF (_Error + _ErrorPrev) < _DDX THEN-- bottom square also
                        SET _Tile = OffsetIntoMap(_X, _Y, MapWidth);
                        INSERT INTO tmp_GetPointsOnVisionLine (MapOrder, FromCode) VALUES (_Tile, 1);
                    ELSEIF (_Error + _ErrorPrev) > _DDX THEN -- left square also
                    -- POINT (y, x-xstep);
                        SET _Tile = OffsetIntoMap(_X - _XStep, _Y, MapWidth);
                        INSERT INTO tmp_GetPointsOnVisionLine (MapOrder, FromCode) VALUES (_Tile, 2);
                    ELSE -- corner: bottom and left squares also
                    -- POINT (y-ystep, x);
                        SET _Tile = OffsetIntoMap(_X, _Y - _YStep, MapWidth);
                        INSERT INTO tmp_GetPointsOnVisionLine (MapOrder, FromCode) VALUES (_Tile, 3);

                        SET _Tile = OffsetIntoMap(_X - _XStep, _Y, MapWidth);
                        INSERT INTO tmp_GetPointsOnVisionLine (MapOrder, FromCode) VALUES (_Tile, 4);
                    END IF;
                END IF;

                SET _Tile = OffsetIntoMap(_X, _Y, MapWidth);
                INSERT INTO tmp_GetPointsOnVisionLine (MapOrder, FromCode) VALUES (_Tile, 5);

                SET _ErrorPrev = _Error;
                SET _I = _I + 1;
            END WHILE; -- WHILE (@i < @dx)
    ELSE -- IF (@ddx >= @ddy)
    -- the same as above
        SET _ErrorPrev = _DY;
        SET _Error = _DY;

        -- for (i=0 ; i < dy ; i++){
        WHILE _I < _DY
            DO
                SET _Y = _Y + _YStep;
                SET _Error = _Error + _DDX;

                IF _Error > _DDY THEN
                    SET _X = _X + _XStep;
                    SET _Error = _Error - _DDY;
                    IF (_Error + _ErrorPrev) < _DDY THEN
                        -- POINT (y, x-xstep);
                        SET _Tile = OffsetIntoMap(_X - _XStep, _Y, MapWidth);
                        INSERT INTO tmp_GetPointsOnVisionLine (MapOrder, FromCode) VALUES (_Tile, 6);
                    ELSEIF (_Error + _ErrorPrev) > _DDY THEN
                        -- POINT (y-ystep, x);
                        SET _Tile = OffsetIntoMap(_X, _Y - _YStep, MapWidth);
                        INSERT INTO tmp_GetPointsOnVisionLine (MapOrder, FromCode) VALUES (_Tile, 7);
                    ELSE
                        -- POINT (y, x-xstep); );
                        SET _Tile = OffsetIntoMap(_X - _XStep, _Y, MapWidth);
                        INSERT INTO tmp_GetPointsOnVisionLine (MapOrder, FromCode) VALUES (_Tile, 8);
                        -- POINT (y-ystep, x);
                        SET _Tile = OffsetIntoMap(_X, _Y - _YStep, MapWidth);
                        INSERT INTO tmp_GetPointsOnVisionLine (MapOrder, FromCode) VALUES (_Tile, 9);
                    END IF;
                END IF;

                SET _Tile = OffsetIntoMap(_X, _Y, MapWidth);
                INSERT INTO tmp_GetPointsOnVisionLine (MapOrder, FromCode) VALUES (_Tile, 10);

                SET _ErrorPrev = _Error;
                SET _I = _I + 1;
            END WHILE; -- WHILE (@i < @dy)
    END IF;-- IF (@ddx >= @ddy)

    SELECT * FROM tmp_GetPointsOnVisionLine;
    DROP TEMPORARY TABLE IF EXISTS tmp_GetPointsOnVisionLine;
END;

CREATE OR REPLACE PROCEDURE AddCharacter(_CustomerGUID CHAR(36),
                                         _UserSessionGUID CHAR(36),
                                         _CharacterName VARCHAR(50),
                                         _ClassName VARCHAR(50))
BEGIN
    DECLARE _ErrorRaised, _SupportUnicode BOOLEAN;
    DECLARE _UserGUID CHAR(36);
    DECLARE _ClassID, _CharacterID, _CountOfCharNamesFound, _CountOfClassInventory, _InvalidCharacters INT;

    SET _ErrorRaised = FALSE;
    SET _SupportUnicode = FALSE;
    SET _CountOfCharNamesFound = 0;
    SET _CountOfClassInventory = 0;

    DROP TEMPORARY TABLE IF EXISTS tmp_AddCharacter;
    CREATE TEMPORARY TABLE tmp_AddCharacter
    (
        ErrorMessage    VARCHAR(100),
        CharacterName   VARCHAR(50),
        ClassName       VARCHAR(50),
        CharacterLevel  INT,
        StartingMapName VARCHAR(50),
        X               FLOAT,
        Y               FLOAT,
        Z               FLOAT,
        RX              FLOAT,
        RY              FLOAT,
        RZ              FLOAT,
        TeamNumber      INT,
        Gold            INT,
        Silver          INT,
        Copper          INT,
        FreeCurrency    INT,
        PremiumCurrency INT,
        Fame            INT,
        Alignment       INT,
        Score           INT,
        Gender          INT,
        XP              INT,
        Size            INT,
        Weight          FLOAT
    ) ENGINE = MEMORY;

    SELECT C.SupportUnicode INTO _SupportUnicode FROM Customers C WHERE C.CustomerGUID = _CustomerGUID;

    SELECT US.UserGUID
    INTO _UserGUID
    FROM UserSessions US
    WHERE US.CustomerGUID = _CustomerGUID
      AND US.UserSessionGUID = _UserSessionGUID;

    SELECT C.ClassID INTO _ClassID FROM Class C WHERE C.CustomerGUID = _CustomerGUID AND C.ClassName = _ClassName;

    SELECT CONCAT(COUNT(*), 0)
    INTO _CountOfCharNamesFound
    FROM Characters C
    WHERE C.CustomerGUID = _CustomerGUID
      AND C.CharName = _CharacterName;

    SET _CharacterName = TRIM(BOTH FROM _CharacterName);
    SET _CharacterName = REPLACE(REPLACE(REPLACE(_CharacterName, ' ', '<>'), '><', ''), '<>', ' ');
    SELECT _CharacterName REGEXP '[^a-zA-Z0-9 ]' INTO _InvalidCharacters;

    IF _InvalidCharacters > 0 AND _SupportUnicode = FALSE THEN
        INSERT INTO tmp_AddCharacter
        VALUES ('Character Name can only contain letters, numbers, and spaces', '', '', 0, '', 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        SET _ErrorRaised = TRUE;
    END IF;

    IF _ErrorRaised = FALSE AND _UserGUID IS NULL THEN
        INSERT INTO tmp_AddCharacter
        VALUES ('Invalid User Session', '', '', 0, '', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        SET _ErrorRaised = TRUE;
    END IF;

    IF _ErrorRaised = FALSE AND _ClassID IS NULL THEN
        INSERT INTO tmp_AddCharacter
        VALUES ('Invalid Class Name', '', '', 0, '', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        SET _ErrorRaised = TRUE;
    END IF;

    IF _ErrorRaised = FALSE AND _CountOfCharNamesFound > 0 THEN
        INSERT INTO tmp_AddCharacter
        VALUES ('Invalid Character Name', '', '', 0, '', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        SET _ErrorRaised = TRUE;
    END IF;

    IF _ErrorRaised = FALSE THEN

        SELECT CONCAT(COUNT(*), 0)
        INTO _CountOfClassInventory
        FROM ClassInventory CI
        WHERE CI.CustomerGUID = _CustomerGUID
          AND CI.ClassID = _ClassID;

        INSERT INTO Characters (CustomerGUID, ClassID, UserGUID, Email, CharName, MapName, X, Y, Z, Perception,
                                Acrobatics, Climb, Stealth, ServerIP, LastActivity, RX, RY, RZ, Spirit, Magic,
                                TeamNumber, Thirst, Hunger, Gold, Score, CharacterLevel, Gender, XP, HitDie, Wounds,
                                Size, weight, MaxHealth, Health, HealthRegenRate, MaxMana, Mana, ManaRegenRate,
                                MaxEnergy, Energy, EnergyRegenRate, MaxFatigue, Fatigue, FatigueRegenRate, MaxStamina,
                                Stamina, StaminaRegenRate, MaxEndurance, Endurance, EnduranceRegenRate, Strength,
                                Dexterity, Constitution, Intellect, Wisdom, Charisma, Agility, Fortitude, Reflex,
                                Willpower, BaseAttack, BaseAttackBonus, AttackPower, AttackSpeed, CritChance,
                                CritMultiplier, Haste, SpellPower, SpellPenetration, Defense, Dodge, Parry, Avoidance,
                                Versatility, Multishot, Initiative, NaturalArmor, PhysicalArmor, BonusArmor, ForceArmor,
                                MagicArmor, Resistance, ReloadSpeed, `Range`, Speed, Silver, Copper, FreeCurrency,
                                PremiumCurrency, Fame, Alignment, Description)
        SELECT _CustomerGUID,
               _ClassID,
               _UserGUID,
               '',
               _CharacterName,
               CL.StartingMapName,
               CL.X,
               CL.Y,
               CL.Z,
               CL.Perception,
               CL.Acrobatics,
               CL.Climb,
               CL.Stealth,
               '',
               NOW(),
               CL.RX,
               CL.RY,
               CL.RZ,
               CL.Spirit,
               CL.Magic,
               CL.TeamNumber,
               CL.Thirst,
               CL.Hunger,
               CL.Gold,
               CL.Score,
               CL.CharacterLevel,
               CL.Gender,
               CL.XP,
               CL.HitDie,
               CL.Wounds,
               CL.Size,
               CL.Weight,
               CL.MaxHealth,
               CL.Health,
               CL.HealthRegenRate,
               CL.MaxMana,
               CL.Mana,
               CL.ManaRegenRate,
               CL.MaxEnergy,
               CL.Energy,
               CL.EnergyRegenRate,
               CL.MaxFatigue,
               CL.Fatigue,
               CL.FatigueRegenRate,
               CL.MaxStamina,
               CL.Stamina,
               CL.StaminaRegenRate,
               CL.MaxEndurance,
               CL.Endurance,
               CL.EnduranceRegenRate,
               CL.Strength,
               CL.Dexterity,
               CL.Constitution,
               CL.Intellect,
               CL.Wisdom,
               CL.Charisma,
               CL.Agility,
               CL.Fortitude,
               CL.Reflex,
               CL.Willpower,
               CL.BaseAttack,
               CL.BaseAttackBonus,
               CL.AttackPower,
               CL.AttackSpeed,
               CL.CritChance,
               CL.CritMultiplier,
               CL.Haste,
               CL.SpellPower,
               CL.SpellPenetration,
               CL.Defense,
               CL.Dodge,
               CL.Parry,
               CL.Avoidance,
               CL.Versatility,
               CL.Multishot,
               CL.Initiative,
               CL.NaturalArmor,
               CL.PhysicalArmor,
               CL.BonusArmor,
               CL.ForceArmor,
               CL.MagicArmor,
               CL.Resistance,
               CL.ReloadSpeed,
               CL.Range,
               CL.Speed,
               CL.Silver,
               CL.Copper,
               CL.FreeCurrency,
               CL.PremiumCurrency,
               CL.Fame,
               CL.Alignment,
               CL.Description
        FROM Class CL
        WHERE CL.ClassID = _ClassID
          AND CL.CustomerGUID = _CustomerGUID;

        SET _CharacterID = LAST_INSERT_ID();

        INSERT INTO tmp_AddCharacter (CharacterName, ClassName, CharacterLevel, StartingMapName, X, Y, Z, RX, RY, RZ,
                                      TeamNumber, Gold, Silver, Copper, FreeCurrency, PremiumCurrency, Fame, Alignment,
                                      Score,
                                      Gender, XP, Size, Weight)
        SELECT _CharacterName,
               _ClassName,
               C.CharacterLevel,
               C.MapName,
               C.X,
               C.Y,
               C.Z,
               C.RX,
               C.RY,
               C.RZ,
               C.TeamNumber,
               C.Gold,
               C.Silver,
               C.Copper,
               C.FreeCurrency,
               C.PremiumCurrency,
               C.Fame,
               C.Alignment,
               C.Score,
               C.Gender,
               C.XP,
               C.Size,
               C.Weight
        FROM Characters C
        WHERE C.CharacterID = _CharacterID;

        IF _CountOfClassInventory < 1 THEN
            INSERT INTO CharInventory (CustomerGUID, CharacterID, InventoryName, InventorySize, InventoryWidth,
                                       InventoryHeight)
            VALUES (_CustomerGUID, _CharacterID, 'Bag', 16, 4, 4);
        ELSE
            INSERT INTO CharInventory (CustomerGUID, CharacterID, InventoryName, InventorySize, InventoryWidth,
                                       InventoryHeight)
            SELECT _CustomerGUID,
                   _CharacterID,
                   CI.InventoryName,
                   CI.InventorySize,
                   CI.InventoryWidth,
                   CI.InventoryHeight
            FROM ClassInventory CI
            WHERE CI.CustomerGUID = _CustomerGUID
              AND CI.ClassID = _ClassID;
        END IF;
    END IF;

    SELECT * FROM tmp_AddCharacter;
    DROP TEMPORARY TABLE IF EXISTS tmp_AddCharacter;
END;

CREATE OR REPLACE PROCEDURE AddCharacterToMapInstanceByCharName(_CustomerGUID CHAR(36),
                                                                _CharacterName VARCHAR(50),
                                                                _MapInstanceID INT)
BEGIN
    DECLARE _CharacterID INT;
    DECLARE _ZoneName VARCHAR(50);

    INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
    VALUES (NOW(), 'AddCharacterToMapInstanceByCharName: Started', _CustomerGUID);

    SELECT CharacterID
    INTO _CharacterID
    FROM Characters C
    WHERE C.CharName = _CharacterName
      AND C.CustomerGUID = _CustomerGUID;


    IF (COALESCE(_CharacterID, 0) > 0) THEN

        DELETE FROM CharOnMapInstance WHERE CharacterID = _CharacterID AND CustomerGUID = _CustomerGUID;

        INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
        VALUES (NOW(), CONCAT('AddCharacterToMapInstanceByCharName: ', CAST(_CustomerGUID AS CHAR), ' - ',
                              CAST(_MapInstanceID AS CHAR), ' - ', CAST(_CharacterID AS CHAR)), _CustomerGUID);

        INSERT INTO CharOnMapInstance (CustomerGUID, MapInstanceID, CharacterID)
        VALUES (_CustomerGUID, _MapInstanceID, _CharacterID);

        SELECT ZoneName
        INTO _ZoneName
        FROM Maps M
                 INNER JOIN MapInstances MI ON MI.CustomerGUID = M.CustomerGUID AND MI.MapID = M.MapID
        WHERE MI.MapInstanceID = _MapInstanceID;

        UPDATE Characters SET MapName = _ZoneName WHERE CharacterID = _CharacterID AND CustomerGUID = _CustomerGUID;
    END IF;
END;

CREATE OR REPLACE PROCEDURE AddOrUpdateCustomCharData(_CustomerGUID CHAR(36),
                                                      _CharacterName VARCHAR(50),
                                                      _CustomFieldName VARCHAR(50),
                                                      _FieldValue TEXT)
BEGIN
    DECLARE _CharacterID INT;

    SELECT CharacterID
    INTO _CharacterID
    FROM Characters C
    WHERE C.CharName = _CharacterName
      AND C.CustomerGUID = _CustomerGUID;

    INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
    VALUES (NOW(), CONCAT('AddOrUpdateCustomCharData: ', CONCAT(_CustomFieldName, 'Empty Field Name'), ': ',
                          CONCAT(_CharacterName, 'Empty CharName')), _CustomerGUID);

    IF _CharacterID > 0 THEN
        -- Update if exists
        UPDATE CustomCharacterData
        SET FieldValue=_FieldValue
        WHERE CustomerGUID = _CustomerGUID
          AND CustomFieldName = _CustomFieldName
          AND CharacterID = _CharacterID;

        -- Insert if not exists
        INSERT INTO CustomCharacterData (CustomerGUID, CharacterID, CustomFieldName, FieldValue)
        SELECT _CustomerGUID, _CharacterID, _CustomFieldName, _FieldValue
        WHERE NOT EXISTS(SELECT CustomCharacterDataID
                         FROM CustomCharacterData
                         WHERE CustomerGUID = _CustomerGUID
                           AND CharacterID = _CharacterID
                           AND CustomFieldName = _CustomFieldName);

        INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
        VALUES (NOW(), CONCAT('AddOrUpdateCustomCharData: ', CAST(LENGTH(_FieldValue) AS CHAR)), _CustomerGUID);

    END IF;
END;

CREATE OR REPLACE PROCEDURE AddOrUpdateMapZone(_CustomerGUID CHAR(36),
                                               _MapID INT,
                                               _MapName VARCHAR(50),
                                               _MapData LONGBLOB,
                                               _ZoneName VARCHAR(50),
                                               _WorldCompContainsFilter VARCHAR(100),
                                               _WorldCompListFilter VARCHAR(200),
                                               _SoftPlayerCap INT,
                                               _HardPlayerCap INT,
                                               _MapMode INT)
BEGIN

    IF (_WorldCompContainsFilter IS NULL) THEN
        SET _WorldCompContainsFilter = '';
    END IF;
    IF (_WorldCompListFilter IS NULL) THEN
        SET _WorldCompListFilter = '';
    END IF;

    UPDATE Maps
    SET CustomerGUID=_CustomerGUID,
        MapName=_MapName,
        MapData=_MapData,
        ZoneName=_ZoneName,
        WorldCompContainsFilter=_WorldCompContainsFilter,
        WorldCompListFilter=_WorldCompListFilter,
        SoftPlayerCap = _SoftPlayerCap,
        HardPlayerCap = _HardPlayerCap,
        MapMode = _MapMode
    WHERE CustomerGUID = _CustomerGUID
      AND MapID = _MapID;

    INSERT INTO Maps (CustomerGUID, MapName, MapData, Width, Height, ZoneName, WorldCompContainsFilter,
                      WorldCompListFilter, SoftPlayerCap, HardPlayerCap, MapMode)
    SELECT _CustomerGUID,
           _MapName,
           _MapData,
           0,
           0,
           _ZoneName,
           _WorldCompContainsFilter,
           _WorldCompListFilter,
           _SoftPlayerCap,
           _HardPlayerCap,
           _MapMode
    WHERE NOT EXISTS(SELECT 1
                     FROM Maps
                     WHERE CustomerGUID = _CustomerGUID
                       AND (MapID = _MapID OR ZoneName = _ZoneName));
END;

CREATE OR REPLACE FUNCTION AddUser(_CustomerGUID CHAR(36),
                                   _FirstName VARCHAR(50),
                                   _LastName VARCHAR(50),
                                   _Email VARCHAR(256),
                                   _Password VARCHAR(256),
                                   _Role VARCHAR(10))
    RETURNS CHAR(36)
    DETERMINISTIC
    MODIFIES SQL DATA
BEGIN
    DECLARE _PasswordHash VARCHAR(128);
    DECLARE _Salt VARCHAR(50);
    DECLARE _UserGUID CHAR(36);

    SET _Salt = SUBSTRING(MD5(RAND()), -10);
    SET _PasswordHash = ENCRYPT(_Password, _Salt);
    SET _UserGUID = UUID();

    INSERT INTO Users (UserGUID, CustomerGUID, FirstName, LastName, Email, PasswordHash, Salt, CreateDate, LastAccess,
                       ROLE)
    VALUES (_UserGUID, _CustomerGUID, _FirstName, _LastName, _Email, _PasswordHash, _Salt, NOW(), NOW(), _Role);

    RETURN _UserGUID;
END;

CREATE OR REPLACE PROCEDURE AddNewCustomer(_CustomerName VARCHAR(50),
                                           _FirstName VARCHAR(50),
                                           _LastName VARCHAR(50),
                                           _Email VARCHAR(256),
                                           _Password VARCHAR(256))
BEGIN
    DECLARE _CustomerGUID, _UserGUID CHAR(36);
    DECLARE _CharacterID, _ClassID INT;
    DECLARE _CharacterName VARCHAR(50);

    SET _CharacterName = 'Test';
    SET _CustomerGUID = UUID();

    INSERT INTO Customers (CustomerGUID, CustomerName, CustomerEmail, CustomerPhone, CustomerNotes, EnableDebugLogging)
    VALUES (_CustomerGUID, _CustomerName, _Email, '', '', TRUE);

    INSERT INTO WorldSettings (CustomerGUID, StartTime)
    SELECT _CustomerGUID, CAST(UNIX_TIMESTAMP() AS INTEGER)
    FROM Customers C
    WHERE C.CustomerGUID = _CustomerGUID;

    SET _UserGUID = AddUser(_CustomerGUID, _FirstName, _LastName, _Email, _Password, 'Developer');

    INSERT INTO Maps (CustomerGUID, MapName, ZoneName, MapData, Width, Height)
    VALUES (_CustomerGUID, 'ThirdPersonExampleMap', 'ThirdPersonExampleMap', NULL, 1, 1);
    INSERT INTO Maps (CustomerGUID, MapName, ZoneName, MapData, Width, Height)
    VALUES (_CustomerGUID, 'Map2', 'Map2', NULL, 1, 1);
    INSERT INTO Maps (CustomerGUID, MapName, ZoneName, MapData, Width, Height)
    VALUES (_CustomerGUID, 'DungeonMap', 'DungeonMap', NULL, 1, 1);
    INSERT INTO Maps (CustomerGUID, MapName, ZoneName, MapData, Width, Height)
    VALUES (_CustomerGUID, 'FourZoneMap', 'Zone1', NULL, 1, 1);
    INSERT INTO Maps (CustomerGUID, MapName, ZoneName, MapData, Width, Height)
    VALUES (_CustomerGUID, 'FourZoneMap', 'Zone2', NULL, 1, 1);

    INSERT INTO Class (CustomerGUID, ClassName, StartingMapName, X, Y, Z, Perception, Acrobatics, Climb, Stealth, RX,
                       RY, RZ, Spirit, Magic, TeamNumber, Thirst, Hunger, Gold, Score, CharacterLevel, Gender, XP,
                       HitDie, Wounds, Size, weight, MaxHealth, Health, HealthRegenRate, MaxMana, Mana, ManaRegenRate,
                       MaxEnergy, Energy, EnergyRegenRate, MaxFatigue, Fatigue, FatigueRegenRate, MaxStamina, Stamina,
                       StaminaRegenRate, MaxEndurance, Endurance, EnduranceRegenRate, Strength, Dexterity, Constitution,
                       Intellect, Wisdom, Charisma, Agility, Fortitude, Reflex, Willpower, BaseAttack, BaseAttackBonus,
                       AttackPower, AttackSpeed, CritChance, CritMultiplier, Haste, SpellPower, SpellPenetration,
                       Defense, Dodge, Parry, Avoidance, Versatility, Multishot, Initiative, NaturalArmor,
                       PhysicalArmor, BonusArmor, ForceArmor, MagicArmor, Resistance, ReloadSpeed, `Range`, Speed,
                       Silver,
                       Copper, FreeCurrency, PremiumCurrency, Fame, ALIGNMENT, Description)
    VALUES (_CustomerGUID, 'MaleWarrior', 'ThirdPersonExampleMap', 0, 0, 250, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0,
            1, 1, 0, 10, 0, 1, 0, 100, 50, 1, 100, 0, 1, 100, 0, 5, 100, 0, 1, 0, 0, 0, 0, 0, 0, 10, 10, 10, 10, 10, 10,
            0, 1, 1, 1, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            '');

    SET _ClassID = LAST_INSERT_ID();

    INSERT INTO Characters (CustomerGUID, ClassID, UserGUID, Email, CharName, MapName, X, Y, Z, Perception, Acrobatics,
                            Climb, Stealth, ServerIP, LastActivity,
                            RX, RY, RZ, Spirit, Magic, TeamNumber, Thirst, Hunger, Gold, Score, CharacterLevel, Gender,
                            XP, HitDie, Wounds, Size, weight, MaxHealth, Health,
                            HealthRegenRate, MaxMana, Mana, ManaRegenRate, MaxEnergy, Energy, EnergyRegenRate,
                            MaxFatigue, Fatigue, FatigueRegenRate, MaxStamina, Stamina,
                            StaminaRegenRate, MaxEndurance, Endurance, EnduranceRegenRate, Strength, Dexterity,
                            Constitution, Intellect, Wisdom, Charisma, Agility, Fortitude,
                            Reflex, Willpower, BaseAttack, BaseAttackBonus, AttackPower, AttackSpeed, CritChance,
                            CritMultiplier, Haste, SpellPower, SpellPenetration, Defense,
                            Dodge, Parry, Avoidance, Versatility, Multishot, Initiative, NaturalArmor, PhysicalArmor,
                            BonusArmor, ForceArmor, MagicArmor, Resistance, ReloadSpeed,
                            `Range`, Speed, Silver, Copper, FreeCurrency, PremiumCurrency, Fame, ALIGNMENT, Description)
    SELECT _CustomerGUID,
           _ClassID,
           _UserGUID,
           '',
           _CharacterName,
           StartingMapName,
           X,
           Y,
           Z,
           Perception,
           Acrobatics,
           Climb,
           Stealth,
           '',
           NOW(),
           RX,
           RY,
           RZ,
           Spirit,
           Magic,
           TeamNumber,
           Thirst,
           Hunger,
           Gold,
           Score,
           CharacterLevel,
           Gender,
           XP,
           HitDie,
           Wounds,
           Size,
           weight,
           MaxHealth,
           Health,
           HealthRegenRate,
           MaxMana,
           Mana,
           ManaRegenRate,
           MaxEnergy,
           Energy,
           EnergyRegenRate,
           MaxFatigue,
           Fatigue,
           FatigueRegenRate,
           MaxStamina,
           Stamina,
           StaminaRegenRate,
           MaxEndurance,
           Endurance,
           EnduranceRegenRate,
           Strength,
           Dexterity,
           Constitution,
           Intellect,
           Wisdom,
           Charisma,
           Agility,
           Fortitude,
           Reflex,
           Willpower,
           BaseAttack,
           BaseAttackBonus,
           AttackPower,
           AttackSpeed,
           CritChance,
           CritMultiplier,
           Haste,
           SpellPower,
           SpellPenetration,
           Defense,
           Dodge,
           Parry,
           Avoidance,
           Versatility,
           Multishot,
           Initiative,
           NaturalArmor,
           PhysicalArmor,
           BonusArmor,
           ForceArmor,
           MagicArmor,
           Resistance,
           ReloadSpeed,
           `Range`,
           Speed,
           Silver,
           Copper,
           FreeCurrency,
           PremiumCurrency,
           Fame,
           ALIGNMENT,
           DESCRIPTION
    FROM Class
    WHERE ClassID = _ClassID;

    SET _CharacterID = LAST_INSERT_ID();

    INSERT INTO CharInventory (CustomerGUID, CharacterID, InventoryName, InventorySize)
    VALUES (_CustomerGUID, _CharacterID, 'Bag', 16);
END;


CREATE OR REPLACE PROCEDURE CheckMapInstanceStatus(_CustomerGUID CHAR(36),
                                                   _MapInstanceID INT)
BEGIN
    SELECT Status
    FROM MapInstances MI
    WHERE MI.MapInstanceID = _MapInstanceID
      AND MI.CustomerGUID = _CustomerGUID;
END;

CREATE OR REPLACE PROCEDURE CleanUp(_CustomerGUID CHAR(36))
BEGIN
    DECLARE _CharacterID INT;
    DECLARE _ZoneName VARCHAR(50);

    DELETE
    FROM CharOnMapInstance
    WHERE CustomerGUID = _CustomerGUID
      AND CharacterID IN (SELECT C.CharacterID
                          FROM Characters C
                                   INNER JOIN Users U
                                              ON U.CustomerGUID = C.CustomerGUID
                                                  AND U.UserGUID = C.UserGUID
                          WHERE U.LastAccess < DATE_SUB(NOW(), INTERVAL 1 MINUTE)
                            AND C.CustomerGUID = _CustomerGUID);

    DROP TEMPORARY TABLE IF EXISTS tmp_CleanUp;
    CREATE TEMPORARY TABLE tmp_CleanUp
    (
        MapInstanceID INT
    ) ENGINE = MEMORY;

    INSERT INTO tmp_CleanUp (MapInstanceID)
    SELECT MapInstanceID
    FROM MapInstances
    WHERE LastUpdateFromServer < DATE_SUB(NOW(), INTERVAL 2 MINUTE)
      AND CustomerGUID = _CustomerGUID;

    DELETE
    FROM CharOnMapInstance
    WHERE CustomerGUID = _CustomerGUID
      AND MapInstanceID IN (SELECT MapInstanceID FROM tmp_CleanUp);

    DELETE
    FROM MapInstances
    WHERE CustomerGUID = _CustomerGUID
      AND MapInstanceID IN (SELECT MapInstanceID FROM tmp_CleanUp);

    DROP TEMPORARY TABLE IF EXISTS tmp_CleanUp;

END;

CREATE OR REPLACE PROCEDURE GetAbilityBars(_CustomerGUID CHAR(36),
                                           _CharName VARCHAR(50))
BEGIN
    SELECT CAB.AbilityBarName,
           CAB.CharAbilityBarID,
           CONCAT(CAB.CharAbilityBarsCustomJSON, '') AS CharAbilityBarsCustomJSON,
           CAB.CharacterID,
           CAB.CustomerGUID,
           CAB.MaxNumberOfSlots,
           CAB.NumberOfUnlockedSlots
    FROM CharAbilityBars CAB
             INNER JOIN Characters C
                        ON C.CharacterID = CAB.CharacterID
                            AND C.CustomerGUID = CAB.CustomerGUID
    WHERE C.CustomerGUID = _CustomerGUID
      AND C.CharName = _CharName;
END;

CREATE OR REPLACE PROCEDURE GetAbilityBarsAndAbilities(_CustomerGUID CHAR(36),
                                                       _CharName VARCHAR(50))
BEGIN
    SELECT CAB.AbilityBarName,
           CAB.CharAbilityBarID,
           CONCAT(CAB.CharAbilityBarsCustomJSON, '')  AS CharAbilityBarsCustomJSON,
           CAB.CharacterID,
           CAB.CustomerGUID,
           CAB.MaxNumberOfSlots,
           CAB.NumberOfUnlockedSlots,
           CHA.AbilityLevel,
           CONCAT(CHA.CharHasAbilitiesCustomJSON, '') AS CharHasAbilitiesCustomJSON,
           AB.AbilityID,
           AB.AbilityName,
           AB.AbilityTypeID,
           AB.Class,
           AB.Race,
           AB.TextureToUseForIcon,
           AB.GameplayAbilityClassName,
           AB.AbilityCustomJSON,
           CABA.InSlotNumber
    FROM CharAbilityBars CAB
             INNER JOIN CharAbilityBarAbilities CABA
                        ON CABA.CharAbilityBarID = CAB.CharAbilityBarID
                            AND CABA.CustomerGUID = CAB.CustomerGUID
             INNER JOIN CharHasAbilities CHA
                        ON CHA.CharHasAbilitiesID = CABA.CharHasAbilitiesID
                            AND CHA.CustomerGUID = CABA.CustomerGUID
             INNER JOIN Abilities AB
                        ON AB.AbilityID = CHA.AbilityID
                            AND AB.CustomerGUID = CHA.CustomerGUID
             INNER JOIN Characters C
                        ON C.CharacterID = CAB.CharacterID
                            AND C.CustomerGUID = CAB.CustomerGUID
    WHERE C.CustomerGUID = _CustomerGUID
      AND C.CharName = _CharName;
END;

CREATE OR REPLACE PROCEDURE GetAllCharacters(_CustomerGUID CHAR(36),
                                             _UserSessionGUID CHAR(36))
BEGIN
    SELECT C.*,
           DATE_FORMAT('%b %d %Y %h:%i%p', C.LastActivity) AS LastActivityString,
           DATE_FORMAT('%b %d %Y %h:%i%p', C.CreateDate)   AS CreateDateString,
           CL.ClassName
    FROM Characters C
             INNER JOIN Class CL
                        ON CL.ClassID = C.ClassID
             INNER JOIN Users U
                        ON U.UserGUID = C.UserGUID
             INNER JOIN UserSessions US
                        ON US.UserGUID = U.UserGUID
    WHERE C.CustomerGUID = _CustomerGUID
      AND US.UserSessionGUID = _UserSessionGUID;
END;

CREATE OR REPLACE PROCEDURE GetCharacterAbilities(_CustomerGUID CHAR(36),
                                                  _CharName VARCHAR(50))
BEGIN
    SELECT A.AbilityID,
           A.AbilityCustomJSON,
           A.AbilityName,
           A.AbilityTypeID,
           A.Class,
           A.CustomerGUID,
           A.Race,
           A.TextureToUseForIcon,
           A.GameplayAbilityClassName,
           CHA.CharHasAbilitiesID,
           CHA.AbilityLevel,
           CHA.CharHasAbilitiesCustomJSON,
           C.CharacterID,
           C.CharName
    FROM CharHasAbilities CHA
             INNER JOIN Abilities A
                        ON A.AbilityID = CHA.AbilityID
                            AND A.CustomerGUID = CHA.CustomerGUID
             INNER JOIN Characters C
                        ON C.CharacterID = CHA.CharacterID
                            AND C.CustomerGUID = CHA.CustomerGUID
    WHERE C.CustomerGUID = _CustomerGUID
      AND C.CharName = _CharName;
END;

CREATE OR REPLACE PROCEDURE GetCharByCharName(_CustomerGUID CHAR(36),
                                              _CharName VARCHAR(50))
BEGIN
    SELECT C.*, MI.Port, WS.ServerIP, CMI.MapInstanceID, CL.ClassName, CU.EnableAutoLoopBack
    FROM Characters C
             INNER JOIN Class CL
                        ON CL.ClassID = C.ClassID
             INNER JOIN Customers CU
                        ON CU.CustomerGUID = C.CustomerGUID
             LEFT JOIN CharOnMapInstance CMI
                       ON CMI.CharacterID = C.CharacterID
             LEFT JOIN MapInstances MI
                       ON MI.MapInstanceID = CMI.MapInstanceID
             LEFT JOIN WorldServers WS
                       ON WS.WorldServerID = MI.WorldServerID
    WHERE C.CustomerGUID = _CustomerGUID
      AND C.CharName = _CharName
    ORDER BY MI.MapInstanceID DESC
    LIMIT 1;
END;

CREATE OR REPLACE PROCEDURE GetCustomCharData(_CustomerGUID CHAR(36),
                                              _CharName VARCHAR(50))
BEGIN
    SELECT CCD.*
    FROM Characters C
             INNER JOIN CustomCharacterData CCD
                        ON CCD.CharacterID = C.CharacterID
                            AND CCD.CustomerGUID = C.CustomerGUID
    WHERE C.CustomerGUID = _CustomerGUID
      AND C.CharName = _CharName;
END;

CREATE OR REPLACE PROCEDURE GetMapInstancesForWorldServerID(_CustomerGUID CHAR(36),
                                                            _WorldServerID INT)
BEGIN
    SELECT MI.*,
           M.SoftPlayerCap,
           M.HardPlayerCap,
           M.MapName,
           M.MapMode,
           M.MinutesToShutdownAfterEmpty,
           FLOOR(TIMESTAMPDIFF(MINUTE, NOW(), MI.LastServerEmptyDate))  AS MinutesServerHasBeenEmpty,
           FLOOR(TIMESTAMPDIFF(MINUTE, NOW(), MI.LastUpdateFromServer)) AS MinutesSinceLastUpdate
    FROM Maps M
             INNER JOIN MapInstances MI
                        ON MI.MapID = M.MapID
    WHERE M.CustomerGUID = _CustomerGUID
      AND MI.WorldServerID = _WorldServerID;
END;

CREATE OR REPLACE PROCEDURE GetPlayerGroupsCharacterIsIn(_CustomerGUID CHAR(36),
                                                         _CharName VARCHAR(50),
                                                         _UserSessionGUID CHAR(36),
                                                         _PlayerGroupTypeID INT)
BEGIN
    SELECT PG.PlayerGroupID,
           PG.CustomerGUID,
           PG.PlayerGroupName,
           PG.PlayerGroupTypeID,
           PG.ReadyState,
           PG.CreateDate,
           PGC.DateAdded,
           PGC.TeamNumber
    FROM PlayerGroupCharacters PGC
             INNER JOIN PlayerGroup PG
                        ON PG.PlayerGroupID = PGC.PlayerGroupID
                            AND PG.CustomerGUID = PGC.CustomerGUID
             INNER JOIN Characters C
                        ON C.CharacterID = PGC.CharacterID
             INNER JOIN UserSessions US
                        ON US.UserGUID = C.UserGUID
                            AND US.CustomerGUID = C.CustomerGUID
    WHERE PGC.CustomerGUID = _CustomerGUID
      AND (PG.PlayerGroupTypeID = _PlayerGroupTypeID OR COALESCE(_PlayerGroupTypeID, 0) = 0)
      AND C.CharName = _CharName
      AND C.CustomerGUID = _CustomerGUID;
END;

CREATE OR REPLACE PROCEDURE GetServerInstanceFromIPandPort(_CustomerGUID CHAR(36),
                                                           _ServerIP VARCHAR(50),
                                                           _Port INT)
BEGIN
    SELECT M.MapName,
           M.ZoneName,
           M.WorldCompContainsFilter,
           M.WorldCompListFilter,
           MI.MapInstanceID,
           MI.Status,
           WS.MaxNumberOfInstances,
           WS.ActiveStartTime,
           WS.ServerStatus,
           WS.InternalServerIP
    FROM MapInstances MI
             INNER JOIN Maps M
                        ON M.MapID = MI.MapID
                            AND M.CustomerGUID = MI.CustomerGUID
             INNER JOIN WorldServers WS
                        ON WS.WorldServerID = MI.WorldServerID
                            AND WS.CustomerGUID = MI.CustomerGUID
    WHERE WS.CustomerGUID = _CustomerGUID
      AND (WS.ServerIP = _ServerIP
        OR InternalServerIP = _ServerIP)
      AND MI.Port = _Port;
END;

CREATE OR REPLACE PROCEDURE GetUser(_CustomerGUID CHAR(36),
                                    _UserGUID CHAR(36))
BEGIN
    SELECT *
    FROM Users U
    WHERE U.CustomerGUID = _CustomerGUID
      AND U.UserGUID = _UserGUID;
END;

CREATE OR REPLACE PROCEDURE GetUserSession(_CustomerGUID CHAR(36),
                                           _UserSessionGUID CHAR(36))
BEGIN
    SELECT US.CustomerGUID,
           US.UserGUID,
           US.UserSessionGUID,
           US.LoginDate,
           US.SelectedCharacterName,
           U.Email,
           U.FirstName,
           U.LastName,
           U.CreateDate,
           U.LastAccess,
           U.Role,
           C.CharacterID,
           C.CharName,
           C.X,
           C.Y,
           C.Z,
           C.RX,
           C.RY,
           C.RZ,
           C.MapName AS ZoneName
    FROM UserSessions US
             INNER JOIN Users U
                        ON U.UserGUID = US.UserGUID
             LEFT JOIN Characters C
                       ON C.CustomerGUID = US.CustomerGUID
                           AND C.CharName = US.SelectedCharacterName
                           AND C.UserGUID = US.UserGUID
    WHERE US.CustomerGUID = _CustomerGUID
      AND US.UserSessionGUID = _UserSessionGUID;
END;

CREATE OR REPLACE PROCEDURE GetWorldStartTime(_CustomerGUID CHAR(36))
BEGIN
    SELECT UNIX_TIMESTAMP(NOW()) - WS.StartTime
               AS CurrentWorldTime
    FROM WorldSettings WS
    WHERE WS.CustomerGUID = _CustomerGUID;
END;

CREATE OR REPLACE PROCEDURE GetZoneInstancesOfZone(_CustomerGUID CHAR(36),
                                                   _ZoneName VARCHAR(50))
BEGIN
    SELECT *
    FROM Maps M
             INNER JOIN MapInstances MI
                        ON MI.MapID = M.MapID
    WHERE M.CustomerGUID = _CustomerGUID
      AND M.ZoneName = _ZoneName;
END;

CREATE OR REPLACE PROCEDURE SpinUpMapInstance(_CustomerGUID CHAR(36),
                                              _ZoneName VARCHAR(50),
                                              _PlayerGroupID INT,
                                              _Nested BOOLEAN)
BEGIN
    DECLARE _ServerIP, _WorldServerIP VARCHAR(50);
    DECLARE _WorldServerID, _WorldServerPort, _Port, _MapInstanceID, _MapID , _MapMode, _MaxNumberOfInstances, _StartingMapInstancePort INT;

    IF _Nested = FALSE THEN
        DROP TEMPORARY TABLE IF EXISTS tmp_SpinUpMapInstance;
        CREATE TEMPORARY TABLE tmp_SpinUpMapInstance
        (
            ServerIP        VARCHAR(50),
            WorldServerID   INT,
            WorldServerIP   VARCHAR(50),
            WorldServerPort INT,
            Port            INT,
            MapInstanceID   INT
        ) ENGINE = MEMORY;
    END IF;

    INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
    VALUES (NOW(), CONCAT('Attempting to Spin Up Map Instance: ', _ZoneName), _CustomerGUID);

    SELECT MapID, MapMode
    INTO _MapID, _MapMode
    FROM Maps M
    WHERE M.ZoneName = _ZoneName
      AND M.CustomerGUID = _CustomerGUID;

    SELECT WS.ServerIP,
           WS.WorldServerID,
           WS.InternalServerIP,
           WS.Port,
           WS.MaxNumberOfInstances,
           WS.StartingMapInstancePort
    INTO
        _ServerIP, _WorldServerID, _WorldServerIP, _WorldServerPort, _MaxNumberOfInstances, _StartingMapInstancePort
    FROM WorldServers WS
             LEFT JOIN MapInstances MI
                       ON MI.WorldServerID = WS.WorldServerID
                           AND MI.CustomerGUID = WS.CustomerGUID
    WHERE WS.CustomerGUID = _CustomerGUID
      AND WS.ServerStatus = 1 -- Active
      AND WS.ActiveStartTime IS NOT NULL
    GROUP BY WS.WorldServerID, WS.ServerIP, WS.InternalServerIP, WS.Port, WS.MaxNumberOfInstances,
             WS.StartingMapInstancePort
    ORDER BY CONCAT(COUNT(MI.MapInstanceID), 0)
    LIMIT 1;

    IF (_WorldServerID IS NULL) THEN
        INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
        VALUES (NOW(), 'Cannot find World Server!', _CustomerGUID);

        SET _MapInstanceID = -1;
    ELSE
        WITH RECURSIVE Num(Pos) AS
                           (SELECT CAST(_StartingMapInstancePort AS INTEGER)
                            UNION ALL
                            SELECT CAST(Pos + 1 AS INTEGER)
                            FROM Num
                            WHERE Pos < _StartingMapInstancePort + CONCAT(_MaxNumberOfInstances, 10))
        SELECT MIN(OpenPort)
        INTO _Port
        FROM (SELECT tmpA.Pos AS OpenPort, MI.Port AS PortInUse
              FROM Num tmpA
                       LEFT JOIN MapInstances MI ON MI.WorldServerID = _WorldServerID
                  AND MI.CustomerGUID = _CustomerGUID
                  AND MI.Port = tmpA.Pos) tmpB
        WHERE tmpB.PortInUse IS NULL;

    END IF;

    IF (_PlayerGroupID > 0 AND _MapMode = 2) -- _MapMode = 2 IS a dungeon instance SERVER
    THEN
        INSERT INTO MapInstances (CustomerGUID, WorldServerID, MapID, Port, Status, PlayerGroupID, LastUpdateFromServer)
        VALUES (_CustomerGUID, _WorldServerID, _MapID, _Port, 1, _PlayerGroupID, NOW());
        -- STATUS 1 = Loading;
    ELSE
        INSERT INTO MapInstances (CustomerGUID, WorldServerID, MapID, Port, Status, LastUpdateFromServer)
        VALUES (_CustomerGUID, _WorldServerID, _MapID, _Port, 1, NOW());
        -- STATUS 1 = Loading;
    END IF;

    SET _MapInstanceID = LAST_INSERT_ID();

    INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
    VALUES (NOW(), CONCAT('Successfully Spun Up Map Instance: ', _ZoneName, ' ServerIP: ', _ServerIP, ' Port: ',
                          CAST(_Port AS CHAR), ' MapInstanceID: ', CAST(_MapInstanceID AS CHAR)), _CustomerGUID);

    INSERT INTO tmp_SpinUpMapInstance (ServerIP, WorldServerID, WorldServerIP, WorldServerPort, Port, MapInstanceID)
    VALUES (_ServerIP, _WorldServerID, _WorldServerIP, _WorldServerPort, _Port, _MapInstanceID);
    IF _Nested = FALSE THEN
        SELECT * FROM tmp_SpinUpMapInstance;
        DROP TEMPORARY TABLE IF EXISTS tmp_SpinUpMapInstance;
    END IF;
END;

CREATE OR REPLACE PROCEDURE JoinMapByCharName(_CustomerGUID CHAR(36),
                                              _CharName VARCHAR(50),
                                              _ZoneName VARCHAR(50),
                                              _PlayerGroupType INT)
BEGIN
    DECLARE _MapID, _CharacterID, _SoftPlayerCap,_PlayerGroupID , _WorldServerID , _WorldServerPort , _Port , _MapInstanceID , _MapInstanceStatus INT;
    DECLARE _MapNameToStart, _ServerIP, _WorldServerIP VARCHAR(50);
    DECLARE _Email VARCHAR(255);
    DECLARE _NeedToStartUpMap ,_EnableAutoLoopBack ,_NoPortForwarding ,_IsInternalNetworkTestUser ,_ErrorRaised BOOLEAN;

    SET _IsInternalNetworkTestUser = FALSE;
    SET _ErrorRaised = FALSE;

    DROP TEMPORARY TABLE IF EXISTS tmp_JoinMapByCharName;
    CREATE TEMPORARY TABLE tmp_JoinMapByCharName
    (
        ServerIP           VARCHAR(50),
        WorldServerID      INT,
        WorldServerIP      VARCHAR(50),
        WorldServerPort    INT,
        Port               INT,
        MapInstanceID      INT,
        MapNameToStart     VARCHAR(50),
        MapInstanceStatus  INT,
        NeedToStartUpMap   BOOLEAN,
        EnableAutoLoopBack BOOLEAN,
        NoPortForwarding   BOOLEAN
    ) ENGINE = MEMORY;

    -- Run Cleanup here for now.  Later this can get moved to a scheduler to run periodically.
    CALL CleanUp(_CustomerGUID);

    INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
    VALUES (NOW(), CONCAT('JoinMapByCharName: ', _ZoneName, ' - ', _CharName), _CustomerGUID);

    SELECT M.MapID, M.MapName, M.SoftPlayerCap
    INTO _MapID, _MapNameToStart, _SoftPlayerCap
    FROM Maps M
    WHERE M.ZoneName = _ZoneName
      AND M.CustomerGUID = _CustomerGUID;

    SELECT C.CharacterID, C.IsInternalNetworkTestUser, C.Email
    INTO _CharacterID, _IsInternalNetworkTestUser, _Email
    FROM Characters C
    WHERE C.CharName = _CharName
      AND C.CustomerGUID = _CustomerGUID;

    IF (_CharacterID IS NULL) THEN
        INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
        VALUES (NOW(), 'JoinMapByCharName: CharacterID is NULL!', _CustomerGUID);

        SET _NeedToStartUpMap = FALSE;
        SET _ErrorRaised = TRUE;
    END IF;

    IF _ErrorRaised = FALSE THEN
        SELECT C.EnableAutoLoopBack, C.NoPortForwarding
        INTO _EnableAutoLoopBack, _NoPortForwarding
        FROM Customers C
        WHERE C.CustomerGUID = _CustomerGUID;
    END IF;

    IF _ErrorRaised = FALSE AND (_PlayerGroupType > 0) THEN
        SELECT COALESCE(PG.PlayerGroupID, 0)
        INTO _PlayerGroupID
        FROM PlayerGroupCharacters PGC
                 INNER JOIN PlayerGroup PG
                            ON PG.PlayerGroupID = PGC.PlayerGroupID
        WHERE PGC.CustomerGUID = _CustomerGUID
          AND PGC.CharacterID = _CharacterID
          AND PG.PlayerGroupTypeID = _PlayerGroupType;
    END IF;

    IF _ErrorRaised = FALSE THEN
        SELECT (CASE
                    WHEN _IsInternalNetworkTestUser = TRUE THEN WS.InternalServerIP
                    ELSE WS.ServerIP END) AS ServerIp,
               WS.InternalServerIP,
               WS.Port                    AS WSPort,
               MI.Port                    AS MIPort,
               MI.MapInstanceID,
               WS.WorldServerID,
               MI.Status
        INTO _ServerIP, _WorldServerIP, _WorldServerPort, _Port, _MapInstanceID, _WorldServerID, _MapInstanceStatus
        FROM WorldServers WS
                 LEFT JOIN MapInstances MI
                           ON MI.WorldServerID = WS.WorldServerID
                               AND MI.CustomerGUID = WS.CustomerGUID
                 LEFT JOIN CharOnMapInstance CMI
                           ON CMI.MapInstanceID = MI.MapInstanceID
                               AND CMI.CustomerGUID = MI.CustomerGUID
        WHERE MI.MapID = _MapID
          AND WS.ActiveStartTime IS NOT NULL
          AND WS.CustomerGUID = _CustomerGUID
          AND MI.NumberOfReportedPlayers < _SoftPlayerCap
          AND (MI.PlayerGroupID = _PlayerGroupID OR COALESCE(_PlayerGroupID,0) = 0) -- Only lookup map instances that MATCH the player group fro this Player group Type OR lookup all if zero
          AND MI.Status = 2
        GROUP BY MI.MapInstanceID, WS.ServerIP, MI.Port, WS.WorldServerID, WS.InternalServerIP, WS.Port, MI.Status
        ORDER BY COUNT(DISTINCT CMI.CharacterID);

        -- There is a map already running to connect to
        IF _MapInstanceID IS NOT NULL THEN
            /*IF (POSITION('\@localhost' IN _Email) > 0) THEN
                _ServerIP = '127.0.0.1';
            END IF;*/

            SET _NeedToStartUpMap = FALSE;

            INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
            VALUES (NOW(), CONCAT('Joined Existing Map: ', COALESCE(_ZoneName, ''), ' - ', COALESCE(_CharName, ''),
                                  ' - ', COALESCE(_ServerIP, '')), _CustomerGUID);
        ELSE -- Spin up a new map

            CREATE TEMPORARY TABLE tmp_SpinUpMapInstance
            (
                ServerIP        VARCHAR(50),
                WorldServerID   INT,
                WorldServerIP   VARCHAR(50),
                WorldServerPort INT,
                Port            INT,
                MapInstanceID   INT
            ) ENGINE = MEMORY;

            CALL SpinUpMapInstance
                (_CustomerGUID, _ZoneName, _PlayerGroupID, TRUE);
            SELECT *
            INTO _ServerIP , _WorldServerID , _WorldServerIP , _WorldServerPort , _Port, _MapInstanceID
            FROM tmp_SpinUpMapInstance;

            DROP TEMPORARY TABLE IF EXISTS tmp_SpinUpMapInstance;
            /*IF (POSITION('@localhost' IN _Email) > 0 OR _IsInternalNetworkTestUser = TRUE) THEN
                _ServerIP = '127.0.0.1';
            END IF;*/

            SET _NeedToStartUpMap = TRUE;

            INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
            VALUES (NOW(),
                    CONCAT('SpinUpMapInstance returned: ', COALESCE(_ZoneName, ''), ' CharName: ',
                           COALESCE(_CharName, ''), ' ServerIP: ',
                           COALESCE(_ServerIP, ''),
                           ' WorldServerPort: ', CAST(COALESCE(_WorldServerPort, -1) AS CHAR)), _CustomerGUID);


            INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
            VALUES (NOW(),
                    CONCAT('JoinMapByCharName returned: ', COALESCE(_MapNameToStart, '[NoMapName]'), ' MapInstanceID: ',
                           CAST(COALESCE(_MapInstanceID, -1) AS CHAR), ' MapInstanceStatus: ',
                           CAST(COALESCE(_MapInstanceStatus, -1) AS CHAR), ' NeedToStartUpMap: ',
                           CAST(_NeedToStartUpMap AS CHAR), ' EnableAutoLoopBack: ',
                           CAST(_EnableAutoLoopBack AS CHAR),
                           ' ServerIP: ', COALESCE(_ServerIP, ''), ' WorldServerIP: ', COALESCE(_WorldServerIP, '')),
                    _CustomerGUID);
        END IF;
    END IF;
    INSERT INTO tmp_JoinMapByCharName(ServerIP, WorldServerID, WorldServerIP, WorldServerPort, Port, MapInstanceID,
                                      MapNameToStart,
                                      MapInstanceStatus, NeedToStartUpMap, EnableAutoLoopBack, NoPortForwarding)
    VALUES (_ServerIP, _WorldServerID, _WorldServerIP, _WorldServerPort, _Port, _MapInstanceID, _MapNameToStart,
            _MapInstanceStatus, _NeedToStartUpMap, _EnableAutoLoopBack, _NoPortForwarding);
    SELECT * FROM tmp_JoinMapByCharName;
END;

CREATE OR REPLACE PROCEDURE PlayerLoginAndCreateSession(_CustomerGUID CHAR(36),
                                                        _Email VARCHAR(256),
                                                        _Password VARCHAR(256),
                                                        _DontCheckPassword BOOLEAN)
BEGIN

    DECLARE _HashInDB, _HashToCheck VARCHAR(128);
    DECLARE _UserGUID, _UserSessionGUID CHAR(36);
    DECLARE _Authenticated ,_PasswordCheck BOOLEAN;

    SET _Authenticated = FALSE;
    SET _PasswordCheck = FALSE;

    DROP TEMPORARY TABLE IF EXISTS tmp_PlayerLoginAndCreateSession;
    CREATE TEMPORARY TABLE tmp_PlayerLoginAndCreateSession
    (
        Authenticated   BOOLEAN,
        UserSessionGUID CHAR(36)
    ) ENGINE = MEMORY;

    SELECT CASE WHEN ENCRYPT(_Password, Salt) = PasswordHash THEN TRUE ELSE FALSE END, UserGUID
    INTO _PasswordCheck, _UserGUID
    FROM Users
    WHERE CustomerGUID = _CustomerGUID
      AND Email = _Email
      AND Role = 'Player';

    IF (_PasswordCheck = TRUE OR _DontCheckPassword = TRUE) THEN
        SET _Authenticated = TRUE;
        DELETE FROM UserSessions WHERE UserGUID = _UserGUID;
        SET _UserSessionGUID = UUID();
        INSERT INTO UserSessions (CustomerGUID, UserSessionGUID, UserGUID, LoginDate)
        VALUES (_CustomerGUID, _UserSessionGUID, _UserGUID, NOW());
    END IF;
    INSERT INTO tmp_PlayerLoginAndCreateSession (Authenticated, UserSessionGUID)
    VALUES (_Authenticated, _UserSessionGUID);

    SELECT * FROM tmp_PlayerLoginAndCreateSession;
    DROP TEMPORARY TABLE IF EXISTS tmp_PlayerLoginAndCreateSession;

END;

CREATE OR REPLACE PROCEDURE PlayerLogOut(_CustomerGUID CHAR(36),
                                         _CharName VARCHAR(50))
BEGIN
    DECLARE _CharacterID INT;

    SELECT CharacterID
    INTO _CharacterID
    FROM Characters WC
    WHERE WC.CharName = _CharName
      AND WC.CustomerGUID = _CustomerGUID;

    INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
    VALUES (NOW(), CONCAT('PlayerLogOut: CharName: ', _CharName), _CustomerGUID);

    DELETE FROM CharOnMapInstance WHERE CharacterID = _CharacterID;
    -- DELETE FROM PlayerGroupCharacters WHERE CharacterID = _CharacterID;

END;

CREATE OR REPLACE PROCEDURE RemoveCharacter(_CustomerGUID CHAR(36),
                                            _UserSessionGUID CHAR(36),
                                            _CharacterName VARCHAR(50))
BEGIN
    DECLARE _CharacterID INT;
    DECLARE _UserGUID CHAR(36);

    SELECT US.UserGUID
    INTO _UserGUID
    FROM UserSessions US
    WHERE US.CustomerGUID = _CustomerGUID
      AND US.UserSessionGUID = _UserSessionGUID;
    IF _UserGUID IS NOT NULL THEN
        SELECT CharacterID
        INTO _CharacterID
        FROM Characters C
        WHERE C.CustomerGUID = _CustomerGUID
          AND C.UserGUID = _UserGUID
          AND C.CharName = _CharacterName;
        DELETE
        FROM CharAbilityBarAbilities
        WHERE CustomerGUID = _CustomerGUID
          AND CharAbilityBarID
            IN (SELECT CharAbilityBarID
                FROM CharAbilityBars
                WHERE CustomerGUID = _CustomerGUID
                  AND CharacterID = _CharacterID);
        DELETE FROM CharAbilityBars WHERE CustomerGUID = _CustomerGUID AND CharacterID = _CharacterID;
        DELETE FROM CharHasAbilities WHERE CustomerGUID = _CustomerGUID AND CharacterID = _CharacterID;
        DELETE FROM CharHasItems WHERE CharacterID = _CharacterID;
        DELETE
        FROM CharInventoryItems
        WHERE CustomerGUID = _CustomerGUID
          AND CharInventoryID
            IN (SELECT CharInventoryID
                FROM CharInventory
                WHERE CustomerGUID = _CustomerGUID
                  AND CharacterID = _CharacterID);
        DELETE FROM CharInventory WHERE CustomerGUID = _CustomerGUID AND CharacterID = _CharacterID;
        DELETE FROM CharOnMapInstance WHERE CustomerGUID = _CustomerGUID AND CharacterID = _CharacterID;
        DELETE FROM ChatGroupUsers WHERE CustomerGUID = _CustomerGUID AND CharacterID = _CharacterID;
        DELETE FROM CustomCharacterData WHERE CustomerGUID = _CustomerGUID AND CharacterID = _CharacterID;
        DELETE FROM PlayerGroupCharacters WHERE CustomerGUID = _CustomerGUID AND CharacterID = _CharacterID;
        DELETE FROM Characters WHERE CustomerGUID = _CustomerGUID AND CharacterID = _CharacterID;
    END IF;

END;

CREATE OR REPLACE PROCEDURE SetMapInstanceStatus(_MapInstanceID INT,
                                                 _MapInstanceStatus INT)
BEGIN
    DECLARE _MapName VARCHAR(50);
    DECLARE _CustomerGUID CHAR(36);

    SELECT MI.CustomerGUID,
           M.MapName
    INTO _CustomerGUID, _MapName
    FROM MapInstances MI
             INNER JOIN Maps M
                        ON M.MapID = MI.MapID
    WHERE MapInstanceID = _MapInstanceID;

    UPDATE MapInstances
    SET Status = _MapInstanceStatus
    WHERE MapInstanceID = _MapInstanceID;

    INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
    VALUES (NOW(), CONCAT('SetMapInstanceStatus: ', _MapName), _CustomerGUID);

END;

CREATE OR REPLACE PROCEDURE ShutdownMapInstance(_CustomerGUID CHAR(36),
                                                _MapInstanceID INT)
BEGIN
    DELETE
    FROM CharOnMapInstance
    WHERE MapInstanceID = _MapInstanceID
      AND CustomerGUID = _CustomerGUID;

    DELETE
    FROM MapInstances
    WHERE MapInstanceID = _MapInstanceID
      AND CustomerGUID = _CustomerGUID;

END;

CREATE OR REPLACE PROCEDURE ShutdownWorldServer(_CustomerGUID CHAR(36),
                                                _WorldServerID INT)
BEGIN
    DELETE
    FROM CharOnMapInstance
    WHERE CustomerGUID = _CustomerGUID
      AND MapInstanceID IN (SELECT MapInstanceID FROM MapInstances MI WHERE MI.WorldServerID = _WorldServerID);

    DELETE FROM MapInstances WHERE WorldServerID = _WorldServerID;

    UPDATE WorldServers
    SET ActiveStartTime=NULL,
        ServerStatus=0
    WHERE CustomerGUID = _CustomerGUID
      AND WorldServerID = _WorldServerID;

    INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
    VALUES (NOW(), CONCAT('ShutdownWorldServer: ', CAST(_WorldServerID AS CHAR)), _CustomerGUID);
END;

CREATE OR REPLACE FUNCTION StartWorldServer(_CustomerGUID CHAR(36),
                                            _ServerIP VARCHAR(50))
    RETURNS INT
    DETERMINISTIC
    MODIFIES SQL DATA
BEGIN
    DECLARE _WorldServerID INT;

    SELECT WS.WorldServerID
    INTO _WorldServerID
    FROM WorldServers WS
    WHERE WS.CustomerGUID = _CustomerGUID
      AND (WS.ServerIP = _ServerIP OR WS.InternalServerIP = _ServerIP);

    IF (_WorldServerID > 0) THEN
        UPDATE WorldServers WS
        SET ActiveStartTime=NOW(),
            ServerStatus=1
        WHERE CustomerGUID = _CustomerGUID
          AND WS.WorldServerID = _WorldServerID;

        INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
        VALUES (NOW(), CONCAT('StartWorldServer Success: ', CAST(_WorldServerID AS CHAR)), _CustomerGUID);
    ELSEIF (_ServerIP = '::1') THEN
        SELECT WS.WorldServerID INTO _WorldServerID FROM WorldServers WS WHERE WS.CustomerGUID = _CustomerGUID LIMIT 1;

        UPDATE WorldServers WS
        SET ActiveStartTime=NOW(),
            ServerStatus=1
        WHERE CustomerGUID = _CustomerGUID
          AND WS.WorldServerID = _WorldServerID;
        -- _WorldServerID = -1;

        INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
        VALUES (NOW(), CONCAT('StartWorldServer Success (Local): ', CAST(_WorldServerID AS CHAR), ' IncomingIP: ',
                              CAST(_ServerIP AS CHAR)), _CustomerGUID);
    ELSE
        INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
        VALUES (NOW(), CONCAT('StartWorldServer Failed: ', CAST(_WorldServerID AS CHAR), ' IncomingIP: ',
                              CAST(_ServerIP AS CHAR)), _CustomerGUID);
    END IF;
    RETURN _WorldServerID;
END;

CREATE OR REPLACE PROCEDURE UpdateCharacterStats(_CustomerGUID CHAR(36),
                                                 _CharName VARCHAR(50),
                                                 _CharacterLevel SMALLINT,
                                                 _Gender SMALLINT,
                                                 _Weight FLOAT,
                                                 _Size SMALLINT,
                                                 _Fame FLOAT,
                                                 _Alignment FLOAT,
                                                 _Description TEXT,
                                                 _XP INT,
                                                 _X FLOAT,
                                                 _Y FLOAT,
                                                 _Z FLOAT,
                                                 _RX FLOAT,
                                                 _RY FLOAT,
                                                 _RZ FLOAT,
                                                 _TeamNumber INT,
                                                 _HitDie SMALLINT,
                                                 _Wounds FLOAT,
                                                 _Thirst FLOAT,
                                                 _Hunger FLOAT,
                                                 _MaxHealth FLOAT,
                                                 _Health FLOAT,
                                                 _HealthRegenRate FLOAT,
                                                 _MaxMana FLOAT,
                                                 _Mana FLOAT,
                                                 _ManaRegenRate FLOAT,
                                                 _MaxEnergy FLOAT,
                                                 _Energy FLOAT,
                                                 _EnergyRegenRate FLOAT,
                                                 _MaxFatigue FLOAT,
                                                 _Fatigue FLOAT,
                                                 _FatigueRegenRate FLOAT,
                                                 _MaxStamina FLOAT,
                                                 _Stamina FLOAT,
                                                 _StaminaRegenRate FLOAT,
                                                 _MaxEndurance FLOAT,
                                                 _Endurance FLOAT,
                                                 _EnduranceRegenRate FLOAT,
                                                 _Strength FLOAT,
                                                 _Dexterity FLOAT,
                                                 _Constitution FLOAT,
                                                 _Intellect FLOAT,
                                                 _Wisdom FLOAT,
                                                 _Charisma FLOAT,
                                                 _Agility FLOAT,
                                                 _Spirit FLOAT,
                                                 _Magic FLOAT,
                                                 _Fortitude FLOAT,
                                                 _Reflex FLOAT,
                                                 _Willpower FLOAT,
                                                 _BaseAttack FLOAT,
                                                 _BaseAttackBonus FLOAT,
                                                 _AttackPower FLOAT,
                                                 _AttackSpeed FLOAT,
                                                 _CritChance FLOAT,
                                                 _CritMultiplier FLOAT,
                                                 _Haste FLOAT,
                                                 _SpellPower FLOAT,
                                                 _SpellPenetration FLOAT,
                                                 _Defense FLOAT,
                                                 _Dodge FLOAT,
                                                 _Parry FLOAT,
                                                 _Avoidance FLOAT,
                                                 _Versatility FLOAT,
                                                 _Multishot FLOAT,
                                                 _Initiative FLOAT,
                                                 _NaturalArmor FLOAT,
                                                 _PhysicalArmor FLOAT,
                                                 _BonusArmor FLOAT,
                                                 _ForceArmor FLOAT,
                                                 _MagicArmor FLOAT,
                                                 _Resistance FLOAT,
                                                 _ReloadSpeed FLOAT,
                                                 _Range FLOAT,
                                                 _Speed FLOAT,
                                                 _Gold INT,
                                                 _Silver INT,
                                                 _Copper INT,
                                                 _FreeCurrency INT,
                                                 _PremiumCurrency INT,
                                                 _Perception FLOAT,
                                                 _Acrobatics FLOAT,
                                                 _Climb FLOAT,
                                                 _Stealth FLOAT,
                                                 _Score INT)
BEGIN
    UPDATE Characters
    SET CharacterLevel=_CharacterLevel,
        Gender=_Gender,
        Weight=_Weight,
        Size=_Size,
        Fame=_Fame,
        Alignment=_Alignment,
        Description=_Description,
        XP=_XP,
        X=_X,
        Y=_Y,
        Z=_Z,
        RX=_RX,
        RY=_RY,
        RZ=_RZ,
        TeamNumber=_TeamNumber,
        HitDie=_HitDie,
        Wounds=_Wounds,
        Thirst=_Thirst,
        Hunger=_Hunger,
        MaxHealth=_MaxHealth,
        Health=_Health,
        HealthRegenRate=_HealthRegenRate,
        MaxMana=_MaxMana,
        Mana=_Mana,
        ManaRegenRate=_ManaRegenRate,
        MaxEnergy=_MaxEnergy,
        Energy=_Energy,
        EnergyRegenRate=_EnergyRegenRate,
        MaxFatigue=_MaxFatigue,
        Fatigue=_Fatigue,
        FatigueRegenRate=_FatigueRegenRate,
        MaxStamina=_MaxStamina,
        Stamina=_Stamina,
        StaminaRegenRate=_StaminaRegenRate,
        MaxEndurance=_MaxEndurance,
        Endurance=_Endurance,
        EnduranceRegenRate=_EnduranceRegenRate,
        Strength=_Strength,
        Dexterity=_Dexterity,
        Constitution=_Constitution,
        Intellect=_Intellect,
        Wisdom=_Wisdom,
        Charisma=_Charisma,
        Agility=_Agility,
        Spirit=_Spirit,
        Magic=_Magic,
        Fortitude=_Fortitude,
        Reflex=_Reflex,
        Willpower=_Willpower,
        BaseAttack=_BaseAttack,
        BaseAttackBonus=_BaseAttackBonus,
        AttackPower=_AttackPower,
        AttackSpeed=_AttackSpeed,
        CritChance=_CritChance,
        CritMultiplier=_CritMultiplier,
        Haste=_Haste,
        SpellPower=_SpellPower,
        SpellPenetration=_SpellPenetration,
        Defense=_Defense,
        Dodge=_Dodge,
        Parry=_Parry,
        Avoidance=_Avoidance,
        Versatility=_Versatility,
        Multishot=_Multishot,
        Initiative=_Initiative,
        NaturalArmor=_NaturalArmor,
        PhysicalArmor=_PhysicalArmor,
        BonusArmor=_BonusArmor,
        ForceArmor=_ForceArmor,
        MagicArmor=_MagicArmor,
        Resistance=_Resistance,
        ReloadSpeed=_ReloadSpeed,
        `Range`=_Range,
        Speed=_Speed,
        Gold=_Gold,
        Silver=_Silver,
        Copper=_Copper,
        FreeCurrency=_FreeCurrency,
        PremiumCurrency=_PremiumCurrency,
        Perception=_Perception,
        Acrobatics=_Acrobatics,
        Climb=_Climb,
        Stealth=_Stealth,
        Score=_Score
    WHERE CustomerGUID = _CustomerGUID
      AND CharName = _CharName;

    INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
    VALUES (NOW(), CONCAT('UpdateCharacterStats: ', _CharName), _CustomerGUID);
END;

CREATE OR REPLACE PROCEDURE UpdateNumberOfPlayers(_CustomerGUID CHAR(36),
                                                  _IP VARCHAR(50),
                                                  _Port INT,
                                                  _NumberOfReportedPlayers INT)
BEGIN
    DECLARE _LastNumberOfReportedPlayers INT;
    DECLARE _WorldServerID INT;

    SELECT MI.NumberOfReportedPlayers
    INTO _LastNumberOfReportedPlayers
    FROM MapInstances MI
             INNER JOIN WorldServers WS
                        ON WS.WorldServerID = MI.WorldServerID
    WHERE (WS.ServerIP = _IP OR WS.InternalServerIP = _IP)
      AND MI.Port = _Port;

    SELECT WS.WorldServerID
    INTO _WorldServerID
    FROM WorldServers WS
    WHERE (WS.ServerIP = _IP
        OR WS.InternalServerIP = _IP);

    -- INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
    -- VALUES (now(), 'UpdateNumberOfPlayers: ' || _IP || ':' || CONVERT(varchar, _Port) || ' #:' || CONVERT(varchar, _NumberOfReportedPlayers) || ' - ' || CONVERT(varchar, _LastNumberOfReportedPlayers), _CustomerGUID);

    IF (_LastNumberOfReportedPlayers > 0 AND _NumberOfReportedPlayers = 0) THEN
        UPDATE MapInstances MI
        SET NumberOfReportedPlayers=_NumberOfReportedPlayers,
            LastUpdateFromServer=NOW(),
            LastServerEmptyDate=NOW()
        WHERE MI.WorldServerID = _WorldServerID
          AND MI.Port = _Port;
    ELSEIF (_LastNumberOfReportedPlayers = 0 AND _NumberOfReportedPlayers > 0) THEN
        UPDATE MapInstances MI
        SET NumberOfReportedPlayers=_NumberOfReportedPlayers,
            LastUpdateFromServer=NOW(),
            LastServerEmptyDate=NULL
        WHERE MI.WorldServerID = _WorldServerID
          AND MI.Port = _Port;
    ELSE
        UPDATE MapInstances MI
        SET NumberOfReportedPlayers=_NumberOfReportedPlayers,
            LastUpdateFromServer=NOW()
        WHERE MI.WorldServerID = _WorldServerID
          AND MI.Port = _Port;
    END IF;
END;

CREATE OR REPLACE PROCEDURE UpdatePositionOfCharacterByName(_CustomerGUID CHAR(36),
                                                            _CharName VARCHAR(50),
                                                            _MapName VARCHAR(50),
                                                            _X FLOAT,
                                                            _Y FLOAT,
                                                            _Z FLOAT,
                                                            _RX FLOAT,
                                                            _RY FLOAT,
                                                            _RZ FLOAT)
BEGIN
    IF (_MapName <> '') THEN
        UPDATE Characters
        SET
            -- MapName=_MapName,
            X=_X,
            Y=_Y,
            Z=_Z + 1,
            RX=_RX,
            RY=_RY,
            RZ=_RZ
        WHERE CharName = _CharName
          AND CustomerGUID = _CustomerGUID;
    ELSE
        UPDATE Characters
        SET X=_X,
            Y=_Y,
            Z=_Z + 1,
            RX=_RX,
            RY=_RY,
            RZ=_RZ
        WHERE CharName = _CharName
          AND CustomerGUID = _CustomerGUID;
    END IF;

    UPDATE Users
    SET LastAccess=NOW()
    WHERE Users.CustomerGUID = _CustomerGUID
      AND Users.UserGUID =
          (SELECT UserGUID FROM Characters WHERE CustomerGUID = _CustomerGUID AND CharName = _CharName);
/*
INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
VALUES (now(), 'UpdatePosition: ' || _CharName || ' - ' || ISNULL(_MapName,'None'), _CustomerGUID);
*/
END;

CREATE OR REPLACE PROCEDURE UserSessionSetSelectedCharacter(_CustomerGUID CHAR(36),
                                                            _UserSessionGUID CHAR(36),
                                                            _SelectedCharacterName VARCHAR(50))
BEGIN
    UPDATE UserSessions
    SET SelectedCharacterName=_SelectedCharacterName
    WHERE CustomerGUID = _CustomerGUID
      AND UserSessionGUID = _UserSessionGUID;
END;

INSERT INTO OWSVersion (OWSDBVersion) VALUES('20210829');
