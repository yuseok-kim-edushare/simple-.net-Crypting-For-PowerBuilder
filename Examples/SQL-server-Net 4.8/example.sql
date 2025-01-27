-- Example usage of all cryptographic functions

-- 1. AES Key Generation and Basic Encryption/Decryption
DECLARE @aesKey nvarchar(max) = dbo.GenerateAESKey();
DECLARE @plainText nvarchar(max) = N'Hello, this is a secret message!';
PRINT 'Original Text: ' + @plainText;
PRINT 'AES Key: ' + @aesKey;

-- Encrypt the message
DECLARE @encryptedData TABLE (CipherText nvarchar(max), IV nvarchar(max));
INSERT INTO @encryptedData
SELECT * FROM dbo.EncryptAES(@plainText, @aesKey);

DECLARE @cipherText nvarchar(max), @iv nvarchar(max);
SELECT @cipherText = CipherText, @iv = IV FROM @encryptedData;
PRINT 'Encrypted (Base64): ' + @cipherText;
PRINT 'IV (Base64): ' + @iv;

-- Decrypt the message
DECLARE @decryptedText nvarchar(max);
SET @decryptedText = dbo.DecryptAES(@cipherText, @aesKey, @iv);
PRINT 'Decrypted Text: ' + @decryptedText;
GO

-- 2. Diffie-Hellman Key Exchange Example
-- Generate keys for Alice
DECLARE @aliceKeys TABLE (PublicKey nvarchar(max), PrivateKey nvarchar(max));
INSERT INTO @aliceKeys
SELECT * FROM dbo.GenerateDiffieHellmanKeys();

-- Generate keys for Bob
DECLARE @bobKeys TABLE (PublicKey nvarchar(max), PrivateKey nvarchar(max));
INSERT INTO @bobKeys
SELECT * FROM dbo.GenerateDiffieHellmanKeys();

-- Get the keys
DECLARE @alicePublic nvarchar(max), @alicePrivate nvarchar(max);
DECLARE @bobPublic nvarchar(max), @bobPrivate nvarchar(max);
SELECT @alicePublic = PublicKey, @alicePrivate = PrivateKey FROM @aliceKeys;
SELECT @bobPublic = PublicKey, @bobPrivate = PrivateKey FROM @bobKeys;

-- Derive shared secrets
DECLARE @aliceShared nvarchar(max) = dbo.DeriveSharedKey(@bobPublic, @alicePrivate);
DECLARE @bobShared nvarchar(max) = dbo.DeriveSharedKey(@alicePublic, @bobPrivate);

-- Verify both parties derived the same key
PRINT 'Alice Shared Key: ' + @aliceShared;
PRINT 'Bob Shared Key: ' + @bobShared;
GO

-- 3. Password Hashing and Verification
DECLARE @password nvarchar(max) = N'MySecurePassword123';
DECLARE @hashedPassword nvarchar(max) = dbo.HashPassword(@password);
PRINT 'Original Password: ' + @password;
PRINT 'Hashed Password: ' + @hashedPassword;

-- Verify correct password
DECLARE @isValid bit = dbo.VerifyPassword(@password, @hashedPassword);
PRINT 'Password Valid: ' + CAST(@isValid AS nvarchar(1));

-- Verify wrong password
SET @isValid = dbo.VerifyPassword('WrongPassword', @hashedPassword);
PRINT 'Wrong Password Valid: ' + CAST(@isValid AS nvarchar(1));
GO

-- 4. AES-GCM Encryption/Decryption
DECLARE @gcmKey nvarchar(max) = dbo.GenerateAESKey();
DECLARE @gcmPlainText nvarchar(max) = N'Secret message using AES-GCM!';
PRINT 'Original Text: ' + @gcmPlainText;

-- Encrypt with AES-GCM
DECLARE @gcmEncrypted nvarchar(max) = dbo.EncryptAesGcm(@gcmPlainText, @gcmKey);
PRINT 'AES-GCM Encrypted: ' + @gcmEncrypted;

-- Decrypt with AES-GCM
DECLARE @gcmDecrypted nvarchar(max) = dbo.DecryptAesGcm(@gcmEncrypted, @gcmKey);
PRINT 'AES-GCM Decrypted: ' + @gcmDecrypted;
GO
