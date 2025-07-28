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
-- This procedure handles stored procedure result sets and can be used with INSERT INTO ... EXEC
CREATE PROCEDURE dbo.RestoreEncryptedTable
    @encryptedData NVARCHAR(MAX),
    @password NVARCHAR(MAX)
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.SqlCLRCrypting].RestoreEncryptedTable;
GO

-- Dynamic temp-table wrapper that automatically discovers any stored procedure's result set structure
-- and creates a matching temp table. This eliminates the need for manual column declarations.
CREATE PROCEDURE dbo.WrapDecryptProcedure
    @procedureName NVARCHAR(MAX),
    @parameters NVARCHAR(MAX) = NULL
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.DynamicTempTableWrapper].WrapDecryptProcedure;
GO

-- Enhanced version of WrapDecryptProcedure that provides more detailed metadata information
-- and supports custom temp table names for better integration with existing workflows.
CREATE PROCEDURE dbo.WrapDecryptProcedureAdvanced
    @procedureName NVARCHAR(MAX),
    @parameters NVARCHAR(MAX) = NULL,
    @tempTableName NVARCHAR(MAX) = NULL
AS EXTERNAL NAME SimpleDotNetCrypting.[SecureLibrary.SQL.DynamicTempTableWrapper].WrapDecryptProcedureAdvanced;
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
    'WrapDecryptProcedure',
    'WrapDecryptProcedureAdvanced'
)
ORDER BY o.name;
GO

PRINT 'Enhanced password-based table encryption objects created successfully!';
PRINT 'Objects available:';
PRINT '';
PRINT 'ENCRYPTION FUNCTIONS:';
PRINT '  - EncryptXmlWithPassword: Encrypts XML using a password (legacy).';
PRINT '  - EncryptXmlWithPasswordIterations: Encrypts XML with custom iteration count (legacy).';
PRINT '  - EncryptTableWithMetadata: Encrypts table data with full schema metadata embedded.';
PRINT '  - EncryptTableWithMetadataIterations: Encrypts table data with metadata and custom iterations.';
PRINT '  - EncryptXmlWithMetadata: Encrypts XML data with inferred schema metadata.';
PRINT '  - EncryptXmlWithMetadataIterations: Encrypts XML data with metadata and custom iterations.';
PRINT '';
PRINT 'DECRYPTION PROCEDURES:';
PRINT '  - RestoreEncryptedTable: Universal procedure to decrypt and restore any table.';
PRINT '    Supports stored procedure result sets and can be used with INSERT INTO ... EXEC.';
PRINT '  - WrapDecryptProcedure: Dynamic temp-table wrapper that automatically discovers result set structure.';
PRINT '    Eliminates the need for manual column declarations.';
PRINT '  - WrapDecryptProcedureAdvanced: Enhanced wrapper with detailed metadata and custom temp table names.';
PRINT '';
PRINT 'KEY BENEFITS:';
PRINT '  ✓ AUTOMATIC TYPE CASTING: No manual casting required - columns are properly typed';
PRINT '  ✓ SELF-DESCRIBING: Schema metadata travels inside encrypted package';
PRINT '  ✓ FULL TYPE SUPPORT: All SQL Server data types with robust fallback';
PRINT '  ✓ UNIVERSAL COMPATIBILITY: Works with any table design';
PRINT '  ✓ STORED PROCEDURE FRIENDLY: Can handle result sets from other procedures';
PRINT '  ✓ ZERO MANUAL COLUMN DECLARATIONS: Dynamic temp table creation eliminates 40-50 column declarations';
PRINT '';
PRINT 'USAGE EXAMPLES:';
PRINT '';
PRINT '-- Enhanced 3-line encryption with metadata:';
PRINT 'DECLARE @encrypted NVARCHAR(MAX) = dbo.EncryptTableWithMetadata(''MyTable'', ''MyPassword'');';
PRINT '';
PRINT '-- OLD APPROACH (requires manual column declarations):';
PRINT 'CREATE TABLE #TempRestore (Col1 NVARCHAR(MAX), Col2 NVARCHAR(MAX), ...);';
PRINT 'INSERT INTO #TempRestore EXEC dbo.RestoreEncryptedTable @encrypted, ''MyPassword'';';
PRINT 'SELECT * FROM #TempRestore;';
PRINT '';
PRINT '-- NEW APPROACH (automatic temp table creation):';
PRINT 'EXEC dbo.WrapDecryptProcedure ''dbo.RestoreEncryptedTable'', ''@encryptedData=''''@encrypted'''', @password=''''MyPassword'''''';';
PRINT '';
PRINT '-- Advanced approach with custom temp table name:';
PRINT 'EXEC dbo.WrapDecryptProcedureAdvanced ''dbo.RestoreEncryptedTable'', ''@encryptedData=''''@encrypted'''', @password=''''MyPassword''''''', ''#MyCustomTable'';';
PRINT '';
PRINT '-- Handling stored procedure result sets:';
PRINT 'CREATE TABLE #SPResults (Col1 NVARCHAR(MAX), Col2 NVARCHAR(MAX), ...);';
PRINT 'INSERT INTO #SPResults EXEC SomeOtherProcedure @param1, @param2;';
PRINT 'DECLARE @encryptedSP NVARCHAR(MAX) = dbo.EncryptXmlWithMetadata((SELECT * FROM #SPResults FOR XML PATH(''Row''), ROOT(''Root'')), ''MyPassword'');';
PRINT '-- Later restore: EXEC dbo.WrapDecryptProcedure ''dbo.RestoreEncryptedTable'', ''@encryptedData=''''@encryptedSP'''', @password=''''MyPassword'''''';';
PRINT '';
PRINT 'Next: Run demonstration scripts to see the enhanced capabilities!';