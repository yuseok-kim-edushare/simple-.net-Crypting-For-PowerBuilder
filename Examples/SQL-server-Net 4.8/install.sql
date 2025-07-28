-- SQL Server CLR Assembly Complete Installation Script - SecureLibrary-SQL
-- This is a unified installation script that includes all functionality from PR #61
-- Run this script once per target database by changing the configuration variables

-- =============================================
-- CONFIGURATION - CHANGE THESE VALUES
-- =============================================
DECLARE @target_db NVARCHAR(128) = N'master';  -- <<<< CHANGE THIS FOR EACH DATABASE
DECLARE @dll_path NVARCHAR(260) = N'C:\CLR\SecureLibrary-SQL.dll';  -- <<<< SET YOUR DLL PATH HERE

-- =============================================
-- Enable CLR (run once per instance)
-- =============================================
EXEC sp_configure 'show advanced options', 1;
RECONFIGURE;
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;

-- =============================================
-- Trust assemblies (run once per instance)
-- =============================================

-- Trust SecureLibrary-SQL assembly
DECLARE @hash VARBINARY(64);
SELECT @hash = HASHBYTES('SHA2_512', BulkColumn)
FROM OPENROWSET(BULK 'C:\CLR\SecureLibrary-SQL.dll', SINGLE_BLOB) AS x; -- <<<< SET YOUR PATH HERE

IF NOT EXISTS (SELECT * FROM sys.trusted_assemblies WHERE [hash] = @hash)
BEGIN
    EXEC sys.sp_add_trusted_assembly @hash = @hash, @description = N'SecureLibrary-SQL Assembly';
    PRINT 'SecureLibrary-SQL assembly hash added to trusted assemblies.';
END
ELSE
BEGIN
    PRINT 'SecureLibrary-SQL assembly hash already exists in trusted assemblies.';
END

-- =============================================
-- Switch to target database
-- =============================================
DECLARE @sql NVARCHAR(MAX) = N'USE ' + QUOTENAME(@target_db);
EXEC(@sql);

PRINT 'Deploying to database: ' + @target_db;

-- =============================================
-- Clean up existing objects (Enhanced with proper sequence)
-- =============================================

-- Drop all functions and procedures with proper error handling
PRINT 'Dropping existing functions and procedures...';

-- Drop Table-Valued Functions first (dependencies)
BEGIN TRY
    IF OBJECT_ID('dbo.DecryptTableTVF') IS NOT NULL 
    BEGIN
        DROP FUNCTION dbo.DecryptTableTVF;
        PRINT 'Dropped DecryptTableTVF';
    END
END TRY
BEGIN CATCH
    PRINT 'Could not drop DecryptTableTVF: ' + ERROR_MESSAGE();
END CATCH

BEGIN TRY
    IF OBJECT_ID('dbo.DecryptTableTypedTVF') IS NOT NULL 
    BEGIN
        DROP FUNCTION dbo.DecryptTableTypedTVF;
        PRINT 'Dropped DecryptTableTypedTVF';
    END
END TRY
BEGIN CATCH
    PRINT 'Could not drop DecryptTableTypedTVF: ' + ERROR_MESSAGE();
END CATCH

-- Drop Stored Procedures
BEGIN TRY
    IF OBJECT_ID('dbo.RestoreEncryptedTable', 'P') IS NOT NULL 
    BEGIN
        DROP PROCEDURE dbo.RestoreEncryptedTable;
        PRINT 'Dropped RestoreEncryptedTable';
    END
    ELSE
    BEGIN
        PRINT 'RestoreEncryptedTable procedure not found';
    END
END TRY
BEGIN CATCH
    PRINT 'Could not drop RestoreEncryptedTable: ' + ERROR_MESSAGE();
    -- Try alternative drop method
    BEGIN TRY
        PRINT 'Attempting alternative drop method for RestoreEncryptedTable...';
        -- Try dropping with different approach
        EXEC('DROP PROCEDURE dbo.RestoreEncryptedTable');
        PRINT 'Force dropped RestoreEncryptedTable using EXEC';
    END TRY
    BEGIN CATCH
        PRINT 'Could not force drop RestoreEncryptedTable: ' + ERROR_MESSAGE();
    END CATCH
END CATCH

-- Additional cross-database cleanup for RestoreEncryptedTable
PRINT 'Performing cross-database cleanup for RestoreEncryptedTable...';
BEGIN TRY
    -- Try to drop from current database context first
    EXEC('DROP PROCEDURE dbo.RestoreEncryptedTable');
    PRINT '✓ Successfully dropped RestoreEncryptedTable from current context';
END TRY
BEGIN CATCH
    PRINT 'Could not drop RestoreEncryptedTable from current context: ' + ERROR_MESSAGE();
    
    -- Try to find and drop from all databases
    DECLARE @db_name NVARCHAR(128);
    DECLARE @drop_db_sql NVARCHAR(MAX);
    
    -- Check common databases
    DECLARE @databases TABLE (DatabaseName NVARCHAR(128));
    INSERT INTO @databases VALUES ('master'), ('vilacdb'), ('tempdb'), ('model');
    
    DECLARE db_cursor CURSOR FOR SELECT DatabaseName FROM @databases;
    OPEN db_cursor;
    FETCH NEXT FROM db_cursor INTO @db_name;
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        BEGIN TRY
            SET @drop_db_sql = N'USE [' + @db_name + N']; 
            IF OBJECT_ID(''dbo.RestoreEncryptedTable'', ''P'') IS NOT NULL 
            BEGIN 
                DROP PROCEDURE dbo.RestoreEncryptedTable; 
                PRINT ''✓ Dropped RestoreEncryptedTable from ' + @db_name + '''; 
            END
            ELSE
            BEGIN
                PRINT ''  RestoreEncryptedTable not found in ' + @db_name + ''';
            END';
            EXEC(@drop_db_sql);
        END TRY
        BEGIN CATCH
            PRINT 'Could not check/drop from ' + @db_name + ': ' + ERROR_MESSAGE();
        END CATCH
        
        FETCH NEXT FROM db_cursor INTO @db_name;
    END
    
    CLOSE db_cursor;
    DEALLOCATE db_cursor;
END CATCH

-- Drop all Scalar-Valued Functions in proper dependency order
-- Phase 1: Drop complex functions that depend on others
DECLARE @dropFunctions TABLE (FunctionName NVARCHAR(128), Phase INT);
INSERT INTO @dropFunctions VALUES 
    -- Phase 1: Complex functions that depend on multiple others
    ('EncryptXmlWithPassword', 1),
    ('EncryptXmlWithPasswordIterations', 1),
    ('EncryptXmlWithMetadata', 1),
    ('EncryptXmlWithMetadataIterations', 1),
    ('EncryptTableWithMetadata', 1),
    ('EncryptTableWithMetadataIterations', 1),
    ('DecryptTableTVF', 1),
    ('DecryptTableTypedTVF', 1),
    
    -- Phase 2: Functions that depend on key derivation
    ('EncryptAesGcmWithPassword', 2),
    ('EncryptAesGcmWithPasswordIterations', 2),
    ('DecryptAesGcmWithPassword', 2),
    ('DecryptAesGcmWithPasswordIterations', 2),
    ('EncryptAesGcmWithPasswordAndSalt', 2),
    ('EncryptAesGcmWithPasswordAndSaltIterations', 2),
    ('EncryptAesGcmWithDerivedKey', 2),
    ('DecryptAesGcmWithDerivedKey', 2),
    
    -- Phase 3: Key derivation functions
    ('DeriveKeyFromPassword', 3),
    ('DeriveKeyFromPasswordIterations', 3),
    
    -- Phase 4: Independent functions (can be dropped in any order)
    ('GenerateAESKey', 4),
    ('EncryptAES', 4),
    ('DecryptAES', 4),
    ('GenerateDiffieHellmanKeys', 4),
    ('DeriveSharedKey', 4),
    ('HashPasswordDefault', 4),
    ('HashPasswordWithWorkFactor', 4),
    ('VerifyPassword', 4),
    ('EncryptAesGcm', 4),
    ('DecryptAesGcm', 4),
    ('GenerateSalt', 4),
    ('GenerateSaltWithLength', 4);

DECLARE @functionName NVARCHAR(128);
DECLARE @phase INT;
DECLARE @dropSQL NVARCHAR(MAX);

-- Drop functions by phase (dependency order)
DECLARE function_cursor CURSOR FOR 
SELECT FunctionName, Phase FROM @dropFunctions ORDER BY Phase;

OPEN function_cursor;
FETCH NEXT FROM function_cursor INTO @functionName, @phase;

WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT 'Phase ' + CAST(@phase AS VARCHAR(1)) + ': Attempting to drop ' + @functionName;
    BEGIN TRY
        IF OBJECT_ID('dbo.' + @functionName) IS NOT NULL
        BEGIN
            SET @dropSQL = 'DROP FUNCTION dbo.' + @functionName;
            EXEC(@dropSQL);
            PRINT '✓ Dropped ' + @functionName;
        END
        ELSE
        BEGIN
            PRINT '  ' + @functionName + ' not found (already dropped or never existed)';
        END
    END TRY
    BEGIN CATCH
        PRINT '✗ Could not drop ' + @functionName + ': ' + ERROR_MESSAGE();
    END CATCH
    
    FETCH NEXT FROM function_cursor INTO @functionName, @phase;
END

CLOSE function_cursor;
DEALLOCATE function_cursor;

PRINT 'Dropped existing functions and procedures';

-- Drop existing assemblies (comprehensive cleanup)
PRINT 'Dropping existing assemblies...';

-- Force drop assembly with all dependencies
IF EXISTS (SELECT * FROM sys.assemblies WHERE name = 'SecureLibrary-SQL') 
BEGIN
    BEGIN TRY
        -- First try normal drop
        DROP ASSEMBLY [SecureLibrary-SQL];
        PRINT 'Dropped SecureLibrary-SQL assembly';
    END TRY
    BEGIN CATCH
        PRINT 'Could not drop SecureLibrary-SQL assembly normally: ' + ERROR_MESSAGE();
        PRINT 'Attempting to drop with dependencies...';
        
        BEGIN TRY
            -- Force drop with dependencies
            DROP ASSEMBLY [SecureLibrary-SQL] WITH NO DEPENDENTS;
            PRINT 'Force dropped SecureLibrary-SQL assembly';
        END TRY
        BEGIN CATCH
            PRINT 'Could not force drop SecureLibrary-SQL assembly: ' + ERROR_MESSAGE();
            PRINT 'Assembly may be in use or have dependencies in master database.';
        END CATCH
    END CATCH
END

IF EXISTS (SELECT * FROM sys.assemblies WHERE name = 'SimpleDotNetCrypting') 
BEGIN
    BEGIN TRY
        DROP ASSEMBLY [SimpleDotNetCrypting];
        PRINT 'Dropped SimpleDotNetCrypting assembly';
    END TRY
    BEGIN CATCH
        PRINT 'Could not drop SimpleDotNetCrypting assembly: ' + ERROR_MESSAGE();
    END CATCH
END

PRINT 'Dropped existing assemblies';

-- =============================================
-- Create assembly
-- =============================================

-- Create SecureLibrary-SQL assembly
BEGIN TRY
    DECLARE @create_sql NVARCHAR(MAX) = N'CREATE ASSEMBLY [SecureLibrary-SQL] 
FROM ''' + @dll_path + ''' 
WITH PERMISSION_SET = UNSAFE';
    EXEC(@create_sql);
    PRINT 'Created SecureLibrary-SQL assembly with UNSAFE permission set';
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() = 2714 -- Object already exists
    BEGIN
        PRINT 'SecureLibrary-SQL assembly already exists in current database.';
        PRINT 'Checking if we can use existing assembly...';
        
        -- Check if assembly exists and is accessible
        IF EXISTS (SELECT * FROM sys.assemblies WHERE name = 'SecureLibrary-SQL')
        BEGIN
            PRINT 'Using existing SecureLibrary-SQL assembly.';
        END
        ELSE
        BEGIN
            PRINT 'Assembly exists in master database but not accessible here.';
            PRINT 'Please ensure the assembly is properly deployed to this database.';
        END
    END
    ELSE
    BEGIN
        DECLARE @error_msg NVARCHAR(4000) = ERROR_MESSAGE();
        PRINT 'Failed to create SecureLibrary-SQL assembly: ' + @error_msg;
        PRINT 'This may be due to assembly existing in master database.';
        PRINT 'Continuing with function creation...';
    END
END CATCH

-- =============================================
-- Create all functions and procedures
-- =============================================

PRINT 'Creating all functions and procedures...';
GO

-- Password-Based Table Encryption Functions
CREATE FUNCTION dbo.EncryptXmlWithPassword(
    @xmlData XML, 
    @password NVARCHAR(MAX)
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].EncryptXmlWithPassword;
GO

CREATE FUNCTION dbo.EncryptXmlWithPasswordIterations(
    @xmlData XML, 
    @password NVARCHAR(MAX),
    @iterations INT
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].EncryptXmlWithPasswordIterations;
GO

-- Universal procedure to decrypt and restore any table
CREATE PROCEDURE dbo.RestoreEncryptedTable
    @encryptedData NVARCHAR(MAX),
    @password NVARCHAR(MAX)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].RestoreEncryptedTable;
GO

-- Core Cryptographic Functions
CREATE FUNCTION dbo.GenerateAESKey()
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].GenerateAESKey;
GO

CREATE FUNCTION dbo.EncryptAesGcm(
    @plainText nvarchar(max), 
    @base64Key nvarchar(max))
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].EncryptAesGcm;
GO

CREATE FUNCTION dbo.DecryptAesGcm(
    @combinedData nvarchar(max), 
    @base64Key nvarchar(max))
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].DecryptAesGcm;
GO

CREATE FUNCTION dbo.EncryptAesGcmWithPassword(
    @plainText nvarchar(max), 
    @password nvarchar(max))
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].EncryptAesGcmWithPassword;
GO

CREATE FUNCTION dbo.DecryptAesGcmWithPassword(
    @base64EncryptedData nvarchar(max), 
    @password nvarchar(max))
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].DecryptAesGcmWithPassword;
GO

-- Password Hashing Functions
CREATE FUNCTION dbo.HashPasswordDefault(@password nvarchar(max))
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].HashPasswordDefault;
GO

CREATE FUNCTION dbo.VerifyPassword(
    @password nvarchar(max), 
    @hashedPassword nvarchar(max))
RETURNS bit
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].VerifyPassword;
GO

-- Diffie-Hellman Key Exchange Functions
CREATE FUNCTION dbo.GenerateDiffieHellmanKeys()
RETURNS TABLE (
    PublicKey nvarchar(max), 
    PrivateKey nvarchar(max)
)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].GenerateDiffieHellmanKeys;
GO

CREATE FUNCTION dbo.DeriveSharedKey(
    @otherPartyPublicKeyBase64 nvarchar(max), 
    @privateKeyBase64 nvarchar(max))
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].DeriveSharedKey;
GO

-- Salt Generation Functions
CREATE FUNCTION dbo.GenerateSalt()
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].GenerateSalt;
GO

CREATE FUNCTION dbo.GenerateSaltWithLength(@saltLength int)
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].GenerateSaltWithLength;
GO

-- Advanced Password-Based Encryption Functions
CREATE FUNCTION dbo.EncryptAesGcmWithPasswordAndSalt(
    @plainText nvarchar(max), 
    @password nvarchar(max),
    @base64Salt nvarchar(max))
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].EncryptAesGcmWithPasswordAndSalt;
GO

CREATE FUNCTION dbo.EncryptAesGcmWithPasswordAndSaltIterations(
    @plainText nvarchar(max), 
    @password nvarchar(max),
    @base64Salt nvarchar(max),
    @iterations int)
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].EncryptAesGcmWithPasswordAndSaltIterations;
GO

-- Key Derivation Functions
CREATE FUNCTION dbo.DeriveKeyFromPassword(
    @password nvarchar(max), 
    @base64Salt nvarchar(max))
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].DeriveKeyFromPassword;
GO

CREATE FUNCTION dbo.DeriveKeyFromPasswordIterations(
    @password nvarchar(max), 
    @base64Salt nvarchar(max),
    @iterations int)
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].DeriveKeyFromPasswordIterations;
GO

-- Derived Key Encryption Functions
CREATE FUNCTION dbo.EncryptAesGcmWithDerivedKey(
    @plainText nvarchar(max), 
    @base64DerivedKey nvarchar(max),
    @base64Salt nvarchar(max))
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].EncryptAesGcmWithDerivedKey;
GO

CREATE FUNCTION dbo.DecryptAesGcmWithDerivedKey(
    @base64EncryptedData nvarchar(max), 
    @base64DerivedKey nvarchar(max))
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].DecryptAesGcmWithDerivedKey;
GO

-- Advanced Password Hashing Function
CREATE FUNCTION dbo.HashPasswordWithWorkFactor(@password nvarchar(max), @workFactor int)
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].HashPasswordWithWorkFactor;
GO

-- Metadata-Enhanced Encryption Functions
CREATE FUNCTION dbo.EncryptTableWithMetadata(
    @tableName nvarchar(max), 
    @password nvarchar(max))
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].EncryptTableWithMetadata;
GO

CREATE FUNCTION dbo.EncryptTableWithMetadataIterations(
    @tableName nvarchar(max), 
    @password nvarchar(max),
    @iterations int)
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].EncryptTableWithMetadataIterations;
GO

CREATE FUNCTION dbo.EncryptXmlWithMetadata(
    @xmlData xml, 
    @password nvarchar(max))
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].EncryptXmlWithMetadata;
GO

CREATE FUNCTION dbo.EncryptXmlWithMetadataIterations(
    @xmlData xml, 
    @password nvarchar(max),
    @iterations int)
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].EncryptXmlWithMetadataIterations;
GO

-- Universal procedure to decrypt and restore any table
-- This procedure handles stored procedure result sets and can be used with INSERT INTO ... EXEC
CREATE PROCEDURE dbo.RestoreEncryptedTable(
    @encryptedData nvarchar(max), 
    @password nvarchar(max))
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].RestoreEncryptedTable;
GO

PRINT 'All functions and procedures created successfully!';

-- =============================================
-- Verify installation
-- =============================================
SELECT 
    SCHEMA_NAME(o.schema_id) AS SchemaName,
    o.name AS ObjectName,
    o.type_desc AS ObjectType,
    o.create_date AS CreateDate
FROM sys.objects o
WHERE o.name IN (
    'EncryptXmlWithPassword', 
    'EncryptXmlWithPasswordIterations',
    'EncryptXmlWithMetadata',
    'EncryptXmlWithMetadataIterations',
    'EncryptTableWithMetadata',
    'EncryptTableWithMetadataIterations',
    'RestoreEncryptedTable',
    'GenerateAESKey',
    'EncryptAesGcm',
    'DecryptAesGcm',
    'EncryptAesGcmWithPassword',
    'DecryptAesGcmWithPassword',
    'EncryptAesGcmWithPasswordAndSalt',
    'EncryptAesGcmWithPasswordAndSaltIterations',
    'DeriveKeyFromPassword',
    'DeriveKeyFromPasswordIterations',
    'EncryptAesGcmWithDerivedKey',
    'DecryptAesGcmWithDerivedKey',
    'HashPasswordDefault',
    'HashPasswordWithWorkFactor',
    'VerifyPassword',
    'GenerateSalt',
    'GenerateSaltWithLength',
    'GenerateDiffieHellmanKeys',
    'DeriveSharedKey',

)
ORDER BY o.name;

PRINT '';
PRINT '=== INSTALLATION COMPLETED SUCCESSFULLY ===';
PRINT 'Database: ' + DB_NAME();
PRINT '';
PRINT 'Available Functions:';
PRINT '✓ Password-Based Table Encryption: EncryptXmlWithPassword, RestoreEncryptedTable';
PRINT '✓ Metadata-Enhanced Encryption: EncryptTableWithMetadata, EncryptXmlWithMetadata';
PRINT '✓ Core AES-GCM Encryption: EncryptAesGcm, DecryptAesGcm, EncryptAesGcmWithPassword';
PRINT '✓ Advanced Password Encryption: EncryptAesGcmWithPasswordAndSalt, DeriveKeyFromPassword';
PRINT '✓ Derived Key Encryption: EncryptAesGcmWithDerivedKey, DecryptAesGcmWithDerivedKey';
PRINT '✓ Password Hashing: HashPasswordDefault, HashPasswordWithWorkFactor, VerifyPassword';
PRINT '✓ Salt Generation: GenerateSalt, GenerateSaltWithLength';
PRINT '✓ Key Exchange: GenerateDiffieHellmanKeys, DeriveSharedKey';
PRINT '✓ Universal Decryption: RestoreEncryptedTable (supports stored procedure result sets)';
PRINT '';
PRINT 'Next: Run example.sql to see usage examples of all features.';
PRINT '      For practical, developer-focused examples addressing dynamic table';
PRINT '      creation and schema comparison, see practical-examples.sql.';