USE OpenWorldServer
GO

UPDATE OWSVersion SET OWSDBVersion='20230304'
GO

SELECT OWSDBVersion FROM OWSVersion
GO


/****** Object:  Table [dbo].[DefaultCharacterValues]    Script Date: 3/4/2023 12:29:25 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[DefaultCharacterValues](
	[CustomerGUID] [uniqueidentifier] NOT NULL,
	[DefaultCharacterValuesID] [int] IDENTITY(1,1) NOT NULL,
	[DefaultSetName] [varchar](50) NOT NULL,
	[StartingMapName] [varchar](50) NOT NULL,
	[X] [float] NOT NULL,
	[Y] [float] NOT NULL,
	[Z] [float] NOT NULL,
	[RX] [float] NOT NULL,
	[RY] [float] NOT NULL,
	[RZ] [float] NOT NULL,
 CONSTRAINT [PK_DefaultCharacterValues] PRIMARY KEY CLUSTERED 
(
	[CustomerGUID] ASC,
	[DefaultCharacterValuesID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO



/****** Object:  Table [dbo].[DefaultCustomCharacterData]    Script Date: 3/4/2023 12:29:49 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[DefaultCustomCharacterData](
	[CustomerGUID] [uniqueidentifier] NOT NULL,
	[DefaultCustomCharacterDataID] [int] IDENTITY(1,1) NOT NULL,
	[DefaultCharacterValuesID] [int] NOT NULL,
	[CustomFieldName] [varchar](50) NOT NULL,
	[FieldValue] [varchar](max) NOT NULL,
 CONSTRAINT [PK_DefaultCustomCharacterData] PRIMARY KEY CLUSTERED 
(
	[CustomerGUID] ASC,
	[DefaultCustomCharacterDataID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[DefaultCustomCharacterData]  WITH CHECK ADD  CONSTRAINT [FK_DefaultCustomCharacterData_DefaultCharacterValueID] FOREIGN KEY([CustomerGUID], [DefaultCharacterValuesID])
REFERENCES [dbo].[DefaultCharacterValues] ([CustomerGUID], [DefaultCharacterValuesID])
GO

ALTER TABLE [dbo].[DefaultCustomCharacterData] CHECK CONSTRAINT [FK_DefaultCustomCharacterData_DefaultCharacterValueID]
GO

