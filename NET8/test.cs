#if !RELEASE_WITHOUT_TESTS
using NUnit.Framework;
using SecureLibrary;
using System;

namespace SecureLibrary.Tests
{
    [TestFixture]
    public class EncryptionHelperTests
    {
        [Test]
        public void TestAesGcmEncryptionDecryption()
        {
            // Arrange
            string plainText = "Hello, World!";
            string key = EncryptionHelper.KeyGenAES256();

            // Act
            string encrypted = EncryptionHelper.EncryptAesGcm(plainText, key);
            string decrypted = EncryptionHelper.DecryptAesGcm(encrypted, key);

            // Assert
            Assert.That(decrypted, Is.EqualTo(plainText));
        }

        [Test]
        public void TestAesCbcEncryptionDecryption()
        {
            // Arrange
            string plainText = "Hello, World!";
            string key = EncryptionHelper.KeyGenAES256();

            // Act
            string[] encryptionResult = EncryptionHelper.EncryptAesCbcWithIv(plainText, key);
            string decrypted = EncryptionHelper.DecryptAesCbcWithIv(encryptionResult[0], key, encryptionResult[1]);

            // Assert
            Assert.That(decrypted, Is.EqualTo(plainText));
        }

        [Test]
        public void TestDiffieHellmanKeyExchange()
        {
            // Arrange
            string[] aliceKeys = EncryptionHelper.GenerateDiffieHellmanKeys();
            string[] bobKeys = EncryptionHelper.GenerateDiffieHellmanKeys();

            // Act
            string aliceSharedKey = EncryptionHelper.DeriveSharedKey(bobKeys[0], aliceKeys[1]);
            string bobSharedKey = EncryptionHelper.DeriveSharedKey(aliceKeys[0], bobKeys[1]);

            // Assert
            Assert.That(aliceSharedKey, Is.EqualTo(bobSharedKey));
        }

        [Test]
        public void TestBcryptPasswordVerification()
        {
            // Arrange
            string password = "MySecurePassword123";

            // Act
            string hashedPassword = EncryptionHelper.BcryptEncoding(password);
            bool isValid = EncryptionHelper.VerifyBcryptPassword(password, hashedPassword);
            bool isInvalid = EncryptionHelper.VerifyBcryptPassword("WrongPassword", hashedPassword);

            // Assert
            Assert.That(isValid, Is.True);
            Assert.That(isInvalid, Is.False);
        }
    }
}
#endif