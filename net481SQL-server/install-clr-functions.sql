-- =============================================
-- SQL Server CLR Functions and Procedures Installation
-- For SecureLibrary.SQL Assembly
-- =============================================

PRINT '=== Installing SQL Server CLR Functions and Procedures ===';
GO

-- =============================================
-- STEP 1: Enable CLR Integration (if not already enabled)
-- =============================================
PRINT '--- STEP 1: Enabling CLR Integration ---';
GO

-- Check if CLR is enabled
IF (SELECT value FROM sys.configurations WHERE name = 'clr enabled') = 0
BEGIN
    EXEC sp_configure 'clr enabled', 1;
    RECONFIGURE;
    PRINT '✓ CLR Integration enabled';
END
ELSE
BEGIN
    PRINT '✓ CLR Integration already enabled';
END
GO

-- =============================================
-- STEP 2: Uninstall Existing Objects (if assembly exists)
-- =============================================
PRINT '--- STEP 2: Uninstalling Existing Objects ---';
GO

-- Check if assembly exists and uninstall all dependent objects first
IF EXISTS (SELECT * FROM sys.assemblies WHERE name = 'SecureLibrary.SQL')
BEGIN
    PRINT 'Found existing assembly. Uninstalling dependent objects...';
    
    -- Drop stored procedures first
    IF EXISTS (SELECT * FROM sys.objects WHERE name = 'EncryptTableWithMetadata' AND type = 'PC')
    BEGIN
        DROP PROCEDURE dbo.EncryptTableWithMetadata;
        PRINT '✓ Dropped EncryptTableWithMetadata';
    END
    
    IF EXISTS (SELECT * FROM sys.objects WHERE name = 'DecryptTableWithMetadata' AND type = 'PC')
    BEGIN
        DROP PROCEDURE dbo.DecryptTableWithMetadata;
        PRINT '✓ Dropped DecryptTableWithMetadata';
    END
    
    IF EXISTS (SELECT * FROM sys.objects WHERE name = 'WrapDecryptProcedure' AND type = 'PC')
    BEGIN
        DROP PROCEDURE dbo.WrapDecryptProcedure;
        PRINT '✓ Dropped WrapDecryptProcedure';
    END
    
    IF EXISTS (SELECT * FROM sys.objects WHERE name = 'EncryptRowWithMetadata' AND type = 'PC')
    BEGIN
        DROP PROCEDURE dbo.EncryptRowWithMetadata;
        PRINT '✓ Dropped EncryptRowWithMetadata';
    END
    
    IF EXISTS (SELECT * FROM sys.objects WHERE name = 'DecryptRowWithMetadata' AND type = 'PC')
    BEGIN
        DROP PROCEDURE dbo.DecryptRowWithMetadata;
        PRINT '✓ Dropped DecryptRowWithMetadata';
    END
    
    IF EXISTS (SELECT * FROM sys.objects WHERE name = 'EncryptRowsBatch' AND type = 'PC')
    BEGIN
        DROP PROCEDURE dbo.EncryptRowsBatch;
        PRINT '✓ Dropped EncryptRowsBatch';
    END
    
    IF EXISTS (SELECT * FROM sys.objects WHERE name = 'DecryptRowsBatch' AND type = 'PC')
    BEGIN
        DROP PROCEDURE dbo.DecryptRowsBatch;
        PRINT '✓ Dropped DecryptRowsBatch';
    END
    
    -- Drop functions
    IF EXISTS (SELECT * FROM sys.objects WHERE name = 'HashPassword' AND type = 'FS')
    BEGIN
        DROP FUNCTION dbo.HashPassword;
        PRINT '✓ Dropped HashPassword';
    END
    
    IF EXISTS (SELECT * FROM sys.objects WHERE name = 'HashPasswordWithWorkFactor' AND type = 'FS')
    BEGIN
        DROP FUNCTION dbo.HashPasswordWithWorkFactor;
        PRINT '✓ Dropped HashPasswordWithWorkFactor';
    END
    
    IF EXISTS (SELECT * FROM sys.objects WHERE name = 'VerifyPassword' AND type = 'FS')
    BEGIN
        DROP FUNCTION dbo.VerifyPassword;
        PRINT '✓ Dropped VerifyPassword';
    END
    
    IF EXISTS (SELECT * FROM sys.objects WHERE name = 'GenerateSalt' AND type = 'FS')
    BEGIN
        DROP FUNCTION dbo.GenerateSalt;
        PRINT '✓ Dropped GenerateSalt';
    END
    
    IF EXISTS (SELECT * FROM sys.objects WHERE name = 'GetHashInfo' AND type = 'FS')
    BEGIN
        DROP FUNCTION dbo.GetHashInfo;
        PRINT '✓ Dropped GetHashInfo';
    END
    
    IF EXISTS (SELECT * FROM sys.objects WHERE name = 'EncryptAesGcm' AND type = 'FS')
    BEGIN
        DROP FUNCTION dbo.EncryptAesGcm;
        PRINT '✓ Dropped EncryptAesGcm';
    END
    
    IF EXISTS (SELECT * FROM sys.objects WHERE name = 'DecryptAesGcm' AND type = 'FS')
    BEGIN
        DROP FUNCTION dbo.DecryptAesGcm;
        PRINT '✓ Dropped DecryptAesGcm';
    END
    
    IF EXISTS (SELECT * FROM sys.objects WHERE name = 'EncryptAesGcmWithPassword' AND type = 'FS')
    BEGIN
        DROP FUNCTION dbo.EncryptAesGcmWithPassword;
        PRINT '✓ Dropped EncryptAesGcmWithPassword';
    END
    
    IF EXISTS (SELECT * FROM sys.objects WHERE name = 'DecryptAesGcmWithPassword' AND type = 'FS')
    BEGIN
        DROP FUNCTION dbo.DecryptAesGcmWithPassword;
        PRINT '✓ Dropped DecryptAesGcmWithPassword';
    END
    
    IF EXISTS (SELECT * FROM sys.objects WHERE name = 'GenerateKey' AND type = 'FS')
    BEGIN
        DROP FUNCTION dbo.GenerateKey;
        PRINT '✓ Dropped GenerateKey';
    END
    
    IF EXISTS (SELECT * FROM sys.objects WHERE name = 'GenerateNonce' AND type = 'FS')
    BEGIN
        DROP FUNCTION dbo.GenerateNonce;
        PRINT '✓ Dropped GenerateNonce';
    END
    
    IF EXISTS (SELECT * FROM sys.objects WHERE name = 'DeriveKeyFromPassword' AND type = 'FS')
    BEGIN
        DROP FUNCTION dbo.DeriveKeyFromPassword;
        PRINT '✓ Dropped DeriveKeyFromPassword';
    END
    
    IF EXISTS (SELECT * FROM sys.objects WHERE name = 'EncryptXml' AND type = 'FS')
    BEGIN
        DROP FUNCTION dbo.EncryptXml;
        PRINT '✓ Dropped EncryptXml';
    END
    
    IF EXISTS (SELECT * FROM sys.objects WHERE name = 'DecryptXml' AND type = 'FS')
    BEGIN
        DROP FUNCTION dbo.DecryptXml;
        PRINT '✓ Dropped DecryptXml';
    END
    
    IF EXISTS (SELECT * FROM sys.objects WHERE name = 'EncryptValue' AND type = 'FS')
    BEGIN
        DROP FUNCTION dbo.EncryptValue;
        PRINT '✓ Dropped EncryptValue';
    END
    
    IF EXISTS (SELECT * FROM sys.objects WHERE name = 'DecryptValue' AND type = 'FS')
    BEGIN
        DROP FUNCTION dbo.DecryptValue;
        PRINT '✓ Dropped DecryptValue';
    END
    
    IF EXISTS (SELECT * FROM sys.objects WHERE name = 'ValidateEncryptionMetadata' AND type = 'FS')
    BEGIN
        DROP FUNCTION dbo.ValidateEncryptionMetadata;
        PRINT '✓ Dropped ValidateEncryptionMetadata';
    END
    
    -- Now drop the assembly
    DROP ASSEMBLY [SecureLibrary.SQL];
    PRINT '✓ Dropped existing assembly';
END
ELSE
BEGIN
    PRINT '✓ No existing assembly found';
END
GO

-- =============================================
-- STEP 3: Create Assembly
-- =============================================
PRINT '--- STEP 3: Creating Assembly ---';
GO

-- Get the hash of the assembly
DECLARE @hash VARBINARY(64);
SELECT @hash = HASHBYTES('SHA2_512', BulkColumn)
FROM OPENROWSET(BULK 'C:\CLR\SecureLibrary-SQL.dll', SINGLE_BLOB) AS x; -- <<<< SET YOUR PATH HERE

-- Trust the assembly
EXEC sys.sp_add_trusted_assembly @hash = @hash, @description = N'SecureLibrary-SQL Assembly';
PRINT '✓ Trusted assembly';

-- Create the assembly
-- Note: Replace the path with the actual path to your compiled DLL
CREATE ASSEMBLY [SecureLibrary.SQL]
FROM 'C:\CLR\SecureLibrary-SQL.dll'
WITH PERMISSION_SET = UNSAFE;
GO
PRINT '✓ Assembly created successfully';
GO

-- =============================================
-- STEP 4: CREATE SCALAR FUNCTIONS
-- =============================================
PRINT '--- STEP 4: Creating Scalar Functions ---';
GO

-- Password Hashing Functions
CREATE FUNCTION dbo.HashPassword(@password NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRFunctions].HashPassword;
PRINT '✓ HashPassword';
GO

CREATE FUNCTION dbo.HashPasswordWithWorkFactor(@password NVARCHAR(MAX), @workFactor INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRFunctions].HashPasswordWithWorkFactor;
PRINT '✓ HashPasswordWithWorkFactor';
GO

CREATE FUNCTION dbo.VerifyPassword(@password NVARCHAR(MAX), @hashedPassword NVARCHAR(MAX))
RETURNS BIT
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRFunctions].VerifyPassword;
PRINT '✓ VerifyPassword';
GO

CREATE FUNCTION dbo.GenerateSalt(@workFactor INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRFunctions].GenerateSalt;
PRINT '✓ GenerateSalt';
GO

CREATE FUNCTION dbo.GetHashInfo(@hashedPassword NVARCHAR(MAX))
RETURNS XML
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRFunctions].GetHashInfo;
PRINT '✓ GetHashInfo';
GO

-- AES-GCM Encryption Functions
CREATE FUNCTION dbo.EncryptAesGcm(@plainText NVARCHAR(MAX), @base64Key NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRFunctions].EncryptAesGcm;
PRINT '✓ EncryptAesGcm';
GO

CREATE FUNCTION dbo.DecryptAesGcm(@base64EncryptedData NVARCHAR(MAX), @base64Key NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRFunctions].DecryptAesGcm;
PRINT '✓ DecryptAesGcm';
GO

CREATE FUNCTION dbo.EncryptAesGcmWithPassword(@plainText NVARCHAR(MAX), @password NVARCHAR(MAX), @iterations INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRFunctions].EncryptAesGcmWithPassword;
PRINT '✓ EncryptAesGcmWithPassword';
GO

CREATE FUNCTION dbo.DecryptAesGcmWithPassword(@base64EncryptedData NVARCHAR(MAX), @password NVARCHAR(MAX), @iterations INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRFunctions].DecryptAesGcmWithPassword;
PRINT '✓ DecryptAesGcmWithPassword';
GO

-- Key Generation Functions
CREATE FUNCTION dbo.GenerateKey(@keySizeBits INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRFunctions].GenerateKey;
PRINT '✓ GenerateKey';
GO

CREATE FUNCTION dbo.GenerateNonce(@nonceSizeBytes INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRFunctions].GenerateNonce;
PRINT '✓ GenerateNonce';
GO

CREATE FUNCTION dbo.DeriveKeyFromPassword(@password NVARCHAR(MAX), @base64Salt NVARCHAR(MAX), @iterations INT, @keySizeBytes INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRFunctions].DeriveKeyFromPassword;
PRINT '✓ DeriveKeyFromPassword';
GO

-- XML Encryption Functions
CREATE FUNCTION dbo.EncryptXml(@xmlData XML, @password NVARCHAR(MAX), @iterations INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRFunctions].EncryptXml;
PRINT '✓ EncryptXml';
GO

CREATE FUNCTION dbo.DecryptXml(@base64EncryptedXml NVARCHAR(MAX), @password NVARCHAR(MAX), @iterations INT)
RETURNS XML
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRFunctions].DecryptXml;
PRINT '✓ DecryptXml';
GO

-- Single Value Encryption Functions
CREATE FUNCTION dbo.EncryptValue(@value NVARCHAR(MAX), @password NVARCHAR(MAX), @iterations INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRFunctions].EncryptValue;
PRINT '✓ EncryptValue';
GO

CREATE FUNCTION dbo.DecryptValue(@encryptedValue NVARCHAR(MAX), @password NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRFunctions].DecryptValue;
PRINT '✓ DecryptValue';
GO

-- Utility Functions
CREATE FUNCTION dbo.ValidateEncryptionMetadata(@metadataXml XML)
RETURNS XML
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRFunctions].ValidateEncryptionMetadata;
PRINT '✓ ValidateEncryptionMetadata';
GO

-- =============================================
-- STEP 5: CREATE STORED PROCEDURES
-- =============================================
PRINT '--- STEP 5: Creating Stored Procedures ---';
GO

-- Table Encryption Procedures
CREATE PROCEDURE dbo.EncryptTableWithMetadata
    @tableName NVARCHAR(MAX),
    @password NVARCHAR(MAX),
    @iterations INT,
    @encryptedData NVARCHAR(MAX) OUTPUT
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRProcedures].EncryptTableWithMetadata;
PRINT '✓ EncryptTableWithMetadata';
GO

CREATE PROCEDURE dbo.DecryptTableWithMetadata
    @encryptedData NVARCHAR(MAX),
    @password NVARCHAR(MAX),
    @targetTableName NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRProcedures].DecryptTableWithMetadata;
PRINT '✓ DecryptTableWithMetadata';
GO

CREATE PROCEDURE dbo.WrapDecryptProcedure
    @encryptedData NVARCHAR(MAX),
    @password NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRProcedures].WrapDecryptProcedure;
PRINT '✓ WrapDecryptProcedure';
GO

-- Enhanced Row-Level Encryption Procedures
CREATE PROCEDURE dbo.EncryptRowWithMetadata
    @rowXml XML, -- XML from FOR XML RAW, ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE
    @password NVARCHAR(MAX),
    @iterations INT,
    @encryptedRow NVARCHAR(MAX) OUTPUT
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRProcedures].EncryptRowWithMetadata;
PRINT '✓ EncryptRowWithMetadata';
GO

CREATE PROCEDURE dbo.DecryptRowWithMetadata
    @encryptedRow NVARCHAR(MAX),
    @password NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRProcedures].DecryptRowWithMetadata;
PRINT '✓ DecryptRowWithMetadata';
GO

CREATE PROCEDURE dbo.EncryptRowsBatch
    @rowsXml XML, -- XML from FOR XML RAW, ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE
    @password NVARCHAR(MAX),
    @iterations INT,
    @batchId NVARCHAR(50)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRProcedures].EncryptRowsBatch;
PRINT '✓ EncryptRowsBatch';
GO

CREATE PROCEDURE dbo.DecryptRowsBatch
    @batchId NVARCHAR(50),
    @password NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRProcedures].DecryptRowsBatch;
PRINT '✓ DecryptRowsBatch';
GO

-- =============================================
-- STEP 6: VERIFICATION
-- =============================================
PRINT '--- STEP 6: Verification ---';
GO

-- Verify assembly
PRINT 'Checking assembly...';
SELECT 
    a.name AS AssemblyName,
    a.permission_set_desc AS PermissionSet,
    a.create_date AS CreateDate
FROM sys.assemblies a
WHERE a.name = 'SecureLibrary.SQL';
GO

-- List all created functions
PRINT 'Checking created functions...';
SELECT 
    o.name AS FunctionName,
    o.type_desc AS ObjectType,
    o.create_date AS CreateDate
FROM sys.objects o
WHERE o.type = 'FS' AND o.name IN (
    'HashPassword', 'HashPasswordWithWorkFactor', 'VerifyPassword', 'GenerateSalt', 'GetHashInfo',
    'EncryptAesGcm', 'DecryptAesGcm', 'EncryptAesGcmWithPassword', 'DecryptAesGcmWithPassword',
    'GenerateKey', 'GenerateNonce', 'DeriveKeyFromPassword',
    'EncryptXml', 'DecryptXml', 'ValidateEncryptionMetadata',
    'EncryptValue', 'DecryptValue'
)
ORDER BY o.name;
GO

-- List all stored procedures
PRINT 'Checking stored procedures...';
SELECT 
    o.name AS ProcedureName,
    o.type_desc AS ObjectType,
    o.create_date AS CreateDate
FROM sys.objects o
WHERE o.type = 'PC' AND o.name IN (
    'EncryptTableWithMetadata', 'DecryptTableWithMetadata', 'WrapDecryptProcedure',
    'EncryptRowWithMetadata', 'DecryptRowWithMetadata', 'EncryptRowsBatch', 'DecryptRowsBatch'
)
ORDER BY o.name;
GO

-- =============================================
-- STEP 7: TEST FUNCTIONS
-- =============================================
PRINT '--- STEP 7: Testing Functions ---';
GO

-- Test password hashing
DECLARE @testPassword NVARCHAR(MAX) = 'TestPassword123!';
DECLARE @hashedPassword NVARCHAR(MAX);
DECLARE @isValid BIT;

SET @hashedPassword = dbo.HashPassword(@testPassword);
PRINT 'Password hashing test: ' + CASE WHEN @hashedPassword IS NOT NULL THEN 'PASSED' ELSE 'FAILED' END;

SET @isValid = dbo.VerifyPassword(@testPassword, @hashedPassword);
PRINT 'Password verification test: ' + CASE WHEN @isValid = 1 THEN 'PASSED' ELSE 'FAILED' END;

-- Test key generation
DECLARE @generatedKey NVARCHAR(MAX) = dbo.GenerateKey(256);
PRINT 'Key generation test: ' + CASE WHEN @generatedKey IS NOT NULL THEN 'PASSED' ELSE 'FAILED' END;

-- Test AES-GCM encryption
DECLARE @plainText NVARCHAR(MAX) = 'Hello, World!';
DECLARE @encryptedText NVARCHAR(MAX);
DECLARE @decryptedText NVARCHAR(MAX);

SET @encryptedText = dbo.EncryptAesGcmWithPassword(@plainText, @testPassword, 10000);
PRINT 'AES-GCM encryption test: ' + CASE WHEN @encryptedText IS NOT NULL THEN 'PASSED' ELSE 'FAILED' END;

SET @decryptedText = dbo.DecryptAesGcmWithPassword(@encryptedText, @testPassword, 10000);
PRINT 'AES-GCM decryption test: ' + CASE WHEN @decryptedText = @plainText THEN 'PASSED' ELSE 'FAILED' END;

-- Test XML encryption
DECLARE @testXml XML = '<TestData><Name>John Doe</Name><Age>30</Age></TestData>';
DECLARE @encryptedXml NVARCHAR(MAX);
DECLARE @decryptedXml XML;

SET @encryptedXml = dbo.EncryptXml(@testXml, @testPassword, 10000);
PRINT 'XML encryption test: ' + CASE WHEN @encryptedXml IS NOT NULL THEN 'PASSED' ELSE 'FAILED' END;

SET @decryptedXml = dbo.DecryptXml(@encryptedXml, @testPassword, 10000);
PRINT 'XML decryption test: ' + CASE WHEN @decryptedXml IS NOT NULL THEN 'PASSED' ELSE 'FAILED' END;

-- Test Single Value Encryption
DECLARE @testValue NVARCHAR(MAX) = 'My secret value';
DECLARE @encryptedValue NVARCHAR(MAX);
DECLARE @decryptedValue NVARCHAR(MAX);

SET @encryptedValue = dbo.EncryptValue(@testValue, @testPassword, 10000);
PRINT 'Single value encryption test: ' + CASE WHEN @encryptedValue IS NOT NULL THEN 'PASSED' ELSE 'FAILED' END;

SET @decryptedValue = dbo.DecryptValue(@encryptedValue, @testPassword);
PRINT 'Single value decryption test: ' + CASE WHEN @decryptedValue = @testValue THEN 'PASSED' ELSE 'FAILED' END;

PRINT '';
PRINT '=== INSTALLATION COMPLETED SUCCESSFULLY ===';
PRINT '';
PRINT 'Available Functions:';
PRINT '  - Password Hashing: HashPassword, HashPasswordWithWorkFactor, VerifyPassword, GenerateSalt, GetHashInfo';
PRINT '  - AES-GCM Encryption: EncryptAesGcm, DecryptAesGcm, EncryptAesGcmWithPassword, DecryptAesGcmWithPassword';
PRINT '  - Key Generation: GenerateKey, GenerateNonce, DeriveKeyFromPassword';
PRINT '  - XML Encryption: EncryptXml, DecryptXml';
PRINT '  - Utilities: ValidateEncryptionMetadata';
PRINT '  - Single Value Encryption: EncryptValue, DecryptValue';
PRINT '';
PRINT 'Available Stored Procedures:';
PRINT '  - Table Operations: EncryptTableWithMetadata, DecryptTableWithMetadata, WrapDecryptProcedure';
PRINT '  - Row Operations: EncryptRowWithMetadata, DecryptRowWithMetadata, EncryptRowsBatch, DecryptRowsBatch';
PRINT '';
PRINT 'Example Usage:';
PRINT '  -- Hash a password';
PRINT '  SELECT dbo.HashPassword(''MyPassword123!'')';
PRINT '';

PRINT '  -- Encrypt text with password';
PRINT '  SELECT dbo.EncryptAesGcmWithPassword(''Secret data'', ''MyPassword'', 10000)';
PRINT '';

PRINT '  -- Encrypt a single value';
PRINT '  SELECT dbo.EncryptValue(''My secret value'', ''MyPassword'', 10000)';
PRINT '';

PRINT '  -- Encrypt XML data';
PRINT '  SELECT dbo.EncryptXml(''<Data>Secret</Data>'', ''MyPassword'', 10000)';
GO 