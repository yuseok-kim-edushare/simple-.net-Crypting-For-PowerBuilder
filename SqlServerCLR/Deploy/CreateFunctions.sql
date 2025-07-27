-- Create SQL Server CLR Functions for Row-by-Row Encryption
-- Run this script after CreateAssembly.sql

USE [YourDatabase]
GO

-- Create Row-by-Row Encryption Functions

-- Single row encryption function
CREATE FUNCTION dbo.EncryptRowDataAesGcm(
    @rowJson NVARCHAR(MAX), 
    @base64Key NVARCHAR(MAX), 
    @base64Nonce NVARCHAR(32)
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].EncryptRowDataAesGcm;
GO

-- Single row decryption function
CREATE FUNCTION dbo.DecryptRowDataAesGcm(
    @base64EncryptedData NVARCHAR(MAX), 
    @base64Key NVARCHAR(MAX), 
    @base64Nonce NVARCHAR(32)
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].DecryptRowDataAesGcm;
GO

-- Table-valued function for bulk row encryption
CREATE FUNCTION dbo.EncryptTableRowsAesGcm(
    @tableDataJson NVARCHAR(MAX), 
    @base64Key NVARCHAR(MAX), 
    @base64Nonce NVARCHAR(32)
)
RETURNS TABLE (RowId INT, EncryptedData NVARCHAR(MAX), AuthTag NVARCHAR(32))
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].EncryptTableRowsAesGcm;
GO

-- Bulk processing procedure with streaming
CREATE PROCEDURE dbo.BulkProcessRowsAesGcm
    @tableDataJson NVARCHAR(MAX),
    @base64Key NVARCHAR(MAX),
    @batchSize INT = 1000
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].BulkProcessRowsAesGcm;
GO

-- Verify functions were created
SELECT 
    SCHEMA_NAME(o.schema_id) AS SchemaName,
    o.name AS ObjectName,
    o.type_desc AS ObjectType,
    o.create_date AS CreateDate
FROM sys.objects o
WHERE o.name IN (
    'EncryptRowDataAesGcm', 
    'DecryptRowDataAesGcm', 
    'EncryptTableRowsAesGcm', 
    'BulkProcessRowsAesGcm'
)
ORDER BY o.name;
GO

PRINT 'Row-by-row encryption functions created successfully!';
PRINT 'Next: Run TestScripts.sql to test the functionality.';