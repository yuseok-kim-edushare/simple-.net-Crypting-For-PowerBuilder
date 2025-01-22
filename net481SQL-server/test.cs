using NUnit.Framework;
using System.Data.SqlTypes;
using SecureLibrary.SQL;

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
            var encrypted = SqlCLRCrypting.EncryptAES(plainText, key);
            var decrypted = SqlCLRCrypting.DecryptAES(encrypted[0], key, encrypted[1]);
            
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
            var keys = SqlCLRCrypting.GenerateDiffieHellmanKeys();

            // Assert
            Assert.That(!keys[0].Equals(null)); // Public key
            Assert.That(!keys[1].Equals(null)); // Private key
        }

        [Test]
        public void DeriveSharedKey_ShouldDeriveKeySuccessfully()
        {
            // Arrange
            var keys1 = SqlCLRCrypting.GenerateDiffieHellmanKeys();
            var keys2 = SqlCLRCrypting.GenerateDiffieHellmanKeys();

            // Act
            var sharedKey1 = SqlCLRCrypting.DeriveSharedKey(keys2[0], keys1[1]);
            var sharedKey2 = SqlCLRCrypting.DeriveSharedKey(keys1[0], keys2[1]);

            // Assert
            Assert.That(!sharedKey1.Equals(null));
            Assert.That(!sharedKey2.Equals(null));
            Assert.That(sharedKey1.Equals(sharedKey2));
        }
    }
}
