# Usage Examples for SQL Server CLR Row-by-Row Encryption

This document provides practical examples of using the new row-by-row encryption functions in real-world scenarios.

## Example 1: Customer Data Encryption for GDPR Compliance

```sql
-- Scenario: Encrypt customer PII data for GDPR compliance
USE CustomerDB
GO

-- Create a view that encrypts sensitive customer data
CREATE VIEW vw_EncryptedCustomers AS
SELECT 
    c.CustomerID,
    c.FirstName,
    c.LastName,
    -- Encrypt the entire customer record as JSON
    e.RowId,
    e.EncryptedData,
    e.AuthTag
FROM Customers c
CROSS APPLY (
    SELECT * FROM dbo.EncryptTableRowsAesGcm(
        (SELECT c.* FOR JSON PATH),
        'gdpr-compliance-key-base64-encoded12345678901234567890123456789012',
        'gdpr-nonce-base64-12'
    )
) e;

-- Query encrypted customer data
SELECT TOP 10 * FROM vw_EncryptedCustomers;
```

## Example 2: Financial Transaction Encryption

```sql
-- Scenario: Encrypt financial transactions for audit trail
DECLARE @transactionKey NVARCHAR(64) = 'financial-audit-key-base64-encoded12345678901234567890123456789012';
DECLARE @transactionNonce NVARCHAR(24) = 'fin-audit-nonce-b64';

-- Get transactions from the last 30 days as JSON
DECLARE @transactionsJson NVARCHAR(MAX) = (
    SELECT 
        TransactionID,
        AccountNumber,
        Amount,
        TransactionDate,
        Description,
        MerchantName
    FROM Transactions 
    WHERE TransactionDate >= DATEADD(DAY, -30, GETDATE())
    FOR JSON PATH
);

-- Encrypt all transactions
CREATE TABLE #EncryptedTransactions (
    RowId INT,
    EncryptedData NVARCHAR(MAX), 
    AuthTag NVARCHAR(32),
    ProcessedDate DATETIME2 DEFAULT SYSDATETIME()
);

INSERT INTO #EncryptedTransactions (RowId, EncryptedData, AuthTag)
SELECT RowId, EncryptedData, AuthTag
FROM dbo.EncryptTableRowsAesGcm(@transactionsJson, @transactionKey, @transactionNonce);

-- Verify encryption
SELECT 
    COUNT(*) AS TotalTransactionsEncrypted,
    AVG(LEN(EncryptedData)) AS AvgEncryptedSize,
    MIN(ProcessedDate) AS StartTime,
    MAX(ProcessedDate) AS EndTime
FROM #EncryptedTransactions;
```

## Example 3: Medical Records Encryption (HIPAA Compliance)

```sql
-- Scenario: Encrypt patient medical records for HIPAA compliance
DECLARE @hipaaKey NVARCHAR(64) = 'hipaa-medical-records-key-base64-encoded12345678901234567890123456';
DECLARE @hipaaBaseNonce NVARCHAR(24) = 'hipaa-med-nonce-b64';

-- Create stored procedure for medical record encryption
CREATE PROCEDURE sp_EncryptPatientRecords
    @PatientID INT = NULL,
    @BatchSize INT = 500
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @patientRecords NVARCHAR(MAX);
    
    -- Get patient records (with optional patient filter)
    IF @PatientID IS NOT NULL
    BEGIN
        SET @patientRecords = (
            SELECT 
                PatientID,
                FirstName,
                LastName,
                DateOfBirth,
                SSN,
                InsuranceNumber,
                MedicalHistory,
                Prescriptions
            FROM PatientRecords 
            WHERE PatientID = @PatientID
            FOR JSON PATH
        );
    END
    ELSE
    BEGIN
        SET @patientRecords = (
            SELECT 
                PatientID,
                FirstName,
                LastName,
                DateOfBirth,
                SSN,
                InsuranceNumber,
                MedicalHistory,
                Prescriptions
            FROM PatientRecords
            FOR JSON PATH
        );
    END
    
    -- Use bulk processing for large datasets
    EXEC dbo.BulkProcessRowsAesGcm 
        @patientRecords, 
        @hipaaKey,
        @BatchSize;
        
    PRINT 'Patient records encrypted successfully for HIPAA compliance';
END
GO

-- Execute for all patients
EXEC sp_EncryptPatientRecords @BatchSize = 100;

-- Execute for specific patient
EXEC sp_EncryptPatientRecords @PatientID = 12345, @BatchSize = 1;
```

## Example 4: E-commerce Order Processing

```sql
-- Scenario: Encrypt customer orders with payment information
DECLARE @ecommerceKey NVARCHAR(64) = 'ecommerce-orders-key-base64-encoded12345678901234567890123456789';
DECLARE @ecommerceNonce NVARCHAR(24) = 'ecom-order-nonce-64';

-- Create function to encrypt single order
CREATE FUNCTION dbo.fn_EncryptOrderData(@OrderID INT)
RETURNS TABLE
AS
RETURN
(
    SELECT 
        @OrderID AS OrderID,
        e.RowId,
        e.EncryptedData,
        e.AuthTag,
        SYSDATETIME() AS EncryptedAt
    FROM (
        SELECT 
            o.OrderID,
            o.CustomerID,
            o.OrderDate,
            o.TotalAmount,
            o.PaymentMethod,
            o.CreditCardLast4,
            o.BillingAddress,
            o.ShippingAddress,
            (
                SELECT 
                    oi.ProductID,
                    oi.Quantity,
                    oi.UnitPrice,
                    p.ProductName
                FROM OrderItems oi
                INNER JOIN Products p ON oi.ProductID = p.ProductID
                WHERE oi.OrderID = o.OrderID
                FOR JSON PATH
            ) AS OrderItems
        FROM Orders o
        WHERE o.OrderID = @OrderID
        FOR JSON PATH
    ) orderData
    CROSS APPLY dbo.EncryptTableRowsAesGcm(
        orderData.json, 
        'ecommerce-orders-key-base64-encoded12345678901234567890123456789',
        'ecom-order-nonce-64'
    ) e
);

-- Usage: Encrypt specific order
SELECT * FROM dbo.fn_EncryptOrderData(12345);

-- Batch encrypt recent orders
DECLARE @recentOrders NVARCHAR(MAX) = (
    SELECT * FROM Orders 
    WHERE OrderDate >= DATEADD(DAY, -7, GETDATE())
    FOR JSON PATH
);

SELECT 'Recent Orders Encryption Results' AS Operation;
SELECT 
    RowId,
    LEFT(EncryptedData, 100) + '...' AS EncryptedSample,
    AuthTag
FROM dbo.EncryptTableRowsAesGcm(@recentOrders, @ecommerceKey, @ecommerceNonce)
ORDER BY RowId;
```

## Example 5: Employee Payroll Data Encryption

```sql
-- Scenario: Encrypt payroll data for security and compliance
CREATE TABLE PayrollEncryption (
    EmployeeID INT,
    PayrollPeriod NVARCHAR(20),
    RowId INT,
    EncryptedPayrollData NVARCHAR(MAX),
    AuthTag NVARCHAR(32),
    EncryptedDate DATETIME2 DEFAULT SYSDATETIME(),
    INDEX IX_PayrollEncryption_Employee (EmployeeID, PayrollPeriod)
);

DECLARE @payrollKey NVARCHAR(64) = 'payroll-security-key-base64-encoded12345678901234567890123456';
DECLARE @payrollNonce NVARCHAR(24) = 'payroll-nonce-b64';

-- Encrypt payroll for current period
DECLARE @currentPeriod NVARCHAR(20) = FORMAT(GETDATE(), 'yyyy-MM');
DECLARE @payrollData NVARCHAR(MAX) = (
    SELECT 
        p.EmployeeID,
        e.FirstName,
        e.LastName,
        p.BaseSalary,
        p.Overtime,
        p.Bonuses,
        p.Deductions,
        p.NetPay,
        p.PayrollPeriod
    FROM Payroll p
    INNER JOIN Employees e ON p.EmployeeID = e.EmployeeID
    WHERE p.PayrollPeriod = @currentPeriod
    FOR JSON PATH
);

-- Insert encrypted payroll data
INSERT INTO PayrollEncryption (EmployeeID, PayrollPeriod, RowId, EncryptedPayrollData, AuthTag)
SELECT 
    JSON_VALUE(SUBSTRING(
        @payrollData, 
        CHARINDEX('"EmployeeID":', @payrollData) + 13,
        CHARINDEX(',', @payrollData, CHARINDEX('"EmployeeID":', @payrollData)) - CHARINDEX('"EmployeeID":', @payrollData) - 13
    ), '$') AS EmployeeID,
    @currentPeriod,
    RowId,
    EncryptedData,
    AuthTag
FROM dbo.EncryptTableRowsAesGcm(@payrollData, @payrollKey, @payrollNonce);

-- Query encrypted payroll summary
SELECT 
    PayrollPeriod,
    COUNT(*) AS EmployeesEncrypted,
    AVG(LEN(EncryptedPayrollData)) AS AvgEncryptedSize,
    MIN(EncryptedDate) AS EncryptionStartTime,
    MAX(EncryptedDate) AS EncryptionEndTime
FROM PayrollEncryption
WHERE PayrollPeriod = @currentPeriod
GROUP BY PayrollPeriod;
```

## Example 6: Integration with PowerBuilder Applications

```sql
-- Scenario: Create views that PowerBuilder applications can use
-- This maintains existing PowerBuilder functionality while adding row-level encryption

-- Create stored procedure that PowerBuilder can call
CREATE PROCEDURE sp_GetEncryptedCustomerData
    @CustomerID INT = NULL,
    @IncludeDecrypted BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @customerKey NVARCHAR(64) = 'pb-customer-key-base64-encoded12345678901234567890123456789';
    DECLARE @customerNonce NVARCHAR(24) = 'pb-cust-nonce-b64';
    
    IF @CustomerID IS NOT NULL
    BEGIN
        -- Single customer
        DECLARE @singleCustomer NVARCHAR(MAX) = (
            SELECT * FROM Customers WHERE CustomerID = @CustomerID FOR JSON PATH
        );
        
        SELECT 
            @CustomerID AS CustomerID,
            RowId,
            EncryptedData,
            AuthTag,
            CASE 
                WHEN @IncludeDecrypted = 1 
                THEN dbo.DecryptRowDataAesGcm(EncryptedData, @customerKey, @customerNonce)
                ELSE NULL 
            END AS DecryptedData
        FROM dbo.EncryptTableRowsAesGcm(@singleCustomer, @customerKey, @customerNonce);
    END
    ELSE
    BEGIN
        -- All customers (be careful with large datasets)
        DECLARE @allCustomers NVARCHAR(MAX) = (
            SELECT TOP 100 * FROM Customers FOR JSON PATH
        );
        
        SELECT 
            RowId,
            EncryptedData,
            AuthTag,
            CASE 
                WHEN @IncludeDecrypted = 1 
                THEN dbo.DecryptRowDataAesGcm(EncryptedData, @customerKey, @customerNonce)
                ELSE NULL 
            END AS DecryptedData
        FROM dbo.EncryptTableRowsAesGcm(@allCustomers, @customerKey, @customerNonce)
        ORDER BY RowId;
    END
END
GO

-- PowerBuilder can call this procedure:
-- EXEC sp_GetEncryptedCustomerData @CustomerID = 123, @IncludeDecrypted = 1
```

## Example 7: Performance Monitoring and Benchmarking

```sql
-- Scenario: Monitor encryption performance for optimization
CREATE TABLE EncryptionPerformanceLog (
    TestID INT IDENTITY(1,1) PRIMARY KEY,
    TestName NVARCHAR(100),
    RowCount INT,
    DataSizeKB INT,
    ExecutionTimeMS INT,
    RowsPerSecond DECIMAL(10,2),
    TestDate DATETIME2 DEFAULT SYSDATETIME()
);

-- Performance test procedure
CREATE PROCEDURE sp_BenchmarkEncryption
    @TestName NVARCHAR(100),
    @RowCount INT = 1000
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @startTime DATETIME2 = SYSDATETIME();
    DECLARE @testKey NVARCHAR(64) = 'performance-test-key-base64-encoded12345678901234567890123';
    DECLARE @testNonce NVARCHAR(24) = 'perf-test-nonce-b64';
    
    -- Generate test data
    DECLARE @testData NVARCHAR(MAX) = '[';
    DECLARE @i INT = 1;
    WHILE @i <= @RowCount
    BEGIN
        IF @i > 1 SET @testData = @testData + ',';
        SET @testData = @testData + '{"id": ' + CAST(@i AS NVARCHAR(10)) + 
                        ', "name": "Test User ' + CAST(@i AS NVARCHAR(10)) + 
                        '", "email": "user' + CAST(@i AS NVARCHAR(10)) + '@test.com"' +
                        ', "data": "' + REPLICATE('X', 200) + '"}';
        SET @i = @i + 1;
    END
    SET @testData = @testData + ']';
    
    DECLARE @dataSizeKB INT = LEN(@testData) / 1024;
    
    -- Execute encryption
    DECLARE @resultCount INT = (
        SELECT COUNT(*) 
        FROM dbo.EncryptTableRowsAesGcm(@testData, @testKey, @testNonce)
    );
    
    DECLARE @endTime DATETIME2 = SYSDATETIME();
    DECLARE @executionTimeMS INT = DATEDIFF(MILLISECOND, @startTime, @endTime);
    DECLARE @rowsPerSecond DECIMAL(10,2) = CASE 
        WHEN @executionTimeMS > 0 THEN (@resultCount * 1000.0) / @executionTimeMS 
        ELSE 0 
    END;
    
    -- Log performance results
    INSERT INTO EncryptionPerformanceLog (
        TestName, RowCount, DataSizeKB, ExecutionTimeMS, RowsPerSecond
    ) VALUES (
        @TestName, @resultCount, @dataSizeKB, @executionTimeMS, @rowsPerSecond
    );
    
    -- Return results
    SELECT 
        @TestName AS TestName,
        @resultCount AS RowsProcessed,
        @dataSizeKB AS DataSizeKB,
        @executionTimeMS AS ExecutionTimeMS,
        @rowsPerSecond AS RowsPerSecond;
END
GO

-- Run performance benchmarks
EXEC sp_BenchmarkEncryption 'Small Dataset', 100;
EXEC sp_BenchmarkEncryption 'Medium Dataset', 1000;
EXEC sp_BenchmarkEncryption 'Large Dataset', 5000;

-- View performance results
SELECT * FROM EncryptionPerformanceLog ORDER BY TestDate DESC;
```

## Best Practices

1. **Key Management**: Always use secure, randomly generated keys
2. **Nonce Uniqueness**: Ensure nonces are unique per encryption operation
3. **Batch Sizes**: Use appropriate batch sizes for bulk operations (typically 500-2000 rows)
4. **Memory Management**: Be mindful of memory usage with large JSON payloads
5. **Error Handling**: Always check for NULL returns from encryption functions
6. **Performance Testing**: Benchmark with your actual data volumes
7. **Security**: Store encryption keys securely, separate from encrypted data
8. **Compliance**: Document encryption methods for audit and compliance requirements

## NEW: SQL Server-Side Decryption Examples

The following examples demonstrate the new decryption capabilities that restore table structures for direct SQL querying.

### Example 8: PowerBuilder Direct Database Access with Decryption

```sql
-- Scenario: Small business using PowerBuilder with direct database access
-- Need to decrypt data server-side for views and stored procedures

-- Step 1: Create a permanent encrypted customer table
CREATE TABLE EncryptedCustomers (
    CustomerID INT,
    RowId INT,
    EncryptedData NVARCHAR(MAX),
    AuthTag NVARCHAR(32),
    CreatedDate DATETIME2 DEFAULT SYSDATETIME(),
    INDEX IX_EncryptedCustomers_Customer (CustomerID, RowId)
);

-- Step 2: Encrypt and store customer data
DECLARE @pbKey NVARCHAR(64) = 'powerbuilder-direct-access-key-base64-12345678901234567890123456';
DECLARE @pbNonce NVARCHAR(24) = 'pb-direct-nonce-b64';

-- Sample Korean customer data (common in Korean businesses)
DECLARE @koreanCustomers NVARCHAR(MAX) = '[
    {"customer_id": 1001, "name": "김철수", "company": "테크솔루션", "phone": "02-123-4567", "email": "kim@techsol.kr"},
    {"customer_id": 1002, "name": "이영희", "company": "디지털코리아", "phone": "02-234-5678", "email": "lee@digital.kr"},
    {"customer_id": 1003, "name": "박민수", "company": "스마트비즈", "phone": "02-345-6789", "email": "park@smart.kr"}
]';

-- Insert encrypted customer data
INSERT INTO EncryptedCustomers (CustomerID, RowId, EncryptedData, AuthTag)
SELECT 
    JSON_VALUE(DecryptedData, '$.customer_id') AS CustomerID,
    RowId,
    -- Re-encrypt individual rows for storage
    dbo.EncryptRowDataAesGcm(DecryptedData, @pbKey, @pbNonce),
    'individual-auth-tag'
FROM (
    SELECT RowId, DecryptedData
    FROM dbo.DecryptBulkTableData(@koreanCustomers, @pbKey, @pbNonce)
) AS temp;

-- Step 3: Create a view that PowerBuilder can query directly
CREATE VIEW vw_DecryptedCustomers AS
SELECT 
    ec.CustomerID,
    ec.RowId,
    JSON_VALUE(dbo.DecryptRowDataAesGcm(ec.EncryptedData, @pbKey, @pbNonce), '$.name') AS CustomerName,
    JSON_VALUE(dbo.DecryptRowDataAesGcm(ec.EncryptedData, @pbKey, @pbNonce), '$.company') AS Company,
    JSON_VALUE(dbo.DecryptRowDataAesGcm(ec.EncryptedData, @pbKey, @pbNonce), '$.phone') AS Phone,
    JSON_VALUE(dbo.DecryptRowDataAesGcm(ec.EncryptedData, @pbKey, @pbNonce), '$.email') AS Email,
    ec.CreatedDate
FROM EncryptedCustomers ec;

-- PowerBuilder can now query this view with standard SQL:
SELECT * FROM vw_DecryptedCustomers WHERE Company LIKE '%디지털%';
SELECT CustomerName, Phone FROM vw_DecryptedCustomers WHERE CustomerID = 1001;
```

### Example 9: Bulk Table Decryption for Data Analysis

```sql
-- Scenario: Business intelligence and reporting on encrypted data
CREATE PROCEDURE sp_DecryptTableForAnalysis
    @TableName NVARCHAR(100),
    @AnalysisKey NVARCHAR(64),
    @AnalysisNonce NVARCHAR(24)
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Create temporary table for decrypted results
    CREATE TABLE #DecryptedAnalysis (
        RowId INT,
        OriginalJSON NVARCHAR(MAX),
        CustomerID INT,
        CustomerName NVARCHAR(100),
        Revenue DECIMAL(18,2),
        AnalysisDate DATETIME2 DEFAULT SYSDATETIME()
    );
    
    -- Sample: Decrypt sales data for analysis
    DECLARE @encryptedSales NVARCHAR(MAX) = (
        SELECT STRING_AGG(
            CAST(RowId AS NVARCHAR(10)) + '|' + EncryptedData + '|' + AuthTag, 
            CHAR(13) + CHAR(10)
        )
        FROM EncryptedSales 
        WHERE SalesDate >= DATEADD(MONTH, -1, GETDATE())
    );
    
    -- Decrypt bulk sales data
    INSERT INTO #DecryptedAnalysis (RowId, OriginalJSON, CustomerID, CustomerName, Revenue)
    SELECT 
        d.RowId,
        d.DecryptedData,
        JSON_VALUE(d.DecryptedData, '$.customer_id'),
        JSON_VALUE(d.DecryptedData, '$.customer_name'),
        CAST(JSON_VALUE(d.DecryptedData, '$.total_amount') AS DECIMAL(18,2))
    FROM dbo.DecryptBulkTableData(@encryptedSales, @AnalysisKey, @AnalysisNonce) d;
    
    -- Return analysis results
    SELECT 
        'Sales Analysis Results' AS ReportType,
        COUNT(*) AS TotalTransactions,
        SUM(Revenue) AS TotalRevenue,
        AVG(Revenue) AS AverageTransaction,
        COUNT(DISTINCT CustomerID) AS UniqueCustomers
    FROM #DecryptedAnalysis;
    
    -- Return detailed decrypted data
    SELECT 
        RowId,
        CustomerID,
        CustomerName,
        Revenue,
        AnalysisDate
    FROM #DecryptedAnalysis
    ORDER BY Revenue DESC;
    
    DROP TABLE #DecryptedAnalysis;
END
GO
```

### Example 10: Decrypted Data Views for Korean Privacy Law Compliance

```sql
-- Scenario: Comply with Korean Personal Information Protection Act (PIPA)
-- Need selective decryption based on user permissions

-- Create role-based decryption function
CREATE FUNCTION dbo.GetDecryptedCustomerData(
    @UserRole NVARCHAR(50),
    @CustomerID INT = NULL
)
RETURNS TABLE
AS
RETURN
(
    SELECT 
        ec.CustomerID,
        ec.RowId,
        CASE 
            WHEN @UserRole IN ('Administrator', 'Manager') THEN
                JSON_VALUE(dbo.DecryptRowDataAesGcm(ec.EncryptedData, 'admin-key-base64-encoded123456789012345678901234567890', 'admin-nonce-base64'), '$.name')
            WHEN @UserRole = 'Sales' THEN
                LEFT(JSON_VALUE(dbo.DecryptRowDataAesGcm(ec.EncryptedData, 'sales-key-base64-encoded123456789012345678901234567890', 'sales-nonce-base64'), '$.name'), 1) + '***'
            ELSE 'RESTRICTED'
        END AS CustomerName,
        CASE 
            WHEN @UserRole IN ('Administrator', 'Manager') THEN
                JSON_VALUE(dbo.DecryptRowDataAesGcm(ec.EncryptedData, 'admin-key-base64-encoded123456789012345678901234567890', 'admin-nonce-base64'), '$.phone')
            ELSE 'XXX-XXXX-XXXX'
        END AS Phone,
        CASE 
            WHEN @UserRole IN ('Administrator', 'Manager', 'Sales') THEN
                JSON_VALUE(dbo.DecryptRowDataAesGcm(ec.EncryptedData, 'business-key-base64-encoded123456789012345678901234567890', 'business-nonce-b64'), '$.company')
            ELSE 'RESTRICTED'
        END AS Company
    FROM EncryptedCustomers ec
    WHERE (@CustomerID IS NULL OR ec.CustomerID = @CustomerID)
);

-- Usage examples for different user roles:
-- Administrator access (full decryption)
SELECT * FROM dbo.GetDecryptedCustomerData('Administrator', NULL);

-- Sales staff access (partial decryption)
SELECT * FROM dbo.GetDecryptedCustomerData('Sales', 1001);

-- Guest access (minimal decryption)
SELECT * FROM dbo.GetDecryptedCustomerData('Guest', NULL);
```

### Example 11: PowerBuilder Integration with Stored Procedures

```sql
-- Scenario: PowerBuilder applications calling stored procedures for encrypted data
CREATE PROCEDURE sp_PowerBuilderCustomerLookup
    @SearchTerm NVARCHAR(100),
    @SearchType NVARCHAR(20) = 'NAME', -- 'NAME', 'COMPANY', 'PHONE'
    @MaxResults INT = 50
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @pbKey NVARCHAR(64) = 'powerbuilder-lookup-key-base64-encoded123456789012345678901234';
    DECLARE @pbNonce NVARCHAR(24) = 'pb-lookup-nonce-b64';
    
    -- Create temporary results table
    CREATE TABLE #SearchResults (
        CustomerID INT,
        RowId INT,
        CustomerName NVARCHAR(100),
        Company NVARCHAR(100),
        Phone NVARCHAR(20),
        Email NVARCHAR(100),
        MatchScore INT
    );
    
    -- Decrypt all customer data for searching
    DECLARE @allEncryptedData NVARCHAR(MAX) = (
        SELECT STRING_AGG(
            CAST(RowId AS NVARCHAR(10)) + '|' + EncryptedData + '|' + AuthTag, 
            CHAR(13) + CHAR(10)
        )
        FROM EncryptedCustomers
    );
    
    -- Insert decrypted data into temp table for searching
    INSERT INTO #SearchResults (CustomerID, RowId, CustomerName, Company, Phone, Email, MatchScore)
    SELECT 
        JSON_VALUE(d.DecryptedData, '$.customer_id'),
        d.RowId,
        JSON_VALUE(d.DecryptedData, '$.name'),
        JSON_VALUE(d.DecryptedData, '$.company'),
        JSON_VALUE(d.DecryptedData, '$.phone'),
        JSON_VALUE(d.DecryptedData, '$.email'),
        CASE 
            WHEN @SearchType = 'NAME' AND JSON_VALUE(d.DecryptedData, '$.name') LIKE '%' + @SearchTerm + '%' THEN 100
            WHEN @SearchType = 'COMPANY' AND JSON_VALUE(d.DecryptedData, '$.company') LIKE '%' + @SearchTerm + '%' THEN 100
            WHEN @SearchType = 'PHONE' AND JSON_VALUE(d.DecryptedData, '$.phone') LIKE '%' + @SearchTerm + '%' THEN 100
            ELSE 0
        END
    FROM dbo.DecryptBulkTableData(@allEncryptedData, @pbKey, @pbNonce) d;
    
    -- Return filtered results for PowerBuilder
    SELECT TOP (@MaxResults)
        CustomerID,
        CustomerName,
        Company,
        Phone,
        Email,
        MatchScore
    FROM #SearchResults
    WHERE MatchScore > 0
    ORDER BY MatchScore DESC, CustomerName;
    
    DROP TABLE #SearchResults;
END
GO

-- PowerBuilder can call this procedure:
-- EXEC sp_PowerBuilderCustomerLookup '김철수', 'NAME', 10
-- EXEC sp_PowerBuilderCustomerLookup '테크솔루션', 'COMPANY', 20
```

### Example 12: Complete Round-Trip Encryption/Decryption Workflow

```sql
-- Scenario: Complete workflow from plain data to encryption to decryption
-- Suitable for small businesses with direct database access needs

-- Step 1: Original business data
CREATE TABLE BusinessContacts (
    ContactID INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100),
    Company NVARCHAR(100),
    Title NVARCHAR(50),
    Phone NVARCHAR(20),
    Email NVARCHAR(100),
    Address NVARCHAR(200),
    Notes NVARCHAR(500)
);

-- Insert sample Korean business data
INSERT INTO BusinessContacts (Name, Company, Title, Phone, Email, Address, Notes) VALUES
('김대표', '한국소프트웨어', '대표이사', '02-555-1234', 'kim@korsoftware.co.kr', '서울시 강남구', '주요 거래처'),
('이부장', '디지털솔루션', '영업부장', '02-555-5678', 'lee@digitalsol.co.kr', '서울시 서초구', 'PowerBuilder 전문'),
('박과장', '테크이노베이션', '개발과장', '02-555-9012', 'park@techinno.co.kr', '경기도 성남시', '신규 프로젝트 협의');

-- Step 2: Convert to JSON and encrypt
DECLARE @businessKey NVARCHAR(64) = 'business-contacts-key-base64-encoded123456789012345678901234567';
DECLARE @businessNonce NVARCHAR(24) = 'business-nonce-b64';

DECLARE @contactsJson NVARCHAR(MAX) = (
    SELECT * FROM BusinessContacts FOR JSON PATH
);

-- Create encrypted storage table
CREATE TABLE EncryptedBusinessContacts (
    ContactID INT,
    RowId INT,
    EncryptedData NVARCHAR(MAX),
    AuthTag NVARCHAR(32),
    EncryptedDate DATETIME2 DEFAULT SYSDATETIME(),
    INDEX IX_EncryptedContacts (ContactID, RowId)
);

-- Insert encrypted data
INSERT INTO EncryptedBusinessContacts (ContactID, RowId, EncryptedData, AuthTag)
SELECT 
    JSON_VALUE(DecryptedData, '$.ContactID') AS ContactID,
    RowId,
    EncryptedData,
    AuthTag
FROM dbo.EncryptTableRowsAesGcm(@contactsJson, @businessKey, @businessNonce);

-- Step 3: Create decryption view for business users
CREATE VIEW vw_BusinessContactsDecrypted AS
SELECT 
    ebc.ContactID,
    JSON_VALUE(dbo.DecryptRowDataAesGcm(ebc.EncryptedData, @businessKey, @businessNonce), '$.Name') AS Name,
    JSON_VALUE(dbo.DecryptRowDataAesGcm(ebc.EncryptedData, @businessKey, @businessNonce), '$.Company') AS Company,
    JSON_VALUE(dbo.DecryptRowDataAesGcm(ebc.EncryptedData, @businessKey, @businessNonce), '$.Title') AS Title,
    JSON_VALUE(dbo.DecryptRowDataAesGcm(ebc.EncryptedData, @businessKey, @businessNonce), '$.Phone') AS Phone,
    JSON_VALUE(dbo.DecryptRowDataAesGcm(ebc.EncryptedData, @businessKey, @businessNonce), '$.Email') AS Email,
    JSON_VALUE(dbo.DecryptRowDataAesGcm(ebc.EncryptedData, @businessKey, @businessNonce), '$.Address') AS Address,
    JSON_VALUE(dbo.DecryptRowDataAesGcm(ebc.EncryptedData, @businessKey, @businessNonce), '$.Notes') AS Notes,
    ebc.EncryptedDate
FROM EncryptedBusinessContacts ebc;

-- Step 4: PowerBuilder and other applications can now query normally
SELECT * FROM vw_BusinessContactsDecrypted WHERE Company LIKE '%소프트웨어%';
SELECT Name, Phone, Email FROM vw_BusinessContactsDecrypted WHERE Title LIKE '%부장%';

-- Step 5: Bulk decryption for reports
DECLARE @bulkContactData NVARCHAR(MAX) = (
    SELECT STRING_AGG(
        CAST(RowId AS NVARCHAR(10)) + '|' + EncryptedData + '|' + AuthTag, 
        CHAR(13) + CHAR(10)
    )
    FROM EncryptedBusinessContacts
);

SELECT 
    'Business Contacts Report' AS ReportTitle,
    RowId,
    JSON_VALUE(DecryptedData, '$.Name') AS Name,
    JSON_VALUE(DecryptedData, '$.Company') AS Company,
    JSON_VALUE(DecryptedData, '$.Phone') AS Phone
FROM dbo.DecryptBulkTableData(@bulkContactData, @businessKey, @businessNonce)
ORDER BY JSON_VALUE(DecryptedData, '$.Company'), JSON_VALUE(DecryptedData, '$.Name');
```

## Summary of New Decryption Capabilities

### Available Functions:
1. **`DecryptRowDataAesGcm`** - Decrypt single encrypted rows back to JSON
2. **`DecryptBulkTableData`** - Bulk decrypt structured table data back to JSON rows  
3. **`DecryptTableFromView`** - Decrypt table data for use in views and stored procedures

### Key Benefits for PowerBuilder Applications:
- **Direct SQL Querying**: Decrypted views can be queried with standard SQL
- **Korean Business Support**: Full support for Korean characters and business practices
- **Small Business Friendly**: Simple integration with existing PowerBuilder applications
- **Compliance Ready**: Role-based access and privacy law compliance capabilities
- **Performance Optimized**: Bulk operations for efficient processing

### PowerBuilder Integration Patterns:
1. **View-Based Access**: Create views that decrypt data transparently
2. **Stored Procedure Access**: Use procedures that return decrypted results
3. **Selective Decryption**: Role-based decryption based on user permissions
4. **Bulk Processing**: Efficient handling of large datasets
5. **Search Functionality**: Server-side searching of encrypted data

These new capabilities address the specific needs mentioned for small businesses using direct database access via PowerBuilder, providing complete round-trip encryption/decryption while maintaining compatibility with existing applications.