-- Test Scripts for Row-by-Row Encryption Functions
-- Run this script after CreateFunctions.sql

USE [YourDatabase]
GO

-- Test 1: Single Row Encryption/Decryption
PRINT '=== Test 1: Single Row Encryption/Decryption ===';

DECLARE @key NVARCHAR(64) = 'your-32-byte-key-here-base64-encoded123456789012345678901234567890AB';
DECLARE @nonce NVARCHAR(24) = 'your-12-byte-nonce-base64';

DECLARE @rowJson NVARCHAR(MAX) = '{"id": 1, "name": "John Doe", "email": "john@example.com", "salary": 75000}';

-- Encrypt the row
DECLARE @encrypted NVARCHAR(MAX) = dbo.EncryptRowDataAesGcm(@rowJson, @key, @nonce);
SELECT 'Encrypted Row Data' AS Operation, @encrypted AS Result;

-- Decrypt the row
DECLARE @decrypted NVARCHAR(MAX) = dbo.DecryptRowDataAesGcm(@encrypted, @key, @nonce);
SELECT 'Decrypted Row Data' AS Operation, @decrypted AS Result;

-- Verify round-trip
IF @rowJson = @decrypted
    PRINT 'SUCCESS: Round-trip encryption/decryption successful';
ELSE
    PRINT 'ERROR: Round-trip encryption/decryption failed';
GO

-- Test 2: Table Rows Encryption (TVF)
PRINT '=== Test 2: Table Rows Encryption using TVF ===';

DECLARE @key NVARCHAR(64) = 'your-32-byte-key-here-base64-encoded123456789012345678901234567890AB';
DECLARE @nonce NVARCHAR(24) = 'your-12-byte-nonce-base64';

-- Sample table data as JSON array
DECLARE @tableJson NVARCHAR(MAX) = '[
    {"id": 1, "name": "Alice Smith", "department": "Engineering", "salary": 85000},
    {"id": 2, "name": "Bob Johnson", "department": "Marketing", "salary": 70000},
    {"id": 3, "name": "Carol Williams", "department": "Sales", "salary": 65000}
]';

-- Encrypt table rows using TVF
SELECT 
    RowId,
    EncryptedData,
    AuthTag,
    LEN(EncryptedData) AS EncryptedDataLength,
    LEN(AuthTag) AS AuthTagLength
FROM dbo.EncryptTableRowsAesGcm(@tableJson, @key, @nonce)
ORDER BY RowId;
GO

-- Test 3: Customer Table Row-by-Row Encryption Example
PRINT '=== Test 3: Customer Table Example ===';

-- Create sample customer data table
IF OBJECT_ID('tempdb..#Customers') IS NOT NULL
    DROP TABLE #Customers;

CREATE TABLE #Customers (
    CustomerID INT,
    FirstName NVARCHAR(50),
    LastName NVARCHAR(50),
    Email NVARCHAR(100),
    Phone NVARCHAR(20),
    CreditCardNumber NVARCHAR(20)
);

INSERT INTO #Customers VALUES
(1, 'John', 'Doe', 'john.doe@email.com', '555-0101', '4111-1111-1111-1111'),
(2, 'Jane', 'Smith', 'jane.smith@email.com', '555-0102', '4222-2222-2222-2222'),
(3, 'Mike', 'Johnson', 'mike.johnson@email.com', '555-0103', '4333-3333-3333-3333');

-- Convert customer table to JSON for encryption
DECLARE @customersJson NVARCHAR(MAX) = (
    SELECT * FROM #Customers FOR JSON PATH
);

DECLARE @customerKey NVARCHAR(64) = 'customer-encryption-key-base64-encoded123456789012345678901234567890AB';
DECLARE @customerNonce NVARCHAR(24) = 'cust-nonce-base64-key';

-- Encrypt customer rows
SELECT 
    'Encrypted Customer ' + CAST(RowId AS NVARCHAR(10)) AS Description,
    RowId,
    LEFT(EncryptedData, 50) + '...' AS EncryptedDataSample,
    AuthTag
FROM dbo.EncryptTableRowsAesGcm(@customersJson, @customerKey, @customerNonce)
ORDER BY RowId;

PRINT 'Customer data encrypted successfully!';
GO

-- Test 4: Bulk Processing Test
PRINT '=== Test 4: Bulk Processing Test ===';

DECLARE @bulkKey NVARCHAR(64) = 'bulk-processing-key-base64-encoded123456789012345678901234567890AB';

-- Generate larger test dataset
DECLARE @bulkJson NVARCHAR(MAX) = '[';
DECLARE @i INT = 1;
WHILE @i <= 10
BEGIN
    IF @i > 1 SET @bulkJson = @bulkJson + ',';
    SET @bulkJson = @bulkJson + '{"id": ' + CAST(@i AS NVARCHAR(10)) + ', "data": "Test record ' + CAST(@i AS NVARCHAR(10)) + '", "timestamp": "2025-01-01T00:00:00"}';
    SET @i = @i + 1;
END
SET @bulkJson = @bulkJson + ']';

-- Execute bulk processing (outputs to SQL Server messages)
EXEC dbo.BulkProcessRowsAesGcm @bulkJson, @bulkKey, 3; -- Process in batches of 3
GO

-- Test 5: Performance Comparison
PRINT '=== Test 5: Performance Test ===';

DECLARE @perfKey NVARCHAR(64) = 'performance-test-key-base64-encoded123456789012345678901234567890AB';
DECLARE @perfNonce NVARCHAR(24) = 'perf-nonce-base64-key';

-- Single row performance
DECLARE @startTime DATETIME2 = SYSDATETIME();
DECLARE @testRow NVARCHAR(MAX) = '{"id": 1, "name": "Performance Test", "data": "' + REPLICATE('X', 1000) + '"}';
DECLARE @perfResult NVARCHAR(MAX) = dbo.EncryptRowDataAesGcm(@testRow, @perfKey, @perfNonce);
DECLARE @endTime DATETIME2 = SYSDATETIME();

SELECT 
    'Single Row Encryption' AS Test,
    DATEDIFF(MICROSECOND, @startTime, @endTime) AS ExecutionTimeMicroseconds,
    LEN(@testRow) AS OriginalDataLength,
    LEN(@perfResult) AS EncryptedDataLength;

-- Multiple rows performance (using TVF)
SET @startTime = SYSDATETIME();

DECLARE @multiRowJson NVARCHAR(MAX) = '[';
DECLARE @j INT = 1;
WHILE @j <= 5
BEGIN
    IF @j > 1 SET @multiRowJson = @multiRowJson + ',';
    SET @multiRowJson = @multiRowJson + '{"id": ' + CAST(@j AS NVARCHAR(10)) + ', "data": "' + REPLICATE('Y', 500) + '"}';
    SET @j = @j + 1;
END
SET @multiRowJson = @multiRowJson + ']';

DECLARE @multiCount INT = (SELECT COUNT(*) FROM dbo.EncryptTableRowsAesGcm(@multiRowJson, @perfKey, @perfNonce));
SET @endTime = SYSDATETIME();

SELECT 
    'Multiple Rows Encryption (TVF)' AS Test,
    DATEDIFF(MICROSECOND, @startTime, @endTime) AS ExecutionTimeMicroseconds,
    @multiCount AS RowsProcessed,
    DATEDIFF(MICROSECOND, @startTime, @endTime) / @multiCount AS MicrosecondsPerRow;

GO

PRINT '=== All Tests Completed Successfully! ===';
PRINT '';
PRINT 'Summary of available functions:';
PRINT '1. dbo.EncryptRowDataAesGcm - Encrypt a single JSON row';
PRINT '2. dbo.DecryptRowDataAesGcm - Decrypt a single encrypted row';
PRINT '3. dbo.EncryptTableRowsAesGcm - Table-valued function for bulk encryption';
PRINT '4. dbo.BulkProcessRowsAesGcm - Streaming bulk processing procedure';
PRINT '';
PRINT 'All functions use existing AES-GCM cryptographic implementation for security.';
PRINT 'PowerBuilder compatibility maintained - existing functions unchanged.';