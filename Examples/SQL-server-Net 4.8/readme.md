### Step by Step

### Notice
** MS SQL Server Required to Import .NET Assemebly, dll file must signed **

1. create a strong name key file
```bash
sn -k SqlCLRCrypto.snk
```

2. add assembly attributes
```Csharp
using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("SqlCLRCrypto")]
[assembly: AssemblyDescription("SQL CLR Cryptography Functions")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("SqlCLRCrypto")]
[assembly: AssemblyCopyright("Copyright © 2025")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: Guid("new-guid-here")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
```

3. Deploy to SQL Server
```SQL
-- Enable CLR integration
sp_configure 'clr enabled', 1
GO
RECONFIGURE
GO

-- Create assembly from the DLL
CREATE ASSEMBLY SqlCLRCrypto
FROM 'C:\Path\To\Your\SqlCLRCrypto.dll'
WITH PERMISSION_SET = UNSAFE
GO

-- Create SQL functions
CREATE FUNCTION GenerateAESKey()
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlCLRCrypto.SqlCLRCrypting.GenerateAESKey
GO

CREATE FUNCTION EncryptAES
(
    @plainText NVARCHAR(MAX),
    @base64Key NVARCHAR(MAX)
)
RETURNS TABLE
(
    CipherText NVARCHAR(MAX),
    IV NVARCHAR(MAX)
)
AS EXTERNAL NAME SqlCLRCrypto.SqlCLRCrypting.EncryptAES
GO

CREATE FUNCTION DecryptAES
(
    @base64CipherText NVARCHAR(MAX),
    @base64Key NVARCHAR(MAX),
    @base64IV NVARCHAR(MAX)
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlCLRCrypto.SqlCLRCrypting.DecryptAES
GO

CREATE FUNCTION HashPassword
(
    @password NVARCHAR(MAX)
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlCLRCrypto.SqlCLRCrypting.HashPassword
GO

CREATE FUNCTION VerifyPassword
(
    @password NVARCHAR(MAX),
    @hashedPassword NVARCHAR(MAX)
)
RETURNS BIT
AS EXTERNAL NAME SqlCLRCrypto.SqlCLRCrypting.VerifyPassword
GO
```
