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