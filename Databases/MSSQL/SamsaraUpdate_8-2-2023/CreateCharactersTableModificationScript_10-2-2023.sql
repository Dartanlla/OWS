BEGIN TRANSACTION

USE [OpenWorldServer]
GO

/****** Object:  Table [dbo].[Characters]    Script Date: 7/29/2023 7:28:56 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

 IF OBJECT_ID(N'dbo.Abilities', N'U') IS NOT NULL
DROP Table IF EXISTS [dbo].[Abilities]
GO
IF OBJECT_ID(N'dbo.AbilityTypes', N'U') IS NOT NULL
DROP Table IF EXISTS [dbo].[AbilityTypes]
GO

IF OBJECT_ID(N'dbo.ClassInventory', N'U') IS NOT NULL
DROP Table IF EXISTS [dbo].[ClassInventory]
GO
IF OBJECT_ID(N'dbo.Class', N'U') IS NOT NULL
DROP Table [dbo].[Class]
GO
IF OBJECT_ID(N'dbo.CharAbilities', N'U') IS NOT NULL
DROP Table [dbo].[CharAbilities]
GO
IF OBJECT_ID(N'dbo.CharAbilityBarAbilities', N'U') IS NOT NULL
DROP Table [dbo].[CharAbilityBarAbilities]
GO
IF OBJECT_ID(N'dbo.CharAbilityBars', N'U') IS NOT NULL
DROP Table [dbo].[CharAbilityBars]
GO
IF OBJECT_ID(N'dbo.CharHasAbilities', N'U') IS NOT NULL
DROP Table [dbo].[CharHasAbilities]
GO
IF OBJECT_ID(N'dbo.CharHasItems', N'U') IS NOT NULL
DROP Table [dbo].[CharHasItems]
GO
IF OBJECT_ID(N'dbo.CustomCharacterData', N'U') IS NOT NULL
DROP Table [dbo].[CustomCharacterData]
GO
IF OBJECT_ID(N'dbo.CharEquipmentItems', N'U') IS NOT NULL
DROP Table [dbo].[CharEquipmentItems]
GO
IF OBJECT_ID(N'dbo.CharStats', N'U') IS NOT NULL
DROP Table IF EXISTS [dbo].[CharStats]
GO
IF OBJECT_ID(N'dbo.Characters', N'U') IS NOT NULL
DROP Table [dbo].[Characters]
GO
IF OBJECT_ID(N'dbo.CharInventoryItems', N'U') IS NOT NULL
DROP Table [dbo].[CharInventoryItems]
GO
IF OBJECT_ID(N'dbo.CharInventory', N'U') IS NOT NULL
DROP Table [dbo].[CharInventory]
GO

IF OBJECT_ID(N'dbo.CharQuests', N'U') IS NOT NULL
DROP Table IF EXISTS [dbo].[CharQuests]
GO

IF OBJECT_ID(N'dbo.Quests', N'U') IS NOT NULL
DROP Table IF EXISTS [dbo].[Quests]
GO

IF OBJECT_ID(N'dbo.PartyMember', N'U') IS NOT NULL
DROP Table IF EXISTS [dbo].[PartyMember]
GO

IF OBJECT_ID(N'dbo.Party', N'U') IS NOT NULL
DROP Table IF EXISTS [dbo].[Party]
GO

IF OBJECT_ID(N'dbo.GuildMember', N'U') IS NOT NULL
DROP Table IF EXISTS [dbo].[GuildMember]
GO

IF OBJECT_ID(N'dbo.GuildCurrentAbility', N'U') IS NOT NULL
DROP Table IF EXISTS [dbo].[GuildCurrentAbility]
GO

IF OBJECT_ID(N'dbo.GuildStorage', N'U') IS NOT NULL
DROP Table IF EXISTS [dbo].[GuildStorage]
GO

IF OBJECT_ID(N'dbo.Guild', N'U') IS NOT NULL
DROP Table IF EXISTS [dbo].[Guild]
GO

CREATE TABLE [dbo].[Abilities](
	[CustomerGUID] [uniqueidentifier] NOT NULL,
	[AbilityIDTag] [varchar](50) NOT NULL,
	[AbilityName] [varchar](50) NOT NULL,
	[AbilityClassName] [varchar](50) NOT NULL,
	[CustomData] [varchar](max) NULL,
 CONSTRAINT [PK_Abilities] PRIMARY KEY CLUSTERED 
(
	[CustomerGUID] ASC,
	[AbilityIDTag] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Class](
	[CustomerGUID] [uniqueidentifier] NOT NULL,
	[ClassID] [int] IDENTITY(1,1) NOT NULL,
	[ClassName] [varchar](50) NOT NULL,
	[StartingMapName] [varchar](50) NOT NULL,
	[X] [float] NOT NULL,
	[Y] [float] NOT NULL,
	[Z] [float] NOT NULL,
	[RX] [float] NOT NULL,
	[RY] [float] NOT NULL,
	[RZ] [float] NOT NULL,
	[TeamNumber] [int] NOT NULL,
	[Gender] [tinyint] NOT NULL,
	[Description] [varchar](max) NULL,
 CONSTRAINT [PK_Class] PRIMARY KEY CLUSTERED 
(
	[CustomerGUID] ASC,
	[ClassID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[Class] ADD  CONSTRAINT [DF_Class_ClassName]  DEFAULT ('') FOR [ClassName]
GO


CREATE TABLE [dbo].[Characters](
	[CustomerGUID] [uniqueidentifier] NOT NULL,
	[CharacterID] [int] IDENTITY(1,1) NOT NULL,
	[UserGUID] [uniqueidentifier] NULL,
	[CharName] [nvarchar](50) NOT NULL,
	[CharGUID] [uniqueidentifier] NULL,
	[MapName] [varchar](50) NULL,
	[X] [float] NOT NULL,
	[Y] [float] NOT NULL,
	[Z] [float] NOT NULL,
	[ServerIP] [varchar](50) NULL,
	[LastActivity] [datetime] NOT NULL,
	[RX] [float] NOT NULL,
	[RY] [float] NOT NULL,
	[RZ] [float] NOT NULL,
	[TeamNumber] [int] NOT NULL,
	[Gender] [tinyint] NOT NULL,
	[Description] [varchar](max) NULL,
	[ClassID] [int] NOT NULL,
	[IsAdmin] [bit] NOT NULL,
	[IsModerator] [bit] NOT NULL,
	[CreateDate] [datetime] NOT NULL,
 CONSTRAINT [PK_WrathChar] PRIMARY KEY CLUSTERED 
(
	[CustomerGUID] ASC,
	[CharacterID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[Characters] ADD  CONSTRAINT [DF__WrathChar__LastA__3631FF56]  DEFAULT (getdate()) FOR [LastActivity]
GO

ALTER TABLE [dbo].[Characters] ADD  CONSTRAINT [DF__WrathChar__RX__61DB776A]  DEFAULT ((0)) FOR [RX]
GO

ALTER TABLE [dbo].[Characters] ADD  CONSTRAINT [DF__WrathChar__RY__62CF9BA3]  DEFAULT ((0)) FOR [RY]
GO

ALTER TABLE [dbo].[Characters] ADD  CONSTRAINT [DF__WrathChar__RZ__63C3BFDC]  DEFAULT ((0)) FOR [RZ]
GO

ALTER TABLE [dbo].[Characters] ADD  CONSTRAINT [DF__WrathChar__TeamN__1DF06171]  DEFAULT ((0)) FOR [TeamNumber]
GO

ALTER TABLE [dbo].[Characters] ADD  CONSTRAINT [DF__Character__Gende__781FBE44]  DEFAULT ((0)) FOR [Gender]
GO

ALTER TABLE [dbo].[Characters] ADD  DEFAULT ((0)) FOR [IsAdmin]
GO

ALTER TABLE [dbo].[Characters] ADD  DEFAULT ((0)) FOR [IsModerator]
GO

ALTER TABLE [dbo].[Characters] ADD  DEFAULT (getdate()) FOR [CreateDate]
GO

ALTER TABLE [dbo].[Characters]  WITH CHECK ADD  CONSTRAINT [FK_Characters_UserGUID] FOREIGN KEY([UserGUID])
REFERENCES [dbo].[Users] ([UserGUID])
GO

ALTER TABLE [dbo].[Characters] CHECK CONSTRAINT [FK_Characters_UserGUID]
GO

CREATE TABLE [dbo].[CharEquipmentItems](
	[CustomerGUID] [uniqueidentifier] NOT NULL,
	[CharacterID] [int] NOT NULL,
	[ItemIDTag] [varchar](50) NOT NULL,
	[Quantity] [int] NOT NULL,
	[InSlotNumber] [int] NOT NULL,
	[CustomData] [varchar](max) NULL,
 CONSTRAINT [PK_CharEquipmentItems] PRIMARY KEY CLUSTERED 
(
	[CustomerGUID] ASC,
	[CharacterID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[CharStats](
	[CustomerGUID] [uniqueidentifier] NOT NULL,
	[CharacterID] [int] NOT NULL,
	[StatIdentifier] [varchar](50) NOT NULL,
	[Value] [int] NOT NULL,
 CONSTRAINT [PK_CharStats] PRIMARY KEY CLUSTERED 
(
	[CustomerGUID] ASC,
	[CharacterID] ASC,
	[StatIdentifier] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]


CREATE TABLE [dbo].[CharAbilities](
	[CustomerGUID] [uniqueidentifier] NOT NULL,
	[CharacterID] [int] NOT NULL,
	[AbilityIDTag] [varchar](50) NOT NULL,
	[CurrentAbilityLevel] [int] NOT NULL,
	[ActualAbilityLevel] [int] NOT NULL,
	[CustomData] [varchar](max) NULL,
 CONSTRAINT [PK_CharAbilities] PRIMARY KEY CLUSTERED 
(
	[CustomerGUID] ASC,
	[CharacterID] ASC,
	[AbilityIDTag] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

CREATE TABLE [dbo].[CharInventory](
	[CustomerGUID] [uniqueidentifier] NOT NULL,
	[CharacterID] [int] NOT NULL,
	[CharInventoryID] [int] IDENTITY(1,1) NOT NULL,
	[InventoryName] [varchar](50) NOT NULL,
	[InventorySize] [int] NOT NULL,
 CONSTRAINT [PK_CharInventory] PRIMARY KEY CLUSTERED 
(
	[CustomerGUID] ASC,
	[CharacterID] ASC,
	[CharInventoryID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[CharInventoryItems](
	[CustomerGUID] [uniqueidentifier] NOT NULL,
	[CharInventoryID] [int] NOT NULL,
	[ItemIDTag] [varchar](50) NOT NULL,
	[Quantity] [int] NOT NULL,
	[InSlotNumber] [int] NOT NULL,
	[CustomData] [varchar](max) NULL,
 CONSTRAINT [PK_CharInventoryItems] PRIMARY KEY CLUSTERED 
(
	[CustomerGUID] ASC,
	[CharInventoryID] ASC,
	[InSlotNumber] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[CharInventoryCurrency](
	[CustomerGUID] [uniqueidentifier] NOT NULL,
	[CharInventoryID] [int] NOT NULL,
	[ItemIDTag] [varchar](50) NOT NULL,
	[Quantity] [int] NOT NULL
 CONSTRAINT [PK_CharInventoryCurrency] PRIMARY KEY CLUSTERED 
(
	[CustomerGUID] ASC,
	[CharInventoryID] ASC,
	[ItemIDTag] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[CharQuests](
	[CustomerGUID] [uniqueidentifier] NOT NULL,
	[CharacterID] [int] NOT NULL,
	[QuestIDTag] [varchar](50) NOT NULL,
	[QuestJournalTagContainer] [varchar](150) NOT NULL,
	[CustomData] [varchar](max) NULL,
 CONSTRAINT [PK_CharQuests] PRIMARY KEY CLUSTERED 
(
	[CustomerGUID] ASC,
	[CharacterID] ASC,
	[QuestIDTag] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

  CREATE TABLE [dbo].[Quest](
	[CustomerGUID] [uniqueidentifier] NOT NULL,
	[QuestIDTag] [varchar](50) NOT NULL,
	[QuestOverview] [varchar](max) NOT NULL,
	[QuestTasks] [varchar](max) NOT NULL,
	[QuestClassName] [varchar](50) NOT NULL,
	[CustomData] [varchar](max) NULL,
 CONSTRAINT [PK_Quests] PRIMARY KEY CLUSTERED 
(
	[CustomerGUID] ASC,
	[QuestIDTag] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

 CREATE TABLE [dbo].[Party](
	[CustomerGUID] [uniqueidentifier] NOT NULL,
	[PartyID] [int] IDENTITY(1,10) NOT NULL,
	[PartyGuid] [uniqueidentifier] NOT NULL,
	[RaidingParty] BIT NOT NULL
 CONSTRAINT [PK_Party] PRIMARY KEY CLUSTERED 
(
	[CustomerGUID] ASC,
	[PartyID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

 CREATE TABLE [dbo].[PartyMember](
	[CustomerGUID] [uniqueidentifier] NOT NULL,
	[PartyID] [int] NOT NULL,
	[CharacterID] [int] NOT NULL,
	[PartyLeader] BIT NOT NULL
 CONSTRAINT [PK_PartyMember] PRIMARY KEY CLUSTERED 
(
	[CustomerGUID] ASC,
	[CharacterID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

 CREATE TABLE [dbo].[Guild](
	[CustomerGUID] [uniqueidentifier] NOT NULL,
	[GuildID] [int] IDENTITY(3,10) NOT NULL,
	[GuildGuid] [uniqueidentifier] NOT NULL,
	[GuildName] [varchar](50) NOT NULL,
	[GuildMessage] [varchar](150),
	[GuildDescription] [varchar](150),
	[GuildImage] [int] DEFAULT 0,
	[GuildDate] [datetime] NOT NULL,
 CONSTRAINT [PK_Guild] PRIMARY KEY CLUSTERED 
(
	[CustomerGUID] ASC,
	[GuildID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Guild] ADD CONSTRAINT UNQ__Guild__GuildName UNIQUE ([GuildName])

 CREATE TABLE [dbo].[GuildMember](
	[CustomerGUID] [uniqueidentifier] NOT NULL,
	[GuildID] [int] NOT NULL,
	[CharacterID] [int] NOT NULL,
	[GuildRank] int NOT NULL,
	[GuildJoinedDate] [datetime] NOT NULL,
 CONSTRAINT [PK_GuildMember] PRIMARY KEY CLUSTERED 
(
	[CustomerGUID] ASC,
	[CharacterID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

 CREATE TABLE [dbo].[GuildCurrentAbility](
	[CustomerGUID] [uniqueidentifier] NOT NULL,
	[GuildID] [int] NOT NULL,
	[GuildAbilityId] [int] NOT NULL
 CONSTRAINT [PK_GuildCurrentAbility] PRIMARY KEY CLUSTERED 
(
	[CustomerGUID] ASC,
	[GuildID] ASC,
	[GuildAbilityId] ASC,
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

 CREATE TABLE [dbo].[GuildStorage](
	[CustomerGUID] [uniqueidentifier] NOT NULL,
	[GuildID] [int] NOT NULL,
	[ItemIDTag] [varchar](50) NOT NULL,
	[Quantity]	[int] NOT NULL,
	[CustomData] [varchar](150)
 CONSTRAINT [PK_GuildStorage] PRIMARY KEY CLUSTERED 
(
	[CustomerGUID] ASC,
	[GuildID] ASC,
	[ItemIDTag] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

 CREATE TABLE [dbo].[PlayerGroupTypes](
	
	[PlayerGroupTypeID] [int] NOT NULL,
	[PlayerGroupTypeDescription] [varchar](50)
 CONSTRAINT [PK_PlayerGroupTypes] PRIMARY KEY CLUSTERED 
(
	[PlayerGroupTypeID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

 CREATE TABLE [dbo].[PlayerGroupMember](
	[CustomerGUID] [uniqueidentifier] NOT NULL,
	[PlayerGroupID] [int] NOT NULL,
	[CharacterID] [int] NOT NULL,
	[DateAdded] [datetime],
	[TeamNumber] [int]
 CONSTRAINT [PK_PlayerGroupMember] PRIMARY KEY CLUSTERED 
(
	[CustomerGUID] ASC,
	[PlayerGroupID] ASC,
	[CharacterID] ASC
	
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

 CREATE TABLE [dbo].[PlayerGroup](
	[CustomerGUID] [uniqueidentifier] NOT NULL,
	[PlayerGroupID] [int] NOT NULL,
	[PlayerGroupTypeID] [int] NOT NULL,
	[ReadyState] [int],
	[DateAdded] [datetime]
 CONSTRAINT [PK_PlayerGroup] PRIMARY KEY CLUSTERED 
(
	[CustomerGUID] ASC,
	[PlayerGroupID] ASC
	
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

 CREATE TABLE [dbo].[ActionHousePlayerItem](
	[CustomerGUID] [uniqueidentifier] NOT NULL,
	[SlotIndex] [int] NOT NULL,
	[CharacterID] [int] NOT NULL,
	[ItemIDTag] [varchar](150) NOT NULL,
	[InProgressQuantity] [int] NOT NULL,
	[TotalQuantity] [int] NOT NULL,
	[SetPrice] [int] NOT NULL,
	[TotalItemQuantityInStorage] [int] NOT NULL,
	[TotalCurrencyInStorage] [int] NOT NULL,
	[ActionHouseActionID] [int] NOT NULL
 CONSTRAINT [PK_ActionHousePlayerItem] PRIMARY KEY CLUSTERED 
(
	[CustomerGUID] ASC,
	[SlotIndex] ASC,
	[CharacterID] ASC,
	[ActionHouseActionID] ASC
	
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

 CREATE TABLE [dbo].[ActionHouseActionType](
	[ActionHouseActionID] [int] NOT NULL,
	[ActionHouseActionTypeDescription] [varchar](50)
 CONSTRAINT [PK_ActionHouseActionType] PRIMARY KEY CLUSTERED 
(
	[ActionHouseActionID] ASC
	
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

 CREATE TABLE [dbo].[ActionHouseStateType](
	[ActionHouseStateID] [int] NOT NULL,
	[ActionHouseStateTypeDescription] [varchar](50)
 CONSTRAINT [PK_ActionHouseStateType] PRIMARY KEY CLUSTERED 
(
	[ActionHouseStateID] ASC
	
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO


DECLARE @CustomerGuid uniqueidentifier
SELECT TOP 1 @CustomerGuid=CustomerGUID FROM Customers
		
IF (NOT EXISTS (SELECT 1 FROM Class C WITH (HOLDLOCK) WHERE C.CustomerGUID=@CustomerGUID AND C.ClassName='Wanderer'))
	BEGIN

		INSERT INTO Class(
			CustomerGUID, 
			ClassName,	
			Gender,
			[Description],
			StartingMapName,
			X,
			Y,
			Z,
			RX,
			RY,
			RZ,
			TeamNumber
		)
		VALUES (
			@CustomerGuid, 
			'Wanderer',	
			0,
			'',
			'Spawn.Location.TestLevel.TestLevel',		
			0,
			0,
			0,
			0,
			0,
			0,
			0
			)

		DECLARE @ClassID int

		SELECT @ClassID=SCOPE_IDENTITY()

		SELECT @ClassID as ClassID

	END

INSERT INTO PlayerGroupTypes(
			PlayerGroupTypeID, 
			PlayerGroupTypeDescription
		)
		VALUES (
			1,
			'Party'
			)
COMMIT

INSERT INTO PlayerGroupTypes(
			PlayerGroupTypeID, 
			PlayerGroupTypeDescription
		)
		VALUES (
			2,
			'Raid'
			)
COMMIT

INSERT INTO PlayerGroupTypes(
			PlayerGroupTypeID, 
			PlayerGroupTypeDescription
		)
		VALUES (
			3,
			'Guild'
			)
COMMIT

INSERT INTO PlayerGroupTypes(
			PlayerGroupTypeID, 
			PlayerGroupTypeDescription
		)
		VALUES (
			4,
			'Team'
			)
COMMIT

INSERT INTO PlayerGroupTypes(
			PlayerGroupTypeID, 
			PlayerGroupTypeDescription
		)
		VALUES (
			5,
			'Faction'
			)
COMMIT

INSERT INTO PlayerGroupTypes(
			PlayerGroupTypeID, 
			PlayerGroupTypeDescription
		)
		VALUES (
			6,
			'FightGroup'
			)
COMMIT

INSERT INTO PlayerGroupTypes(
			PlayerGroupTypeID, 
			PlayerGroupTypeDescription
		)
		VALUES (
			7,
			'Alliance'
			)
COMMIT

INSERT INTO PlayerGroupTypes(
			PlayerGroupTypeID, 
			PlayerGroupTypeDescription
		)
		VALUES (
			8,
			'Squad'
			)
COMMIT

INSERT INTO PlayerGroupTypes(
			PlayerGroupTypeID, 
			PlayerGroupTypeDescription
		)
		VALUES (
			9,
			'PvPBattleGroup'
			)
COMMIT

INSERT INTO PlayerGroupTypes(
			PlayerGroupTypeID, 
			PlayerGroupTypeDescription
		)
		VALUES (
			99,
			'Other'
			)
COMMIT

INSERT INTO ActionHouseActionType(
			ActionHouseActionID, 
			ActionHouseActionTypeDescription
		)
		VALUES (
			0,
			'Buy'
			)
COMMIT

INSERT INTO ActionHouseActionType(
			ActionHouseActionID, 
			ActionHouseActionTypeDescription
		)
		VALUES (
			1,
			'Sell'
			)
COMMIT


INSERT INTO ActionHouseStateType(
			ActionHouseStateID, 
			ActionHouseStateTypeDescription
		)
		VALUES (
			0,
			'In Progress'
			)
COMMIT

INSERT INTO ActionHouseStateType(
			ActionHouseStateID, 
			ActionHouseStateTypeDescription
		)
		VALUES (
			1,
			'Completed'
			)
COMMIT

INSERT INTO ActionHouseStateType(
			ActionHouseStateID, 
			ActionHouseStateTypeDescription
		)
		VALUES (
			2,
			'Cancelled'
			)
COMMIT

