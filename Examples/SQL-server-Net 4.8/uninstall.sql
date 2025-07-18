-- SQL Server CLR Assembly Uninstall Script - SecureLibrary-SQL
-- Run this script to completely remove the SecureLibrary-SQL assembly and functions

-- =============================================
-- CONFIGURATION - CHANGE THESE VALUES
-- =============================================
DECLARE @target_db NVARCHAR(128) = N'master';  -- <<<< CHANGE THIS FOR EACH DATABASE
DECLARE @dll_path NVARCHAR(260) = N'G:\DBMS\SecureLibrary-SQL.dll';  -- <<<< SET YOUR PATH HERE

-- =============================================
-- Switch to target database
-- =============================================
DECLARE @sql NVARCHAR(MAX) = N'USE ' + QUOTENAME(@target_db);
EXEC(@sql);

PRINT 'Uninstalling from database: ' + @target_db;

-- =============================================
-- Drop existing functions
-- =============================================

IF OBJECT_ID('dbo.GenerateAESKey') IS NOT NULL 
BEGIN
    DROP FUNCTION dbo.GenerateAESKey;
    PRINT 'Dropped GenerateAESKey function';
END

IF OBJECT_ID('dbo.EncryptAES') IS NOT NULL 
BEGIN
    DROP FUNCTION dbo.EncryptAES;
    PRINT 'Dropped EncryptAES function';
END

IF OBJECT_ID('dbo.DecryptAES') IS NOT NULL 
BEGIN
    DROP FUNCTION dbo.DecryptAES;
    PRINT 'Dropped DecryptAES function';
END

IF OBJECT_ID('dbo.GenerateDiffieHellmanKeys') IS NOT NULL 
BEGIN
    DROP FUNCTION dbo.GenerateDiffieHellmanKeys;
    PRINT 'Dropped GenerateDiffieHellmanKeys function';
END

IF OBJECT_ID('dbo.DeriveSharedKey') IS NOT NULL 
BEGIN
    DROP FUNCTION dbo.DeriveSharedKey;
    PRINT 'Dropped DeriveSharedKey function';
END

IF OBJECT_ID('dbo.HashPassword') IS NOT NULL 
BEGIN
    DROP FUNCTION dbo.HashPassword;
    PRINT 'Dropped HashPassword function';
END

IF OBJECT_ID('dbo.VerifyPassword') IS NOT NULL 
BEGIN
    DROP FUNCTION dbo.VerifyPassword;
    PRINT 'Dropped VerifyPassword function';
END

IF OBJECT_ID('dbo.EncryptAesGcm') IS NOT NULL 
BEGIN
    DROP FUNCTION dbo.EncryptAesGcm;
    PRINT 'Dropped EncryptAesGcm function';
END

IF OBJECT_ID('dbo.DecryptAesGcm') IS NOT NULL 
BEGIN
    DROP FUNCTION dbo.DecryptAesGcm;
    PRINT 'Dropped DecryptAesGcm function';
END

IF OBJECT_ID('dbo.EncryptAesGcmWithPassword') IS NOT NULL 
BEGIN
    DROP FUNCTION dbo.EncryptAesGcmWithPassword;
    PRINT 'Dropped EncryptAesGcmWithPassword function';
END

IF OBJECT_ID('dbo.DecryptAesGcmWithPassword') IS NOT NULL 
BEGIN
    DROP FUNCTION dbo.DecryptAesGcmWithPassword;
    PRINT 'Dropped DecryptAesGcmWithPassword function';
END

IF OBJECT_ID('dbo.GenerateSalt') IS NOT NULL 
BEGIN
    DROP FUNCTION dbo.GenerateSalt;
    PRINT 'Dropped GenerateSalt function';
END

IF OBJECT_ID('dbo.EncryptAesGcmWithPasswordAndSalt') IS NOT NULL 
BEGIN
    DROP FUNCTION dbo.EncryptAesGcmWithPasswordAndSalt;
    PRINT 'Dropped EncryptAesGcmWithPasswordAndSalt function';
END

-- =============================================
-- Drop existing assemblies
-- =============================================

IF EXISTS (SELECT * FROM sys.assemblies WHERE name = 'SecureLibrary-SQL') 
BEGIN
    DROP ASSEMBLY [SecureLibrary-SQL];
    PRINT 'Dropped SecureLibrary-SQL assembly';
END
ELSE
BEGIN
    PRINT 'SecureLibrary-SQL assembly not found';
END

-- =============================================
-- Remove from trusted assemblies (instance level)
-- =============================================

DECLARE @hash VARBINARY(64);
SELECT @hash = HASHBYTES('SHA2_512', BulkColumn)
FROM OPENROWSET(BULK 'G:\DBMS\SecureLibrary-SQL.dll', SINGLE_BLOB) AS x;

IF EXISTS (SELECT * FROM sys.trusted_assemblies WHERE [hash] = @hash)
BEGIN
    EXEC sys.sp_drop_trusted_assembly @hash = @hash;
    PRINT 'Removed SecureLibrary-SQL assembly from trusted assemblies';
END
ELSE
BEGIN
    PRINT 'SecureLibrary-SQL assembly hash not found in trusted assemblies';
END

PRINT 'Uninstall completed for database: ' + DB_NAME();

