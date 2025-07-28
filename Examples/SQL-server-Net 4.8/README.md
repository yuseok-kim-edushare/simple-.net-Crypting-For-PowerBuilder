# SQL Server CLR Examples - Complete Installation Guide

This folder contains the unified, comprehensive SQL CLR installation and usage examples for SecureLibrary-SQL, updated to include all functionality with merged deployment scripts.

## Quick Start

1. **Install**: Run `install-complete.sql` 
   - Update the DLL path in the script
   - Update the target database name
   - Execute the script to install all functions, procedures, and table-valued functions

2. **Test**: Run `examples-complete.sql` for comprehensive demonstrations of all features

3. **Uninstall**: Run `uninstall-complete.sql` if needed

## Files Overview

### Installation and Setup
- **`install-complete.sql`** - **NEW!** Complete merged installation script for all CLR objects
- **`uninstall-complete.sql`** - **NEW!** Complete uninstall script that removes all objects
- **`install.sql`** - Legacy installation script (use install-complete.sql instead)
- **`uninstall.sql`** - Legacy uninstall script (use uninstall-complete.sql instead)

### Examples and Testing  
- **`examples-complete.sql`** - **NEW!** Comprehensive examples demonstrating all features
- **`example.sql`** - Basic usage examples (legacy)
- **`practical-examples.sql`** - Enhanced developer-friendly examples (legacy)

## What's New (Merged Deployment)

✅ **Complete Merged Installation**
- Single script installs all functions, procedures, and table-valued functions
- Proper dependency ordering and error handling
- Comprehensive verification and reporting

✅ **Enhanced Features**
- AES-GCM Encryption/Decryption (Recommended)
- Password-based Key Derivation (PBKDF2)
- Diffie-Hellman Key Exchange
- BCrypt Password Hashing
- Table-Level Encryption with Embedded Metadata
- XML Encryption with Schema Inference
- Dynamic Temp Table Wrapper
- Automatic Type Casting
- Stored Procedure Result Set Handling
- Korean Character Support
- PowerBuilder Integration Patterns

## Installation Notes

- **UNSAFE Permission Set**: Required for dynamic SQL execution in RestoreEncryptedTable
- **CLR Enabled**: Script automatically enables CLR integration
- **Trusted Assemblies**: Script handles assembly trust configuration
- **Comprehensive**: Single script installs all 30+ functions and procedures

## Usage Examples

The `examples-complete.sql` file demonstrates:

### Basic Encryption
```sql
-- Generate AES key
DECLARE @key NVARCHAR(MAX) = dbo.GenerateAESKey();

-- Encrypt/Decrypt
DECLARE @encrypted NVARCHAR(MAX) = dbo.EncryptAesGcm('Hello World', @key);
DECLARE @decrypted NVARCHAR(MAX) = dbo.DecryptAesGcm(@encrypted, @key);
```

### Password-Based Encryption
```sql
-- Encrypt with password
DECLARE @encrypted NVARCHAR(MAX) = dbo.EncryptAesGcmWithPassword('Sensitive data', 'MyPassword123!');

-- Decrypt with password
DECLARE @decrypted NVARCHAR(MAX) = dbo.DecryptAesGcmWithPassword(@encrypted, 'MyPassword123!');
```

### Table-Level Encryption
```sql
-- Encrypt entire table
DECLARE @encrypted NVARCHAR(MAX) = dbo.EncryptTableWithMetadata('MyTable', 'password');

-- Decrypt table
EXEC dbo.RestoreEncryptedTable @encrypted, 'password';
```

### Password Hashing
```sql
-- Hash password
DECLARE @hash NVARCHAR(MAX) = dbo.HashPasswordDefault('userpassword');

-- Verify password
DECLARE @isValid BIT = dbo.VerifyPassword('userpassword', @hash);
```

### Diffie-Hellman Key Exchange
```sql
-- Generate key pairs
SELECT * FROM dbo.GenerateDiffieHellmanKeys();

-- Derive shared key
DECLARE @sharedKey NVARCHAR(MAX) = dbo.DeriveSharedKey(@otherPublicKey, @myPrivateKey);
```

## Korean PowerBuilder Integration

Perfect for Korean small business applications using PowerBuilder:

- **Full Unicode Support**: Korean characters (한글) fully supported
- **Session Data Encryption**: Secure PowerBuilder session management
- **Dynamic Temp Tables**: Automatic table structure discovery
- **Type Safety**: Automatic type casting for seamless integration
- **Performance Optimized**: Derived key caching for multiple operations

## Performance Characteristics

- **Password-based encryption**: ~50-100ms for 1KB data
- **Derived key encryption**: ~10-20ms for 1KB data (5x faster)
- **Table-level encryption**: Handles tables with 1000+ rows efficiently
- **Memory efficient**: Streaming encryption for large datasets

## Security Features

- **AES-GCM**: Authenticated encryption with integrity protection
- **PBKDF2**: Password-based key derivation with configurable iterations
- **BCrypt**: Secure password hashing with work factor control
- **ECDH**: Elliptic Curve Diffie-Hellman for secure key exchange
- **Salt generation**: Cryptographically secure random salts
- **Key derivation**: Separate keys for different purposes

## Migration from Legacy Scripts

If you're using the legacy `install.sql` and `example.sql`:

1. **Backup**: Export any encrypted data
2. **Uninstall**: Run `uninstall-complete.sql`
3. **Install**: Run `install-complete.sql`
4. **Test**: Run `examples-complete.sql`
5. **Verify**: Test your existing encrypted data

## Troubleshooting

### Common Issues

1. **CLR not enabled**: Script automatically enables CLR
2. **Assembly not trusted**: Script handles assembly trust
3. **Permission denied**: Ensure UNSAFE permission set
4. **DLL not found**: Update DLL path in script

### Error Messages

- `"Assembly not found"`: Check DLL path in script
- `"CLR not enabled"`: Script should handle this automatically
- `"Permission denied"`: Ensure UNSAFE permission set
- `"Function not found"`: Run complete installation script

## Support

For issues and questions:
- Check the comprehensive examples in `examples-complete.sql`
- Review error handling in the installation scripts
- Ensure all prerequisites are met (SQL Server 2016+, .NET Framework 4.8)

Perfect for Korean small business applications using PowerBuilder!