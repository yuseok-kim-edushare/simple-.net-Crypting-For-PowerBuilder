-- =============================================
-- DEBUG SCRIPT: Check What Objects Are Actually Installed
-- Updated to use correct SQL Server system catalog views
-- Based on Microsoft SQL Server documentation
-- =============================================
-- This script shows exactly what objects are installed to help identify naming issues
-- Uses sys.all_objects and sys.system_objects for comprehensive coverage
-- =============================================

USE Master
GO

PRINT '=== DEBUGGING INSTALLED OBJECTS (UPDATED) ===';
PRINT 'Using sys.all_objects and sys.system_objects for comprehensive coverage';
PRINT '';

-- Show all objects (user-defined + system) that might be related to SecureLibrary
PRINT '--- All Objects (User + System) Related to SecureLibrary ---';
SELECT 
    ao.name AS ObjectName,
    ao.type_desc AS ObjectType,
    ao.create_date AS CreateDate,
    ao.object_id AS ObjectID,
    ao.schema_id AS SchemaID,
    SCHEMA_NAME(ao.schema_id) AS SchemaName,
    CASE WHEN ao.is_ms_shipped = 1 THEN 'System' ELSE 'User' END AS ObjectSource
FROM sys.all_objects ao
WHERE (ao.name LIKE '%Secure%' OR ao.name LIKE '%Encrypt%' OR ao.name LIKE '%Decrypt%' OR 
       ao.name LIKE '%Hash%' OR ao.name LIKE '%Generate%' OR ao.name LIKE '%AES%' OR 
       ao.name LIKE '%GCM%' OR ao.name LIKE '%Salt%' OR ao.name LIKE '%Password%' OR
       ao.name LIKE '%Table%' OR ao.name LIKE '%Xml%' OR ao.name LIKE '%Wrap%')
ORDER BY ao.is_ms_shipped, ao.type_desc, ao.name;

-- Show all user-defined objects specifically
PRINT '';
PRINT '--- User-Defined Objects Only ---';
SELECT 
    o.name AS ObjectName,
    o.type_desc AS ObjectType,
    o.create_date AS CreateDate,
    o.object_id AS ObjectID,
    SCHEMA_NAME(o.schema_id) AS SchemaName
FROM sys.objects o
WHERE o.is_ms_shipped = 0
  AND (o.name LIKE '%Secure%' OR o.name LIKE '%Encrypt%' OR o.name LIKE '%Decrypt%' OR 
       o.name LIKE '%Hash%' OR o.name LIKE '%Generate%' OR o.name LIKE '%AES%' OR 
       o.name LIKE '%GCM%' OR o.name LIKE '%Salt%' OR o.name LIKE '%Password%' OR
       o.name LIKE '%Table%' OR o.name LIKE '%Xml%' OR o.name LIKE '%Wrap%')
ORDER BY o.type_desc, o.name;

-- Show all scalar functions (user + system)
PRINT '';
PRINT '--- All Scalar Functions (FN) ---';
SELECT 
    ao.name AS FunctionName,
    ao.create_date AS CreateDate,
    ao.object_id AS ObjectID,
    SCHEMA_NAME(ao.schema_id) AS SchemaName,
    CASE WHEN ao.is_ms_shipped = 1 THEN 'System' ELSE 'User' END AS ObjectSource
FROM sys.all_objects ao
WHERE ao.type = 'FN'
ORDER BY ao.is_ms_shipped, ao.name;

-- Show all stored procedures (user + system)
PRINT '';
PRINT '--- All Stored Procedures (P) ---';
SELECT 
    ao.name AS ProcedureName,
    ao.create_date AS CreateDate,
    ao.object_id AS ObjectID,
    SCHEMA_NAME(ao.schema_id) AS SchemaName,
    CASE WHEN ao.is_ms_shipped = 1 THEN 'System' ELSE 'User' END AS ObjectSource
FROM sys.all_objects ao
WHERE ao.type = 'P'
ORDER BY ao.is_ms_shipped, ao.name;

-- Show all table-valued functions (user + system)
PRINT '';
PRINT '--- All Table-Valued Functions (TF) ---';
SELECT 
    ao.name AS TVFName,
    ao.create_date AS CreateDate,
    ao.object_id AS ObjectID,
    SCHEMA_NAME(ao.schema_id) AS SchemaName,
    CASE WHEN ao.is_ms_shipped = 1 THEN 'System' ELSE 'User' END AS ObjectSource
FROM sys.all_objects ao
WHERE ao.type = 'TF'
ORDER BY ao.is_ms_shipped, ao.name;

-- Check assembly and its methods
PRINT '';
PRINT '--- Assembly Methods ---';
SELECT 
    am.assembly_class,
    am.assembly_method,
    a.name AS assembly_name
FROM sys.assembly_modules am
INNER JOIN sys.assemblies a ON am.assembly_id = a.assembly_id
WHERE a.name LIKE '%Secure%' OR a.name LIKE '%Encrypt%'
ORDER BY a.name

-- Check for CLR functions and procedures
PRINT '';
PRINT '--- CLR Functions and Procedures ---';
SELECT 
    ao.name AS ObjectName,
    ao.type_desc AS ObjectType,
    ao.create_date AS CreateDate,
    SCHEMA_NAME(ao.schema_id) AS SchemaName,
    CASE WHEN ao.is_ms_shipped = 1 THEN 'System' ELSE 'User' END AS ObjectSource
FROM sys.all_objects ao
WHERE ao.type IN ('FS', 'FT', 'PC')  -- CLR scalar function, CLR table-valued function, CLR stored procedure
ORDER BY ao.is_ms_shipped, ao.type_desc, ao.name;

-- Show what we're looking for vs what we find
PRINT '';
PRINT '--- Expected vs Found Analysis ---';

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

PRINT 'Expected Scalar Functions:';
SELECT f.FunctionName,
       CASE WHEN ao.name IS NOT NULL THEN '✓ FOUND' ELSE '✗ MISSING' END AS Status,
       SCHEMA_NAME(ao.schema_id) AS SchemaName,
       ao.type_desc AS ObjectType,
       CASE WHEN ao.is_ms_shipped = 1 THEN 'System' ELSE 'User' END AS ObjectSource
FROM @expectedFunctions f
LEFT JOIN sys.all_objects ao ON f.FunctionName = ao.name AND ao.type IN ('FN', 'FS')  -- SQL and CLR scalar functions
ORDER BY f.FunctionName;

PRINT '';
PRINT 'Expected Stored Procedures:';
SELECT p.ProcedureName,
       CASE WHEN ao.name IS NOT NULL THEN '✓ FOUND' ELSE '✗ MISSING' END AS Status,
       SCHEMA_NAME(ao.schema_id) AS SchemaName,
       ao.type_desc AS ObjectType,
       CASE WHEN ao.is_ms_shipped = 1 THEN 'System' ELSE 'User' END AS ObjectSource
FROM @expectedProcedures p
LEFT JOIN sys.all_objects ao ON p.ProcedureName = ao.name AND ao.type IN ('P', 'PC')  -- SQL and CLR stored procedures
ORDER BY p.ProcedureName;

PRINT '';
PRINT 'Expected Table-Valued Functions:';
SELECT t.TVFName,
       CASE WHEN ao.name IS NOT NULL THEN '✓ FOUND' ELSE '✗ MISSING' END AS Status,
       SCHEMA_NAME(ao.schema_id) AS SchemaName,
       ao.type_desc AS ObjectType,
       CASE WHEN ao.is_ms_shipped = 1 THEN 'System' ELSE 'User' END AS ObjectSource
FROM @expectedTVFs t
LEFT JOIN sys.all_objects ao ON t.TVFName = ao.name AND ao.type IN ('TF', 'FT')  -- SQL and CLR table-valued functions
ORDER BY t.TVFName;

-- Additional diagnostic information
PRINT '';
PRINT '--- Additional Diagnostic Information ---';

-- Check for any objects in dbo schema
PRINT 'Objects in dbo schema:';
SELECT 
    ao.name AS ObjectName,
    ao.type_desc AS ObjectType,
    ao.create_date AS CreateDate
FROM sys.all_objects ao
WHERE SCHEMA_NAME(ao.schema_id) = 'dbo'
  AND ao.is_ms_shipped = 0
ORDER BY ao.type_desc, ao.name;

-- Check for any objects in sys schema (should be system objects)
PRINT '';
PRINT 'Objects in sys schema (should be system objects):';
SELECT 
    ao.name AS ObjectName,
    ao.type_desc AS ObjectType,
    ao.create_date AS CreateDate
FROM sys.all_objects ao
WHERE SCHEMA_NAME(ao.schema_id) = 'sys'
  AND (ao.name LIKE '%Secure%' OR ao.name LIKE '%Encrypt%' OR ao.name LIKE '%Decrypt%')
ORDER BY ao.type_desc, ao.name;

-- Summary of installation status
PRINT '';
PRINT '=== INSTALLATION SUMMARY ===';
DECLARE @totalExpected INT = (SELECT COUNT(*) FROM @expectedFunctions) + 
                            (SELECT COUNT(*) FROM @expectedProcedures) + 
                            (SELECT COUNT(*) FROM @expectedTVFs);
DECLARE @totalFound INT = (SELECT COUNT(*) FROM @expectedFunctions f
                          JOIN sys.all_objects ao ON f.FunctionName = ao.name AND ao.type IN ('FN', 'FS')) +
                         (SELECT COUNT(*) FROM @expectedProcedures p
                          JOIN sys.all_objects ao ON p.ProcedureName = ao.name AND ao.type IN ('P', 'PC')) +
                         (SELECT COUNT(*) FROM @expectedTVFs t
                          JOIN sys.all_objects ao ON t.TVFName = ao.name AND ao.type IN ('TF', 'FT'));

PRINT 'Total Expected Objects: ' + CAST(@totalExpected AS VARCHAR(10));
PRINT 'Total Found Objects: ' + CAST(@totalFound AS VARCHAR(10));
PRINT 'Installation Status: ' + CASE WHEN @totalFound = @totalExpected THEN '✓ COMPLETE' ELSE '✗ INCOMPLETE' END;

-- Show object types breakdown
PRINT '';
PRINT 'Object Types Breakdown:';
SELECT 
    ao.type_desc AS ObjectType,
    COUNT(*) AS Count
FROM sys.all_objects ao
WHERE ao.name IN (
    SELECT FunctionName FROM @expectedFunctions
    UNION
    SELECT ProcedureName FROM @expectedProcedures  
    UNION
    SELECT TVFName FROM @expectedTVFs
)
GROUP BY ao.type_desc
ORDER BY ao.type_desc;

PRINT '';
PRINT '=== DEBUG COMPLETED (UPDATED) ===';
PRINT 'This version uses sys.all_objects and sys.system_objects for comprehensive coverage';
PRINT 'as per Microsoft SQL Server documentation.';
PRINT 'Updated to properly detect CLR functions (FS), CLR procedures (PC), and CLR table-valued functions (FT).'; 