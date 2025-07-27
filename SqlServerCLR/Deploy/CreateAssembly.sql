-- Create SQL Server CLR Assembly for Row-by-Row Encryption
-- This script deploys the enhanced SecureLibrary-SQL.dll with row-by-row encryption capabilities

USE [YourDatabase]
GO

-- Drop existing functions if they exist
IF OBJECT_ID('dbo.EncryptTableRowsAesGcm', 'TF') IS NOT NULL
    DROP FUNCTION dbo.EncryptTableRowsAesGcm;
GO

IF OBJECT_ID('dbo.EncryptRowDataAesGcm', 'FN') IS NOT NULL
    DROP FUNCTION dbo.EncryptRowDataAesGcm;
GO

IF OBJECT_ID('dbo.DecryptRowDataAesGcm', 'FN') IS NOT NULL
    DROP FUNCTION dbo.DecryptRowDataAesGcm;
GO

IF OBJECT_ID('dbo.BulkProcessRowsAesGcm', 'PC') IS NOT NULL
    DROP PROCEDURE dbo.BulkProcessRowsAesGcm;
GO

-- Drop existing assembly if it exists
IF EXISTS (SELECT * FROM sys.assemblies WHERE name = 'SimpleDotNetCrypting')
    DROP ASSEMBLY SimpleDotNetCrypting;
GO

-- Enable CLR integration (required for CLR assemblies)
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;
GO

-- Create the CLR assembly from the DLL
-- Note: Replace [PATH] with the actual path to your SecureLibrary-SQL.dll
CREATE ASSEMBLY SimpleDotNetCrypting
FROM '[PATH]/SecureLibrary-SQL.dll'
WITH PERMISSION_SET = SAFE;
GO

-- Verify assembly was created
SELECT 
    a.name AS AssemblyName,
    a.permission_set_desc AS PermissionSet,
    a.create_date AS CreateDate,
    a.modify_date AS ModifyDate
FROM sys.assemblies a
WHERE a.name = 'SimpleDotNetCrypting';
GO

PRINT 'Assembly SimpleDotNetCrypting created successfully!';
PRINT 'Next: Run CreateFunctions.sql to create the CLR functions.';