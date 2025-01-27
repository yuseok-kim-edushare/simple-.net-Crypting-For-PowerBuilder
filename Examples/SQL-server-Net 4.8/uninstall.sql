-- You need to be sysadmin to execute this

-- Drop all functions first
DROP FUNCTION IF EXISTS dbo.GenerateAESKey;
DROP FUNCTION IF EXISTS dbo.EncryptAES;
DROP FUNCTION IF EXISTS dbo.DecryptAES;
DROP FUNCTION IF EXISTS dbo.GenerateDiffieHellmanKeys;
DROP FUNCTION IF EXISTS dbo.DeriveSharedKey;
DROP FUNCTION IF EXISTS dbo.HashPassword;
DROP FUNCTION IF EXISTS dbo.VerifyPassword;
DROP FUNCTION IF EXISTS dbo.EncryptAesGcm;
DROP FUNCTION IF EXISTS dbo.DecryptAesGcm;
GO

-- Drop the assembly
DROP ASSEMBLY IF EXISTS [SecureLibrary-SQL];
GO

-- Remove from trusted assemblies
DECLARE @hash varbinary(64) = (SELECT HASHBYTES('SHA2_512', BulkColumn) 
                              FROM OPENROWSET(BULK 'G:\DBMS\SecureLibrary-SQL.dll', SINGLE_BLOB) AS x);
EXEC sys.sp_drop_trusted_assembly @hash = @hash;
GO

