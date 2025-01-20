-- Generate a new AES key
DECLARE @key NVARCHAR(MAX) = dbo.GenerateAESKey();

-- Encrypt data
DECLARE @plainText NVARCHAR(MAX) = 'Secret message';
DECLARE @encrypted TABLE (CipherText NVARCHAR(MAX), IV NVARCHAR(MAX));
INSERT INTO @encrypted
SELECT * FROM dbo.EncryptAES(@plainText, @key);

-- Decrypt data
DECLARE @cipherText NVARCHAR(MAX), @iv NVARCHAR(MAX);
SELECT @cipherText = CipherText, @iv = IV FROM @encrypted;
SELECT dbo.DecryptAES(@cipherText, @key, @iv) as DecryptedText;

-- Hash and verify password
DECLARE @hashedPwd NVARCHAR(MAX) = dbo.HashPassword('MyPassword123');
SELECT dbo.VerifyPassword('MyPassword123', @hashedPwd) as IsPasswordValid;