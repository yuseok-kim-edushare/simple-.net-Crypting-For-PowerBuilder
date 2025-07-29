# SQL Server CLR Encryption Library

This directory contains SQL Server CLR integration examples for the .NET encryption library, specifically designed for PowerBuilder integration.

## Quick Start

### 1. Installation
```sql
-- Run the complete installation script
EXEC install-complete.sql
```

### 2. Test Installation
```sql
-- Run the simple test script
EXEC simple-restore.sql
```

### 3. Debug Issues
```sql
-- If you encounter problems, run the debug script
EXEC debug-restore.sql
```

## Features

### Password-Based Table Encryption
- **EncryptTableWithMetadata**: Encrypt entire tables with embedded schema metadata
- **RestoreEncryptedTable**: Decrypt and restore tables with automatic type casting
- **WrapDecryptProcedure**: Dynamic temp table wrapper for automatic column discovery

### XML Encryption
- **EncryptXmlWithMetadata**: Encrypt XML data with inferred schema information
- **EncryptXmlWithPassword**: Legacy XML encryption (backward compatibility)

### Row-by-Row Encryption
- **EncryptRowDataAesGcm**: Encrypt individual JSON rows
- **DecryptRowDataAesGcm**: Decrypt individual JSON rows
- **EncryptTableRowsAesGcm**: Bulk row encryption TVF

### Core Encryption Functions
- **AES-GCM**: Authenticated encryption with Galois/Counter Mode
- **Password-based key derivation**: PBKDF2 with configurable iterations
- **Salt generation**: Cryptographically secure random salts
- **Key derivation**: Diffie-Hellman key exchange support

## PowerBuilder Integration Examples

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

## Troubleshooting

### RestoreEncryptedTable Issues

If you encounter issues with `RestoreEncryptedTable` returning NULL values:

1. **Check Encryption Method**: Ensure you're using `EncryptTableWithMetadata` or `EncryptXmlWithMetadata` for encryption
2. **Verify Password**: Make sure the same password is used for encryption and decryption
3. **Check Iterations**: The default iteration count is 2000; ensure both encryption and decryption use the same count
4. **Run Debug Script**: Use `debug-restore.sql` to get detailed error information

### Common Error Messages

- **"Decryption returned null"**: Password mismatch or corrupted data
- **"No columns found"**: XML structure issue or metadata parsing problem
- **"Error in RestoreEncryptedTable"**: Check the detailed error message and stack trace

### Debugging Steps

1. Run `debug-restore.sql` to get comprehensive diagnostic information
2. Check if all required functions are installed correctly
3. Verify the assembly permissions (UNSAFE required)
4. Test with simple data first before using complex tables

## Installation Notes

### Requirements
- SQL Server 2016 or later
- CLR Integration enabled
- UNSAFE permission set for the assembly
- .NET Framework 4.8 runtime

### Security Considerations
- The assembly requires UNSAFE permission due to native cryptography calls
- Store passwords securely and never hardcode them
- Use strong passwords for encryption operations
- Consider using derived keys for performance-critical operations

### Performance Tips
- Use derived keys for multiple operations on the same data
- Consider iteration count based on security requirements vs performance
- Use the dynamic temp table wrapper for automatic column discovery

## File Structure

- `install-complete.sql`: Complete installation script
- `install.sql`: Basic installation script
- `uninstall.sql`: Cleanup script
- `simple-restore.sql`: Basic functionality test
- `debug-restore.sql`: Comprehensive debugging script
- `examples-complete.sql`: Full feature demonstration
- `practical-examples.sql`: Real-world usage examples
- `QUICK_REFERENCE.md`: Quick reference guide

## Recent Fixes

### RestoreEncryptedTable NULL Issue (Latest)
- **Problem**: `RestoreEncryptedTable` was returning NULL values for decrypted data
- **Root Cause**: Error handling was throwing exceptions instead of providing user-friendly messages
- **Solution**: Enhanced error handling with detailed error messages and better debugging information
- **Files Updated**: 
  - `net481SQL-server/draft.cs` (RestoreEncryptedTable method)
  - `Examples/SQL-server-Net 4.8/simple-restore.sql` (enhanced debugging)
  - `Examples/SQL-server-Net 4.8/debug-restore.sql` (comprehensive diagnostics)

The fix ensures that:
1. Decryption errors are caught and reported clearly
2. XML parsing issues are identified and reported
3. Column discovery problems are diagnosed
4. Users get actionable error messages instead of silent failures

## Support

For issues or questions:
1. Run the debug scripts first
2. Check the error messages for specific guidance
3. Verify your SQL Server version and CLR configuration
4. Test with the provided example scripts