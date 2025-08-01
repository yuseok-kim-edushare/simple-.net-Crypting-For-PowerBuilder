-- Create test XML for UnifiedSchemaCompatibilityTests
-- This script creates a safe test XML file with minimal columns and dummy data
-- Only includes the columns that the tests actually verify: emp_id and emp_nm

-- Create table variable with minimal schema
DECLARE @TestTable TABLE (
    emp_id CHAR(5),
    emp_nm VARCHAR(20)
);

-- Insert dummy test data (no sensitive information)
INSERT INTO @TestTable (emp_id, emp_nm) VALUES 
('EMP01', 'Test User 1'),
('EMP02', 'Test User 2');

-- Execute encryption and get XML result
DECLARE @Password NVARCHAR(100) = 'TestPassword123!@#';
DECLARE @Iterations INT = 1000;
DECLARE @RowXml XML;
DECLARE @EncryptedRow NVARCHAR(MAX);

-- Convert table variable to XML format required by EncryptRowWithMetadata
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

-- Output the XML result
SELECT @EncryptedRow AS EncryptedXml;

-- Alternative: If you need to save to file, you can use this approach:
-- 1. Copy the XML result from SSMS output
-- 2. Save it as 'test-encrypted-row.xml' in the project
-- 3. Update the test file path in UnifiedSchemaCompatibilityTests.cs

-- For manual testing, you can also use:
-- SELECT * FROM @TestTable; -- To verify the test data 

-- 복호화 검증용 쿼리
-- 실제 SQL Server에서 데이터 검증을 위한 테스트
CREATE TABLE #temp (id INT, name NVARCHAR(100), reason NVARCHAR(200));
INSERT INTO #temp EXEC dbo.DecryptMultiRows @encryptedRowsXml, @password;
SELECT * FROM #temp;