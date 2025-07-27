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

SELECT RowId, EncryptedData, AuthTag
FROM dbo.EncryptTableRowsAesGcm(
    @tableData,
    'table-encryption-key-base64-encoded',
    'table-nonce-base64'
)
ORDER BY RowId;
```

### 4. **NEW: Bulk Table Decryption (TVF)**
```sql
-- Decrypt bulk encrypted table data back to structured format
-- Input: pipe-delimited encrypted data (RowId|EncryptedData|AuthTag per line)
DECLARE @encryptedBulk NVARCHAR(MAX) = 
    '1|base64-data-1|base64-tag-1' + CHAR(13) + CHAR(10) +
    '2|base64-data-2|base64-tag-2' + CHAR(13) + CHAR(10) +
    '3|base64-data-3|base64-tag-3';

SELECT RowId, DecryptedData
FROM dbo.DecryptBulkTableData(
    @encryptedBulk,
    'table-encryption-key-base64-encoded',
    'table-nonce-base64'
)
ORDER BY RowId;
```

### 5. **NEW: Decryption for Views and Stored Procedures**
```sql
-- Use in views for PowerBuilder direct database access
CREATE VIEW vw_DecryptedCustomers AS
SELECT 
    RowId,
    JSON_VALUE(DecryptedData, '$.name') AS CustomerName,
    JSON_VALUE(DecryptedData, '$.email') AS Email,
    JSON_VALUE(DecryptedData, '$.department') AS Department
FROM dbo.DecryptTableFromView(
    'view-decryption-key-base64-encoded',
    'view-nonce-base64'
);

-- PowerBuilder can now query this view directly:
SELECT * FROM vw_DecryptedCustomers WHERE Department = 'Engineering';
```

### 6. Bulk Processing Procedure
```sql
-- Process large datasets in batches
EXEC dbo.BulkProcessRowsAesGcm
    @tableDataJson = '[...large JSON array...]',
    @base64Key = 'your-32-byte-key-here-base64-encoded',
    @batchSize = 1000;  -- Process in batches of 1000 rows
```

## NEW DECRYPTION CAPABILITIES

### PowerBuilder Integration Benefits
- **Direct SQL Querying**: Decrypted views can be queried with standard SQL
- **Korean Business Support**: Full support for Korean characters and business workflows  
- **Small Business Friendly**: Simple integration with existing PowerBuilder applications
- **Role-Based Access**: Selective decryption based on user permissions
- **Complete Round-Trip**: Full encryption/decryption cycle with table structure restoration

### Function Summary

| Function | Purpose | Input | Output |
|----------|---------|-------|--------|
| `EncryptRowDataAesGcm` | Encrypt single JSON row | JSON string, key, nonce | Base64 encrypted data |
| `DecryptRowDataAesGcm` | Decrypt single row | Encrypted data, key, nonce | JSON string |
| `EncryptTableRowsAesGcm` | Bulk encrypt JSON array | JSON array, key, nonce | Table with RowId, EncryptedData, AuthTag |
| `DecryptBulkTableData` | **NEW** Bulk decrypt table data | Structured encrypted data, key, nonce | Table with RowId, DecryptedData |
| `DecryptTableFromView` | **NEW** Decrypt for views/procedures | Key, nonce | Table with RowId, DecryptedData |
| `BulkProcessRowsAesGcm` | Stream processing | JSON array, key, batch size | Console output |

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

## **NEW: SQL Server-Side Decryption for Small Businesses**

### Addressing Korean Business Requirements

This update specifically addresses the need for **SQL Server-side decryption** that restores table structures for easier handling with basic SQL queries, as requested for small Korean businesses using direct PowerBuilder database access.

### Key New Capabilities:

1. **Table Structure Restoration**: New `DecryptBulkTableData()` function restores encrypted data back to queryable table structures
2. **Direct SQL Access**: PowerBuilder applications can now query decrypted data using standard SQL through views and stored procedures
3. **Korean Privacy Law Compliance**: Support for selective decryption based on user roles and permissions
4. **Small Business Optimization**: Designed for direct database access patterns common in small business PowerBuilder applications

### Usage for PowerBuilder Direct Access:

```sql
-- 1. Create encrypted data storage
CREATE TABLE EncryptedBusinessData (...);

-- 2. Create decryption view for PowerBuilder
CREATE VIEW vw_DecryptedData AS
SELECT 
    RowId,
    JSON_VALUE(DecryptedData, '$.name') AS CustomerName,
    JSON_VALUE(DecryptedData, '$.company') AS Company
FROM dbo.DecryptBulkTableData(@encryptedData, @key, @nonce);

-- 3. PowerBuilder queries the view normally
SELECT * FROM vw_DecryptedData WHERE Company LIKE '%소프트웨어%';
```

### Benefits for Korean Businesses:
- **Direct Database Access**: No application-layer decryption required
- **Standard SQL Queries**: Use familiar SQL syntax in views and stored procedures  
- **Korean Character Support**: Full Unicode support for Korean business names and data
- **Privacy Compliance**: Granular access control for Korean Personal Information Protection Act
- **PowerBuilder Integration**: Seamless integration with existing PowerBuilder applications

This addresses the specific feedback requesting SQL Server-side decryption capabilities for table structure restoration and direct PowerBuilder database access.