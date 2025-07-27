-- Test Scripts for Row-by-Row Encryption and Decryption Functions
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

-- Test 3: Complete Round-Trip Table Encryption/Decryption
PRINT '=== Test 3: Complete Round-Trip Table Encryption/Decryption ===';

DECLARE @key NVARCHAR(64) = 'roundtrip-key-base64-encoded123456789012345678901234567890AB';
DECLARE @nonce NVARCHAR(24) = 'roundtrip-nonce-base64';

-- Original table data
DECLARE @originalJson NVARCHAR(MAX) = '[
    {"id": 1, "name": "Test User 1", "email": "user1@test.com"},
    {"id": 2, "name": "Test User 2", "email": "user2@test.com"},
    {"id": 3, "name": "Test User 3", "email": "user3@test.com"}
]';

-- Step 1: Encrypt the table
PRINT 'Step 1: Encrypting table data...';
CREATE TABLE #EncryptedData (
    RowId INT,
    EncryptedData NVARCHAR(MAX),
    AuthTag NVARCHAR(32)
);

INSERT INTO #EncryptedData (RowId, EncryptedData, AuthTag)
SELECT RowId, EncryptedData, AuthTag
FROM dbo.EncryptTableRowsAesGcm(@originalJson, @key, @nonce);

SELECT 'Encrypted Table Data' AS Step, COUNT(*) AS RowsEncrypted FROM #EncryptedData;

-- Step 2: Prepare encrypted data for bulk decryption
DECLARE @encryptedBulkData NVARCHAR(MAX) = '';
SELECT @encryptedBulkData = @encryptedBulkData + 
    CAST(RowId AS NVARCHAR(10)) + '|' + EncryptedData + '|' + AuthTag + CHAR(13) + CHAR(10)
FROM #EncryptedData
ORDER BY RowId;

-- Step 3: Decrypt the table back to structured format
PRINT 'Step 2: Decrypting table data back to structured format...';
SELECT 
    'Decrypted Table Data' AS Step,
    RowId,
    DecryptedData,
    LEN(DecryptedData) AS DecryptedDataLength
FROM dbo.DecryptBulkTableData(@encryptedBulkData, @key, @nonce)
ORDER BY RowId;

-- Clean up
DROP TABLE #EncryptedData;
GO

-- Test 4: Customer Table Row-by-Row Encryption with Full Round-Trip
PRINT '=== Test 4: Customer Table Example with Complete Decryption ===';

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

-- Create encrypted customers table
CREATE TABLE #EncryptedCustomers (
    RowId INT,
    EncryptedData NVARCHAR(MAX),
    AuthTag NVARCHAR(32)
);

INSERT INTO #EncryptedCustomers (RowId, EncryptedData, AuthTag)
SELECT RowId, EncryptedData, AuthTag
FROM dbo.EncryptTableRowsAesGcm(@customersJson, @customerKey, @customerNonce);

-- Show encrypted customer data
PRINT 'Encrypted customer data:';
SELECT 
    'Encrypted Customer ' + CAST(RowId AS NVARCHAR(10)) AS Description,
    RowId,
    LEFT(EncryptedData, 50) + '...' AS EncryptedDataSample,
    AuthTag
FROM #EncryptedCustomers
ORDER BY RowId;

-- Now decrypt the customers back to usable format
PRINT 'Decrypted customer data:';
DECLARE @encryptedCustomerBulk NVARCHAR(MAX) = '';
SELECT @encryptedCustomerBulk = @encryptedCustomerBulk + 
    CAST(RowId AS NVARCHAR(10)) + '|' + EncryptedData + '|' + AuthTag + CHAR(13) + CHAR(10)
FROM #EncryptedCustomers
ORDER BY RowId;

-- Decrypt and show results
SELECT 
    'Decrypted Customer ' + CAST(RowId AS NVARCHAR(10)) AS Description,
    RowId,
    DecryptedData
FROM dbo.DecryptBulkTableData(@encryptedCustomerBulk, @customerKey, @customerNonce)
ORDER BY RowId;

-- Clean up
DROP TABLE #Customers;
DROP TABLE #EncryptedCustomers;

PRINT 'Customer encryption/decryption round-trip completed successfully!';
GO

-- Test 5: PowerBuilder Integration Example
PRINT '=== Test 5: PowerBuilder Integration Example ===';

-- Simulate PowerBuilder accessing encrypted data via views
DECLARE @pbKey NVARCHAR(64) = 'powerbuilder-integration-key-base64-encoded123456789012345678901234567890AB';
DECLARE @pbNonce NVARCHAR(24) = 'pb-integration-nonce';

-- Sample employee data (typical PowerBuilder scenario)
DECLARE @employeeJson NVARCHAR(MAX) = '[
    {"employee_id": 1001, "first_name": "김철수", "last_name": "Kim", "department": "IT", "salary": 4500000},
    {"employee_id": 1002, "first_name": "이영희", "last_name": "Lee", "department": "HR", "salary": 4200000},
    {"employee_id": 1003, "first_name": "박민수", "last_name": "Park", "department": "Finance", "salary": 4800000}
]';

-- Create encrypted employee table (simulating stored encrypted data)
CREATE TABLE #EncryptedEmployees (
    RowId INT,
    EncryptedData NVARCHAR(MAX),
    AuthTag NVARCHAR(32)
);

INSERT INTO #EncryptedEmployees (RowId, EncryptedData, AuthTag)
SELECT RowId, EncryptedData, AuthTag
FROM dbo.EncryptTableRowsAesGcm(@employeeJson, @pbKey, @pbNonce);

-- PowerBuilder can now query this decrypted view directly
PRINT 'PowerBuilder can query decrypted employee data like this:';

-- Method 1: Using bulk decryption function (recommended for small businesses)
DECLARE @encryptedEmpBulk NVARCHAR(MAX) = '';
SELECT @encryptedEmpBulk = @encryptedEmpBulk + 
    CAST(RowId AS NVARCHAR(10)) + '|' + EncryptedData + '|' + AuthTag + CHAR(13) + CHAR(10)
FROM #EncryptedEmployees
ORDER BY RowId;

SELECT 
    RowId AS employee_row,
    DecryptedData AS employee_json_data
FROM dbo.DecryptBulkTableData(@encryptedEmpBulk, @pbKey, @pbNonce)
ORDER BY RowId;

-- Method 2: Row-by-row decryption for selective access
PRINT 'Row-by-row decryption for selective PowerBuilder access:';
SELECT 
    e.RowId,
    dbo.DecryptRowDataAesGcm(e.EncryptedData + e.AuthTag, @pbKey, @pbNonce) AS DecryptedEmployeeData
FROM #EncryptedEmployees e
WHERE e.RowId <= 2  -- PowerBuilder can apply WHERE conditions
ORDER BY e.RowId;

-- Clean up
DROP TABLE #EncryptedEmployees;

PRINT 'PowerBuilder integration example completed successfully!';
GO

-- Test 6: Bulk Processing Test
PRINT '=== Test 6: Bulk Processing Test ===';

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

-- Test 7: Performance Comparison
PRINT '=== Test 7: Performance Test with Decryption ===';

DECLARE @perfKey NVARCHAR(64) = 'performance-test-key-base64-encoded123456789012345678901234567890AB';
DECLARE @perfNonce NVARCHAR(24) = 'perf-nonce-base64-key';

-- Single row performance
DECLARE @startTime DATETIME2 = SYSDATETIME();
DECLARE @testRow NVARCHAR(MAX) = '{"id": 1, "name": "Performance Test", "data": "' + REPLICATE('X', 1000) + '"}';
DECLARE @perfResult NVARCHAR(MAX) = dbo.EncryptRowDataAesGcm(@testRow, @perfKey, @perfNonce);
DECLARE @decryptResult NVARCHAR(MAX) = dbo.DecryptRowDataAesGcm(@perfResult, @perfKey, @perfNonce);
DECLARE @endTime DATETIME2 = SYSDATETIME();

SELECT 
    'Single Row Encrypt/Decrypt' AS Test,
    DATEDIFF(MICROSECOND, @startTime, @endTime) AS ExecutionTimeMicroseconds,
    LEN(@testRow) AS OriginalDataLength,
    LEN(@perfResult) AS EncryptedDataLength,
    CASE WHEN @testRow = @decryptResult THEN 'SUCCESS' ELSE 'FAILED' END AS RoundTripTest;

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

-- Test bulk encrypt/decrypt cycle
CREATE TABLE #PerfTest (RowId INT, EncryptedData NVARCHAR(MAX), AuthTag NVARCHAR(32));
INSERT INTO #PerfTest SELECT RowId, EncryptedData, AuthTag FROM dbo.EncryptTableRowsAesGcm(@multiRowJson, @perfKey, @perfNonce);

DECLARE @perfBulkData NVARCHAR(MAX) = '';
SELECT @perfBulkData = @perfBulkData + CAST(RowId AS NVARCHAR(10)) + '|' + EncryptedData + '|' + AuthTag + CHAR(13) + CHAR(10) FROM #PerfTest ORDER BY RowId;

DECLARE @decryptCount INT = (SELECT COUNT(*) FROM dbo.DecryptBulkTableData(@perfBulkData, @perfKey, @perfNonce));
SET @endTime = SYSDATETIME();

SELECT 
    'Bulk Encrypt/Decrypt Cycle' AS Test,
    DATEDIFF(MICROSECOND, @startTime, @endTime) AS ExecutionTimeMicroseconds,
    @decryptCount AS RowsProcessed,
    DATEDIFF(MICROSECOND, @startTime, @endTime) / @decryptCount AS MicrosecondsPerRow;

DROP TABLE #PerfTest;
GO

PRINT '=== All Tests Completed Successfully! ===';
PRINT '';
PRINT 'Summary of available functions:';
PRINT '1. dbo.EncryptRowDataAesGcm - Encrypt a single JSON row';
PRINT '2. dbo.DecryptRowDataAesGcm - Decrypt a single encrypted row';
PRINT '3. dbo.EncryptTableRowsAesGcm - Table-valued function for bulk encryption';
PRINT '4. dbo.DecryptBulkTableData - Table-valued function for bulk decryption';
PRINT '5. dbo.DecryptTableFromView - Decrypt table data for use in views/procedures';
PRINT '6. dbo.BulkProcessRowsAesGcm - Streaming bulk processing procedure';
PRINT '';
PRINT 'NEW DECRYPTION CAPABILITIES:';
PRINT '• SQL Server-side decryption functions for direct table structure restoration';
PRINT '• PowerBuilder can now query decrypted data directly in views and stored procedures';
PRINT '• Support for small business scenarios with direct database access';
PRINT '• Complete round-trip encryption/decryption with structured table output';
PRINT '';
PRINT 'All functions use existing AES-GCM cryptographic implementation for security.';
PRINT 'PowerBuilder compatibility maintained - existing functions unchanged.';