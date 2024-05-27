CREATE DATABASE openworldserver;

\connect openworldserver;

CREATE EXTENSION pgcrypto;

CREATE TABLE DebugLog
(
    DebugLogID   SERIAL
        CONSTRAINT PK_DebugLogK
            PRIMARY KEY,
    DebugDate    TIMESTAMP,
    DebugDesc    TEXT,
    CustomerGUID UUID
);

CREATE TABLE Customers
(
    CustomerGUID       UUID        DEFAULT gen_random_uuid() NOT NULL
        CONSTRAINT PK_Customers
            PRIMARY KEY,
    CustomerName       VARCHAR(50)                           NOT NULL,
    CustomerEmail      VARCHAR(255)                          NOT NULL,
    CustomerPhone      VARCHAR(20),
    CustomerNotes      TEXT,
    EnableDebugLogging BOOLEAN         DEFAULT FALSE            NOT NULL,
    EnableAutoLoopBack BOOLEAN         DEFAULT TRUE            NOT NULL,
    DeveloperPaid      BOOLEAN         DEFAULT FALSE            NOT NULL,
    PublisherPaidDate  TIMESTAMP,
    StripeCustomerID   VARCHAR(50) DEFAULT ''                NOT NULL,
    FreeTrialStarted   TIMESTAMP,
    SupportUnicode     BOOLEAN         DEFAULT FALSE            NOT NULL,
    CreateDate         TIMESTAMP   DEFAULT NOW()             NOT NULL,
    NoPortForwarding   BOOLEAN         DEFAULT FALSE            NOT NULL
);

CREATE TABLE AbilityTypes
(
    AbilityTypeID   SERIAL,
    AbilityTypeName VARCHAR(50) NOT NULL,
    CustomerGUID    UUID        NOT NULL,
    CONSTRAINT PK_AbilityTypes
        PRIMARY KEY (AbilityTypeID, CustomerGUID)
);

CREATE TABLE Abilities
(
    CustomerGUID             UUID                    NOT NULL,
    AbilityID                SERIAL,
    AbilityName              VARCHAR(50)             NOT NULL,
    AbilityTypeID            INT                     NOT NULL,
    TextureToUseForIcon      VARCHAR(200) DEFAULT '' NOT NULL,
    Class                    INT,
    Race                     INT,
    AbilityCustomJSON        TEXT,
    GameplayAbilityClassName VARCHAR(200) DEFAULT '' NOT NULL,
    CONSTRAINT PK_Abilities
        PRIMARY KEY (CustomerGUID, AbilityID),
    CONSTRAINT FK_Abilities_AbilityTypes
        FOREIGN KEY (CustomerGUID, AbilityTypeID) REFERENCES AbilityTypes (CustomerGUID, AbilityTypeID)
);

CREATE TABLE AreaOfInterestTypes
(
    AreaOfInterestTypeID   SERIAL
        CONSTRAINT PK_AreaOfInterestTypes
            PRIMARY KEY,
    AreaOfInterestTypeDesc VARCHAR(50) NOT NULL
);

CREATE TABLE AreasOfInterest
(
    CustomerGUID        UUID        NOT NULL,
    AreasOfInterestGUID UUID        NOT NULL,
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
    UserGUID     UUID      DEFAULT gen_random_uuid() NOT NULL
        CONSTRAINT PK_Users
            PRIMARY KEY,
    CustomerGUID UUID                                NOT NULL,
    FirstName    VARCHAR(50)                         NOT NULL,
    LastName     VARCHAR(50)                         NOT NULL,
    Email        VARCHAR(255)                        NOT NULL,
    PasswordHash VARCHAR(128)                        NOT NULL,
    CreateDate   TIMESTAMP DEFAULT NOW()             NOT NULL,
    LastAccess   TIMESTAMP DEFAULT NOW()             NOT NULL,
    Role         VARCHAR(10)                         NOT NULL,
    CONSTRAINT AK_User
        UNIQUE (CustomerGUID, Email, Role)
);

CREATE TABLE Characters
(
    CustomerGUID              UUID                        NOT NULL,
    CharacterID               SERIAL                      NOT NULL,
    UserGUID                  UUID                        NULL,
    Email                     VARCHAR(256)                NOT NULL,
    CharName                  VARCHAR(50)                 NOT NULL,
    MapName                   VARCHAR(50)                 NULL,
    X                         FLOAT                       NOT NULL,
    Y                         FLOAT                       NOT NULL,
    Z                         FLOAT                       NOT NULL,
    Perception                FLOAT                       NOT NULL,
    Acrobatics                FLOAT                       NOT NULL,
    Climb                     FLOAT                       NOT NULL,
    Stealth                   FLOAT                       NOT NULL,
    ServerIP                  VARCHAR(50)                 NULL,
    LastActivity              TIMESTAMP    DEFAULT NOW()  NOT NULL,
    RX                        FLOAT        DEFAULT 0      NOT NULL,
    RY                        FLOAT        DEFAULT 0      NOT NULL,
    RZ                        FLOAT        DEFAULT 0      NOT NULL,
    Spirit                    FLOAT        DEFAULT 0      NOT NULL,
    Magic                     FLOAT        DEFAULT 0      NOT NULL,
    TeamNumber                INT          DEFAULT 0      NOT NULL,
    Thirst                    FLOAT        DEFAULT 0      NOT NULL,
    Hunger                    FLOAT        DEFAULT 0      NOT NULL,
    Gold                      INT          DEFAULT 0      NOT NULL,
    Score                     INT          DEFAULT 0      NOT NULL,
    CharacterLevel            SMALLINT     DEFAULT 0      NOT NULL,
    Gender                    SMALLINT     DEFAULT 0      NOT NULL,
    XP                        INT          DEFAULT 0      NOT NULL,
    HitDie                    SMALLINT     DEFAULT 0      NOT NULL,
    Wounds                    FLOAT        DEFAULT 0      NOT NULL,
    Size                      SMALLINT     DEFAULT 0      NOT NULL,
    Weight                    FLOAT        DEFAULT 0      NOT NULL,
    MaxHealth                 FLOAT        DEFAULT 0      NOT NULL,
    Health                    FLOAT        DEFAULT 0      NOT NULL,
    HealthRegenRate           FLOAT        DEFAULT 0      NOT NULL,
    MaxMana                   FLOAT        DEFAULT 0      NOT NULL,
    Mana                      FLOAT        DEFAULT 0      NOT NULL,
    ManaRegenRate             FLOAT        DEFAULT 0      NOT NULL,
    MaxEnergy                 FLOAT        DEFAULT 0      NOT NULL,
    Energy                    FLOAT        DEFAULT 0      NOT NULL,
    EnergyRegenRate           FLOAT        DEFAULT 0      NOT NULL,
    MaxFatigue                FLOAT        DEFAULT 0      NOT NULL,
    Fatigue                   FLOAT        DEFAULT 0      NOT NULL,
    FatigueRegenRate          FLOAT        DEFAULT 0      NOT NULL,
    MaxStamina                FLOAT        DEFAULT 0      NOT NULL,
    Stamina                   FLOAT        DEFAULT 0      NOT NULL,
    StaminaRegenRate          FLOAT        DEFAULT 0      NOT NULL,
    MaxEndurance              FLOAT        DEFAULT 0      NOT NULL,
    Endurance                 FLOAT        DEFAULT 0      NOT NULL,
    EnduranceRegenRate        FLOAT        DEFAULT 0      NOT NULL,
    Strength                  FLOAT        DEFAULT 0      NOT NULL,
    Dexterity                 FLOAT        DEFAULT 0      NOT NULL,
    Constitution              FLOAT        DEFAULT 0      NOT NULL,
    Intellect                 FLOAT        DEFAULT 0      NOT NULL,
    Wisdom                    FLOAT        DEFAULT 0      NOT NULL,
    Charisma                  FLOAT        DEFAULT 0      NOT NULL,
    Agility                   FLOAT        DEFAULT 0      NOT NULL,
    Fortitude                 FLOAT        DEFAULT 0      NOT NULL,
    Reflex                    FLOAT        DEFAULT 0      NOT NULL,
    Willpower                 FLOAT        DEFAULT 0      NOT NULL,
    BaseAttack                FLOAT        DEFAULT 0      NOT NULL,
    BaseAttackBonus           FLOAT        DEFAULT 0      NOT NULL,
    AttackPower               FLOAT        DEFAULT 0      NOT NULL,
    AttackSpeed               FLOAT        DEFAULT 0      NOT NULL,
    CritChance                FLOAT        DEFAULT 0      NOT NULL,
    CritMultiplier            FLOAT        DEFAULT 0      NOT NULL,
    Haste                     FLOAT        DEFAULT 0      NOT NULL,
    SpellPower                FLOAT        DEFAULT 0      NOT NULL,
    SpellPenetration          FLOAT        DEFAULT 0      NOT NULL,
    Defense                   FLOAT        DEFAULT 0      NOT NULL,
    Dodge                     FLOAT        DEFAULT 0      NOT NULL,
    Parry                     FLOAT        DEFAULT 0      NOT NULL,
    Avoidance                 FLOAT        DEFAULT 0      NOT NULL,
    Versatility               FLOAT        DEFAULT 0      NOT NULL,
    Multishot                 FLOAT        DEFAULT 0      NOT NULL,
    Initiative                FLOAT        DEFAULT 0      NOT NULL,
    NaturalArmor              FLOAT        DEFAULT 0      NOT NULL,
    PhysicalArmor             FLOAT        DEFAULT 0      NOT NULL,
    BonusArmor                FLOAT        DEFAULT 0      NOT NULL,
    ForceArmor                FLOAT        DEFAULT 0      NOT NULL,
    MagicArmor                FLOAT        DEFAULT 0      NOT NULL,
    Resistance                FLOAT        DEFAULT 0      NOT NULL,
    ReloadSpeed               FLOAT        DEFAULT 0      NOT NULL,
    Range                     FLOAT        DEFAULT 0      NOT NULL,
    Speed                     FLOAT        DEFAULT 0      NOT NULL,
    Silver                    INT          DEFAULT 0      NOT NULL,
    Copper                    INT          DEFAULT 0      NOT NULL,
    FreeCurrency              INT          DEFAULT 0      NOT NULL,
    PremiumCurrency           INT          DEFAULT 0      NOT NULL,
    Fame                      FLOAT        DEFAULT 0      NOT NULL,
    Alignment                 FLOAT        DEFAULT 0      NOT NULL,
    Description               TEXT                        NULL,
    DefaultPawnClassPath      VARCHAR(200) DEFAULT ''     NOT NULL,
    IsInternalNetworkTestUser BOOLEAN          DEFAULT FALSE NOT NULL,
    ClassID                   INT                         NOT NULL,
    BaseMesh                  VARCHAR(100)                NULL,
    IsAdmin                   BOOLEAN          DEFAULT FALSE NOT NULL,
    IsModerator               BOOLEAN          DEFAULT FALSE NOT NULL,
    CreateDate                TIMESTAMP    DEFAULT NOW()  NOT NULL,
    CONSTRAINT PK_Chars
        PRIMARY KEY (CustomerGUID, CharacterID),
    CONSTRAINT FK_Characters_UserGUID
        FOREIGN KEY (UserGUID) REFERENCES Users (UserGUID)
);

CREATE TABLE CharHasAbilities
(
    CustomerGUID               UUID          NOT NULL,
    CharHasAbilitiesID         SERIAL        NOT NULL,
    CharacterID                INT           NOT NULL,
    AbilityID                  INT           NOT NULL,
    AbilityLevel               INT DEFAULT 1 NOT NULL,
    CharHasAbilitiesCustomJSON TEXT          NULL,
    CONSTRAINT PK_CharHasAbilities
        PRIMARY KEY (CustomerGUID, CharHasAbilitiesID),
    CONSTRAINT FK_CharHasAbilities_CharacterID
        FOREIGN KEY (CustomerGUID, CharacterID) REFERENCES Characters (CustomerGUID, CharacterID)
);

CREATE TABLE CharAbilityBars
(
    CustomerGUID              UUID                   NOT NULL,
    CharAbilityBarID          SERIAL                 NOT NULL,
    CharacterID               INT                    NOT NULL,
    AbilityBarName            VARCHAR(50) DEFAULT '' NOT NULL,
    CharAbilityBarsCustomJSON TEXT                   NULL,
    MaxNumberOfSlots          INT         DEFAULT 1  NOT NULL,
    NumberOfUnlockedSlots     INT         DEFAULT 1  NOT NULL,
    CONSTRAINT PK_CharAbilityBars
        PRIMARY KEY (CustomerGUID, CharAbilityBarID)
);

CREATE TABLE CharAbilityBarAbilities
(
    CustomerGUID                      UUID          NOT NULL,
    CharAbilityBarAbilityID           SERIAL        NOT NULL,
    CharAbilityBarID                  INT           NOT NULL,
    CharHasAbilitiesID                INT           NOT NULL,
    CharAbilityBarAbilitiesCustomJSON TEXT          NULL,
    InSlotNumber                      INT DEFAULT 1 NOT NULL,
    CONSTRAINT PK_CharAbilityBarAbilities
        PRIMARY KEY (CustomerGUID, CharAbilityBarAbilityID),
    CONSTRAINT FK_CharAbilityBarAbilities_CharAbilityBarID
        FOREIGN KEY (CustomerGUID, CharAbilityBarID) REFERENCES CharAbilityBars (CustomerGUID, CharAbilityBarID),
    CONSTRAINT FK_CharAbilityBarAbilities_CharHasAbilities
        FOREIGN KEY (CustomerGUID, CharHasAbilitiesID) REFERENCES CharHasAbilities (CustomerGUID, CharHasAbilitiesID)
);

CREATE TABLE CharHasItems
(
    CustomerGUID  UUID               NOT NULL,
    CharacterID   INT                NOT NULL,
    CharHasItemID SERIAL             NOT NULL,
    ItemID        INT                NOT NULL,
    Quantity      INT DEFAULT 1      NOT NULL,
    Equipped      BOOLEAN DEFAULT FALSE NOT NULL,
    CONSTRAINT PK_CharHasItems
        PRIMARY KEY (CustomerGUID, CharacterID, CharHasItemID),
    CONSTRAINT FK_CharHasItems_CharacterID
        FOREIGN KEY (CustomerGUID, CharacterID) REFERENCES Characters (CustomerGUID, CharacterID)
);

CREATE TABLE CharInventory
(
    CustomerGUID    UUID          NOT NULL,
    CharacterID     INT           NOT NULL,
    CharInventoryID SERIAL        NOT NULL,
    InventoryName   VARCHAR(50)   NOT NULL,
    InventorySize   INT           NOT NULL,
    InventoryWidth  INT DEFAULT 1 NOT NULL,
    InventoryHeight INT DEFAULT 1 NOT NULL,
    CONSTRAINT PK_CharInventory
        PRIMARY KEY (CustomerGUID, CharacterID, CharInventoryID)
);

CREATE TABLE CharInventoryItems
(
    CustomerGUID          UUID                           NOT NULL,
    CharInventoryID       INT                            NOT NULL,
    CharInventoryItemID   SERIAL                         NOT NULL,
    ItemID                INT                            NOT NULL,
    InSlotNumber          INT                            NOT NULL,
    Quantity              INT                            NOT NULL,
    NumberOfUsesLeft      INT  DEFAULT 0                 NOT NULL,
    Condition             INT  DEFAULT 100               NOT NULL,
    CharInventoryItemGUID UUID DEFAULT gen_random_uuid() NOT NULL,
    CustomData            TEXT                           NULL,
    CONSTRAINT PK_CharInventoryItems
        PRIMARY KEY (CustomerGUID, CharInventoryID, CharInventoryItemID)
);

CREATE TABLE CharOnMapInstance
(
    CustomerGUID  UUID NOT NULL,
    CharacterID   INT  NOT NULL,
    MapInstanceID INT  NOT NULL,
    CONSTRAINT PK_CharOnMapInstance
        PRIMARY KEY (CustomerGUID, CharacterID, MapInstanceID)
);

CREATE TABLE ChatGroups
(
    CustomerGUID  UUID        NOT NULL,
    ChatGroupID   SERIAL      NOT NULL,
    ChatGroupName VARCHAR(50) NOT NULL,
    CONSTRAINT PK_ChatGroups
        PRIMARY KEY (CustomerGUID, ChatGroupID)
);

CREATE TABLE ChatGroupUsers
(
    CustomerGUID UUID NOT NULL,
    ChatGroupID  INT  NOT NULL,
    CharacterID  INT  NOT NULL,
    CONSTRAINT PK_ChatGroupUsers
        PRIMARY KEY (CustomerGUID, ChatGroupID, CharacterID)
);

CREATE TABLE ChatMessages
(
    CustomerGUID  UUID                    NOT NULL,
    ChatMessageID SERIAL                  NOT NULL,
    SentByCharID  INT                     NOT NULL,
    SentToCharID  INT                     NULL,
    ChatGroupID   INT                     NULL,
    ChatMessage   TEXT                    NOT NULL,
    MessageDate   TIMESTAMP DEFAULT NOW() NOT NULL,
    CONSTRAINT PK_ChatMessages
        PRIMARY KEY (CustomerGUID, ChatMessageID)
);

CREATE TABLE Class
(
    CustomerGUID       UUID                   NOT NULL,
    ClassID            SERIAL                 NOT NULL,
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
    Range              FLOAT                  NOT NULL,
    Speed              FLOAT                  NOT NULL,
    Silver             INT                    NOT NULL,
    Copper             INT                    NOT NULL,
    FreeCurrency       INT                    NOT NULL,
    PremiumCurrency    INT                    NOT NULL,
    Fame               FLOAT                  NOT NULL,
    Alignment          FLOAT                  NOT NULL,
    Description        TEXT                   NULL,
    CONSTRAINT PK_Class
        PRIMARY KEY (CustomerGUID, ClassID)
);

CREATE TABLE ClassInventory
(
    ClassInventoryID SERIAL      NOT NULL,
    CustomerGUID     UUID        NOT NULL,
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
    CustomerGUID          UUID        NOT NULL,
    CustomCharacterDataID SERIAL      NOT NULL,
    CharacterID           INT         NOT NULL,
    CustomFieldName       VARCHAR(50) NOT NULL,
    FieldValue            TEXT        NOT NULL,
    CONSTRAINT PK_CustomCharacterData
        PRIMARY KEY (CustomerGUID, CustomCharacterDataID),
    CONSTRAINT FK_CustomCharacterData_CharID
        FOREIGN KEY (CustomerGUID, CharacterID) REFERENCES Characters (CustomerGUID, CharacterID)
);

CREATE TABLE GlobalData
(
    CustomerGUID    UUID        NOT NULL,
    GlobalDataKey   VARCHAR(50) NOT NULL,
    GlobalDataValue TEXT        NOT NULL,
    CONSTRAINT PK_GlobalData
        PRIMARY KEY (CustomerGUID, GlobalDataKey)
);

CREATE TABLE Items
(
    CustomerGUID         UUID                          NOT NULL,
    ItemID               SERIAL                        NOT NULL,
    ItemTypeID           INT                           NOT NULL,
    ItemName             VARCHAR(50)                   NOT NULL,
    ItemWeight           DECIMAL(18, 2) DEFAULT 0      NOT NULL,
    ItemCanStack         BOOLEAN            DEFAULT FALSE NOT NULL,
    ItemStackSize        SMALLINT       DEFAULT 0      NOT NULL,
    ItemIsUsable         BOOLEAN            DEFAULT FALSE NOT NULL,
    ItemIsConsumedOnUse  BOOLEAN            DEFAULT TRUE NOT NULL,
    CustomData           VARCHAR(2000)  DEFAULT ''     NOT NULL,
    DefaultNumberOfUses  INT            DEFAULT 0      NOT NULL,
    ItemValue            INT            DEFAULT 0      NOT NULL,
    ItemMesh             VARCHAR(200)   DEFAULT ''     NOT NULL,
    MeshToUseForPickup   VARCHAR(200)   DEFAULT ''     NOT NULL,
    TextureToUseForIcon  VARCHAR(200)   DEFAULT ''     NOT NULL,
    PremiumCurrencyPrice INT            DEFAULT 0      NOT NULL,
    FreeCurrencyPrice    INT            DEFAULT 0      NOT NULL,
    ItemTier             INT            DEFAULT 0      NOT NULL,
    ItemDescription      TEXT           DEFAULT ''     NOT NULL,
    ItemCode             VARCHAR(50)    DEFAULT ''     NOT NULL,
    ItemDuration         INT            DEFAULT 0      NOT NULL,
    CanBeDropped         BOOLEAN            DEFAULT TRUE NOT NULL,
    CanBeDestroyed       BOOLEAN            DEFAULT FALSE NOT NULL,
    WeaponActorClass     VARCHAR(200)   DEFAULT ''     NOT NULL,
    StaticMesh           VARCHAR(200)   DEFAULT ''     NOT NULL,
    SkeletalMesh         VARCHAR(200)   DEFAULT ''     NOT NULL,
    ItemQuality          SMALLINT       DEFAULT 0      NOT NULL,
    IconSlotWidth        INT            DEFAULT 1      NOT NULL,
    IconSlotHeight       INT            DEFAULT 1      NOT NULL,
    ItemMeshID           INT            DEFAULT 0      NOT NULL,
    CONSTRAINT PK_Items
        PRIMARY KEY (CustomerGUID, ItemID)
);

CREATE TABLE ItemTypes
(
    CustomerGUID      UUID               NOT NULL,
    ItemTypeID        SERIAL             NOT NULL,
    ItemTypeDesc      VARCHAR(50)        NOT NULL,
    UserItemType      SMALLINT DEFAULT 0 NOT NULL,
    EquipmentType     SMALLINT DEFAULT 0 NOT NULL,
    ItemQuality       SMALLINT DEFAULT 0 NOT NULL,
    EquipmentSlotType SMALLINT DEFAULT 0 NOT NULL,
    CONSTRAINT PK_ItemTypes
        PRIMARY KEY (CustomerGUID, ItemTypeID)
);

CREATE TABLE MapInstances
(
    CustomerGUID            UUID                    NOT NULL,
    MapInstanceID           SERIAL                  NOT NULL,
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
        PRIMARY KEY (CustomerGUID, MapInstanceID)
);

CREATE TABLE Maps
(
    CustomerGUID                UUID                    NOT NULL,
    MapID                       SERIAL                  NOT NULL,
    MapName                     VARCHAR(50)             NOT NULL,
    MapData                     BYTEA                   NULL,
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
        PRIMARY KEY (CustomerGUID, MapID)
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
    PlayerGroupID     SERIAL        NOT NULL,
    CustomerGUID      UUID          NOT NULL,
    PlayerGroupName   VARCHAR(50)   NOT NULL,
    PlayerGroupTypeID INT           NOT NULL,
    ReadyState        INT DEFAULT 0 NOT NULL,
    CreateDate        TIMESTAMP     NULL,
    CONSTRAINT PK_PlayerGroup
        PRIMARY KEY (CustomerGUID, PlayerGroupID),
    CONSTRAINT FK_PlayerGroup_PlayerGroupType
        FOREIGN KEY (PlayerGroupTypeID) REFERENCES PlayerGroupTypes (PlayerGroupTypeID)
);

CREATE TABLE PlayerGroupCharacters
(
    PlayerGroupID INT                     NOT NULL,
    CustomerGUID  UUID                    NOT NULL,
    CharacterID   INT                     NOT NULL,
    DateAdded     TIMESTAMP DEFAULT NOW() NOT NULL,
    TeamNumber    INT       DEFAULT 0     NOT NULL,
    CONSTRAINT PK_PlayerGroupCharacters
        PRIMARY KEY (PlayerGroupID, CustomerGUID, CharacterID)
);

CREATE TABLE Races
(
    CustomerGUID UUID        NOT NULL,
    RaceID       SERIAL      NOT NULL,
    RaceName     VARCHAR(50) NOT NULL,
    CONSTRAINT PK_Races
        PRIMARY KEY (CustomerGUID, RaceID)
);

CREATE TABLE UserSessions
(
    CustomerGUID          UUID                           NOT NULL,
    UserSessionGUID       UUID DEFAULT gen_random_uuid() NOT NULL,
    UserGUID              UUID                           NOT NULL,
    LoginDate             TIMESTAMP                      NOT NULL,
    SelectedCharacterName VARCHAR(50)                    NULL,
    CONSTRAINT PK_UserSessions
        PRIMARY KEY (CustomerGUID, UserSessionGUID),
    CONSTRAINT FK_UserSessions_UserGUID
        FOREIGN KEY (UserGUID) REFERENCES Users (UserGUID)
);

CREATE TABLE UsersInQueue
(
    CustomerGUID     UUID          NOT NULL,
    UserGUID         UUID          NOT NULL,
    QueueName        VARCHAR(20)   NOT NULL,
    JoinDT           TIMESTAMP     NOT NULL,
    MatchMakingScore INT DEFAULT 0 NOT NULL,
    CONSTRAINT PK_UsersInQueue
        PRIMARY KEY (CustomerGUID, UserGUID, QueueName)
);

CREATE TABLE WorldServers
(
    CustomerGUID            UUID                     NOT NULL,
    WorldServerID           SERIAL                   NOT NULL,
    ServerIP                VARCHAR(50)              NOT NULL,
    MaxNumberOfInstances    INT                      NOT NULL,
    ActiveStartTime         TIMESTAMP                NULL,
    Port                    INT         DEFAULT 8181 NOT NULL,
    ServerStatus            SMALLINT    DEFAULT 0    NOT NULL,
    InternalServerIP        VARCHAR(50) DEFAULT ''   NOT NULL,
    StartingMapInstancePort INT         DEFAULT 7778 NOT NULL,
    CONSTRAINT PK_WorldServers
        PRIMARY KEY (CustomerGUID, WorldServerID)
);

CREATE TABLE WorldSettings
(
    CustomerGUID    UUID   NOT NULL,
    WorldSettingsID SERIAL NOT NULL,
    StartTime       BIGINT NOT NULL,
    CONSTRAINT PK_WorldSettings
        PRIMARY KEY (CustomerGUID, WorldSettingsID)
);

CREATE VIEW vRandNumber AS
SELECT RANDOM() AS RandNumber;

CREATE OR REPLACE FUNCTION AbilityMod(AbilityCore INT) RETURNS INT
    LANGUAGE SQL
    IMMUTABLE
AS
$$
SELECT FLOOR(($1 - 10) / 2)
$$;

CREATE OR REPLACE FUNCTION CalcXFromTile(Tile INT, MapWidth SMALLINT) RETURNS INT
    LANGUAGE SQL
    IMMUTABLE
AS
$$
SELECT $1 % $2
$$;

CREATE OR REPLACE FUNCTION CalcYFromTile(Tile INT, MapWidth SMALLINT) RETURNS INT
    LANGUAGE SQL
    IMMUTABLE
AS
$$
SELECT FLOOR($1 / $2)
$$;

CREATE OR REPLACE FUNCTION OffsetIntoMap(X INT, Y INT, MapWidth SMALLINT) RETURNS INT
    LANGUAGE SQL
    IMMUTABLE
AS
$$
SELECT ($2 - 1) * $3 + $1
$$;

CREATE OR REPLACE FUNCTION RollDice(NumberOfDice INT, MaxDiceValue INT) RETURNS INT
    LANGUAGE PLPGSQL
AS
$$
DECLARE
    _TotalValue      INT := 0;
    _CurrentDiceRoll INT := 0;
BEGIN
    WHILE (_CurrentDiceRoll < NumberOfDice)
        LOOP
            SELECT _TotalValue + CAST(FLOOR(($2) * RandNumber) AS INT) + 1 FROM vRandNumber INTO _TotalValue;
            _CurrentDiceRoll := _CurrentDiceRoll + 1;
        END LOOP;
    RETURN _TotalValue;
END;
$$;

CREATE OR REPLACE FUNCTION CalculateDistanceBetweenTwoTiles(Tile1 INT, Tile2 INT, MapWidth SMALLINT) RETURNS INT
    LANGUAGE PLPGSQL
AS
$$
DECLARE
    _Distance INT   := 0;
    _Tile1X   INT   := 0;
    _Tile1Y   INT   := 0;
    _Tile2X   INT   := 0;
    _Tile2Y   INT   := 0;
    _DiffX    FLOAT := 0.0;
    _DiffY    FLOAT := 0.0;
BEGIN
    SELECT CalcXFromTile($1, $3) INTO _Tile1X;
    SELECT CalcYFromTile($1, $3) INTO _Tile1Y;
    SELECT CalcXFromTile($2, $3) INTO _Tile2X;
    SELECT CalcYFromTile($2, $3) INTO _Tile2Y;

    IF (_Tile1X > _Tile2X) THEN
        IF (_Tile1Y > _Tile2Y) THEN
            _DiffX := _Tile1X - _Tile2X;
            _DiffY := _Tile1Y - _Tile2Y;
        ELSE
            _DiffX := _Tile1X - _Tile2X;
            _DiffY := _Tile2Y - _Tile1Y;
        END IF;
    ELSE
        IF (_Tile1Y > _Tile2Y) THEN
            _DiffX := _Tile2X - _Tile1X;
            _DiffY := _Tile1Y - _Tile2Y;
        ELSE
            _DiffX := _Tile2X - _Tile1X;
            _DiffY := _Tile2Y - _Tile1Y;
        END IF;
    END IF;

    IF (_DiffX > _DiffY) THEN
        _Distance := (FLOOR(_DiffY * 1.5) + (_DiffX - _DiffY));
    ELSE
        _Distance := (FLOOR(_DiffY * 1.5) + (_DiffY - _DiffX));
    END IF;

    RETURN _Distance * 5;
END
$$;

CREATE OR REPLACE FUNCTION fSplit(DelimitedString VARCHAR(8000), Delimiter VARCHAR(100))
    RETURNS TABLE
            (
                ElementID INT,
                Element   VARCHAR(1000)
            )
    LANGUAGE PLPGSQL
AS
$$
DECLARE
    _Index   SMALLINT := 0;
    _Start   SMALLINT := 0;
    _DelSize SMALLINT := 0;
BEGIN
    CREATE TEMP TABLE IF NOT EXISTS temp_table
    (
        ElementID SERIAL,
        ELEMENT   VARCHAR(1000)
    ) ON COMMIT DROP;

    _DelSize := LENGTH($2);
    WHILE LENGTH(DelimitedString) > 0
        LOOP
            _Index := POSITION(Delimiter IN DelimitedString);
            IF _Index = 0 THEN
                INSERT INTO temp_table (ELEMENT) VALUES (TRIM(BOTH FROM DelimitedString));
                EXIT;
            ELSE
                INSERT INTO temp_table (ELEMENT) VALUES (TRIM(BOTH FROM SUBSTRING(DelimitedString, 1, (_Index - 1))));
                _Start := _Index + _DelSize;
                DelimitedString = SUBSTRING(DelimitedString, _Start, (LENGTH(DelimitedString) - _Start + 1));
            END IF;
        END LOOP;
    RETURN QUERY SELECT * FROM temp_table;
END;
$$;

CREATE OR REPLACE FUNCTION GetPointsOnLine(Tile1 INT, Tile2 INT, MapWidth SMALLINT)
    RETURNS TABLE
            (
                MapOrder INT
            )
    LANGUAGE PLPGSQL
AS
$$
DECLARE
    _Tile1X INT := 0;
    _Tile1Y INT := 0;
    _Tile2X INT := 0;
    _Tile2Y INT := 0;
    _SwapXY BIT := 0::BIT;
    _Temp   INT := 0;
    _DeltaX INT := 0;
    _DeltaY INT := 0;
    _Error  INT := 0;
    _Y      INT := 0;
    _YStep  INT := 0;
    _X      INT := 0;
    _Tile   INT := 0;
BEGIN

    CREATE TEMP TABLE IF NOT EXISTS temp_table
    (
        MapOrder INT
    ) ON COMMIT DROP;

    SELECT CalcXFromTile($1 - 1, $3) INTO _Tile1X;
    SELECT CalcYFromTile($1 - 1, $3) + 1 INTO _Tile1Y;
    SELECT CalcXFromTile($2 - 1, $3) INTO _Tile2X;
    SELECT CalcYFromTile($2 - 1, $3) + 1 INTO _Tile2Y;

    IF (ABS(_Tile2Y - _Tile1Y) > ABS(_Tile2X - _Tile1X)) THEN
        _SwapXY := 1::BIT;
    END IF;

    IF (_SwapXY = 1::BIT) THEN
        -- swap x and y
        _Temp := _Tile1X;
        _Tile1X := _Tile1Y;
        _Tile1Y := _Temp; -- swap x0 and y0
        _Temp := _Tile2X;
        _Tile2X := _Tile2Y;
        _Tile2Y := _Temp; -- swap x1 and y1
    END IF;

    IF (_Tile1X > _Tile2X) THEN
        -- make sure x0 < x1
        _Temp := _Tile1X;
        _Tile1X := _Tile2X;
        _Tile2X := _Temp; -- swap x0 and x1
        _Temp := _Tile1Y;
        _Tile1Y := _Tile2Y;
        _Tile2Y := _Temp; -- swap y0 and y1
    END IF;

    _DeltaX := _Tile2X - _Tile1X;
    _DeltaY := FLOOR(ABS(_Tile2Y - _Tile1Y));
    _Error := FLOOR(_DeltaX / 2.0);
    _Y := _Tile1Y;

    IF (_Tile1Y < _Tile2Y) THEN
        _YStep := 1;
    ELSE
        _YStep := -1;
    END IF;

    IF (_SwapXY = 1::BIT) THEN
        -- Y / X
        _X := _Tile1X;
        WHILE _X < (_Tile2X + 1)
            LOOP
                _Tile := OffsetIntoMap(_Y + 1, _X, $3);
                _X := _X + 1;

                INSERT INTO temp_table (MapOrder) VALUES (_Tile);

                _Error := _Error - _DeltaY;
                IF _Error < 0 THEN
                    _Y := _Y + _YStep;
                    _Error := _Error + _DeltaX;
                END IF;
            END LOOP;
    ELSE
        -- X / Y
        _X := _Tile1X;
        WHILE _X < (_Tile2X + 1)
            LOOP
                _X := _X + 1;
                _Tile := OffsetIntoMap(_X, _Y, $3);
                INSERT INTO temp_table (MapOrder) VALUES (_Tile);

                _Error := _Error - _DeltaY;
                IF _Error < 0 THEN
                    _Y := _Y + _YStep;
                    _Error := _Error + _DeltaX;
                END IF;
            END LOOP;
    END IF;
    RETURN QUERY SELECT * FROM temp_table;
END
$$;

CREATE OR REPLACE FUNCTION GetPointsOnVisionLine(Tile1 INT, Tile2 INT, MapWidth SMALLINT)
    RETURNS TABLE
            (
                MapOrder INT,
                FromCode INT
            )
    LANGUAGE PLPGSQL
AS
$$
DECLARE
    _Tile1X    INT := 0;
    _Tile1Y    INT := 0;
    _Tile2X    INT := 0;
    _Tile2Y    INT := 0;
    _Error     INT := 0;
    _Y         INT := 0;
    _YStep     INT := 0;
    _X         INT := 0;
    _Tile      INT := 0;
    _I         INT;
    _XStep     INT := 0;
    _ErrorPrev INT := 0;
    _DDY       INT := 0;
    _DDX       INT := 0;
    _DX        INT := 0;
    _DY        INT := 0;
BEGIN

    CREATE TEMP TABLE IF NOT EXISTS temp_table
    (
        MapOrder INT,
        FromCode INT
    ) ON COMMIT DROP;

    SELECT CalcXFromTile($1, $3) INTO _Tile1X;
    SELECT CalcYFromTile($1 - 1, $3) + 1 INTO _Tile1Y;
    SELECT CalcXFromTile($2, $3) INTO _Tile2X;
    SELECT CalcYFromTile($2 - 1, $3) + 1 INTO _Tile2Y;

    _Y := _Tile1Y;
    _X := _Tile1X;
    _DX := _Tile2X - _Tile1X;
    _DY := _Tile2Y - _Tile1Y;

    --POINT (y1, x1);  // first point
    _Tile := OffsetIntoMap(_Tile1X, _Tile1Y, $3);
    INSERT INTO temp_table (MapOrder, FromCode) VALUES (_Tile, 0);

    -- NB the last point can't be here, because of its previous point (which has to be verified)
    IF (_DY < 0) THEN
        _YStep := -1;
        _DY := -_DY;
    ELSE
        _YStep := 1;
    END IF;

    IF (_DX < 0) THEN
        _XStep := -1;
        _DX := -_DX;
    ELSE
        _XStep := 1;
    END IF;

    _DDY := 2 * _DY; --work with double values for full precision
    _DDX := 2 * _DX;

    IF (_DDX >= _DDY) THEN -- first octant (0 <= slope <= 1)
    -- compulsory initialization (even for errorprev, needed when dx==dy)
        _ErrorPrev := _DX;-- start in the middle of the square
        _Error := _DX;
        _I := 0;
        --for (i=0 ; i < dx ; i++){  -- do not use the first point (already done)
        WHILE _I < _DX
            LOOP
                _X := _X + _XStep;
                _Error := _Error + _DDY;

                IF _Error > _DDX THEN -- increment y if AFTER the middle ( > )
                    _Y := _Y + _YStep;
                    _Error := _Error - _DDX;
                    -- three cases (octant == right->right-top for directions below):
                    IF (_Error + _ErrorPrev) < _DDX THEN-- bottom square also
                        _Tile := OffsetIntoMap(_X, _Y, $3);
                        INSERT INTO temp_table (MapOrder, FromCode) VALUES (_Tile, 1);
                    ELSIF (_Error + _ErrorPrev) > _DDX THEN -- left square also
                    --POINT (y, x-xstep);
                        _Tile := OffsetIntoMap(_X - _XStep, _Y, $3);
                        INSERT INTO temp_table (MapOrder, FromCode) VALUES (_Tile, 2);
                    ELSE -- corner: bottom and left squares also
                    --POINT (y-ystep, x);
                        _Tile := OffsetIntoMap(_X, _Y - _YStep, $3);
                        INSERT INTO temp_table (MapOrder, FromCode) VALUES (_Tile, 3);

                        _Tile := OffsetIntoMap(_X - _XStep, _Y, $3);
                        INSERT INTO temp_table (MapOrder, FromCode) VALUES (_Tile, 4);
                    END IF;
                END IF;

                _Tile := OffsetIntoMap(_X, _Y, $3);
                INSERT INTO temp_table (MapOrder, FromCode) VALUES (_Tile, 5);

                _ErrorPrev := _Error;
                _I := _I + 1;
            END LOOP; --WHILE (@i < @dx)
    ELSE --IF (@ddx >= @ddy)
    -- the same as above
        _ErrorPrev := _DY;
        _Error := _DY;

        --for (i=0 ; i < dy ; i++){
        WHILE _I < _DY
            LOOP
                _Y := _Y + _YStep;
                _Error := _Error + _DDX;

                IF _Error > _DDY THEN
                    _X := _X + _XStep;
                    _Error := _Error - _DDY;
                    IF (_Error + _ErrorPrev) < _DDY THEN
                        --POINT (y, x-xstep);
                        _Tile := OffsetIntoMap(_X - _XStep, _Y, $3);
                        INSERT INTO temp_table (MapOrder, FromCode) VALUES (_Tile, 6);
                    ELSIF (_Error + _ErrorPrev) > _DDY THEN
                        --POINT (y-ystep, x);
                        _Tile := OffsetIntoMap(_X, _Y - _YStep, $3);
                        INSERT INTO temp_table (MapOrder, FromCode) VALUES (_Tile, 7);
                    ELSE
                        --POINT (y, x-xstep); );
                        _Tile := OffsetIntoMap(_X - _XStep, _Y, $3);
                        INSERT INTO temp_table (MapOrder, FromCode) VALUES (_Tile, 8);
                        --POINT (y-ystep, x);
                        _Tile := OffsetIntoMap(_X, _Y - _YStep, $3);
                        INSERT INTO temp_table (MapOrder, FromCode) VALUES (_Tile, 9);
                    END IF;
                END IF;

                _Tile := OffsetIntoMap(_X, _Y, $3);
                INSERT INTO temp_table (MapOrder, FromCode) VALUES (_Tile, 10);

                _ErrorPrev := _Error;
                _I := _I + 1;
            END LOOP; --WHILE (@i < @dy)
    END IF;--IF (@ddx >= @ddy)

    RETURN QUERY SELECT * FROM temp_table;
END
$$;

CREATE OR REPLACE FUNCTION AddCharacter(_CustomerGUID UUID,
                                        _UserSessionGUID UUID,
                                        _CharacterName VARCHAR(50),
                                        _ClassName VARCHAR(50))
    RETURNS TABLE
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
            )
    LANGUAGE PLPGSQL
AS
$$
DECLARE
    _ErrorRaised           BOOLEAN = FALSE;
    _SupportUnicode        BOOLEAN = FALSE;
    _UserGUID              UUID;
    _ClassID               INT;
    _CharacterID           INT;
    _CountOfCharNamesFound INT     = 0;
    _CountOfClassInventory INT     = 0;
    _InvalidCharacters     INT;

BEGIN
    CREATE TEMP TABLE IF NOT EXISTS temp_table
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
    ) ON COMMIT DROP;

    SELECT C.SupportUnicode INTO _SupportUnicode FROM Customers C WHERE C.CustomerGUID = _CustomerGUID;

    SELECT US.UserGUID
    FROM UserSessions US
    WHERE US.CustomerGUID = _CustomerGUID
      AND US.UserSessionGUID = _UserSessionGUID
    INTO _UserGUID;

    SELECT C.ClassID INTO _ClassID FROM Class C WHERE C.CustomerGUID = _CustomerGUID AND C.ClassName = _ClassName;
raise notice '_ClassID: %', _ClassID;

    SELECT CONCAT(COUNT(*), 0)
    FROM Characters C
    WHERE C.CustomerGUID = _CustomerGUID
      AND C.CharName = _CharacterName
    INTO _CountOfCharNamesFound;
    _CharacterName := TRIM(BOTH FROM _CharacterName);
    _CharacterName := REPLACE(REPLACE(REPLACE(_CharacterName, ' ', '<>'), '><', ''), '<>', ' ');
    _InvalidCharacters := LENGTH(ARRAY_TO_STRING(REGEXP_MATCHES(_CharacterName, '[^a-zA-Z0-9 ]'), ''));

    IF _InvalidCharacters > 0 AND _SupportUnicode = FALSE THEN
        INSERT INTO temp_table
        VALUES ('Character Name can only contain letters, numbers, and spaces', '', '', 0, '', 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        _ErrorRaised := TRUE;
    END IF;

    IF _ErrorRaised = FALSE AND _UserGUID IS NULL THEN
        INSERT INTO temp_table
        VALUES ('Invalid User Session', '', '', 0, '', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        _ErrorRaised := TRUE;
    END IF;

    IF _ErrorRaised = FALSE AND _ClassID IS NULL THEN
        INSERT INTO temp_table
        VALUES ('Invalid Class Name', '', '', 0, '', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        _ErrorRaised := TRUE;
    END IF;

    IF _ErrorRaised = FALSE AND _CountOfCharNamesFound > 0 THEN
        INSERT INTO temp_table
        VALUES ('Invalid Character Name', '', '', 0, '', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        _ErrorRaised := TRUE;
    END IF;

    IF _ErrorRaised = FALSE THEN

        SELECT CONCAT(COUNT(*), 0)
        FROM ClassInventory CI
        WHERE CI.CustomerGUID = _CustomerGUID
          AND CI.ClassID = _ClassID
        INTO _CountOfClassInventory;

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
                                MagicArmor, Resistance, ReloadSpeed, Range, Speed, Silver, Copper, FreeCurrency,
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

        _CharacterID := CURRVAL(PG_GET_SERIAL_SEQUENCE('characters', 'characterid'));

        INSERT INTO temp_table (CharacterName, ClassName, CharacterLevel, StartingMapName, X, Y, Z, RX, RY, RZ,
                                TeamNumber, Gold, Silver, Copper, FreeCurrency, PremiumCurrency, Fame, Alignment, Score,
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

    RETURN QUERY SELECT * FROM temp_table;
END
$$;

CREATE OR REPLACE PROCEDURE AddCharacterToMapInstanceByCharName(_CustomerGUID UUID,
                                                                _CharacterName VARCHAR(50),
                                                                _MapInstanceID INT)
    LANGUAGE PLPGSQL
AS
$$
DECLARE
    _CharacterID INT;
    _ZoneName    VARCHAR(50);
BEGIN

    INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
    VALUES (NOW(), 'AddCharacterToMapInstanceByCharName: Started', _CustomerGUID);

    SELECT CharacterID
    FROM Characters C
    WHERE C.CharName = _CharacterName
      AND C.CustomerGUID = _CustomerGUID
    INTO _CharacterID;


    IF (COALESCE(_CharacterID, 0)::INT > 0) THEN

        DELETE FROM CharOnMapInstance WHERE CharacterID = _CharacterID AND CustomerGUID = _CustomerGUID;

        INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
        VALUES (NOW(), 'AddCharacterToMapInstanceByCharName: ' || CAST(_CustomerGUID AS VARCHAR) || ' - ' ||
                       CAST(_MapInstanceID AS VARCHAR) || ' - ' || CAST(_CharacterID AS VARCHAR), _CustomerGUID);

        INSERT INTO CharOnMapInstance (CustomerGUID, MapInstanceID, CharacterID)
        VALUES (_CustomerGUID, _MapInstanceID, _CharacterID);

        SELECT ZoneName
        FROM Maps M
                 INNER JOIN MapInstances MI ON MI.CustomerGUID = M.CustomerGUID AND MI.MapID = M.MapID
        WHERE MI.MapInstanceID = _MapInstanceID
        INTO _ZoneName;

        UPDATE Characters SET MapName = _ZoneName WHERE CharacterID = _CharacterID AND CustomerGUID = _CustomerGUID;
    END IF;
END
$$;

CREATE OR REPLACE PROCEDURE AddOrUpdateCustomCharData(_CustomerGUID UUID,
                                                      _CharacterName VARCHAR(50),
                                                      _CustomFieldName VARCHAR(50),
                                                      _FieldValue TEXT)
    LANGUAGE PLPGSQL
AS
$$
DECLARE
    _CharacterID INT;
BEGIN

    SELECT CharacterID
    FROM Characters C
    WHERE C.CharName = _CharacterName
      AND C.CustomerGUID = _CustomerGUID
    INTO _CharacterID;

    INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
    VALUES (NOW(), 'AddOrUpdateCustomCharData: ' || CONCAT(_CustomFieldName, 'Empty Field Name') || ': ' ||
                   CONCAT(_CharacterName, 'Empty CharName'), _CustomerGUID);

    IF _CharacterID > 0 THEN
        IF NOT EXISTS(SELECT
                      FROM CustomCharacterData
                      WHERE CustomerGUID = _CustomerGUID
                        AND CharacterID = _CharacterID
                        AND CustomFieldName = _CustomFieldName
                          FOR UPDATE) THEN
            INSERT INTO CustomCharacterData (CustomerGUID, CharacterID, CustomFieldName, FieldValue)
            VALUES (_CustomerGUID, _CharacterID, _CustomFieldName, _FieldValue);
        ELSE
            UPDATE CustomCharacterData
            SET FieldValue=_FieldValue
            WHERE CustomerGUID = _CustomerGUID
              AND CustomFieldName = _CustomFieldName
              AND CharacterID = _CharacterID;

            INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
            VALUES (NOW(), 'AddOrUpdateCustomCharData: ' || CAST(LENGTH(_FieldValue) AS VARCHAR), _CustomerGUID);
        END IF;
    END IF;
END
$$;

CREATE OR REPLACE PROCEDURE AddOrUpdateMapZone(_CustomerGUID UUID,
                                               _MapID INT,
                                               _MapName VARCHAR(50),
                                               _MapData BYTEA,
                                               _ZoneName VARCHAR(50),
                                               _WorldCompContainsFilter VARCHAR(100),
                                               _WorldCompListFilter VARCHAR(200),
                                               _SoftPlayerCap INT,
                                               _HardPlayerCap INT,
                                               _MapMode INT
)
    LANGUAGE PLPGSQL
AS
$$
BEGIN
    IF (_WorldCompContainsFilter IS NULL) THEN
        _WorldCompContainsFilter := '';
    END IF;
    IF (_WorldCompListFilter IS NULL) THEN
        _WorldCompListFilter := '';
    END IF;
    IF NOT EXISTS(SELECT
                  FROM Maps
                  WHERE CustomerGUID = _CustomerGUID
                    AND (MapID = _MapID OR ZoneName = _ZoneName) FOR UPDATE) THEN
        RAISE INFO 'Not exists';
        INSERT INTO Maps (CustomerGUID, MapName, MapData, Width, Height, ZoneName, WorldCompContainsFilter,
                          WorldCompListFilter, SoftPlayerCap, HardPlayerCap, MapMode)
        VALUES (_CustomerGUID, _MapName, _MapData, 0, 0, _ZoneName, _WorldCompContainsFilter, _WorldCompListFilter,
                _SoftPlayerCap, _HardPlayerCap, _MapMode);
    ELSE
        RAISE INFO 'exists';
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
    END IF;
END
$$;

CREATE OR REPLACE FUNCTION AddUser(_CustomerGUID UUID,
                                   _FirstName VARCHAR(50),
                                   _LastName VARCHAR(50),
                                   _Email VARCHAR(256),
                                   _Password VARCHAR(256),
                                   _Role VARCHAR(10))
    RETURNS TABLE
            (
                UserGUID UUID
            )
    LANGUAGE PLPGSQL
AS
$$
DECLARE
    _PasswordHash VARCHAR(128);
    _UserGUID     UUID;
BEGIN
    CREATE TEMP TABLE IF NOT EXISTS temp_table
    (
        UserGUID UUID
    ) ON COMMIT DROP;

    _PasswordHash = crypt(_Password, gen_salt('md5'));
    _UserGUID := gen_random_uuid();
    INSERT INTO temp_table (UserGUID) VALUES (_UserGUID);

    INSERT INTO Users (UserGUID, CustomerGUID, FirstName, LastName, Email, PasswordHash, CreateDate, LastAccess,
                       ROLE)
    VALUES (_UserGUID, _CustomerGUID, _FirstName, _LastName, _Email, _PasswordHash, NOW(), NOW(), _Role);
    RETURN QUERY SELECT *
                 FROM temp_table;
END
$$;

CREATE OR REPLACE PROCEDURE AddNewCustomer(_CustomerName VARCHAR(50),
                                           _FirstName VARCHAR(50),
                                           _LastName VARCHAR(50),
                                           _Email VARCHAR(256),
                                           _Password VARCHAR(256)
)
    LANGUAGE PLPGSQL
AS
$$
DECLARE
    _CustomerGUID  UUID        := gen_random_uuid();
    _UserGUID      UUID;
    _ClassID       INT;
    _CharacterName VARCHAR(50) := 'Test';
    _CharacterID   INT;
BEGIN
    INSERT INTO Customers (CustomerGUID, CustomerName, CustomerEmail, CustomerPhone, CustomerNotes, EnableDebugLogging)
    VALUES (_CustomerGUID, _CustomerName, _Email, '', '', TRUE);

    INSERT INTO WorldSettings (CustomerGUID, StartTime)
    SELECT _CustomerGUID, CAST(EXTRACT(EPOCH FROM CURRENT_TIMESTAMP) AS BIGINT)
    FROM Customers C
    WHERE C.CustomerGUID = _CustomerGUID;

    SELECT UserGUID FROM AddUser(_CustomerGUID, _FirstName, _LastName, _Email, _Password, 'Developer') INTO _UserGUID;

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

    INSERT INTO CLASS (CustomerGUID, ClassName, StartingMapName, X, Y, Z, Perception, Acrobatics, Climb, Stealth, RX,
                       RY, RZ, Spirit, Magic, TeamNumber, Thirst, Hunger, Gold, Score, CharacterLevel, Gender, XP,
                       HitDie, Wounds, Size, weight, MaxHealth, Health, HealthRegenRate, MaxMana, Mana, ManaRegenRate,
                       MaxEnergy, Energy, EnergyRegenRate, MaxFatigue, Fatigue, FatigueRegenRate, MaxStamina, Stamina,
                       StaminaRegenRate, MaxEndurance, Endurance, EnduranceRegenRate, Strength, Dexterity, Constitution,
                       Intellect, Wisdom, Charisma, Agility, Fortitude, Reflex, Willpower, BaseAttack, BaseAttackBonus,
                       AttackPower, AttackSpeed, CritChance, CritMultiplier, Haste, SpellPower, SpellPenetration,
                       Defense, Dodge, Parry, Avoidance, Versatility, Multishot, Initiative, NaturalArmor,
                       PhysicalArmor, BonusArmor, ForceArmor, MagicArmor, Resistance, ReloadSpeed, RANGE, Speed, Silver,
                       Copper, FreeCurrency, PremiumCurrency, Fame, ALIGNMENT, Description)
    VALUES (_CustomerGUID, 'MaleWarrior', 'ThirdPersonExampleMap', 0, 0, 250, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0,
            1, 1, 0, 10, 0, 1, 0, 100, 50, 1, 100, 0, 1, 100, 0, 5, 100, 0, 1, 0, 0, 0, 0, 0, 0, 10, 10, 10, 10, 10, 10,
            0, 1, 1, 1, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            '');

    _ClassID := CURRVAL(PG_GET_SERIAL_SEQUENCE('class', 'classid'));

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
                            RANGE, Speed, Silver, Copper, FreeCurrency, PremiumCurrency, Fame, ALIGNMENT, Description)
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
           RANGE,
           Speed,
           Silver,
           Copper,
           FreeCurrency,
           PremiumCurrency,
           Fame,
           ALIGNMENT,
           Description
    FROM CLASS
    WHERE ClassID = _ClassID;

    _CharacterID := CURRVAL(PG_GET_SERIAL_SEQUENCE('characters', 'characterid'));

    INSERT INTO CharInventory (CustomerGUID, CharacterID, InventoryName, InventorySize)
    VALUES (_CustomerGUID, _CharacterID, 'Bag', 16);
END
$$;

CREATE OR REPLACE FUNCTION CheckMapInstanceStatus(_CustomerGUID UUID,
                                                  _MapInstanceID INT)
    RETURNS TABLE
            (
                MapInstanceStatus INT
            )
    LANGUAGE PLPGSQL
AS
$$
BEGIN
    CREATE TEMP TABLE IF NOT EXISTS temp_table
    (
        MapInstanceStatus INT
    );
    INSERT INTO DebugLog (CustomerGUID, DebugDate, DebugDesc)
    VALUES (_CustomerGUID, NOW(), 'CheckMapInstanceStatus');

    INSERT INTO temp_table
    SELECT Status
    FROM MapInstances MI
    WHERE MI.MapInstanceID = _MapInstanceID
      AND MI.CustomerGUID = _CustomerGUID;

    RETURN QUERY SELECT * FROM temp_table;
END
$$;

CREATE OR REPLACE PROCEDURE CleanUp(_CustomerGUID UUID)
    LANGUAGE PLPGSQL
AS
$$
DECLARE
    _CharacterID INT;
    _ZoneName    VARCHAR(50);
BEGIN

    DELETE
    FROM CharOnMapInstance
    WHERE CustomerGUID = _CustomerGUID
      AND CharacterID IN (SELECT C.CharacterID
                          FROM Characters C
                                   INNER JOIN Users U
                                              ON U.CustomerGUID = C.CustomerGUID
                                                  AND U.UserGUID = C.UserGUID
                          WHERE U.LastAccess < CURRENT_TIMESTAMP - (1 || ' minutes')::INTERVAL
                            AND C.CustomerGUID = _CustomerGUID);

    CREATE TEMP TABLE IF NOT EXISTS temp_CleanUp
    (
        MapInstanceID INT
    );

    INSERT INTO temp_CleanUp (MapInstanceID)
    SELECT MapInstanceID
    FROM MapInstances
    WHERE LastUpdateFromServer < CURRENT_TIMESTAMP - (2 || ' minutes')::INTERVAL
      AND CustomerGUID = _CustomerGUID;

    DELETE
    FROM CharOnMapInstance
    WHERE CustomerGUID = _CustomerGUID
      AND MapInstanceID IN (SELECT MapInstanceID FROM temp_CleanUp);

    DELETE
    FROM MapInstances
    WHERE CustomerGUID = _CustomerGUID
      AND MapInstanceID IN (SELECT MapInstanceID FROM temp_CleanUp);

END
$$;

CREATE OR REPLACE FUNCTION GetAbilityBars(_CustomerGUID UUID,
                                          _CharName VARCHAR(50))
    RETURNS TABLE
            (
                AbilityBarName            VARCHAR(50),
                CharAbilityBarID          INT,
                CharAbilityBarsCustomJSON TEXT,
                CharacterID               INT,
                CustomerGUID              UUID,
                MaxNumberOfSlots          INT,
                NumberOfUnlockedSlots     INT

            )
    LANGUAGE SQL
AS
$$
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
  AND C.CharName = _CharName
$$;

CREATE OR REPLACE FUNCTION GetAbilityBarsAndAbilities(_CustomerGUID UUID,
                                                      _CharName VARCHAR(50))
    RETURNS TABLE
            (
                AbilityBarName             VARCHAR(50),
                CharAbilityBarID           INT,
                CharAbilityBarsCustomJSON  TEXT,
                CharacterID                INT,
                CustomerGUID               UUID,
                MaxNumberOfSlots           INT,
                NumberOfUnlockedSlots      INT,
                AbilityLevel               INT,
                CharHasAbilitiesCustomJSON TEXT,
                AbilityID                  INT,
                AbilityName                VARCHAR(50),
                AbilityTypeID              INT,
                Class                      INT,
                Race                       INT,
                TextureToUseForIcon        VARCHAR(200),
                GameplayAbilityClassName   VARCHAR(200),
                AbilityCustomJSON          TEXT,
                InSlotNumber               INT
            )
    LANGUAGE SQL
AS
$$
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
  AND C.CharName = _CharName
$$;

CREATE OR REPLACE FUNCTION GetAllCharacters(_CustomerGUID UUID,
                                            _UserSessionGUID UUID)
    RETURNS TABLE
            (
                CustomerGUID              UUID,
                CharacterID               INT,
                UserGUID                  UUID,
                Email                     VARCHAR(256),
                CharName                  VARCHAR(50),
                MapName                   VARCHAR(50),
                X                         FLOAT,
                Y                         FLOAT,
                Z                         FLOAT,
                Perception                FLOAT,
                Acrobatics                FLOAT,
                Climb                     FLOAT,
                Stealth                   FLOAT,
                ServerIP                  VARCHAR(50),
                LastActivity              TIMESTAMP,
                RX                        FLOAT,
                RY                        FLOAT,
                RZ                        FLOAT,
                Spirit                    FLOAT,
                Magic                     FLOAT,
                TeamNumber                INT,
                Thirst                    FLOAT,
                Hunger                    FLOAT,
                Gold                      INT,
                Score                     INT,
                CharacterLevel            SMALLINT,
                Gender                    SMALLINT,
                XP                        INT,
                HitDie                    SMALLINT,
                Wounds                    FLOAT,
                Size                      SMALLINT,
                weight                    FLOAT,
                MaxHealth                 FLOAT,
                Health                    FLOAT,
                HealthRegenRate           FLOAT,
                MaxMana                   FLOAT,
                Mana                      FLOAT,
                ManaRegenRate             FLOAT,
                MaxEnergy                 FLOAT,
                Energy                    FLOAT,
                EnergyRegenRate           FLOAT,
                MaxFatigue                FLOAT,
                Fatigue                   FLOAT,
                FatigueRegenRate          FLOAT,
                MaxStamina                FLOAT,
                Stamina                   FLOAT,
                StaminaRegenRate          FLOAT,
                MaxEndurance              FLOAT,
                Endurance                 FLOAT,
                EnduranceRegenRate        FLOAT,
                Strength                  FLOAT,
                Dexterity                 FLOAT,
                Constitution              FLOAT,
                Intellect                 FLOAT,
                Wisdom                    FLOAT,
                Charisma                  FLOAT,
                Agility                   FLOAT,
                Fortitude                 FLOAT,
                Reflex                    FLOAT,
                Willpower                 FLOAT,
                BaseAttack                FLOAT,
                BaseAttackBonus           FLOAT,
                AttackPower               FLOAT,
                AttackSpeed               FLOAT,
                CritChance                FLOAT,
                CritMultiplier            FLOAT,
                Haste                     FLOAT,
                SpellPower                FLOAT,
                SpellPenetration          FLOAT,
                Defense                   FLOAT,
                Dodge                     FLOAT,
                Parry                     FLOAT,
                Avoidance                 FLOAT,
                Versatility               FLOAT,
                Multishot                 FLOAT,
                Initiative                FLOAT,
                NaturalArmor              FLOAT,
                PhysicalArmor             FLOAT,
                BonusArmor                FLOAT,
                ForceArmor                FLOAT,
                MagicArmor                FLOAT,
                Resistance                FLOAT,
                ReloadSpeed               FLOAT,
                Range                     FLOAT,
                Speed                     FLOAT,
                Silver                    INT,
                Copper                    INT,
                FreeCurrency              INT,
                PremiumCurrency           INT,
                Fame                      FLOAT,
                Alignment                 FLOAT,
                Description               TEXT,
                DefaultPawnClassPath      VARCHAR(200),
                IsInternalNetworkTestUser BOOLEAN,
                ClassID                   INT,
                BaseMesh                  VARCHAR(100),
                IsAdmin                   BOOLEAN,
                IsModerator               BOOLEAN,
                CreateDate                TIMESTAMP,
                LastActivityString        VARCHAR(30),
                CreateDateString          VARCHAR(30),
                ClassName                 VARCHAR(50)
            )
    LANGUAGE SQL
AS
$$
SELECT C.*,
       TO_CHAR(C.LastActivity, 'mon dd yyyy hh:miAM') AS LastActivityString,
       TO_CHAR(C.CreateDate, 'mon dd yyyy hh:miAM')   AS CreateDateString,
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
$$;

CREATE OR REPLACE FUNCTION GetCharacterAbilities(_CustomerGUID UUID,
                                                 _CharName VARCHAR(50))
    RETURNS TABLE
            (
                AbilityID                  INT,
                AbilityCustomJSON          TEXT,
                AbilityName                VARCHAR(50),
                AbilityTypeID              INT,
                Class                      INT,
                CustomerGUID               UUID,
                Race                       INT,
                TextureToUseForIcon        VARCHAR(200),
                GameplayAbilityClassName   VARCHAR(200),
                CharHasAbilitiesID         INT,
                AbilityLevel               INT,
                CharHasAbilitiesCustomJSON TEXT,
                CharacterID                INT,
                CharName                   VARCHAR(50)
            )
    LANGUAGE SQL
AS
$$
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
  AND C.CharName = _CharName
$$;

CREATE OR REPLACE FUNCTION GetCharByCharName(_CustomerGUID UUID,
                                             _CharName VARCHAR(50))
    RETURNS TABLE
            (
                CustomerGUID              UUID,
                CharacterID               INT,
                UserGUID                  UUID,
                Email                     VARCHAR(256),
                CharName                  VARCHAR(50),
                MapName                   VARCHAR(50),
                X                         FLOAT,
                Y                         FLOAT,
                Z                         FLOAT,
                Perception                FLOAT,
                Acrobatics                FLOAT,
                Climb                     FLOAT,
                Stealth                   FLOAT,
                CharacterServerIP         VARCHAR(50),
                LastActivity              TIMESTAMP,
                RX                        FLOAT,
                RY                        FLOAT,
                RZ                        FLOAT,
                Spirit                    FLOAT,
                Magic                     FLOAT,
                TeamNumber                INT,
                Thirst                    FLOAT,
                Hunger                    FLOAT,
                Gold                      INT,
                Score                     INT,
                CharacterLevel            SMALLINT,
                Gender                    SMALLINT,
                XP                        INT,
                HitDie                    SMALLINT,
                Wounds                    FLOAT,
                Size                      SMALLINT,
                Weight                    FLOAT,
                MaxHealth                 FLOAT,
                Health                    FLOAT,
                HealthRegenRate           FLOAT,
                MaxMana                   FLOAT,
                Mana                      FLOAT,
                ManaRegenRate             FLOAT,
                MaxEnergy                 FLOAT,
                Energy                    FLOAT,
                EnergyRegenRate           FLOAT,
                MaxFatigue                FLOAT,
                Fatigue                   FLOAT,
                FatigueRegenRate          FLOAT,
                MaxStamina                FLOAT,
                Stamina                   FLOAT,
                StaminaRegenRate          FLOAT,
                MaxEndurance              FLOAT,
                Endurance                 FLOAT,
                EnduranceRegenRate        FLOAT,
                Strength                  FLOAT,
                Dexterity                 FLOAT,
                Constitution              FLOAT,
                Intellect                 FLOAT,
                Wisdom                    FLOAT,
                Charisma                  FLOAT,
                Agility                   FLOAT,
                Fortitude                 FLOAT,
                Reflex                    FLOAT,
                Willpower                 FLOAT,
                BaseAttack                FLOAT,
                BaseAttackBonus           FLOAT,
                AttackPower               FLOAT,
                AttackSpeed               FLOAT,
                CritChance                FLOAT,
                CritMultiplier            FLOAT,
                Haste                     FLOAT,
                SpellPower                FLOAT,
                SpellPenetration          FLOAT,
                Defense                   FLOAT,
                Dodge                     FLOAT,
                Parry                     FLOAT,
                Avoidance                 FLOAT,
                Versatility               FLOAT,
                Multishot                 FLOAT,
                Initiative                FLOAT,
                NaturalArmor              FLOAT,
                PhysicalArmor             FLOAT,
                BonusArmor                FLOAT,
                ForceArmor                FLOAT,
                MagicArmor                FLOAT,
                Resistance                FLOAT,
                ReloadSpeed               FLOAT,
                Range                     FLOAT,
                Speed                     FLOAT,
                Silver                    INT,
                Copper                    INT,
                FreeCurrency              INT,
                PremiumCurrency           INT,
                Fame                      FLOAT,
                Alignment                 FLOAT,
                Description               TEXT,
                DefaultPawnClassPath      VARCHAR(200),
                IsInternalNetworkTestUser BOOLEAN,
                ClassID                   INT,
                BaseMesh                  VARCHAR(100),
                IsAdmin                   BOOLEAN,
                IsModerator               BOOLEAN,
                CreateDate                TIMESTAMP,
                Port                      INT,
                ServerIP                  VARCHAR(50),
                MapInstanceID             INT,
                ClassName                 VARCHAR(50),
                EnableAutoLoopBack        BOOLEAN
            )
    LANGUAGE SQL
AS
$$
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
$$;

CREATE OR REPLACE FUNCTION GetCustomCharData(_CustomerGUID UUID,
                                             _CharName VARCHAR(50))
    RETURNS TABLE
            (
                CustomerGUID          UUID,
                CustomCharacterDataID INT,
                CharacterID           INT,
                CustomFieldName       VARCHAR(50),
                FieldValue            TEXT
            )
    LANGUAGE SQL
AS
$$
SELECT CCD.*
FROM Characters C
         INNER JOIN CustomCharacterData CCD
                    ON CCD.CharacterID = C.CharacterID
                        AND CCD.CustomerGUID = C.CustomerGUID
WHERE C.CustomerGUID = _CustomerGUID
  AND C.CharName = _CharName
$$;

CREATE OR REPLACE FUNCTION GetMapInstancesForWorldServerID(_CustomerGUID UUID,
                                                           _WorldServerID INT
)
    RETURNS TABLE
            (
                CustomerGUID                UUID,
                MapInstanceID               INT,
                WorldServerID               INT,
                MapID                       INT,
                Port                        INT,
                Status                      INT,
                PlayerGroupID               INT,
                NumberOfReportedPlayers     INT,
                LastUpdateFromServer        TIMESTAMP,
                LastServerEmptyDate         TIMESTAMP,
                CreateDate                  TIMESTAMP,
                SoftPlayerCap               INT,
                HardPlayerCap               INT,
                MapName                     VARCHAR(50),
                MapMode                     INT,
                MinutesToShutdownAfterEmpty INT,
                MinutesServerHasBeenEmpty   INT,
                MinutesSinceLastUpdate      INT
            )
    LANGUAGE SQL
AS
$$
SELECT MI.*,
       M.SoftPlayerCap,
       M.HardPlayerCap,
       M.MapName,
       M.MapMode,
       M.MinutesToShutdownAfterEmpty,
       FLOOR(EXTRACT(EPOCH FROM NOW()::TIMESTAMP - MI.LastServerEmptyDate) / 60)  AS MinutesServerHasBeenEmpty,
       FLOOR(EXTRACT(EPOCH FROM NOW()::TIMESTAMP - MI.LastUpdateFromServer) / 60) AS MinutesSinceLastUpdate
FROM Maps M
         INNER JOIN MapInstances MI
                    ON MI.MapID = M.MapID
WHERE M.CustomerGUID = _CustomerGUID
  AND MI.WorldServerID = _WorldServerID
$$;

CREATE OR REPLACE FUNCTION GetPlayerGroupsCharacterIsIn(_CustomerGUID UUID,
                                                        _CharName VARCHAR(50),
                                                        _UserSessionGUID UUID,
                                                        _PlayerGroupTypeID INT = 0)
    RETURNS TABLE
            (
                PlayerGroupID     INT,
                CustomerGUID      UUID,
                PlayerGroupName   VARCHAR(50),
                PlayerGroupTypeID INT,
                ReadyState        INT,
                CreateDate        TIMESTAMP,
                DateAdded         TIMESTAMP,
                TeamNumber        INT
            )
    LANGUAGE SQL
AS
$$
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
  AND (PG.PlayerGroupTypeID = _PlayerGroupTypeID OR _PlayerGroupTypeID = 0)
  AND C.CharName = _CharName
  AND C.CustomerGUID = _CustomerGUID
$$;

CREATE OR REPLACE FUNCTION GetServerInstanceFromIPandPort(_CustomerGUID UUID,
                                                          _ServerIP VARCHAR(50),
                                                          _Port INT)
    RETURNS TABLE
            (
                MapName                 VARCHAR(50),
                ZoneName                VARCHAR(50),
                WorldCompContainsFilter VARCHAR(100),
                WorldCompListFilter     VARCHAR(200),
                MapInstanceID           INT,
                Status                  INT,
                MaxNumberOfInstances    INT,
                ActiveStartTime         TIMESTAMP,
                ServerStatus            SMALLINT,
                InternalServerIP        VARCHAR(50)
            )
    LANGUAGE SQL
AS
$$
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
$$;

CREATE OR REPLACE FUNCTION GetUser(_CustomerGUID UUID,
                                   _UserGUID UUID)
    RETURNS SETOF Users
    LANGUAGE SQL
AS
$$
SELECT *
FROM Users U
WHERE U.CustomerGUID = _CustomerGUID
  AND U.UserGUID = _UserGUID;
$$;

CREATE OR REPLACE FUNCTION GetUserSession(_CustomerGUID UUID,
                                          _UserSessionGUID UUID)
    RETURNS TABLE
            (
                CustomerGUID          UUID,
                UserGUID              UUID,
                UserSessionGUID       UUID,
                LoginDate             TIMESTAMP,
                SelectedCharacterName VARCHAR(50),
                Email                 VARCHAR(255),
                FirstName             VARCHAR(50),
                LastName              VARCHAR(50),
                CreateDate            TIMESTAMP,
                LastAccess            TIMESTAMP,
                Role                  VARCHAR(10),
                CharacterID           INT,
                CharName              VARCHAR(50),
                X                     FLOAT,
                Y                     FLOAT,
                Z                     FLOAT,
                RX                    FLOAT,
                RY                    FLOAT,
                RZ                    FLOAT,
                ZoneName              VARCHAR(50)
            )
    LANGUAGE SQL
AS
$$
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
$$;

CREATE OR REPLACE FUNCTION GetWorldStartTime(_CustomerGUID UUID)
    RETURNS TABLE
            (
                CurrentWorldTime BIGINT
            )
    LANGUAGE SQL
AS
$$
SELECT CAST(EXTRACT(EPOCH FROM CURRENT_TIMESTAMP) AS BIGINT) - WS.StartTime
           AS CurrentWorldTime
FROM WorldSettings WS
WHERE WS.CustomerGUID = _CustomerGUID;
$$;

CREATE OR REPLACE FUNCTION GetZoneInstancesOfZone(_CustomerGUID UUID,
                                                  _ZoneName VARCHAR(50))
    RETURNS TABLE
            (
                CustomerGUID                UUID,
                MapID                       INT,
                MapName                     VARCHAR(50),
                MapData                     BYTEA,
                Width                       SMALLINT,
                Height                      SMALLINT,
                ZoneName                    VARCHAR(50),
                WorldCompContainsFilter     VARCHAR(100),
                WorldCompListFilter         VARCHAR(200),
                MapMode                     INT,
                SoftPlayerCap               INT,
                HardPlayerCap               INT,
                MinutesToShutdownAfterEmpty INT,
                InstanceCustomerGUID        UUID,
                MapInstanceID               INT,
                WorldServerID               INT,
                InstanceMapID               INT,
                Port                        INT,
                Status                      INT,
                PlayerGroupID               INT,
                NumberOfReportedPlayers     INT,
                LastUpdateFromServer        TIMESTAMP,
                LastServerEmptyDate         TIMESTAMP,
                CreateDate                  TIMESTAMP
            )
    LANGUAGE SQL
AS
$$
SELECT *
FROM Maps M
         INNER JOIN MapInstances MI
                    ON MI.MapID = M.MapID
WHERE M.CustomerGUID = _CustomerGUID
  AND M.ZoneName = _ZoneName
$$;

CREATE OR REPLACE FUNCTION SpinUpMapInstance(_CustomerGUID UUID,
                                             _ZoneName VARCHAR(50),
                                             _PlayerGroupID INT)
    RETURNS TABLE
            (
                ServerIP        VARCHAR(50),
                WorldServerID   INT,
                WorldServerIP   VARCHAR(50),
                WorldServerPort INT,
                Port            INT,
                MapInstanceID   INT
            )
    LANGUAGE PLPGSQL
AS
$$
DECLARE
    _ServerIP                VARCHAR(50);
    _WorldServerID           INT;
    _WorldServerIP           VARCHAR(50);
    _WorldServerPort         INT;
    _Port                    INT;
    _MapInstanceID           INT;
    _MapID                   INT;
    _MapMode                 INT;
    _MaxNumberOfInstances    INT;
    _StartingMapInstancePort INT;
BEGIN
    CREATE TEMP TABLE IF NOT EXISTS temp_SpinUpMapInstance
    (
        ServerIP        VARCHAR(50),
        WorldServerID   INT,
        WorldServerIP   VARCHAR(50),
        WorldServerPort INT,
        Port            INT,
        MapInstanceID   INT
    ) ON COMMIT DROP;

    INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
    VALUES (NOW(), 'Attempting to Spin Up Map Instance: ' || _ZoneName, _CustomerGUID);

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
      AND WS.ServerStatus = 1 --Active
      AND WS.ActiveStartTime IS NOT NULL
    GROUP BY WS.WorldServerID, WS.ServerIP, WS.InternalServerIP, WS.Port, WS.MaxNumberOfInstances,
             WS.StartingMapInstancePort
    ORDER BY CONCAT(COUNT(MI.MapInstanceID), 0)
    LIMIT 1;

    IF (_WorldServerID IS NULL) THEN
        INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
        VALUES (NOW(), 'Cannot find World Server!', _CustomerGUID);

        _MapInstanceID := -1;
    ELSE
        WITH RECURSIVE Num(Pos) AS
                           (SELECT CAST(_StartingMapInstancePort AS INT)
                            UNION ALL
                            SELECT CAST(Pos + 1 AS INT)
                            FROM Num
                            WHERE Pos < _StartingMapInstancePort + CONCAT(_MaxNumberOfInstances, 10)::INT)
        SELECT MIN(OpenPort)
        INTO _Port
        FROM (SELECT tmpA.Pos AS OpenPort, MI.Port AS PortInUse
              FROM Num tmpA
                       LEFT JOIN MapInstances MI ON MI.WorldServerID = _WorldServerID
                  AND MI.CustomerGUID = _CustomerGUID
                  AND MI.Port = tmpA.Pos) tmpB
        WHERE tmpB.PortInUse IS NULL;

    END IF;

    IF (_PlayerGroupID > 0 AND _MapMode = 2) --_MapMode = 2 is a dungeon instance server
    THEN
        INSERT INTO MapInstances (CustomerGUID, WorldServerID, MapID, Port, Status, PlayerGroupID, LastUpdateFromServer)
        VALUES (_CustomerGUID, _WorldServerID, _MapID, _Port, 1, _PlayerGroupID, NOW()); --Status 1 = Loading;
    ELSE
        INSERT INTO MapInstances (CustomerGUID, WorldServerID, MapID, Port, Status, LastUpdateFromServer)
        VALUES (_CustomerGUID, _WorldServerID, _MapID, _Port, 1, NOW()); --Status 1 = Loading;
    END IF;

    _MapInstanceID := CURRVAL(PG_GET_SERIAL_SEQUENCE('mapinstances', 'mapinstanceid'));

    INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
    VALUES (NOW(), 'Successfully Spun Up Map Instance: ' || _ZoneName || ' ServerIP: ' || _ServerIP || ' Port: ' ||
                   CAST(_Port AS VARCHAR) || ' MapInstanceID: ' || CAST(_MapInstanceID AS VARCHAR), _CustomerGUID);

    INSERT INTO temp_SpinUpMapInstance (ServerIP, WorldServerID, WorldServerIP, WorldServerPort, Port, MapInstanceID)
    VALUES (_ServerIP, _WorldServerID, _WorldServerIP, _WorldServerPort, _Port, _MapInstanceID);
    RETURN QUERY SELECT * FROM temp_SpinUpMapInstance;
END;
$$;

CREATE OR REPLACE FUNCTION JoinMapByCharName(_CustomerGUID UUID,
                                             _CharName VARCHAR(50),
                                             _ZoneName VARCHAR(50),
                                             _PlayerGroupType INT)
    RETURNS TABLE
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
            )
    LANGUAGE PLPGSQL
AS
$$
DECLARE
    _MapID                     INT;
    _MapNameToStart            VARCHAR(50);
    _CharacterID               INT;
    _Email                     VARCHAR(255);
    _SoftPlayerCap             INT;
    _PlayerGroupID             INT;
    _ServerIP                  VARCHAR(50);
    _WorldServerID             INT;
    _WorldServerIP             VARCHAR(50);
    _WorldServerPort           INT;
    _Port                      INT;
    _MapInstanceID             INT;
    _MapInstanceStatus         INT;
    _NeedToStartUpMap          BOOLEAN;
    _EnableAutoLoopBack        BOOLEAN;
    _NoPortForwarding          BOOLEAN;
    _IsInternalNetworkTestUser BOOLEAN := FALSE;
    _ErrorRaised               BOOLEAN := FALSE;
BEGIN
    CREATE TEMP TABLE IF NOT EXISTS temp_table
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
    ) ON COMMIT DROP;

    --Run Cleanup here for now.  Later this can get moved to a scheduler to run periodically.
    CALL CleanUp(_CustomerGUID);

    INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
    VALUES (NOW(), 'JoinMapByCharName: ' || _ZoneName || ' - ' || _CharName, _CustomerGUID);

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

        _NeedToStartUpMap := 0;
        _ErrorRaised := TRUE;
    END IF;

    IF _ErrorRaised = FALSE THEN
        SELECT C.EnableAutoLoopBack, C.NoPortForwarding
        INTO _EnableAutoLoopBack, _NoPortForwarding
        FROM Customers C
        WHERE C.CustomerGUID = _CustomerGUID;
    END IF;

    IF _ErrorRaised = FALSE AND (_PlayerGroupType > 0) THEN
        SELECT COALESCE(PG.PlayerGroupID, 0)
        FROM PlayerGroupCharacters PGC
                 INNER JOIN PlayerGroup PG
                            ON PG.PlayerGroupID = PGC.PlayerGroupID
        WHERE PGC.CustomerGUID = _CustomerGUID
          AND PGC.CharacterID = _CharacterID
          AND PG.PlayerGroupTypeID = _PlayerGroupType
        INTO _PlayerGroupID;
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
          AND (MI.PlayerGroupID = _PlayerGroupID OR _PlayerGroupID = 0) --Only lookup map instances that match the player group fro this Player Group Type or lookup all if zero
          AND MI.Status = 2
        GROUP BY MI.MapInstanceID, WS.ServerIP, MI.Port, WS.WorldServerID, WS.InternalServerIP, WS.Port, MI.Status
        ORDER BY COUNT(DISTINCT CMI.CharacterID);


        --There is a map already running to connect to
        IF _MapInstanceID IS NOT NULL THEN
            /*IF (POSITION('\@localhost' IN _Email) > 0) THEN
                _ServerIP := '127.0.0.1';
            END IF;*/

            _NeedToStartUpMap := FALSE;

            INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
            VALUES (NOW(), 'Joined Existing Map: ' || COALESCE(_ZoneName, '') || ' - ' || COALESCE(_CharName, '') ||
                           ' - ' || COALESCE(_ServerIP, ''),
                    _CustomerGUID);
        ELSE --Spin up a new map

            SELECT *
            FROM SpinUpMapInstance(_CustomerGUID, _ZoneName, _PlayerGroupID)
            INTO _ServerIP , _WorldServerID , _WorldServerIP , _WorldServerPort , _Port, _MapInstanceID;

            /*IF (POSITION('@localhost' IN _Email) > 0 OR _IsInternalNetworkTestUser = TRUE) THEN
                _ServerIP := '127.0.0.1';
            END IF;*/

            _NeedToStartUpMap := TRUE;

            INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
            VALUES (NOW(),
                    'SpinUpMapInstance returned: ' || COALESCE(_ZoneName, '') || ' CharName: ' ||
                    COALESCE(_CharName, '') || ' ServerIP: ' ||
                    COALESCE(_ServerIP, '') ||
                    ' WorldServerPort: ' || CAST(COALESCE(_WorldServerPort, -1) AS VARCHAR), _CustomerGUID);


            INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
            VALUES (NOW(),
                    'JoinMapByCharName returned: ' || COALESCE(_MapNameToStart, '[NoMapName]') || ' MapInstanceID: ' ||
                    CAST(COALESCE(_MapInstanceID, -1) AS VARCHAR) || ' MapInstanceStatus: ' ||
                    CAST(COALESCE(_MapInstanceStatus, -1) AS VARCHAR) || ' NeedToStartUpMap: ' ||
                    CAST(_NeedToStartUpMap AS VARCHAR) || ' EnableAutoLoopBack: ' ||
                    CAST(_EnableAutoLoopBack AS VARCHAR) ||
                    ' ServerIP: ' || COALESCE(_ServerIP, '') || ' WorldServerIP: ' || COALESCE(_WorldServerIP, ''),
                    _CustomerGUID);
        END IF;
    END IF;
    INSERT INTO temp_table(ServerIP, WorldServerID, WorldServerIP, WorldServerPort, Port, MapInstanceID, MapNameToStart,
                           MapInstanceStatus, NeedToStartUpMap, EnableAutoLoopBack, NoPortForwarding)
    VALUES (_ServerIP, _WorldServerID, _WorldServerIP, _WorldServerPort, _Port, _MapInstanceID, _MapNameToStart,
            _MapInstanceStatus, _NeedToStartUpMap, _EnableAutoLoopBack, _NoPortForwarding);
    RETURN QUERY SELECT * FROM temp_table;
END;
$$;

CREATE OR REPLACE FUNCTION PlayerLoginAndCreateSession(_CustomerGUID UUID,
                                                       _Email VARCHAR(256),
                                                       _Password VARCHAR(256),
                                                       _DontCheckPassword BOOLEAN = FALSE
)
    RETURNS TABLE
            (
                Authenticated   BOOLEAN,
                UserSessionGUID UUID
            )
    LANGUAGE PLPGSQL
AS
$$
DECLARE
    _Authenticated   BOOLEAN = FALSE;
    _PasswordCheck   BOOLEAN = FALSE;
    _HashInDB        VARCHAR(128);
    _HashToCheck     VARCHAR(128);
    _UserGUID        UUID;
    _UserSessionGUID UUID;
BEGIN

    CREATE TEMP TABLE IF NOT EXISTS temp_table
    (
        Authenticated   BOOLEAN,
        UserSessionGUID UUID
    ) ON COMMIT DROP;

    SELECT (PasswordHash = crypt(_Password, PasswordHash)), UserGUID
    INTO _PasswordCheck, _UserGUID
    FROM Users
    WHERE CustomerGUID = _CustomerGUID
      AND Email = _Email
      AND ROLE = 'Player';

    IF (_PasswordCheck = TRUE OR _DontCheckPassword = TRUE) THEN
        _Authenticated := TRUE;
        DELETE FROM UserSessions WHERE UserGUID = _UserGUID;
        _UserSessionGUID := gen_random_uuid();
        INSERT INTO UserSessions (CustomerGUID, UserSessionGUID, UserGUID, LoginDate)
        VALUES (_CustomerGUID, _UserSessionGUID, _UserGUID, NOW());
    END IF;
    INSERT INTO temp_table (Authenticated, UserSessionGUID) VALUES (_Authenticated, _UserSessionGUID);

    RETURN QUERY SELECT * FROM temp_table;

END
$$;

CREATE OR REPLACE PROCEDURE PlayerLogOut(_CustomerGUID UUID,
                                         _CharName VARCHAR(50)
)
    LANGUAGE PLPGSQL
AS
$$
DECLARE
    _CharacterID INT;
BEGIN

    SELECT CharacterID
    INTO _CharacterID
    FROM Characters WC
    WHERE WC.CharName = _CharName
      AND WC.CustomerGUID = _CustomerGUID;

    INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
    VALUES (NOW(), 'PlayerLogOut: CharName: ' || _CharName, _CustomerGUID);

    DELETE FROM CharOnMapInstance WHERE CharacterID = _CharacterID;
    --DELETE FROM PlayerGroupCharacters WHERE CharacterID=_CharacterID

END
$$;

CREATE OR REPLACE PROCEDURE RemoveCharacter(_CustomerGUID UUID,
                                            _UserSessionGUID UUID,
                                            _CharacterName VARCHAR(50)
)
    LANGUAGE PLPGSQL
AS
$$
DECLARE
    _UserGUID    UUID;
    _CharacterID INT;
BEGIN

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
END
$$;

CREATE OR REPLACE PROCEDURE SetMapInstanceStatus(_MapInstanceID INT,
                                                 _MapInstanceStatus INT
)
    LANGUAGE PLPGSQL
AS
$$
DECLARE
    _CustomerGUID UUID;
    _MapName      VARCHAR(50);
BEGIN

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
    VALUES (NOW(), 'SetMapInstanceStatus: ' || _MapName, _CustomerGUID);
END
$$;

CREATE OR REPLACE PROCEDURE ShutdownMapInstance(
    _CustomerGUID UUID,
    _MapInstanceID INT
)
    LANGUAGE PLPGSQL
AS
$$
BEGIN
    DELETE
    FROM CharOnMapInstance
    WHERE MapInstanceID = _MapInstanceID
      AND CustomerGUID = _CustomerGUID;

    DELETE
    FROM MapInstances
    WHERE MapInstanceID = _MapInstanceID
      AND CustomerGUID = _CustomerGUID;
END
$$;

CREATE OR REPLACE PROCEDURE ShutdownWorldServer(_CustomerGUID UUID,
                                                _WorldServerID INT)
    LANGUAGE PLPGSQL
AS
$$
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
    VALUES (NOW(), 'ShutdownWorldServer: ' || CAST(_WorldServerID AS VARCHAR), _CustomerGUID);
END
$$;

CREATE OR REPLACE FUNCTION StartWorldServer(_CustomerGUID UUID,
                                            _ServerIP VARCHAR(50))
    RETURNS TABLE
            (
                WorldServerID INT
            )
    LANGUAGE PLPGSQL
AS
$$
DECLARE
    _WorldServerID INT;
BEGIN
    CREATE TEMP TABLE IF NOT EXISTS temp_table
    (
        WorldServerID INT
    ) ON COMMIT DROP;

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
        VALUES (NOW(), 'StartWorldServer Success: ' || CAST(_WorldServerID AS VARCHAR), _CustomerGUID);
    ELSIF (_ServerIP = '::1') THEN
        SELECT WS.WorldServerID INTO _WorldServerID FROM WorldServers WS WHERE WS.CustomerGUID = _CustomerGUID LIMIT 1;

        UPDATE WorldServers WS
        SET ActiveStartTime=NOW(),
            ServerStatus=1
        WHERE CustomerGUID = _CustomerGUID
          AND WS.WorldServerID = _WorldServerID;
        --_WorldServerID := -1;

        INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
        VALUES (NOW(), 'StartWorldServer Success (Local): ' || CAST(_WorldServerID AS VARCHAR) || ' IncomingIP: ' ||
                       CAST(_ServerIP AS VARCHAR), _CustomerGUID);
    ELSE
        INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
        VALUES (NOW(), 'StartWorldServer Failed: ' || CAST(_WorldServerID AS VARCHAR) || ' IncomingIP: ' ||
                       CAST(_ServerIP AS VARCHAR), _CustomerGUID);
    END IF;

    INSERT INTO temp_table(WorldServerID) VALUES (_WorldServerID);
    RETURN QUERY SELECT * FROM temp_table;
END
$$;

CREATE OR REPLACE PROCEDURE UpdateCharacterStats(_CustomerGUID UUID,
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
                                                 _Score INT
)
    LANGUAGE PLPGSQL
AS
$$
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
        Range=_Range,
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
    VALUES (NOW(), 'UpdateCharacterStats: ' || _CharName, _CustomerGUID);
END
$$;

CREATE OR REPLACE PROCEDURE UpdateNumberOfPlayers(_CustomerGUID UUID,
                                                  _IP VARCHAR(50),
                                                  _Port INT,
                                                  _NumberOfReportedPlayers INT
)
    LANGUAGE PLPGSQL
AS
$$
DECLARE
    _LastNumberOfReportedPlayers INT;
BEGIN

    SELECT MI.NumberOfReportedPlayers
    INTO _LastNumberOfReportedPlayers
    FROM MapInstances MI
             INNER JOIN WorldServers WS
                        ON WS.WorldServerID = MI.WorldServerID
    WHERE (WS.ServerIP = _IP OR WS.InternalServerIP = _IP)
      AND MI.Port = _Port;

    --INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
    --VALUES (now(), 'UpdateNumberOfPlayers: ' || _IP || ':' || CONVERT(varchar, _Port) || ' #:' || CONVERT(varchar, _NumberOfReportedPlayers) || ' - ' || CONVERT(varchar, _LastNumberOfReportedPlayers), _CustomerGUID);

    IF (_LastNumberOfReportedPlayers > 0 AND _NumberOfReportedPlayers = 0) THEN
        UPDATE MapInstances MI
        SET NumberOfReportedPlayers=_NumberOfReportedPlayers,
            LastUpdateFromServer=NOW(),
            LastServerEmptyDate=NOW()
        FROM WorldServers WS
        WHERE WS.WorldServerID = MI.WorldServerID
          AND (WS.ServerIP = _IP OR WS.InternalServerIP = _IP)
          AND MI.Port = _Port;
    ELSIF (_LastNumberOfReportedPlayers = 0 AND _NumberOfReportedPlayers > 0) THEN
        UPDATE MapInstances MI
        SET NumberOfReportedPlayers=_NumberOfReportedPlayers,
            LastUpdateFromServer=NOW(),
            LastServerEmptyDate=NULL
        FROM WorldServers WS
        WHERE WS.WorldServerID = MI.WorldServerID
          AND (WS.ServerIP = _IP OR WS.InternalServerIP = _IP)
          AND MI.Port = _Port;
    ELSE
        UPDATE MapInstances MI
        SET NumberOfReportedPlayers=_NumberOfReportedPlayers,
            LastUpdateFromServer=NOW()
        FROM WorldServers WS
        WHERE WS.WorldServerID = MI.WorldServerID
          AND (WS.ServerIP = _IP OR WS.InternalServerIP = _IP)
          AND MI.Port = _Port;
    END IF;
END
$$;

CREATE OR REPLACE PROCEDURE UpdatePositionOfCharacterByName(_CustomerGUID UUID,
                                                            _CharName VARCHAR(50),
                                                            _MapName VARCHAR(50),
                                                            _X FLOAT,
                                                            _Y FLOAT,
                                                            _Z FLOAT,
                                                            _RX FLOAT,
                                                            _RY FLOAT,
                                                            _RZ FLOAT
)
    LANGUAGE PLPGSQL
AS
$$
BEGIN
    IF (_MapName <> '') THEN
        UPDATE Characters
        SET
            --MapName=_MapName,
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
    FROM Characters C
    WHERE CharName = _CharName
      AND Users.CustomerGUID = _CustomerGUID
      AND Users.UserGUID = C.UserGUID;
/*
INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
VALUES (now(), 'UpdatePosition: ' || _CharName || ' - ' || ISNULL(_MapName,'None'), _CustomerGUID);
*/
END
$$;

CREATE OR REPLACE PROCEDURE UserSessionSetSelectedCharacter(_CustomerGUID UUID,
                                                            _UserSessionGUID UUID,
                                                            _SelectedCharacterName VARCHAR(50)
)
    LANGUAGE PLPGSQL
AS
$$
BEGIN
    UPDATE UserSessions
    SET SelectedCharacterName=_SelectedCharacterName
    WHERE CustomerGUID = _CustomerGUID
      AND UserSessionGUID = _UserSessionGUID;
END
$$;

ALTER TABLE WorldServers
ADD ZoneServerGUID UUID NULL;

ALTER TABLE WorldServers
ADD CONSTRAINT AK_ZoneServers UNIQUE (CustomerGUID, ZoneServerGUID);


CREATE OR REPLACE PROCEDURE AddOrUpdateAbility(_CustomerGUID UUID,
                                               _AbilityID INT,
                                               _AbilityName VARCHAR(50),
                                               _AbilityTypeID INT,
                                               _TextureToUseForIcon VARCHAR(200),
                                               _Class INT,
                                               _Race INT,
                                               _GameplayAbilityClassName VARCHAR(200),
                                               _AbilityCustomJSON TEXT)
    LANGUAGE PLPGSQL
AS
$$
BEGIN

    IF
        NOT EXISTS(SELECT
                   FROM Abilities AB
                   WHERE AB.CustomerGUID = _CustomerGUID
                     AND (AB.AbilityID = _AbilityID
                       OR AB.AbilityName = _AbilityName)
                       FOR UPDATE) THEN
        INSERT INTO Abilities (CustomerGUID, AbilityName, AbilityTypeID, TextureToUseForIcon, Class, Race,
                               GameplayAbilityClassName, AbilityCustomJSON)
        VALUES (_CustomerGUID, _AbilityName, _AbilityTypeID, _TextureToUseForIcon, _Class, _Race,
                _GameplayAbilityClassName, _AbilityCustomJSON);
    ELSE
        UPDATE Abilities AB
        SET AbilityName              = _AbilityName,
            AbilityTypeID            = _AbilityTypeID,
            TextureToUseForIcon      = _TextureToUseForIcon,
            Class                    = _Class,
            Race                     = _Race,
            GameplayAbilityClassName = _GameplayAbilityClassName,
            AbilityCustomJSON        = _AbilityCustomJSON
        WHERE AB.CustomerGUID = _CustomerGUID
          AND AB.AbilityID = _AbilityID;
    END IF;
END
$$;


CREATE OR REPLACE PROCEDURE AddOrUpdateAbilityType(_CustomerGUID UUID,
                                                   _AbilityTypeID INT,
                                                   _AbilityTypeName VARCHAR(50))
    LANGUAGE PLPGSQL
AS
$$
BEGIN

    IF
        NOT EXISTS(SELECT
                   FROM AbilityTypes ABT
                   WHERE ABT.CustomerGUID = _CustomerGUID
                     AND (ABT.AbilityTypeID = _AbilityTypeID
                       OR ABT.AbilityTypeName = _AbilityTypeName)
                       FOR UPDATE) THEN
        INSERT INTO AbilityTypes (CustomerGUID, AbilityTypeName)
        VALUES (_CustomerGUID, _AbilityTypeName);
    ELSE
        UPDATE AbilityTypes ABT
        SET AbilityTypeName = _AbilityTypeName
        WHERE ABT.CustomerGUID = _CustomerGUID
          AND ABT.AbilityTypeID = _AbilityTypeID;
    END IF;
END
$$;


CREATE OR REPLACE FUNCTION GetAbilityTypes(_CustomerGUID UUID)
    RETURNS TABLE
            (
                AbilityTypeID     INT,
                AbilityTypeName   VARCHAR(50),
                CustomerGUID      UUID,
                NumberOfAbilities INT
            )
    LANGUAGE SQL
AS
$$
SELECT *
     , (SELECT COUNT(*) FROM Abilities AB WHERE AB.AbilityTypeID = ABT.AbilityTypeID) AS NumberOfAbilities
FROM AbilityTypes ABT
WHERE ABT.CustomerGUID = _CustomerGUID
ORDER BY AbilityTypeName;
$$;


CREATE OR REPLACE PROCEDURE AddAbilityToCharacter(_CustomerGUID UUID,
                                                  _AbilityName VARCHAR(50),
                                                  _CharacterName VARCHAR(50),
                                                  _AbilityLevel INT,
                                                  _CharHasAbilitiesCustomJSON TEXT)
    LANGUAGE PLPGSQL
AS
$$
BEGIN
    IF NOT EXISTS(SELECT
                  FROM CharHasAbilities CHA
                           INNER JOIN Characters C
                                      ON C.CharacterID = CHA.CharacterID
                                          AND C.CustomerGUID = CHA.CustomerGUID
                           INNER JOIN Abilities A
                                      ON A.AbilityID = CHA.AbilityID
                                          AND A.CustomerGUID = CHA.CustomerGUID
                  WHERE CHA.CustomerGUID = _CustomerGUID
                    AND C.CharName = _CharacterName
                    AND A.AbilityName = _AbilityName FOR UPDATE) THEN
        INSERT INTO CharHasAbilities (CustomerGUID, CharacterID, AbilityID, AbilityLevel, CharHasAbilitiesCustomJSON)
        SELECT _CustomerGUID AS CustomerGUID,
               (SELECT C.CharacterID
                FROM Characters C
                WHERE C.CharName = _CharacterName AND C.CustomerGUID = _CustomerGUID
                LIMIT 1),
               (SELECT A.AbilityID
                FROM Abilities A
                WHERE A.AbilityName = _AbilityName AND A.CustomerGUID = _CustomerGUID
                LIMIT 1),
               _AbilityLevel,
               _CharHasAbilitiesCustomJSON;
    END IF;
END
$$;


CREATE OR REPLACE PROCEDURE AddNewCustomer(_CustomerName VARCHAR(50),
                                           _FirstName VARCHAR(50),
                                           _LastName VARCHAR(50),
                                           _Email VARCHAR(256),
                                           _Password VARCHAR(256),
                                           _CustomerGuid UUID)
    LANGUAGE PLPGSQL
AS
$$
DECLARE
    _UserGUID      UUID;
    _ClassID       INT;
    _CharacterName VARCHAR(50) := 'Test';
    _CharacterID   INT;
BEGIN
    IF _CustomerGUID IS NULL THEN
            _CustomerGUID := gen_random_uuid();
    END IF;

    IF NOT EXISTS(SELECT
              FROM Customers
              WHERE CustomerGUID = _CustomerGUID)
        THEN

        INSERT INTO Customers (CustomerGUID, CustomerName, CustomerEmail, CustomerPhone, CustomerNotes, EnableDebugLogging)
        VALUES (_CustomerGUID, _CustomerName, _Email, '', '', TRUE);

        INSERT INTO WorldSettings (CustomerGUID, StartTime)
        SELECT _CustomerGUID, CAST(EXTRACT(EPOCH FROM CURRENT_TIMESTAMP) AS BIGINT)
        FROM Customers C
        WHERE C.CustomerGUID = _CustomerGUID;

        SELECT UserGUID FROM AddUser(_CustomerGUID, _FirstName, _LastName, _Email, _Password, 'Developer') INTO _UserGUID;

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

        INSERT INTO CLASS (CustomerGUID, ClassName, StartingMapName, X, Y, Z, Perception, Acrobatics, Climb, Stealth, RX,
                           RY, RZ, Spirit, Magic, TeamNumber, Thirst, Hunger, Gold, Score, CharacterLevel, Gender, XP,
                           HitDie, Wounds, Size, weight, MaxHealth, Health, HealthRegenRate, MaxMana, Mana, ManaRegenRate,
                           MaxEnergy, Energy, EnergyRegenRate, MaxFatigue, Fatigue, FatigueRegenRate, MaxStamina, Stamina,
                           StaminaRegenRate, MaxEndurance, Endurance, EnduranceRegenRate, Strength, Dexterity, Constitution,
                           Intellect, Wisdom, Charisma, Agility, Fortitude, Reflex, Willpower, BaseAttack, BaseAttackBonus,
                           AttackPower, AttackSpeed, CritChance, CritMultiplier, Haste, SpellPower, SpellPenetration,
                           Defense, Dodge, Parry, Avoidance, Versatility, Multishot, Initiative, NaturalArmor,
                           PhysicalArmor, BonusArmor, ForceArmor, MagicArmor, Resistance, ReloadSpeed, RANGE, Speed, Silver,
                           Copper, FreeCurrency, PremiumCurrency, Fame, ALIGNMENT, Description)
        VALUES (_CustomerGUID, 'MaleWarrior', 'ThirdPersonExampleMap', 0, 0, 250, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0,
                1, 1, 0, 10, 0, 1, 0, 100, 50, 1, 100, 0, 1, 100, 0, 5, 100, 0, 1, 0, 0, 0, 0, 0, 0, 10, 10, 10, 10, 10, 10,
                0, 1, 1, 1, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                '');

        _ClassID := CURRVAL(PG_GET_SERIAL_SEQUENCE('class', 'classid'));

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
                                RANGE, Speed, Silver, Copper, FreeCurrency, PremiumCurrency, Fame, ALIGNMENT, Description)
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
               RANGE,
               Speed,
               Silver,
               Copper,
               FreeCurrency,
               PremiumCurrency,
               Fame,
               ALIGNMENT,
               Description
        FROM CLASS
        WHERE ClassID = _ClassID;

        _CharacterID := CURRVAL(PG_GET_SERIAL_SEQUENCE('characters', 'characterid'));

        INSERT INTO CharInventory (CustomerGUID, CharacterID, InventoryName, InventorySize)
        VALUES (_CustomerGUID, _CharacterID, 'Bag', 16);
    ELSE
        RAISE 'Duplicate Customer GUID: %', _CustomerGUID USING ERRCODE = 'unique_violation';
    END IF;
END
$$;



CREATE OR REPLACE FUNCTION JoinMapByCharName(_CustomerGUID UUID,
                                             _CharName VARCHAR(50),
                                             _ZoneName VARCHAR(50),
                                             _PlayerGroupType INT)
    RETURNS TABLE
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
            )
    LANGUAGE PLPGSQL
AS
$$
DECLARE
    _MapID                     INT;
    _MapNameToStart            VARCHAR(50);
    _CharacterID               INT;
    _Email                     VARCHAR(255);
    _SoftPlayerCap             INT;
    _PlayerGroupID             INT;
    _ServerIP                  VARCHAR(50);
    _WorldServerID             INT;
    _WorldServerIP             VARCHAR(50);
    _WorldServerPort           INT;
    _Port                      INT;
    _MapInstanceID             INT;
    _MapInstanceStatus         INT;
    _NeedToStartUpMap          BOOLEAN;
    _EnableAutoLoopBack        BOOLEAN;
    _NoPortForwarding          BOOLEAN;
    _IsInternalNetworkTestUser BOOLEAN := FALSE;
    _ErrorRaised               BOOLEAN := FALSE;
BEGIN
    CREATE TEMP TABLE IF NOT EXISTS temp_table
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
    ) ON COMMIT DROP;

    --Run Cleanup here for now.  Later this can get moved to a scheduler to run periodically.
    CALL CleanUp(_CustomerGUID);

    INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
    VALUES (NOW(), 'JoinMapByCharName: ' || _ZoneName || ' - ' || _CharName, _CustomerGUID);

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

        _NeedToStartUpMap := 0;
        _ErrorRaised := TRUE;
    END IF;

    IF _ErrorRaised = FALSE THEN
        SELECT C.EnableAutoLoopBack, C.NoPortForwarding
        INTO _EnableAutoLoopBack, _NoPortForwarding
        FROM Customers C
        WHERE C.CustomerGUID = _CustomerGUID;
    END IF;

    IF _ErrorRaised = FALSE AND (_PlayerGroupType > 0) THEN
        SELECT COALESCE(PG.PlayerGroupID, 0)
        FROM PlayerGroupCharacters PGC
                 INNER JOIN PlayerGroup PG
                            ON PG.PlayerGroupID = PGC.PlayerGroupID
        WHERE PGC.CustomerGUID = _CustomerGUID
          AND PGC.CharacterID = _CharacterID
          AND PG.PlayerGroupTypeID = _PlayerGroupType
        INTO _PlayerGroupID;
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
          AND (MI.PlayerGroupID = _PlayerGroupID OR COALESCE(_PlayerGroupID,0) = 0) --Only lookup map instances that match the player group fro this Player Group Type or lookup all if zero
          AND MI.Status = 2
        GROUP BY MI.MapInstanceID, WS.ServerIP, MI.Port, WS.WorldServerID, WS.InternalServerIP, WS.Port, MI.Status
        ORDER BY COUNT(DISTINCT CMI.CharacterID);


        --There is a map already running to connect to
        IF _MapInstanceID IS NOT NULL THEN
            /*IF (POSITION('\@localhost' IN _Email) > 0) THEN
                _ServerIP := '127.0.0.1';
            END IF;*/

            _NeedToStartUpMap := FALSE;

            INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
            VALUES (NOW(), 'Joined Existing Map: ' || COALESCE(_ZoneName, '') || ' - ' || COALESCE(_CharName, '') ||
                           ' - ' || COALESCE(_ServerIP, ''),
                    _CustomerGUID);
        ELSE --Spin up a new map

            SELECT *
            FROM SpinUpMapInstance(_CustomerGUID, _ZoneName, _PlayerGroupID)
            INTO _ServerIP , _WorldServerID , _WorldServerIP , _WorldServerPort , _Port, _MapInstanceID;

            /*IF (POSITION('@localhost' IN _Email) > 0 OR _IsInternalNetworkTestUser = TRUE) THEN
                _ServerIP := '127.0.0.1';
            END IF;*/

            _NeedToStartUpMap := TRUE;

            INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
            VALUES (NOW(),
                    'SpinUpMapInstance returned: ' || COALESCE(_ZoneName, '') || ' CharName: ' ||
                    COALESCE(_CharName, '') || ' ServerIP: ' ||
                    COALESCE(_ServerIP, '') ||
                    ' WorldServerPort: ' || CAST(COALESCE(_WorldServerPort, -1) AS VARCHAR), _CustomerGUID);


            INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
            VALUES (NOW(),
                    'JoinMapByCharName returned: ' || COALESCE(_MapNameToStart, '[NoMapName]') || ' MapInstanceID: ' ||
                    CAST(COALESCE(_MapInstanceID, -1) AS VARCHAR) || ' MapInstanceStatus: ' ||
                    CAST(COALESCE(_MapInstanceStatus, -1) AS VARCHAR) || ' NeedToStartUpMap: ' ||
                    CAST(_NeedToStartUpMap AS VARCHAR) || ' EnableAutoLoopBack: ' ||
                    CAST(_EnableAutoLoopBack AS VARCHAR) ||
                    ' ServerIP: ' || COALESCE(_ServerIP, '') || ' WorldServerIP: ' || COALESCE(_WorldServerIP, ''),
                    _CustomerGUID);
        END IF;
    END IF;
    INSERT INTO temp_table(ServerIP, WorldServerID, WorldServerIP, WorldServerPort, Port, MapInstanceID, MapNameToStart,
                           MapInstanceStatus, NeedToStartUpMap, EnableAutoLoopBack, NoPortForwarding)
    VALUES (_ServerIP, _WorldServerID, _WorldServerIP, _WorldServerPort, _Port, _MapInstanceID, _MapNameToStart,
            _MapInstanceStatus, _NeedToStartUpMap, _EnableAutoLoopBack, _NoPortForwarding);
    RETURN QUERY SELECT * FROM temp_table;
END;
$$;


CREATE TABLE DefaultCharacterValues
(
    CustomerGUID              UUID                       NOT NULL,
    DefaultCharacterValuesID  SERIAL                     NOT NULL,
    DefaultSetName            VARCHAR(50)                NOT NULL,
    StartingMapName           VARCHAR(50)                NOT NULL,
    X                         FLOAT                      NOT NULL,
    Y                         FLOAT                      NOT NULL,
    Z                         FLOAT                      NOT NULL,
    RX                        FLOAT        DEFAULT 0     NOT NULL,
    RY                        FLOAT        DEFAULT 0     NOT NULL,
    RZ                        FLOAT        DEFAULT 0     NOT NULL,
    CONSTRAINT PK_DefaultCharacterValues
        PRIMARY KEY (DefaultCharacterValuesID, CustomerGUID)
);


CREATE TABLE DefaultCustomCharacterData
(
    CustomerGUID                 UUID                       NOT NULL,
    DefaultCustomCharacterDataID SERIAL                     NOT NULL,
    DefaultCharacterValuesID     INT                        NOT NULL,
    CustomFieldName              VARCHAR(50)                NOT NULL,
    FieldValue                   TEXT                       NOT NULL,
    CONSTRAINT PK_DefaultCustomCharacterData
        PRIMARY KEY (DefaultCustomCharacterDataID, CustomerGUID),
    CONSTRAINT FK_DefaultCustomCharacterData_DefaultCharacterValueID
        FOREIGN KEY (DefaultCharacterValuesID, CustomerGUID) REFERENCES DefaultCharacterValues (DefaultCharacterValuesID, CustomerGUID)
);


INSERT INTO OWSVersion (OWSDBVersion) VALUES('20230304');
