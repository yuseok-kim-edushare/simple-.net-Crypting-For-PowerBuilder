-- You need to be sysadmin to execute this
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;
go

-- Be Careful, CLR Assembly need to registered trusted assembly, thus I use ILMerge to create dll

DECLARE @hash varbinary(64) = (SELECT HASHBYTES('SHA2_512', BulkColumn) 
                              FROM OPENROWSET(BULK 'G:\DBMS\SecureLibrary-SQL.dll', SINGLE_BLOB) AS x);
EXEC sys.sp_add_trusted_assembly @hash = @hash, @description = N'SecureLibrary-SQL Assembly';
GO

CREATE ASSEMBLY [SecureLibrary-SQL]
FROM 'G:\DBMS\SecureLibrary-SQL.dll'
WITH PERMISSION_SET = UNSAFE;
go
-- Create SQL Server Functions for each CLR method
CREATE or ALTER FUNCTION dbo.GenerateAESKey()
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].GenerateAESKey;
GO

CREATE or ALTER FUNCTION dbo.EncryptAES(
    @plainText nvarchar(max), 
    @base64Key nvarchar(max))
RETURNS TABLE (
    CipherText nvarchar(max), 
    IV nvarchar(max)
)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].EncryptAES;
GO

CREATE or ALTER FUNCTION dbo.DecryptAES(
    @base64CipherText nvarchar(max), 
    @base64Key nvarchar(max), 
    @base64IV nvarchar(max))
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].DecryptAES;
GO

CREATE or ALTER FUNCTION dbo.GenerateDiffieHellmanKeys()
RETURNS TABLE (
    PublicKey nvarchar(max), 
    PrivateKey nvarchar(max)
)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].GenerateDiffieHellmanKeys;
GO

CREATE or ALTER FUNCTION dbo.DeriveSharedKey(
    @otherPartyPublicKeyBase64 nvarchar(max), 
    @privateKeyBase64 nvarchar(max))
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].DeriveSharedKey;
GO

CREATE or ALTER FUNCTION dbo.HashPassword(@password nvarchar(max))
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].HashPassword;
GO

CREATE or ALTER FUNCTION dbo.VerifyPassword(
    @password nvarchar(max), 
    @hashedPassword nvarchar(max))
RETURNS bit
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].VerifyPassword;
GO

CREATE or ALTER FUNCTION dbo.EncryptAesGcm(
    @plainText nvarchar(max), 
    @base64Key nvarchar(max))
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].EncryptAesGcm;
GO

CREATE or ALTER FUNCTION dbo.DecryptAesGcm(
    @combinedData nvarchar(max), 
    @base64Key nvarchar(max))
RETURNS nvarchar(max)
AS EXTERNAL NAME [SecureLibrary-SQL].[SecureLibrary.SQL.SqlCLRCrypting].DecryptAesGcm;
GO

