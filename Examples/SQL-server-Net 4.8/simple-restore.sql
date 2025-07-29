-- =============================================
-- SIMPLE RESTORE ALTERNATIVE
-- =============================================
-- This script provides a simple alternative to DecryptTableWithMetadata
-- by manually decrypting and parsing the XML
-- =============================================

USE Master
GO

PRINT '=== SIMPLE RESTORE ALTERNATIVE ===';
PRINT '';

-- Create a simple test table
CREATE TABLE TestTable (
    ID INT,
    Name NVARCHAR(50),
    Value DECIMAL(10,2)
);

INSERT INTO TestTable VALUES 
(1, 'Test1', 100.50),
(2, 'Test2', 200.75),
(3, '한국어테스트', 300.25);

PRINT 'Created test table with 3 rows';

-- Encrypt the table using the same method as DecryptTableWithMetadata expects
DECLARE @password NVARCHAR(MAX) = 'TestPassword123!';
DECLARE @encrypted NVARCHAR(MAX) = dbo.EncryptTableWithMetadata('TestTable', @password);

PRINT 'Table encrypted successfully. Length: ' + CAST(LEN(@encrypted) AS VARCHAR(20)) + ' characters';

-- Manual decryption and parsing
PRINT '';
PRINT '--- Manual Decryption and Parsing ---';

BEGIN TRY
    -- Decrypt using the same method as DecryptTableWithMetadata
    DECLARE @decryptedXml NVARCHAR(MAX) = dbo.DecryptAesGcmWithPasswordIterations(@encrypted, @password, 2000);
    
    IF @decryptedXml IS NOT NULL
    BEGIN
        PRINT '✓ XML decryption successful. Length: ' + CAST(LEN(@decryptedXml) AS VARCHAR(20)) + ' characters';
        PRINT 'Decrypted XML starts with: ' + LEFT(@decryptedXml, 200) + '...';
        
        -- Parse the XML
        DECLARE @xml XML = @decryptedXml;
        DECLARE @xmlRowCount INT = @xml.value('count(/Root/Row)', 'int');
        PRINT 'XML contains ' + CAST(@xmlRowCount AS VARCHAR(10)) + ' rows';
        
        -- Show decrypted data
        SELECT 
            T.c.value('@ID', 'int') AS ID,
            T.c.value('@Name', 'nvarchar(50)') AS Name,
            T.c.value('@Value', 'decimal(10,2)') AS Value
        FROM @xml.nodes('/Root/Row') AS T(c)
        ORDER BY T.c.value('@ID', 'int');
    END
    ELSE
    BEGIN
        PRINT '✗ XML decryption returned NULL';
    END
END TRY
BEGIN CATCH
    PRINT '✗ Error in manual decryption: ' + ERROR_MESSAGE();
END CATCH

-- Test DecryptTableWithMetadata procedure
PRINT '';
PRINT '--- Testing DecryptTableWithMetadata Procedure ---';

BEGIN TRY
    -- Create temp table to capture results
    IF OBJECT_ID('tempdb..#RestoreTest') IS NOT NULL
        DROP TABLE #RestoreTest;

    CREATE TABLE #RestoreTest (
        ID NVARCHAR(MAX),
        Name NVARCHAR(MAX),
        Value NVARCHAR(MAX)
    );

    -- Try to insert results from DecryptTableWithMetadata
    INSERT INTO #RestoreTest
    EXEC dbo.DecryptTableWithMetadata @encrypted, @password;
    
    DECLARE @rowCount INT;
    SELECT @rowCount = COUNT(*) FROM #RestoreTest;
    PRINT '✓ DecryptTableWithMetadata executed successfully. Rows returned: ' + CAST(@rowCount AS VARCHAR(10));
    
    IF @rowCount > 0
    BEGIN
        PRINT 'Decrypted data from DecryptTableWithMetadata:';
        SELECT 
            CAST(ID AS INT) AS ID,
            Name,
            CAST(Value AS DECIMAL(10,2)) AS Value
        FROM #RestoreTest
        ORDER BY CAST(ID AS INT);
    END
    ELSE
    BEGIN
        PRINT '✗ No rows returned from DecryptTableWithMetadata';
    END
    
    DROP TABLE #RestoreTest;
END TRY
BEGIN CATCH
    PRINT '✗ Error executing DecryptTableWithMetadata: ' + ERROR_MESSAGE();
    PRINT 'Error Number: ' + CAST(ERROR_NUMBER() AS VARCHAR(10));
    PRINT 'Error State: ' + CAST(ERROR_STATE() AS VARCHAR(10));
    PRINT 'Error Severity: ' + CAST(ERROR_SEVERITY() AS VARCHAR(10));
END CATCH

-- Test XML encryption separately
PRINT '';
PRINT '--- Testing XML Encryption Function ---';

BEGIN TRY
    -- Create XML from table
    DECLARE @tableXml XML = (SELECT * FROM TestTable FOR XML PATH('Row'), ROOT('Root'));
    
    -- Encrypt XML using the metadata-enhanced method
    DECLARE @xmlEncrypted NVARCHAR(MAX) = dbo.EncryptXmlWithMetadata(@tableXml, @password);
    
    PRINT 'XML encrypted successfully. Length: ' + CAST(LEN(@xmlEncrypted) AS VARCHAR(20)) + ' characters';
    
    -- Decrypt XML (use correct iterations)
    DECLARE @xmlDecrypted NVARCHAR(MAX) = dbo.DecryptAesGcmWithPasswordIterations(@xmlEncrypted, @password, 2000);
    
    IF @xmlDecrypted IS NOT NULL
    BEGIN
        DECLARE @xmlParsed XML = @xmlDecrypted;
        
        PRINT 'XML decryption successful. Extracted data:';
        SELECT 
            T.c.value('@ID', 'int') AS ID,
            T.c.value('@Name', 'nvarchar(50)') AS Name,
            T.c.value('@Value', 'decimal(10,2)') AS Value
        FROM @xmlParsed.nodes('/Root/Row') AS T(c)
        ORDER BY T.c.value('@ID', 'int');
    END
    ELSE
    BEGIN
        PRINT '✗ XML decryption failed';
    END
END TRY
BEGIN CATCH
    PRINT '✗ Error in XML encryption test: ' + ERROR_MESSAGE();
END CATCH

-- Cleanup
DROP TABLE TestTable;

PRINT '';
PRINT '=== SIMPLE RESTORE COMPLETED ==='; 