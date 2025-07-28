-- Create SQL Server CLR Functions and Procedures for Password-Based Table Encryption
-- Run this script after CreateAssembly.sql

USE [YourDatabase]
GO

-- Create Password-Based Table Encryption Functions

-- Encrypts table data (as XML) using a password
CREATE FUNCTION dbo.EncryptXmlWithPassword(
    @xmlData XML, 
    @password NVARCHAR(MAX)
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].EncryptXmlWithPassword;
GO

-- Encrypts table data with a specific iteration count
CREATE FUNCTION dbo.EncryptXmlWithPasswordIterations(
    @xmlData XML, 
    @password NVARCHAR(MAX),
    @iterations INT
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].EncryptXmlWithPasswordIterations;
GO

-- Universal procedure to decrypt and restore any table
CREATE PROCEDURE dbo.RestoreEncryptedTable
    @encryptedData NVARCHAR(MAX),
    @password NVARCHAR(MAX)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].RestoreEncryptedTable;
GO

-- Verify objects were created
SELECT 
    SCHEMA_NAME(o.schema_id) AS SchemaName,
    o.name AS ObjectName,
    o.type_desc AS ObjectType,
    o.create_date AS CreateDate
FROM sys.objects o
WHERE o.name IN (
    'EncryptXmlWithPassword', 
    'EncryptXmlWithPasswordIterations',
    'RestoreEncryptedTable'
)
ORDER BY o.name;
GO

PRINT 'Password-based table encryption objects created successfully!';
PRINT 'Objects available:';
PRINT '  - EncryptXmlWithPassword: Encrypts XML using a password.';
PRINT '  - RestoreEncryptedTable: The universal procedure to decrypt and restore any table.';
PRINT '';
PRINT 'Next: Run TestScripts.sql to test the new functionality.';