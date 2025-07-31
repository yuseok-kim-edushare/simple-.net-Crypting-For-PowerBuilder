# CHAR and NCHAR Space Padding Implementation

## Overview

This document describes the implementation of space padding for SQL Server `CHAR` and `NCHAR` data types in the encryption/decryption system. The implementation ensures that fixed-length character types are properly padded with spaces to match their defined maximum length, which is essential for maintaining data integrity when working with SQL Server tables.

## Problem Statement

In SQL Server, `CHAR` and `NCHAR` are fixed-length data types that automatically pad values with spaces to the right if the actual value is shorter than the maximum length. When data is processed through the encryption/decryption system, this padding information was being lost, causing errors when trying to insert the decrypted data back into SQL Server tables.

### Example Issue
```sql
-- SQL Server table definition
CREATE TABLE TestTable (
    Id INT,
    CharField CHAR(10),    -- Fixed length, pads with spaces
    NCharField NCHAR(8)    -- Fixed length, pads with spaces
);

-- Inserting "ABC" into CHAR(10) results in "ABC       " (7 trailing spaces)
-- Inserting "XY" into NCHAR(8) results in "XY      " (6 trailing spaces)
```

## Solution Implementation

### 1. Enhanced SqlXmlConverter

The `SqlXmlConverter` class has been enhanced with a new `ApplyCharPadding` method that handles space padding for `CHAR` and `NCHAR` types:

```csharp
/// <summary>
/// Applies space padding to string values for char and nchar SQL types
/// </summary>
/// <param name="value">The string value to pad</param>
/// <param name="sqlDbType">The SQL data type</param>
/// <param name="maxLength">The maximum length for the column</param>
/// <returns>The padded string value</returns>
private string ApplyCharPadding(string value, SqlDbType sqlDbType, int maxLength)
{
    // Only apply padding for char and nchar types
    if (sqlDbType != SqlDbType.Char && sqlDbType != SqlDbType.NChar)
        return value;

    // If maxLength is not positive, return the original value
    if (maxLength <= 0)
        return value;

    // If the value is null, return empty string padded to maxLength
    if (value == null)
        return new string(' ', maxLength);

    // If the value is already at or exceeds maxLength, return as is
    if (value.Length >= maxLength)
        return value;

    // Pad the value with spaces to reach maxLength
    return value.PadRight(maxLength, ' ');
}
```

### 2. Enhanced FromXml Method

The `FromXml` method in `SqlXmlConverter` has been updated to apply space padding when converting string values:

```csharp
// Apply space padding for char and nchar types
if (convertedValue is string stringValue)
{
    var sqlDbTypeString = column.Attribute("SqlDbType")?.Value;
    var maxLengthString = column.Attribute("MaxLength")?.Value;
    
    if (!string.IsNullOrEmpty(sqlDbTypeString) && !string.IsNullOrEmpty(maxLengthString))
    {
        if (Enum.TryParse<SqlDbType>(sqlDbTypeString, out SqlDbType sqlDbType) &&
            int.TryParse(maxLengthString, out int maxLength))
        {
            convertedValue = ApplyCharPadding(stringValue, sqlDbType, maxLength);
        }
    }
}
```

### 3. Empty String Handling

Empty strings for `CHAR` and `NCHAR` columns are also properly padded:

```csharp
// For non-nullable columns, preserve empty string but apply char padding if needed
var emptyValue = string.Empty;

// Check if this is a char/nchar column that needs padding
var sqlDbTypeString = column.Attribute("SqlDbType")?.Value;
var maxLengthString = column.Attribute("MaxLength")?.Value;

if (!string.IsNullOrEmpty(sqlDbTypeString) && !string.IsNullOrEmpty(maxLengthString))
{
    if (Enum.TryParse<SqlDbType>(sqlDbTypeString, out SqlDbType sqlDbType) &&
        int.TryParse(maxLengthString, out int maxLength))
    {
        emptyValue = ApplyCharPadding(emptyValue, sqlDbType, maxLength);
    }
}
```

## Usage Examples

### 1. Basic Usage with XML Conversion

```csharp
// Create a table with char/nchar columns
var table = new DataTable("TestTable");
table.Columns.Add("Id", typeof(int));
table.Columns.Add("CharField", typeof(string));
table.Columns["CharField"].MaxLength = 10;
table.Columns["CharField"].AllowDBNull = false;

// Create XML with explicit SQL type information
var doc = new XDocument();
var root = new XElement("Row");
doc.Add(root);

var charColumn = new XElement("Column",
    new XAttribute("Name", "CharField"),
    new XAttribute("Type", "String"),
    new XAttribute("SqlDbType", "Char"), // Explicitly set as Char
    new XAttribute("SqlTypeName", "CHAR(10)"),
    new XAttribute("MaxLength", "10"),
    new XAttribute("IsNullable", "false"),
    new XAttribute("Ordinal", "1"),
    new XAttribute("IsNull", "false")
);
charColumn.Value = "ABC";
root.Add(charColumn);

// Convert from XML - padding will be applied automatically
var restoredRow = _xmlConverter.FromXml(doc, table);
Assert.AreEqual("ABC       ", restoredRow["CharField"]); // Padded to 10 chars
```

### 2. Real-World Scenario

```csharp
// Simulate data from SQL Server with char/nchar columns
var table = new DataTable("CustomerTable");
table.Columns.Add("CustomerId", typeof(int));
table.Columns.Add("CustomerCode", typeof(string)); // CHAR(10) in SQL Server
table.Columns["CustomerCode"].MaxLength = 10;
table.Columns["CustomerCode"].AllowDBNull = false;

// Create XML with SQL type information
var doc = new XDocument();
var root = new XElement("Row");
doc.Add(root);

var customerCodeColumn = new XElement("Column",
    new XAttribute("Name", "CustomerCode"),
    new XAttribute("Type", "String"),
    new XAttribute("SqlDbType", "Char"), // Explicitly CHAR type
    new XAttribute("SqlTypeName", "CHAR(10)"),
    new XAttribute("MaxLength", "10"),
    new XAttribute("IsNullable", "false"),
    new XAttribute("Ordinal", "1"),
    new XAttribute("IsNull", "false")
);
customerCodeColumn.Value = "CUST001";
root.Add(customerCodeColumn);

// Convert and verify padding
var restoredRow = _xmlConverter.FromXml(doc, table);
Assert.AreEqual("CUST001   ", restoredRow["CustomerCode"]); // Padded to 10 chars
```

### 3. Encryption/Decryption with Padding

```csharp
// The padding is preserved through encryption/decryption cycles
var metadata = new EncryptionMetadata
{
    Algorithm = "AES-GCM",
    Key = "TestPassword123!",
    Salt = _cgnService.GenerateNonce(32),
    Iterations = 10000,
    AutoGenerateNonce = true
};

var encryptedData = _encryptionEngine.EncryptRow(restoredRow, metadata);
var decryptedRow = _encryptionEngine.DecryptRow(encryptedData, metadata);

// Padding is preserved
Assert.AreEqual("CUST001   ", decryptedRow["CustomerCode"]);
```

## Important Notes

### 1. SQL Type Information Requirement

The space padding feature requires explicit SQL type information in the XML. The system cannot automatically distinguish between `CHAR`, `NCHAR`, `VARCHAR`, and `NVARCHAR` types from CLR types alone, as they all map to `string` in .NET.

### 2. XML Schema Requirements

For space padding to work, the XML must include:
- `SqlDbType` attribute with value "Char" or "NChar"
- `MaxLength` attribute with the maximum length
- `SqlTypeName` attribute with the full SQL type name (e.g., "CHAR(10)")

### 3. Current Limitations

- The system cannot automatically infer SQL types from CLR types
- Manual specification of SQL type information is required in XML
- The encryption engine's schema preservation will still use `NVarChar` for string types

### 4. Best Practices

1. **Always include SQL type information** when working with `CHAR` and `NCHAR` columns
2. **Test with real SQL Server tables** to ensure compatibility
3. **Use explicit SQL type attributes** in XML when creating custom schemas
4. **Verify padding behavior** with unit tests for critical data

## Testing

The implementation includes comprehensive tests in `SchemaPreservationTests.cs`:

- `TestCharNCharSpacePadding()` - Tests basic padding functionality
- `TestCharNCharSpacePaddingWithCustomSchema()` - Tests with explicit SQL type information
- `TestRealWorldCharNCharScenario()` - Tests real-world usage scenarios

All tests verify that:
- Short values are properly padded with spaces
- Empty strings are padded to full length
- Values already at maximum length are not modified
- Padding is preserved through encryption/decryption cycles

## Conclusion

This implementation ensures that SQL Server `CHAR` and `NCHAR` data types are properly handled with space padding, maintaining data integrity when working with fixed-length character columns. The solution is backward compatible and only applies padding when explicit SQL type information indicates `CHAR` or `NCHAR` types. 