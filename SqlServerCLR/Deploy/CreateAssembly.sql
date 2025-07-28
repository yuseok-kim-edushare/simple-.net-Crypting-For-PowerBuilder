-- Create SQL Server CLR Assembly for Password-Based Table Encryption
-- This script deploys the SecureLibrary-SQL.dll.
-- NOTE: PERMISSION_SET is set to UNSAFE because the RestoreEncryptedTable
-- procedure dynamically generates and executes SQL. This is a requirement.

USE [YourDatabase]
GO

-- Drop existing objects if they exist to ensure a clean install
IF OBJECT_ID('dbo.RestoreEncryptedTable', 'P') IS NOT NULL
    DROP PROCEDURE dbo.RestoreEncryptedTable;
GO
IF OBJECT_ID('dbo.EncryptXmlWithPassword', 'FN') IS NOT NULL
    DROP FUNCTION dbo.EncryptXmlWithPassword;
GO
IF OBJECT_ID('dbo.EncryptXmlWithPasswordIterations', 'FN') IS NOT NULL
    DROP FUNCTION dbo.EncryptXmlWithPasswordIterations;
GO

-- Drop existing assembly if it exists
IF EXISTS (SELECT * FROM sys.assemblies WHERE name = 'SimpleDotNetCrypting')
    DROP ASSEMBLY SimpleDotNetCrypting;
GO

-- Enable CLR integration (required for CLR assemblies)
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;
GO

-- Create the CLR assembly from the DLL with UNSAFE permissions
-- This is required for the dynamic SQL execution in the restore procedure.
CREATE ASSEMBLY SimpleDotNetCrypting
FROM '[PATH]/SecureLibrary-SQL.dll'
WITH PERMISSION_SET = UNSAFE;
GO

-- Verify assembly was created
SELECT 
    a.name AS AssemblyName,
    a.permission_set_desc AS PermissionSet,
    a.create_date AS CreateDate
FROM sys.assemblies a
WHERE a.name = 'SimpleDotNetCrypting';
GO

PRINT 'Assembly SimpleDotNetCrypting created successfully with UNSAFE permission set!';
PRINT 'Next: Run CreateFunctions.sql to create the CLR objects.';