BEGIN TRANSACTION

USE [OpenWorldServer]
GO

/****** Object:  Table [dbo].[Characters]    Script Date: 7/29/2023 7:28:56 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
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
	[ItemIDTag] [varchar](150) NOT NULL,
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
			'TestLevel',		
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
	

COMMIT