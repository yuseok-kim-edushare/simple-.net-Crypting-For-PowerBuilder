-- SQL Server CLR Assembly Deployment Script - SecureLibrary-SQL
-- Run this script once per target database by changing the @target_db variable

-- =============================================
-- CONFIGURATION - CHANGE THESE VALUES
-- =============================================
DECLARE @target_db NVARCHAR(128) = N'master';  -- <<<< CHANGE THIS FOR EACH DATABASE
DECLARE @dll_path NVARCHAR(260) = N'G:\DBMS\SecureLibrary-SQL.dll';  -- <<<< SET YOUR PATH HERE

-- =============================================
-- Enable CLR (run once per instance)
-- =============================================
EXEC sp_configure 'show advanced options', 1;
RECONFIGURE;
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;

-- =============================================
-- Trust assemblies (run once per instance)
-- =============================================

-- Trust SecureLibrary-SQL assembly
DECLARE @hash VARBINARY(64);
SELECT @hash = HASHBYTES('SHA2_512', BulkColumn)
FROM OPENROWSET(BULK 'G:\DBMS\SecureLibrary-SQL.dll', SINGLE_BLOB) AS x; -- <<<< SET YOUR PATH HERE

IF NOT EXISTS (SELECT * FROM sys.trusted_assemblies WHERE [hash] = @hash)
BEGIN
    EXEC sys.sp_add_trusted_assembly @hash = @hash, @description = N'SecureLibrary-SQL Assembly';
    PRINT 'SecureLibrary-SQL assembly hash added to trusted assemblies.';
END
ELSE
BEGIN
    PRINT 'SecureLibrary-SQL assembly hash already exists in trusted assemblies.';
END

-- =============================================
-- Switch to target database
-- =============================================
DECLARE @sql NVARCHAR(MAX) = N'USE ' + QUOTENAME(@target_db);
EXEC(@sql);

PRINT 'Deploying to database: ' + @target_db;

-- =============================================
-- Clean up existing objects
-- =============================================

-- Drop existing functions
IF OBJECT_ID('dbo.GenerateAESKey') IS NOT NULL DROP FUNCTION dbo.GenerateAESKey;
IF OBJECT_ID('dbo.EncryptAES') IS NOT NULL DROP FUNCTION dbo.EncryptAES;
IF OBJECT_ID('dbo.DecryptAES') IS NOT NULL DROP FUNCTION dbo.DecryptAES;
IF OBJECT_ID('dbo.GenerateDiffieHellmanKeys') IS NOT NULL DROP FUNCTION dbo.GenerateDiffieHellmanKeys;
IF OBJECT_ID('dbo.DeriveSharedKey') IS NOT NULL DROP FUNCTION dbo.DeriveSharedKey;
IF OBJECT_ID('dbo.HashPasswordDefault') IS NOT NULL DROP FUNCTION dbo.HashPasswordDefault;
IF OBJECT_ID('dbo.HashPasswordWithWorkFactor') IS NOT NULL DROP FUNCTION dbo.HashPasswordWithWorkFactor;
IF OBJECT_ID('dbo.VerifyPassword') IS NOT NULL DROP FUNCTION dbo.VerifyPassword;
IF OBJECT_ID('dbo.EncryptAesGcm') IS NOT NULL DROP FUNCTION dbo.EncryptAesGcm;
IF OBJECT_ID('dbo.DecryptAesGcm') IS NOT NULL DROP FUNCTION dbo.DecryptAesGcm;
IF OBJECT_ID('dbo.EncryptAesGcmWithPassword') IS NOT NULL DROP FUNCTION dbo.EncryptAesGcmWithPassword;
IF OBJECT_ID('dbo.EncryptAesGcmWithPasswordIterations') IS NOT NULL DROP FUNCTION dbo.EncryptAesGcmWithPasswordIterations;
IF OBJECT_ID('dbo.DecryptAesGcmWithPassword') IS NOT NULL DROP FUNCTION dbo.DecryptAesGcmWithPassword;
IF OBJECT_ID('dbo.DecryptAesGcmWithPasswordIterations') IS NOT NULL DROP FUNCTION dbo.DecryptAesGcmWithPasswordIterations;
IF OBJECT_ID('dbo.GenerateSalt') IS NOT NULL DROP FUNCTION dbo.GenerateSalt;
IF OBJECT_ID('dbo.GenerateSaltWithLength') IS NOT NULL DROP FUNCTION dbo.GenerateSaltWithLength;
IF OBJECT_ID('dbo.EncryptAesGcmWithPasswordAndSalt') IS NOT NULL DROP FUNCTION dbo.EncryptAesGcmWithPasswordAndSalt;
IF OBJECT_ID('dbo.EncryptAesGcmWithPasswordAndSaltIterations') IS NOT NULL DROP FUNCTION dbo.EncryptAesGcmWithPasswordAndSaltIterations;

PRINT 'Dropped existing functions';

-- Drop existing assemblies
IF EXISTS (SELECT * FROM sys.assemblies WHERE name = 'SecureLibrary-SQL') 
    DROP ASSEMBLY [SecureLibrary-SQL];

PRINT 'Dropped existing assemblies';

-- =============================================
-- Create assemblies
-- =============================================

-- Create SecureLibrary-SQL assembly
SET @sql = N'CREATE ASSEMBLY [SecureLibrary-SQL] 
FROM ''' + @dll_path + ''' 
WITH PERMISSION_SET = UNSAFE';
EXEC(@sql);
PRINT 'Created SecureLibrary-SQL assembly';

-- =============================================
-- Create functions
-- =============================================

PRINT 'Creating functions...';
GO

-- GenerateAESKey
CREATE FUNCTION dbo.GenerateAESKey()
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].GenerateAESKey;
GO

PRINT 'GenerateAESKey created';
GO

-- EncryptAES
CREATE FUNCTION dbo.EncryptAES(
    @plainText nvarchar(max), 
    @base64Key nvarchar(max))
RETURNS TABLE (
    CipherText nvarchar(max), 
    IV nvarchar(max)
)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].EncryptAES;
GO

PRINT 'EncryptAES created';
GO

-- DecryptAES
CREATE FUNCTION dbo.DecryptAES(
    @base64CipherText nvarchar(max), 
    @base64Key nvarchar(max), 
    @base64IV nvarchar(max))
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].DecryptAES;
GO

PRINT 'DecryptAES created';
GO

-- GenerateDiffieHellmanKeys
CREATE FUNCTION dbo.GenerateDiffieHellmanKeys()
RETURNS TABLE (
    PublicKey nvarchar(max), 
    PrivateKey nvarchar(max)
)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].GenerateDiffieHellmanKeys;
GO

PRINT 'GenerateDiffieHellmanKeys created';
GO

-- DeriveSharedKey
CREATE FUNCTION dbo.DeriveSharedKey(
    @otherPartyPublicKeyBase64 nvarchar(max), 
    @privateKeyBase64 nvarchar(max))
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].DeriveSharedKey;
GO

PRINT 'DeriveSharedKey created';
GO

-- HashPasswordDefault (default overload)
CREATE FUNCTION dbo.HashPasswordDefault(@password nvarchar(max))
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].HashPasswordDefault;
GO

PRINT 'HashPasswordDefault created';
GO

-- HashPasswordWithWorkFactor (with work factor)
CREATE FUNCTION dbo.HashPasswordWithWorkFactor(@password nvarchar(max), @workFactor int)
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].HashPasswordWithWorkFactor;
GO

PRINT 'HashPasswordWithWorkFactor created';
GO

-- VerifyPassword
CREATE FUNCTION dbo.VerifyPassword(
    @password nvarchar(max), 
    @hashedPassword nvarchar(max))
RETURNS bit
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].VerifyPassword;
GO

PRINT 'VerifyPassword created';
GO

-- EncryptAesGcm
CREATE FUNCTION dbo.EncryptAesGcm(
    @plainText nvarchar(max), 
    @base64Key nvarchar(max))
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].EncryptAesGcm;
GO

PRINT 'EncryptAesGcm created';
GO

-- DecryptAesGcm
CREATE FUNCTION dbo.DecryptAesGcm(
    @combinedData nvarchar(max), 
    @base64Key nvarchar(max))
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].DecryptAesGcm;
GO

PRINT 'DecryptAesGcm created';
GO

-- EncryptAesGcmWithPassword (default overload)
CREATE FUNCTION dbo.EncryptAesGcmWithPassword(
    @plainText nvarchar(max), 
    @password nvarchar(max))
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].EncryptAesGcmWithPassword;
GO

PRINT 'EncryptAesGcmWithPassword (default) created';
GO

-- EncryptAesGcmWithPasswordIterations (with iterations)
CREATE FUNCTION dbo.EncryptAesGcmWithPasswordIterations(
    @plainText nvarchar(max), 
    @password nvarchar(max), 
    @iterations int)
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].EncryptAesGcmWithPasswordIterations;
GO

PRINT 'EncryptAesGcmWithPasswordIterations created';
GO

-- DecryptAesGcmWithPassword (default overload)
CREATE FUNCTION dbo.DecryptAesGcmWithPassword(
    @base64EncryptedData nvarchar(max), 
    @password nvarchar(max))
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].DecryptAesGcmWithPassword;
GO

PRINT 'DecryptAesGcmWithPassword (default) created';
GO

-- DecryptAesGcmWithPasswordIterations (with iterations)
CREATE FUNCTION dbo.DecryptAesGcmWithPasswordIterations(
    @base64EncryptedData nvarchar(max), 
    @password nvarchar(max), 
    @iterations int)
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].DecryptAesGcmWithPasswordIterations;
GO

PRINT 'DecryptAesGcmWithPasswordIterations created';
GO

-- GenerateSalt (default overload)
CREATE FUNCTION dbo.GenerateSalt()
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].GenerateSalt;
GO

PRINT 'GenerateSalt (default) created';
GO

-- GenerateSaltWithLength (with length parameter)
CREATE FUNCTION dbo.GenerateSaltWithLength(@saltLength int)
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].GenerateSaltWithLength;
GO

PRINT 'GenerateSaltWithLength created';
GO

-- EncryptAesGcmWithPasswordAndSalt (default overload)
CREATE FUNCTION dbo.EncryptAesGcmWithPasswordAndSalt(
    @plainText nvarchar(max), 
    @password nvarchar(max), 
    @base64Salt nvarchar(max))
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].EncryptAesGcmWithPasswordAndSalt;
GO

PRINT 'EncryptAesGcmWithPasswordAndSalt (default) created';
GO

-- EncryptAesGcmWithPasswordAndSaltIterations (with iterations)
CREATE FUNCTION dbo.EncryptAesGcmWithPasswordAndSaltIterations(
    @plainText nvarchar(max), 
    @password nvarchar(max), 
    @base64Salt nvarchar(max), 
    @iterations int)
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].EncryptAesGcmWithPasswordAndSaltIterations;
GO

PRINT 'EncryptAesGcmWithPasswordAndSaltIterations created';

PRINT 'Deployment completed for database: ' + DB_NAME();

