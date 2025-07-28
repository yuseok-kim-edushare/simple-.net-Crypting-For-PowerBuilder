-- Test Scripts for Password-Based Table Encryption and Decryption
-- This script demonstrates the final, recommended universal restore procedure.

USE [YourDatabase]
GO

PRINT '=== Test 1: Full Table Encryption and Universal Decryption via Stored Procedure ===';

-- Step 1: Create a sample table with data
IF OBJECT_ID('tempdb..#SampleData') IS NOT NULL
    DROP TABLE #SampleData;

CREATE TABLE #SampleData (
    ID INT PRIMARY KEY,
    Name NVARCHAR(100),
    Department NVARCHAR(50),
    Salary DECIMAL(18, 2),
    JoinDate DATETIME
);
INSERT INTO #SampleData VALUES
(1, 'John Doe', 'Engineering', 95000.00, '2023-01-15'),
(2, 'Jane Smith', 'Marketing', 82000.00, '2022-05-20'),
(3, '김민준', 'IT Support', 68000.00, '2023-08-01');

PRINT 'Original data created.';

-- Step 2: Define a user-friendly password
DECLARE @password NVARCHAR(MAX) = 'SuperSecretP@ssw0rdForTesting!';

-- Step 3: Serialize the table to XML and encrypt it with the password
DECLARE @xmlData XML = (SELECT * FROM #SampleData FOR XML PATH('Row'), ROOT('Root'));
DECLARE @encryptedData NVARCHAR(MAX) = dbo.EncryptXmlWithPassword(@xmlData, @password);
PRINT 'Table has been encrypted into a single string using a password.';

-- Step 4: Use the universal restore procedure to get the table back
PRINT 'Restoring the table by executing the stored procedure...';

-- In a real application, you would just execute the procedure.
-- For this test script, we insert the results into a temp table to verify them.
CREATE TABLE #RestoredData (
    ID NVARCHAR(MAX),
    Name NVARCHAR(MAX),
    Department NVARCHAR(MAX),
    Salary NVARCHAR(MAX),
    JoinDate NVARCHAR(MAX)
);

INSERT INTO #RestoredData
EXEC dbo.RestoreEncryptedTable @encryptedData, @password;

PRINT 'Restored data:';
SELECT * FROM #RestoredData;

-- Step 5: Verify data integrity
PRINT 'Verifying data integrity...';

-- Note: All restored columns are NVARCHAR(MAX), so we need to cast for a proper comparison.
IF NOT EXISTS (
    SELECT ID, Name, Department, Salary, JoinDate FROM #SampleData
    EXCEPT
    SELECT 
        CAST(ID AS INT), 
        Name, 
        Department, 
        CAST(Salary AS DECIMAL(18, 2)), 
        CAST(JoinDate AS DATETIME) 
    FROM #RestoredData
) AND NOT EXISTS (
    SELECT 
        CAST(ID AS INT), 
        Name, 
        Department, 
        CAST(Salary AS DECIMAL(18, 2)), 
        CAST(JoinDate AS DATETIME) 
    FROM #RestoredData
    EXCEPT
    SELECT ID, Name, Department, Salary, JoinDate FROM #SampleData
)
    PRINT 'SUCCESS: The restored data matches the original data.';
ELSE
    PRINT 'ERROR: Data mismatch between original and restored tables.';

-- Clean up
DROP TABLE #SampleData;
DROP TABLE #RestoredData;

PRINT '=== Test Completed Successfully ===';
GO