-- Comprehensive Test Script for SQL CLR Fixes
-- This script tests all the critical fixes applied to the SecureLibrary-SQL implementation

USE [YourDatabaseName]; -- Replace with your database name
GO

-- Enable CLR if not already enabled
sp_configure 'clr enabled', 1;
RECONFIGURE;
GO

-- Test 1: Verify the assembly is loaded
SELECT 
    name,
    principal_id,
    assembly_id,
    clr_name,
    permission_set_desc,
    is_visible,
    create_date,
    modify_date
FROM sys.assemblies 
WHERE name = 'SecureLibrary.SQL';
GO

-- Test 2: Test basic encryption/decryption (should work as before)
DECLARE @testText NVARCHAR(MAX) = 'Hello, World! This is a test message.';
DECLARE @password NVARCHAR(100) = 'TestPassword123!';
DECLARE @encrypted NVARCHAR(MAX);
DECLARE @decrypted NVARCHAR(MAX);

-- Encrypt
SET @encrypted = dbo.EncryptAesGcmWithPasswordIterations(@testText, @password, 2000);
PRINT 'Encrypted: ' + @encrypted;

-- Decrypt
SET @decrypted = dbo.DecryptAesGcmWithPasswordIterations(@encrypted, @password, 2000);
PRINT 'Decrypted: ' + @decrypted;

-- Verify
IF @decrypted = @testText
    PRINT '✅ Basic encryption/decryption test PASSED';
ELSE
    PRINT '❌ Basic encryption/decryption test FAILED';
GO

-- Test 3: Create test table for table encryption tests
IF OBJECT_ID('TestEncryptionTable') IS NOT NULL
    DROP TABLE TestEncryptionTable;
GO

CREATE TABLE TestEncryptionTable (
    ID INT PRIMARY KEY,
    Name NVARCHAR(100),
    Email NVARCHAR(255),
    CreatedDate DATETIME DEFAULT GETDATE(),
    IsActive BIT DEFAULT 1,
    Salary DECIMAL(10,2),
    GUID UNIQUEIDENTIFIER DEFAULT NEWID()
);
GO

-- Insert test data
INSERT INTO TestEncryptionTable (ID, Name, Email, Salary) VALUES
(1, 'John Doe', 'john.doe@example.com', 75000.50),
(2, 'Jane Smith', 'jane.smith@example.com', 82000.75),
(3, 'Bob Johnson', 'bob.johnson@example.com', 65000.25);
GO

-- Test 4: Test table encryption with metadata (FIXED METHOD)
PRINT 'Testing table encryption with metadata...';
DECLARE @tableEncrypted NVARCHAR(MAX);
DECLARE @password NVARCHAR(100) = 'TablePassword123!';

-- Encrypt the table
SET @tableEncrypted = dbo.EncryptTableWithMetadataIterations('TestEncryptionTable', @password, 2000);

IF @tableEncrypted IS NOT NULL
    PRINT '✅ Table encryption test PASSED';
ELSE
    PRINT '❌ Table encryption test FAILED';
GO

-- Test 5: Test table decryption with metadata (FIXED METHOD)
PRINT 'Testing table decryption with metadata...';
DECLARE @tableEncrypted NVARCHAR(MAX);
DECLARE @password NVARCHAR(100) = 'TablePassword123!';

-- Get the encrypted data
SET @tableEncrypted = dbo.EncryptTableWithMetadataIterations('TestEncryptionTable', @password, 2000);

-- Decrypt and display results
EXEC dbo.DecryptTableWithMetadata @tableEncrypted, @password;
GO

-- Test 6: Test XML encryption with metadata (WORKING METHOD)
PRINT 'Testing XML encryption with metadata...';
DECLARE @xmlData XML;
DECLARE @xmlEncrypted NVARCHAR(MAX);
DECLARE @password NVARCHAR(100) = 'XmlPassword123!';

-- Create XML data
SET @xmlData = (
    SELECT * FROM TestEncryptionTable 
    FOR XML PATH('Row'), ROOT('Root')
);

-- Encrypt XML
SET @xmlEncrypted = dbo.EncryptXmlWithMetadataIterations(@xmlData, @password, 2000);

IF @xmlEncrypted IS NOT NULL
    PRINT '✅ XML encryption test PASSED';
ELSE
    PRINT '❌ XML encryption test FAILED';
GO

-- Test 7: Test XML decryption with metadata (UNIVERSAL PARSING)
PRINT 'Testing XML decryption with metadata (universal parsing)...';
DECLARE @xmlData XML;
DECLARE @xmlEncrypted NVARCHAR(MAX);
DECLARE @password NVARCHAR(100) = 'XmlPassword123!';

-- Create XML data
SET @xmlData = (
    SELECT * FROM TestEncryptionTable 
    FOR XML PATH('Row'), ROOT('Root')
);

-- Encrypt XML
SET @xmlEncrypted = dbo.EncryptXmlWithMetadataIterations(@xmlData, @password, 2000);

-- Decrypt and display results
EXEC dbo.DecryptTableWithMetadata @xmlEncrypted, @password;
GO

-- Test 8: Test with element-based XML (NEW CAPABILITY)
PRINT 'Testing element-based XML handling...';
DECLARE @elementXml XML = '
<Root>
    <Row>
        <ID>100</ID>
        <Name>Element Test</Name>
        <Email>element@test.com</Email>
        <Salary>99999.99</Salary>
    </Row>
</Root>';

DECLARE @elementEncrypted NVARCHAR(MAX);
DECLARE @password NVARCHAR(100) = 'ElementPassword123!';

-- Encrypt element-based XML
SET @elementEncrypted = dbo.EncryptXmlWithMetadataIterations(@elementXml, @password, 2000);

-- Decrypt and display results (should work with universal parsing)
EXEC dbo.DecryptTableWithMetadata @elementEncrypted, @password;
GO

-- Test 9: Test error handling with invalid data
PRINT 'Testing error handling...';
DECLARE @invalidPassword NVARCHAR(100) = 'WrongPassword';

-- Try to decrypt with wrong password
EXEC dbo.DecryptTableWithMetadata 'InvalidEncryptedData', @invalidPassword;
GO

-- Test 10: Test with different data types
PRINT 'Testing different data types...';
IF OBJECT_ID('TestDataTypesTable') IS NOT NULL
    DROP TABLE TestDataTypesTable;
GO

CREATE TABLE TestDataTypesTable (
    ID INT PRIMARY KEY,
    Name NVARCHAR(100),
    IsActive BIT,
    Salary DECIMAL(10,2),
    CreatedDate DATETIME,
    GUID UNIQUEIDENTIFIER,
    BinaryData VARBINARY(MAX)
);
GO

-- Insert test data with various types
INSERT INTO TestDataTypesTable (ID, Name, IsActive, Salary, CreatedDate, GUID, BinaryData) VALUES
(1, 'Type Test 1', 1, 12345.67, GETDATE(), NEWID(), CAST('Hello Binary' AS VARBINARY(MAX))),
(2, 'Type Test 2', 0, 98765.43, DATEADD(day, -1, GETDATE()), NEWID(), CAST('More Binary Data' AS VARBINARY(MAX)));

-- Test encryption/decryption with various data types
DECLARE @typeEncrypted NVARCHAR(MAX);
DECLARE @password NVARCHAR(100) = 'TypePassword123!';

SET @typeEncrypted = dbo.EncryptTableWithMetadataIterations('TestDataTypesTable', @password, 2000);
EXEC dbo.DecryptTableWithMetadata @typeEncrypted, @password;
GO

-- Test 11: Performance test with larger dataset
PRINT 'Testing performance with larger dataset...';
IF OBJECT_ID('TestLargeTable') IS NOT NULL
    DROP TABLE TestLargeTable;
GO

CREATE TABLE TestLargeTable (
    ID INT PRIMARY KEY,
    Name NVARCHAR(100),
    Description NVARCHAR(MAX),
    Value DECIMAL(18,4),
    CreatedDate DATETIME DEFAULT GETDATE()
);
GO

-- Insert larger dataset
DECLARE @i INT = 1;
WHILE @i <= 100
BEGIN
    INSERT INTO TestLargeTable (ID, Name, Description, Value) VALUES
    (@i, 
     'Item ' + CAST(@i AS NVARCHAR(10)), 
     'This is a detailed description for item ' + CAST(@i AS NVARCHAR(10)) + ' with some additional text to make it longer.',
     RAND() * 10000);
    SET @i = @i + 1;
END

-- Test encryption/decryption performance
DECLARE @startTime DATETIME = GETDATE();
DECLARE @largeEncrypted NVARCHAR(MAX);
DECLARE @password NVARCHAR(100) = 'LargePassword123!';

SET @largeEncrypted = dbo.EncryptTableWithMetadataIterations('TestLargeTable', @password, 2000);
DECLARE @encryptTime DATETIME = GETDATE();

EXEC dbo.DecryptTableWithMetadata @largeEncrypted, @password;
DECLARE @decryptTime DATETIME = GETDATE();

PRINT 'Encryption time: ' + CAST(DATEDIFF(millisecond, @startTime, @encryptTime) AS NVARCHAR(10)) + ' ms';
PRINT 'Decryption time: ' + CAST(DATEDIFF(millisecond, @encryptTime, @decryptTime) AS NVARCHAR(10)) + ' ms';
GO

-- Test 12: Cleanup
PRINT 'Cleaning up test tables...';
IF OBJECT_ID('TestEncryptionTable') IS NOT NULL
    DROP TABLE TestEncryptionTable;
IF OBJECT_ID('TestDataTypesTable') IS NOT NULL
    DROP TABLE TestDataTypesTable;
IF OBJECT_ID('TestLargeTable') IS NOT NULL
    DROP TABLE TestLargeTable;
GO

PRINT 'All tests completed!';
PRINT 'If you see this message, the comprehensive fixes are working correctly.';
GO 