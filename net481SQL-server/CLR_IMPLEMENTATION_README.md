# SQL Server CLR Implementation for SecureLibrary.SQL

This document describes the new SQL Server CLR implementation that provides T-SQL accessible functions and procedures for cryptographic operations, making it compatible with SQL Server and PowerBuilder applications.

## 🎯 Problem Solved

The original service-based implementation used standard .NET types and patterns that couldn't be called directly from SQL Server. Additionally, the initial CLR implementation required manual XML conversion, which was not user-friendly. This enhanced implementation leverages SQL Server's native `FOR XML` capabilities:

- ✅ **SQL Server Compatible**: Uses proper `SqlTypes` and CLR attributes
- ✅ **T-SQL Accessible**: Can be called directly from T-SQL scripts and PowerBuilder
- ✅ **FOR XML Integration**: Uses SQL Server's native `FOR XML RAW, ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE` for automatic row data conversion
- ✅ **Automatic Schema Generation**: XMLSCHEMA provides complete table structure information
- ✅ **NULL Value Handling**: XSINIL properly handles NULL values in XML output
- ✅ **Binary Data Support**: BINARY BASE64 handles binary data types automatically
- ✅ **Service Integration**: Leverages the existing service implementations as underlying logic
- ✅ **Comprehensive Coverage**: Provides functions for all major cryptographic operations
- ✅ **User-Friendly**: No manual data conversion required - SQL Server generates structured XML automatically

## 🏗️ Architecture Overview

### Core Components

1. **`SqlCLRFunctions.cs`** - Scalar functions for individual operations
2. **`SqlCLRProcedures.cs`** - Stored procedures for complex operations
3. **Service Layer** - Existing service implementations (`CgnService`, `EncryptionEngine`, etc.)
4. **Installation Script** - `install-clr-functions.sql` for deployment

### Key Features

- **Password Hashing**: Bcrypt with configurable work factors
- **AES-GCM Encryption**: Authenticated encryption with password-based key derivation
- **Key Generation**: Cryptographically secure random keys and nonces
- **XML Encryption**: Secure XML data encryption/decryption
- **Table Operations**: Full table encryption with schema preservation
- **Row Operations**: Individual row encryption/decryption
- **Metadata Validation**: Comprehensive validation of encryption parameters

## 🚀 Quick Start

### 1. Build the Assembly

```bash
# Navigate to the project directory
cd net481SQL-server

# Build the project
dotnet build --configuration Release

# The DLL will be in: bin/Release/net48/SecureLibrary-SQL.dll
```

### 2. Deploy to SQL Server

```sql
-- Run the installation script
EXEC install-clr-functions.sql
```

**Note**: Update the DLL path in the installation script before running.

### 3. Test the Installation

```sql
-- Test password hashing
SELECT dbo.HashPassword('MyPassword123!') AS HashedPassword;

-- Test encryption
SELECT dbo.EncryptAesGcmWithPassword('Secret data', 'MyPassword', 10000) AS EncryptedData;
```

## 📋 Available Functions

### Password Hashing Functions

| Function | Description | Parameters | Returns |
|----------|-------------|------------|---------|
| `HashPassword` | Hash password with default work factor | `@password NVARCHAR(MAX)` | `NVARCHAR(MAX)` |
| `HashPasswordWithWorkFactor` | Hash password with custom work factor | `@password NVARCHAR(MAX), @workFactor INT` | `NVARCHAR(MAX)` |
| `VerifyPassword` | Verify password against hash | `@password NVARCHAR(MAX), @hashedPassword NVARCHAR(MAX)` | `BIT` |
| `GenerateSalt` | Generate salt for password hashing | `@workFactor INT` | `NVARCHAR(MAX)` |
| `GetHashInfo` | Get information about hashed password | `@hashedPassword NVARCHAR(MAX)` | `XML` |

### AES-GCM Encryption Functions

| Function | Description | Parameters | Returns |
|----------|-------------|------------|---------|
| `EncryptAesGcm` | Encrypt with provided key | `@plainText NVARCHAR(MAX), @base64Key NVARCHAR(MAX)` | `NVARCHAR(MAX)` |
| `DecryptAesGcm` | Decrypt with provided key | `@base64EncryptedData NVARCHAR(MAX), @base64Key NVARCHAR(MAX)` | `NVARCHAR(MAX)` |
| `EncryptAesGcmWithPassword` | Encrypt with password-based key derivation | `@plainText NVARCHAR(MAX), @password NVARCHAR(MAX), @iterations INT` | `NVARCHAR(MAX)` |
| `DecryptAesGcmWithPassword` | Decrypt with password-based key derivation | `@base64EncryptedData NVARCHAR(MAX), @password NVARCHAR(MAX), @iterations INT` | `NVARCHAR(MAX)` |

### Key Generation Functions

| Function | Description | Parameters | Returns |
|----------|-------------|------------|---------|
| `GenerateKey` | Generate cryptographically secure key | `@keySizeBits INT` | `NVARCHAR(MAX)` |
| `GenerateNonce` | Generate cryptographically secure nonce | `@nonceSizeBytes INT` | `NVARCHAR(MAX)` |
| `DeriveKeyFromPassword` | Derive key from password using PBKDF2 | `@password NVARCHAR(MAX), @base64Salt NVARCHAR(MAX), @iterations INT, @keySizeBytes INT` | `NVARCHAR(MAX)` |

### XML Encryption Functions

| Function | Description | Parameters | Returns |
|----------|-------------|------------|---------|
| `EncryptXml` | Encrypt XML data | `@xmlData XML, @password NVARCHAR(MAX), @iterations INT` | `NVARCHAR(MAX)` |
| `DecryptXml` | Decrypt XML data | `@base64EncryptedXml NVARCHAR(MAX), @password NVARCHAR(MAX), @iterations INT` | `XML` |

### Utility Functions

| Function | Description | Parameters | Returns |
|----------|-------------|------------|---------|
| `ValidateEncryptionMetadata` | Validate encryption metadata | `@metadataXml XML` | `XML` |

## 📋 Available Stored Procedures

### Table Operations

| Procedure | Description | Parameters |
|-----------|-------------|------------|
| `EncryptTableWithMetadata` | Encrypt entire table with schema preservation | `@tableName NVARCHAR(MAX), @password NVARCHAR(MAX), @iterations INT, @encryptedData NVARCHAR(MAX) OUTPUT` |
| `DecryptTableWithMetadata` | Decrypt table and restore to target table | `@encryptedData NVARCHAR(MAX), @password NVARCHAR(MAX), @targetTableName NVARCHAR(MAX)` |
| `WrapDecryptProcedure` | Wrapper for automatic temp table creation | `@encryptedData NVARCHAR(MAX), @password NVARCHAR(MAX)` |

### Row Operations

| Procedure | Description | Parameters |
|-----------|-------------|------------|
| `EncryptRowWithMetadata` | Encrypt single row from FOR XML query output | `@rowXml XML, @password NVARCHAR(MAX), @iterations INT, @encryptedRow NVARCHAR(MAX) OUTPUT` |
| `DecryptRowWithMetadata` | Decrypt row and return as result set | `@encryptedRow NVARCHAR(MAX), @password NVARCHAR(MAX)` |
| `EncryptRowsBatch` | Batch encrypt multiple rows from FOR XML query output | `@rowsXml XML, @password NVARCHAR(MAX), @iterations INT, @batchId NVARCHAR(50)` |
| `DecryptRowsBatch` | Batch decrypt multiple rows and return as result set | `@batchId NVARCHAR(50), @password NVARCHAR(MAX)` |

## 💡 Usage Examples

### Basic Password Operations

```sql
-- Hash a password
DECLARE @hashedPassword NVARCHAR(MAX) = dbo.HashPassword('MySecurePassword123!');
PRINT 'Hashed Password: ' + @hashedPassword;

-- Verify a password
DECLARE @isValid BIT = dbo.VerifyPassword('MySecurePassword123!', @hashedPassword);
PRINT 'Password Valid: ' + CASE WHEN @isValid = 1 THEN 'Yes' ELSE 'No' END;

-- Hash with custom work factor
DECLARE @customHash NVARCHAR(MAX) = dbo.HashPasswordWithWorkFactor('MyPassword', 14);
```

### Text Encryption

```sql
-- Encrypt text with password
DECLARE @plainText NVARCHAR(MAX) = 'This is secret data that needs to be encrypted';
DECLARE @password NVARCHAR(MAX) = 'MyEncryptionPassword';
DECLARE @encrypted NVARCHAR(MAX) = dbo.EncryptAesGcmWithPassword(@plainText, @password, 10000);

-- Decrypt text
DECLARE @decrypted NVARCHAR(MAX) = dbo.DecryptAesGcmWithPassword(@encrypted, @password, 10000);
PRINT 'Decrypted: ' + @decrypted;
```

### XML Encryption

```sql
-- Encrypt XML data
DECLARE @xmlData XML = '<UserData><Name>John Doe</Name><Email>john@example.com</Email><SSN>123-45-6789</SSN></UserData>';
DECLARE @encryptedXml NVARCHAR(MAX) = dbo.EncryptXml(@xmlData, 'MyPassword', 10000);

-- Decrypt XML data
DECLARE @decryptedXml XML = dbo.DecryptXml(@encryptedXml, 'MyPassword', 10000);
SELECT @decryptedXml AS DecryptedData;
```

### Key Generation

```sql
-- Generate a 256-bit AES key
DECLARE @aesKey NVARCHAR(MAX) = dbo.GenerateKey(256);
PRINT 'Generated Key: ' + @aesKey;

-- Generate a 12-byte nonce for AES-GCM
DECLARE @nonce NVARCHAR(MAX) = dbo.GenerateNonce(12);
PRINT 'Generated Nonce: ' + @nonce;

-- Derive key from password
DECLARE @salt NVARCHAR(MAX) = dbo.GenerateSalt(12);
DECLARE @derivedKey NVARCHAR(MAX) = dbo.DeriveKeyFromPassword('MyPassword', @salt, 10000, 32);
```

### Table Operations

```sql
-- Encrypt an entire table
DECLARE @encryptedTableData NVARCHAR(MAX);
EXEC dbo.EncryptTableWithMetadata 
    @tableName = 'Users',
    @password = 'MyTablePassword',
    @iterations = 10000,
    @encryptedData = @encryptedTableData OUTPUT;

-- Decrypt table to a new table
EXEC dbo.DecryptTableWithMetadata 
    @encryptedData = @encryptedTableData,
    @password = 'MyTablePassword',
    @targetTableName = 'Users_Decrypted';
```

### Row Operations with FOR XML

```sql
-- Encrypt a single row using FOR XML (automatic schema generation!)
DECLARE @rowXml XML;
DECLARE @encryptedRow NVARCHAR(MAX);

-- Get row as XML with schema and metadata
SET @rowXml = (
    SELECT * 
    FROM Users 
    WHERE UserID = 1 
    FOR XML RAW('Row'), ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE
);

-- Encrypt the row
EXEC dbo.EncryptRowWithMetadata 
    @rowXml = @rowXml,
    @password = 'MyPassword',
    @iterations = 10000,
    @encryptedRow = @encryptedRow OUTPUT;

-- Decrypt the row and return as result set
EXEC dbo.DecryptRowWithMetadata @encryptedRow, 'MyPassword';

-- Batch encrypt multiple rows
DECLARE @rowsXml XML;
DECLARE @batchId NVARCHAR(50) = 'BATCH_' + CAST(GETDATE() AS NVARCHAR(50));

-- Get multiple rows as XML with schema
SET @rowsXml = (
    SELECT * 
    FROM Users 
    WHERE IsActive = 1
    FOR XML RAW('Row'), ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE, ROOT('Rows')
);

-- Encrypt the rows in batch
EXEC dbo.EncryptRowsBatch 
    @rowsXml = @rowsXml,
    @password = 'MyPassword',
    @iterations = 10000,
    @batchId = @batchId;

-- Decrypt the batch
EXEC dbo.DecryptRowsBatch @batchId, 'MyPassword';

-- Selective column encryption
DECLARE @sensitiveRowXml XML = (
    SELECT 
        UserID,
        Email,
        Phone,
        Salary
    FROM Users 
    WHERE UserID = 1 
    FOR XML RAW('SensitiveRow'), ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE
);

-- Encrypt only sensitive columns
EXEC dbo.EncryptRowWithMetadata 
    @rowXml = @sensitiveRowXml,
    @password = 'MyPassword',
    @iterations = 10000,
    @encryptedRow = @encryptedRow OUTPUT;
```

## 🔧 PowerBuilder Integration

### Example PowerBuilder Code

```powerbuilder
// Hash a password
string ls_hashed_password
ls_hashed_password = dw_1.GetItemString(1, "password_hash")
ls_hashed_password = "SELECT dbo.HashPassword('" + ls_password + "')"

// Verify a password
boolean lb_is_valid
lb_is_valid = dw_1.GetItemBoolean(1, "is_valid")
lb_is_valid = "SELECT dbo.VerifyPassword('" + ls_password + "', '" + ls_hashed_password + "')"

// Encrypt sensitive data
string ls_encrypted_data
ls_encrypted_data = "SELECT dbo.EncryptAesGcmWithPassword('" + ls_sensitive_data + "', '" + ls_password + "', 10000)"

// Decrypt data
string ls_decrypted_data
ls_decrypted_data = "SELECT dbo.DecryptAesGcmWithPassword('" + ls_encrypted_data + "', '" + ls_password + "', 10000)"

// Encrypt a user row using FOR XML
string ls_encrypted_row
ls_encrypted_row = "DECLARE @rowXml XML = (SELECT * FROM Users WHERE UserID = " + li_user_id + " FOR XML RAW('Row'), ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE); EXEC dbo.EncryptRowWithMetadata @rowXml = @rowXml, @password = '" + ls_password + "', @iterations = 10000, @encryptedRow = @encryptedRow OUTPUT"

// Decrypt a user row and get result set
string ls_decrypt_query
ls_decrypt_query = "EXEC dbo.DecryptRowWithMetadata '" + ls_encrypted_row + "', '" + ls_password + "'"
```

## 🔄 FOR XML Integration Benefits

### Why FOR XML?

The refactored implementation uses SQL Server's native `FOR XML` capabilities instead of requiring manual XML conversion. This approach provides several key advantages:

#### 1. **Automatic Schema Generation**
```sql
-- XMLSCHEMA automatically includes complete table structure
SELECT * FROM Users WHERE UserID = 1 
FOR XML RAW('Row'), ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE
```
- **Complete Metadata**: Includes column names, data types, and constraints
- **Type Safety**: Preserves exact SQL Server data types
- **No Manual Schema**: No need to manually specify column information

#### 2. **NULL Value Handling**
```sql
-- XSINIL properly handles NULL values in XML output
SELECT cust_id, cust_name, email FROM Users WHERE email IS NULL
FOR XML RAW('Row'), ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE
```
- **Proper NULL Representation**: Uses `xsi:nil="true"` for NULL values
- **Data Integrity**: Preserves NULL state during encryption/decryption
- **Consistent Handling**: Works across all data types

#### 3. **Binary Data Support**
```sql
-- BINARY BASE64 automatically handles binary data types
SELECT id, name, binary_data FROM BinaryTable WHERE id = 1
FOR XML RAW('Row'), ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE
```
- **Automatic Encoding**: Binary data is automatically Base64 encoded
- **Type Preservation**: Maintains binary data type information
- **Secure Handling**: Binary data is properly encrypted and decrypted

#### 4. **Complex Data Types**
```sql
-- Supports all SQL Server data types including complex ones
SELECT 
    id, 
    xml_data,           -- XML type
    json_data,          -- NVARCHAR(MAX) with JSON
    guid_value,         -- UNIQUEIDENTIFIER
    datetime_value,     -- DATETIME2
    decimal_value       -- DECIMAL(18,4)
FROM ComplexDataTable 
WHERE id = 1
FOR XML RAW('Row'), ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE
```

#### 5. **PowerBuilder Integration**
```sql
-- PowerBuilder-friendly wrapper procedure
CREATE PROCEDURE dbo.EncryptRowForPowerBuilder
    @tableName NVARCHAR(128),
    @whereClause NVARCHAR(MAX),
    @password NVARCHAR(MAX),
    @iterations INT = 10000,
    @encryptedRow NVARCHAR(MAX) OUTPUT
AS
BEGIN
    DECLARE @sql NVARCHAR(MAX);
    DECLARE @rowXml XML;
    
    -- Dynamic SQL with FOR XML
    SET @sql = N'
        SET @rowXml = (
            SELECT * 
            FROM ' + QUOTENAME(@tableName) + N' 
            WHERE ' + @whereClause + N'
            FOR XML RAW(''Row''), ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE
        )';
    
    EXEC sp_executesql @sql, N'@rowXml XML OUTPUT', @rowXml OUTPUT;
    
    -- Encrypt the row
    EXEC dbo.EncryptRowWithMetadata 
        @rowXml = @rowXml,
        @password = @password,
        @iterations = @iterations,
        @encryptedRow = @encryptedRow OUTPUT;
END
```

### FOR XML Query Structure

The recommended FOR XML query structure for optimal compatibility:

```sql
SELECT [columns] 
FROM [table] 
WHERE [conditions]
FOR XML RAW('RowName'), ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE
```

**Parameters Explained:**
- `RAW('RowName')`: Creates XML elements with the specified name
- `ELEMENTS`: Returns data as sub-elements rather than attributes
- `XSINIL`: Includes `xsi:nil="true"` for NULL values
- `BINARY BASE64`: Encodes binary data as Base64
- `XMLSCHEMA`: Includes XML schema information
- `TYPE`: Returns XML type instead of NVARCHAR

### Batch Processing

For multiple rows, use the `ROOT` parameter:

```sql
SELECT [columns] 
FROM [table] 
WHERE [conditions]
FOR XML RAW('RowName'), ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE, ROOT('RootName')
```

## 🛡️ Security Considerations

### Best Practices

1. **Strong Passwords**: Use strong, unique passwords for encryption
2. **High Iteration Count**: Use at least 10,000 iterations for PBKDF2
3. **Secure Storage**: Store encrypted data securely, not in plain text
4. **Key Management**: Implement proper key management practices
5. **Access Control**: Restrict access to encryption functions

### Security Features

- **AES-GCM**: Authenticated encryption prevents tampering
- **PBKDF2**: Secure password-based key derivation
- **Bcrypt**: Secure password hashing with salt
- **Memory Clearing**: Sensitive data is cleared from memory
- **Input Validation**: Comprehensive parameter validation

## 🔍 Troubleshooting

### Common Issues

1. **CLR Not Enabled**
   ```sql
   -- Enable CLR integration
   EXEC sp_configure 'clr enabled', 1;
   RECONFIGURE;
   ```

2. **Assembly Permission Issues**
   ```sql
   -- Grant UNSAFE permissions
   ALTER ASSEMBLY [SecureLibrary.SQL] WITH PERMISSION_SET = UNSAFE;
   ```

3. **Function Not Found**
   - Ensure the assembly is properly loaded
   - Check function names match exactly
   - Verify parameter types

4. **Encryption/Decryption Failures**
   - Verify password is correct
   - Check iteration count matches
   - Ensure encrypted data format is correct

### Debugging

```sql
-- Check assembly status
SELECT * FROM sys.assemblies WHERE name = 'SecureLibrary.SQL';

-- List all CLR functions
SELECT * FROM sys.objects WHERE type = 'FS' AND name LIKE '%Hash%' OR name LIKE '%Encrypt%';

-- Test individual functions
SELECT dbo.HashPassword('test') AS TestHash;
```

## 📚 Additional Resources

- [SQL Server CLR Integration Documentation](https://docs.microsoft.com/en-us/sql/relational-databases/clr-integration/)
- [SQL Server Security Best Practices](https://docs.microsoft.com/en-us/sql/relational-databases/security/)
- [Cryptographic Standards](https://docs.microsoft.com/en-us/dotnet/standard/security/cryptography-model)

## 🤝 Contributing

When contributing to this implementation:

1. Follow the existing code patterns
2. Use proper SQL Server CLR attributes
3. Implement comprehensive error handling
4. Add appropriate security measures
5. Include unit tests for new functions
6. Update documentation

## 📄 License

This implementation follows the same license as the main project. See the main LICENSE file for details. 