-- =============================================
-- FOR XML Usage Examples for SQL Server CLR Encryption
-- Demonstrates how to use the refactored procedures with FOR XML queries
-- =============================================

PRINT '=== FOR XML Usage Examples for SQL Server CLR Encryption ===';
GO

-- =============================================
-- Example 1: Single Row Encryption with FOR XML
-- =============================================
PRINT '--- Example 1: Single Row Encryption with FOR XML ---';
GO

-- Create a test table
IF OBJECT_ID('dbo.tb_test_cust', 'U') IS NOT NULL
    DROP TABLE dbo.tb_test_cust;
GO

CREATE TABLE dbo.tb_test_cust (
    cust_id INT PRIMARY KEY,
    cust_name NVARCHAR(100),
    email NVARCHAR(255),
    phone NVARCHAR(20),
    birth_date DATE,
    salary DECIMAL(10,2),
    is_active BIT,
    created_at DATETIME2 DEFAULT GETDATE()
);
GO

-- Insert test data
INSERT INTO dbo.tb_test_cust (cust_id, cust_name, email, phone, birth_date, salary, is_active)
VALUES 
    (16424, 'John Doe', 'john.doe@example.com', '+1-555-0123', '1985-03-15', 75000.00, 1),
    (16425, 'Jane Smith', 'jane.smith@example.com', '+1-555-0124', '1990-07-22', 82000.00, 1),
    (16426, 'Bob Johnson', NULL, '+1-555-0125', '1978-11-08', 65000.00, 0);
GO

-- Encrypt a single row using FOR XML
DECLARE @rowXml XML;
DECLARE @encryptedRow NVARCHAR(MAX);
DECLARE @password NVARCHAR(MAX) = 'MySecurePassword123!';
DECLARE @iterations INT = 10000;

-- Get row as XML with schema (single row only)
-- Note: Wrapped in root element to handle XMLSCHEMA properly
SET @rowXml = (
    SELECT (
        SELECT TOP 1 * 
        FROM dbo.tb_test_cust 
        WHERE cust_id = 16424 
        FOR XML RAW('Row'), ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE
    ) AS 'RowData'
    FOR XML PATH('root'), TYPE
);

-- Encrypt the row
EXEC dbo.EncryptRowWithMetadata 
    @rowXml = @rowXml,
    @password = @password,
    @iterations = @iterations,
    @encryptedRow = @encryptedRow OUTPUT;

PRINT 'Encrypted row data:';
PRINT @encryptedRow;

-- Decrypt the row
EXEC dbo.DecryptRowWithMetadata 
    @encryptedRow = @encryptedRow,
    @password = @password;

GO

-- =============================================
-- Example 2: Multiple Rows Batch Encryption with FOR XML
-- =============================================
PRINT '--- Example 2: Multiple Rows Batch Encryption with FOR XML ---';
GO

DECLARE @rowsXml XML;
DECLARE @password NVARCHAR(MAX) = 'MySecurePassword123!';
DECLARE @iterations INT = 10000;
DECLARE @batchId NVARCHAR(50) = 'BATCH_' + CAST(GETDATE() AS NVARCHAR(50));

-- Get multiple rows as XML with schema (for batch processing)
-- Note: Wrapped in root element to handle XMLSCHEMA properly
SET @rowsXml = (
    SELECT (
        SELECT * 
        FROM dbo.tb_test_cust 
        WHERE cust_id IN (16424, 16425, 16426)
        FOR XML RAW('Row'), ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE, ROOT('Rows')
    ) AS 'RowsData'
    FOR XML PATH('root'), TYPE
);

-- Encrypt the rows in batch
EXEC dbo.EncryptRowsBatch 
    @rowsXml = @rowsXml,
    @password = @password,
    @iterations = @iterations,
    @batchId = @batchId;

-- Decrypt the batch
EXEC dbo.DecryptRowsBatch 
    @batchId = @batchId,
    @password = @password;

GO

-- =============================================
-- Example 3: Selective Column Encryption
-- =============================================
PRINT '--- Example 3: Selective Column Encryption ---';
GO

DECLARE @sensitiveRowXml XML;
DECLARE @encryptedSensitiveRow NVARCHAR(MAX);
DECLARE @password NVARCHAR(MAX) = 'MySecurePassword123!';
DECLARE @iterations INT = 10000;

-- Get only sensitive columns as XML (single row only)
-- Note: Wrapped in root element to handle XMLSCHEMA properly
SET @sensitiveRowXml = (
    SELECT (
        SELECT TOP 1
            cust_id,
            cust_name,
            email,
            phone,
            salary
        FROM dbo.tb_test_cust 
        WHERE cust_id = 16424 
        FOR XML RAW('SensitiveRow'), ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE
    ) AS 'RowData'
    FOR XML PATH('root'), TYPE
);

-- Encrypt the sensitive data
EXEC dbo.EncryptRowWithMetadata 
    @rowXml = @sensitiveRowXml,
    @password = @password,
    @iterations = @iterations,
    @encryptedRow = @encryptedSensitiveRow OUTPUT;

PRINT 'Encrypted sensitive row data:';
PRINT @encryptedSensitiveRow;

-- Decrypt the sensitive data
EXEC dbo.DecryptRowWithMetadata 
    @encryptedRow = @encryptedSensitiveRow,
    @password = @password;

GO

-- =============================================
-- Example 4: Complex Data Types with FOR XML
-- =============================================
PRINT '--- Example 4: Complex Data Types with FOR XML ---';
GO

-- Create a table with complex data types
IF OBJECT_ID('dbo.tb_complex_data', 'U') IS NOT NULL
    DROP TABLE dbo.tb_complex_data;
GO

CREATE TABLE dbo.tb_complex_data (
    id INT PRIMARY KEY,
    name NVARCHAR(100),
    binary_data VARBINARY(MAX),
    xml_data XML,
    json_data NVARCHAR(MAX),
    decimal_value DECIMAL(18,4),
    datetime_value DATETIME2,
    guid_value UNIQUEIDENTIFIER
);
GO

-- Insert test data with complex types
INSERT INTO dbo.tb_complex_data (
    id, name, binary_data, xml_data, json_data, 
    decimal_value, datetime_value, guid_value
)
VALUES (
    1, 
    'Complex Record',
    0x48656C6C6F20576F726C64, -- "Hello World" in hex
    '<TestData><Value>123</Value><Text>Sample XML</Text></TestData>',
    '{"key1": "value1", "key2": 42, "key3": true}',
    12345.6789,
    '2024-01-15T10:30:00.1234567',
    NEWID()
);
GO

DECLARE @complexRowXml XML;
DECLARE @encryptedComplexRow NVARCHAR(MAX);
DECLARE @password NVARCHAR(MAX) = 'MySecurePassword123!';
DECLARE @iterations INT = 10000;

-- Get complex row as XML (single row only)
SET @complexRowXml = (
    SELECT TOP 1 * 
    FROM dbo.tb_complex_data 
    WHERE id = 1 
    FOR XML RAW('ComplexRow'), ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE
);

-- Encrypt the complex row
EXEC dbo.EncryptRowWithMetadata 
    @rowXml = @complexRowXml,
    @password = @password,
    @iterations = @iterations,
    @encryptedRow = @encryptedComplexRow OUTPUT;

PRINT 'Encrypted complex row data:';
PRINT @encryptedComplexRow;

-- Decrypt the complex row
EXEC dbo.DecryptRowWithMetadata 
    @encryptedRow = @encryptedComplexRow,
    @password = @password;

GO

-- =============================================
-- Example 5: PowerBuilder-Friendly Integration
-- =============================================
PRINT '--- Example 5: PowerBuilder-Friendly Integration ---';
GO

-- Create a wrapper procedure for PowerBuilder integration
IF OBJECT_ID('dbo.EncryptRowForPowerBuilder', 'P') IS NOT NULL
    DROP PROCEDURE dbo.EncryptRowForPowerBuilder;
GO

CREATE PROCEDURE dbo.EncryptRowForPowerBuilder
    @tableName NVARCHAR(128),
    @whereClause NVARCHAR(MAX),
    @password NVARCHAR(MAX),
    @iterations INT = 10000,
    @encryptedRow NVARCHAR(MAX) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @sql NVARCHAR(MAX);
    DECLARE @rowXml XML;
    
    -- Build dynamic SQL to get row as XML with schema (single row only)
    -- Note: Wrapped in root element to handle XMLSCHEMA properly
    SET @sql = N'
        SET @rowXml = (
            SELECT (
                SELECT TOP 1 * 
                FROM ' + QUOTENAME(@tableName) + N' 
                WHERE ' + @whereClause + N'
                FOR XML RAW(''Row''), ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE
            ) AS ''RowData''
            FOR XML PATH(''root''), TYPE
        )';
    
    -- Execute dynamic SQL
    EXEC sp_executesql @sql, N'@rowXml XML OUTPUT', @rowXml OUTPUT;
    
    -- Encrypt the row
    EXEC dbo.EncryptRowWithMetadata 
        @rowXml = @rowXml,
        @password = @password,
        @iterations = @iterations,
        @encryptedRow = @encryptedRow OUTPUT;
END
GO

-- Test the PowerBuilder-friendly procedure
DECLARE @encryptedRowForPB NVARCHAR(MAX);

EXEC dbo.EncryptRowForPowerBuilder
    @tableName = 'tb_test_cust',
    @whereClause = 'cust_id = 16424',
    @password = 'MySecurePassword123!',
    @iterations = 10000,
    @encryptedRow = @encryptedRowForPB OUTPUT;

PRINT 'PowerBuilder-friendly encrypted row:';
PRINT @encryptedRowForPB;

-- Decrypt the row
EXEC dbo.DecryptRowWithMetadata 
    @encryptedRow = @encryptedRowForPB,
    @password = 'MySecurePassword123!';

GO

-- =============================================
-- Example 6: Error Handling and Validation
-- =============================================
PRINT '--- Example 6: Error Handling and Validation ---';
GO

-- Test with invalid XML
BEGIN TRY
    DECLARE @invalidXml XML = '<InvalidData>This is not a proper row</InvalidData>';
    DECLARE @encryptedInvalidRow NVARCHAR(MAX);
    
    EXEC dbo.EncryptRowWithMetadata 
        @rowXml = @invalidXml,
        @password = 'MySecurePassword123!',
        @iterations = 10000,
        @encryptedRow = @encryptedInvalidRow OUTPUT;
        
    PRINT 'Invalid XML test should have failed';
END TRY
BEGIN CATCH
    PRINT 'Expected error caught: ' + ERROR_MESSAGE();
END CATCH
GO

-- Test with NULL password
BEGIN TRY
    DECLARE @rowXml XML = (
        SELECT TOP 1 * 
        FROM dbo.tb_test_cust 
        WHERE cust_id = 16424 
        FOR XML RAW('Row'), ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE
    );
    DECLARE @encryptedRow NVARCHAR(MAX);
    
    EXEC dbo.EncryptRowWithMetadata 
        @rowXml = @rowXml,
        @password = NULL,
        @iterations = 10000,
        @encryptedRow = @encryptedRow OUTPUT;
        
    PRINT 'NULL password test should have failed';
END TRY
BEGIN CATCH
    PRINT 'Expected error caught: ' + ERROR_MESSAGE();
END CATCH
GO

-- =============================================
-- Example 7: Performance Testing
-- =============================================
PRINT '--- Example 7: Performance Testing ---';
GO

-- Create a larger test dataset
IF OBJECT_ID('dbo.tb_performance_test', 'U') IS NOT NULL
    DROP TABLE dbo.tb_performance_test;
GO

CREATE TABLE dbo.tb_performance_test (
    id INT PRIMARY KEY,
    name NVARCHAR(100),
    description NVARCHAR(500),
    value1 DECIMAL(10,2),
    value2 DECIMAL(10,2),
    value3 DECIMAL(10,2),
    created_at DATETIME2 DEFAULT GETDATE()
);
GO

-- Insert 1000 test records
DECLARE @i INT = 1;
WHILE @i <= 1000
BEGIN
    INSERT INTO dbo.tb_performance_test (id, name, description, value1, value2, value3)
    VALUES (
        @i,
        'Record ' + CAST(@i AS NVARCHAR(10)),
        'Description for record ' + CAST(@i AS NVARCHAR(10)),
        RAND() * 10000,
        RAND() * 10000,
        RAND() * 10000
    );
    SET @i = @i + 1;
END
GO

-- Test batch encryption performance
DECLARE @startTime DATETIME2 = GETDATE();
DECLARE @rowsXml XML;
DECLARE @password NVARCHAR(MAX) = 'MySecurePassword123!';
DECLARE @iterations INT = 10000;
DECLARE @batchId NVARCHAR(50) = 'PERF_BATCH_' + CAST(GETDATE() AS NVARCHAR(50));

-- Get first 100 rows as XML
SET @rowsXml = (
    SELECT TOP 100 * 
    FROM dbo.tb_performance_test 
    FOR XML RAW('Row'), ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE, ROOT('Rows')
);

-- Encrypt the batch
EXEC dbo.EncryptRowsBatch 
    @rowsXml = @rowsXml,
    @password = @password,
    @iterations = @iterations,
    @batchId = @batchId;

DECLARE @endTime DATETIME2 = GETDATE();
DECLARE @duration INT = DATEDIFF(MILLISECOND, @startTime, @endTime);

PRINT 'Batch encryption of 100 rows completed in ' + CAST(@duration AS NVARCHAR(10)) + ' milliseconds';

-- Decrypt the batch
SET @startTime = GETDATE();

EXEC dbo.DecryptRowsBatch 
    @batchId = @batchId,
    @password = @password;

SET @endTime = GETDATE();
SET @duration = DATEDIFF(MILLISECOND, @startTime, @endTime);

PRINT 'Batch decryption of 100 rows completed in ' + CAST(@duration AS NVARCHAR(10)) + ' milliseconds';

GO

-- =============================================
-- Example 8: Security Validation
-- =============================================
PRINT '--- Example 8: Security Validation ---';
GO

-- Test that encrypted data cannot be decrypted with wrong password
DECLARE @rowXml XML = (
    SELECT TOP 1 * 
    FROM dbo.tb_test_cust 
    WHERE cust_id = 16424 
    FOR XML RAW('Row'), ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE
);
DECLARE @encryptedRow NVARCHAR(MAX);
DECLARE @correctPassword NVARCHAR(MAX) = 'MySecurePassword123!';
DECLARE @wrongPassword NVARCHAR(MAX) = 'WrongPassword123!';

-- Encrypt with correct password
EXEC dbo.EncryptRowWithMetadata 
    @rowXml = @rowXml,
    @password = @correctPassword,
    @iterations = 10000,
    @encryptedRow = @encryptedRow OUTPUT;

-- Try to decrypt with wrong password
BEGIN TRY
    EXEC dbo.DecryptRowWithMetadata 
        @encryptedRow = @encryptedRow,
        @password = @wrongPassword;
        
    PRINT 'Security test failed - should not decrypt with wrong password';
END TRY
BEGIN CATCH
    PRINT 'Security test passed - correctly rejected wrong password: ' + ERROR_MESSAGE();
END CATCH

-- Decrypt with correct password to verify it works
EXEC dbo.DecryptRowWithMetadata 
    @encryptedRow = @encryptedRow,
    @password = @correctPassword;

GO

PRINT '';
PRINT '=== FOR XML USAGE EXAMPLES COMPLETED ===';
PRINT '';
PRINT 'Key Benefits of FOR XML Integration:';
PRINT '  1. Automatic schema generation with XMLSCHEMA';
PRINT '  2. Proper handling of NULL values with XSINIL';
PRINT '  3. Binary data support with BINARY BASE64';
PRINT '  4. No manual XML conversion required';
PRINT '  5. SQL Server native XML generation';
PRINT '  6. PowerBuilder-friendly integration';
PRINT '  7. Batch processing capabilities';
PRINT '  8. Complex data type support';
PRINT '';
PRINT 'Example FOR XML Query (Single Row):';
PRINT '  SELECT (';
PRINT '      SELECT TOP 1 * FROM dbo.tb_test_cust WHERE cust_id = 16424';
PRINT '      FOR XML RAW(''Row''), ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE';
PRINT '  ) AS ''RowData''';
PRINT '  FOR XML PATH(''root''), TYPE';
GO 