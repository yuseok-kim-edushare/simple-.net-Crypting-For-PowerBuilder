#if !RELEASE_WITHOUT_TESTS
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureLibrary;
using System;

namespace SecureLibrary.Tests
{
    [TestClass]
    public class EncryptionHelperTests
    {
        [TestMethod]
        public void TestAesGcmEncryptionDecryption()
        {
            // Arrange
            string plainText = "Hello, World!";
            string key = EncryptionHelper.KeyGenAES256();

            // Act
            string encrypted = EncryptionHelper.EncryptAesGcm(plainText, key);
            string decrypted = EncryptionHelper.DecryptAesGcm(encrypted, key);

            // Assert
            Assert.AreEqual(plainText, decrypted);
        }

        [TestMethod]
        public void TestAesCbcEncryptionDecryption()
        {
            // Arrange
            string plainText = "Hello, World!";
            string key = EncryptionHelper.KeyGenAES256();

            // Act
            string[] encryptionResult = EncryptionHelper.EncryptAesCbcWithIv(plainText, key);
            string decrypted = EncryptionHelper.DecryptAesCbcWithIv(encryptionResult[0], key, encryptionResult[1]);

            // Assert
            Assert.AreEqual(plainText, decrypted);
        }

        [TestMethod]
        public void TestDiffieHellmanKeyExchange()
        {
            // Arrange
            string[] aliceKeys = EncryptionHelper.GenerateDiffieHellmanKeys();
            string[] bobKeys = EncryptionHelper.GenerateDiffieHellmanKeys();

            // Act
            string aliceSharedKey = EncryptionHelper.DeriveSharedKey(bobKeys[0], aliceKeys[1]);
            string bobSharedKey = EncryptionHelper.DeriveSharedKey(aliceKeys[0], bobKeys[1]);

            // Assert
            Assert.AreEqual(aliceSharedKey, bobSharedKey);
        }

        [TestMethod]
        public void TestBcryptPasswordVerification()
        {
            // Arrange
            string password = "MySecurePassword123";

            // Act
            string hashedPassword = EncryptionHelper.BcryptEncoding(password);
            bool isValid = EncryptionHelper.VerifyBcryptPassword(password, hashedPassword);
            bool isInvalid = EncryptionHelper.VerifyBcryptPassword("WrongPassword", hashedPassword);

            // Assert
            Assert.IsTrue(isValid);
            Assert.IsFalse(isInvalid);
        }
    }
}
#endif