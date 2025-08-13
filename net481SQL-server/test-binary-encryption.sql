-- =============================================
-- Test Script for Binary Value Encryption/Decryption
-- This script demonstrates the usage of the new EncryptBinaryValue function
-- along with the existing DecryptBinaryValue function
-- =============================================

PRINT '=== Testing Binary Value Encryption/Decryption ===';
GO

-- Test variables
DECLARE @password NVARCHAR(MAX) = N'test_password_123';
DECLARE @iterations INT = 10000;
DECLARE @originalBinary VARBINARY(MAX);
DECLARE @encryptedData NVARCHAR(MAX);
DECLARE @decryptedBinary VARBINARY(MAX);

-- Test Case 1: Simple binary data
PRINT 'Test Case 1: Simple binary data';
SET @originalBinary = CONVERT(VARBINARY(MAX), 'Hello, World! This is test binary data.');

PRINT 'Original binary length: ' + CAST(DATALENGTH(@originalBinary) AS NVARCHAR(10));
PRINT 'Original data as string: ' + CONVERT(NVARCHAR(MAX), @originalBinary);

-- Encrypt binary value
SET @encryptedData = dbo.EncryptBinaryValue(@originalBinary, @password, @iterations);
PRINT 'Encryption completed. Encrypted data length: ' + CAST(LEN(@encryptedData) AS NVARCHAR(10));

-- Decrypt binary value
SET @decryptedBinary = dbo.DecryptBinaryValue(@encryptedData, @password);
PRINT 'Decryption completed. Decrypted binary length: ' + CAST(DATALENGTH(@decryptedBinary) AS NVARCHAR(10));
PRINT 'Decrypted data as string: ' + CONVERT(NVARCHAR(MAX), @decryptedBinary);

-- Verify data integrity
IF @originalBinary = @decryptedBinary
    PRINT '✓ Test Case 1: PASSED - Data integrity verified';
ELSE
    PRINT '✗ Test Case 1: FAILED - Data integrity check failed';

PRINT '';
GO

-- Test Case 2: Larger binary data
PRINT 'Test Case 2: Larger binary data (image-like data)';
DECLARE @password2 NVARCHAR(MAX) = N'secure_password_456';
DECLARE @iterations2 INT = 15000;
DECLARE @largeBinary VARBINARY(MAX);
DECLARE @encryptedData2 NVARCHAR(MAX);
DECLARE @decryptedBinary2 VARBINARY(MAX);

-- Create larger test data (simulating image/file data)
SET @largeBinary = CONVERT(VARBINARY(MAX), REPLICATE('TestData123!@#', 100));

PRINT 'Original large binary length: ' + CAST(DATALENGTH(@largeBinary) AS NVARCHAR(10));

-- Encrypt large binary value
SET @encryptedData2 = dbo.EncryptBinaryValue(@largeBinary, @password2, @iterations2);
PRINT 'Large data encryption completed. Encrypted data length: ' + CAST(LEN(@encryptedData2) AS NVARCHAR(10));

-- Decrypt large binary value
SET @decryptedBinary2 = dbo.DecryptBinaryValue(@encryptedData2, @password2);
PRINT 'Large data decryption completed. Decrypted binary length: ' + CAST(DATALENGTH(@decryptedBinary2) AS NVARCHAR(10));

-- Verify large data integrity
IF @largeBinary = @decryptedBinary2
    PRINT '✓ Test Case 2: PASSED - Large data integrity verified';
ELSE
    PRINT '✗ Test Case 2: FAILED - Large data integrity check failed';

PRINT '';
GO

-- Test Case 3: Error handling (wrong password)
PRINT 'Test Case 3: Error handling with wrong password';
DECLARE @password3 NVARCHAR(MAX) = N'correct_password';
DECLARE @wrongPassword NVARCHAR(MAX) = N'wrong_password';
DECLARE @iterations3 INT = 10000;
DECLARE @testBinary VARBINARY(MAX);
DECLARE @encryptedData3 NVARCHAR(MAX);

SET @testBinary = CONVERT(VARBINARY(MAX), 'Secret binary data for password test');

-- Encrypt with correct password
SET @encryptedData3 = dbo.EncryptBinaryValue(@testBinary, @password3, @iterations3);
PRINT 'Data encrypted with correct password';

-- Try to decrypt with wrong password (this should fail)
BEGIN TRY
    DECLARE @wrongDecryption VARBINARY(MAX) = dbo.DecryptBinaryValue(@encryptedData3, @wrongPassword);
    PRINT '✗ Test Case 3: FAILED - Wrong password should have failed but didn''t';
END TRY
BEGIN CATCH
    PRINT '✓ Test Case 3: PASSED - Wrong password correctly failed with error: ' + ERROR_MESSAGE();
END CATCH

PRINT '';
GO

PRINT '=== Binary Value Encryption/Decryption Tests Completed ===';
PRINT '';
PRINT 'The new EncryptBinaryValue function provides:';
PRINT '1. Proper handling of VARBINARY(MAX) input data';
PRINT '2. Guaranteed binary data type preservation in metadata';
PRINT '3. Seamless integration with existing DecryptBinaryValue function';
PRINT '4. Strong encryption using AES-GCM with password-based key derivation';
PRINT '';
PRINT 'Usage Examples:';
PRINT '  -- Encrypt binary data:';
PRINT '  SELECT dbo.EncryptBinaryValue(CONVERT(VARBINARY(MAX), ''your data''), N''password'', 10000)';
PRINT '';
PRINT '  -- Decrypt binary data:';
PRINT '  SELECT dbo.DecryptBinaryValue(@encryptedXmlData, N''password'')';
GO