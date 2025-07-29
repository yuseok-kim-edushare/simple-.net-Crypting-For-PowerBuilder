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
    'Remaining Assemblies' AS ObjectType,
    COUNT(*) AS Count
FROM sys.assemblies a
WHERE a.name = 'SecureLibrary.SQL';

PRINT '';
PRINT '=== CLEANUP COMPLETED ===';
PRINT 'All SecureLibrary-SQL objects have been forcefully removed.';
PRINT 'You can now run install-complete.sql to perform a fresh installation.';
PRINT '============================================='; 