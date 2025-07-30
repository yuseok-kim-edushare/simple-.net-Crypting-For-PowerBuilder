-- =============================================
-- Cursor Usage Example for Row-by-Row Encryption
-- Demonstrates how to use the encryption procedures with cursors
-- for processing rows one at a time (typical PowerBuilder scenario)
-- =============================================

PRINT '=== Cursor Usage Example for Row-by-Row Encryption ===';
GO

-- =============================================
-- STEP 1: Create Sample Table
-- =============================================
PRINT '--- STEP 1: Creating Sample Table ---';
GO

-- Create a sample customers table
IF OBJECT_ID('dbo.Customers', 'U') IS NOT NULL
    DROP TABLE dbo.Customers;
GO

CREATE TABLE dbo.Customers (
    CustomerID INT IDENTITY(1,1) PRIMARY KEY,
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    Email NVARCHAR(100),
    Phone NVARCHAR(20),
    SSN NVARCHAR(11),
    CreditCard NVARCHAR(20),
    Salary DECIMAL(10,2),
    CreatedDate DATETIME2 DEFAULT GETDATE()
);
GO

-- Insert sample data
INSERT INTO dbo.Customers (FirstName, LastName, Email, Phone, SSN, CreditCard, Salary)
VALUES 
    ('John', 'Doe', 'john.doe@email.com', '+1-555-0101', '123-45-6789', '4111-1111-1111-1111', 75000.00),
    ('Jane', 'Smith', 'jane.smith@email.com', '+1-555-0102', '987-65-4321', '4222-2222-2222-2222', 82000.00),
    ('Bob', 'Johnson', 'bob.johnson@email.com', '+1-555-0103', '456-78-9012', '4333-3333-3333-3333', 65000.00),
    ('Alice', 'Brown', 'alice.brown@email.com', '+1-555-0104', '789-01-2345', '4444-4444-4444-4444', 95000.00),
    ('Charlie', 'Wilson', 'charlie.wilson@email.com', '+1-555-0105', '321-54-6789', '4555-5555-5555-5555', 70000.00);
GO

PRINT '✓ Sample data inserted';

-- =============================================
-- STEP 2: Cursor-Based Row Encryption Example
-- =============================================
PRINT '--- STEP 2: Cursor-Based Row Encryption Example ---';
GO

-- Create a table to store encrypted rows
IF OBJECT_ID('dbo.EncryptedCustomers', 'U') IS NOT NULL
    DROP TABLE dbo.EncryptedCustomers;
GO

CREATE TABLE dbo.EncryptedCustomers (
    CustomerID INT PRIMARY KEY,
    EncryptedRow NVARCHAR(MAX) NOT NULL,
    EncryptedAt DATETIME2 DEFAULT GETDATE(),
    Password NVARCHAR(100) NOT NULL -- In real scenario, this would be stored securely
);
GO

-- Cursor-based encryption example
PRINT 'Starting cursor-based encryption...';

DECLARE @customerID INT;
DECLARE @rowXml XML;
DECLARE @encryptedRow NVARCHAR(MAX);
DECLARE @password NVARCHAR(MAX) = 'MySecurePassword123!';
DECLARE @processedCount INT = 0;

-- Declare cursor for processing customers
DECLARE customer_cursor CURSOR FOR
    SELECT CustomerID 
    FROM dbo.Customers 
    ORDER BY CustomerID;

OPEN customer_cursor;
FETCH NEXT FROM customer_cursor INTO @customerID;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Get single row as XML (this is the key - always use TOP 1 for single row)
    SET @rowXml = (
        SELECT TOP 1 * 
        FROM dbo.Customers 
        WHERE CustomerID = @customerID 
        FOR XML RAW('Row'), ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE
    );

    -- Encrypt the row
    EXEC dbo.EncryptRowWithMetadata 
        @rowXml = @rowXml,
        @password = @password,
        @iterations = 10000,
        @encryptedRow = @encryptedRow OUTPUT;

    -- Store encrypted row
    INSERT INTO dbo.EncryptedCustomers (CustomerID, EncryptedRow, Password)
    VALUES (@customerID, @encryptedRow, @password);

    SET @processedCount = @processedCount + 1;
    PRINT 'Processed customer ID: ' + CAST(@customerID AS NVARCHAR(10)) + 
          ' (Total: ' + CAST(@processedCount AS NVARCHAR(10)) + ')';

    FETCH NEXT FROM customer_cursor INTO @customerID;
END

CLOSE customer_cursor;
DEALLOCATE customer_cursor;

PRINT '✓ Cursor-based encryption completed. Processed ' + CAST(@processedCount AS NVARCHAR(10)) + ' customers.';
GO

-- =============================================
-- STEP 3: Cursor-Based Row Decryption Example
-- =============================================
PRINT '--- STEP 3: Cursor-Based Row Decryption Example ---';
GO

-- Cursor-based decryption example
PRINT 'Starting cursor-based decryption...';

DECLARE @customerID INT;
DECLARE @encryptedRow NVARCHAR(MAX);
DECLARE @password NVARCHAR(MAX);
DECLARE @decryptedCount INT = 0;

-- Declare cursor for processing encrypted customers
DECLARE encrypted_customer_cursor CURSOR FOR
    SELECT CustomerID, EncryptedRow, Password 
    FROM dbo.EncryptedCustomers 
    ORDER BY CustomerID;

OPEN encrypted_customer_cursor;
FETCH NEXT FROM encrypted_customer_cursor INTO @customerID, @encryptedRow, @password;

WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT 'Decrypting customer ID: ' + CAST(@customerID AS NVARCHAR(10));
    
    -- Decrypt the row and return as result set
    EXEC dbo.DecryptRowWithMetadata 
        @encryptedRow = @encryptedRow,
        @password = @password;

    SET @decryptedCount = @decryptedCount + 1;
    FETCH NEXT FROM encrypted_customer_cursor INTO @customerID, @encryptedRow, @password;
END

CLOSE encrypted_customer_cursor;
DEALLOCATE encrypted_customer_cursor;

PRINT '✓ Cursor-based decryption completed. Processed ' + CAST(@decryptedCount AS NVARCHAR(10)) + ' customers.';
GO

-- =============================================
-- STEP 4: PowerBuilder-Style Integration Example
-- =============================================
PRINT '--- STEP 4: PowerBuilder-Style Integration Example ---';
GO

-- Create a procedure that mimics PowerBuilder's row-by-row processing
IF OBJECT_ID('dbo.ProcessCustomerRow', 'P') IS NOT NULL
    DROP PROCEDURE dbo.ProcessCustomerRow;
GO

CREATE PROCEDURE dbo.ProcessCustomerRow
    @customerID INT,
    @password NVARCHAR(MAX),
    @action NVARCHAR(10) = 'ENCRYPT', -- 'ENCRYPT' or 'DECRYPT'
    @encryptedRow NVARCHAR(MAX) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    IF @action = 'ENCRYPT'
    BEGIN
        -- Get single row as XML
        DECLARE @rowXml XML = (
            SELECT TOP 1 * 
            FROM dbo.Customers 
            WHERE CustomerID = @customerID 
            FOR XML RAW('Row'), ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE
        );

        -- Encrypt the row
        EXEC dbo.EncryptRowWithMetadata 
            @rowXml = @rowXml,
            @password = @password,
            @iterations = 10000,
            @encryptedRow = @encryptedRow OUTPUT;
            
        PRINT 'Encrypted customer ID: ' + CAST(@customerID AS NVARCHAR(10));
    END
    ELSE IF @action = 'DECRYPT'
    BEGIN
        -- Get encrypted row from storage
        SELECT @encryptedRow = EncryptedRow 
        FROM dbo.EncryptedCustomers 
        WHERE CustomerID = @customerID;

        IF @encryptedRow IS NOT NULL
        BEGIN
            -- Decrypt and return as result set
            EXEC dbo.DecryptRowWithMetadata 
                @encryptedRow = @encryptedRow,
                @password = @password;
                
            PRINT 'Decrypted customer ID: ' + CAST(@customerID AS NVARCHAR(10));
        END
        ELSE
        BEGIN
            PRINT 'No encrypted data found for customer ID: ' + CAST(@customerID AS NVARCHAR(10));
        END
    END
END
GO

-- Test the PowerBuilder-style procedure
PRINT 'Testing PowerBuilder-style integration...';

DECLARE @encryptedRowForPB NVARCHAR(MAX);

-- Encrypt a specific customer
EXEC dbo.ProcessCustomerRow
    @customerID = 1,
    @password = 'MySecurePassword123!',
    @action = 'ENCRYPT',
    @encryptedRow = @encryptedRowForPB OUTPUT;

PRINT 'Encrypted row length: ' + CAST(LEN(@encryptedRowForPB) AS NVARCHAR(10));

-- Decrypt the same customer
EXEC dbo.ProcessCustomerRow
    @customerID = 1,
    @password = 'MySecurePassword123!',
    @action = 'DECRYPT',
    @encryptedRow = @encryptedRowForPB OUTPUT;
GO

-- =============================================
-- STEP 5: Performance Comparison
-- =============================================
PRINT '--- STEP 5: Performance Comparison ---';
GO

-- Test single row encryption performance
PRINT 'Testing single row encryption performance...';

DECLARE @startTime DATETIME2 = GETDATE();
DECLARE @rowXml XML;
DECLARE @encryptedRow NVARCHAR(MAX);
DECLARE @password NVARCHAR(MAX) = 'MySecurePassword123!';
DECLARE @iterations INT = 10000;

-- Get single row as XML
SET @rowXml = (
    SELECT TOP 1 * 
    FROM dbo.Customers 
    WHERE CustomerID = 1 
    FOR XML RAW('Row'), ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE
);

-- Encrypt the row
EXEC dbo.EncryptRowWithMetadata 
    @rowXml = @rowXml,
    @password = @password,
    @iterations = @iterations,
    @encryptedRow = @encryptedRow OUTPUT;

DECLARE @endTime DATETIME2 = GETDATE();
DECLARE @duration INT = DATEDIFF(MILLISECOND, @startTime, @endTime);

PRINT 'Single row encryption completed in ' + CAST(@duration AS NVARCHAR(10)) + ' milliseconds';
PRINT 'Encrypted data length: ' + CAST(LEN(@encryptedRow) AS NVARCHAR(10)) + ' characters';

-- Decrypt the row
SET @startTime = GETDATE();

EXEC dbo.DecryptRowWithMetadata 
    @encryptedRow = @encryptedRow,
    @password = @password;

SET @endTime = GETDATE();
SET @duration = DATEDIFF(MILLISECOND, @startTime, @endTime);

PRINT 'Single row decryption completed in ' + CAST(@duration AS NVARCHAR(10)) + ' milliseconds';
GO

-- =============================================
-- SUMMARY
-- =============================================
PRINT '';
PRINT '=== CURSOR USAGE EXAMPLE COMPLETED ===';
PRINT '';
PRINT 'Key Points for Row-by-Row Processing:';
PRINT '  1. Always use SELECT TOP 1 for single row queries';
PRINT '  2. Use cursors for processing multiple rows one at a time';
PRINT '  3. Each row is encrypted/decrypted individually';
PRINT '  4. Perfect for PowerBuilder integration';
PRINT '  5. No need for complex batch processing';
PRINT '  6. Simple and straightforward approach';
PRINT '  7. Easy to debug and maintain';
PRINT '  8. Suitable for real-time applications';
PRINT '';
PRINT 'Example Single Row Query:';
PRINT '  SELECT TOP 1 * FROM dbo.Customers WHERE CustomerID = 1';
PRINT '  FOR XML RAW(''Row''), ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE';
PRINT '';
PRINT 'This approach is ideal for:';
PRINT '  - PowerBuilder applications';
PRINT '  - Real-time data processing';
PRINT '  - Row-by-row validation';
PRINT '  - Interactive user interfaces';
PRINT '  - Cursor-based processing';
GO 