USE [master];
GO

IF DB_ID(N'ForumSimpleAdmin') IS NULL
BEGIN
    CREATE DATABASE [ForumSimpleAdmin];
END
GO

USE [ForumSimpleAdmin];
GO

IF OBJECT_ID(N'dbo.AdminRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AdminRoles
    (
        Id              INT             IDENTITY(1,1) NOT NULL,
        Name            NVARCHAR(100)   NOT NULL,
        Description     NVARCHAR(255)   NULL,
        RoleType        SMALLINT        NOT NULL,
        IsVisible       BIT             NOT NULL DEFAULT 1,
        IsSystemDefined BIT             NOT NULL DEFAULT 0,
        CONSTRAINT PK_AdminRoles PRIMARY KEY (Id)
    );
END
GO

IF OBJECT_ID(N'dbo.AdminUsers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AdminUsers
    (
        Id                      INT             IDENTITY(1,1) NOT NULL,
        Email                   NVARCHAR(256)   NOT NULL,
        FullName                NVARCHAR(100)   NOT NULL,
        Phone                   VARCHAR(50)     NULL,
        RoleId                  INT             NOT NULL,
        PasswordHash            VARBINARY(32)   NOT NULL,
        PasswordSalt            VARBINARY(32)   NOT NULL,
        EmailVerificationCode   VARCHAR(100)    NULL,
        VerificationCodeDate    DATETIME        NULL,
        PictureUrl              VARCHAR(255)    NULL,
        Active                  BIT             NOT NULL DEFAULT 1,
        CreateDate              DATETIME        NOT NULL DEFAULT GETDATE(),
        UpdateDate              DATETIME        NOT NULL DEFAULT GETDATE(),
        UpdateBy                INT             NOT NULL DEFAULT 1,
        LastLoginDate           DATETIME        NULL,
        LastIpAddress           VARCHAR(50)     NULL,
        Archived                BIT             NOT NULL DEFAULT 0,
        CONSTRAINT PK_AdminUsers PRIMARY KEY (Id),
        CONSTRAINT UQ_AdminUsers_Email UNIQUE (Email),
        CONSTRAINT FK_AdminUsers_Role FOREIGN KEY (RoleId) REFERENCES dbo.AdminRoles(Id)
    );
END
GO

IF OBJECT_ID(N'dbo.Settings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Settings
    (
        ClassName       VARCHAR(100)    NOT NULL,
        Name            NVARCHAR(100)   NOT NULL,
        Data            NVARCHAR(MAX)   NOT NULL,
        CreateDate      DATETIME        NOT NULL DEFAULT GETDATE(),
        UpdateDate      DATETIME        NOT NULL DEFAULT GETDATE(),
        UpdateBy        INT             NOT NULL DEFAULT 1,
        CONSTRAINT PK_Settings PRIMARY KEY (ClassName)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AdminRoles WHERE Id = 0)
BEGIN
    SET IDENTITY_INSERT dbo.AdminRoles ON;
    INSERT INTO dbo.AdminRoles (Id, Name, Description, RoleType, IsVisible, IsSystemDefined)
    VALUES (0, N'Dino Admin', N'Master administrator with full system access', 0, 0, 1);
    SET IDENTITY_INSERT dbo.AdminRoles OFF;
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AdminUsers WHERE Id = 0)
BEGIN
    SET IDENTITY_INSERT dbo.AdminUsers ON;
    INSERT INTO dbo.AdminUsers
    (Id, Email, FullName, Phone, RoleId, PasswordHash, PasswordSalt, EmailVerificationCode, VerificationCodeDate, PictureUrl, Active, LastLoginDate, LastIpAddress, Archived, CreateDate, UpdateDate, UpdateBy)
    VALUES
    (0, N'admin@devdino.com', N'Dino Admin', NULL, 0, 0x00, 0x00, NULL, NULL, NULL, 1, NULL, NULL, 0, GETDATE(), GETDATE(), 0);
    SET IDENTITY_INSERT dbo.AdminUsers OFF;
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Settings WHERE ClassName = 'DinoMasterSettings')
    INSERT INTO dbo.Settings (ClassName, Name, Data) VALUES ('DinoMasterSettings', N'Dino Master Settings', N'{}');

IF NOT EXISTS (SELECT 1 FROM dbo.Settings WHERE ClassName = 'SystemSettings')
    INSERT INTO dbo.Settings (ClassName, Name, Data) VALUES ('SystemSettings', N'System Settings', N'{}');
GO

DECLARE @UserId INT = 0;
DECLARE @PlainPassword VARCHAR(50) = 'Aa1234567';
DECLARE @Salt VARBINARY(32) = CRYPT_GEN_RANDOM(32);
DECLARE @PasswordBytes VARBINARY(MAX) = CAST(@PlainPassword AS VARBINARY(MAX));
DECLARE @CombinedBytes VARBINARY(MAX) = @PasswordBytes + @Salt;
DECLARE @FinalHash VARBINARY(32) = HASHBYTES('SHA2_256', @CombinedBytes);

UPDATE dbo.AdminUsers
SET PasswordHash = @FinalHash,
    PasswordSalt = @Salt,
    UpdateDate = GETDATE(),
    UpdateBy = 0
WHERE Id = @UserId;
GO

IF OBJECT_ID(N'dbo.ForumUsers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ForumUsers
    (
        Id                  INT             IDENTITY(1,1) NOT NULL,
        Name                NVARCHAR(100)   NOT NULL,
        PasswordHash        NVARCHAR(256)   NOT NULL,
        ProfilePicturePath  NVARCHAR(300)   NULL,
        IsManager           BIT             NOT NULL CONSTRAINT DF_ForumUsers_IsManager DEFAULT(0),
        IsDeleted           BIT             NOT NULL CONSTRAINT DF_ForumUsers_IsDeleted DEFAULT(0),
        CreateDate          DATETIME        NOT NULL CONSTRAINT DF_ForumUsers_CreateDate DEFAULT(GETDATE()),
        UpdateDate          DATETIME        NOT NULL CONSTRAINT DF_ForumUsers_UpdateDate DEFAULT(GETDATE()),
        UpdateBy            INT             NOT NULL CONSTRAINT DF_ForumUsers_UpdateBy DEFAULT(1),
        CONSTRAINT PK_ForumUsers PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_ForumUsers_Name UNIQUE (Name)
    );
END
GO

IF OBJECT_ID(N'dbo.Forums', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Forums
    (
        Id                  INT             IDENTITY(1,1) NOT NULL,
        Name                NVARCHAR(100)   NOT NULL,
        ManagersOnlyPosting BIT             NOT NULL CONSTRAINT DF_Forums_ManagersOnlyPosting DEFAULT(0),
        Active              BIT             NOT NULL CONSTRAINT DF_Forums_Active DEFAULT(1),
        SortIndex           INT             NOT NULL CONSTRAINT DF_Forums_SortIndex DEFAULT(0),
        IsDeleted           BIT             NOT NULL CONSTRAINT DF_Forums_IsDeleted DEFAULT(0),
        CreateDate          DATETIME        NOT NULL CONSTRAINT DF_Forums_CreateDate DEFAULT(GETDATE()),
        UpdateDate          DATETIME        NOT NULL CONSTRAINT DF_Forums_UpdateDate DEFAULT(GETDATE()),
        UpdateBy            INT             NOT NULL CONSTRAINT DF_Forums_UpdateBy DEFAULT(1),
        CONSTRAINT PK_Forums PRIMARY KEY CLUSTERED (Id)
    );
END
GO

IF OBJECT_ID(N'dbo.ForumPosts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ForumPosts
    (
        Id                  INT             IDENTITY(1,1) NOT NULL,
        ForumId             INT             NOT NULL,
        UserId              INT             NOT NULL,
        Title               NVARCHAR(200)   NOT NULL,
        Content             NVARCHAR(MAX)   NOT NULL,
        IsDeleted           BIT             NOT NULL CONSTRAINT DF_ForumPosts_IsDeleted DEFAULT(0),
        CreateDate          DATETIME        NOT NULL CONSTRAINT DF_ForumPosts_CreateDate DEFAULT(GETDATE()),
        UpdateDate          DATETIME        NOT NULL CONSTRAINT DF_ForumPosts_UpdateDate DEFAULT(GETDATE()),
        UpdateBy            INT             NOT NULL CONSTRAINT DF_ForumPosts_UpdateBy DEFAULT(1),
        CONSTRAINT PK_ForumPosts PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_ForumPosts_Forums FOREIGN KEY (ForumId) REFERENCES dbo.Forums(Id),
        CONSTRAINT FK_ForumPosts_ForumUsers FOREIGN KEY (UserId) REFERENCES dbo.ForumUsers(Id)
    );
END
GO

IF OBJECT_ID(N'dbo.ForumComments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ForumComments
    (
        Id                  INT             IDENTITY(1,1) NOT NULL,
        PostId              INT             NOT NULL,
        UserId              INT             NOT NULL,
        Content             NVARCHAR(1000)  NOT NULL,
        IsDeleted           BIT             NOT NULL CONSTRAINT DF_ForumComments_IsDeleted DEFAULT(0),
        CreateDate          DATETIME        NOT NULL CONSTRAINT DF_ForumComments_CreateDate DEFAULT(GETDATE()),
        UpdateDate          DATETIME        NOT NULL CONSTRAINT DF_ForumComments_UpdateDate DEFAULT(GETDATE()),
        UpdateBy            INT             NOT NULL CONSTRAINT DF_ForumComments_UpdateBy DEFAULT(1),
        CONSTRAINT PK_ForumComments PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_ForumComments_ForumPosts FOREIGN KEY (PostId) REFERENCES dbo.ForumPosts(Id),
        CONSTRAINT FK_ForumComments_ForumUsers FOREIGN KEY (UserId) REFERENCES dbo.ForumUsers(Id)
    );
END
GO

IF OBJECT_ID(N'dbo.ForumSessions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ForumSessions
    (
        Id                  INT             IDENTITY(1,1) NOT NULL,
        UserId              INT             NOT NULL,
        Token               NVARCHAR(100)   NOT NULL,
        ExpirationDate      DATETIME        NOT NULL,
        CreateDate          DATETIME        NOT NULL CONSTRAINT DF_ForumSessions_CreateDate DEFAULT(GETDATE()),
        CONSTRAINT PK_ForumSessions PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_ForumSessions_ForumUsers FOREIGN KEY (UserId) REFERENCES dbo.ForumUsers(Id),
        CONSTRAINT UQ_ForumSessions_Token UNIQUE (Token)
    );
END
GO

IF OBJECT_ID(N'dbo.SiteSettings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SiteSettings
    (
        Id                  INT             IDENTITY(1,1) NOT NULL,
        IsLocked            BIT             NOT NULL CONSTRAINT DF_SiteSettings_IsLocked DEFAULT(0),
        CreateDate          DATETIME        NOT NULL CONSTRAINT DF_SiteSettings_CreateDate DEFAULT(GETDATE()),
        UpdateDate          DATETIME        NOT NULL CONSTRAINT DF_SiteSettings_UpdateDate DEFAULT(GETDATE()),
        UpdateBy            INT             NOT NULL CONSTRAINT DF_SiteSettings_UpdateBy DEFAULT(1),
        CONSTRAINT PK_SiteSettings PRIMARY KEY CLUSTERED (Id)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Forums)
BEGIN
    INSERT INTO dbo.Forums (Name, ManagersOnlyPosting, Active, SortIndex, IsDeleted, CreateDate, UpdateDate, UpdateBy)
    VALUES
        (N'General Discussion', 0, 1, 1, 0, GETDATE(), GETDATE(), 1),
        (N'Announcements', 1, 1, 2, 0, GETDATE(), GETDATE(), 1),
        (N'Support', 0, 1, 3, 0, GETDATE(), GETDATE(), 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SiteSettings)
BEGIN
    INSERT INTO dbo.SiteSettings (IsLocked, CreateDate, UpdateDate, UpdateBy)
    VALUES (0, GETDATE(), GETDATE(), 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Forums') AND name = N'IX_Forums_SortIndex')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Forums_SortIndex ON dbo.Forums (SortIndex);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Forums') AND name = N'IX_Forums_IsDeleted')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Forums_IsDeleted ON dbo.Forums (IsDeleted);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ForumUsers') AND name = N'IX_ForumUsers_IsDeleted')
BEGIN
    CREATE NONCLUSTERED INDEX IX_ForumUsers_IsDeleted ON dbo.ForumUsers (IsDeleted);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ForumPosts') AND name = N'IX_ForumPosts_ForumId')
BEGIN
    CREATE NONCLUSTERED INDEX IX_ForumPosts_ForumId ON dbo.ForumPosts (ForumId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ForumPosts') AND name = N'IX_ForumPosts_UserId')
BEGIN
    CREATE NONCLUSTERED INDEX IX_ForumPosts_UserId ON dbo.ForumPosts (UserId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ForumPosts') AND name = N'IX_ForumPosts_IsDeleted')
BEGIN
    CREATE NONCLUSTERED INDEX IX_ForumPosts_IsDeleted ON dbo.ForumPosts (IsDeleted);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ForumPosts') AND name = N'IX_ForumPosts_CreateDate')
BEGIN
    CREATE NONCLUSTERED INDEX IX_ForumPosts_CreateDate ON dbo.ForumPosts (CreateDate);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ForumComments') AND name = N'IX_ForumComments_PostId')
BEGIN
    CREATE NONCLUSTERED INDEX IX_ForumComments_PostId ON dbo.ForumComments (PostId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ForumComments') AND name = N'IX_ForumComments_UserId')
BEGIN
    CREATE NONCLUSTERED INDEX IX_ForumComments_UserId ON dbo.ForumComments (UserId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ForumComments') AND name = N'IX_ForumComments_IsDeleted')
BEGIN
    CREATE NONCLUSTERED INDEX IX_ForumComments_IsDeleted ON dbo.ForumComments (IsDeleted);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ForumComments') AND name = N'IX_ForumComments_CreateDate')
BEGIN
    CREATE NONCLUSTERED INDEX IX_ForumComments_CreateDate ON dbo.ForumComments (CreateDate);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ForumSessions') AND name = N'IX_ForumSessions_ExpirationDate')
BEGIN
    CREATE NONCLUSTERED INDEX IX_ForumSessions_ExpirationDate ON dbo.ForumSessions (ExpirationDate);
END
GO
