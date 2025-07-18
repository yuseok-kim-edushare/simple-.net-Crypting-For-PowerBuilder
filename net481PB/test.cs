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
        private string plainText;
        private string key;
        private string password;
        
        [TestInitialize]
        public void Setup()
        {
            plainText = "This is a test string";
            key = EncryptionHelper.KeyGenAES256();
            password = "securePassword123";
        }
        [TestMethod]
        public void EncryptAesGcm_ShouldEncryptAndDecryptSuccessfully()
        {
            // Act
            var encrypted = EncryptionHelper.EncryptAesGcm(plainText, key);
            var decrypted = EncryptionHelper.DecryptAesGcm(encrypted, key);

            // Assert
            Assert.AreEqual(plainText, decrypted);
        }

        [TestMethod]
        public void EncryptAesCbcWithIv_ShouldEncryptAndDecryptSuccessfully()
        {
            // Act
            var encrypted = EncryptionHelper.EncryptAesCbcWithIv(plainText, key);
            var decrypted = EncryptionHelper.DecryptAesCbcWithIv(encrypted[0], key, encrypted[1]);
            
            // Assert
            Assert.AreEqual(plainText, decrypted);
        }
        
        [TestMethod]
        public void KeyGenAES256_ShouldGenerateValidKey()
        {
            // Act
            var generatedKey = EncryptionHelper.KeyGenAES256();
            
            // Assert
            Assert.IsNotNull(generatedKey);
            Assert.AreEqual(44, generatedKey.Length); // Base64 length of 256-bit key
        }
        
        [TestMethod]
        public void GenerateDiffieHellmanKeys_ShouldGenerateKeysSuccessfully()
        {
            // Act
            var keys = EncryptionHelper.GenerateDiffieHellmanKeys();
            
            // Assert
            Assert.IsNotNull(keys);
            Assert.AreEqual(2, keys.Length);
            Assert.IsNotNull(keys[0]); // Public key
            Assert.IsNotNull(keys[1]); // Private key
        }
        
        [TestMethod]
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
        
        [TestMethod]
        public void BcryptEncoding_ShouldEncodeAndVerifyPasswordSuccessfully()
        {
            // Act
            var hashedPassword = EncryptionHelper.BcryptEncoding(password);
            var isValid = EncryptionHelper.VerifyBcryptPassword(password, hashedPassword);
            
            // Assert
            Assert.IsTrue(isValid);
        }

        // Password-based AES-GCM encryption tests
        [TestMethod]
        public void EncryptAesGcmWithPassword_ShouldEncryptAndDecryptSuccessfully()
        {
            // Arrange
            string plainText = "This is a test string for password-based encryption";
            string password = "mySecurePassword123";
            
            // Act
            string encrypted = EncryptionHelper.EncryptAesGcmWithPassword(plainText, password);
            string decrypted = EncryptionHelper.DecryptAesGcmWithPassword(encrypted, password);
            
            // Assert
            Assert.AreEqual(plainText, decrypted);
        }

        [TestMethod]
        public void EncryptAesGcmWithPassword_CustomIterations_ShouldWork()
        {
            // Arrange
            string plainText = "Test with custom iterations";
            string password = "myPassword";
            int iterations = 5000;
            
            // Act
            string encrypted = EncryptionHelper.EncryptAesGcmWithPassword(plainText, password, iterations);
            string decrypted = EncryptionHelper.DecryptAesGcmWithPassword(encrypted, password, iterations);
            
            // Assert
            Assert.AreEqual(plainText, decrypted);
        }

        [TestMethod]
        public void EncryptAesGcmWithPassword_DifferentIterationsShouldFail()
        {
            // Arrange
            string plainText = "Test with different iterations";
            string password = "myPassword";
            int encryptIterations = 2000;
            int decryptIterations = 5000;
            
            // Act
            string encrypted = EncryptionHelper.EncryptAesGcmWithPassword(plainText, password, encryptIterations);
            
            // Assert - Should throw exception due to wrong iterations
            Assert.ThrowsException<CryptographicException>(() => 
                EncryptionHelper.DecryptAesGcmWithPassword(encrypted, password, decryptIterations));
        }

        [TestMethod]
        public void GenerateSalt_ShouldGenerateValidSalt()
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
        public void GenerateSalt_CustomLength_ShouldWork()
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
        public void GenerateSalt_InvalidLength_ShouldThrowException()
        {
            // Arrange
            int tooSmall = 4;  // Below minimum of 8
            int tooLarge = 128; // Above maximum of 64
            
            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => EncryptionHelper.GenerateSalt(tooSmall));
            Assert.ThrowsException<ArgumentException>(() => EncryptionHelper.GenerateSalt(tooLarge));
        }

        [TestMethod]
        public void EncryptAesGcmWithPasswordAndSalt_ShouldWork()
        {
            // Arrange
            string plainText = "Test with custom salt";
            string password = "myPassword";
            string salt = EncryptionHelper.GenerateSalt();
            
            // Act
            string encrypted = EncryptionHelper.EncryptAesGcmWithPasswordAndSalt(plainText, password, salt);
            string decrypted = EncryptionHelper.DecryptAesGcmWithPassword(encrypted, password);
            
            // Assert
            Assert.AreEqual(plainText, decrypted);
        }

        [TestMethod]
        public void EncryptAesGcmWithPasswordAndSalt_CustomIterations_ShouldWork()
        {
            // Arrange
            string plainText = "Test with custom salt and iterations";
            string password = "myPassword";
            string salt = EncryptionHelper.GenerateSalt(24);
            int iterations = 3000;
            
            // Act
            string encrypted = EncryptionHelper.EncryptAesGcmWithPasswordAndSalt(plainText, password, salt, iterations);
            string decrypted = EncryptionHelper.DecryptAesGcmWithPassword(encrypted, password, iterations);
            
            // Assert
            Assert.AreEqual(plainText, decrypted);
        }

        [TestMethod]
        public void PasswordBasedEncryption_CrossCompatibility_ShouldWork()
        {
            // Test that the same password produces different encrypted results (due to random salt/nonce)
            string plainText = "Same text, different encryption";
            string password = "myPassword";
            
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
        public void PasswordBasedEncryption_InvalidInputs_ShouldThrowException()
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
        public void PasswordBasedEncryption_InvalidIterations_ShouldThrowException()
        {
            // Arrange
            string plainText = "Test invalid iterations";
            string password = "myPassword";
            int tooLow = 500;   // Below minimum of 1000
            int tooHigh = 200000; // Above maximum of 100000
            
            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => 
                EncryptionHelper.EncryptAesGcmWithPassword(plainText, password, tooLow));
            Assert.ThrowsException<ArgumentException>(() => 
                EncryptionHelper.EncryptAesGcmWithPassword(plainText, password, tooHigh));
        }

        [TestMethod]
        public void PasswordBasedEncryption_LargeData_ShouldWork()
        {
            // Arrange
            string largeText = new string('A', 10000); // 10KB of data
            string password = "myPassword";
            
            // Act
            string encrypted = EncryptionHelper.EncryptAesGcmWithPassword(largeText, password);
            string decrypted = EncryptionHelper.DecryptAesGcmWithPassword(encrypted, password);
            
            // Assert
            Assert.AreEqual(largeText, decrypted);
        }

        [TestMethod]
        public void PasswordBasedEncryption_SpecialCharacters_ShouldWork()
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
        public void PasswordBasedEncryption_UnicodeCharacters_ShouldWork()
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