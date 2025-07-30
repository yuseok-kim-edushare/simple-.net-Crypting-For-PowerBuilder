# FOR XML XMLSCHEMA Handling Guide

## Problem Statement

When using `FOR XML ... XMLSCHEMA` in SQL Server, the output contains multiple root elements:
1. The XML schema definition (`<xsd:schema>`)
2. The actual data rows

This creates invalid XML that causes parsing errors like "Multiple root elements" when passed to CLR procedures.

## Solution

### 1. SQL Query Wrapping

Wrap the FOR XML output in an additional root element using nested SELECT:

#### Single Row Example:
```sql
-- Instead of:
SELECT TOP 1 * FROM dbo.tb_test_cust 
WHERE cust_id = 16424 
FOR XML RAW('Row'), ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE

-- Use:
SELECT (
    SELECT TOP 1 * FROM dbo.tb_test_cust 
    WHERE cust_id = 16424 
    FOR XML RAW('Row'), ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE
) AS 'RowData'
FOR XML PATH('root'), TYPE
```

#### Multiple Rows Example:
```sql
-- Instead of:
SELECT * FROM dbo.tb_test_cust 
WHERE cust_id IN (16424, 16425, 16426)
FOR XML RAW('Row'), ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE, ROOT('Rows')

-- Use:
SELECT (
    SELECT * FROM dbo.tb_test_cust 
    WHERE cust_id IN (16424, 16425, 16426)
    FOR XML RAW('Row'), ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE, ROOT('Rows')
) AS 'RowsData'
FOR XML PATH('root'), TYPE
```

### 2. C# Code Updates

The `SqlXmlConverter` has been updated to handle the wrapped XML structure:

```csharp
// Check if this is wrapped XML (root > RowData/RowsData > actual content)
if (root.Name.LocalName == "root" || root.Name.LocalName == "Rows")
{
    var dataElement = root.Element("RowData") ?? root.Element("RowsData") ?? root.Element("Data");
    if (dataElement != null && dataElement.HasElements)
    {
        // Use the inner content
        workingRoot = dataElement;
    }
}
```

### 3. Boolean Value Handling

SQL Server returns boolean values as `1` or `0` in FOR XML output, not `true` or `false`. The converter now handles both formats:

```csharp
if (dataType == typeof(bool))
{
    // Handle SQL Server boolean format (1/0) as well as true/false
    if (value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase))
        return true;
    if (value == "0" || value.Equals("false", StringComparison.OrdinalIgnoreCase))
        return false;
    return bool.Parse(value);
}
```

## Benefits of Using XMLSCHEMA

1. **Complete Schema Preservation**: Essential for tables with 40-1000 columns
2. **Data Type Information**: Preserves exact SQL data types for accurate restoration
3. **NULL Handling**: Properly distinguishes between NULL and empty values
4. **Column Metadata**: Preserves max length, precision, scale, etc.
5. **Automatic Type Conversion**: No manual type mapping required

## Example Output Structure

The wrapped XML structure looks like:
```xml
<root>
  <RowData>
    <xsd:schema ...>
      <!-- Schema definition -->
    </xsd:schema>
    <Row>
      <cust_id>16424</cust_id>
      <cust_name>John Doe</cust_name>
      <is_active>1</is_active>
      <!-- More columns -->
    </Row>
  </RowData>
</root>
```

## PowerBuilder Integration

For PowerBuilder applications, use the wrapper procedure:

```sql
EXEC dbo.EncryptRowForPowerBuilder
    @tableName = 'tb_test_cust',
    @whereClause = 'cust_id = 16424',
    @password = 'MySecurePassword123!',
    @iterations = 10000,
    @encryptedRow = @encryptedRowForPB OUTPUT;
```

The procedure automatically handles the XML wrapping internally.

## Best Practices

1. Always use the wrapping technique when using XMLSCHEMA
2. Test with tables containing various data types
3. Verify boolean columns are handled correctly
4. Use appropriate iteration counts for security (10000+ recommended)
5. Store encrypted data with proper access controls

## Troubleshooting

If you encounter errors:

1. **"Multiple root elements"**: Ensure the FOR XML query is properly wrapped
2. **"Boolean parsing error"**: Update to the latest SqlXmlConverter version
3. **"No Row element found"**: Check the XML structure and element names
4. **Performance issues**: Consider batch processing for large datasets