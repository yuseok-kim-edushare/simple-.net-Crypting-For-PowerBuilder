# Usage Examples for Password-Based Table Encryption

This document demonstrates the recommended workflow for encrypting and decrypting table data using the new universal restore procedure. This approach is the most maintainable and provides the best developer experience, as it requires no manual SQL query writing for decryption.

## The Recommended Pattern: Universal Restore Procedure

The workflow is incredibly simple:
1.  **Encrypt** your table into a single string using a password.
2.  **Decrypt** the data and restore the table by executing a single stored procedure.

### Example 1: Full Round-Trip with the Universal Restore Procedure

This example shows the end-to-end process for encrypting and restoring a `Customers` table.

```sql
-- ========= SETUP: Create and encrypt the original data =============

CREATE TABLE #Customers (
    CustomerID INT PRIMARY KEY,
    FirstName NVARCHAR(50),
    Company NVARCHAR(100),
    Email NVARCHAR(100),
    LastLogin DATETIME
);
INSERT INTO #Customers VALUES
(1, 'John Doe', 'ACME Inc.', 'john.doe@acme.com', GETDATE()),
(2, 'Jane Smith', 'Widgets LLC', 'jane.smith@widgets.com', GETDATE()-1),
(3, '김민준', '코리아솔루션', 'mj.kim@koreasolutions.kr', GETDATE()-2);

DECLARE @password NVARCHAR(MAX) = 'VeryStrongP@ssw0rdForCustomers!';

-- Encrypt the entire table into one string
DECLARE @customerXml XML = (SELECT * FROM #Customers FOR XML PATH('Row'), ROOT('Root'));
DECLARE @encryptedCustomers NVARCHAR(MAX) = dbo.EncryptXmlWithPassword(@customerXml, @password);

PRINT 'Customer data has been encrypted.';

-- ========= RECOMMENDED USAGE: Use the Restore Procedure =============

-- This is the only step needed to get the data back in its original structure.
-- It's perfect for PowerBuilder or any other application.
EXEC dbo.RestoreEncryptedTable @encryptedCustomers, @password;
```

### Example 2: Using the Restored Data in a Stored Procedure

You can easily use the restored data inside another stored procedure, for example, to find a specific record.

```sql
CREATE PROCEDURE dbo.sp_GetDecryptedCustomerByID
    @encryptedData NVARCHAR(MAX),
    @password NVARCHAR(MAX),
    @CustomerID INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Create a temporary table to hold the restored structure
    CREATE TABLE #TempCustomers (
        CustomerID NVARCHAR(MAX),
        FirstName NVARCHAR(MAX),
        Company NVARCHAR(MAX),
        Email NVARCHAR(MAX),
        LastLogin NVARCHAR(MAX)
    );

    -- Execute the restore procedure and insert the results
    INSERT INTO #TempCustomers
    EXEC dbo.RestoreEncryptedTable @encryptedData, @password;

    -- Now, query the restored data
    SELECT 
        CAST(CustomerID AS INT) AS CustomerID,
        FirstName,
        Company,
        Email,
        CAST(LastLogin AS DATETIME) AS LastLogin
    FROM #TempCustomers
    WHERE CAST(CustomerID AS INT) = @CustomerID;

    -- Clean up
    DROP TABLE #TempCustomers;
END
GO

-- To use it:
EXEC dbo.sp_GetDecryptedCustomerByID @encryptedCustomers, @password, 2;
```

## Best Practices

1.  **Password Management**: Use strong, unique passwords. Store them securely in a secrets management system (like Azure Key Vault), not in your code.
2.  **XML Structure**: When encrypting, always use `FOR XML PATH('Row'), ROOT('Root')`. The restore procedure expects this specific structure to work correctly.
3.  **Data Types**: The `RestoreEncryptedTable` procedure returns all columns as `NVARCHAR(MAX)`. This ensures it can handle any data type from the source table. When using the restored data, you should `CAST` or `CONVERT` the values back to their original, appropriate data types.
4.  **Error Handling**: If decryption fails (e.g., due to a wrong password), the procedure will send an error message to the client. Your application should be prepared to handle this.
5.  **Permissions**: Using this CLR procedure requires the assembly to have the `UNSAFE` permission set in SQL Server. This is necessary for the procedure to execute dynamic SQL internally.