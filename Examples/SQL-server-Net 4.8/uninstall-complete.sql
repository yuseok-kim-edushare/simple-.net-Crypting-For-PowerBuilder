-- =============================================
-- COMPLETE SQL SERVER CLR UNINSTALL SCRIPT
-- =============================================
-- SecureLibrary-SQL: Complete Uninstallation
-- This script removes all CLR objects, functions, procedures, and assembly
-- 
-- WARNING: This will permanently remove all encryption functions and procedures
-- Make sure you have backups of any encrypted data before running this script
-- =============================================

-- =============================================
-- CONFIGURATION - CHANGE THESE VALUES
-- =============================================
DECLARE @target_db NVARCHAR(128) = N'Master';  -- <<<< CHANGE THIS FOR EACH DATABASE

PRINT '=== SECURELIBRARY-SQL COMPLETE UNINSTALL ===';
PRINT 'Target Database: ' + @target_db;
PRINT 'WARNING: This will remove all encryption functions and procedures!';
PRINT '';

-- =============================================
-- STEP 1: SWITCH TO TARGET DATABASE
-- =============================================
PRINT '--- STEP 1: Switching to Target Database ---';

DECLARE @sql NVARCHAR(MAX) = N'USE ' + QUOTENAME(@target_db);
EXEC(@sql);

PRINT 'Now uninstalling from database: ' + @target_db;
PRINT '';

-- =============================================
-- STEP 2: DROP TABLE-VALUED FUNCTIONS FIRST
-- =============================================
PRINT '--- STEP 2: Dropping Table-Valued Functions ---';

BEGIN TRY
    IF OBJECT_ID('dbo.DecryptTableTVF') IS NOT NULL 
    BEGIN
        DROP FUNCTION dbo.DecryptTableTVF;
        PRINT '✓ Dropped DecryptTableTVF';
    END
    ELSE
    BEGIN
        PRINT 'DecryptTableTVF not found';
    END
END TRY
BEGIN CATCH
    PRINT 'Could not drop DecryptTableTVF: ' + ERROR_MESSAGE();
END CATCH

BEGIN TRY
    IF OBJECT_ID('dbo.DecryptTableTypedTVF') IS NOT NULL 
    BEGIN
        DROP FUNCTION dbo.DecryptTableTypedTVF;
        PRINT '✓ Dropped DecryptTableTypedTVF';
    END
    ELSE
    BEGIN
        PRINT 'DecryptTableTypedTVF not found';
    END
END TRY
BEGIN CATCH
    PRINT 'Could not drop DecryptTableTypedTVF: ' + ERROR_MESSAGE();
END CATCH

PRINT '';

-- =============================================
-- STEP 3: DROP STORED PROCEDURES
-- =============================================
PRINT '--- STEP 3: Dropping Stored Procedures ---';

BEGIN TRY
    IF OBJECT_ID('dbo.RestoreEncryptedTable', 'P') IS NOT NULL 
    BEGIN
        DROP PROCEDURE dbo.RestoreEncryptedTable;
        PRINT '✓ Dropped RestoreEncryptedTable';
    END
    ELSE
    BEGIN
        PRINT 'RestoreEncryptedTable not found';
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
    ELSE
    BEGIN
        PRINT 'WrapDecryptProcedure not found';
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
    ELSE
    BEGIN
        PRINT 'WrapDecryptProcedureAdvanced not found';
    END
END TRY
BEGIN CATCH
    PRINT 'Could not drop WrapDecryptProcedureAdvanced: ' + ERROR_MESSAGE();
END CATCH

PRINT '';

-- =============================================
-- STEP 4: DROP SCALAR FUNCTIONS
-- =============================================
PRINT '--- STEP 4: Dropping Scalar Functions ---';

-- Define all function names to drop
DECLARE @functions TABLE (name NVARCHAR(128));
INSERT INTO @functions VALUES 
    -- AES Functions
    ('GenerateAESKey'),
    ('EncryptAES'),
    ('DecryptAES'),
    ('EncryptAesGcm'),
    ('DecryptAesGcm'),
    
    -- Diffie-Hellman Functions
    ('GenerateDiffieHellmanKeys'),
    ('DeriveSharedKey'),
    
    -- BCrypt Functions
    ('HashPasswordDefault'),
    ('HashPasswordWithWorkFactor'),
    ('VerifyPassword'),
    
    -- Password-Based AES-GCM Functions
    ('EncryptAesGcmWithPassword'),
    ('EncryptAesGcmWithPasswordIterations'),
    ('DecryptAesGcmWithPassword'),
    ('DecryptAesGcmWithPasswordIterations'),
    
    -- Salt Functions
    ('GenerateSalt'),
    ('GenerateSaltWithLength'),
    
    -- Password-Based Encryption with Custom Salt
    ('EncryptAesGcmWithPasswordAndSalt'),
    ('EncryptAesGcmWithPasswordAndSaltIterations'),
    
    -- Key Derivation Functions
    ('DeriveKeyFromPassword'),
    ('DeriveKeyFromPasswordIterations'),
    
    -- Derived Key Encryption
    ('EncryptAesGcmWithDerivedKey'),
    ('DecryptAesGcmWithDerivedKey'),
    
    -- Legacy XML Encryption
    ('EncryptXmlWithPassword'),
    ('EncryptXmlWithPasswordIterations'),
    
    -- Enhanced Table Encryption
    ('EncryptTableWithMetadata'),
    ('EncryptTableWithMetadataIterations'),
    
    -- Enhanced XML Encryption
    ('EncryptXmlWithMetadata'),
    ('EncryptXmlWithMetadataIterations');

-- Drop functions using cursor
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

PRINT '';

-- =============================================
-- STEP 5: DROP CLR ASSEMBLY
-- =============================================
PRINT '--- STEP 5: Dropping CLR Assembly ---';

BEGIN TRY
    IF EXISTS (SELECT * FROM sys.assemblies WHERE name = 'SimpleDotNetCrypting')
    BEGIN
        DROP ASSEMBLY SimpleDotNetCrypting;
        PRINT '✓ Dropped SimpleDotNetCrypting assembly';
    END
    ELSE
    BEGIN
        PRINT 'SimpleDotNetCrypting assembly not found';
    END
END TRY
BEGIN CATCH
    PRINT 'Could not drop assembly: ' + ERROR_MESSAGE();
END CATCH

PRINT '';

-- =============================================
-- STEP 6: VERIFICATION
-- =============================================
PRINT '--- STEP 6: Verification ---';

-- Check for remaining objects
SELECT 
    'Remaining Functions' AS ObjectType,
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
    'Remaining Procedures' AS ObjectType,
    COUNT(*) AS Count
FROM sys.objects o
WHERE o.type = 'P' AND o.name IN (
    'RestoreEncryptedTable', 'WrapDecryptProcedure', 'WrapDecryptProcedureAdvanced'
)
UNION ALL
SELECT 
    'Remaining Table-Valued Functions' AS ObjectType,
    COUNT(*) AS Count
FROM sys.objects o
WHERE o.type = 'TF' AND o.name IN (
    'DecryptTableTVF', 'DecryptTableTypedTVF'
)
UNION ALL
SELECT 
    'Remaining Assemblies' AS ObjectType,
    COUNT(*) AS Count
FROM sys.assemblies a
WHERE a.name = 'SimpleDotNetCrypting';

PRINT '';
PRINT '=== UNINSTALL COMPLETED ===';
PRINT '';
PRINT 'IMPORTANT NOTES:';
PRINT '• All encryption functions and procedures have been removed';
PRINT '• Any encrypted data can no longer be decrypted without re-installing the assembly';
PRINT '• Make sure you have backups of encrypted data before uninstalling';
PRINT '';
PRINT 'To reinstall, run install-complete.sql';
PRINT '============================================='; 