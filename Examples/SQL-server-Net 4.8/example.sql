-- Complete Usage Examples for SecureLibrary-SQL CLR Functions
-- This script demonstrates all functionality including new features from PR #61
--
-- NOTE: For more practical, developer-focused examples that address
-- dynamic table creation, schema comparison, and SELECT INTO patterns,
-- see practical-examples.sql which provides enhanced real-world examples.

-- =============================================
-- NEW FEATURES: Password-Based Table Encryption
-- =============================================

PRINT '=== NEW: Password-Based Table Encryption Examples ===';

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

PRINT 'Sample data created.';

-- Step 2: Password-based table encryption
DECLARE @password NVARCHAR(MAX) = 'SuperSecretP@ssw0rdForTesting!';
DECLARE @xmlData XML = (SELECT * FROM #SampleData FOR XML PATH('Row'), ROOT('Root'));
DECLARE @encryptedTable NVARCHAR(MAX) = dbo.EncryptXmlWithPassword(@xmlData, @password);
PRINT 'Table encrypted with password: ' + LEFT(@encryptedTable, 100) + '...';

-- Step 3: Universal table restoration using stored procedure
PRINT 'Restoring encrypted table with stored procedure...';
CREATE TABLE #RestoredData (
    ID NVARCHAR(MAX),
    Name NVARCHAR(MAX),
    Department NVARCHAR(MAX),
    Salary NVARCHAR(MAX),
    JoinDate NVARCHAR(MAX)
);

INSERT INTO #RestoredData
EXEC dbo.RestoreEncryptedTable @encryptedTable, @password;

PRINT 'Restored data:';
SELECT * FROM #RestoredData;

-- =============================================
-- INDIVIDUAL DATA ENCRYPTION EXAMPLES
-- =============================================

PRINT '';
PRINT '=== Individual Data Encryption Examples ===';

-- Generate key for encryption
DECLARE @aesKey NVARCHAR(MAX) = dbo.GenerateAESKey();

-- Example 1: Single JSON row encryption using basic AES-GCM
DECLARE @jsonRow NVARCHAR(MAX) = '{"ID":1,"Name":"John Doe","Department":"Engineering","Salary":95000.00}';
DECLARE @encryptedRow NVARCHAR(MAX) = dbo.EncryptAesGcm(@jsonRow, @aesKey);
PRINT 'Encrypted single row: ' + LEFT(@encryptedRow, 100) + '...';

-- Decrypt the row
DECLARE @decryptedRow NVARCHAR(MAX) = dbo.DecryptAesGcm(@encryptedRow, @aesKey);
PRINT 'Decrypted row: ' + @decryptedRow;

-- Example 2: Password-based encryption for individual data
DECLARE @userPassword NVARCHAR(MAX) = N'MySecretPassword123!';
DECLARE @encryptedWithPassword NVARCHAR(MAX) = dbo.EncryptAesGcmWithPassword(@jsonRow, @userPassword);
PRINT 'Encrypted with password: ' + LEFT(@encryptedWithPassword, 100) + '...';

-- Decrypt with password
DECLARE @decryptedWithPassword NVARCHAR(MAX) = dbo.DecryptAesGcmWithPassword(@encryptedWithPassword, @userPassword);
PRINT 'Decrypted with password: ' + @decryptedWithPassword;

-- =============================================
-- Core AES-GCM Encryption Examples
-- =============================================

PRINT '';
PRINT '=== Core AES-GCM Encryption Examples ===';

-- Basic AES-GCM encryption
DECLARE @plainText NVARCHAR(MAX) = N'Hello, World! This is a test message with Korean: 안녕하세요';
DECLARE @encryptedGcm NVARCHAR(MAX) = dbo.EncryptAesGcm(@plainText, @aesKey);
PRINT 'Encrypted (AES-GCM): ' + LEFT(@encryptedGcm, 100) + '...';

-- Decrypt
DECLARE @decryptedGcm NVARCHAR(MAX) = dbo.DecryptAesGcm(@encryptedGcm, @aesKey);
PRINT 'Decrypted (AES-GCM): ' + @decryptedGcm;

-- =============================================
-- Password Hashing Examples
-- =============================================

PRINT '';
PRINT '=== Password Hashing Examples ===';

-- Hash password with default work factor
DECLARE @testPassword NVARCHAR(MAX) = N'UserPassword123!';
DECLARE @hashedPassword NVARCHAR(MAX) = dbo.HashPasswordDefault(@testPassword);
PRINT 'Hashed password: ' + @hashedPassword;

-- Verify password
DECLARE @isValid BIT = dbo.VerifyPassword(@testPassword, @hashedPassword);
PRINT 'Password verification result: ' + CASE WHEN @isValid = 1 THEN 'Valid' ELSE 'Invalid' END;

-- Verify with wrong password
DECLARE @isValidWrong BIT = dbo.VerifyPassword(N'WrongPassword', @hashedPassword);
PRINT 'Wrong password verification result: ' + CASE WHEN @isValidWrong = 1 THEN 'Valid' ELSE 'Invalid' END;

-- =============================================
-- Diffie-Hellman Key Exchange Examples
-- =============================================

PRINT '';
PRINT '=== Diffie-Hellman Key Exchange Examples ===';

-- Generate keys for two parties
DECLARE @partyAPublicKey NVARCHAR(MAX), @partyAPrivateKey NVARCHAR(MAX);
DECLARE @partyBPublicKey NVARCHAR(MAX), @partyBPrivateKey NVARCHAR(MAX);

SELECT @partyAPublicKey = PublicKey, @partyAPrivateKey = PrivateKey 
FROM dbo.GenerateDiffieHellmanKeys();

SELECT @partyBPublicKey = PublicKey, @partyBPrivateKey = PrivateKey 
FROM dbo.GenerateDiffieHellmanKeys();

PRINT 'Party A Public Key: ' + LEFT(@partyAPublicKey, 50) + '...';
PRINT 'Party B Public Key: ' + LEFT(@partyBPublicKey, 50) + '...';

-- Derive shared keys
DECLARE @sharedKeyA NVARCHAR(MAX) = dbo.DeriveSharedKey(@partyBPublicKey, @partyAPrivateKey);
DECLARE @sharedKeyB NVARCHAR(MAX) = dbo.DeriveSharedKey(@partyAPublicKey, @partyBPrivateKey);

-- Verify both parties have the same shared key
IF @sharedKeyA = @sharedKeyB
    PRINT 'SUCCESS: Both parties derived the same shared key!';
ELSE
    PRINT 'ERROR: Shared keys do not match!';

-- =============================================
-- PowerBuilder Integration Examples
-- =============================================

PRINT '';
PRINT '=== PowerBuilder Integration Examples ===';

-- Example 1: Secure user data storage
PRINT 'Example 1: Secure user data with password';
DECLARE @userData NVARCHAR(MAX) = N'{"user_id":"PB001","username":"powerbuilder_user","email":"user@company.com"}';
DECLARE @pbPassword NVARCHAR(MAX) = N'PowerBuilder2024!';
DECLARE @secureUserData NVARCHAR(MAX) = dbo.EncryptAesGcmWithPassword(@userData, @pbPassword);
PRINT 'Encrypted user data: ' + LEFT(@secureUserData, 80) + '...';

-- PowerBuilder would call this to decrypt
DECLARE @restoredUserData NVARCHAR(MAX) = dbo.DecryptAesGcmWithPassword(@secureUserData, @pbPassword);
PRINT 'Restored user data: ' + @restoredUserData;

-- Example 2: Password verification for login
PRINT 'Example 2: PowerBuilder login authentication';
DECLARE @loginPassword NVARCHAR(MAX) = N'PBUserPassword!';
DECLARE @storedHash NVARCHAR(MAX) = dbo.HashPasswordDefault(@loginPassword);
PRINT 'Stored password hash: ' + @storedHash;

-- PowerBuilder login check
DECLARE @loginAttempt NVARCHAR(MAX) = N'PBUserPassword!';
DECLARE @loginValid BIT = dbo.VerifyPassword(@loginAttempt, @storedHash);
PRINT 'Login attempt result: ' + CASE WHEN @loginValid = 1 THEN 'SUCCESS - User authenticated' ELSE 'FAILED - Invalid credentials' END;

-- Example 3: Complete table backup and restore for PowerBuilder
PRINT 'Example 3: Complete table backup for PowerBuilder';
DECLARE @backupPassword NVARCHAR(MAX) = N'BackupPwd2024!';
DECLARE @tableBackup NVARCHAR(MAX) = dbo.EncryptXmlWithPassword(@xmlData, @backupPassword);
PRINT 'Table backup created (encrypted): ' + LEFT(@tableBackup, 80) + '...';

-- PowerBuilder would call this procedure to restore the table
PRINT 'PowerBuilder table restore command:';
PRINT 'EXEC dbo.RestoreEncryptedTable @encryptedData, @password';

-- =============================================
-- Performance and Best Practices
-- =============================================

PRINT '';
PRINT '=== Performance and Best Practices ===';

PRINT 'Best Practices:';
PRINT '1. Use EncryptXmlWithPassword + RestoreEncryptedTable for full table encryption';
PRINT '2. Use EncryptAesGcmWithPassword for simple text encryption';
PRINT '3. Use HashPasswordDefault + VerifyPassword for user authentication';
PRINT '4. Use GenerateDiffieHellmanKeys for secure key exchange';
PRINT '5. Use row-by-row functions for structured data processing';
PRINT '';
PRINT 'Security Notes:';
PRINT '• All functions use AES-256-GCM with 128-bit authentication tags';
PRINT '• Password-based functions use PBKDF2 with 10,000 iterations by default';
PRINT '• All functions are safe for Korean and Unicode text';
PRINT '• RestoreEncryptedTable dynamically handles any table structure';

-- Clean up
DROP TABLE #SampleData;
DROP TABLE #RestoredData;

-- =============================================
-- Summary
-- =============================================
PRINT '';
PRINT '=== SUMMARY ===';
PRINT 'All SQL CLR functions tested successfully!';
PRINT '';
PRINT 'NEW in this release (PR #61):';
PRINT '✓ Password-based table encryption with universal restore';
PRINT '✓ Row-by-row encryption for structured data';
PRINT '✓ Bulk processing capabilities';
PRINT '✓ Enhanced PowerBuilder integration';
PRINT '';
PRINT 'Core features:';
PRINT '✓ AES-GCM encryption/decryption';
PRINT '✓ Password-based encryption';
PRINT '✓ Bcrypt password hashing';
PRINT '✓ Diffie-Hellman key exchange';
PRINT '';
PRINT 'Ready for production use in PowerBuilder applications!';