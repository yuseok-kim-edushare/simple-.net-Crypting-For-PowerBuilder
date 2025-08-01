# Test XML Generation Guide

## Solution
Create a minimal test XML file with only the necessary columns and dummy data.

## Required Columns
Based on the test analysis, only these columns are needed:
- `emp_id` (CHAR(5))
- `emp_nm` (VARCHAR(20))

## Steps to Generate Safe Test XML

### 1. Use the SQL Script
Run the `create-test-xml.sql` script in SQL Server Management Studio (SSMS):

```sql
-- The script creates a table variable with minimal schema
DECLARE @TestTable TABLE (
    emp_id CHAR(5),
    emp_nm VARCHAR(20)
);

-- Insert dummy test data
INSERT INTO @TestTable (emp_id, emp_nm) VALUES 
('EMP01', 'Test User 1'),
('EMP02', 'Test User 2');
```

### 2. Execute Encryption
Use the EncryptRowWithMetadata procedure to generate the XML:

```sql
DECLARE @Password NVARCHAR(100) = 'TestPassword123!@#';
DECLARE @Iterations INT = 1000;
DECLARE @RowXml XML;
DECLARE @EncryptedRow NVARCHAR(MAX);

-- Convert table variable to XML format
SET @RowXml = (
    SELECT (
        SELECT TOP 1 * 
        FROM @TestTable
        FOR XML RAW('Row'), ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE
    ) AS 'RowData'
    FOR XML PATH('root'), TYPE
);

-- Execute the encryption procedure
EXEC dbo.EncryptRowWithMetadata 
    @rowXml = @RowXml,
    @password = @Password,
    @iterations = @Iterations,
    @encryptedRow = @EncryptedRow OUTPUT;

SELECT @EncryptedRow AS EncryptedXml;
```

### 3. Save the Result
1. Copy the XML result from SSMS output
2. Save it as `test-encrypted-row.xml` in the project root
3. The test will automatically use this file instead of the sensitive one

## Test Password
Use `TestPassword123!@#` as the test password to match the existing test expectations.

## File Structure
```
net481SQL-server/
├── create-test-xml.sql          # SQL script to generate test XML
├── xmlresult18.xml              
└── Tests/
    └── UnifiedSchemaCompatibilityTests.cs  
```

## Security Notes
- The generated XML contains only dummy data
- No real employee information is included
- The encryption is real but the data is fake
- Safe for public repository
