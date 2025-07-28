-- =============================================
-- COMPLETE SQL SERVER CLR DEPLOYMENT SCRIPT
-- =============================================
-- SecureLibrary-SQL: Complete Installation and Setup
-- This script deploys the entire CLR assembly with all functions and procedures
-- 
-- Features Included:
-- ✓ AES-GCM Encryption/Decryption
-- ✓ Password-based Key Derivation (PBKDF2)
-- ✓ Diffie-Hellman Key Exchange
-- ✓ BCrypt Password Hashing
-- ✓ Table-Level Encryption with Metadata
-- ✓ XML Encryption with Schema Inference
-- ✓ Dynamic Temp Table Wrapper
-- ✓ Automatic Type Casting
-- ✓ Stored Procedure Result Set Handling
-- =============================================

-- =============================================
-- CONFIGURATION - CHANGE THESE VALUES
-- =============================================
DECLARE @target_db NVARCHAR(128) = N'YourDatabase';  -- <<<< CHANGE THIS FOR EACH DATABASE
DECLARE @dll_path NVARCHAR(260) = N'C:\CLR\SecureLibrary-SQL.dll';  -- <<<< SET YOUR DLL PATH HERE

PRINT '=== SECURELIBRARY-SQL COMPLETE DEPLOYMENT ===';
PRINT 'Target Database: ' + @target_db;
PRINT 'DLL Path: ' + @dll_path;
PRINT '';

-- =============================================
-- STEP 1: ENABLE CLR INTEGRATION
-- =============================================
PRINT '--- STEP 1: Enabling CLR Integration ---';

EXEC sp_configure 'show advanced options', 1;
RECONFIGURE;
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;

PRINT 'CLR integration enabled successfully.';
PRINT '';

-- =============================================
-- STEP 2: TRUST ASSEMBLY (INSTANCE-LEVEL)
-- =============================================
PRINT '--- STEP 2: Trusting Assembly (Instance Level) ---';

DECLARE @hash VARBINARY(64);
SELECT @hash = HASHBYTES('SHA2_512', BulkColumn)
FROM OPENROWSET(BULK @dll_path, SINGLE_BLOB) AS x;

IF NOT EXISTS (SELECT * FROM sys.trusted_assemblies WHERE [hash] = @hash)
BEGIN
    EXEC sys.sp_add_trusted_assembly @hash = @hash, @description = N'SecureLibrary-SQL Assembly';
    PRINT 'SecureLibrary-SQL assembly hash added to trusted assemblies.';
END
ELSE
BEGIN
    PRINT 'SecureLibrary-SQL assembly hash already exists in trusted assemblies.';
END
PRINT '';

-- =============================================
-- STEP 3: SWITCH TO TARGET DATABASE
-- =============================================
PRINT '--- STEP 3: Switching to Target Database ---';

DECLARE @sql NVARCHAR(MAX) = N'USE ' + QUOTENAME(@target_db);
EXEC(@sql);

PRINT 'Now deploying to database: ' + @target_db;
PRINT '';

-- =============================================
-- STEP 4: CLEAN UP EXISTING OBJECTS
-- =============================================
PRINT '--- STEP 4: Cleaning Up Existing Objects ---';

-- Drop Stored Procedures
BEGIN TRY
    IF OBJECT_ID('dbo.RestoreEncryptedTable', 'P') IS NOT NULL 
    BEGIN
        DROP PROCEDURE dbo.RestoreEncryptedTable;
        PRINT '✓ Dropped RestoreEncryptedTable';
    END
END TRY
BEGIN CATCH
    PRINT 'Could not drop RestoreEncryptedTable: ' + ERROR_MESSAGE();
END CATCH

BEGIN TRY
    IF OBJECT_ID('dbo.WrapDecryptProcedure', 'P') IS NOT NULL 
    BEGIN
        DROP PROCEDURE dbo.WrapDecryptProcedure;
        PRINT '✓ Dropped WrapDecryptProcedure';
    END
END TRY
BEGIN CATCH
    PRINT 'Could not drop WrapDecryptProcedure: ' + ERROR_MESSAGE();
END CATCH

BEGIN TRY
    IF OBJECT_ID('dbo.WrapDecryptProcedureAdvanced', 'P') IS NOT NULL 
    BEGIN
        DROP PROCEDURE dbo.WrapDecryptProcedureAdvanced;
        PRINT '✓ Dropped WrapDecryptProcedureAdvanced';
    END
END TRY
BEGIN CATCH
    PRINT 'Could not drop WrapDecryptProcedureAdvanced: ' + ERROR_MESSAGE();
END CATCH

-- Drop Scalar Functions
DECLARE @functions TABLE (name NVARCHAR(128));
INSERT INTO @functions VALUES 
    ('EncryptXmlWithPassword'),
    ('EncryptXmlWithPasswordIterations'),
    ('EncryptTableWithMetadata'),
    ('EncryptTableWithMetadataIterations'),
    ('EncryptXmlWithMetadata'),
    ('EncryptXmlWithMetadataIterations'),
    ('GenerateAESKey'),
    ('EncryptAES'),
    ('DecryptAES'),
    ('GenerateDiffieHellmanKeys'),
    ('DeriveSharedKey'),
    ('HashPasswordDefault'),
    ('HashPasswordWithWorkFactor'),
    ('VerifyPassword'),
    ('EncryptAesGcm'),
    ('DecryptAesGcm'),
    ('EncryptAesGcmWithPassword'),
    ('EncryptAesGcmWithPasswordIterations'),
    ('DecryptAesGcmWithPassword'),
    ('DecryptAesGcmWithPasswordIterations'),
    ('GenerateSalt'),
    ('GenerateSaltWithLength'),
    ('EncryptAesGcmWithPasswordAndSalt'),
    ('EncryptAesGcmWithPasswordAndSaltIterations'),
    ('DeriveKeyFromPassword'),
    ('DeriveKeyFromPasswordIterations'),
    ('EncryptAesGcmWithDerivedKey'),
    ('DecryptAesGcmWithDerivedKey');

DECLARE @function_name NVARCHAR(128);
DECLARE function_cursor CURSOR FOR SELECT name FROM @functions;
OPEN function_cursor;
FETCH NEXT FROM function_cursor INTO @function_name;

WHILE @@FETCH_STATUS = 0
BEGIN
    BEGIN TRY
        DECLARE @drop_sql NVARCHAR(MAX) = 'IF OBJECT_ID(''dbo.' + @function_name + ''', ''FN'') IS NOT NULL DROP FUNCTION dbo.' + @function_name;
        EXEC(@drop_sql);
        PRINT '✓ Dropped ' + @function_name;
    END TRY
    BEGIN CATCH
        PRINT 'Could not drop ' + @function_name + ': ' + ERROR_MESSAGE();
    END CATCH
    
    FETCH NEXT FROM function_cursor INTO @function_name;
END

CLOSE function_cursor;
DEALLOCATE function_cursor;

-- Drop Assembly
BEGIN TRY
    IF EXISTS (SELECT * FROM sys.assemblies WHERE name = 'SimpleDotNetCrypting')
    BEGIN
        DROP ASSEMBLY SimpleDotNetCrypting;
        PRINT '✓ Dropped SimpleDotNetCrypting assembly';
    END
END TRY
BEGIN CATCH
    PRINT 'Could not drop assembly: ' + ERROR_MESSAGE();
END CATCH

PRINT 'Cleanup completed.';
PRINT '';

-- =============================================
-- STEP 5: CREATE CLR ASSEMBLY
-- =============================================
PRINT '--- STEP 5: Creating CLR Assembly ---';

CREATE ASSEMBLY SimpleDotNetCrypting
FROM @dll_path
WITH PERMISSION_SET = UNSAFE;

PRINT '✓ Assembly SimpleDotNetCrypting created successfully with UNSAFE permission set';
PRINT '';

-- =============================================
-- STEP 6: CREATE SCALAR FUNCTIONS
-- =============================================
PRINT '--- STEP 6: Creating Scalar Functions ---';

-- AES Key Generation
CREATE FUNCTION dbo.GenerateAESKey()
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].GenerateAESKey;
PRINT '✓ GenerateAESKey';

-- Legacy AES Functions (Deprecated but included for compatibility)
CREATE FUNCTION dbo.EncryptAES(
    @plainText NVARCHAR(MAX), 
    @base64Key NVARCHAR(MAX)
)
RETURNS TABLE (cipherText NVARCHAR(MAX), iv NVARCHAR(MAX))
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].EncryptAES;
PRINT '✓ EncryptAES (Legacy)';

CREATE FUNCTION dbo.DecryptAES(
    @base64CipherText NVARCHAR(MAX), 
    @base64Key NVARCHAR(MAX), 
    @base64IV NVARCHAR(MAX)
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].DecryptAES;
PRINT '✓ DecryptAES (Legacy)';

-- Diffie-Hellman Key Exchange
CREATE FUNCTION dbo.GenerateDiffieHellmanKeys()
RETURNS TABLE (publicKey NVARCHAR(MAX), privateKey NVARCHAR(MAX))
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].GenerateDiffieHellmanKeys;
PRINT '✓ GenerateDiffieHellmanKeys';

CREATE FUNCTION dbo.DeriveSharedKey(
    @otherPartyPublicKeyBase64 NVARCHAR(MAX), 
    @privateKeyBase64 NVARCHAR(MAX)
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].DeriveSharedKey;
PRINT '✓ DeriveSharedKey';

-- BCrypt Password Hashing
CREATE FUNCTION dbo.HashPasswordDefault(@password NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].HashPasswordDefault;
PRINT '✓ HashPasswordDefault';

CREATE FUNCTION dbo.HashPasswordWithWorkFactor(@password NVARCHAR(MAX), @workFactor INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].HashPasswordWithWorkFactor;
PRINT '✓ HashPasswordWithWorkFactor';

CREATE FUNCTION dbo.VerifyPassword(@password NVARCHAR(MAX), @hashedPassword NVARCHAR(MAX))
RETURNS BIT
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].VerifyPassword;
PRINT '✓ VerifyPassword';

-- AES-GCM Encryption (Recommended)
CREATE FUNCTION dbo.EncryptAesGcm(@plainText NVARCHAR(MAX), @base64Key NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].EncryptAesGcm;
PRINT '✓ EncryptAesGcm';

CREATE FUNCTION dbo.DecryptAesGcm(@combinedData NVARCHAR(MAX), @base64Key NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].DecryptAesGcm;
PRINT '✓ DecryptAesGcm';

-- Password-Based AES-GCM Encryption
CREATE FUNCTION dbo.EncryptAesGcmWithPassword(@plainText NVARCHAR(MAX), @password NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].EncryptAesGcmWithPassword;
PRINT '✓ EncryptAesGcmWithPassword';

CREATE FUNCTION dbo.EncryptAesGcmWithPasswordIterations(@plainText NVARCHAR(MAX), @password NVARCHAR(MAX), @iterations INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].EncryptAesGcmWithPasswordIterations;
PRINT '✓ EncryptAesGcmWithPasswordIterations';

CREATE FUNCTION dbo.DecryptAesGcmWithPassword(@base64EncryptedData NVARCHAR(MAX), @password NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].DecryptAesGcmWithPassword;
PRINT '✓ DecryptAesGcmWithPassword';

CREATE FUNCTION dbo.DecryptAesGcmWithPasswordIterations(@base64EncryptedData NVARCHAR(MAX), @password NVARCHAR(MAX), @iterations INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].DecryptAesGcmWithPasswordIterations;
PRINT '✓ DecryptAesGcmWithPasswordIterations';

-- Salt Generation
CREATE FUNCTION dbo.GenerateSalt()
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].GenerateSalt;
PRINT '✓ GenerateSalt';

CREATE FUNCTION dbo.GenerateSaltWithLength(@saltLength INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].GenerateSaltWithLength;
PRINT '✓ GenerateSaltWithLength';

-- Password-Based Encryption with Custom Salt
CREATE FUNCTION dbo.EncryptAesGcmWithPasswordAndSalt(@plainText NVARCHAR(MAX), @password NVARCHAR(MAX), @base64Salt NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].EncryptAesGcmWithPasswordAndSalt;
PRINT '✓ EncryptAesGcmWithPasswordAndSalt';

CREATE FUNCTION dbo.EncryptAesGcmWithPasswordAndSaltIterations(@plainText NVARCHAR(MAX), @password NVARCHAR(MAX), @base64Salt NVARCHAR(MAX), @iterations INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].EncryptAesGcmWithPasswordAndSaltIterations;
PRINT '✓ EncryptAesGcmWithPasswordAndSaltIterations';

-- Key Derivation
CREATE FUNCTION dbo.DeriveKeyFromPassword(@password NVARCHAR(MAX), @base64Salt NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].DeriveKeyFromPassword;
PRINT '✓ DeriveKeyFromPassword';

CREATE FUNCTION dbo.DeriveKeyFromPasswordIterations(@password NVARCHAR(MAX), @base64Salt NVARCHAR(MAX), @iterations INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].DeriveKeyFromPasswordIterations;
PRINT '✓ DeriveKeyFromPasswordIterations';

-- Derived Key Encryption
CREATE FUNCTION dbo.EncryptAesGcmWithDerivedKey(@plainText NVARCHAR(MAX), @base64DerivedKey NVARCHAR(MAX), @base64Salt NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].EncryptAesGcmWithDerivedKey;
PRINT '✓ EncryptAesGcmWithDerivedKey';

CREATE FUNCTION dbo.DecryptAesGcmWithDerivedKey(@base64EncryptedData NVARCHAR(MAX), @base64DerivedKey NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].DecryptAesGcmWithDerivedKey;
PRINT '✓ DecryptAesGcmWithDerivedKey';

-- Legacy XML Encryption (for backward compatibility)
CREATE FUNCTION dbo.EncryptXmlWithPassword(@xmlData XML, @password NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].EncryptXmlWithPassword;
PRINT '✓ EncryptXmlWithPassword (Legacy)';

CREATE FUNCTION dbo.EncryptXmlWithPasswordIterations(@xmlData XML, @password NVARCHAR(MAX), @iterations INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].EncryptXmlWithPasswordIterations;
PRINT '✓ EncryptXmlWithPasswordIterations (Legacy)';

-- Enhanced Table Encryption with Metadata
CREATE FUNCTION dbo.EncryptTableWithMetadata(@tableName NVARCHAR(MAX), @password NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].EncryptTableWithMetadata;
PRINT '✓ EncryptTableWithMetadata';

CREATE FUNCTION dbo.EncryptTableWithMetadataIterations(@tableName NVARCHAR(MAX), @password NVARCHAR(MAX), @iterations INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].EncryptTableWithMetadataIterations;
PRINT '✓ EncryptTableWithMetadataIterations';

-- Enhanced XML Encryption with Metadata
CREATE FUNCTION dbo.EncryptXmlWithMetadata(@xmlData XML, @password NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].EncryptXmlWithMetadata;
PRINT '✓ EncryptXmlWithMetadata';

CREATE FUNCTION dbo.EncryptXmlWithMetadataIterations(@xmlData XML, @password NVARCHAR(MAX), @iterations INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].EncryptXmlWithMetadataIterations;
PRINT '✓ EncryptXmlWithMetadataIterations';

PRINT 'All scalar functions created successfully.';
PRINT '';

-- =============================================
-- STEP 7: CREATE STORED PROCEDURES
-- =============================================
PRINT '--- STEP 7: Creating Stored Procedures ---';

-- Universal Table Restoration Procedure
CREATE PROCEDURE dbo.RestoreEncryptedTable
    @encryptedData NVARCHAR(MAX),
    @password NVARCHAR(MAX)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].RestoreEncryptedTable;
PRINT '✓ RestoreEncryptedTable';

-- Dynamic Temp Table Wrapper
CREATE PROCEDURE dbo.WrapDecryptProcedure
    @procedureName NVARCHAR(MAX),
    @parameters NVARCHAR(MAX) = NULL
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.DynamicTempTableWrapper].WrapDecryptProcedure;
PRINT '✓ WrapDecryptProcedure';

-- Advanced Dynamic Temp Table Wrapper
CREATE PROCEDURE dbo.WrapDecryptProcedureAdvanced
    @procedureName NVARCHAR(MAX),
    @parameters NVARCHAR(MAX) = NULL,
    @tempTableName NVARCHAR(MAX) = NULL
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.DynamicTempTableWrapper].WrapDecryptProcedureAdvanced;
PRINT '✓ WrapDecryptProcedureAdvanced';

PRINT 'All stored procedures created successfully.';
PRINT '';

-- =============================================
-- STEP 8: CREATE TABLE-VALUED FUNCTIONS
-- =============================================
PRINT '--- STEP 8: Creating Table-Valued Functions ---';

-- Note: Table-Valued Functions are created automatically with the scalar functions
-- EncryptAES returns TABLE (cipherText NVARCHAR(MAX), iv NVARCHAR(MAX))
-- GenerateDiffieHellmanKeys returns TABLE (publicKey NVARCHAR(MAX), privateKey NVARCHAR(MAX))

PRINT 'Table-valued functions are created automatically with scalar functions.';
PRINT '';

-- =============================================
-- STEP 9: VERIFICATION
-- =============================================
PRINT '--- STEP 9: Verification ---';

-- Verify assembly
SELECT 
    a.name AS AssemblyName,
    a.permission_set_desc AS PermissionSet,
    a.create_date AS CreateDate
FROM sys.assemblies a
WHERE a.name = 'SimpleDotNetCrypting';

-- Count created objects
SELECT 
    'Functions' AS ObjectType,
    COUNT(*) AS Count
FROM sys.objects o
WHERE o.type = 'FN' AND o.name IN (
    'GenerateAESKey', 'EncryptAES', 'DecryptAES', 'GenerateDiffieHellmanKeys', 'DeriveSharedKey',
    'HashPasswordDefault', 'HashPasswordWithWorkFactor', 'VerifyPassword', 'EncryptAesGcm', 'DecryptAesGcm',
    'EncryptAesGcmWithPassword', 'EncryptAesGcmWithPasswordIterations', 'DecryptAesGcmWithPassword', 'DecryptAesGcmWithPasswordIterations',
    'GenerateSalt', 'GenerateSaltWithLength', 'EncryptAesGcmWithPasswordAndSalt', 'EncryptAesGcmWithPasswordAndSaltIterations',
    'DeriveKeyFromPassword', 'DeriveKeyFromPasswordIterations', 'EncryptAesGcmWithDerivedKey', 'DecryptAesGcmWithDerivedKey',
    'EncryptXmlWithPassword', 'EncryptXmlWithPasswordIterations', 'EncryptTableWithMetadata', 'EncryptTableWithMetadataIterations',
    'EncryptXmlWithMetadata', 'EncryptXmlWithMetadataIterations'
)
UNION ALL
SELECT 
    'Procedures' AS ObjectType,
    COUNT(*) AS Count
FROM sys.objects o
WHERE o.type = 'P' AND o.name IN (
    'RestoreEncryptedTable', 'WrapDecryptProcedure', 'WrapDecryptProcedureAdvanced'
)
UNION ALL
SELECT 
    'Table-Valued Functions' AS ObjectType,
    COUNT(*) AS Count
FROM sys.objects o
WHERE o.type = 'TF' AND o.name IN (
    'EncryptAES', 'GenerateDiffieHellmanKeys'
);

PRINT '';
PRINT '=== DEPLOYMENT COMPLETED SUCCESSFULLY ===';
PRINT '';
PRINT 'FEATURES DEPLOYED:';
PRINT '✓ AES-GCM Encryption/Decryption (Recommended)';
PRINT '✓ Password-based Key Derivation (PBKDF2)';
PRINT '✓ Diffie-Hellman Key Exchange';
PRINT '✓ BCrypt Password Hashing';
PRINT '✓ Table-Level Encryption with Embedded Metadata';
PRINT '✓ XML Encryption with Schema Inference';
PRINT '✓ Dynamic Temp Table Wrapper';
PRINT '✓ Automatic Type Casting';
PRINT '✓ Stored Procedure Result Set Handling';
PRINT '✓ Legacy AES-CBC Support (Deprecated)';
PRINT '';
PRINT 'NEXT STEPS:';
PRINT '1. Run example.sql for basic usage examples';
PRINT '2. Run practical-examples.sql for enhanced demonstrations';
PRINT '3. Test with your specific use cases';
PRINT '';
PRINT 'USAGE EXAMPLES:';
PRINT '-- Generate AES key: SELECT dbo.GenerateAESKey()';
PRINT '-- Encrypt table: SELECT dbo.EncryptTableWithMetadata(''MyTable'', ''password'')';
PRINT '-- Decrypt table: EXEC dbo.RestoreEncryptedTable @encrypted, ''password''';
PRINT '-- Hash password: SELECT dbo.HashPasswordDefault(''mypassword'')';
PRINT '';
PRINT 'For Korean PowerBuilder integration, see practical-examples.sql';
PRINT '============================================='; 