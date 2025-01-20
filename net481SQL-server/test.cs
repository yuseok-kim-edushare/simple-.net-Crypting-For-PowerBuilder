using NUnit.Framework;
using SecureLibrary.SQL;
using System;
using System.Data.SqlTypes;

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
            Assert.That(!generatedKey.IsNull);
            Assert.That(generatedKey.Value.Length, Is.EqualTo(44)); // Base64 length of 256-bit key
        }
        
        [Test]
        public void EncryptAES_ShouldEncryptAndDecryptSuccessfully()
        {
            // Act
            var encrypted = SqlCLRCrypting.EncryptAES(plainText, key);
            var decrypted = SqlCLRCrypting.DecryptAES(encrypted[0], key, encrypted[1]);
            
            // Assert
            Assert.That(decrypted.Value, Is.EqualTo(plainText.Value));
        }
        
        [Test]
        public void HashPassword_ShouldHashPasswordSuccessfully()
        {
            // Act
            var hashedPassword = SqlCLRCrypting.HashPassword(password);
            
            // Assert
            Assert.That(!hashedPassword.IsNull);
        }
        
        [Test]
        public void VerifyPassword_ShouldVerifyPasswordSuccessfully()
        {
            // Arrange
            var hashedPassword = SqlCLRCrypting.HashPassword(password);
            
            // Act
            var isValid = SqlCLRCrypting.VerifyPassword(password, hashedPassword);
            
            // Assert
            Assert.That(isValid.IsTrue);
        }
    }
}
