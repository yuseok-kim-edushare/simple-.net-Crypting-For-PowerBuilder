-- Create SQL Server CLR Functions and Procedures for Password-Based Table Encryption
-- Run this script after CreateAssembly.sql

USE [YourDatabase]
GO

-- Create Password-Based Table Encryption Functions

-- Encrypts table data (as XML) using a password - Legacy version
CREATE FUNCTION dbo.EncryptXmlWithPassword(
    @xmlData XML, 
    @password NVARCHAR(MAX)
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].EncryptXmlWithPassword;
GO

-- Encrypts table data with a specific iteration count - Legacy version
CREATE FUNCTION dbo.EncryptXmlWithPasswordIterations(
    @xmlData XML, 
    @password NVARCHAR(MAX),
    @iterations INT
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].EncryptXmlWithPasswordIterations;
GO

-- Enhanced function: Encrypts table data with embedded schema metadata for zero-cast decryption
CREATE FUNCTION dbo.EncryptTableWithMetadata(
    @tableName NVARCHAR(MAX),
    @password NVARCHAR(MAX)
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].EncryptTableWithMetadata;
GO

-- Enhanced function: Encrypts table data with embedded schema metadata and custom iterations
CREATE FUNCTION dbo.EncryptTableWithMetadataIterations(
    @tableName NVARCHAR(MAX),
    @password NVARCHAR(MAX),
    @iterations INT
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].EncryptTableWithMetadataIterations;
GO

-- Enhanced function: Encrypts XML data with embedded schema metadata
CREATE FUNCTION dbo.EncryptXmlWithMetadata(
    @xmlData XML,
    @password NVARCHAR(MAX)
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].EncryptXmlWithMetadata;
GO

-- Enhanced function: Encrypts XML data with embedded schema metadata and custom iterations
CREATE FUNCTION dbo.EncryptXmlWithMetadataIterations(
    @xmlData XML,
    @password NVARCHAR(MAX),
    @iterations INT
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].EncryptXmlWithMetadataIterations;
GO

-- Universal procedure to decrypt and restore any table
CREATE PROCEDURE dbo.RestoreEncryptedTable
    @encryptedData NVARCHAR(MAX),
    @password NVARCHAR(MAX)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].RestoreEncryptedTable;
GO

-- Table-Valued Function that wraps the decryption logic - Legacy version
-- This allows direct XML shredding in SELECT statements without temp tables:
-- SELECT T.c.value('@ColumnName', 'NVARCHAR(MAX)') AS ColumnName
-- FROM dbo.DecryptTableTVF(@encrypted, @password) d
-- CROSS APPLY d.DecryptedXml.nodes('/Root/Row') AS T(c)
CREATE FUNCTION dbo.DecryptTableTVF(
    @encryptedData NVARCHAR(MAX),
    @password NVARCHAR(MAX)
)
RETURNS TABLE (DecryptedXml XML)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].DecryptTableTVF;
GO

-- SOPHISTICATED TVF WITH EMBEDDED SCHEMA METADATA & ROBUST TYPED OUTPUT
-- This enhanced CLR TVF eliminates all manual SQL-side casting by:
-- 1. Reading embedded schema metadata from the encrypted package
-- 2. Building proper SqlMetaData[] array for all column types  
-- 3. Returning SqlDataRecord objects with correct typing
-- 4. Providing robust error handling and partial recovery
-- 
-- Usage: SELECT * FROM dbo.DecryptTableTypedTVF(@encrypted, @password)
-- Result: Properly typed columns ready to use - NO CASTING REQUIRED!
CREATE FUNCTION dbo.DecryptTableTypedTVF(
    @encryptedPackage NVARCHAR(MAX),
    @password NVARCHAR(MAX)
)
RETURNS TABLE
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].DecryptTableTypedTVF;
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
    'EncryptTableWithMetadata',
    'EncryptTableWithMetadataIterations',
    'EncryptXmlWithMetadata',
    'EncryptXmlWithMetadataIterations',
    'RestoreEncryptedTable',
    'DecryptTableTVF',
    'DecryptTableTypedTVF'
)
ORDER BY o.name;
GO

PRINT 'Enhanced password-based table encryption objects created successfully!';
PRINT 'Objects available:';
PRINT '';
PRINT 'LEGACY FUNCTIONS (require manual casting):';
PRINT '  - EncryptXmlWithPassword: Encrypts XML using a password.';
PRINT '  - RestoreEncryptedTable: The universal procedure to decrypt and restore any table.';
PRINT '  - DecryptTableTVF: Table-Valued Function for direct SELECT usage without temp tables.';
PRINT '';
PRINT 'ENHANCED FUNCTIONS (with embedded schema metadata):';
PRINT '  - EncryptTableWithMetadata: Encrypts table data with full schema metadata embedded.';
PRINT '  - EncryptXmlWithMetadata: Encrypts XML data with inferred schema metadata.';
PRINT '  - DecryptTableTypedTVF: ZERO-CAST TVF returns properly typed columns directly!';
PRINT '';
PRINT 'KEY BENEFITS OF NEW ENHANCED FUNCTIONS:';
PRINT '  ✓ ZERO SQL CAST: No manual casting required - columns are properly typed';
PRINT '  ✓ SELF-DESCRIBING: Schema metadata travels inside encrypted package';
PRINT '  ✓ FULL TYPE SUPPORT: All SQL Server data types with robust fallback';
PRINT '  ✓ PARTIAL RECOVERY: Continues working even if some metadata is missing';
PRINT '  ✓ UNIVERSAL COMPATIBILITY: Works with any table design';
PRINT '';
PRINT 'USAGE EXAMPLES:';
PRINT '';
PRINT '-- Enhanced 3-line encryption with metadata:';
PRINT 'DECLARE @encrypted NVARCHAR(MAX) = dbo.EncryptTableWithMetadata(''MyTable'', ''MyPassword'');';
PRINT '-- Zero-cast decryption:';
PRINT 'SELECT * FROM dbo.DecryptTableTypedTVF(@encrypted, ''MyPassword'');';
PRINT '';
PRINT '-- Legacy approach (still supported):';
PRINT 'DECLARE @xml XML = (SELECT * FROM MyTable FOR XML PATH(''Row''), ROOT(''Root''));';
PRINT 'DECLARE @encrypted NVARCHAR(MAX) = dbo.EncryptXmlWithPassword(@xml, ''MyPassword'');';
PRINT 'SELECT T.c.value(''@ColName'', ''NVARCHAR(MAX)'') AS ColName';
PRINT '  FROM dbo.DecryptTableTVF(@encrypted, ''MyPassword'') d';
PRINT '  CROSS APPLY d.DecryptedXml.nodes(''/Root/Row'') AS T(c);';
PRINT '';
PRINT 'Next: Run demonstration scripts to see the enhanced capabilities!';