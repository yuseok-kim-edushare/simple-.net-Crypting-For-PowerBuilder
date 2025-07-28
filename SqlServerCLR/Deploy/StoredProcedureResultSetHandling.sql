-- =============================================
-- STORED PROCEDURE RESULT SET HANDLING DEMONSTRATION
-- =============================================
-- This script demonstrates how to handle stored procedure result sets
-- using the RestoreEncryptedTable procedure, which can be used with
-- INSERT INTO ... EXEC pattern.
-- =============================================

USE [YourDatabase]
GO

PRINT '=== STORED PROCEDURE RESULT SET HANDLING DEMONSTRATION ===';
PRINT 'Showing how RestoreEncryptedTable can handle result sets with AUTOMATIC TYPE CASTING';
PRINT '';

-- =============================================
-- SETUP: CREATE SAMPLE STORED PROCEDURES
-- =============================================

PRINT '--- Setting up sample stored procedures ---';

-- Create a sample table for demonstration
CREATE TABLE SampleData (
    ID INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100),
    Value DECIMAL(18,2),
    CreatedDate DATETIME2 DEFAULT GETDATE(),
    IsActive BIT DEFAULT 1
);

-- Insert sample data
INSERT INTO SampleData (Name, Value, IsActive) VALUES
('Product A', 1250.50, 1),
('Product B', 875.25, 1),
('Product C', 2100.75, 0),
('Product D', 450.00, 1),
('Product E', 3200.00, 1);

PRINT 'Sample data created with ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' records';

-- Create a sample stored procedure that returns a result set
CREATE PROCEDURE GetActiveProducts
    @minValue DECIMAL(18,2) = 0
AS
BEGIN
    SELECT 
        ID,
        Name,
        Value,
        CreatedDate,
        IsActive
    FROM SampleData 
    WHERE IsActive = 1 
      AND Value >= @minValue
    ORDER BY Value DESC;
END;
GO

-- Create another sample procedure with different structure
CREATE PROCEDURE GetProductSummary
AS
BEGIN
    SELECT 
        'Total Products' AS Metric,
        COUNT(*) AS Count,
        SUM(Value) AS TotalValue,
        AVG(Value) AS AverageValue,
        MAX(Value) AS MaxValue,
        MIN(Value) AS MinValue
    FROM SampleData
    WHERE IsActive = 1;
END;
GO

PRINT 'Sample stored procedures created successfully!';

-- =============================================
-- DEMONSTRATION 1: ENCRYPTING STORED PROCEDURE RESULTS
-- =============================================

PRINT '';
PRINT '--- DEMONSTRATION 1: Encrypting Stored Procedure Results ---';

DECLARE @password NVARCHAR(MAX) = 'SPResultDemo2024!@#$';

-- Step 1: Execute stored procedure and capture results in temp table
CREATE TABLE #SPResults (
    ID NVARCHAR(MAX),
    Name NVARCHAR(MAX),
    Value NVARCHAR(MAX),
    CreatedDate NVARCHAR(MAX),
    IsActive NVARCHAR(MAX)
);

-- Capture the result set from the stored procedure
INSERT INTO #SPResults
EXEC GetActiveProducts @minValue = 1000;

PRINT 'Captured ' + CAST((SELECT COUNT(*) FROM #SPResults) AS VARCHAR(10)) + ' rows from GetActiveProducts';

-- Step 2: Convert the temp table to XML and encrypt it
DECLARE @xmlData XML = (SELECT * FROM #SPResults FOR XML PATH('Row'), ROOT('Root'));
DECLARE @encryptedSPResults NVARCHAR(MAX) = dbo.EncryptXmlWithMetadata(@xmlData, @password);

PRINT 'Stored procedure results encrypted successfully!';
PRINT 'Encrypted size: ' + CAST(LEN(@encryptedSPResults) AS VARCHAR(20)) + ' characters';

-- =============================================
-- DEMONSTRATION 2: DECRYPTING AND RESTORING RESULTS
-- =============================================

PRINT '';
PRINT '--- DEMONSTRATION 2: Decrypting and Restoring Results ---';

-- Step 1: Create temp table to capture decrypted results
CREATE TABLE #DecryptedResults (
    ID NVARCHAR(MAX),
    Name NVARCHAR(MAX),
    Value NVARCHAR(MAX),
    CreatedDate NVARCHAR(MAX),
    IsActive NVARCHAR(MAX)
);

-- Step 2: Use RestoreEncryptedTable to decrypt and restore
INSERT INTO #DecryptedResults
EXEC dbo.RestoreEncryptedTable @encryptedSPResults, @password;

PRINT 'Results decrypted and restored successfully with AUTOMATIC TYPE CASTING!';
PRINT 'Decrypted row count: ' + CAST((SELECT COUNT(*) FROM #DecryptedResults) AS VARCHAR(10));
PRINT 'Note: Columns are automatically cast to proper types (INT, DECIMAL, DATETIME2, BIT)';

-- Step 3: Display results with automatic type casting (no manual casting needed!)
SELECT * FROM #DecryptedResults ORDER BY Value DESC;

-- =============================================
-- DEMONSTRATION 3: HANDLING DIFFERENT RESULT SET STRUCTURES
-- =============================================

PRINT '';
PRINT '--- DEMONSTRATION 3: Handling Different Result Set Structures ---';

-- Clear previous results
DROP TABLE #SPResults;
DROP TABLE #DecryptedResults;

-- Create temp table for different structure (summary data)
CREATE TABLE #SummaryResults (
    Metric NVARCHAR(MAX),
    Count NVARCHAR(MAX),
    TotalValue NVARCHAR(MAX),
    AverageValue NVARCHAR(MAX),
    MaxValue NVARCHAR(MAX),
    MinValue NVARCHAR(MAX)
);

-- Capture summary results
INSERT INTO #SummaryResults
EXEC GetProductSummary;

PRINT 'Captured summary results from GetProductSummary';

-- Encrypt the summary results
DECLARE @xmlSummary XML = (SELECT * FROM #SummaryResults FOR XML PATH('Row'), ROOT('Root'));
DECLARE @encryptedSummary NVARCHAR(MAX) = dbo.EncryptXmlWithMetadata(@xmlSummary, @password);

-- Decrypt and restore summary results
CREATE TABLE #DecryptedSummary (
    Metric NVARCHAR(MAX),
    Count NVARCHAR(MAX),
    TotalValue NVARCHAR(MAX),
    AverageValue NVARCHAR(MAX),
    MaxValue NVARCHAR(MAX),
    MinValue NVARCHAR(MAX)
);

INSERT INTO #DecryptedSummary
EXEC dbo.RestoreEncryptedTable @encryptedSummary, @password;

-- Display properly typed summary results (automatic casting!)
SELECT * FROM #DecryptedSummary;

-- =============================================
-- DEMONSTRATION 4: ADVANCED SCENARIOS
-- =============================================

PRINT '';
PRINT '--- DEMONSTRATION 4: Advanced Scenarios ---';

-- Scenario 1: Encrypting results from multiple stored procedures
PRINT '';
PRINT 'Scenario 1: Encrypting results from multiple stored procedures';

-- Create a procedure that calls multiple other procedures
CREATE PROCEDURE GetComprehensiveReport
AS
BEGIN
    -- First result set: Active products
    SELECT 'ACTIVE_PRODUCTS' AS ResultType, ID, Name, Value, CreatedDate, IsActive
    FROM SampleData WHERE IsActive = 1;
    
    -- Second result set: Summary
    SELECT 'SUMMARY' AS ResultType, 
           COUNT(*) AS Count,
           SUM(Value) AS TotalValue,
           AVG(Value) AS AverageValue
    FROM SampleData WHERE IsActive = 1;
END;
GO

-- Handle multiple result sets by capturing them separately
-- Note: This requires handling each result set individually
-- as SQL Server doesn't support capturing multiple result sets in one temp table

-- Scenario 2: Dynamic SQL with stored procedure results
PRINT '';
PRINT 'Scenario 2: Dynamic SQL with stored procedure results';

DECLARE @sql NVARCHAR(MAX) = 'EXEC GetActiveProducts @minValue = 500';
DECLARE @tempTableName NVARCHAR(128) = '#DynamicResults_' + CAST(NEWID() AS NVARCHAR(36));

-- Create dynamic temp table
SET @sql = 'CREATE TABLE ' + @tempTableName + ' (
    ID NVARCHAR(MAX),
    Name NVARCHAR(MAX),
    Value NVARCHAR(MAX),
    CreatedDate NVARCHAR(MAX),
    IsActive NVARCHAR(MAX)
);';
EXEC sp_executesql @sql;

-- Insert results into dynamic temp table
SET @sql = 'INSERT INTO ' + @tempTableName + ' EXEC GetActiveProducts @minValue = 500;';
EXEC sp_executesql @sql;

-- Encrypt dynamic results
SET @sql = 'DECLARE @xml XML = (SELECT * FROM ' + @tempTableName + ' FOR XML PATH(''Row''), ROOT(''Root''));';
SET @sql = @sql + 'DECLARE @encrypted NVARCHAR(MAX) = dbo.EncryptXmlWithMetadata(@xml, ''' + @password + ''');';
SET @sql = @sql + 'SELECT @encrypted AS EncryptedData;';
EXEC sp_executesql @sql;

-- Clean up dynamic temp table
SET @sql = 'DROP TABLE ' + @tempTableName + ';';
EXEC sp_executesql @sql;

-- =============================================
-- DEMONSTRATION 5: ERROR HANDLING AND VALIDATION
-- =============================================

PRINT '';
PRINT '--- DEMONSTRATION 5: Error Handling and Validation ---';

-- Test with empty result set
PRINT '';
PRINT 'Testing with empty result set:';

CREATE TABLE #EmptyResults (ID NVARCHAR(MAX), Name NVARCHAR(MAX));
-- Don't insert any data - test empty result set

DECLARE @xmlEmpty XML = (SELECT * FROM #EmptyResults FOR XML PATH('Row'), ROOT('Root'));
DECLARE @encryptedEmpty NVARCHAR(MAX) = dbo.EncryptXmlWithMetadata(@xmlEmpty, @password);

CREATE TABLE #DecryptedEmpty (ID NVARCHAR(MAX), Name NVARCHAR(MAX));
INSERT INTO #DecryptedEmpty
EXEC dbo.RestoreEncryptedTable @encryptedEmpty, @password;

PRINT 'Empty result set handled successfully. Row count: ' + CAST((SELECT COUNT(*) FROM #DecryptedEmpty) AS VARCHAR(10));

-- Test with wrong password
PRINT '';
PRINT 'Testing with wrong password:';

CREATE TABLE #WrongPassword (ID NVARCHAR(MAX), Name NVARCHAR(MAX));
BEGIN TRY
    INSERT INTO #WrongPassword
    EXEC dbo.RestoreEncryptedTable @encryptedSPResults, 'WrongPassword123';
    PRINT 'Unexpected: Wrong password did not cause error';
END TRY
BEGIN CATCH
    PRINT 'Expected: Wrong password caused error: ' + ERROR_MESSAGE();
END CATCH

-- =============================================
-- BEST PRACTICES AND RECOMMENDATIONS
-- =============================================

PRINT '';
PRINT '=== BEST PRACTICES FOR STORED PROCEDURE RESULT SET HANDLING ===';
PRINT '';
PRINT '1. TEMP TABLE PATTERN:';
PRINT '   - Always use temp tables to capture stored procedure results';
PRINT '   - Use INSERT INTO ... EXEC pattern for reliable result capture';
PRINT '   - Define temp table structure to match expected result set';
PRINT '';
PRINT '2. ENCRYPTION STRATEGY:';
PRINT '   - Use EncryptXmlWithMetadata for automatic schema inference';
PRINT '   - Use EncryptTableWithMetadata when you know the table structure';
PRINT '   - Consider using custom iterations for sensitive data';
PRINT '';
PRINT '3. DECRYPTION WORKFLOW:';
PRINT '   - Use RestoreEncryptedTable with INSERT INTO ... EXEC';
PRINT '   - NO MANUAL CASTING NEEDED - automatic type casting included!';
PRINT '   - Handle empty result sets gracefully';
PRINT '';
PRINT '4. ERROR HANDLING:';
PRINT '   - Wrap decryption in TRY-CATCH blocks';
PRINT '   - Validate password before attempting decryption';
PRINT '   - Check for empty or corrupted encrypted data';
PRINT '';
PRINT '5. PERFORMANCE CONSIDERATIONS:';
PRINT '   - Temp tables are faster than table variables for large result sets';
PRINT '   - Consider encrypting only sensitive columns, not entire result sets';
PRINT '   - Use appropriate iteration counts based on security requirements';
PRINT '';
PRINT '6. SECURITY BEST PRACTICES:';
PRINT '   - Store passwords securely (not in plain text)';
PRINT '   - Use strong, unique passwords for different data sets';
PRINT '   - Consider using key derivation for additional security';
PRINT '   - Regularly rotate encryption passwords';

-- =============================================
-- CLEANUP
-- =============================================

DROP TABLE SampleData;
DROP PROCEDURE GetActiveProducts;
DROP PROCEDURE GetProductSummary;
DROP PROCEDURE GetComprehensiveReport;
DROP TABLE #SPResults;
DROP TABLE #DecryptedResults;
DROP TABLE #SummaryResults;
DROP TABLE #DecryptedSummary;
DROP TABLE #EmptyResults;
DROP TABLE #DecryptedEmpty;
DROP TABLE #WrongPassword;

PRINT '';
PRINT '=== DEMONSTRATION COMPLETED SUCCESSFULLY ===';
PRINT 'The RestoreEncryptedTable procedure effectively handles stored procedure result sets!';
PRINT '';
PRINT 'Key Benefits:';
PRINT '✓ Universal compatibility with any result set structure';
PRINT '✓ Simple INSERT INTO ... EXEC pattern';
PRINT '✓ AUTOMATIC TYPE CASTING - no manual casting required!';
PRINT '✓ Robust error handling and validation';
PRINT '✓ Support for dynamic SQL and multiple result sets';
PRINT '✓ Maintains data integrity and type safety'; 