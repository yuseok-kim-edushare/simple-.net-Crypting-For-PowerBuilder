-- SQL Server CLR Assembly Complete Installation Script - SecureLibrary-SQL
-- This is a unified installation script that includes all functionality from PR #61
-- Run this script once per target database by changing the configuration variables

-- =============================================
-- CONFIGURATION - CHANGE THESE VALUES
-- =============================================
DECLARE @target_db NVARCHAR(128) = N'master';  -- <<<< CHANGE THIS FOR EACH DATABASE
DECLARE @dll_path NVARCHAR(260) = N'C:\Path\To\SecureLibrary-SQL.dll';  -- <<<< SET YOUR DLL PATH HERE

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
FROM OPENROWSET(BULK 'C:\Path\To\SecureLibrary-SQL.dll', SINGLE_BLOB) AS x; -- <<<< SET YOUR PATH HERE

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

-- Drop existing functions and procedures
IF OBJECT_ID('dbo.RestoreEncryptedTable', 'P') IS NOT NULL DROP PROCEDURE dbo.RestoreEncryptedTable;
IF OBJECT_ID('dbo.EncryptRowDataAesGcm') IS NOT NULL DROP FUNCTION dbo.EncryptRowDataAesGcm;
IF OBJECT_ID('dbo.DecryptRowDataAesGcm') IS NOT NULL DROP FUNCTION dbo.DecryptRowDataAesGcm;
IF OBJECT_ID('dbo.EncryptTableRowsAesGcm') IS NOT NULL DROP FUNCTION dbo.EncryptTableRowsAesGcm;
IF OBJECT_ID('dbo.BulkProcessRowsAesGcm', 'P') IS NOT NULL DROP PROCEDURE dbo.BulkProcessRowsAesGcm;
IF OBJECT_ID('dbo.EncryptXmlWithPassword') IS NOT NULL DROP FUNCTION dbo.EncryptXmlWithPassword;
IF OBJECT_ID('dbo.EncryptXmlWithPasswordIterations') IS NOT NULL DROP FUNCTION dbo.EncryptXmlWithPasswordIterations;
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
IF OBJECT_ID('dbo.DeriveKeyFromPassword') IS NOT NULL DROP FUNCTION dbo.DeriveKeyFromPassword;
IF OBJECT_ID('dbo.DeriveKeyFromPasswordIterations') IS NOT NULL DROP FUNCTION dbo.DeriveKeyFromPasswordIterations;
IF OBJECT_ID('dbo.EncryptAesGcmWithDerivedKey') IS NOT NULL DROP FUNCTION dbo.EncryptAesGcmWithDerivedKey;
IF OBJECT_ID('dbo.DecryptAesGcmWithDerivedKey') IS NOT NULL DROP FUNCTION dbo.DecryptAesGcmWithDerivedKey;

PRINT 'Dropped existing functions and procedures';

-- Drop existing assemblies
IF EXISTS (SELECT * FROM sys.assemblies WHERE name = 'SecureLibrary-SQL') 
    DROP ASSEMBLY [SecureLibrary-SQL];
IF EXISTS (SELECT * FROM sys.assemblies WHERE name = 'SimpleDotNetCrypting') 
    DROP ASSEMBLY [SimpleDotNetCrypting];

PRINT 'Dropped existing assemblies';

-- =============================================
-- Create assembly
-- =============================================

-- Create SecureLibrary-SQL assembly
SET @sql = N'CREATE ASSEMBLY [SecureLibrary-SQL] 
FROM ''' + @dll_path + ''' 
WITH PERMISSION_SET = UNSAFE';
EXEC(@sql);
PRINT 'Created SecureLibrary-SQL assembly with UNSAFE permission set';

-- =============================================
-- Create all functions and procedures
-- =============================================

PRINT 'Creating all functions and procedures...';
GO

-- Password-Based Table Encryption Functions (NEW in PR #61)
CREATE FUNCTION dbo.EncryptXmlWithPassword(
    @xmlData XML, 
    @password NVARCHAR(MAX)
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].EncryptXmlWithPassword;
GO

CREATE FUNCTION dbo.EncryptXmlWithPasswordIterations(
    @xmlData XML, 
    @password NVARCHAR(MAX),
    @iterations INT
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].EncryptXmlWithPasswordIterations;
GO

-- Universal procedure to decrypt and restore any table (NEW in PR #61)
CREATE PROCEDURE dbo.RestoreEncryptedTable
    @encryptedData NVARCHAR(MAX),
    @password NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].RestoreEncryptedTable;
GO

-- Row-by-Row Encryption Functions (NEW in PR #61)
CREATE FUNCTION dbo.EncryptRowDataAesGcm(
    @jsonRowData NVARCHAR(MAX),
    @base64Key NVARCHAR(MAX),
    @base64Nonce NVARCHAR(MAX)
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].EncryptRowDataAesGcm;
GO

CREATE FUNCTION dbo.DecryptRowDataAesGcm(
    @encryptedData NVARCHAR(MAX),
    @base64Key NVARCHAR(MAX)
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].DecryptRowDataAesGcm;
GO

CREATE FUNCTION dbo.EncryptTableRowsAesGcm(
    @jsonArrayData NVARCHAR(MAX),
    @base64Key NVARCHAR(MAX),
    @base64Nonce NVARCHAR(MAX)
)
RETURNS TABLE (
    RowId INT,
    EncryptedData NVARCHAR(MAX),
    AuthTag NVARCHAR(MAX)
)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].EncryptTableRowsAesGcm;
GO

CREATE PROCEDURE dbo.BulkProcessRowsAesGcm
    @jsonArrayData NVARCHAR(MAX),
    @base64Key NVARCHAR(MAX),
    @base64Nonce NVARCHAR(MAX),
    @batchSize INT = 1000
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].BulkProcessRowsAesGcm;
GO

-- Core Cryptographic Functions
CREATE FUNCTION dbo.GenerateAESKey()
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].GenerateAESKey;
GO

CREATE FUNCTION dbo.EncryptAesGcm(
    @plainText nvarchar(max), 
    @base64Key nvarchar(max))
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].EncryptAesGcm;
GO

CREATE FUNCTION dbo.DecryptAesGcm(
    @combinedData nvarchar(max), 
    @base64Key nvarchar(max))
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].DecryptAesGcm;
GO

CREATE FUNCTION dbo.EncryptAesGcmWithPassword(
    @plainText nvarchar(max), 
    @password nvarchar(max))
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].EncryptAesGcmWithPassword;
GO

CREATE FUNCTION dbo.DecryptAesGcmWithPassword(
    @base64EncryptedData nvarchar(max), 
    @password nvarchar(max))
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].DecryptAesGcmWithPassword;
GO

-- Password Hashing Functions
CREATE FUNCTION dbo.HashPasswordDefault(@password nvarchar(max))
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].HashPasswordDefault;
GO

CREATE FUNCTION dbo.VerifyPassword(
    @password nvarchar(max), 
    @hashedPassword nvarchar(max))
RETURNS bit
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].VerifyPassword;
GO

-- Diffie-Hellman Key Exchange Functions
CREATE FUNCTION dbo.GenerateDiffieHellmanKeys()
RETURNS TABLE (
    PublicKey nvarchar(max), 
    PrivateKey nvarchar(max)
)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].GenerateDiffieHellmanKeys;
GO

CREATE FUNCTION dbo.DeriveSharedKey(
    @otherPartyPublicKeyBase64 nvarchar(max), 
    @privateKeyBase64 nvarchar(max))
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].DeriveSharedKey;
GO

PRINT 'All functions and procedures created successfully!';

-- =============================================
-- Verify installation
-- =============================================
SELECT 
    SCHEMA_NAME(o.schema_id) AS SchemaName,
    o.name AS ObjectName,
    o.type_desc AS ObjectType,
    o.create_date AS CreateDate
FROM sys.objects o
WHERE o.name IN (
    'EncryptXmlWithPassword', 
    'EncryptXmlWithPasswordIterations',
    'RestoreEncryptedTable',
    'EncryptRowDataAesGcm',
    'DecryptRowDataAesGcm',
    'EncryptTableRowsAesGcm',
    'BulkProcessRowsAesGcm',
    'GenerateAESKey',
    'EncryptAesGcm',
    'DecryptAesGcm',
    'EncryptAesGcmWithPassword',
    'DecryptAesGcmWithPassword',
    'HashPasswordDefault',
    'VerifyPassword',
    'GenerateDiffieHellmanKeys',
    'DeriveSharedKey'
)
ORDER BY o.name;

PRINT '';
PRINT '=== INSTALLATION COMPLETED SUCCESSFULLY ===';
PRINT 'Database: ' + DB_NAME();
PRINT '';
PRINT 'Available Functions:';
PRINT '✓ Password-Based Table Encryption (NEW): EncryptXmlWithPassword, RestoreEncryptedTable';
PRINT '✓ Row-by-Row Encryption (NEW): EncryptRowDataAesGcm, DecryptRowDataAesGcm, EncryptTableRowsAesGcm';
PRINT '✓ Core AES-GCM Encryption: EncryptAesGcm, DecryptAesGcm, EncryptAesGcmWithPassword';
PRINT '✓ Password Hashing: HashPasswordDefault, VerifyPassword';
PRINT '✓ Key Exchange: GenerateDiffieHellmanKeys, DeriveSharedKey';
PRINT '';
PRINT 'Next: Run example.sql to see usage examples of all features.'
PRINT '      For practical, developer-focused examples addressing dynamic table'
PRINT '      creation and schema comparison, see practical-examples.sql';