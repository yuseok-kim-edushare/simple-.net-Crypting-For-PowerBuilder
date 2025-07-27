# SQL Server CLR Row-by-Row Encryption Extension

This extension adds row-by-row encryption capabilities to the existing simple-.NET-Crypting-For-PowerBuilder library, enabling SQL Server CLR assembly integration for structured data encryption.

## New Features

### Row-by-Row Encryption Functions

The extension leverages the existing AES-GCM cryptographic implementation to provide:

- **Single Row Processing**: Encrypt/decrypt individual JSON rows
- **Table-Valued Functions (TVFs)**: Process multiple rows with structured output
- **Bulk Processing**: Stream processing for large datasets
- **JSON Support**: Row-as-JSON processing approach

### Backward Compatibility

- **100% PowerBuilder Compatibility**: All existing PowerBuilder functions remain unchanged
- **Existing SQL Functions**: All current SQL CLR functions continue to work
- **Same Cryptographic Core**: Uses the same battle-tested AES-GCM implementation

## Available Functions

### 1. Single Row Encryption
```sql
-- Encrypt a single JSON row
SELECT dbo.EncryptRowDataAesGcm(
    '{"id": 1, "name": "John Doe", "email": "john@example.com"}',
    'your-32-byte-key-here-base64-encoded',
    'your-12-byte-nonce-base64'
) AS EncryptedRow;
```

### 2. Single Row Decryption
```sql
-- Decrypt a single encrypted row
SELECT dbo.DecryptRowDataAesGcm(
    'base64-encrypted-data-here',
    'your-32-byte-key-here-base64-encoded',
    'your-12-byte-nonce-base64'
) AS DecryptedJson;
```

### 3. Table-Valued Function (TVF) for Bulk Encryption
```sql
-- Encrypt multiple table rows from JSON array
DECLARE @tableData NVARCHAR(MAX) = '[
    {"id": 1, "name": "Alice", "department": "Engineering"},
    {"id": 2, "name": "Bob", "department": "Marketing"},
    {"id": 3, "name": "Carol", "department": "Sales"}
]';

SELECT 
    RowId,
    EncryptedData,
    AuthTag
FROM dbo.EncryptTableRowsAesGcm(
    @tableData,
    'your-32-byte-key-here-base64-encoded',
    'your-12-byte-nonce-base64'
)
ORDER BY RowId;
```

### 4. Bulk Processing with Streaming
```sql
-- Process large datasets in batches
EXEC dbo.BulkProcessRowsAesGcm
    @tableDataJson = '[...large JSON array...]',
    @base64Key = 'your-32-byte-key-here-base64-encoded',
    @batchSize = 1000;  -- Process in batches of 1000 rows
```

## Real-World Example: Customer Data Encryption

```sql
-- Convert existing customer table to JSON for encryption
DECLARE @customerData NVARCHAR(MAX) = (
    SELECT 
        CustomerID,
        FirstName,
        LastName,
        Email,
        Phone,
        CreditCardNumber
    FROM Customers 
    FOR JSON PATH
);

-- Encrypt customer data row-by-row
SELECT 
    'Customer ' + CAST(RowId AS NVARCHAR(10)) AS CustomerRef,
    RowId,
    EncryptedData,
    AuthTag
FROM dbo.EncryptTableRowsAesGcm(
    @customerData,
    'customer-encryption-key-base64-encoded',
    'customer-nonce-base64'
)
ORDER BY RowId;
```

## Deployment Instructions

### 1. Deploy Assembly
```sql
-- Run CreateAssembly.sql
-- Replace [PATH] with actual path to SecureLibrary-SQL.dll
USE [YourDatabase]
GO
CREATE ASSEMBLY SimpleDotNetCrypting
FROM '[PATH]/SecureLibrary-SQL.dll'
WITH PERMISSION_SET = SAFE;
```

### 2. Create Functions
```sql
-- Run CreateFunctions.sql
-- Creates all row-by-row encryption functions
```

### 3. Test Installation
```sql
-- Run TestScripts.sql
-- Comprehensive tests for all new functionality
```

## Technical Details

### Architecture
- **Shared Cryptographic Core**: Uses existing `BcryptInterop` class with Windows CNG API
- **JSON Processing**: Simple JSON array parser for SQL Server compatibility
- **Memory Management**: Proper cleanup of sensitive data
- **Error Handling**: Graceful handling of invalid inputs

### Security Features
- **AES-GCM Authenticated Encryption**: 256-bit keys with 128-bit authentication tags
- **Unique Nonces**: Support for per-row nonce generation
- **Memory Clearing**: Automatic cleanup of sensitive key material
- **Input Validation**: Comprehensive validation of keys, nonces, and data

### Performance Optimizations
- **Batch Processing**: Configurable batch sizes for large datasets
- **Streaming Support**: Memory-efficient processing of large data sets
- **Minimal Memory Footprint**: Efficient buffer management
- **Reusable Components**: Leverages existing optimized encryption functions

## Integration Examples

### PowerBuilder Integration (Unchanged)
```powerbuilder
// Existing PowerBuilder code continues to work unchanged
string encrypted_data = encrypt_aes_gcm(original_data, key, nonce)
string decrypted_data = decrypt_aes_gcm(encrypted_data, key, nonce)
```

### SQL Server Stored Procedure Integration
```sql
CREATE PROCEDURE EncryptCustomerBatch
    @batchSize INT = 1000
AS
BEGIN
    DECLARE @customerJson NVARCHAR(MAX) = (
        SELECT * FROM SensitiveCustomers FOR JSON PATH
    );
    
    EXEC dbo.BulkProcessRowsAesGcm 
        @customerJson, 
        'your-encryption-key', 
        @batchSize;
END
```

## Requirements

- SQL Server 2012 or later with CLR integration enabled
- .NET Framework 4.8.1 runtime
- Windows environment (uses Windows CNG APIs)
- `PERMISSION_SET = SAFE` CLR assembly trust level

## Files Structure

```
SqlServerCLR/
├── Deploy/
│   ├── CreateAssembly.sql     # Deploy CLR assembly
│   ├── CreateFunctions.sql    # Create CLR functions  
│   └── TestScripts.sql        # Comprehensive tests
└── README.md                  # This documentation
```

## Error Handling

All functions return `NULL` on error to maintain SQL Server compatibility:

- Invalid JSON format returns `NULL`
- Invalid key/nonce length returns `NULL`  
- Encryption/decryption failures return `NULL`
- Memory or system errors return `NULL`

Check SQL Server error logs for detailed error information.

## Support

This extension maintains full backward compatibility with:
- All existing PowerBuilder functions
- All existing SQL Server CLR functions
- Existing AES-GCM, Bcrypt, and ECDH functionality

No changes are required to existing PowerBuilder or SQL Server code.