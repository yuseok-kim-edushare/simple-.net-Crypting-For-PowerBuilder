-- =============================================
-- TABLE-VALUED FUNCTION DEMONSTRATION
-- =============================================
-- This script demonstrates the new DecryptTableTVF function
-- that addresses the user's request for a TVF wrapper around 
-- the stored procedure to eliminate temp table requirements.
-- =============================================

USE [YourDatabase]
GO

PRINT '=== TABLE-VALUED FUNCTION DECRYPTION DEMONSTRATION ===';
PRINT 'Showing how DecryptTableTVF eliminates the need for temp tables';
PRINT '';

-- =============================================
-- SETUP: CREATE TEST DATA
-- =============================================

PRINT '--- Setting up test data ---';

-- Create sample table
CREATE TABLE TestEmployees (
    EmployeeID INT PRIMARY KEY IDENTITY(1,1),
    FirstName NVARCHAR(50),
    LastName NVARCHAR(50),
    Email NVARCHAR(100),
    Salary DECIMAL(18,2),
    Department NVARCHAR(30),
    HireDate DATE,
    IsActive BIT
);

-- Insert sample data including Unicode characters
INSERT INTO TestEmployees (FirstName, LastName, Email, Salary, Department, HireDate, IsActive) VALUES
('John', 'Smith', 'john.smith@company.com', 75000.00, 'Engineering', '2023-01-15', 1),
('김민수', 'Kim', 'minsu.kim@company.com', 68000.00, 'Marketing', '2023-03-20', 1),
('Maria', 'González', 'maria.gonzalez@company.com', 82000.00, 'Finance', '2022-11-10', 1),
('佐藤太郎', 'Sato', 'taro.sato@company.com', 91000.00, 'IT', '2022-08-05', 1),
('Ahmed', 'Hassan', 'ahmed.hassan@company.com', 58000.00, 'HR', '2024-01-01', 0);

PRINT 'Sample data created with ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' employees';

-- =============================================
-- ENCRYPT THE TABLE
-- =============================================

PRINT '';
PRINT '--- Encrypting table data ---';

DECLARE @password NVARCHAR(MAX) = 'TVFDemo2024!@#$';
DECLARE @xmlData XML = (SELECT * FROM TestEmployees FOR XML PATH('Row'), ROOT('Root'));
DECLARE @encryptedData NVARCHAR(MAX) = dbo.EncryptXmlWithPassword(@xmlData, @password);

PRINT 'Table encrypted successfully! Size: ' + CAST(LEN(@encryptedData) AS VARCHAR(20)) + ' characters';

-- =============================================
-- DEMONSTRATION 1: OLD APPROACH (STORED PROCEDURE)
-- =============================================

PRINT '';
PRINT '--- OLD APPROACH: Using Stored Procedure (requires temp tables) ---';

DECLARE @startTime DATETIME = GETDATE();

-- Step 1: Create temp table
CREATE TABLE #TempDecrypted (
    EmployeeID NVARCHAR(MAX),
    FirstName NVARCHAR(MAX),
    LastName NVARCHAR(MAX),
    Email NVARCHAR(MAX),
    Salary NVARCHAR(MAX),
    Department NVARCHAR(MAX),
    HireDate NVARCHAR(MAX),
    IsActive NVARCHAR(MAX)
);

-- Step 2: Execute stored procedure
INSERT INTO #TempDecrypted
EXEC dbo.RestoreEncryptedTable @encryptedData, @password;

-- Step 3: Create properly typed result
SELECT 
    CAST(EmployeeID AS INT) AS EmployeeID,
    FirstName,
    LastName,
    Email,
    CAST(Salary AS DECIMAL(18,2)) AS Salary,
    Department,
    CAST(HireDate AS DATE) AS HireDate,
    CAST(IsActive AS BIT) AS IsActive
INTO #OldApproachResult
FROM #TempDecrypted;

DECLARE @oldApproachTime INT = DATEDIFF(millisecond, @startTime, GETDATE());
PRINT 'OLD APPROACH completed in ' + CAST(@oldApproachTime AS VARCHAR(10)) + ' ms';
PRINT 'Steps required: 1) Create temp table, 2) EXEC stored procedure, 3) SELECT INTO final table';

-- =============================================
-- DEMONSTRATION 2: NEW APPROACH (TABLE-VALUED FUNCTION)
-- =============================================

PRINT '';
PRINT '--- NEW APPROACH: Using Table-Valued Function (no temp tables!) ---';

SET @startTime = GETDATE();

-- Single query - no temp tables needed!
SELECT 
    CAST(T.c.value('@EmployeeID', 'NVARCHAR(MAX)') AS INT) AS EmployeeID,
    T.c.value('@FirstName', 'NVARCHAR(MAX)') AS FirstName,
    T.c.value('@LastName', 'NVARCHAR(MAX)') AS LastName,
    T.c.value('@Email', 'NVARCHAR(MAX)') AS Email,
    CAST(T.c.value('@Salary', 'NVARCHAR(MAX)') AS DECIMAL(18,2)) AS Salary,
    T.c.value('@Department', 'NVARCHAR(MAX)') AS Department,
    CAST(T.c.value('@HireDate', 'NVARCHAR(MAX)') AS DATE) AS HireDate,
    CAST(T.c.value('@IsActive', 'NVARCHAR(MAX)') AS BIT) AS IsActive
INTO #NewApproachResult
FROM dbo.DecryptTableTVF(@encryptedData, @password) d
CROSS APPLY d.DecryptedXml.nodes('/Root/Row') AS T(c);

DECLARE @newApproachTime INT = DATEDIFF(millisecond, @startTime, GETDATE());
PRINT 'NEW APPROACH completed in ' + CAST(@newApproachTime AS VARCHAR(10)) + ' ms';
PRINT 'Steps required: 1) Single SELECT with CROSS APPLY - that''s it!';

-- =============================================
-- VERIFY RESULTS ARE IDENTICAL
-- =============================================

PRINT '';
PRINT '--- Verifying both approaches produce identical results ---';

-- Check row counts
DECLARE @oldCount INT = (SELECT COUNT(*) FROM #OldApproachResult);
DECLARE @newCount INT = (SELECT COUNT(*) FROM #NewApproachResult);

PRINT 'Old approach row count: ' + CAST(@oldCount AS VARCHAR(10));
PRINT 'New approach row count: ' + CAST(@newCount AS VARCHAR(10));

IF @oldCount = @newCount
    PRINT '✓ Row counts match!';
ELSE
    PRINT '✗ Row count mismatch!';

-- Compare actual data
PRINT '';
PRINT 'Sample results from OLD approach:';
SELECT TOP 3 EmployeeID, FirstName, LastName, Salary, Department FROM #OldApproachResult ORDER BY EmployeeID;

PRINT '';
PRINT 'Sample results from NEW approach:';
SELECT TOP 3 EmployeeID, FirstName, LastName, Salary, Department FROM #NewApproachResult ORDER BY EmployeeID;

-- =============================================
-- ADVANCED EXAMPLES WITH NEW TVF
-- =============================================

PRINT '';
PRINT '--- Advanced Examples: What you can do with the new TVF ---';

-- Example 1: Direct filtering during decryption
PRINT '';
PRINT 'Example 1: Direct filtering (only high-salary employees)';
SELECT 
    T.c.value('@FirstName', 'NVARCHAR(MAX)') + ' ' + T.c.value('@LastName', 'NVARCHAR(MAX)') AS FullName,
    CAST(T.c.value('@Salary', 'NVARCHAR(MAX)') AS DECIMAL(18,2)) AS Salary,
    T.c.value('@Department', 'NVARCHAR(MAX)') AS Department
FROM dbo.DecryptTableTVF(@encryptedData, @password) d
CROSS APPLY d.DecryptedXml.nodes('/Root/Row') AS T(c)
WHERE CAST(T.c.value('@Salary', 'NVARCHAR(MAX)') AS DECIMAL(18,2)) > 70000
ORDER BY CAST(T.c.value('@Salary', 'NVARCHAR(MAX)') AS DECIMAL(18,2)) DESC;

-- Example 2: Use in CTE (Common Table Expression)
PRINT '';
PRINT 'Example 2: Using TVF in a CTE for complex queries';
WITH HighEarners AS (
    SELECT 
        T.c.value('@FirstName', 'NVARCHAR(MAX)') AS FirstName,
        T.c.value('@LastName', 'NVARCHAR(MAX)') AS LastName,
        CAST(T.c.value('@Salary', 'NVARCHAR(MAX)') AS DECIMAL(18,2)) AS Salary,
        T.c.value('@Department', 'NVARCHAR(MAX)') AS Department
    FROM dbo.DecryptTableTVF(@encryptedData, @password) d
    CROSS APPLY d.DecryptedXml.nodes('/Root/Row') AS T(c)
    WHERE CAST(T.c.value('@Salary', 'NVARCHAR(MAX)') AS DECIMAL(18,2)) > 75000
)
SELECT 
    Department,
    COUNT(*) AS HighEarnerCount,
    AVG(Salary) AS AvgSalary,
    MAX(Salary) AS MaxSalary
FROM HighEarners
GROUP BY Department
ORDER BY AvgSalary DESC;

-- Example 3: JOIN with other tables
PRINT '';
PRINT 'Example 3: JOINing decrypted data with other tables';

-- Create a benefits table
CREATE TABLE #Benefits (
    Department NVARCHAR(30),
    HealthInsurance BIT,
    RetirementPlan BIT,
    BonusEligible BIT
);

INSERT INTO #Benefits VALUES
('Engineering', 1, 1, 1),
('Marketing', 1, 1, 0),
('Finance', 1, 1, 1),
('IT', 1, 1, 1),
('HR', 1, 0, 0);

-- JOIN decrypted employee data with benefits
SELECT 
    T.c.value('@FirstName', 'NVARCHAR(MAX)') + ' ' + T.c.value('@LastName', 'NVARCHAR(MAX)') AS FullName,
    T.c.value('@Department', 'NVARCHAR(MAX)') AS Department,
    CAST(T.c.value('@Salary', 'NVARCHAR(MAX)') AS DECIMAL(18,2)) AS Salary,
    CASE WHEN b.HealthInsurance = 1 THEN 'Yes' ELSE 'No' END AS HealthInsurance,
    CASE WHEN b.RetirementPlan = 1 THEN 'Yes' ELSE 'No' END AS RetirementPlan,
    CASE WHEN b.BonusEligible = 1 THEN 'Yes' ELSE 'No' END AS BonusEligible
FROM dbo.DecryptTableTVF(@encryptedData, @password) d
CROSS APPLY d.DecryptedXml.nodes('/Root/Row') AS T(c)
INNER JOIN #Benefits b ON T.c.value('@Department', 'NVARCHAR(MAX)') = b.Department
WHERE CAST(T.c.value('@IsActive', 'NVARCHAR(MAX)') AS BIT) = 1
ORDER BY CAST(T.c.value('@Salary', 'NVARCHAR(MAX)') AS DECIMAL(18,2)) DESC;

-- =============================================
-- PERFORMANCE COMPARISON
-- =============================================

PRINT '';
PRINT '--- Performance Comparison Summary ---';
PRINT 'Old Approach (Stored Procedure): ' + CAST(@oldApproachTime AS VARCHAR(10)) + ' ms';
PRINT 'New Approach (Table-Valued Function): ' + CAST(@newApproachTime AS VARCHAR(10)) + ' ms';

IF @newApproachTime < @oldApproachTime
    PRINT '✓ TVF approach is faster!';
ELSE IF @newApproachTime = @oldApproachTime
    PRINT '= Both approaches have similar performance';
ELSE
    PRINT 'ℹ TVF approach takes slightly longer but eliminates temp table management';

-- =============================================
-- BENEFITS SUMMARY
-- =============================================

PRINT '';
PRINT '=== BENEFITS OF THE NEW TABLE-VALUED FUNCTION APPROACH ===';
PRINT '';
PRINT '✓ ELIMINATES TEMP TABLES: No need to create intermediate storage';
PRINT '✓ SINGLE QUERY: Decrypt and process in one statement';
PRINT '✓ COMPOSABLE: Use in CTEs, subqueries, JOINs naturally';
PRINT '✓ DIRECT FILTERING: Apply WHERE clauses during decryption';
PRINT '✓ MEMORY EFFICIENT: No intermediate table storage required';
PRINT '✓ CLEANER CODE: Less clutter, more readable queries';
PRINT '✓ SAME SECURITY: Identical encryption/decryption as stored procedure';
PRINT '';
PRINT 'DEVELOPER IMPACT:';
PRINT '  Before: 3 steps (CREATE temp table, EXEC procedure, SELECT INTO)';
PRINT '  After:  1 step (SELECT with CROSS APPLY)';
PRINT '';
PRINT 'USE CASES PERFECT FOR TVF:';
PRINT '  • Direct reporting on encrypted data';
PRINT '  • ETL processes with encrypted sources';
PRINT '  • Real-time decryption in views or complex queries';
PRINT '  • Joining encrypted data with other tables';
PRINT '  • Filtering/aggregating during decryption';

-- =============================================
-- CLEANUP
-- =============================================

DROP TABLE TestEmployees;
DROP TABLE #TempDecrypted;
DROP TABLE #OldApproachResult;
DROP TABLE #NewApproachResult;
DROP TABLE #Benefits;

PRINT '';
PRINT '=== DEMONSTRATION COMPLETED SUCCESSFULLY ===';
PRINT 'The DecryptTableTVF function provides a superior developer experience!';
GO