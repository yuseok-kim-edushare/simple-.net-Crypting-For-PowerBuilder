# Quick Reference Guide - SecureLibrary-SQL

## Installation

### One-Command Installation
```sql
-- 1. Update these values in install-complete.sql
DECLARE @target_db NVARCHAR(128) = N'YourDatabase';
DECLARE @dll_path NVARCHAR(260) = N'C:\CLR\SecureLibrary-SQL.dll';

-- 2. Run the complete installation
-- Execute: install-complete.sql
```

### One-Command Uninstall
```sql
-- 1. Update database name in uninstall-complete.sql
DECLARE @target_db NVARCHAR(128) = N'YourDatabase';

-- 2. Run the complete uninstall
-- Execute: uninstall-complete.sql
```

## Core Functions

### Basic Encryption (Recommended)
```sql
-- Generate key
DECLARE @key NVARCHAR(MAX) = dbo.GenerateAESKey();

-- Encrypt/Decrypt with AES-GCM (recommended)
DECLARE @encrypted NVARCHAR(MAX) = dbo.EncryptAesGcm('data', @key);
DECLARE @decrypted NVARCHAR(MAX) = dbo.DecryptAesGcm(@encrypted, @key);
```

### Password-Based Encryption
```sql
-- Encrypt with password
DECLARE @encrypted NVARCHAR(MAX) = dbo.EncryptAesGcmWithPassword('data', 'password');

-- Decrypt with password
DECLARE @decrypted NVARCHAR(MAX) = dbo.DecryptAesGcmWithPassword(@encrypted, 'password');
```

### Table Encryption
```sql
-- Encrypt entire table
DECLARE @encrypted NVARCHAR(MAX) = dbo.EncryptTableWithMetadata('TableName', 'password');

-- Decrypt table
EXEC dbo.RestoreEncryptedTable @encrypted, 'password';
```

### Password Hashing
```sql
-- Hash password
DECLARE @hash NVARCHAR(MAX) = dbo.HashPasswordDefault('password');

-- Verify password
DECLARE @isValid BIT = dbo.VerifyPassword('password', @hash);
```

### Key Exchange
```sql
-- Generate key pair (returns table with publicKey, privateKey)
SELECT * FROM dbo.GenerateDiffieHellmanKeys();

-- Derive shared key
DECLARE @sharedKey NVARCHAR(MAX) = dbo.DeriveSharedKey(@publicKey, @privateKey);
```

### Legacy AES Encryption (Deprecated)
```sql
-- Encrypt with AES-CBC (returns table with cipherText, iv)
SELECT * FROM dbo.EncryptAES('data', @key);

-- Decrypt with AES-CBC
DECLARE @decrypted NVARCHAR(MAX) = dbo.DecryptAES(@cipherText, @key, @iv);
```

## Advanced Patterns

### Performance Optimization
```sql
-- Derive key once, use multiple times
DECLARE @salt NVARCHAR(MAX) = dbo.GenerateSalt();
DECLARE @derivedKey NVARCHAR(MAX) = dbo.DeriveKeyFromPassword('password', @salt);

-- Use derived key for multiple operations
DECLARE @encrypted1 NVARCHAR(MAX) = dbo.EncryptAesGcmWithDerivedKey('data1', @derivedKey, @salt);
DECLARE @encrypted2 NVARCHAR(MAX) = dbo.EncryptAesGcmWithDerivedKey('data2', @derivedKey, @salt);
```

### Dynamic Temp Tables
```sql
-- Automatic temp table creation
EXEC dbo.WrapDecryptProcedure 'dbo.RestoreEncryptedTable', '@encryptedData=''@encrypted'', @password=''password''';
```

### XML Encryption
```sql
-- Encrypt XML with metadata
DECLARE @xmlData XML = (SELECT * FROM MyTable FOR XML PATH('Row'), ROOT('Root'));
DECLARE @encrypted NVARCHAR(MAX) = dbo.EncryptXmlWithMetadata(@xmlData, 'password');

-- Decrypt using stored procedure
EXEC dbo.RestoreEncryptedTable @encrypted, 'password';
```

## Korean PowerBuilder Integration

### Session Data Encryption
```sql
-- Encrypt PowerBuilder session data
CREATE TABLE PowerBuilderSession (
    SessionID NVARCHAR(50),
    UserData NVARCHAR(MAX),
    LastAccess DATETIME2
);

-- Encrypt session data
DECLARE @encrypted NVARCHAR(MAX) = dbo.EncryptTableWithMetadata('PowerBuilderSession', 'sessionPassword');

-- Decrypt for PowerBuilder use
EXEC dbo.RestoreEncryptedTable @encrypted, 'sessionPassword';
```

### Unicode Support
```sql
-- Korean characters fully supported
DECLARE @koreanText NVARCHAR(MAX) = '안녕하세요 세계';
DECLARE @encrypted NVARCHAR(MAX) = dbo.EncryptAesGcmWithPassword(@koreanText, 'password');
DECLARE @decrypted NVARCHAR(MAX) = dbo.DecryptAesGcmWithPassword(@encrypted, 'password');
-- Result: 안녕하세요 세계
```

## Error Handling

### Common Patterns
```sql
-- Check for NULL results
DECLARE @result NVARCHAR(MAX) = dbo.DecryptAesGcmWithPassword(@encrypted, 'password');
IF @result IS NULL
BEGIN
    PRINT 'Decryption failed - check password or data integrity';
END

-- Try-catch for table operations
BEGIN TRY
    EXEC dbo.RestoreEncryptedTable @encrypted, 'password';
END TRY
BEGIN CATCH
    PRINT 'Error: ' + ERROR_MESSAGE();
END CATCH
```

## Performance Tips

### Optimization Strategies
1. **Use derived keys** for multiple operations (5x faster)
2. **Cache derived keys** when possible
3. **Use table-level encryption** for bulk data
4. **Use XML encryption** for smaller datasets
5. **Batch operations** when processing multiple items

### Memory Management
```sql
-- Clear sensitive variables
DECLARE @key NVARCHAR(MAX) = dbo.GenerateAESKey();
-- Use key for operations
-- Clear when done
SET @key = NULL;
```

## Deprecated Functions

### Legacy AES-CBC Functions
The following functions are deprecated and should not be used for new development:
- `EncryptAES` / `DecryptAES` - Use `EncryptAesGcm` / `DecryptAesGcm` instead
- `EncryptXmlWithPassword` / `EncryptXmlWithPasswordIterations` - Use `EncryptXmlWithMetadata` instead

**Reason for deprecation:** AES-CBC without authentication is vulnerable to various attacks. AES-GCM provides both encryption and authentication.

## Security Best Practices

### Password Guidelines
- Use 12+ characters
- Mix uppercase, lowercase, numbers, symbols
- Avoid common patterns
- Use unique passwords per application

### Key Management
- Generate unique salts per user/purpose
- Use appropriate iteration counts (2000+ for PBKDF2)
- Store encrypted data securely
- Never store passwords in plain text

### Data Protection
- Encrypt sensitive data at rest
- Use authenticated encryption (AES-GCM)
- Implement proper access controls
- Regular security audits

## Troubleshooting

### Common Issues
| Issue | Solution |
|-------|----------|
| CLR not enabled | Script handles automatically |
| Assembly not trusted | Script handles automatically |
| Permission denied | Ensure UNSAFE permission set |
| DLL not found | Update DLL path in script |
| Function not found | Run complete installation |

### Verification Commands
```sql
-- Check assembly
SELECT * FROM sys.assemblies WHERE name = 'SimpleDotNetCrypting';

-- Check functions
SELECT name, type_desc FROM sys.objects 
WHERE name LIKE '%Encrypt%' OR name LIKE '%Decrypt%' OR name LIKE '%Hash%' OR name LIKE '%Generate%' OR name LIKE '%Derive%'
ORDER BY name;

-- Check procedures
SELECT name, type_desc FROM sys.objects 
WHERE name LIKE '%Restore%' OR name LIKE '%Wrap%'
ORDER BY name;

-- Check table-valued functions
SELECT name, type_desc FROM sys.objects 
WHERE type = 'TF' AND name IN ('EncryptAES', 'GenerateDiffieHellmanKeys')
ORDER BY name;
```

## File Structure

```
Examples/SQL-server-Net 4.8/
├── install-complete.sql      # Complete installation
├── uninstall-complete.sql    # Complete uninstall
├── examples-complete.sql     # Comprehensive examples
├── README.md                 # Detailed documentation
├── QUICK_REFERENCE.md        # This file
├── install.sql              # Legacy installation
├── uninstall.sql            # Legacy uninstall
├── example.sql              # Legacy examples
└── practical-examples.sql   # Legacy practical examples
```

## Migration Guide

### From Legacy Scripts
1. **Backup** encrypted data
2. **Uninstall** legacy: `uninstall-complete.sql`
3. **Install** new: `install-complete.sql`
4. **Test**: `examples-complete.sql`
5. **Verify** existing data works

### Feature Mapping
| Legacy | New |
|--------|-----|
| `install.sql` | `install-complete.sql` |
| `example.sql` | `examples-complete.sql` |
| `uninstall.sql` | `uninstall-complete.sql` |

Perfect for Korean PowerBuilder applications! 🇰🇷 