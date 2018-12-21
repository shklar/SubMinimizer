
-- --------------------------------------------------
-- Entity Designer DDL Script for SQL Server 2005, 2008, 2012 and Azure
-- --------------------------------------------------
-- Date Created: 11/24/2018 22:50:52
-- Generated from EDMX file: C:\projects\nom\SubMinimizer\Shared\DataAccess.edmx
-- --------------------------------------------------

SET QUOTED_IDENTIFIER OFF;
GO
USE [sub_min_db_eviten_stat];
GO
IF SCHEMA_ID(N'dbo') IS NULL EXECUTE(N'CREATE SCHEMA [dbo]');
GO

-- --------------------------------------------------
-- Dropping existing FOREIGN KEY constraints
-- --------------------------------------------------


-- --------------------------------------------------
-- Dropping existing tables
-- --------------------------------------------------

IF OBJECT_ID(N'[dbo].[Resources]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Resources];
GO
IF OBJECT_ID(N'[dbo].[Subscriptions]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Subscriptions];
GO
IF OBJECT_ID(N'[dbo].[PerUserTokenCaches]', 'U') IS NOT NULL
    DROP TABLE [dbo].[PerUserTokenCaches];
GO

-- --------------------------------------------------
-- Creating all tables
-- --------------------------------------------------

-- Creating table 'Resources'
CREATE TABLE [dbo].[Resources] (
    [Id] nvarchar(256)  NOT NULL,
    [Name] nvarchar(max)  NOT NULL,
    [Type] nvarchar(max)  NOT NULL,
    [ConfirmedOwner] bit  NOT NULL,
    [ResourceGroup] nvarchar(max)  NOT NULL,
    [Description] nvarchar(max)  NULL,
    [FirstFoundDate] datetime  NOT NULL,
    [ExpirationDate] datetime  NOT NULL,
    [LastVisitedDate] datetime  NOT NULL,
    [AzureResourceIdentifier] nvarchar(max)  NOT NULL,
    [SubscriptionId] nvarchar(max)  NOT NULL,
    [Status] int  NOT NULL,
    [Expired] bit  NOT NULL,
    [Owner] nvarchar(max)  NULL
);
GO

-- Creating table 'Subscriptions'
CREATE TABLE [dbo].[Subscriptions] (
    [IsConnected] bit  NOT NULL,
    [DisplayName] nvarchar(max)  NOT NULL,
    [Id] nvarchar(256)  NOT NULL,
    [OrganizationId] nvarchar(max)  NOT NULL,
    [ConnectedOn] datetime  NOT NULL,
    [ConnectedBy] nvarchar(max)  NOT NULL,
    [AzureAccessNeedsToBeRepaired] bit  NOT NULL,
    [LastAnalysisDate] datetime  NOT NULL,
    [ReserveIntervalInDays] int  NOT NULL,
    [ExpirationIntervalInDays] int  NOT NULL,
    [ExpirationUnclaimedIntervalInDays] int  NOT NULL,
    [DeleteIntervalInDays] int  NOT NULL,
    [ManagementLevel] int  NOT NULL,
    [SendEmailToCoAdmins] bit  NOT NULL,
    [SendEmailOnlyInvalidResources] bit  NOT NULL
);
GO

-- Creating table 'PerUserTokenCaches'
CREATE TABLE [dbo].[PerUserTokenCaches] (
    [Id] nvarchar(256)  NOT NULL,
    [webUserUniqueId] nvarchar(max)  NOT NULL,
    [cacheBits] varbinary(max)  NOT NULL,
    [LastWrite] datetime  NOT NULL
);
GO

-- --------------------------------------------------
-- Creating all PRIMARY KEY constraints
-- --------------------------------------------------

-- Creating primary key on [Id] in table 'Resources'
ALTER TABLE [dbo].[Resources]
ADD CONSTRAINT [PK_Resources]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'Subscriptions'
ALTER TABLE [dbo].[Subscriptions]
ADD CONSTRAINT [PK_Subscriptions]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'PerUserTokenCaches'
ALTER TABLE [dbo].[PerUserTokenCaches]
ADD CONSTRAINT [PK_PerUserTokenCaches]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- --------------------------------------------------
-- Creating all FOREIGN KEY constraints
-- --------------------------------------------------

-- --------------------------------------------------
-- Script has ended
-- --------------------------------------------------