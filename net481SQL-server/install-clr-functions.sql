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
-- STEP 2: Create Assembly (if not exists)
-- =============================================
PRINT '--- STEP 2: Creating Assembly ---';
GO

-- Drop existing assembly if it exists
IF EXISTS (SELECT * FROM sys.assemblies WHERE name = 'SecureLibrary.SQL')
BEGIN
    DROP ASSEMBLY [SecureLibrary.SQL];
    PRINT '✓ Dropped existing assembly';
END
GO

-- Create the assembly
-- Note: Replace the path with the actual path to your compiled DLL
CREATE ASSEMBLY [SecureLibrary.SQL]
FROM 'C:\Path\To\Your\SecureLibrary-SQL.dll'
WITH PERMISSION_SET = UNSAFE;
GO
PRINT '✓ Assembly created successfully';
GO

-- =============================================
-- STEP 3: CREATE SCALAR FUNCTIONS
-- =============================================
PRINT '--- STEP 3: Creating Scalar Functions ---';
GO

-- Password Hashing Functions
CREATE FUNCTION dbo.HashPassword(@password NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRFunctions].HashPassword;
GO
PRINT '✓ HashPassword';

CREATE FUNCTION dbo.HashPasswordWithWorkFactor(@password NVARCHAR(MAX), @workFactor INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRFunctions].HashPasswordWithWorkFactor;
GO
PRINT '✓ HashPasswordWithWorkFactor';

CREATE FUNCTION dbo.VerifyPassword(@password NVARCHAR(MAX), @hashedPassword NVARCHAR(MAX))
RETURNS BIT
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRFunctions].VerifyPassword;
GO
PRINT '✓ VerifyPassword';

CREATE FUNCTION dbo.GenerateSalt(@workFactor INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRFunctions].GenerateSalt;
GO
PRINT '✓ GenerateSalt';

CREATE FUNCTION dbo.GetHashInfo(@hashedPassword NVARCHAR(MAX))
RETURNS XML
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRFunctions].GetHashInfo;
GO
PRINT '✓ GetHashInfo';

-- AES-GCM Encryption Functions
CREATE FUNCTION dbo.EncryptAesGcm(@plainText NVARCHAR(MAX), @base64Key NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRFunctions].EncryptAesGcm;
GO
PRINT '✓ EncryptAesGcm';

CREATE FUNCTION dbo.DecryptAesGcm(@base64EncryptedData NVARCHAR(MAX), @base64Key NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRFunctions].DecryptAesGcm;
GO
PRINT '✓ DecryptAesGcm';

CREATE FUNCTION dbo.EncryptAesGcmWithPassword(@plainText NVARCHAR(MAX), @password NVARCHAR(MAX), @iterations INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRFunctions].EncryptAesGcmWithPassword;
GO
PRINT '✓ EncryptAesGcmWithPassword';

CREATE FUNCTION dbo.DecryptAesGcmWithPassword(@base64EncryptedData NVARCHAR(MAX), @password NVARCHAR(MAX), @iterations INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRFunctions].DecryptAesGcmWithPassword;
GO
PRINT '✓ DecryptAesGcmWithPassword';

-- Key Generation Functions
CREATE FUNCTION dbo.GenerateKey(@keySizeBits INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRFunctions].GenerateKey;
GO
PRINT '✓ GenerateKey';

CREATE FUNCTION dbo.GenerateNonce(@nonceSizeBytes INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRFunctions].GenerateNonce;
GO
PRINT '✓ GenerateNonce';

CREATE FUNCTION dbo.DeriveKeyFromPassword(@password NVARCHAR(MAX), @base64Salt NVARCHAR(MAX), @iterations INT, @keySizeBytes INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRFunctions].DeriveKeyFromPassword;
GO
PRINT '✓ DeriveKeyFromPassword';

-- XML Encryption Functions
CREATE FUNCTION dbo.EncryptXml(@xmlData XML, @password NVARCHAR(MAX), @iterations INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRFunctions].EncryptXml;
GO
PRINT '✓ EncryptXml';

CREATE FUNCTION dbo.DecryptXml(@base64EncryptedXml NVARCHAR(MAX), @password NVARCHAR(MAX), @iterations INT)
RETURNS XML
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRFunctions].DecryptXml;
GO
PRINT '✓ DecryptXml';

-- Utility Functions
CREATE FUNCTION dbo.ValidateEncryptionMetadata(@metadataXml XML)
RETURNS XML
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRFunctions].ValidateEncryptionMetadata;
GO
PRINT '✓ ValidateEncryptionMetadata';

-- =============================================
-- STEP 4: CREATE STORED PROCEDURES
-- =============================================
PRINT '--- STEP 4: Creating Stored Procedures ---';
GO

-- Table Encryption Procedures
CREATE PROCEDURE dbo.EncryptTableWithMetadata
    @tableName NVARCHAR(MAX),
    @password NVARCHAR(MAX),
    @iterations INT,
    @encryptedData NVARCHAR(MAX) OUTPUT
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRProcedures].EncryptTableWithMetadata;
GO
PRINT '✓ EncryptTableWithMetadata';

CREATE PROCEDURE dbo.DecryptTableWithMetadata
    @encryptedData NVARCHAR(MAX),
    @password NVARCHAR(MAX),
    @targetTableName NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRProcedures].DecryptTableWithMetadata;
GO
PRINT '✓ DecryptTableWithMetadata';

CREATE PROCEDURE dbo.WrapDecryptProcedure
    @encryptedData NVARCHAR(MAX),
    @password NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRProcedures].WrapDecryptProcedure;
GO
PRINT '✓ WrapDecryptProcedure';

-- Enhanced Row-Level Encryption Procedures
CREATE PROCEDURE dbo.EncryptRowWithMetadata
    @rowXml XML, -- XML from FOR XML RAW, ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE
    @password NVARCHAR(MAX),
    @iterations INT,
    @encryptedRow NVARCHAR(MAX) OUTPUT
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRProcedures].EncryptRowWithMetadata;
GO
PRINT '✓ EncryptRowWithMetadata';

CREATE PROCEDURE dbo.DecryptRowWithMetadata
    @encryptedRow NVARCHAR(MAX),
    @password NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRProcedures].DecryptRowWithMetadata;
GO
PRINT '✓ DecryptRowWithMetadata';

CREATE PROCEDURE dbo.EncryptRowsBatch
    @rowsXml XML, -- XML from FOR XML RAW, ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE
    @password NVARCHAR(MAX),
    @iterations INT,
    @batchId NVARCHAR(50)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRProcedures].EncryptRowsBatch;
GO
PRINT '✓ EncryptRowsBatch';

CREATE PROCEDURE dbo.DecryptRowsBatch
    @batchId NVARCHAR(50),
    @password NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRProcedures].DecryptRowsBatch;
GO
PRINT '✓ DecryptRowsBatch';

-- =============================================
-- STEP 5: VERIFICATION
-- =============================================
PRINT '--- STEP 5: Verification ---';
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
    'EncryptXml', 'DecryptXml', 'ValidateEncryptionMetadata'
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
-- STEP 6: TEST FUNCTIONS
-- =============================================
PRINT '--- STEP 6: Testing Functions ---';
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

PRINT '';
PRINT '=== INSTALLATION COMPLETED SUCCESSFULLY ===';
PRINT '';
PRINT 'Available Functions:';
PRINT '  - Password Hashing: HashPassword, HashPasswordWithWorkFactor, VerifyPassword, GenerateSalt, GetHashInfo';
PRINT '  - AES-GCM Encryption: EncryptAesGcm, DecryptAesGcm, EncryptAesGcmWithPassword, DecryptAesGcmWithPassword';
PRINT '  - Key Generation: GenerateKey, GenerateNonce, DeriveKeyFromPassword';
PRINT '  - XML Encryption: EncryptXml, DecryptXml';
PRINT '  - Utilities: ValidateEncryptionMetadata';
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
PRINT '  -- Encrypt XML data';
PRINT '  SELECT dbo.EncryptXml(''<Data>Secret</Data>'', ''MyPassword'', 10000)';
GO 