-- =============================================
-- DEBUG RESTOREENCRYPTEDTABLE PROCEDURE
-- =============================================
-- This script helps debug the RestoreEncryptedTable procedure
-- =============================================

USE Master
GO

PRINT '=== DEBUGGING RESTOREENCRYPTEDTABLE PROCEDURE ===';
PRINT '';

-- Test 1: Simple table creation and encryption
PRINT '--- Test 1: Simple Table Encryption/Decryption ---';

-- Create a simple test table
CREATE TABLE TestTable (
    ID INT,
    Name NVARCHAR(50),
    Value DECIMAL(10,2)
);

INSERT INTO TestTable VALUES 
(1, 'Test1', 100.50),
(2, 'Test2', 200.75);

PRINT 'Created test table with 2 rows';

-- Encrypt the table using the metadata-enhanced method
DECLARE @password NVARCHAR(MAX) = 'TestPassword123!';
DECLARE @encrypted NVARCHAR(MAX) = dbo.EncryptTableWithMetadata('TestTable', @password);

PRINT 'Table encrypted successfully. Length: ' + CAST(LEN(@encrypted) AS VARCHAR(20)) + ' characters';
PRINT 'Encrypted data starts with: ' + LEFT(@encrypted, 50) + '...';

-- Try to decrypt using RestoreEncryptedTable
PRINT '';
PRINT 'Attempting to decrypt using RestoreEncryptedTable...';

-- Create temp table to capture results
IF OBJECT_ID('tempdb..#DebugRestore') IS NOT NULL
    DROP TABLE #DebugRestore;

CREATE TABLE #DebugRestore (
    ID NVARCHAR(MAX),
    Name NVARCHAR(MAX),
    Value NVARCHAR(MAX)
);

-- Try to insert results from RestoreEncryptedTable
BEGIN TRY
    INSERT INTO #DebugRestore
    EXEC dbo.RestoreEncryptedTable @encrypted, @password;
    
    DECLARE @rowCount INT;
    SELECT @rowCount = COUNT(*) FROM #DebugRestore;
    PRINT '✓ RestoreEncryptedTable executed successfully. Rows returned: ' + CAST(@rowCount AS VARCHAR(10));
    
    IF @rowCount > 0
    BEGIN
        PRINT 'Decrypted data:';
        SELECT 
            CAST(ID AS INT) AS ID,
            Name,
            CAST(Value AS DECIMAL(10,2)) AS Value
        FROM #DebugRestore
        ORDER BY CAST(ID AS INT);
    END
    ELSE
    BEGIN
        PRINT '✗ No rows returned from RestoreEncryptedTable';
    END
END TRY
BEGIN CATCH
    PRINT '✗ Error executing RestoreEncryptedTable: ' + ERROR_MESSAGE();
    PRINT 'Error Number: ' + CAST(ERROR_NUMBER() AS VARCHAR(10));
    PRINT 'Error State: ' + CAST(ERROR_STATE() AS VARCHAR(10));
    PRINT 'Error Severity: ' + CAST(ERROR_SEVERITY() AS VARCHAR(10));
END CATCH

-- Test 2: Try direct XML decryption
PRINT '';
PRINT '--- Test 2: Direct XML Decryption ---';

BEGIN TRY
    -- Try to decrypt the XML directly (use correct iterations)
    DECLARE @decryptedXml NVARCHAR(MAX) = dbo.DecryptAesGcmWithPasswordIterations(@encrypted, @password, 2000);
    
    IF @decryptedXml IS NOT NULL
    BEGIN
        PRINT '✓ XML decryption successful. Length: ' + CAST(LEN(@decryptedXml) AS VARCHAR(20)) + ' characters';
        PRINT 'Decrypted XML starts with: ' + LEFT(@decryptedXml, 200) + '...';
        
        -- Try to parse the XML
        DECLARE @xml XML = @decryptedXml;
        DECLARE @xmlRowCount INT = @xml.value('count(/Root/Row)', 'int');
        PRINT 'XML contains ' + CAST(@xmlRowCount AS VARCHAR(10)) + ' rows';
        
        -- Show first row
        SELECT TOP 1 
            T.c.value('@ID', 'int') AS ID,
            T.c.value('@Name', 'nvarchar(50)') AS Name,
            T.c.value('@Value', 'decimal(10,2)') AS Value
        FROM @xml.nodes('/Root/Row') AS T(c);
        
        -- Show XML structure for debugging
        PRINT '';
        PRINT 'XML structure (first 500 characters):';
        PRINT LEFT(@decryptedXml, 500);
    END
    ELSE
    BEGIN
        PRINT '✗ XML decryption returned NULL';
    END
END TRY
BEGIN CATCH
    PRINT '✗ Error in direct XML decryption: ' + ERROR_MESSAGE();
END CATCH

-- Test 3: Check if RestoreEncryptedTable exists and is accessible
PRINT '';
PRINT '--- Test 3: Procedure Verification ---';

SELECT 
    o.name AS ProcedureName,
    o.type_desc AS ObjectType,
    o.create_date AS CreateDate,
    CASE WHEN o.is_ms_shipped = 1 THEN 'System' ELSE 'User' END AS ObjectType
FROM sys.objects o
WHERE o.name = 'RestoreEncryptedTable' AND o.type = 'P';

-- Test 4: Check assembly
PRINT '';
PRINT '--- Test 4: Assembly Verification ---';

SELECT 
    a.name AS AssemblyName,
    a.permission_set_desc AS PermissionSet,
    a.create_date AS CreateDate
FROM sys.assemblies a
WHERE a.name = 'SecureLibrary.SQL';

-- Test 5: Check all encryption functions
PRINT '';
PRINT '--- Test 5: Encryption Functions Verification ---';

SELECT 
    o.name AS FunctionName,
    o.type_desc AS ObjectType,
    o.create_date AS CreateDate
FROM sys.objects o
WHERE o.name IN (
    'EncryptTableWithMetadata',
    'EncryptXmlWithMetadata',
    'DecryptAesGcmWithPassword',
    'DecryptAesGcmWithPasswordIterations'
) AND o.type = 'FN'
ORDER BY o.name;

-- Cleanup
DROP TABLE TestTable;
IF OBJECT_ID('tempdb..#DebugRestore') IS NOT NULL
    DROP TABLE #DebugRestore;

PRINT '';
PRINT '=== DEBUG COMPLETED ==='; 