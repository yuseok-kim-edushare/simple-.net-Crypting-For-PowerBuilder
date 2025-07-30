# SQL Server CLR Encryption Library

A comprehensive SQL Server CLR assembly providing robust encryption, decryption, and cryptographic operations for PowerBuilder applications. This library implements SOLID principles with clear abstractions and supports tables with up to 1000 columns.

## 🏗️ Architecture Overview

The library follows SOLID principles with the following key components:

### Core Interfaces
- **`ICgnService`** - Windows CGN (Cryptographic Next Generation) API wrapper
- **`IEncryptionEngine`** - Row-level encryption/decryption operations
- **`ISqlXmlConverter`** - SQL type to XML conversion utilities

### Service Implementations
- **`CgnService`** - Thread-safe Windows CGN API implementation
- **`EncryptionEngine`** - Row-level encryption with schema preservation
- **`SqlXmlConverter`** - Robust SQL type conversion with round-trip capability

### Key Features
- ✅ **SOLID Architecture** - Clear separation of concerns
- ✅ **Thread-Safe Operations** - Safe for concurrent use
- ✅ **Schema Preservation** - Maintains original column structure
- ✅ **Scalable Design** - Supports up to 1000 columns
- ✅ **Comprehensive Testing** - 90%+ unit test coverage
- ✅ **Security Best Practices** - AES-GCM, PBKDF2, proper key management

## 🚀 Quick Start

### Prerequisites
- SQL Server 2016 or later
- .NET Framework 4.8
- Visual Studio 2019/2022 or .NET CLI
- SQL Server CLR enabled

### Building the Assembly

1. **Clone the repository:**
   ```bash
   git clone <repository-url>
   cd net481SQL-server
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

3. **Build the assembly:**
   ```bash
   dotnet build --configuration Release
   ```

4. **Run tests:**
   ```bash
   dotnet test
   ```

### Deploying to SQL Server

1. **Enable CLR Integration:**
   ```sql
   sp_configure 'clr enabled', 1
   RECONFIGURE
   ```

2. **Run the installation script:**
   ```sql
   -- Update the DLL path in install-clr-functions.sql first
   EXEC install-clr-functions.sql
   ```

3. **Test the installation:**
   ```sql
   -- Test password hashing
   SELECT dbo.HashPassword('MyPassword123!') AS HashedPassword;
   
   -- Test encryption
   SELECT dbo.EncryptAesGcmWithPassword('Secret data', 'MyPassword', 10000) AS EncryptedData;
   ```

## 🔧 Configuration

### Encryption Keys

The library supports both password-based and direct key encryption:

#### Password-Based Encryption (Recommended)
```csharp
var metadata = new EncryptionMetadata
{
    Algorithm = "AES-GCM",
    Key = "YourSecurePassword123!",
    Salt = Convert.FromBase64String("YourBase64Salt"),
    Iterations = 2000,  // PBKDF2 iterations
    AutoGenerateNonce = true
};
```

#### Direct Key Encryption (Performance)
```csharp
var metadata = new EncryptionMetadata
{
    Algorithm = "AES-GCM",
    Key = Convert.ToBase64String(yourAesKey),
    Salt = Convert.FromBase64String("YourBase64Salt"),
    Iterations = 2000,
    AutoGenerateNonce = true
};
```

### Security Recommendations

1. **Password Strength**: Use strong passwords (12+ characters, mixed case, numbers, symbols)
2. **Salt Generation**: Use cryptographically secure random salts (16+ bytes)
3. **Iteration Count**: Use at least 2000 PBKDF2 iterations
4. **Key Management**: Store keys securely, never in code or configuration files
5. **Nonce Management**: Always use unique nonces for each encryption operation

## 📖 Usage Examples

### Basic Row Encryption

```csharp
// Create encryption engine
var cgnService = new CgnService();
var xmlConverter = new SqlXmlConverter();
var encryptionEngine = new EncryptionEngine(cgnService, xmlConverter);

// Prepare metadata
var metadata = new EncryptionMetadata
{
    Algorithm = "AES-GCM",
    Key = "YourPassword123!",
    Salt = Convert.FromBase64String("YourBase64Salt"),
    Iterations = 2000,
    AutoGenerateNonce = true
};

// Encrypt a row
var encryptedData = encryptionEngine.EncryptRow(dataRow, metadata);

// Decrypt the row
var decryptedRow = encryptionEngine.DecryptRow(encryptedData, metadata);
```

### Batch Processing

```csharp
// Encrypt multiple rows
var encryptedRows = encryptionEngine.EncryptRows(dataRows, metadata);

// Decrypt multiple rows
var decryptedRows = encryptionEngine.DecryptRows(encryptedRows, metadata);
```

### SQL Server Integration with FOR XML

```sql
-- Encrypt a single row using FOR XML (automatic schema generation!)
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

-- Encrypt entire table
DECLARE @encryptedTableData NVARCHAR(MAX);
EXEC dbo.EncryptTableWithMetadata 
    @tableName = 'Users',
    @password = 'YourPassword123!',
    @iterations = 10000,
    @encryptedData = @encryptedTableData OUTPUT;
```

## 🧪 Testing

### Running Unit Tests

```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~EncryptionEngineTests"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Test Coverage

The library includes comprehensive tests covering:
- ✅ Single row encryption/decryption
- ✅ Batch processing
- ✅ Metadata validation
- ✅ Error handling
- ✅ Edge cases (null values, large data, special characters)
- ✅ Performance with large datasets

### Performance Testing

```csharp
// Test with 1000 columns
var largeTable = CreateLargeTable(1000);
var stopwatch = Stopwatch.StartNew();
var encryptedData = encryptionEngine.EncryptRow(largeTable.Rows[0], metadata);
stopwatch.Stop();
Console.WriteLine($"Encryption time: {stopwatch.ElapsedMilliseconds}ms");
```

## 🔍 Troubleshooting

### Common Issues

1. **CLR Integration Disabled**
   ```sql
   -- Check CLR status
   SELECT name, value, value_in_use 
   FROM sys.configurations 
   WHERE name = 'clr enabled'
   ```

2. **Assembly Loading Errors**
   - Verify assembly path is accessible to SQL Server
   - Check file permissions
   - Ensure .NET Framework 4.8 is installed

3. **Encryption Failures**
   - Verify password and salt are correct
   - Check iteration count is within valid range (1000-100000)
   - Ensure nonce is exactly 12 bytes for AES-GCM

4. **Memory Issues with Large Tables**
   - Use batch processing for tables with many rows
   - Monitor memory usage during encryption operations
   - Consider processing in smaller chunks

### Debugging

Enable detailed logging:

```csharp
var logger = new ConsoleLogger(); // Or implement custom logger
var encryptionEngine = new EncryptionEngine(cgnService, xmlConverter, logger);
```

### Performance Optimization

1. **Use Direct Keys**: For high-performance scenarios, derive keys once and reuse
2. **Batch Processing**: Process multiple rows together
3. **Memory Management**: Dispose of services when done
4. **Connection Pooling**: Reuse database connections

## 📚 API Reference

### ICgnService

```csharp
public interface ICgnService
{
    byte[] InvokeCgnOperation(byte[] inputData, CgnOperationType operationType);
    byte[] GenerateKey(int keySize);
    byte[] GenerateNonce(int nonceSize);
    byte[] EncryptAesGcm(byte[] plainData, byte[] key, byte[] nonce);
    byte[] DecryptAesGcm(byte[] cipherData, byte[] key, byte[] nonce);
    byte[] DeriveKeyFromPassword(string password, byte[] salt, int iterations, int keySize);
}
```

### IEncryptionEngine

```csharp
public interface IEncryptionEngine
{
    EncryptedRowData EncryptRow(DataRow row, EncryptionMetadata metadata);
    DataRow DecryptRow(EncryptedRowData encryptedData, EncryptionMetadata metadata);
    IEnumerable<EncryptedRowData> EncryptRows(IEnumerable<DataRow> rows, EncryptionMetadata metadata);
    IEnumerable<DataRow> DecryptRows(IEnumerable<EncryptedRowData> encryptedRows, EncryptionMetadata metadata);
    ValidationResult ValidateEncryptionMetadata(EncryptionMetadata metadata);
    int MaxSupportedColumns { get; }
    IEnumerable<string> SupportedAlgorithms { get; }
}
```

### ISqlXmlConverter

```csharp
public interface ISqlXmlConverter
{
    XDocument ToXml(SqlDataRecord record);
    SqlDataRecord FromXml(XDocument xml, SqlMetaData[] metadata);
    XDocument ToXml(DataRow row);
    DataRow FromXml(XDocument xml, DataTable table);
    XDocument ToXml(DataTable table);
    DataTable FromXml(XDocument xml);
    bool CanConvertToSqlType(object value, SqlDbType sqlType);
    object ConvertToSqlType(object value, SqlDbType sqlType);
    ValidationResult ValidateXmlStructure(XDocument xml);
}
```

## 🔒 Security Considerations

### Key Management
- Store encryption keys in secure key management systems (Azure Key Vault, AWS KMS, etc.)
- Rotate keys regularly
- Use different keys for different environments
- Never log or expose encryption keys

### Data Protection
- Encrypt sensitive data at rest
- Use secure communication channels
- Implement proper access controls
- Audit encryption/decryption operations

### Compliance
- The library supports various compliance requirements
- Maintain audit trails for encryption operations
- Implement proper data retention policies

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Ensure all tests pass
6. Submit a pull request

### Development Guidelines
- Follow SOLID principles
- Add comprehensive unit tests
- Update documentation
- Use meaningful commit messages
- Follow the existing code style

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

For support and questions:
- Create an issue in the repository
- Check the troubleshooting section
- Review the test examples
- Consult the API documentation

## 🔄 Version History

### v2.0.0 (Current)
- Complete refactoring with SOLID architecture
- New interfaces and service implementations
- Enhanced security and performance
- Comprehensive unit tests
- Support for up to 1000 columns

### v1.0.0
- Initial release with basic encryption functionality
- SQL Server CLR integration
- PowerBuilder compatibility

---

**Note**: This library is designed for SQL Server environments and requires appropriate permissions for CLR integration. Always test in a development environment before deploying to production. 