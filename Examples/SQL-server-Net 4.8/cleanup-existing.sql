-- =============================================
-- CLEANUP EXISTING INSTALLATION SCRIPT
-- =============================================
-- This script forcefully removes all existing SecureLibrary-SQL objects
-- Use this when you encounter "object already exists" errors during installation
-- =============================================

USE Master
GO

PRINT '=== FORCE CLEANUP OF EXISTING SECURELIBRARY-SQL OBJECTS ===';
PRINT 'This script will forcefully remove all existing objects.';
PRINT 'Use this before running install-complete.sql to ensure clean installation.';
PRINT '';

-- =============================================
-- STEP 1: FORCE DROP ALL STORED PROCEDURES
-- =============================================
PRINT '--- STEP 1: Force Dropping All Stored Procedures ---';

-- Force drop RestoreEncryptedTable
BEGIN TRY
    IF OBJECT_ID('dbo.RestoreEncryptedTable', 'P') IS NOT NULL
    BEGIN
        DROP PROCEDURE dbo.RestoreEncryptedTable;
        PRINT '✓ Force dropped RestoreEncryptedTable';
    END
    ELSE
    BEGIN
        PRINT '  RestoreEncryptedTable not found or already dropped';
    END
END TRY
BEGIN CATCH
    PRINT '  Could not drop RestoreEncryptedTable: ' + ERROR_MESSAGE();
END CATCH

-- Force drop WrapDecryptProcedure
BEGIN TRY
    IF OBJECT_ID('dbo.WrapDecryptProcedure', 'P') IS NOT NULL
    BEGIN
        DROP PROCEDURE dbo.WrapDecryptProcedure;
        PRINT '✓ Force dropped WrapDecryptProcedure';
    END
    ELSE
    BEGIN
        PRINT '  WrapDecryptProcedure not found or already dropped';
    END
END TRY
BEGIN CATCH
    PRINT '  Could not drop WrapDecryptProcedure: ' + ERROR_MESSAGE();
END CATCH

-- Force drop WrapDecryptProcedureAdvanced
BEGIN TRY
    IF OBJECT_ID('dbo.WrapDecryptProcedureAdvanced', 'P') IS NOT NULL
    BEGIN
        DROP PROCEDURE dbo.WrapDecryptProcedureAdvanced;
        PRINT '✓ Force dropped WrapDecryptProcedureAdvanced';
    END
    ELSE
    BEGIN
        PRINT '  WrapDecryptProcedureAdvanced not found or already dropped';
    END
END TRY
BEGIN CATCH
    PRINT '  Could not drop WrapDecryptProcedureAdvanced: ' + ERROR_MESSAGE();
END CATCH

-- Force drop DecryptTableWithMetadata
BEGIN TRY
    IF OBJECT_ID('dbo.DecryptTableWithMetadata', 'P') IS NOT NULL
    BEGIN
        DROP PROCEDURE dbo.DecryptTableWithMetadata;
        PRINT '✓ Force dropped DecryptTableWithMetadata';
    END
    ELSE
    BEGIN
        PRINT '  DecryptTableWithMetadata not found or already dropped';
    END
END TRY
BEGIN CATCH
    PRINT '  Could not drop DecryptTableWithMetadata: ' + ERROR_MESSAGE();
END CATCH

PRINT '';

-- =============================================
-- STEP 2: FORCE DROP ALL FUNCTIONS
-- =============================================
PRINT '--- STEP 2: Force Dropping All Functions ---';

-- List of all function names to drop
DECLARE @functions TABLE (name NVARCHAR(128));
INSERT INTO @functions VALUES 
    ('GenerateAESKey'),
    ('EncryptAES'),
    ('DecryptAES'),
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
    ('DecryptAesGcmWithDerivedKey'),
    ('GenerateDiffieHellmanKeys'),
    ('DeriveSharedKey'),
    ('HashPasswordDefault'),
    ('HashPasswordWithWorkFactor'),
    ('VerifyPassword'),
    ('EncryptXmlWithPassword'),
    ('EncryptXmlWithPasswordIterations'),
    ('EncryptTableWithMetadata'),
    ('EncryptTableWithMetadataIterations'),
    ('EncryptXmlWithMetadata'),
    ('EncryptXmlWithMetadataIterations');

-- Force drop each function
DECLARE @function_name NVARCHAR(128);
DECLARE function_cursor CURSOR FOR SELECT name FROM @functions;
OPEN function_cursor;
FETCH NEXT FROM function_cursor INTO @function_name;

WHILE @@FETCH_STATUS = 0
BEGIN
    BEGIN TRY
        EXEC('DROP FUNCTION dbo.' + @function_name);
        PRINT '✓ Force dropped ' + @function_name;
    END TRY
    BEGIN CATCH
        PRINT '  ' + @function_name + ' not found or already dropped';
    END CATCH
    
    FETCH NEXT FROM function_cursor INTO @function_name;
END

CLOSE function_cursor;
DEALLOCATE function_cursor;

PRINT '';

-- =============================================
-- STEP 3: FORCE DROP ASSEMBLY
-- =============================================
PRINT '--- STEP 3: Force Dropping Assembly ---';

BEGIN TRY
    EXEC('DROP ASSEMBLY [SecureLibrary.SQL]');
    PRINT '✓ Force dropped SecureLibrary.SQL assembly';
END TRY
BEGIN CATCH
    PRINT '  SecureLibrary.SQL assembly not found or already dropped';
END CATCH

PRINT '';

-- =============================================
-- STEP 4: VERIFICATION
-- =============================================
PRINT '--- STEP 4: Verification ---';

-- Check for remaining objects (Updated for CLR objects)
SELECT 
    'Remaining CLR Functions' AS ObjectType,
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
    'Remaining CLR Procedures' AS ObjectType,
    COUNT(*) AS Count
FROM sys.objects o
WHERE o.type = 'PC' AND o.name IN (
    'RestoreEncryptedTable', 'WrapDecryptProcedure', 'WrapDecryptProcedureAdvanced', 'DecryptTableWithMetadata'
)
UNION ALL
SELECT 
    'Remaining CLR Table-Valued Functions' AS ObjectType,
    COUNT(*) AS Count
FROM sys.objects o
WHERE o.type = 'FT' AND o.name IN (
    'EncryptAES', 'GenerateDiffieHellmanKeys'
)
UNION ALL
SELECT 
    'Remaining Assemblies' AS ObjectType,
    COUNT(*) AS Count
FROM sys.assemblies a
WHERE a.name = 'SecureLibrary.SQL';

-- Check for any remaining SecureLibrary objects
PRINT '';
PRINT 'Checking for any remaining SecureLibrary-related objects:';
SELECT 
    o.name AS ObjectName,
    o.type_desc AS ObjectType,
    o.create_date AS CreateDate
FROM sys.objects o
WHERE o.name LIKE '%Secure%' OR o.name LIKE '%Encrypt%' OR o.name LIKE '%Decrypt%' OR o.name LIKE '%Hash%' OR o.name LIKE '%Generate%'
ORDER BY o.type_desc, o.name;

PRINT '';
PRINT '=== CLEANUP COMPLETED ===';
PRINT 'All SecureLibrary-SQL objects have been forcefully removed.';
PRINT '';
PRINT 'NEXT STEPS:';
PRINT '1. Ensure the updated DLL file is in the correct location';
PRINT '2. Run install-complete.sql to perform a fresh installation';
PRINT '3. The updated installation includes fixes for table encryption issues';
PRINT '4. Test with simple-test.sql after installation';
PRINT '';
PRINT 'IMPORTANT: This cleanup ensures a clean slate for the updated assembly.';
PRINT 'The new installation will include enhanced error handling and debugging.';
PRINT '============================================='; 