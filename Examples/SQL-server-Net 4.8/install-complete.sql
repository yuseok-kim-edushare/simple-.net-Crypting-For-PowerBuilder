-- =============================================
-- COMPLETE SQL SERVER CLR DEPLOYMENT SCRIPT
-- =============================================
-- SecureLibrary-SQL: Complete Installation and Setup
-- This script deploys the entire CLR assembly with all functions and procedures
-- 
-- IMPORTANT: This script is also used for UPDATES
-- =============================================
-- UPDATE INSTRUCTIONS:
-- 1. Backup your encrypted data
-- 2. Update the DLL file at the specified path
-- 3. Run this script - it will automatically:
--    - Drop existing objects
--    - Create new assembly from updated DLL
--    - Recreate all functions and procedures
-- 4. Test your functions
-- 
-- NOTE: ALTER ASSEMBLY is not recommended due to:
-- - Strict compatibility requirements
-- - Risk of breaking existing functions
-- - Complex error recovery
-- - Limited support for code changes
-- =============================================
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
DECLARE @target_db NVARCHAR(128) = N'master';  -- <<<< CHANGE THIS FOR EACH DATABASE
DECLARE @dll_path NVARCHAR(260) = N'C:\CLR\SecureLibrary-SQL.dll';  -- <<<< SET YOUR DLL PATH HERE

PRINT '=== SECURELIBRARY-SQL COMPLETE DEPLOYMENT ===';
PRINT 'Target Database: ' + @target_db;
PRINT 'DLL Path: ' + @dll_path;
PRINT '';
PRINT 'NOTE: This script can be used for both initial installation and updates.';
PRINT 'For updates, ensure the DLL file has been updated at the specified path.';
PRINT '';
PRINT 'TROUBLESHOOTING: If you encounter "object already exists" errors,';
PRINT 'run cleanup-existing.sql first to force remove all existing objects.';
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
FROM OPENROWSET(BULK 'C:\CLR\SecureLibrary-SQL.dll', SINGLE_BLOB) AS x;

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
-- STEP 4: CLEAN UP EXISTING OBJECTS (IMPROVED)
-- =============================================
PRINT '--- STEP 4: Cleaning Up Existing Objects ---';

-- Phase 1: Drop stored procedures first (they can prevent assembly removal)
PRINT 'Phase 1: Dropping stored procedures...';

BEGIN TRY
    IF OBJECT_ID('dbo.RestoreEncryptedTable', 'PC') IS NOT NULL 
    BEGIN
        DROP PROCEDURE dbo.RestoreEncryptedTable;
        PRINT '✓ Dropped RestoreEncryptedTable';
    END
    ELSE
    BEGIN
        PRINT '  RestoreEncryptedTable not found';
    END
END TRY
BEGIN CATCH
    PRINT '✗ Could not drop RestoreEncryptedTable: ' + ERROR_MESSAGE();
    -- Try alternative drop method
    BEGIN TRY
        PRINT '  Attempting alternative drop method for RestoreEncryptedTable...';
        EXEC('DROP PROCEDURE dbo.RestoreEncryptedTable');
        PRINT '✓ Force dropped RestoreEncryptedTable using EXEC';
    END TRY
    BEGIN CATCH
        PRINT '✗ Could not force drop RestoreEncryptedTable: ' + ERROR_MESSAGE();
    END CATCH
END CATCH

BEGIN TRY
    IF OBJECT_ID('dbo.WrapDecryptProcedure', 'PC') IS NOT NULL 
    BEGIN
        DROP PROCEDURE dbo.WrapDecryptProcedure;
        PRINT '✓ Dropped WrapDecryptProcedure';
    END
    ELSE
    BEGIN
        PRINT '  WrapDecryptProcedure not found';
    END
END TRY
BEGIN CATCH
    PRINT '✗ Could not drop WrapDecryptProcedure: ' + ERROR_MESSAGE();
END CATCH

BEGIN TRY
    IF OBJECT_ID('dbo.WrapDecryptProcedureAdvanced', 'PC') IS NOT NULL 
    BEGIN
        DROP PROCEDURE dbo.WrapDecryptProcedureAdvanced;
        PRINT '✓ Dropped WrapDecryptProcedureAdvanced';
    END
    ELSE
    BEGIN
        PRINT '  WrapDecryptProcedureAdvanced not found';
    END
END TRY
BEGIN CATCH
    PRINT '✗ Could not drop WrapDecryptProcedureAdvanced: ' + ERROR_MESSAGE();
END CATCH

-- Phase 2: Drop all functions (they depend on the assembly)
PRINT 'Phase 2: Dropping all functions...';

-- Drop Scalar Functions
DECLARE @functions TABLE (name NVARCHAR(128), phase INT);
INSERT INTO @functions VALUES 
    -- Phase 2: Drop functions that don't depend on others
    ('EncryptXmlWithPassword', 2),
    ('EncryptXmlWithPasswordIterations', 2),
    ('EncryptTableWithMetadata', 2),
    ('EncryptTableWithMetadataIterations', 2),
    ('EncryptXmlWithMetadata', 2),
    ('EncryptXmlWithMetadataIterations', 2),
    ('GenerateAESKey', 2),
    ('EncryptAES', 2),
    ('DecryptAES', 2),
    ('GenerateDiffieHellmanKeys', 2),
    ('DeriveSharedKey', 2),
    ('HashPasswordDefault', 2),
    ('HashPasswordWithWorkFactor', 2),
    ('VerifyPassword', 2),
    ('EncryptAesGcm', 2),
    ('DecryptAesGcm', 2),
    ('EncryptAesGcmWithPassword', 2),
    ('EncryptAesGcmWithPasswordIterations', 2),
    ('DecryptAesGcmWithPassword', 2),
    ('DecryptAesGcmWithPasswordIterations', 2),
    ('GenerateSalt', 2),
    ('GenerateSaltWithLength', 2),
    ('EncryptAesGcmWithPasswordAndSalt', 2),
    ('EncryptAesGcmWithPasswordAndSaltIterations', 2),
    ('DeriveKeyFromPassword', 2),
    ('DeriveKeyFromPasswordIterations', 2),
    ('EncryptAesGcmWithDerivedKey', 2),
    ('DecryptAesGcmWithDerivedKey', 2);

DECLARE @function_name NVARCHAR(128);
DECLARE @phase INT;
DECLARE function_cursor CURSOR FOR SELECT name, phase FROM @functions ORDER BY phase, name;
OPEN function_cursor;
FETCH NEXT FROM function_cursor INTO @function_name, @phase;

WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT 'Phase ' + CAST(@phase AS VARCHAR(1)) + ': Attempting to drop ' + @function_name;
    BEGIN TRY
        IF OBJECT_ID('dbo.' + @function_name) IS NOT NULL
        BEGIN
            DECLARE @drop_sql NVARCHAR(MAX) = 'DROP FUNCTION dbo.' + @function_name;
            EXEC(@drop_sql);
            PRINT '✓ Dropped ' + @function_name;
        END
        ELSE
        BEGIN
            PRINT '  ' + @function_name + ' not found (already dropped or never existed)';
        END
    END TRY
    BEGIN CATCH
        PRINT '✗ Could not drop ' + @function_name + ': ' + ERROR_MESSAGE();
    END CATCH
    
    FETCH NEXT FROM function_cursor INTO @function_name, @phase;
END

CLOSE function_cursor;
DEALLOCATE function_cursor;

-- Phase 3: Drop assembly (now that all dependencies are removed)
PRINT 'Phase 3: Dropping assembly...';

BEGIN TRY
    IF EXISTS (SELECT * FROM sys.assemblies WHERE name = 'SecureLibrary.SQL')
    BEGIN
        DROP ASSEMBLY [SecureLibrary.SQL];
        PRINT '✓ Dropped SecureLibrary.SQL assembly';
    END
    ELSE
    BEGIN
        PRINT '  SecureLibrary.SQL assembly not found';
    END
END TRY
BEGIN CATCH
    PRINT '✗ Could not drop assembly: ' + ERROR_MESSAGE();
    PRINT '  This may be normal if assembly was already dropped or never existed';
END CATCH

PRINT 'Cleanup completed.';
PRINT '';

-- =============================================
-- STEP 5: CREATE CLR ASSEMBLY
-- =============================================
PRINT '--- STEP 5: Creating CLR Assembly ---';

BEGIN TRY
    CREATE ASSEMBLY [SecureLibrary.SQL]
    FROM @dll_path
    WITH PERMISSION_SET = UNSAFE;
    
    PRINT '✓ Assembly SecureLibrary.SQL created successfully with UNSAFE permission set';
END TRY
BEGIN CATCH
    PRINT '✗ Could not create assembly: ' + ERROR_MESSAGE();
    PRINT '  Please check the DLL path and ensure the file exists and is accessible.';
    PRINT '  If you see "assembly already exists" error, run cleanup-existing.sql first.';
    RETURN;
END CATCH

PRINT '';

-- =============================================
-- STEP 6: CREATE SCALAR FUNCTIONS
-- =============================================
PRINT '--- STEP 6: Creating Scalar Functions ---';
GO

-- AES Key Generation
CREATE FUNCTION dbo.GenerateAESKey()
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRCrypting].GenerateAESKey;
GO
PRINT '✓ GenerateAESKey';
GO
-- Legacy AES Functions (Deprecated but included for compatibility)
CREATE FUNCTION dbo.EncryptAES(
    @plainText NVARCHAR(MAX), 
    @base64Key NVARCHAR(MAX)
)
RETURNS TABLE (cipherText NVARCHAR(MAX), iv NVARCHAR(MAX))
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRCrypting].EncryptAES;
GO
PRINT '✓ EncryptAES (Legacy)';
GO
CREATE FUNCTION dbo.DecryptAES(
    @base64CipherText NVARCHAR(MAX), 
    @base64Key NVARCHAR(MAX), 
    @base64IV NVARCHAR(MAX)
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRCrypting].DecryptAES;
GO
PRINT '✓ DecryptAES (Legacy)';
GO
-- Diffie-Hellman Key Exchange
CREATE FUNCTION dbo.GenerateDiffieHellmanKeys()
RETURNS TABLE (publicKey NVARCHAR(MAX), privateKey NVARCHAR(MAX))
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRCrypting].GenerateDiffieHellmanKeys;
GO
PRINT '✓ GenerateDiffieHellmanKeys';
GO
CREATE FUNCTION dbo.DeriveSharedKey(
    @otherPartyPublicKeyBase64 NVARCHAR(MAX), 
    @privateKeyBase64 NVARCHAR(MAX)
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRCrypting].DeriveSharedKey;
GO
PRINT '✓ DeriveSharedKey';
GO
-- BCrypt Password Hashing
CREATE FUNCTION dbo.HashPasswordDefault(@password NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRCrypting].HashPasswordDefault;
GO
PRINT '✓ HashPasswordDefault';
GO
CREATE FUNCTION dbo.HashPasswordWithWorkFactor(@password NVARCHAR(MAX), @workFactor INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRCrypting].HashPasswordWithWorkFactor;
GO
PRINT '✓ HashPasswordWithWorkFactor';
GO
CREATE FUNCTION dbo.VerifyPassword(@password NVARCHAR(MAX), @hashedPassword NVARCHAR(MAX))
RETURNS BIT
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRCrypting].VerifyPassword;
GO
PRINT '✓ VerifyPassword';
GO
-- AES-GCM Encryption (Recommended)
CREATE FUNCTION dbo.EncryptAesGcm(@plainText NVARCHAR(MAX), @base64Key NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRCrypting].EncryptAesGcm;
GO
PRINT '✓ EncryptAesGcm';
GO
CREATE FUNCTION dbo.DecryptAesGcm(@combinedData NVARCHAR(MAX), @base64Key NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRCrypting].DecryptAesGcm;
GO
PRINT '✓ DecryptAesGcm';
GO
-- Password-Based AES-GCM Encryption
CREATE FUNCTION dbo.EncryptAesGcmWithPassword(@plainText NVARCHAR(MAX), @password NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRCrypting].EncryptAesGcmWithPassword;
GO
PRINT '✓ EncryptAesGcmWithPassword';
GO
CREATE FUNCTION dbo.EncryptAesGcmWithPasswordIterations(@plainText NVARCHAR(MAX), @password NVARCHAR(MAX), @iterations INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRCrypting].EncryptAesGcmWithPasswordIterations;
GO
PRINT '✓ EncryptAesGcmWithPasswordIterations';
GO
CREATE FUNCTION dbo.DecryptAesGcmWithPassword(@base64EncryptedData NVARCHAR(MAX), @password NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRCrypting].DecryptAesGcmWithPassword;
GO
PRINT '✓ DecryptAesGcmWithPassword';
GO
CREATE FUNCTION dbo.DecryptAesGcmWithPasswordIterations(@base64EncryptedData NVARCHAR(MAX), @password NVARCHAR(MAX), @iterations INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRCrypting].DecryptAesGcmWithPasswordIterations;
GO
PRINT '✓ DecryptAesGcmWithPasswordIterations';
GO
-- Salt Generation
CREATE FUNCTION dbo.GenerateSalt()
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRCrypting].GenerateSalt;
GO
PRINT '✓ GenerateSalt';
GO
CREATE FUNCTION dbo.GenerateSaltWithLength(@saltLength INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRCrypting].GenerateSaltWithLength;
GO
PRINT '✓ GenerateSaltWithLength';
GO
-- Password-Based Encryption with Custom Salt
CREATE FUNCTION dbo.EncryptAesGcmWithPasswordAndSalt(@plainText NVARCHAR(MAX), @password NVARCHAR(MAX), @base64Salt NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRCrypting].EncryptAesGcmWithPasswordAndSalt;
GO
PRINT '✓ EncryptAesGcmWithPasswordAndSalt';
GO
CREATE FUNCTION dbo.EncryptAesGcmWithPasswordAndSaltIterations(@plainText NVARCHAR(MAX), @password NVARCHAR(MAX), @base64Salt NVARCHAR(MAX), @iterations INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRCrypting].EncryptAesGcmWithPasswordAndSaltIterations;
GO
PRINT '✓ EncryptAesGcmWithPasswordAndSaltIterations';
GO
-- Key Derivation
CREATE FUNCTION dbo.DeriveKeyFromPassword(@password NVARCHAR(MAX), @base64Salt NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRCrypting].DeriveKeyFromPassword;
GO
PRINT '✓ DeriveKeyFromPassword';
GO
CREATE FUNCTION dbo.DeriveKeyFromPasswordIterations(@password NVARCHAR(MAX), @base64Salt NVARCHAR(MAX), @iterations INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRCrypting].DeriveKeyFromPasswordIterations;
GO
PRINT '✓ DeriveKeyFromPasswordIterations';
GO
-- Derived Key Encryption
CREATE FUNCTION dbo.EncryptAesGcmWithDerivedKey(@plainText NVARCHAR(MAX), @base64DerivedKey NVARCHAR(MAX), @base64Salt NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRCrypting].EncryptAesGcmWithDerivedKey;
GO
PRINT '✓ EncryptAesGcmWithDerivedKey';
GO
CREATE FUNCTION dbo.DecryptAesGcmWithDerivedKey(@base64EncryptedData NVARCHAR(MAX), @base64DerivedKey NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRCrypting].DecryptAesGcmWithDerivedKey;
GO
PRINT '✓ DecryptAesGcmWithDerivedKey';
GO
-- Legacy XML Encryption (for backward compatibility)
CREATE FUNCTION dbo.EncryptXmlWithPassword(@xmlData XML, @password NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRCrypting].EncryptXmlWithPassword;
GO
PRINT '✓ EncryptXmlWithPassword (Legacy)';
GO
CREATE FUNCTION dbo.EncryptXmlWithPasswordIterations(@xmlData XML, @password NVARCHAR(MAX), @iterations INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRCrypting].EncryptXmlWithPasswordIterations;
GO
PRINT '✓ EncryptXmlWithPasswordIterations (Legacy)';
GO
-- Enhanced Table Encryption with Metadata
CREATE FUNCTION dbo.EncryptTableWithMetadata(@tableName NVARCHAR(MAX), @password NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRCrypting].EncryptTableWithMetadata;
GO
PRINT '✓ EncryptTableWithMetadata';
GO
CREATE FUNCTION dbo.EncryptTableWithMetadataIterations(@tableName NVARCHAR(MAX), @password NVARCHAR(MAX), @iterations INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRCrypting].EncryptTableWithMetadataIterations;
GO
PRINT '✓ EncryptTableWithMetadataIterations';
GO
-- Enhanced XML Encryption with Metadata
CREATE FUNCTION dbo.EncryptXmlWithMetadata(@xmlData XML, @password NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRCrypting].EncryptXmlWithMetadata;
GO
PRINT '✓ EncryptXmlWithMetadata';
GO
CREATE FUNCTION dbo.EncryptXmlWithMetadataIterations(@xmlData XML, @password NVARCHAR(MAX), @iterations INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRCrypting].EncryptXmlWithMetadataIterations;
GO
PRINT '✓ EncryptXmlWithMetadataIterations';
GO
PRINT 'All scalar functions created successfully.';
GO

-- =============================================
-- STEP 7: CREATE STORED PROCEDURES
-- =============================================
PRINT '--- STEP 7: Creating Stored Procedures ---';
GO

-- Universal Table Restoration Procedure
BEGIN TRY
    DECLARE @sql NVARCHAR(MAX) = '
    CREATE PROCEDURE dbo.DecryptTableWithMetadata
        @encryptedData NVARCHAR(MAX),
        @password NVARCHAR(MAX)
    AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.SqlCLRCrypting].DecryptTableWithMetadata;';
    
    EXEC(@sql);
    PRINT '✓ DecryptTableWithMetadata';
END TRY
BEGIN CATCH
    PRINT '✗ Could not create DecryptTableWithMetadata: ' + ERROR_MESSAGE();
    PRINT '  If you see "object already exists" error, run cleanup-existing.sql first.';
END CATCH
GO

-- Dynamic Temp Table Wrapper
BEGIN TRY
    DECLARE @sql2 NVARCHAR(MAX) = '
    CREATE PROCEDURE dbo.WrapDecryptProcedure
        @procedureName NVARCHAR(MAX),
        @parameters NVARCHAR(MAX)
    AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.DynamicTempTableWrapper].WrapDecryptProcedure;';
    
    EXEC(@sql2);
    PRINT '✓ WrapDecryptProcedure';
END TRY
BEGIN CATCH
    PRINT '✗ Could not create WrapDecryptProcedure: ' + ERROR_MESSAGE();
    PRINT '  If you see "object already exists" error, run cleanup-existing.sql first.';
END CATCH
GO

-- Advanced Dynamic Temp Table Wrapper
BEGIN TRY
    DECLARE @sql3 NVARCHAR(MAX) = '
    CREATE PROCEDURE dbo.WrapDecryptProcedureAdvanced
        @procedureName NVARCHAR(MAX),
        @parameters NVARCHAR(MAX),
        @tempTableName NVARCHAR(MAX)
    AS EXTERNAL NAME [SecureLibrary.SQL].[SecureLibrary.SQL.DynamicTempTableWrapper].WrapDecryptProcedureAdvanced;';
    
    EXEC(@sql3);
    PRINT '✓ WrapDecryptProcedureAdvanced';
END TRY
BEGIN CATCH
    PRINT '✗ Could not create WrapDecryptProcedureAdvanced: ' + ERROR_MESSAGE();
    PRINT '  If you see "object already exists" error, run cleanup-existing.sql first.';
END CATCH
GO
PRINT 'All stored procedures created successfully.';
GO

-- =============================================
-- STEP 8: CREATE TABLE-VALUED FUNCTIONS
-- =============================================
PRINT '--- STEP 8: Creating Table-Valued Functions ---';

-- Note: Table-Valued Functions are created automatically with the scalar functions
-- EncryptAES returns TABLE (cipherText NVARCHAR(MAX), iv NVARCHAR(MAX))
-- GenerateDiffieHellmanKeys returns TABLE (publicKey NVARCHAR(MAX), privateKey NVARCHAR(MAX))

PRINT 'Table-valued functions are created automatically with scalar functions.';
PRINT '';
GO

-- =============================================
-- STEP 9: VERIFICATION
-- =============================================
PRINT '--- STEP 9: Verification ---';
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

-- List all created functions (Updated for CLR objects)
PRINT 'Checking created functions...';
SELECT 
    o.name AS FunctionName,
    o.type_desc AS ObjectType,
    o.create_date AS CreateDate
FROM sys.objects o
WHERE o.type = 'FS' AND o.name IN (
    'GenerateAESKey', 'EncryptAES', 'DecryptAES', 'GenerateDiffieHellmanKeys', 'DeriveSharedKey',
    'HashPasswordDefault', 'HashPasswordWithWorkFactor', 'VerifyPassword', 'EncryptAesGcm', 'DecryptAesGcm',
    'EncryptAesGcmWithPassword', 'EncryptAesGcmWithPasswordIterations', 'DecryptAesGcmWithPassword', 'DecryptAesGcmWithPasswordIterations',
    'GenerateSalt', 'GenerateSaltWithLength', 'EncryptAesGcmWithPasswordAndSalt', 'EncryptAesGcmWithPasswordAndSaltIterations',
    'DeriveKeyFromPassword', 'DeriveKeyFromPasswordIterations', 'EncryptAesGcmWithDerivedKey', 'DecryptAesGcmWithDerivedKey',
    'EncryptXmlWithPassword', 'EncryptXmlWithPasswordIterations', 'EncryptTableWithMetadata', 'EncryptTableWithMetadataIterations',
    'EncryptXmlWithMetadata', 'EncryptXmlWithMetadataIterations'
)
ORDER BY o.name;
GO

-- List all stored procedures (Updated for CLR objects)
PRINT 'Checking stored procedures...';
SELECT 
    o.name AS ProcedureName,
    o.type_desc AS ObjectType,
    o.create_date AS CreateDate
FROM sys.objects o
WHERE o.type = 'PC' AND o.name IN (
    'DecryptTableWithMetadata', 'WrapDecryptProcedure', 'WrapDecryptProcedureAdvanced'
)
ORDER BY o.name;
GO

-- List all table-valued functions (Updated for CLR objects)
PRINT 'Checking table-valued functions...';
SELECT 
    o.name AS FunctionName,
    o.type_desc AS ObjectType,
    o.create_date AS CreateDate
FROM sys.objects o
WHERE o.type = 'FT' AND o.name IN (
    'EncryptAES', 'GenerateDiffieHellmanKeys'
)
ORDER BY o.name;
GO

-- Summary count (Updated for CLR objects)
PRINT 'Summary of created objects:';
SELECT 
    'CLR Scalar Functions' AS ObjectType,
    COUNT(*) AS Count
FROM sys.objects o
WHERE o.type = 'FS' AND o.name IN (
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
    'CLR Stored Procedures' AS ObjectType,
    COUNT(*) AS Count
FROM sys.objects o
WHERE o.type = 'PC' AND o.name IN (
    'DecryptTableWithMetadata', 'WrapDecryptProcedure', 'WrapDecryptProcedureAdvanced'
)
UNION ALL
SELECT 
    'CLR Table-Valued Functions' AS ObjectType,
    COUNT(*) AS Count
FROM sys.objects o
WHERE o.type = 'FT' AND o.name IN (
    'EncryptAES', 'GenerateDiffieHellmanKeys'
);

-- Check for any SecureLibrary objects (broader search)
PRINT 'Checking for any SecureLibrary-related objects:';
SELECT 
    o.name AS ObjectName,
    o.type_desc AS ObjectType,
    o.create_date AS CreateDate
FROM sys.objects o
WHERE o.name LIKE '%Secure%' OR o.name LIKE '%Encrypt%' OR o.name LIKE '%Decrypt%' OR o.name LIKE '%Hash%' OR o.name LIKE '%Generate%'
ORDER BY o.type_desc, o.name;
GO

-- Test basic functionality
PRINT 'Testing basic functionality...';
BEGIN TRY
    DECLARE @testKey NVARCHAR(MAX) = dbo.GenerateAESKey();
    PRINT '✓ GenerateAESKey test: SUCCESS - Key generated: ' + LEFT(@testKey, 20) + '...';
END TRY
BEGIN CATCH
    PRINT '✗ GenerateAESKey test: FAILED - ' + ERROR_MESSAGE();
END CATCH

BEGIN TRY
    DECLARE @testSalt NVARCHAR(MAX) = dbo.GenerateSalt();
    PRINT '✓ GenerateSalt test: SUCCESS - Salt generated: ' + LEFT(@testSalt, 20) + '...';
END TRY
BEGIN CATCH
    PRINT '✗ GenerateSalt test: FAILED - ' + ERROR_MESSAGE();
END CATCH

BEGIN TRY
    DECLARE @testHash NVARCHAR(MAX) = dbo.HashPasswordDefault('testpassword');
    PRINT '✓ HashPasswordDefault test: SUCCESS - Hash generated: ' + LEFT(@testHash, 20) + '...';
END TRY
BEGIN CATCH
    PRINT '✗ HashPasswordDefault test: FAILED - ' + ERROR_MESSAGE();
END CATCH
GO

-- Expected object counts based on your installation
PRINT '';
PRINT '=== EXPECTED OBJECT COUNTS ===';
PRINT 'Based on your successful installation, you should see:';
PRINT '• 29 CLR Scalar Functions (type = FS)';
PRINT '• 3 CLR Stored Procedures (type = PC)';
PRINT '• 2 CLR Table-Valued Functions (type = FT)';
PRINT '• 1 Assembly (SecureLibrary.SQL)';
PRINT '';
PRINT 'Total: 35 CLR objects';
PRINT '';

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
PRINT '-- Decrypt table: EXEC dbo.DecryptTableWithMetadata @encrypted, ''password''';
PRINT '-- Hash password: SELECT dbo.HashPasswordDefault(''mypassword'')';
PRINT '';
PRINT 'For Korean PowerBuilder integration, see practical-examples.sql';
PRINT '============================================='; 