-- =============================================
-- Enhanced SQL Server CLR Usage Examples with FOR XML
-- Demonstrates the improved row encryption procedures using FOR XML
-- =============================================

PRINT '=== Enhanced SQL Server CLR Usage Examples with FOR XML ===';
GO

-- =============================================
-- STEP 1: Create Sample Tables
-- =============================================
PRINT '--- STEP 1: Creating Sample Tables ---';
GO

-- Create a sample users table
CREATE TABLE Users (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    Password NVARCHAR(MAX) NOT NULL,
    SSN NVARCHAR(11),
    Salary DECIMAL(10,2),
    HireDate DATE,
    IsActive BIT DEFAULT 1
);
GO

-- Insert sample data
INSERT INTO Users (Username, Email, Password, SSN, Salary, HireDate, IsActive)
VALUES 
    ('john.doe', 'john.doe@company.com', 'temp123', '123-45-6789', 75000.00, '2023-01-15', 1),
    ('jane.smith', 'jane.smith@company.com', 'temp456', '987-65-4321', 82000.00, '2023-03-20', 1),
    ('bob.wilson', 'bob.wilson@company.com', 'temp789', '456-78-9012', 65000.00, '2023-06-10', 0);
GO

PRINT '✓ Sample data inserted';

-- =============================================
-- STEP 2: Enhanced Row Encryption Examples with FOR XML
-- =============================================
PRINT '--- STEP 2: Enhanced Row Encryption Examples with FOR XML ---';
GO

-- Example 1: Encrypt a single row using FOR XML
-- This leverages SQL Server's native XML capabilities for automatic schema generation
PRINT 'Example 1: Encrypting a single row using FOR XML';

DECLARE @rowXml XML;
DECLARE @encryptedRow NVARCHAR(MAX);
DECLARE @password NVARCHAR(MAX) = 'MySecurePassword123!';

-- Get row as XML with schema and metadata (single row only)
SET @rowXml = (
    SELECT (
        SELECT TOP 1 * 
        FROM Users 
        WHERE UserID = 1 
        FOR XML RAW('Row'), ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE
    ) AS 'RowData'
    FOR XML PATH('root'), TYPE
);

-- Encrypt the row
EXEC dbo.EncryptRowWithMetadata 
    @rowXml = @rowXml,
    @password = @password,
    @iterations = 10000,
    @encryptedRow = @encryptedRow OUTPUT;

PRINT 'Encrypted row data length: ' + CAST(LEN(@encryptedRow) AS NVARCHAR(10));

-- Decrypt the row and return as result set
PRINT 'Decrypting row...';
EXEC dbo.DecryptRowWithMetadata @encryptedRow, @password;
GO

-- Example 2: Batch encryption of multiple rows using FOR XML
PRINT 'Example 2: Batch encryption of multiple rows using FOR XML';

DECLARE @rowsXml XML;
DECLARE @batchId NVARCHAR(50) = 'BATCH_' + CAST(GETDATE() AS NVARCHAR(50));
DECLARE @password NVARCHAR(MAX) = 'MySecurePassword123!';

-- Get multiple rows as XML with schema
SET @rowsXml = (
    SELECT (
        SELECT * 
        FROM Users 
        WHERE IsActive = 1
        FOR XML RAW('Row'), ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE, ROOT('Rows')
    ) AS 'RowsData'
    FOR XML PATH('root'), TYPE
);

-- Encrypt the rows in batch
EXEC dbo.EncryptRowsBatch 
    @rowsXml = @rowsXml,
    @password = @password,
    @iterations = 10000,
    @batchId = @batchId;

PRINT 'Batch encryption completed with ID: ' + @batchId;

-- Decrypt the batch
PRINT 'Decrypting batch...';
EXEC dbo.DecryptRowsBatch @batchId, @password;
GO

-- Example 3: Selective column encryption using FOR XML
PRINT 'Example 3: Selective column encryption using FOR XML';

DECLARE @sensitiveRowXml XML;
DECLARE @encryptedSensitiveRow NVARCHAR(MAX);
DECLARE @password NVARCHAR(MAX) = 'MySecurePassword123!';

-- Get only sensitive columns as XML (single row only)
SET @sensitiveRowXml = (
    SELECT (
        SELECT TOP 1
            UserID,
            Username,
            Email,
            SSN,
            Salary
        FROM Users 
        WHERE UserID = 1 
        FOR XML RAW('SensitiveRow'), ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE
    ) AS 'RowData'
    FOR XML PATH('root'), TYPE
);

-- Encrypt the sensitive data
EXEC dbo.EncryptRowWithMetadata 
    @rowXml = @sensitiveRowXml,
    @password = @password,
    @iterations = 10000,
    @encryptedRow = @encryptedSensitiveRow OUTPUT;

PRINT 'Encrypted sensitive row data length: ' + CAST(LEN(@encryptedSensitiveRow) AS NVARCHAR(10));

-- Decrypt the sensitive data
EXEC dbo.DecryptRowWithMetadata @encryptedSensitiveRow, @password;
GO

-- =============================================
-- STEP 3: PowerBuilder-Friendly Integration
-- =============================================
PRINT '--- STEP 3: PowerBuilder-Friendly Integration ---';
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
    
    -- Build dynamic SQL to get row as XML (single row only)
    SET @sql = N'
        SET @rowXml = (
            SELECT TOP 1 * 
            FROM ' + QUOTENAME(@tableName) + N' 
            WHERE ' + @whereClause + N'
            FOR XML RAW(''Row''), ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE
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
PRINT 'Testing PowerBuilder-friendly procedure...';

DECLARE @encryptedRowForPB NVARCHAR(MAX);

EXEC dbo.EncryptRowForPowerBuilder
    @tableName = 'Users',
    @whereClause = 'UserID = 1',
    @password = 'MySecurePassword123!',
    @iterations = 10000,
    @encryptedRow = @encryptedRowForPB OUTPUT;

PRINT 'PowerBuilder-friendly encrypted row length: ' + CAST(LEN(@encryptedRowForPB) AS NVARCHAR(10));

-- Decrypt the row
EXEC dbo.DecryptRowWithMetadata @encryptedRowForPB, 'MySecurePassword123!';
GO

-- =============================================
-- STEP 4: Complex Data Types with FOR XML
-- =============================================
PRINT '--- STEP 4: Complex Data Types with FOR XML ---';
GO

-- Create a table with complex data types
IF OBJECT_ID('dbo.ComplexData', 'U') IS NOT NULL
    DROP TABLE dbo.ComplexData;
GO

CREATE TABLE dbo.ComplexData (
    ID INT PRIMARY KEY,
    Name NVARCHAR(100),
    BinaryData VARBINARY(MAX),
    XmlData XML,
    JsonData NVARCHAR(MAX),
    DecimalValue DECIMAL(18,4),
    DateTimeValue DATETIME2,
    GuidValue UNIQUEIDENTIFIER
);
GO

-- Insert test data with complex types
INSERT INTO dbo.ComplexData (
    ID, Name, BinaryData, XmlData, JsonData, 
    DecimalValue, DateTimeValue, GuidValue
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

-- Encrypt complex row using FOR XML
PRINT 'Encrypting complex data row...';

DECLARE @complexRowXml XML;
DECLARE @encryptedComplexRow NVARCHAR(MAX);
DECLARE @password NVARCHAR(MAX) = 'MySecurePassword123!';

-- Get complex row as XML (single row only)
SET @complexRowXml = (
    SELECT TOP 1 * 
    FROM dbo.ComplexData 
    WHERE ID = 1 
    FOR XML RAW('ComplexRow'), ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE
);

-- Encrypt the complex row
EXEC dbo.EncryptRowWithMetadata 
    @rowXml = @complexRowXml,
    @password = @password,
    @iterations = 10000,
    @encryptedRow = @encryptedComplexRow OUTPUT;

PRINT 'Encrypted complex row data length: ' + CAST(LEN(@encryptedComplexRow) AS NVARCHAR(10));

-- Decrypt the complex row
EXEC dbo.DecryptRowWithMetadata @encryptedComplexRow, @password;
GO

-- =============================================
-- STEP 5: Error Handling and Validation
-- =============================================
PRINT '--- STEP 5: Error Handling and Validation ---';
GO

-- Test with invalid XML
PRINT 'Testing error handling with invalid XML...';
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
PRINT 'Testing error handling with NULL password...';
BEGIN TRY
    DECLARE @rowXml XML = (
        SELECT TOP 1 * 
        FROM Users 
        WHERE UserID = 1 
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
-- STEP 6: Performance Testing
-- =============================================
PRINT '--- STEP 6: Performance Testing ---';
GO

-- Create a larger test dataset
IF OBJECT_ID('dbo.PerformanceTest', 'U') IS NOT NULL
    DROP TABLE dbo.PerformanceTest;
GO

CREATE TABLE dbo.PerformanceTest (
    ID INT PRIMARY KEY,
    Name NVARCHAR(100),
    Description NVARCHAR(500),
    Value1 DECIMAL(10,2),
    Value2 DECIMAL(10,2),
    Value3 DECIMAL(10,2),
    CreatedAt DATETIME2 DEFAULT GETDATE()
);
GO

-- Insert 1000 test records
DECLARE @i INT = 1;
WHILE @i <= 1000
BEGIN
    INSERT INTO dbo.PerformanceTest (ID, Name, Description, Value1, Value2, Value3)
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
PRINT 'Testing batch encryption performance...';
DECLARE @startTime DATETIME2 = GETDATE();
DECLARE @rowsXml XML;
DECLARE @password NVARCHAR(MAX) = 'MySecurePassword123!';
DECLARE @iterations INT = 10000;
DECLARE @batchId NVARCHAR(50) = 'PERF_BATCH_' + CAST(GETDATE() AS NVARCHAR(50));

-- Get first 100 rows as XML
SET @rowsXml = (
    SELECT TOP 100 * 
    FROM dbo.PerformanceTest 
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
-- STEP 7: Security Validation
-- =============================================
PRINT '--- STEP 7: Security Validation ---';
GO

-- Test that encrypted data cannot be decrypted with wrong password
PRINT 'Testing security with wrong password...';
DECLARE @rowXml XML = (
    SELECT TOP 1 * 
    FROM Users 
    WHERE UserID = 1 
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

-- =============================================
-- SUMMARY
-- =============================================
PRINT '';
PRINT '=== ENHANCED USAGE EXAMPLES COMPLETED ===';
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
PRINT '  9. Comprehensive error handling';
PRINT '  10. Performance optimization';
PRINT '';
PRINT 'Example FOR XML Query (Single Row):';
PRINT '  SELECT TOP 1 * FROM Users WHERE UserID = 1';
PRINT '  FOR XML RAW(''Row''), ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE';
PRINT '';
PRINT 'For more examples, see: for-xml-usage-examples.sql';
GO 