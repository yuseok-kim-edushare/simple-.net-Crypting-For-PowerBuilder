#if !RELEASE_WITHOUT_TESTS
using NUnit.Framework;
using System.Data.SqlTypes;
using SecureLibrary.SQL;
using System;
using System.Linq;

namespace SecureLibrary.Tests 
{
    [TestFixture]
    public class SqlCLRCryptingTests
    {
        private SqlString plainText;
        private SqlString key;
        private SqlString password;

        [SetUp]
        public void Setup()
        {
            plainText = new SqlString("This is a test string");
            key = SqlCLRCrypting.GenerateAESKey();
            password = new SqlString("securePassword123");
        }
        
        [Test]
        public void GenerateAESKey_ShouldGenerateValidKey()
        {
            // Act
            var generatedKey = SqlCLRCrypting.GenerateAESKey();
            
            // Assert
            Assert.That(generatedKey, Is.Not.Null, "Generated key should not be null");
            Assert.That(generatedKey.Value.Length, Is.EqualTo(44), "Key length should be 44 characters");
        }
        
        [Test]
        public void EncryptAES_ShouldEncryptAndDecryptSuccessfully()
        {
            // Act
            var encryptResult = SqlCLRCrypting.EncryptAES(plainText, key).Cast<SqlString[]>().First();
            var decrypted = SqlCLRCrypting.DecryptAES(encryptResult[0], key, encryptResult[1]);
            
            // Assert
            Assert.That(decrypted.Value, Is.EqualTo(plainText.Value), "Decrypted text should match original");
        }
        
        [Test]
        public void HashPassword_ShouldHashPasswordSuccessfully()
        {
            // Act
            var hashedPassword = SqlCLRCrypting.HashPassword(password);
            
            // Assert
            Assert.That(!string.IsNullOrEmpty(hashedPassword.Value), "Hashed password should not be null");
            Assert.That(hashedPassword.Value, Does.StartWith("$2"), "Hash should be in BCrypt format");
        }
        
        [Test]
        public void VerifyPassword_ShouldVerifyPasswordSuccessfully()
        {
            // Arrange
            var hashedPassword = SqlCLRCrypting.HashPassword(password);
            
            // Act
            var isValid = SqlCLRCrypting.VerifyPassword(password, hashedPassword);
            
            // Assert
            Assert.That(isValid.IsTrue, Is.True, "Password verification should succeed");
        }

        [Test]
        public void GenerateDiffieHellmanKeys_ShouldGenerateValidKeys()
        {
            // Act
            var keysResult = SqlCLRCrypting.GenerateDiffieHellmanKeys().Cast<SqlString[]>().First();

            // Assert
            Assert.That(!keysResult[0].IsNull, "Public key should not be null");
            Assert.That(!keysResult[1].IsNull, "Private key should not be null");
        }

        [Test]
        public void DeriveSharedKey_ShouldDeriveKeySuccessfully()
        {
            // Arrange
            var keys1 = SqlCLRCrypting.GenerateDiffieHellmanKeys().Cast<SqlString[]>().First();
            var keys2 = SqlCLRCrypting.GenerateDiffieHellmanKeys().Cast<SqlString[]>().First();

            // Act
            var sharedKey1 = SqlCLRCrypting.DeriveSharedKey(keys2[0], keys1[1]);
            var sharedKey2 = SqlCLRCrypting.DeriveSharedKey(keys1[0], keys2[1]);

            // Assert
            Assert.That(!sharedKey1.IsNull, "Shared key 1 should not be null");
            Assert.That(!sharedKey2.IsNull, "Shared key 2 should not be null");
            Assert.That(sharedKey1.Value, Is.EqualTo(sharedKey2.Value), "Shared keys should match");
        }

        [Test]
        public void EncryptAesGcm_ShouldEncryptAndDecryptSuccessfully()
        {
            // Arrange
            Assert.That(plainText.IsNull, Is.False, "Input plainText should not be null");
            Assert.That(key.IsNull, Is.False, "Input key should not be null");
            
            // Validate key length
            byte[] keyBytes = Convert.FromBase64String(key.Value);
            Assert.That(keyBytes.Length, Is.EqualTo(32), "Key must be 32 bytes");
            
            // Act - Encryption
            var encrypted = SqlCLRCrypting.EncryptAesGcm(plainText, key);
            Assert.That(encrypted.IsNull, Is.False, "Encrypted value should not be null");
            
            // Validate encrypted format
            string[] parts = encrypted.Value.Split(':');
            Assert.That(parts.Length, Is.EqualTo(2), "Encrypted value should contain nonce and ciphertext");
            Assert.That(Convert.FromBase64String(parts[0]).Length, Is.EqualTo(12), "Nonce should be 12 bytes");
            
            // Act - Decryption
            var decrypted = SqlCLRCrypting.DecryptAesGcm(encrypted, key);
            Assert.That(decrypted.IsNull, Is.False, "Decrypted value should not be null");
            
            // Assert
            Assert.That(decrypted.Value, Is.EqualTo(plainText.Value), "Decrypted text should match original");
        }
    }
}
#endif