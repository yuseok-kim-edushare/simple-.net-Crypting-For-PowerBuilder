# SQL Server CLR Password-Based Table Encryption

This extension provides a powerful, simple, and highly maintainable solution for table-level encryption in SQL Server. It allows entire tables or query results to be encrypted into a single string using a password, and then decrypted back into a fully structured, queryable result set with a single command.

This is the ideal solution for secure data archival, transfer, and seamless integration with applications like PowerBuilder.

## Core Features

- **Password-Based Encryption**: Encrypts an entire dataset with just a password using `dbo.EncryptXmlWithPassword`.
- **Universal Dynamic Restoration**: A single stored procedure, `dbo.RestoreEncryptedTable`, decrypts any table and restores it to its original structure dynamically.
- **Zero Maintenance for Decryption**: There is no need to create custom functions or views for different table structures. The single restore procedure handles everything.
- **PowerBuilder-Friendly**: The ultimate simple interface for PowerBuilder. Just `EXEC` the stored procedure and get a standard, ready-to-use result set.
- **High Security**: Uses AES-256-GCM with PBKDF2 for key derivation, with automatic salt and nonce handling.

## The Recommended Workflow

This workflow is designed for maximum simplicity and maintainability.

### Step 1: Encrypt the Table with a Password
Use `dbo.EncryptXmlWithPassword` to convert your table into a single, encrypted string.

```sql
DECLARE @password NVARCHAR(MAX) = 'YourStrongP@ssw0rd!';
DECLARE @customerXml XML = (SELECT * FROM Customers FOR XML PATH('Row'), ROOT('Root'));
DECLARE @encryptedData NVARCHAR(MAX) = dbo.EncryptXmlWithPassword(@customerXml, @password);
```

### Step 2: Decrypt and Restore the Table with a Single Command
Use the universal `dbo.RestoreEncryptedTable` stored procedure to get the data back.

```sql
-- This single command is all you need to decrypt and restore the table.
EXEC dbo.RestoreEncryptedTable @encryptedData, @password;
```

This universal procedure means you never have to write custom decryption logic again. It dynamically discovers the columns from the encrypted data and returns a perfectly structured result set every time.

## Deployment and Core Objects

The assembly provides one function for encryption and one stored procedure for decryption.

| Object                        | Type              | Purpose                                                                 |
|-------------------------------|-------------------|-------------------------------------------------------------------------|
| `EncryptXmlWithPassword`      | Scalar Function   | Encrypts an XML document using a password.                              |
| `RestoreEncryptedTable`       | Stored Procedure  | The universal procedure to decrypt and restore any table.               |

### 1. Deploy Assembly
Run `CreateAssembly.sql`. **Note: This solution requires the `UNSAFE` permission set for the assembly**, as it dynamically generates and executes SQL to restore the table structure.

```sql
CREATE ASSEMBLY SimpleDotNetCrypting
FROM '[PATH]/SecureLibrary-SQL.dll'
WITH PERMISSION_SET = UNSAFE;
```

### 2. Create Core Objects
Run `CreateFunctions.sql` to create the CLR function and stored procedure.

### 3. Test Installation
Run `TestScripts.sql` to perform a full round-trip test.

## PowerBuilder and Small Business Integration

This workflow is the ultimate solution for small businesses, especially in the Korean market, that use PowerBuilder.

### Key Benefits:
- **Zero Maintenance**: Encrypt and restore any number of tables without ever writing new SQL functions or views.
- **Maximum Simplicity**: PowerBuilder calls a single, consistent stored procedure for any encrypted table.
- **Full Korean Character Support**: The XML approach is fully Unicode-aware.
- **High Security**: Strong, password-based key derivation helps meet privacy requirements like the Korean Personal Information Protection Act (PIPA).