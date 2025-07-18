#if !RELEASE_WITHOUT_TESTS
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureLibrary;
using System;
using System.Security.Cryptography;

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

        // Password-based AES-GCM encryption tests
        [TestMethod]
        public void TestPasswordBasedAesGcmEncryptionDecryption()
        {
            // Arrange
            string plainText = "Hello, World!";
            string password = "MySecurePassword123";

            // Act
            string encrypted = EncryptionHelper.EncryptAesGcmWithPassword(plainText, password);
            string decrypted = EncryptionHelper.DecryptAesGcmWithPassword(encrypted, password);

            // Assert
            Assert.AreEqual(plainText, decrypted);
        }

        [TestMethod]
        public void TestPasswordBasedAesGcmWithCustomIterations()
        {
            // Arrange
            string plainText = "Test with custom iterations";
            string password = "MyPassword";
            int iterations = 5000;

            // Act
            string encrypted = EncryptionHelper.EncryptAesGcmWithPassword(plainText, password, iterations);
            string decrypted = EncryptionHelper.DecryptAesGcmWithPassword(encrypted, password, iterations);

            // Assert
            Assert.AreEqual(plainText, decrypted);
        }

        [TestMethod]
        public void TestPasswordBasedAesGcmDifferentIterationsShouldFail()
        {
            // Arrange
            string plainText = "Test with different iterations";
            string password = "MyPassword";
            int encryptIterations = 2000;
            int decryptIterations = 5000;

            // Act
            string encrypted = EncryptionHelper.EncryptAesGcmWithPassword(plainText, password, encryptIterations);
            
            // Assert - Should throw exception due to wrong iterations
            Assert.ThrowsException<CryptographicException>(() => 
                EncryptionHelper.DecryptAesGcmWithPassword(encrypted, password, decryptIterations));
        }

        [TestMethod]
        public void TestGenerateSalt()
        {
            // Act
            string salt = EncryptionHelper.GenerateSalt();
            
            // Assert
            Assert.IsNotNull(salt);
            Assert.AreEqual(24, salt.Length); // 16 bytes = 24 chars in base64
            
            // Validate it's valid base64
            byte[] saltBytes = Convert.FromBase64String(salt);
            Assert.AreEqual(16, saltBytes.Length);
        }

        [TestMethod]
        public void TestGenerateSaltCustomLength()
        {
            // Arrange
            int saltLength = 32;
            
            // Act
            string salt = EncryptionHelper.GenerateSalt(saltLength);
            
            // Assert
            Assert.IsNotNull(salt);
            
            // Validate it's valid base64 and correct length
            byte[] saltBytes = Convert.FromBase64String(salt);
            Assert.AreEqual(32, saltBytes.Length);
        }

        [TestMethod]
        public void TestGenerateSaltInvalidLength()
        {
            // Arrange
            int tooSmall = 4;  // Below minimum of 8
            int tooLarge = 128; // Above maximum of 64
            
            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => EncryptionHelper.GenerateSalt(tooSmall));
            Assert.ThrowsException<ArgumentException>(() => EncryptionHelper.GenerateSalt(tooLarge));
        }

        [TestMethod]
        public void TestPasswordBasedAesGcmWithCustomSalt()
        {
            // Arrange
            string plainText = "Test with custom salt";
            string password = "MyPassword";
            string salt = EncryptionHelper.GenerateSalt();
            
            // Act
            string encrypted = EncryptionHelper.EncryptAesGcmWithPasswordAndSalt(plainText, password, salt);
            string decrypted = EncryptionHelper.DecryptAesGcmWithPassword(encrypted, password);
            
            // Assert
            Assert.AreEqual(plainText, decrypted);
        }

        [TestMethod]
        public void TestPasswordBasedAesGcmWithCustomSaltAndIterations()
        {
            // Arrange
            string plainText = "Test with custom salt and iterations";
            string password = "MyPassword";
            string salt = EncryptionHelper.GenerateSalt(24);
            int iterations = 3000;
            
            // Act
            string encrypted = EncryptionHelper.EncryptAesGcmWithPasswordAndSalt(plainText, password, salt, iterations);
            string decrypted = EncryptionHelper.DecryptAesGcmWithPassword(encrypted, password, iterations);
            
            // Assert
            Assert.AreEqual(plainText, decrypted);
        }

        [TestMethod]
        public void TestPasswordBasedAesGcmCrossCompatibility()
        {
            // Test that the same password produces different encrypted results (due to random salt/nonce)
            string plainText = "Same text, different encryption";
            string password = "MyPassword";
            
            // Act - Encrypt same text twice
            string encrypted1 = EncryptionHelper.EncryptAesGcmWithPassword(plainText, password);
            string encrypted2 = EncryptionHelper.EncryptAesGcmWithPassword(plainText, password);
            
            // Assert - Results should be different due to random salt/nonce
            Assert.AreNotEqual(encrypted1, encrypted2);
            
            // But both should decrypt to the same plaintext
            string decrypted1 = EncryptionHelper.DecryptAesGcmWithPassword(encrypted1, password);
            string decrypted2 = EncryptionHelper.DecryptAesGcmWithPassword(encrypted2, password);
            
            Assert.AreEqual(plainText, decrypted1);
            Assert.AreEqual(plainText, decrypted2);
        }

        [TestMethod]
        public void TestPasswordBasedAesGcmInvalidInputs()
        {
            // Test null inputs
            Assert.ThrowsException<ArgumentNullException>(() => 
                EncryptionHelper.EncryptAesGcmWithPassword(null, "password"));
            Assert.ThrowsException<ArgumentNullException>(() => 
                EncryptionHelper.EncryptAesGcmWithPassword("text", null));
            Assert.ThrowsException<ArgumentNullException>(() => 
                EncryptionHelper.DecryptAesGcmWithPassword(null, "password"));
            Assert.ThrowsException<ArgumentNullException>(() => 
                EncryptionHelper.DecryptAesGcmWithPassword("data", null));
        }

        [TestMethod]
        public void TestPasswordBasedAesGcmInvalidIterations()
        {
            // Arrange
            string plainText = "Test invalid iterations";
            string password = "MyPassword";
            int tooLow = 500;   // Below minimum of 1000
            int tooHigh = 200000; // Above maximum of 100000
            
            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => 
                EncryptionHelper.EncryptAesGcmWithPassword(plainText, password, tooLow));
            Assert.ThrowsException<ArgumentException>(() => 
                EncryptionHelper.EncryptAesGcmWithPassword(plainText, password, tooHigh));
        }

        [TestMethod]
        public void TestPasswordBasedAesGcmLargeData()
        {
            // Arrange
            string largeText = new string('A', 10000); // 10KB of data
            string password = "MyPassword";
            
            // Act
            string encrypted = EncryptionHelper.EncryptAesGcmWithPassword(largeText, password);
            string decrypted = EncryptionHelper.DecryptAesGcmWithPassword(encrypted, password);
            
            // Assert
            Assert.AreEqual(largeText, decrypted);
        }

        [TestMethod]
        public void TestPasswordBasedAesGcmSpecialCharacters()
        {
            // Arrange
            string specialText = "Special chars: !@#$%^&*()_+-=[]{}|;':\",./<>?`~";
            string password = "Password with spaces and symbols: !@#";
            
            // Act
            string encrypted = EncryptionHelper.EncryptAesGcmWithPassword(specialText, password);
            string decrypted = EncryptionHelper.DecryptAesGcmWithPassword(encrypted, password);
            
            // Assert
            Assert.AreEqual(specialText, decrypted);
        }

        [TestMethod]
        public void TestPasswordBasedAesGcmUnicodeCharacters()
        {
            // Arrange
            string unicodeText = "Unicode: 你好世界 🌍 🚀 こんにちは";
            string password = "Unicode password: パスワード";
            
            // Act
            string encrypted = EncryptionHelper.EncryptAesGcmWithPassword(unicodeText, password);
            string decrypted = EncryptionHelper.DecryptAesGcmWithPassword(encrypted, password);
            
            // Assert
            Assert.AreEqual(unicodeText, decrypted);
        }
    }
}
#endif