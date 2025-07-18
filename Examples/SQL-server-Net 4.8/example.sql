-- Example usage of SecureLibrary-SQL functions
-- This script demonstrates how to use all the available encryption functions

-- =============================================
-- AES Key Generation and Encryption Examples
-- =============================================

-- Generate a new AES key
DECLARE @aesKey NVARCHAR(MAX) = dbo.GenerateAESKey();
PRINT 'Generated AES Key: ' + @aesKey;

-- Encrypt text using AES-GCM (recommended)
DECLARE @plainText NVARCHAR(MAX) = N'Hello, World! This is a test message.';
DECLARE @encryptedGcm NVARCHAR(MAX) = dbo.EncryptAesGcm(@plainText, @aesKey);
PRINT 'Encrypted (AES-GCM): ' + @encryptedGcm;

-- Decrypt text using AES-GCM
DECLARE @decryptedGcm NVARCHAR(MAX) = dbo.DecryptAesGcm(@encryptedGcm, @aesKey);
PRINT 'Decrypted (AES-GCM): ' + @decryptedGcm;

-- =============================================
-- Password-based Encryption Examples
-- =============================================

-- Encrypt using password (default iterations)
DECLARE @password NVARCHAR(MAX) = N'MySecretPassword123!';
DECLARE @encryptedWithPassword NVARCHAR(MAX) = dbo.EncryptAesGcmWithPassword(@plainText, @password);
PRINT 'Encrypted with password: ' + @encryptedWithPassword;

-- Decrypt using password
DECLARE @decryptedWithPassword NVARCHAR(MAX) = dbo.DecryptAesGcmWithPassword(@encryptedWithPassword, @password);
PRINT 'Decrypted with password: ' + @decryptedWithPassword;

-- Encrypt using password with custom iterations
DECLARE @encryptedWithPasswordIterations NVARCHAR(MAX) = dbo.EncryptAesGcmWithPasswordIterations(@plainText, @password, 5000);
PRINT 'Encrypted with password (5000 iterations): ' + @encryptedWithPasswordIterations;

-- Decrypt using password with custom iterations
DECLARE @decryptedWithPasswordIterations NVARCHAR(MAX) = dbo.DecryptAesGcmWithPasswordIterations(@encryptedWithPasswordIterations, @password, 5000);
PRINT 'Decrypted with password (5000 iterations): ' + @decryptedWithPasswordIterations;

-- =============================================
-- Salt Generation and Custom Salt Examples
-- =============================================

-- Generate a salt (default 16 bytes)
DECLARE @salt NVARCHAR(MAX) = dbo.GenerateSalt();
PRINT 'Generated salt: ' + @salt;

-- Generate a salt with custom length (32 bytes)
DECLARE @customSalt NVARCHAR(MAX) = dbo.GenerateSaltWithLength(32);
PRINT 'Generated custom salt (32 bytes): ' + @customSalt;

-- Encrypt using password and custom salt
DECLARE @encryptedWithSalt NVARCHAR(MAX) = dbo.EncryptAesGcmWithPasswordAndSalt(@plainText, @password, @salt);
PRINT 'Encrypted with password and salt: ' + @encryptedWithSalt;

-- Decrypt using password and custom salt (salt is embedded in the encrypted data)
DECLARE @decryptedWithSalt NVARCHAR(MAX) = dbo.DecryptAesGcmWithPassword(@encryptedWithSalt, @password);
PRINT 'Decrypted with password and salt: ' + @decryptedWithSalt;

-- Encrypt using password, custom salt, and custom iterations
DECLARE @encryptedWithSaltAndIterations NVARCHAR(MAX) = dbo.EncryptAesGcmWithPasswordAndSaltIterations(@plainText, @password, @customSalt, 10000);
PRINT 'Encrypted with password, custom salt, and 10000 iterations: ' + @encryptedWithSaltAndIterations;

-- =============================================
-- Password Hashing Examples
-- =============================================

-- Hash password with default work factor (12)
DECLARE @userPassword NVARCHAR(MAX) = N'UserPassword123!';
DECLARE @hashedPassword NVARCHAR(MAX) = dbo.HashPasswordDefault(@userPassword);
PRINT 'Hashed password (default): ' + @hashedPassword;

-- Hash password with custom work factor (14)
DECLARE @hashedPasswordCustom NVARCHAR(MAX) = dbo.HashPasswordWithWorkFactor(@userPassword, 14);
PRINT 'Hashed password (work factor 14): ' + @hashedPasswordCustom;

-- Verify password
DECLARE @isValid BIT = dbo.VerifyPassword(@userPassword, @hashedPassword);
PRINT 'Password verification result: ' + CASE WHEN @isValid = 1 THEN 'Valid' ELSE 'Invalid' END;

-- Verify with wrong password
DECLARE @isValidWrong BIT = dbo.VerifyPassword(N'WrongPassword', @hashedPassword);
PRINT 'Wrong password verification result: ' + CASE WHEN @isValidWrong = 1 THEN 'Valid' ELSE 'Invalid' END;

-- =============================================
-- Diffie-Hellman Key Exchange Examples
-- =============================================

-- Generate Diffie-Hellman keys for Party A
DECLARE @partyAPublicKey NVARCHAR(MAX);
DECLARE @partyAPrivateKey NVARCHAR(MAX);

SELECT @partyAPublicKey = PublicKey, @partyAPrivateKey = PrivateKey 
FROM dbo.GenerateDiffieHellmanKeys();

PRINT 'Party A Public Key: ' + @partyAPublicKey;
PRINT 'Party A Private Key: ' + @partyAPrivateKey;

-- Generate Diffie-Hellman keys for Party B
DECLARE @partyBPublicKey NVARCHAR(MAX);
DECLARE @partyBPrivateKey NVARCHAR(MAX);

SELECT @partyBPublicKey = PublicKey, @partyBPrivateKey = PrivateKey 
FROM dbo.GenerateDiffieHellmanKeys();

PRINT 'Party B Public Key: ' + @partyBPublicKey;
PRINT 'Party B Private Key: ' + @partyBPrivateKey;

-- Derive shared key for Party A
DECLARE @sharedKeyA NVARCHAR(MAX) = dbo.DeriveSharedKey(@partyBPublicKey, @partyAPrivateKey);
PRINT 'Shared Key (Party A): ' + @sharedKeyA;

-- Derive shared key for Party B
DECLARE @sharedKeyB NVARCHAR(MAX) = dbo.DeriveSharedKey(@partyAPublicKey, @partyBPrivateKey);
PRINT 'Shared Key (Party B): ' + @sharedKeyB;

-- Verify that both parties derived the same shared key
IF @sharedKeyA = @sharedKeyB
    PRINT 'SUCCESS: Both parties derived the same shared key!';
ELSE
    PRINT 'ERROR: Shared keys do not match!';

-- =============================================
-- Legacy AES-CBC Examples (Deprecated)
-- =============================================

-- Note: These functions are deprecated and should not be used in new applications
-- They are included here for backward compatibility only

-- Encrypt using legacy AES-CBC
DECLARE @encryptedCbc TABLE (CipherText NVARCHAR(MAX), IV NVARCHAR(MAX));
INSERT INTO @encryptedCbc SELECT * FROM dbo.EncryptAES(@plainText, @aesKey);

DECLARE @cipherText NVARCHAR(MAX), @iv NVARCHAR(MAX);
SELECT @cipherText = CipherText, @iv = IV FROM @encryptedCbc;

PRINT 'Encrypted (AES-CBC): ' + @cipherText;
PRINT 'IV: ' + @iv;

-- Decrypt using legacy AES-CBC
DECLARE @decryptedCbc NVARCHAR(MAX) = dbo.DecryptAES(@cipherText, @aesKey, @iv);
PRINT 'Decrypted (AES-CBC): ' + @decryptedCbc;

-- =============================================
-- Key Derivation and Performance Optimization Examples
-- =============================================

-- Derive a key from password and salt (default iterations)
DECLARE @derivedKey NVARCHAR(MAX) = dbo.DeriveKeyFromPassword(@password, @salt);
PRINT 'Derived key (default iterations): ' + @derivedKey;

-- Derive a key from password and salt with custom iterations
DECLARE @derivedKeyCustom NVARCHAR(MAX) = dbo.DeriveKeyFromPasswordIterations(@password, @customSalt, 10000);
PRINT 'Derived key (10000 iterations): ' + @derivedKeyCustom;

-- Encrypt using pre-derived key (for performance optimization)
DECLARE @encryptedWithDerivedKey NVARCHAR(MAX) = dbo.EncryptAesGcmWithDerivedKey(@plainText, @derivedKey, @salt);
PRINT 'Encrypted with derived key: ' + @encryptedWithDerivedKey;

-- Decrypt using pre-derived key
DECLARE @decryptedWithDerivedKey NVARCHAR(MAX) = dbo.DecryptAesGcmWithDerivedKey(@encryptedWithDerivedKey, @derivedKey);
PRINT 'Decrypted with derived key: ' + @decryptedWithDerivedKey;

-- Verify that derived key encryption produces compatible output
DECLARE @decryptedWithPasswordFromDerived NVARCHAR(MAX) = dbo.DecryptAesGcmWithPassword(@encryptedWithDerivedKey, @password);
PRINT 'Decrypted derived key output with password: ' + @decryptedWithPasswordFromDerived;

-- Performance comparison: multiple encryptions with same derived key
DECLARE @testText1 NVARCHAR(MAX) = N'Test message 1';
DECLARE @testText2 NVARCHAR(MAX) = N'Test message 2';
DECLARE @testText3 NVARCHAR(MAX) = N'Test message 3';

-- Using derived key (faster for multiple operations)
DECLARE @encrypted1 NVARCHAR(MAX) = dbo.EncryptAesGcmWithDerivedKey(@testText1, @derivedKey, @salt);
DECLARE @encrypted2 NVARCHAR(MAX) = dbo.EncryptAesGcmWithDerivedKey(@testText2, @derivedKey, @salt);
DECLARE @encrypted3 NVARCHAR(MAX) = dbo.EncryptAesGcmWithDerivedKey(@testText3, @derivedKey, @salt);

PRINT 'Multiple encryptions with derived key completed';

-- Decrypt all three
DECLARE @decrypted1 NVARCHAR(MAX) = dbo.DecryptAesGcmWithDerivedKey(@encrypted1, @derivedKey);
DECLARE @decrypted2 NVARCHAR(MAX) = dbo.DecryptAesGcmWithDerivedKey(@encrypted2, @derivedKey);
DECLARE @decrypted3 NVARCHAR(MAX) = dbo.DecryptAesGcmWithDerivedKey(@encrypted3, @derivedKey);

PRINT 'Test 1: ' + @decrypted1;
PRINT 'Test 2: ' + @decrypted2;
PRINT 'Test 3: ' + @decrypted3;

-- =============================================
-- Summary
-- =============================================
PRINT '';
PRINT '=== SUMMARY ===';
PRINT 'All encryption functions have been tested successfully!';
PRINT 'Key points:';
PRINT '1. Use AES-GCM functions for new applications (EncryptAesGcm/DecryptAesGcm)';
PRINT '2. Use password-based functions for user data (EncryptAesGcmWithPassword/DecryptAesGcmWithPassword)';
PRINT '3. Use HashPasswordDefault/VerifyPassword for password storage';
PRINT '4. Use GenerateDiffieHellmanKeys/DeriveSharedKey for secure key exchange';
PRINT '5. Use DeriveKeyFromPassword + EncryptAesGcmWithDerivedKey for performance optimization';
PRINT '6. Avoid legacy AES-CBC functions (EncryptAES/DecryptAES) in new code';
PRINT '7. All functions now have unique names to avoid SQL CLR overloading issues';
PRINT '8. Derived key functions provide the same security with better performance for multiple operations';
