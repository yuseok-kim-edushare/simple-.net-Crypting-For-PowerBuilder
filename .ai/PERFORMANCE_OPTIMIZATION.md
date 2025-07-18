# Performance Optimization for Password-Based Encryption

This document explains the new performance optimization features added to address the performance issue when processing multiple database rows with the same password.

## Problem

When using password-based encryption methods like `EncryptAesGcmWithPassword()` and `DecryptAesGcmWithPassword()` for processing database tables, each operation performs expensive PBKDF2 key derivation. This creates a performance bottleneck when processing many rows with the same password.

## Solution

The library now provides cached key-based methods that allow you to:

1. **Derive a key once** from the password and cache it
2. **Encrypt/decrypt multiple times** using the cached key
3. **Maintain full compatibility** with existing password-based methods

## New Methods

### Key Derivation
```csharp
// Derive a key once and cache it for reuse
string derivedKey = EncryptionHelper.DeriveKeyFromPassword(password, salt, iterations);
```

### Cached Key Encryption/Decryption
```csharp
// Encrypt using cached key (much faster than password-based)
string encrypted = EncryptionHelper.EncryptAesGcmWithDerivedKey(plaintext, derivedKey, salt);

// Decrypt using cached key (much faster than password-based)
string decrypted = EncryptionHelper.DecryptAesGcmWithDerivedKey(encrypted, derivedKey);
```

## Usage Examples

### Before (Slow - Key derivation on every operation)
```csharp
string password = "DatabasePassword123";
string salt = EncryptionHelper.GenerateSalt();

// Each encryption/decryption performs expensive PBKDF2
foreach (var record in databaseRecords)
{
    string encrypted = EncryptionHelper.EncryptAesGcmWithPasswordAndSalt(
        record.Data, password, salt);
    // Save encrypted to database
}
```

### After (Fast - Key derivation only once)
```csharp
string password = "DatabasePassword123"; 
string salt = EncryptionHelper.GenerateSalt();

// Derive key once
string cachedKey = EncryptionHelper.DeriveKeyFromPassword(password, salt);

// Use cached key for all operations
foreach (var record in databaseRecords)
{
    string encrypted = EncryptionHelper.EncryptAesGcmWithDerivedKey(
        record.Data, cachedKey, salt);
    // Save encrypted to database
}
```

### SQL Server CLR Functions

For SQL Server, equivalent functions are available:

```sql
-- Derive key once per batch
DECLARE @CachedKey NVARCHAR(MAX) = dbo.DeriveKeyFromPassword(@Password, @Salt, @Iterations)

-- Use cached key for multiple operations
SELECT dbo.EncryptAesGcmWithDerivedKey(ColumnData, @CachedKey, @Salt)
FROM YourTable

-- Decrypt using cached key
SELECT dbo.DecryptAesGcmWithDerivedKey(EncryptedData, @CachedKey)
FROM YourTable
```

## Performance Benefits

- **Significant performance improvement** when processing multiple records with the same password
- **Same security level** as password-based methods
- **Full compatibility** - can decrypt data encrypted with either method
- **Same output format** as existing password-based methods

## Compatibility

The new methods are:
- **Forward compatible**: Data encrypted with derived keys can be decrypted with password-based methods
- **Backward compatible**: Data encrypted with password-based methods can be decrypted with derived keys
- **Cross-platform**: Available in all three target frameworks (.NET 8, .NET 4.8.1 for PowerBuilder, .NET 4.8.1 for SQL Server)

## Security Notes

- **Key derivation is identical** to password-based methods (PBKDF2 with SHA-256)
- **Same encryption algorithm** (AES-GCM with 256-bit keys)
- **Same output format** includes salt, nonce, and authentication tag
- **Keys should be cached securely** and cleared from memory when no longer needed
- **Use appropriate iteration counts** (minimum 1000, default 2000, maximum 100000)

## Method Reference

### .NET/PowerBuilder Methods
- `DeriveKeyFromPassword(password, salt, iterations)` - Derive cacheable key
- `EncryptAesGcmWithDerivedKey(plaintext, derivedKey, salt)` - Encrypt with cached key  
- `DecryptAesGcmWithDerivedKey(encrypted, derivedKey)` - Decrypt with cached key

### SQL Server CLR Functions
- `DeriveKeyFromPassword(@Password, @Salt, @Iterations)` - Derive cacheable key
- `EncryptAesGcmWithDerivedKey(@PlainText, @DerivedKey, @Salt)` - Encrypt with cached key
- `DecryptAesGcmWithDerivedKey(@EncryptedData, @DerivedKey)` - Decrypt with cached key