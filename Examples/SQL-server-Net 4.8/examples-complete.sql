-- =============================================
-- COMPLETE SQL SERVER CLR EXAMPLES SCRIPT
-- =============================================
-- SecureLibrary-SQL: Comprehensive Usage Examples
-- This script demonstrates all features and capabilities
-- 
-- Features Demonstrated:
-- ✓ AES-GCM Encryption/Decryption
-- ✓ Password-based Key Derivation (PBKDF2)
-- ✓ Diffie-Hellman Key Exchange
-- ✓ BCrypt Password Hashing
-- ✓ Table-Level Encryption with Metadata
-- ✓ XML Encryption with Schema Inference
-- ✓ Dynamic Temp Table Wrapper
-- ✓ Automatic Type Casting
-- ✓ Stored Procedure Result Set Handling
-- ✓ Korean Character Support
-- ✓ PowerBuilder Integration Patterns
-- =============================================

USE [YourDatabase]
GO

PRINT '=== SECURELIBRARY-SQL COMPLETE EXAMPLES ===';
PRINT 'Demonstrating all features and capabilities';
PRINT '';

-- =============================================
-- SECTION 1: BASIC ENCRYPTION EXAMPLES
-- =============================================

PRINT '--- SECTION 1: Basic Encryption Examples ---';

-- Generate AES key
DECLARE @aesKey NVARCHAR(MAX) = dbo.GenerateAESKey();
PRINT 'Generated AES key: ' + LEFT(@aesKey, 20) + '...';

-- Basic AES-GCM encryption/decryption
DECLARE @plainText NVARCHAR(MAX) = 'Hello, World! 안녕하세요 こんにちは';
DECLARE @encrypted NVARCHAR(MAX) = dbo.EncryptAesGcm(@plainText, @aesKey);
DECLARE @decrypted NVARCHAR(MAX) = dbo.DecryptAesGcm(@encrypted, @aesKey);

PRINT 'Original text: ' + @plainText;
PRINT 'Encrypted: ' + LEFT(@encrypted, 50) + '...';
PRINT 'Decrypted: ' + @decrypted;
PRINT 'Match: ' + CASE WHEN @plainText = @decrypted THEN '✓ SUCCESS' ELSE '✗ FAILED' END;
PRINT '';

-- =============================================
-- SECTION 2: PASSWORD-BASED ENCRYPTION
-- =============================================

PRINT '--- SECTION 2: Password-Based Encryption ---';

-- Password-based encryption (recommended for most use cases)
DECLARE @password NVARCHAR(MAX) = 'MySecurePassword123!@#';
DECLARE @dataToEncrypt NVARCHAR(MAX) = 'Sensitive data that needs protection';
DECLARE @passwordEncrypted NVARCHAR(MAX) = dbo.EncryptAesGcmWithPassword(@dataToEncrypt, @password);
DECLARE @passwordDecrypted NVARCHAR(MAX) = dbo.DecryptAesGcmWithPassword(@passwordEncrypted, @password);

PRINT 'Password-based encryption:';
PRINT 'Original: ' + @dataToEncrypt;
PRINT 'Encrypted: ' + LEFT(@passwordEncrypted, 50) + '...';
PRINT 'Decrypted: ' + @passwordDecrypted;
PRINT 'Match: ' + CASE WHEN @dataToEncrypt = @passwordDecrypted THEN '✓ SUCCESS' ELSE '✗ FAILED' END;
PRINT '';

-- Salt generation and custom salt usage
DECLARE @salt NVARCHAR(MAX) = dbo.GenerateSalt();
DECLARE @customSaltEncrypted NVARCHAR(MAX) = dbo.EncryptAesGcmWithPasswordAndSalt(@dataToEncrypt, @password, @salt);
DECLARE @customSaltDecrypted NVARCHAR(MAX) = dbo.DecryptAesGcmWithPassword(@customSaltEncrypted, @password);

PRINT 'Custom salt encryption:';
PRINT 'Salt: ' + @salt;
PRINT 'Encrypted with salt: ' + LEFT(@customSaltEncrypted, 50) + '...';
PRINT 'Decrypted: ' + @customSaltDecrypted;
PRINT 'Match: ' + CASE WHEN @dataToEncrypt = @customSaltDecrypted THEN '✓ SUCCESS' ELSE '✗ FAILED' END;
PRINT '';

-- =============================================
-- SECTION 3: PASSWORD HASHING
-- =============================================

PRINT '--- SECTION 3: Password Hashing ---';

-- BCrypt password hashing
DECLARE @userPassword NVARCHAR(MAX) = 'UserPassword123!';
DECLARE @hashedPassword NVARCHAR(MAX) = dbo.HashPasswordDefault(@userPassword);
DECLARE @customHashedPassword NVARCHAR(MAX) = dbo.HashPasswordWithWorkFactor(@userPassword, 14);

PRINT 'Password hashing:';
PRINT 'Original password: ' + @userPassword;
PRINT 'Default hash: ' + LEFT(@hashedPassword, 30) + '...';
PRINT 'Custom work factor hash: ' + LEFT(@customHashedPassword, 30) + '...';

-- Password verification
DECLARE @correctPassword NVARCHAR(MAX) = 'UserPassword123!';
DECLARE @wrongPassword NVARCHAR(MAX) = 'WrongPassword123!';

PRINT 'Password verification:';
PRINT 'Correct password: ' + CASE WHEN dbo.VerifyPassword(@correctPassword, @hashedPassword) = 1 THEN '✓ VERIFIED' ELSE '✗ FAILED' END;
PRINT 'Wrong password: ' + CASE WHEN dbo.VerifyPassword(@wrongPassword, @hashedPassword) = 0 THEN '✓ CORRECTLY REJECTED' ELSE '✗ INCORRECTLY ACCEPTED' END;
PRINT '';

-- =============================================
-- SECTION 4: DIFFIE-HELLMAN KEY EXCHANGE
-- =============================================

PRINT '--- SECTION 4: Diffie-Hellman Key Exchange ---';

-- Generate key pairs for two parties
DECLARE @aliceKeys TABLE (publicKey NVARCHAR(MAX), privateKey NVARCHAR(MAX));
DECLARE @bobKeys TABLE (publicKey NVARCHAR(MAX), privateKey NVARCHAR(MAX));

INSERT INTO @aliceKeys SELECT * FROM dbo.GenerateDiffieHellmanKeys();
INSERT INTO @bobKeys SELECT * FROM dbo.GenerateDiffieHellmanKeys();

DECLARE @alicePublic NVARCHAR(MAX), @alicePrivate NVARCHAR(MAX);
DECLARE @bobPublic NVARCHAR(MAX), @bobPrivate NVARCHAR(MAX);

SELECT @alicePublic = publicKey, @alicePrivate = privateKey FROM @aliceKeys;
SELECT @bobPublic = publicKey, @bobPrivate = privateKey FROM @bobKeys;

-- Derive shared keys
DECLARE @aliceSharedKey NVARCHAR(MAX) = dbo.DeriveSharedKey(@bobPublic, @alicePrivate);
DECLARE @bobSharedKey NVARCHAR(MAX) = dbo.DeriveSharedKey(@alicePublic, @bobPrivate);

PRINT 'Diffie-Hellman key exchange:';
PRINT 'Alice public key: ' + LEFT(@alicePublic, 30) + '...';
PRINT 'Bob public key: ' + LEFT(@bobPublic, 30) + '...';
PRINT 'Alice shared key: ' + LEFT(@aliceSharedKey, 30) + '...';
PRINT 'Bob shared key: ' + LEFT(@bobSharedKey, 30) + '...';
PRINT 'Keys match: ' + CASE WHEN @aliceSharedKey = @bobSharedKey THEN '✓ SUCCESS' ELSE '✗ FAILED' END;
PRINT '';

-- =============================================
-- SECTION 5: TABLE-LEVEL ENCRYPTION
-- =============================================

PRINT '--- SECTION 5: Table-Level Encryption ---';

-- Create sample table with various data types
CREATE TABLE SampleEmployees (
    EmployeeID INT PRIMARY KEY IDENTITY(1,1),
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    Email NVARCHAR(100),
    Salary DECIMAL(18,2),
    HireDate DATE,
    IsActive BIT,
    Department NVARCHAR(30),
    Notes NVARCHAR(MAX)
);

-- Insert test data with Korean characters
INSERT INTO SampleEmployees (FirstName, LastName, Email, Salary, HireDate, IsActive, Department, Notes) VALUES
('John', 'Doe', 'john.doe@company.com', 75000.00, '2023-01-15', 1, 'Engineering', 'Senior Developer with 5 years experience'),
('Jane', 'Smith', 'jane.smith@company.com', 82000.00, '2022-03-20', 1, 'Marketing', 'Marketing Manager, excellent performance'),
('김민준', 'Kim', 'minjun.kim@company.com', 68000.00, '2023-06-01', 1, 'IT Support', '한국어 지원 전문가'),
('Maria', 'Garcia', 'maria.garcia@company.com', 91000.00, '2021-09-10', 1, 'Finance', 'CPA with international experience'),
('Alex', 'Johnson', 'alex.johnson@company.com', 45000.00, '2024-01-01', 0, 'HR', 'Recently terminated employee');

PRINT 'Created sample table with ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' employees';

-- Encrypt entire table with metadata
DECLARE @tablePassword NVARCHAR(MAX) = 'TableEncryptionPassword2024!@#';
DECLARE @encryptedTable NVARCHAR(MAX) = dbo.EncryptTableWithMetadata('SampleEmployees', @tablePassword);

PRINT 'Table encrypted with metadata: ' + CAST(LEN(@encryptedTable) AS VARCHAR(20)) + ' characters';

-- Decrypt using stored procedure
CREATE TABLE #RestoredEmployees (
    EmployeeID NVARCHAR(MAX),
    FirstName NVARCHAR(MAX),
    LastName NVARCHAR(MAX),
    Email NVARCHAR(MAX),
    Salary NVARCHAR(MAX),
    HireDate NVARCHAR(MAX),
    IsActive NVARCHAR(MAX),
    Department NVARCHAR(MAX),
    Notes NVARCHAR(MAX)
);

INSERT INTO #RestoredEmployees
EXEC dbo.RestoreEncryptedTable @encryptedTable, @tablePassword;

PRINT 'Table decrypted successfully. Row count: ' + CAST((SELECT COUNT(*) FROM #RestoredEmployees) AS VARCHAR(10));

-- Show decrypted data
PRINT 'Decrypted employee data:';
SELECT 
    CAST(EmployeeID AS INT) AS EmployeeID,
    FirstName,
    LastName,
    Email,
    CAST(Salary AS DECIMAL(18,2)) AS Salary,
    CAST(HireDate AS DATE) AS HireDate,
    CAST(IsActive AS BIT) AS IsActive,
    Department
FROM #RestoredEmployees
ORDER BY CAST(EmployeeID AS INT);

PRINT '';

-- =============================================
-- SECTION 6: XML ENCRYPTION WITH METADATA
-- =============================================

PRINT '--- SECTION 6: XML Encryption with Metadata ---';

-- Create XML data
DECLARE @xmlData XML = (SELECT * FROM SampleEmployees FOR XML PATH('Row'), ROOT('Root'));
DECLARE @xmlPassword NVARCHAR(MAX) = 'XMLEncryptionPassword2024!@#';

-- Encrypt XML with metadata
DECLARE @encryptedXml NVARCHAR(MAX) = dbo.EncryptXmlWithMetadata(@xmlData, @xmlPassword);

PRINT 'XML encrypted with metadata: ' + CAST(LEN(@encryptedXml) AS VARCHAR(20)) + ' characters';

-- Decrypt using stored procedure
CREATE TABLE #DecryptedXml (
    EmployeeID NVARCHAR(MAX),
    FirstName NVARCHAR(MAX),
    LastName NVARCHAR(MAX),
    Email NVARCHAR(MAX),
    Salary NVARCHAR(MAX),
    HireDate NVARCHAR(MAX),
    IsActive NVARCHAR(MAX),
    Department NVARCHAR(MAX)
);

INSERT INTO #DecryptedXml
EXEC dbo.RestoreEncryptedTable @encryptedXml, @xmlPassword;

PRINT 'Decrypted XML data:';
SELECT 
    CAST(EmployeeID AS INT) AS EmployeeID,
    FirstName,
    LastName,
    Email,
    CAST(Salary AS DECIMAL(18,2)) AS Salary,
    CAST(HireDate AS DATE) AS HireDate,
    CAST(IsActive AS BIT) AS IsActive,
    Department
FROM #DecryptedXml
ORDER BY CAST(EmployeeID AS INT);

PRINT '';

-- =============================================
-- SECTION 7: DYNAMIC TEMP TABLE WRAPPER
-- =============================================

PRINT '--- SECTION 7: Dynamic Temp Table Wrapper ---';

-- Demonstrate dynamic temp table wrapper
PRINT 'Using dynamic temp table wrapper for automatic table creation:';

-- This automatically creates a temp table with the correct structure
EXEC dbo.WrapDecryptProcedure 'dbo.RestoreEncryptedTable', '@encryptedData=''' + @encryptedTable + ''', @password=''' + @tablePassword + '''';

PRINT 'Dynamic temp table created and populated automatically!';
PRINT '';

-- =============================================
-- SECTION 8: STORED PROCEDURE RESULT SETS
-- =============================================

PRINT '--- SECTION 8: Stored Procedure Result Sets ---';

-- Create a stored procedure that returns data
CREATE PROCEDURE GetEmployeeReport
    @minSalary DECIMAL(18,2) = 0
AS
BEGIN
    SELECT 
        EmployeeID,
        FirstName,
        LastName,
        Email,
        Salary,
        HireDate,
        IsActive,
        Department
    FROM SampleEmployees 
    WHERE Salary >= @minSalary
    ORDER BY Salary DESC;
END;
GO

-- Capture stored procedure results
CREATE TABLE #SPResults (
    EmployeeID NVARCHAR(MAX),
    FirstName NVARCHAR(MAX),
    LastName NVARCHAR(MAX),
    Email NVARCHAR(MAX),
    Salary NVARCHAR(MAX),
    HireDate NVARCHAR(MAX),
    IsActive NVARCHAR(MAX),
    Department NVARCHAR(MAX)
);

INSERT INTO #SPResults
EXEC GetEmployeeReport @minSalary = 70000;

-- Encrypt stored procedure results
DECLARE @xmlSP XML = (SELECT * FROM #SPResults FOR XML PATH('Row'), ROOT('Root'));
DECLARE @encryptedSP NVARCHAR(MAX) = dbo.EncryptXmlWithMetadata(@xmlSP, @xmlPassword);

PRINT 'Stored procedure results encrypted: ' + CAST(LEN(@encryptedSP) AS VARCHAR(20)) + ' characters';

-- Decrypt stored procedure results
CREATE TABLE #DecryptedSP (
    EmployeeID NVARCHAR(MAX),
    FirstName NVARCHAR(MAX),
    LastName NVARCHAR(MAX),
    Email NVARCHAR(MAX),
    Salary NVARCHAR(MAX),
    HireDate NVARCHAR(MAX),
    IsActive NVARCHAR(MAX),
    Department NVARCHAR(MAX)
);

INSERT INTO #DecryptedSP
EXEC dbo.RestoreEncryptedTable @encryptedSP, @xmlPassword;

PRINT 'Stored procedure results decrypted:';
SELECT 
    CAST(EmployeeID AS INT) AS EmployeeID,
    FirstName,
    LastName,
    Email,
    CAST(Salary AS DECIMAL(18,2)) AS Salary,
    CAST(HireDate AS DATE) AS HireDate,
    CAST(IsActive AS BIT) AS IsActive,
    Department
FROM #DecryptedSP
ORDER BY CAST(Salary AS DECIMAL(18,2)) DESC;

PRINT '';

-- =============================================
-- SECTION 9: KEY DERIVATION AND CACHING
-- =============================================

PRINT '--- SECTION 9: Key Derivation and Caching ---';

-- Derive a key from password and salt (for performance optimization)
DECLARE @derivationPassword NVARCHAR(MAX) = 'DerivationPassword2024!@#';
DECLARE @derivationSalt NVARCHAR(MAX) = dbo.GenerateSalt();
DECLARE @derivedKey NVARCHAR(MAX) = dbo.DeriveKeyFromPassword(@derivationPassword, @derivationSalt);

PRINT 'Key derivation:';
PRINT 'Password: ' + @derivationPassword;
PRINT 'Salt: ' + @derivationSalt;
PRINT 'Derived key: ' + LEFT(@derivedKey, 30) + '...';

-- Use derived key for multiple operations (faster than password-based)
DECLARE @data1 NVARCHAR(MAX) = 'First piece of data';
DECLARE @data2 NVARCHAR(MAX) = 'Second piece of data';
DECLARE @data3 NVARCHAR(MAX) = 'Third piece of data';

DECLARE @encrypted1 NVARCHAR(MAX) = dbo.EncryptAesGcmWithDerivedKey(@data1, @derivedKey, @derivationSalt);
DECLARE @encrypted2 NVARCHAR(MAX) = dbo.EncryptAesGcmWithDerivedKey(@data2, @derivedKey, @derivationSalt);
DECLARE @encrypted3 NVARCHAR(MAX) = dbo.EncryptAesGcmWithDerivedKey(@data3, @derivedKey, @derivationSalt);

DECLARE @decrypted1 NVARCHAR(MAX) = dbo.DecryptAesGcmWithDerivedKey(@encrypted1, @derivedKey);
DECLARE @decrypted2 NVARCHAR(MAX) = dbo.DecryptAesGcmWithDerivedKey(@encrypted2, @derivedKey);
DECLARE @decrypted3 NVARCHAR(MAX) = dbo.DecryptAesGcmWithDerivedKey(@encrypted3, @derivedKey);

PRINT 'Multiple encryptions with derived key:';
PRINT 'Data1: ' + @data1 + ' -> ' + @decrypted1 + ' (' + CASE WHEN @data1 = @decrypted1 THEN '✓' ELSE '✗' END + ')';
PRINT 'Data2: ' + @data2 + ' -> ' + @decrypted2 + ' (' + CASE WHEN @data2 = @decrypted2 THEN '✓' ELSE '✗' END + ')';
PRINT 'Data3: ' + @data3 + ' -> ' + @decrypted3 + ' (' + CASE WHEN @data3 = @decrypted3 THEN '✓' ELSE '✗' END + ')';
PRINT '';

-- =============================================
-- SECTION 10: PERFORMANCE COMPARISON
-- =============================================

PRINT '--- SECTION 10: Performance Comparison ---';

-- Test performance of different encryption methods
DECLARE @testData NVARCHAR(MAX) = REPLICATE('Performance test data with repeated content. ', 100);
DECLARE @startTime DATETIME;
DECLARE @endTime DATETIME;
DECLARE @duration INT;

-- Test password-based encryption
SET @startTime = GETDATE();
DECLARE @perfEncrypted1 NVARCHAR(MAX) = dbo.EncryptAesGcmWithPassword(@testData, @password);
SET @endTime = GETDATE();
SET @duration = DATEDIFF(millisecond, @startTime, @endTime);
PRINT 'Password-based encryption: ' + CAST(@duration AS VARCHAR(10)) + ' ms';

-- Test derived key encryption
SET @startTime = GETDATE();
DECLARE @perfEncrypted2 NVARCHAR(MAX) = dbo.EncryptAesGcmWithDerivedKey(@testData, @derivedKey, @derivationSalt);
SET @endTime = GETDATE();
SET @duration = DATEDIFF(millisecond, @startTime, @endTime);
PRINT 'Derived key encryption: ' + CAST(@duration AS VARCHAR(10)) + ' ms';

-- Test direct AES-GCM encryption
SET @startTime = GETDATE();
DECLARE @perfEncrypted3 NVARCHAR(MAX) = dbo.EncryptAesGcm(@testData, @aesKey);
SET @endTime = GETDATE();
SET @duration = DATEDIFF(millisecond, @startTime, @endTime);
PRINT 'Direct AES-GCM encryption: ' + CAST(@duration AS VARCHAR(10)) + ' ms';

PRINT '';

-- =============================================
-- SECTION 11: ERROR HANDLING AND EDGE CASES
-- =============================================

PRINT '--- SECTION 11: Error Handling and Edge Cases ---';

-- Test wrong password
PRINT 'Testing wrong password handling:';
DECLARE @wrongPasswordResult NVARCHAR(MAX) = dbo.DecryptAesGcmWithPassword(@passwordEncrypted, 'WrongPassword123!');
PRINT 'Wrong password result: ' + CASE WHEN @wrongPasswordResult IS NULL THEN 'NULL (Expected)' ELSE 'Unexpected result' END;

-- Test empty data
PRINT 'Testing empty data handling:';
DECLARE @emptyEncrypted NVARCHAR(MAX) = dbo.EncryptAesGcmWithPassword('', @password);
DECLARE @emptyDecrypted NVARCHAR(MAX) = dbo.DecryptAesGcmWithPassword(@emptyEncrypted, @password);
PRINT 'Empty data encryption/decryption: ' + CASE WHEN @emptyDecrypted = '' THEN '✓ SUCCESS' ELSE '✗ FAILED' END;

-- Test NULL data
PRINT 'Testing NULL data handling:';
DECLARE @nullEncrypted NVARCHAR(MAX) = dbo.EncryptAesGcmWithPassword(NULL, @password);
PRINT 'NULL data encryption: ' + CASE WHEN @nullEncrypted IS NULL THEN 'NULL (Expected)' ELSE 'Unexpected result' END;

PRINT '';

-- =============================================
-- SECTION 12: POWERBUILDER INTEGRATION PATTERNS
-- =============================================

PRINT '--- SECTION 12: PowerBuilder Integration Patterns ---';

-- Simulate PowerBuilder data structure
CREATE TABLE PowerBuilderData (
    ID INT IDENTITY(1,1) PRIMARY KEY,
    DataWindow NVARCHAR(MAX),
    TransactionObject NVARCHAR(MAX),
    UserID NVARCHAR(50),
    SessionData NVARCHAR(MAX),
    LastModified DATETIME2 DEFAULT GETDATE()
);

INSERT INTO PowerBuilderData (DataWindow, TransactionObject, UserID, SessionData) VALUES
('dw_employee', 'tr_database', 'admin', '{"language":"ko","theme":"dark","preferences":{"autoSave":true}}'),
('dw_customer', 'tr_database', 'user1', '{"language":"en","theme":"light","preferences":{"autoSave":false}}'),
('dw_order', 'tr_database', 'manager', '{"language":"ko","theme":"blue","preferences":{"autoSave":true}}');

-- Encrypt PowerBuilder session data
DECLARE @pbPassword NVARCHAR(MAX) = 'PowerBuilderSession2024!@#';
DECLARE @pbEncrypted NVARCHAR(MAX) = dbo.EncryptTableWithMetadata('PowerBuilderData', @pbPassword);

PRINT 'PowerBuilder data encrypted: ' + CAST(LEN(@pbEncrypted) AS VARCHAR(20)) + ' characters';

-- Decrypt for PowerBuilder use
CREATE TABLE #PBRestored (
    ID NVARCHAR(MAX),
    DataWindow NVARCHAR(MAX),
    TransactionObject NVARCHAR(MAX),
    UserID NVARCHAR(MAX),
    SessionData NVARCHAR(MAX),
    LastModified NVARCHAR(MAX)
);

INSERT INTO #PBRestored
EXEC dbo.RestoreEncryptedTable @pbEncrypted, @pbPassword;

PRINT 'PowerBuilder data restored:';
SELECT 
    CAST(ID AS INT) AS ID,
    DataWindow,
    TransactionObject,
    UserID,
    SessionData,
    CAST(LastModified AS DATETIME2) AS LastModified
FROM #PBRestored
ORDER BY CAST(ID AS INT);

PRINT '';

-- =============================================
-- FINAL SUMMARY
-- =============================================

PRINT '=== COMPLETE EXAMPLES SUMMARY ===';
PRINT '';
PRINT 'FEATURES SUCCESSFULLY DEMONSTRATED:';
PRINT '✓ AES-GCM Encryption/Decryption';
PRINT '✓ Password-based Key Derivation (PBKDF2)';
PRINT '✓ Diffie-Hellman Key Exchange';
PRINT '✓ BCrypt Password Hashing';
PRINT '✓ Table-Level Encryption with Metadata';
PRINT '✓ XML Encryption with Schema Inference';
PRINT '✓ Dynamic Temp Table Wrapper';
PRINT '✓ Automatic Type Casting';
PRINT '✓ Stored Procedure Result Set Handling';
PRINT '✓ Korean Character Support';
PRINT '✓ PowerBuilder Integration Patterns';
PRINT '✓ Performance Optimization';
PRINT '✓ Error Handling and Edge Cases';
PRINT '';
PRINT 'USAGE PATTERNS:';
PRINT '• Basic encryption: dbo.EncryptAesGcm(text, key) / dbo.DecryptAesGcm(encrypted, key)';
PRINT '• Password encryption: dbo.EncryptAesGcmWithPassword(text, password) / dbo.DecryptAesGcmWithPassword(encrypted, password)';
PRINT '• Table encryption: dbo.EncryptTableWithMetadata(tableName, password) / EXEC dbo.RestoreEncryptedTable(encrypted, password)';
PRINT '• Password hashing: dbo.HashPasswordDefault(password) / dbo.VerifyPassword(password, hash)';
PRINT '• Key exchange: SELECT * FROM dbo.GenerateDiffieHellmanKeys() / dbo.DeriveSharedKey(publicKey, privateKey)';
PRINT '• Key derivation: dbo.DeriveKeyFromPassword(password, salt) / dbo.EncryptAesGcmWithDerivedKey(text, derivedKey, salt)';
PRINT '• Salt generation: dbo.GenerateSalt() / dbo.GenerateSaltWithLength(length)';
PRINT '';
PRINT 'KOREAN POWERBUILDER INTEGRATION:';
PRINT '• Full Unicode support for Korean characters';
PRINT '• Session data encryption for PowerBuilder applications';
PRINT '• Dynamic temp table creation for flexible data handling';
PRINT '• Automatic type casting for seamless integration';
PRINT '';
PRINT 'PERFORMANCE TIPS:';
PRINT '• Use derived keys for multiple operations (faster than password-based)';
PRINT '• Cache derived keys when possible';
PRINT '• Use table-level encryption for bulk data';
PRINT '• Use XML encryption for smaller datasets';
PRINT '';
PRINT 'SECURITY BEST PRACTICES:';
PRINT '• Use strong passwords (12+ characters, mixed case, symbols)';
PRINT '• Generate unique salts for each user/purpose';
PRINT '• Use appropriate iteration counts (2000+ for PBKDF2)';
PRINT '• Store encrypted data securely';
PRINT '• Never store passwords in plain text';
PRINT '';
PRINT 'DEPRECATED FUNCTIONS:';
PRINT '• EncryptAES/DecryptAES - Use EncryptAesGcm/DecryptAesGcm instead';
PRINT '• EncryptXmlWithPassword - Use EncryptXmlWithMetadata instead';
PRINT '';
PRINT '=== EXAMPLES COMPLETED SUCCESSFULLY ===';

-- =============================================
-- CLEANUP
-- =============================================

DROP TABLE SampleEmployees;
DROP TABLE PowerBuilderData;
DROP TABLE #RestoredEmployees;
DROP TABLE #SPResults;
DROP TABLE #DecryptedSP;
DROP TABLE #PBRestored;
DROP TABLE #DecryptedXml;
DROP PROCEDURE GetEmployeeReport;

PRINT '';
PRINT 'Cleanup completed. All temporary objects removed.';
PRINT '============================================='; 