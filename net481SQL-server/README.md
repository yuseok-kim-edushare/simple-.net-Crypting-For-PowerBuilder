# SQL Server CLR Encryption Library (`SecureLibrary-SQL.csproj`)

A robust SQL Server CLR assembly for PowerBuilder and .NET applications, providing secure cryptographic operations. This project currently supports:

- **Single Value Encryption/Decryption** (AES-GCM, password-based and direct key, with dedicated binary support)
- **Single Row Encryption/Decryption** (AES-GCM, password-based and direct key)
- **Bcrypt Password Hashing**

> **Note:** Table/multi-row encryption and ECDH key exchange are not yet implemented. See below for details.

---

## 🏗️ Architecture Overview

- **Core Interfaces**
  - `ICgnService` – Windows CGN (Cryptographic Next Generation) API wrapper
  - `IEncryptionEngine` – Row-level and value-level encryption/decryption
  - `ISqlXmlConverter` – SQL type to XML conversion utilities

- **Service Implementations**
  - `CgnService` – Thread-safe Windows CGN API implementation
  - `EncryptionEngine` – Row and value encryption with schema preservation
  - `SqlXmlConverter` – SQL type conversion with round-trip capability
  - `BcryptPasswordHashingService` – Bcrypt password hashing

---

## ✅ Current Features

- **Single Value Encryption/Decryption**
  - Encrypt and decrypt individual values using AES-GCM.
  - Supports both password-based and direct key encryption.
  - Dedicated binary encryption function (`EncryptBinaryValue`) for proper VARBINARY handling.
- **Single Row Encryption/Decryption**
  - Encrypt and decrypt a single row (as XML) with schema preservation.
- **Bcrypt Password Hashing**
  - Secure password hashing and verification using Bcrypt.

---

## ⚠️ Current Limitations

- **No Table/Multi-Row Encryption/Decryption**
  - Batch/table-level encryption and decryption are not yet implemented.
- **No ECDH Key Exchange**
  - ECDH (Elliptic Curve Diffie-Hellman) is not yet available.
- **Key Derivation for Performance**
  - Refactoring in progress: single value encryption/decryption will use derived keys for performance (same password-based encryption will reuse derived keys).

---

## 🔄 Remaining Refactoring

- **Derived Key Usage for Single Value Encryption/Decryption**
  - To improve performance, repeated password-based encryption/decryption of single values will use a derived key (PBKDF2) instead of deriving the key every time.
- **ECDH Implementation**
  - ECDH key exchange functionality is planned but not yet implemented.

- **IF you want to use these features, you can use legacy code.**
  - [How To](../Examples/SQL-server-Net%204.8)

---

## 🚧 Remaining Implementations

- **Table/Multi-Row Level Encryption/Decryption**
  - Planned: encrypt and decrypt entire tables or multiple rows at once.

---

## 🚀 Quick Start

### Prerequisites

- SQL Server 2016 or later
- .NET Framework 4.8
- Visual Studio 2019/2022 or .NET CLI
- SQL Server CLR enabled

### Building the Assembly

```bash
git clone <repository-url>
cd net481SQL-server
dotnet restore
dotnet build --configuration Release
```

### Deploying to SQL Server

1. **Enable CLR Integration:**
   ```sql
   sp_configure 'clr enabled', 1
   RECONFIGURE
   ```
2. **Run the installation script:**
   - Update the DLL path in `install-clr-functions.sql`
   - Run the script in SQL Server Management Studio

---

## 📖 Usage Examples

### Single Value Encryption/Decryption as String

```sql
-- Encrypt a single value
DECLARE @encryptedValue NVARCHAR(MAX) = dbo.EncryptValue('My secret data', 'YourPassword123!', 10000);
SELECT @encryptedValue AS EncryptedValue;

-- Decrypt a single value
SELECT dbo.DecryptValue(@encryptedValue, 'YourPassword123!') AS DecryptedValue;
```

### Single Value Encryption/Decryption as Binary

```sql
-- Encrypt binary data using the dedicated binary encryption function
DECLARE @binaryData VARBINARY(MAX) = CONVERT(VARBINARY(MAX), 'My secret binary data');
DECLARE @encryptedValue NVARCHAR(MAX) = dbo.EncryptBinaryValue(@binaryData, 'YourPassword123!', 10000);
SELECT @encryptedValue AS EncryptedValue;

-- Decrypt binary data back to VARBINARY(MAX)
SELECT dbo.DecryptBinaryValue(@encryptedValue, 'YourPassword123!') AS DecryptedBinaryValue;

-- Alternative: Using generic EncryptValue (for compatibility)
DECLARE @encryptedGeneric NVARCHAR(MAX) = dbo.EncryptValue('My secret data', 'YourPassword123!', 10000);
SELECT dbo.DecryptBinaryValue(@encryptedGeneric, 'YourPassword123!') AS DecryptedValue;
```

### Single Row Encryption/Decryption

```sql
-- Encrypt a single row using FOR XML
DECLARE @rowXml XML = (
    SELECT * FROM Users WHERE UserID = 1 
    FOR XML RAW('Row'), ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE
);
DECLARE @encryptedRow NVARCHAR(MAX);

EXEC dbo.EncryptRowWithMetadata 
    @rowXml = @rowXml,
    @password = 'YourPassword123!',
    @iterations = 10000,
    @encryptedRow = @encryptedRow OUTPUT;

-- Decrypt the row
EXEC dbo.DecryptRowWithMetadata @encryptedRow, 'YourPassword123!';
```

### Bcrypt Password Hashing

```sql
-- Hash a password
SELECT dbo.HashPassword('MyPassword123!') AS HashedPassword;

-- Verify a password (returns 1 for match, 0 for no match)
SELECT dbo.VerifyPassword('MyPassword123!', dbo.HashPassword('MyPassword123!')) AS IsValid;
```

---

## 🧪 Testing

```bash
dotnet test
```

---

## 📚 API Reference

See the source code for detailed interface and class documentation.

---

## 🔒 Security Considerations

- Use strong passwords and secure key management.
- Never store or log encryption keys in code or configuration files.
- Always use unique nonces for AES-GCM.

---

## 🤝 Contributing

See [Contributing.md](../Contributing.md) for guidelines.

---

**Note:** This library is designed for SQL Server environments and requires appropriate permissions for CLR integration. Always test in a development environment before deploying to production. 