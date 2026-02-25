/********************/
/*   Version: 1.0   */
/********************/

-- Initialize.
USE Master;
GO

-- Drop exiting database.
/*DROP DATABASE {PROJECT_NAME};
GO*/

-- Create the database.
CREATE DATABASE {PROJECT_NAME}
COLLATE Hebrew_CI_AS;
GO

-- Use it.
USE {PROJECT_NAME}
GO

-- Create the login account.
CREATE LOGIN {PROJECT_NAME}_user
WITH 
PASSWORD = '{PROJECT_NAME}_P$sS_!3^',
DEFAULT_DATABASE = {PROJECT_NAME};

CREATE USER {PROJECT_NAME}_user FOR LOGIN {PROJECT_NAME}_user;
EXEC sp_addrolemember 'db_datareader', '{PROJECT_NAME}_user';
EXEC sp_addrolemember 'db_datawriter', '{PROJECT_NAME}_user';
GRANT EXECUTE TO {PROJECT_NAME}_user;
GO


--==============================--
--== Base Adminosaurus Tables ==--
--==============================--

CREATE TABLE AdminRoles
(
    Id              INT             IDENTITY(1,1),
    Name            NVARCHAR(100)   NOT NULL,
    Description     NVARCHAR(255)   NULL,
    RoleType        SMALLINT        NOT NULL,  -- 0 = DinoAdmin, 1 = RegularAdmin, 2 = Custom
    IsVisible       BIT             NOT NULL DEFAULT 1,
    IsSystemDefined BIT             NOT NULL DEFAULT 0,
    CONSTRAINT PK_AdminRoles PRIMARY KEY(Id)
);

-- Insert system-defined roles only if they don't exist
SET IDENTITY_INSERT AdminRoles ON
GO

IF NOT EXISTS (SELECT 1 FROM AdminRoles WHERE Id = 0)
    INSERT INTO AdminRoles (Id, Name, Description, RoleType, IsVisible, IsSystemDefined) VALUES (0, 'Dino Admin', 'Master administrator with full system access', 0, 0, 1);
IF NOT EXISTS (SELECT 1 FROM AdminRoles WHERE Id = 1)
    INSERT INTO AdminRoles (Id, Name, Description, RoleType, IsVisible, IsSystemDefined) VALUES (1, 'Administrator', 'Regular administrator with standard privileges', 1, 1, 1);

SET IDENTITY_INSERT AdminRoles OFF
GO

CREATE TABLE AdminUsers
(
    Id                      INT             IDENTITY(1,1),
    Email                   NVARCHAR(256)   NOT NULL,
    FullName                NVARCHAR(100)   NOT NULL,
    Phone                   VARCHAR(50)     NULL,
    RoleId                  INT             NOT NULL,  -- Direct reference to the user's role
    PasswordHash            VARBINARY(32)   NOT NULL,
    PasswordSalt            VARBINARY(32)   NOT NULL,
    EmailVerificationCode   VARCHAR(100)    NULL,
    VerificationCodeDate    DATETIME        NULL,
    PictureUrl              VARCHAR(255)    NULL,
    Active                  BIT             NOT NULL DEFAULT 1,
    CreateDate              DATETIME        NOT NULL DEFAULT GETDATE(),
	UpdateDate      		DATETIME        NOT NULL DEFAULT GETDATE(),
	UpdateBy				INT				NOT NULL DEFAULT 1,
    LastLoginDate           DATETIME        NULL,
    LastIpAddress           VARCHAR(50)     NULL,
    Archived                BIT             NOT NULL DEFAULT 0,
    CONSTRAINT PK_AdminUsers PRIMARY KEY(Id),
    CONSTRAINT UQ_AdminUsers_Email UNIQUE(Email),
    CONSTRAINT FK_AdminUsers_Role FOREIGN KEY(RoleId) REFERENCES AdminRoles(Id)
);

-- Add index for email to optimize login queries
CREATE NONCLUSTERED INDEX IX_AdminUsers_Email ON AdminUsers (Email);

SET IDENTITY_INSERT AdminUsers ON
GO

-- Add default users if they don't exist
IF NOT EXISTS (SELECT 1 FROM AdminUsers WHERE Id = 0)
    INSERT INTO AdminUsers (Id, Email, FullName, Phone, RoleId, PasswordHash, PasswordSalt, EmailVerificationCode, VerificationCodeDate, PictureUrl, Active, LastLoginDate, LastIpAddress, Archived) VALUES (0, 'admin@devdino.com', 'Dino Admin', NULL, 0, 0x6B, 0x00,NULL, NULL, NULL, 1, NULL, NULL, 0);
IF NOT EXISTS (SELECT 1 FROM AdminUsers WHERE Id = 1)
    INSERT INTO AdminUsers (Id, Email, FullName, Phone, RoleId, PasswordHash, PasswordSalt, EmailVerificationCode, VerificationCodeDate, PictureUrl, Active, LastLoginDate, LastIpAddress, Archived) VALUES (1, 'client@email.com', 'Regular Admin', NULL, 1, 0x00, 0x00, NULL, NULL, NULL, 1, NULL, NULL, 0);

SET IDENTITY_INSERT AdminUsers OFF
GO




-- Update Dino-Admin password.
DECLARE @UserId INT = 0; -- Target User (Dino Admin)
DECLARE @PlainPassword VARCHAR(50) = 'Aa1234567'; 
DECLARE @Salt VARBINARY(32);
DECLARE @PasswordBytes VARBINARY(MAX);
DECLARE @CombinedBytes VARBINARY(MAX);
DECLARE @FinalHash VARBINARY(32);

-- 1. Generate a new random 32-byte salt (Matches C# GenerateRandomSalt)
SET @Salt = CRYPT_GEN_RANDOM(32);

-- 2. Convert string to bytes
SET @PasswordBytes = CAST(@PlainPassword AS VARBINARY(MAX));

-- 3. Combine Password + Salt (Matches C# loop logic)
SET @CombinedBytes = @PasswordBytes + @Salt;

-- 4. Generate SHA256 Hash (Matches C# GenerateSaltedHashSHA256)
SET @FinalHash = HASHBYTES('SHA2_256', @CombinedBytes);

-- 5. Update the record
UPDATE AdminUsers
SET PasswordHash = @FinalHash,
    PasswordSalt = @Salt,
    UpdateDate = GETDATE(),
    UpdateBy = 0 -- Self updated
WHERE Id = @UserId;


-- Settings table for storing configuration as JSON
CREATE TABLE Settings
(
	ClassName		VARCHAR(100)	NOT NULL,
    Name            NVARCHAR(100)   NOT NULL,
    Data            NVARCHAR(MAX)   NOT NULL,  -- JSON data storage
    CreateDate      DATETIME        NOT NULL DEFAULT GETDATE(),
    UpdateDate      DATETIME        NOT NULL DEFAULT GETDATE(),  -- Track when settings were last updated
	UpdateBy		INT				NOT NULL DEFAULT 1,
    CONSTRAINT PK_Settings PRIMARY KEY(ClassName)
);

-- Add basic settings if they don't exist
IF NOT EXISTS (SELECT 1 FROM Settings WHERE ClassName = 'DinoMasterSettings')
    INSERT INTO Settings (ClassName, Name, Data) VALUES ('DinoMasterSettings', 'Dino Master Settings', N'{}');
IF NOT EXISTS (SELECT 1 FROM Settings WHERE ClassName = 'SystemSettings')
    INSERT INTO Settings (ClassName, Name, Data) VALUES ('SystemSettings', 'System Settings', N'{}');


--====================--
--== Project Tables ==--
--====================--

