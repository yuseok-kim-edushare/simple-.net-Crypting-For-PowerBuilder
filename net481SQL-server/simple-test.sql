-- Simple Test Script for SQL CLR Fixes
-- This script tests the basic functionality with better error reporting

USE [YourDatabaseName]; -- Replace with your database name
GO

-- Test 1: Create a simple test table
IF OBJECT_ID('SimpleTestTable') IS NOT NULL
    DROP TABLE SimpleTestTable;
GO

CREATE TABLE SimpleTestTable (
    ID INT PRIMARY KEY,
    Name NVARCHAR(100),
    Email NVARCHAR(255)
);
GO

-- Insert test data
INSERT INTO SimpleTestTable (ID, Name, Email) VALUES
(1, 'John Doe', 'john@example.com'),
(2, 'Jane Smith', 'jane@example.com');
GO

-- Test 2: Test table encryption with metadata
PRINT '=== Testing Table Encryption ===';
DECLARE @tableEncrypted NVARCHAR(MAX);
DECLARE @password NVARCHAR(100) = 'TestPassword123!';

-- Encrypt the table
SET @tableEncrypted = dbo.EncryptTableWithMetadataIterations('SimpleTestTable', @password, 2000);

IF @tableEncrypted IS NOT NULL
    PRINT '✅ Table encryption SUCCESS';
ELSE
    PRINT '❌ Table encryption FAILED';
GO

-- Test 3: Test table decryption with metadata
PRINT '=== Testing Table Decryption ===';
DECLARE @tableEncrypted NVARCHAR(MAX);
DECLARE @password NVARCHAR(100) = 'TestPassword123!';

-- Get the encrypted data
SET @tableEncrypted = dbo.EncryptTableWithMetadataIterations('SimpleTestTable', @password, 2000);

-- Decrypt and display results
EXEC dbo.DecryptTableWithMetadata @tableEncrypted, @password;
GO

-- Test 4: Test with XML data
PRINT '=== Testing XML Encryption ===';
DECLARE @xmlData XML;
DECLARE @xmlEncrypted NVARCHAR(MAX);
DECLARE @password NVARCHAR(100) = 'XmlPassword123!';

-- Create XML data
SET @xmlData = (
    SELECT * FROM SimpleTestTable 
    FOR XML PATH('Row'), ROOT('Root')
);

-- Encrypt XML
SET @xmlEncrypted = dbo.EncryptXmlWithMetadataIterations(@xmlData, @password, 2000);

IF @xmlEncrypted IS NOT NULL
    PRINT '✅ XML encryption SUCCESS';
ELSE
    PRINT '❌ XML encryption FAILED';
GO

-- Test 5: Test XML decryption
PRINT '=== Testing XML Decryption ===';
DECLARE @xmlData XML;
DECLARE @xmlEncrypted NVARCHAR(MAX);
DECLARE @password NVARCHAR(100) = 'XmlPassword123!';

-- Create XML data
SET @xmlData = (
    SELECT * FROM SimpleTestTable 
    FOR XML PATH('Row'), ROOT('Root')
);

-- Encrypt XML
SET @xmlEncrypted = dbo.EncryptXmlWithMetadataIterations(@xmlData, @password, 2000);

-- Decrypt and display results
EXEC dbo.DecryptTableWithMetadata @xmlEncrypted, @password;
GO

-- Test 6: Cleanup
PRINT '=== Cleaning Up ===';
IF OBJECT_ID('SimpleTestTable') IS NOT NULL
    DROP TABLE SimpleTestTable;
GO

PRINT '=== Test Completed ===';
GO 