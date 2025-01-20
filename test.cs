using NUnit.Framework;
using SecureLibrary;
using System;
using System.Text;

namespace SecureLibrary.Tests
{
    [TestFixture]
    public class EncryptionHelperTests
    {
        private string plainText;
        private string key;
        private string password;

        [SetUp]
        public void Setup()
        {
            plainText = "This is a test string";
            key = EncryptionHelper.KeyGenAES256();
            password = "securePassword123";
        }

        [Test]
        public void EncryptAesCbcWithIv_ShouldEncryptAndDecryptSuccessfully()
        {
            // Act
            var encrypted = EncryptionHelper.EncryptAesCbcWithIv(plainText, key);
            var decrypted = EncryptionHelper.DecryptAesCbcWithIv(encrypted[0], key, encrypted[1]);

            // Assert
            Assert.AreEqual(plainText, decrypted);
        }

        [Test]
        public void KeyGenAES256_ShouldGenerateValidKey()
        {
            // Act
            var generatedKey = EncryptionHelper.KeyGenAES256();

            // Assert
            Assert.IsNotNull(generatedKey);
            Assert.AreEqual(44, generatedKey.Length); // Base64 length of 256-bit key
        }

        [Test]
        public void GenerateDiffieHellmanKeys_ShouldGenerateKeysSuccessfully()
        {
            // Act
            var keys = EncryptionHelper.GenerateDiffieHellmanKeys();

            // Assert
            Assert.IsNotNull(keys);
            Assert.AreEqual(2, keys.Length);
            Assert.IsNotEmpty(keys[0]); // Public key
            Assert.IsNotEmpty(keys[1]); // Private key
        }

        [Test]
        public void DeriveSharedKey_ShouldDeriveKeySuccessfully()
        {
            // Arrange
            var keys1 = EncryptionHelper.GenerateDiffieHellmanKeys();
            var keys2 = EncryptionHelper.GenerateDiffieHellmanKeys();

            // Act
            var sharedKey1 = EncryptionHelper.DeriveSharedKey(keys2[0], keys1[1]);
            var sharedKey2 = EncryptionHelper.DeriveSharedKey(keys1[0], keys2[1]);

            // Assert
            Assert.IsNotNull(sharedKey1);
            Assert.IsNotNull(sharedKey2);
            Assert.AreEqual(sharedKey1, sharedKey2);
        }

        [Test]
        public void BcryptEncoding_ShouldEncodeAndVerifyPasswordSuccessfully()
        {
            // Act
            var hashedPassword = EncryptionHelper.BcryptEncoding(password);
            var isValid = EncryptionHelper.VerifyBcryptPassword(password, hashedPassword);

            // Assert
            Assert.IsTrue(isValid);
        }
    }
}
