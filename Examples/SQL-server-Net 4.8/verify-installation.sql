-- =============================================
-- VERIFICATION SCRIPT FOR SECURELIBRARY INSTALLATION
-- =============================================
-- This script verifies that all functions and procedures are correctly installed
-- =============================================

USE Master
GO

PRINT '=== VERIFYING SECURELIBRARY INSTALLATION ===';
PRINT '';

-- Check assembly installation first
PRINT '--- Assembly Verification ---';
DECLARE @assemblyCount INT = 0;
SELECT @assemblyCount = COUNT(*) 
FROM sys.assemblies a 
WHERE a.name = 'SecureLibrary.SQL';

IF @assemblyCount = 0
BEGIN
    PRINT '✗ CRITICAL: SecureLibrary.SQL assembly is NOT installed!';
    PRINT '  Please run install-complete.sql first to install the assembly.';
    PRINT '';
    PRINT 'Available assemblies:';
    SELECT 
        a.name AS AssemblyName,
        a.permission_set_desc AS PermissionSet,
        a.create_date AS CreateDate
    FROM sys.assemblies a
    WHERE a.is_user_defined = 1
    ORDER BY a.name;
END
ELSE
BEGIN
    PRINT '✓ SecureLibrary.SQL assembly is installed.';
    SELECT 
        a.name AS AssemblyName,
        a.permission_set_desc AS PermissionSet,
        a.create_date AS CreateDate
    FROM sys.assemblies a
    WHERE a.name = 'SecureLibrary.SQL';
END

-- Check what objects are actually installed (broader search)
PRINT '';
PRINT '--- Currently Installed Objects ---';
SELECT 
    o.name AS ObjectName,
    o.type_desc AS ObjectType,
    o.create_date AS CreateDate,
    CASE WHEN o.is_ms_shipped = 1 THEN 'System' ELSE 'User' END AS ObjectSource
FROM sys.objects o
WHERE (o.name LIKE '%Secure%' OR o.name LIKE '%Encrypt%' OR o.name LIKE '%Decrypt%' OR 
       o.name LIKE '%Hash%' OR o.name LIKE '%Generate%' OR o.name LIKE '%AES%' OR 
       o.name LIKE '%GCM%' OR o.name LIKE '%Salt%' OR o.name LIKE '%Password%')
  AND o.is_ms_shipped = 0
ORDER BY o.type_desc, o.name;

-- Check specific expected objects
PRINT '';
PRINT '--- Expected Objects Check ---';

-- Expected scalar functions
DECLARE @expectedFunctions TABLE (FunctionName NVARCHAR(128));
INSERT INTO @expectedFunctions VALUES 
('GenerateAESKey'), ('EncryptAES'), ('DecryptAES'), ('GenerateDiffieHellmanKeys'), ('DeriveSharedKey'),
('HashPasswordDefault'), ('HashPasswordWithWorkFactor'), ('VerifyPassword'), ('EncryptAesGcm'), ('DecryptAesGcm'),
('EncryptAesGcmWithPassword'), ('EncryptAesGcmWithPasswordIterations'), ('DecryptAesGcmWithPassword'), ('DecryptAesGcmWithPasswordIterations'),
('GenerateSalt'), ('GenerateSaltWithLength'), ('EncryptAesGcmWithPasswordAndSalt'), ('EncryptAesGcmWithPasswordAndSaltIterations'),
('DeriveKeyFromPassword'), ('DeriveKeyFromPasswordIterations'), ('EncryptAesGcmWithDerivedKey'), ('DecryptAesGcmWithDerivedKey'),
('EncryptXmlWithPassword'), ('EncryptXmlWithPasswordIterations'), ('EncryptTableWithMetadata'), ('EncryptTableWithMetadataIterations'),
('EncryptXmlWithMetadata'), ('EncryptXmlWithMetadataIterations');

-- Expected stored procedures
DECLARE @expectedProcedures TABLE (ProcedureName NVARCHAR(128));
INSERT INTO @expectedProcedures VALUES 
('DecryptTableWithMetadata'), ('WrapDecryptProcedure'), ('WrapDecryptProcedureAdvanced');

-- Expected table-valued functions
DECLARE @expectedTVFs TABLE (TVFName NVARCHAR(128));
INSERT INTO @expectedTVFs VALUES 
('EncryptAES'), ('GenerateDiffieHellmanKeys');

-- Check which expected objects are missing
PRINT 'Missing Scalar Functions:';
SELECT f.FunctionName
FROM @expectedFunctions f
LEFT JOIN sys.objects o ON f.FunctionName = o.name AND o.type = 'FN'
WHERE o.name IS NULL
ORDER BY f.FunctionName;

PRINT '';
PRINT 'Missing Stored Procedures:';
SELECT p.ProcedureName
FROM @expectedProcedures p
LEFT JOIN sys.objects o ON p.ProcedureName = o.name AND o.type = 'P'
WHERE o.name IS NULL
ORDER BY p.ProcedureName;

PRINT '';
PRINT 'Missing Table-Valued Functions:';
SELECT t.TVFName
FROM @expectedTVFs t
LEFT JOIN sys.objects o ON t.TVFName = o.name AND o.type = 'TF'
WHERE o.name IS NULL
ORDER BY t.TVFName;

-- Summary count with better error handling
PRINT '';
PRINT '--- Installation Summary ---';
DECLARE @scalarCount INT, @procCount INT, @tvfCount INT;

SELECT @scalarCount = COUNT(*)
FROM sys.objects o
WHERE o.type = 'FN' AND o.name IN (
    'GenerateAESKey', 'EncryptAES', 'DecryptAES', 'GenerateDiffieHellmanKeys', 'DeriveSharedKey',
    'HashPasswordDefault', 'HashPasswordWithWorkFactor', 'VerifyPassword', 'EncryptAesGcm', 'DecryptAesGcm',
    'EncryptAesGcmWithPassword', 'EncryptAesGcmWithPasswordIterations', 'DecryptAesGcmWithPassword', 'DecryptAesGcmWithPasswordIterations',
    'GenerateSalt', 'GenerateSaltWithLength', 'EncryptAesGcmWithPasswordAndSalt', 'EncryptAesGcmWithPasswordAndSaltIterations',
    'DeriveKeyFromPassword', 'DeriveKeyFromPasswordIterations', 'EncryptAesGcmWithDerivedKey', 'DecryptAesGcmWithDerivedKey',
    'EncryptXmlWithPassword', 'EncryptXmlWithPasswordIterations', 'EncryptTableWithMetadata', 'EncryptTableWithMetadataIterations',
    'EncryptXmlWithMetadata', 'EncryptXmlWithMetadataIterations'
);

SELECT @procCount = COUNT(*)
FROM sys.objects o
WHERE o.type = 'P' AND o.name IN (
    'DecryptTableWithMetadata', 'WrapDecryptProcedure', 'WrapDecryptProcedureAdvanced'
);

SELECT @tvfCount = COUNT(*)
FROM sys.objects o
WHERE o.type = 'TF' AND o.name IN (
    'EncryptAES', 'GenerateDiffieHellmanKeys'
);

SELECT 
    'Scalar Functions' AS ObjectType,
    @scalarCount AS Count,
    CASE WHEN @scalarCount = 28 THEN '✓ Complete' 
         WHEN @scalarCount = 0 THEN '✗ Not Installed' 
         ELSE '⚠ Partial' END AS Status
UNION ALL
SELECT 
    'Stored Procedures' AS ObjectType,
    @procCount AS Count,
    CASE WHEN @procCount = 3 THEN '✓ Complete' 
         WHEN @procCount = 0 THEN '✗ Not Installed' 
         ELSE '⚠ Partial' END AS Status
UNION ALL
SELECT 
    'Table-Valued Functions' AS ObjectType,
    @tvfCount AS Count,
    CASE WHEN @tvfCount = 2 THEN '✓ Complete' 
         WHEN @tvfCount = 0 THEN '✗ Not Installed' 
         ELSE '⚠ Partial' END AS Status;

-- Provide installation guidance
PRINT '';
PRINT '--- Installation Status ---';
IF @assemblyCount = 0
BEGIN
    PRINT '✗ INSTALLATION REQUIRED:';
    PRINT '  1. Run: install-complete.sql';
    PRINT '  2. Then run this verification script again';
END
ELSE IF @scalarCount = 0 AND @procCount = 0 AND @tvfCount = 0
BEGIN
    PRINT '✗ FUNCTIONS NOT INSTALLED:';
    PRINT '  Assembly is installed but functions are missing.';
    PRINT '  This may indicate an installation error.';
    PRINT '  Try running: install-complete.sql again';
END
ELSE IF @scalarCount = 28 AND @procCount = 3 AND @tvfCount = 2
BEGIN
    PRINT '✓ INSTALLATION COMPLETE: All 33 objects installed successfully!';
END
ELSE
BEGIN
    PRINT '⚠ PARTIAL INSTALLATION: Some objects are missing.';
    PRINT '  Expected: 28 functions, 3 procedures, 2 TVFs';
    PRINT '  Found: ' + CAST(@scalarCount AS VARCHAR(3)) + ' functions, ' + 
          CAST(@procCount AS VARCHAR(3)) + ' procedures, ' + 
          CAST(@tvfCount AS VARCHAR(3)) + ' TVFs';
END

-- Test basic functionality only if assembly is installed
IF @assemblyCount > 0
BEGIN
    PRINT '';
    PRINT '--- Functionality Tests ---';

    -- Test 1: Generate AES Key
    BEGIN TRY
        DECLARE @testKey NVARCHAR(MAX) = dbo.GenerateAESKey();
        PRINT '✓ GenerateAESKey test: SUCCESS - Key generated: ' + LEFT(@testKey, 20) + '...';
    END TRY
    BEGIN CATCH
        PRINT '✗ GenerateAESKey test: FAILED - ' + ERROR_MESSAGE();
    END CATCH

    -- Test 2: Generate Salt
    BEGIN TRY
        DECLARE @testSalt NVARCHAR(MAX) = dbo.GenerateSalt();
        PRINT '✓ GenerateSalt test: SUCCESS - Salt generated: ' + LEFT(@testSalt, 20) + '...';
    END TRY
    BEGIN CATCH
        PRINT '✗ GenerateSalt test: FAILED - ' + ERROR_MESSAGE();
    END CATCH

    -- Test 3: Hash Password
    BEGIN TRY
        DECLARE @testHash NVARCHAR(MAX) = dbo.HashPasswordDefault('testpassword');
        PRINT '✓ HashPasswordDefault test: SUCCESS - Hash generated: ' + LEFT(@testHash, 20) + '...';
    END TRY
    BEGIN CATCH
        PRINT '✗ HashPasswordDefault test: FAILED - ' + ERROR_MESSAGE();
    END CATCH
END

PRINT '';
PRINT '=== VERIFICATION COMPLETED ===';
PRINT '';
PRINT 'EXPECTED COUNTS:';
PRINT '• Scalar Functions: 28';
PRINT '• Stored Procedures: 3';
PRINT '• Table-Valued Functions: 2';
PRINT '• Total Objects: 33';
PRINT '';
PRINT 'If counts are 0, run: install-complete.sql';
PRINT 'If some counts are missing, check the "Missing" sections above.'; 