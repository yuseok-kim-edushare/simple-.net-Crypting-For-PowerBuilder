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
            #pragma warning disable CS0618
            // This is a test method for the AES-CBC encryption with IV
            // so we need to disable the warning
            // Act
            var encrypted = EncryptionHelper.EncryptAesCbcWithIv(plainText, key);
            var decrypted = EncryptionHelper.DecryptAesCbcWithIv(encrypted[0], key, encrypted[1]);
            #pragma warning restore CS0618
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
            Assert.ThrowsExactly<CryptographicException>(() => 
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
            Assert.ThrowsExactly<ArgumentException>(() => EncryptionHelper.GenerateSalt(tooSmall));
            Assert.ThrowsExactly<ArgumentException>(() => EncryptionHelper.GenerateSalt(tooLarge));
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
            Assert.ThrowsExactly<ArgumentNullException>(() => 
                EncryptionHelper.EncryptAesGcmWithPassword(null, "password"));
            Assert.ThrowsExactly<ArgumentNullException>(() => 
                EncryptionHelper.EncryptAesGcmWithPassword("text", null));
            Assert.ThrowsExactly<ArgumentNullException>(() => 
                EncryptionHelper.DecryptAesGcmWithPassword(null, "password"));
            Assert.ThrowsExactly<ArgumentNullException>(() => 
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
            Assert.ThrowsExactly<ArgumentException>(() => 
                EncryptionHelper.EncryptAesGcmWithPassword(plainText, password, tooLow));
            Assert.ThrowsExactly<ArgumentException>(() => 
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

        // Derived Key Tests
        [TestMethod]
        public void DeriveKeyFromPassword_ShouldDeriveValidKey()
        {
            // Arrange
            string password = "TestPassword123";
            string salt = EncryptionHelper.GenerateSalt();
            int iterations = 2000;
            
            // Act
            string derivedKey = EncryptionHelper.DeriveKeyFromPassword(password, salt, iterations);
            
            // Assert
            Assert.IsNotNull(derivedKey);
            Assert.AreEqual(44, derivedKey.Length); // 32 bytes = 44 chars in base64
            
            // Validate it's valid base64 and correct length
            byte[] keyBytes = Convert.FromBase64String(derivedKey);
            Assert.AreEqual(32, keyBytes.Length); // 256-bit key
        }

        [TestMethod]
        public void DeriveKeyFromPassword_CustomIterations_ShouldWork()
        {
            // Arrange
            string password = "TestPassword123";
            string salt = EncryptionHelper.GenerateSalt(24);
            int iterations = 5000;
            
            // Act
            string derivedKey = EncryptionHelper.DeriveKeyFromPassword(password, salt, iterations);
            
            // Assert
            Assert.IsNotNull(derivedKey);
            byte[] keyBytes = Convert.FromBase64String(derivedKey);
            Assert.AreEqual(32, keyBytes.Length);
        }

        [TestMethod]
        public void DeriveKeyFromPassword_Consistency_ShouldWork()
        {
            // Arrange
            string password = "TestPassword123";
            string salt = EncryptionHelper.GenerateSalt();
            int iterations = 2000;
            
            // Act - Derive key multiple times with same parameters
            string key1 = EncryptionHelper.DeriveKeyFromPassword(password, salt, iterations);
            string key2 = EncryptionHelper.DeriveKeyFromPassword(password, salt, iterations);
            string key3 = EncryptionHelper.DeriveKeyFromPassword(password, salt, iterations);
            
            // Assert - All should be identical
            Assert.AreEqual(key1, key2);
            Assert.AreEqual(key2, key3);
            Assert.AreEqual(key1, key3);
        }

        [TestMethod]
        public void DeriveKeyFromPassword_InvalidInputs_ShouldThrowException()
        {
            // Test null inputs
            Assert.ThrowsExactly<ArgumentNullException>(() => 
                EncryptionHelper.DeriveKeyFromPassword(null, "salt"));
            Assert.ThrowsExactly<ArgumentNullException>(() => 
                EncryptionHelper.DeriveKeyFromPassword("password", null));
            
            // Test invalid salt
            Assert.ThrowsExactly<ArgumentException>(() => 
                EncryptionHelper.DeriveKeyFromPassword("password", Convert.ToBase64String(new byte[4])));
            
            // Test invalid iterations
            Assert.ThrowsExactly<ArgumentException>(() => 
                EncryptionHelper.DeriveKeyFromPassword("password", EncryptionHelper.GenerateSalt(), 500));
            Assert.ThrowsExactly<ArgumentException>(() => 
                EncryptionHelper.DeriveKeyFromPassword("password", EncryptionHelper.GenerateSalt(), 200000));
        }

        [TestMethod]
        public void EncryptAesGcmWithDerivedKey_ShouldEncryptSuccessfully()
        {
            // Arrange
            string plainText = "Test with derived key";
            string password = "myPassword";
            string salt = EncryptionHelper.GenerateSalt();
            string derivedKey = EncryptionHelper.DeriveKeyFromPassword(password, salt);
            
            // Act
            string encrypted = EncryptionHelper.EncryptAesGcmWithDerivedKey(plainText, derivedKey, salt);
            
            // Assert
            Assert.IsNotNull(encrypted);
            Assert.AreNotEqual(plainText, encrypted);
        }

        [TestMethod]
        public void DecryptAesGcmWithDerivedKey_ShouldDecryptSuccessfully()
        {
            // Arrange
            string plainText = "Test decryption with derived key";
            string password = "myPassword";
            string salt = EncryptionHelper.GenerateSalt();
            string derivedKey = EncryptionHelper.DeriveKeyFromPassword(password, salt);
            string encrypted = EncryptionHelper.EncryptAesGcmWithDerivedKey(plainText, derivedKey, salt);
            
            // Act
            string decrypted = EncryptionHelper.DecryptAesGcmWithDerivedKey(encrypted, derivedKey);
            
            // Assert
            Assert.AreEqual(plainText, decrypted);
        }

        [TestMethod]
        public void DerivedKeyEncryptionDecryption_CrossCompatibility_ShouldWork()
        {
            // Test that derived key encryption/decryption works with password-based methods
            string plainText = "Cross compatibility test";
            string password = "myPassword";
            string salt = EncryptionHelper.GenerateSalt();
            int iterations = 2000;
            
            // Derive key
            string derivedKey = EncryptionHelper.DeriveKeyFromPassword(password, salt, iterations);
            
            // Encrypt with derived key
            string encryptedWithKey = EncryptionHelper.EncryptAesGcmWithDerivedKey(plainText, derivedKey, salt);
            
            // Decrypt with password-based method
            string decryptedWithPassword = EncryptionHelper.DecryptAesGcmWithPassword(encryptedWithKey, password, iterations);
            
            // Assert
            Assert.AreEqual(plainText, decryptedWithPassword);
        }

        [TestMethod]
        public void DerivedKeyEncryptionDecryption_ReverseCrossCompatibility_ShouldWork()
        {
            // Test that password-based encryption works with derived key decryption
            string plainText = "Reverse cross compatibility test";
            string password = "myPassword";
            string salt = EncryptionHelper.GenerateSalt();
            int iterations = 2000;
            
            // Derive key
            string derivedKey = EncryptionHelper.DeriveKeyFromPassword(password, salt, iterations);
            
            // Encrypt with password-based method
            string encryptedWithPassword = EncryptionHelper.EncryptAesGcmWithPasswordAndSalt(plainText, password, salt, iterations);
            
            // Decrypt with derived key
            string decryptedWithKey = EncryptionHelper.DecryptAesGcmWithDerivedKey(encryptedWithPassword, derivedKey);
            
            // Assert
            Assert.AreEqual(plainText, decryptedWithKey);
        }

        [TestMethod]
        public void DerivedKeyEncryption_InvalidInputs_ShouldThrowException()
        {
            // Test null inputs for encryption with derived key
            Assert.ThrowsExactly<ArgumentNullException>(() => 
                EncryptionHelper.EncryptAesGcmWithDerivedKey(null, "key", "salt"));
            Assert.ThrowsExactly<ArgumentNullException>(() => 
                EncryptionHelper.EncryptAesGcmWithDerivedKey("text", null, "salt"));
            Assert.ThrowsExactly<ArgumentNullException>(() => 
                EncryptionHelper.EncryptAesGcmWithDerivedKey("text", "key", null));
            
            // Test invalid key length
            Assert.ThrowsExactly<ArgumentException>(() => 
                EncryptionHelper.EncryptAesGcmWithDerivedKey("text", Convert.ToBase64String(new byte[16]), EncryptionHelper.GenerateSalt()));
            
            // Test invalid salt
            string validKey = EncryptionHelper.DeriveKeyFromPassword("password", EncryptionHelper.GenerateSalt());
            Assert.ThrowsExactly<ArgumentException>(() => 
                EncryptionHelper.EncryptAesGcmWithDerivedKey("text", validKey, Convert.ToBase64String(new byte[4])));
        }

        [TestMethod]
        public void DerivedKeyDecryption_InvalidInputs_ShouldThrowException()
        {
            // Test null inputs for decryption with derived key
            Assert.ThrowsExactly<ArgumentNullException>(() => 
                EncryptionHelper.DecryptAesGcmWithDerivedKey(null, "key"));
            Assert.ThrowsExactly<ArgumentNullException>(() => 
                EncryptionHelper.DecryptAesGcmWithDerivedKey("data", null));
            
            // Test invalid key length
            Assert.ThrowsExactly<ArgumentException>(() => 
                EncryptionHelper.DecryptAesGcmWithDerivedKey("dummy", Convert.ToBase64String(new byte[16])));
        }

        [TestMethod]
        public void DerivedKeyEncryption_LargeData_ShouldWork()
        {
            // Arrange
            string largeText = new string('A', 10000); // 10KB of data
            string password = "myPassword";
            string salt = EncryptionHelper.GenerateSalt();
            string derivedKey = EncryptionHelper.DeriveKeyFromPassword(password, salt);
            
            // Act
            string encrypted = EncryptionHelper.EncryptAesGcmWithDerivedKey(largeText, derivedKey, salt);
            string decrypted = EncryptionHelper.DecryptAesGcmWithDerivedKey(encrypted, derivedKey);
            
            // Assert
            Assert.AreEqual(largeText, decrypted);
        }

        [TestMethod]
        public void DerivedKeyEncryption_SpecialCharacters_ShouldWork()
        {
            // Arrange
            string specialText = "Special chars: !@#$%^&*()_+-=[]{}|;':\",./<>?`~";
            string password = "Password with spaces and symbols: !@#";
            string salt = EncryptionHelper.GenerateSalt();
            string derivedKey = EncryptionHelper.DeriveKeyFromPassword(password, salt);
            
            // Act
            string encrypted = EncryptionHelper.EncryptAesGcmWithDerivedKey(specialText, derivedKey, salt);
            string decrypted = EncryptionHelper.DecryptAesGcmWithDerivedKey(encrypted, derivedKey);
            
            // Assert
            Assert.AreEqual(specialText, decrypted);
        }

        [TestMethod]
        public void DerivedKeyEncryption_UnicodeCharacters_ShouldWork()
        {
            // Arrange
            string unicodeText = "Unicode: 你好世界 🌍 🚀 こんにちは";
            string password = "Unicode password: パスワード";
            string salt = EncryptionHelper.GenerateSalt();
            string derivedKey = EncryptionHelper.DeriveKeyFromPassword(password, salt);
            
            // Act
            string encrypted = EncryptionHelper.EncryptAesGcmWithDerivedKey(unicodeText, derivedKey, salt);
            string decrypted = EncryptionHelper.DecryptAesGcmWithDerivedKey(encrypted, derivedKey);
            
            // Assert
            Assert.AreEqual(unicodeText, decrypted);
        }

        [TestMethod]
        public void DerivedKeyEncryption_DifferentSaltsShouldFail()
        {
            // Arrange
            string plainText = "Test with different salts";
            string password = "myPassword";
            string salt1 = EncryptionHelper.GenerateSalt();
            string salt2 = EncryptionHelper.GenerateSalt();
            string derivedKey1 = EncryptionHelper.DeriveKeyFromPassword(password, salt1);
            string derivedKey2 = EncryptionHelper.DeriveKeyFromPassword(password, salt2);
            
            // Act
            string encrypted = EncryptionHelper.EncryptAesGcmWithDerivedKey(plainText, derivedKey1, salt1);
            
            // Assert - Should throw exception when trying to decrypt with key derived from different salt
            Assert.ThrowsExactly<CryptographicException>(() => 
                EncryptionHelper.DecryptAesGcmWithDerivedKey(encrypted, derivedKey2));
        }

        [TestMethod]
        public void DerivedKeyEncryption_WrongKeyShouldFail()
        {
            // Arrange
            string plainText = "Test with wrong key";
            string password1 = "myPassword1";
            string password2 = "myPassword2";
            string salt = EncryptionHelper.GenerateSalt();
            string derivedKey1 = EncryptionHelper.DeriveKeyFromPassword(password1, salt);
            string derivedKey2 = EncryptionHelper.DeriveKeyFromPassword(password2, salt);
            
            // Act
            string encrypted = EncryptionHelper.EncryptAesGcmWithDerivedKey(plainText, derivedKey1, salt);
            
            // Assert - Should throw exception when trying to decrypt with wrong key
            Assert.ThrowsExactly<CryptographicException>(() => 
                EncryptionHelper.DecryptAesGcmWithDerivedKey(encrypted, derivedKey2));
        }

        // Additional Error Handling and Edge Case Tests
        [TestMethod]
        public void AesGcmEncryption_InvalidKeyLength_ShouldThrowException()
        {
            // Arrange
            string plainText = "Test invalid key length";
            string invalidKey = Convert.ToBase64String(new byte[16]); // 128-bit key instead of 256-bit
            
            // Act & Assert
            Assert.ThrowsExactly<ArgumentException>(() => 
                EncryptionHelper.EncryptAesGcm(plainText, invalidKey));
        }

        [TestMethod]
        public void AesGcmDecryption_InvalidKeyLength_ShouldThrowException()
        {
            // Arrange
            string validKey = EncryptionHelper.KeyGenAES256();
            string encrypted = EncryptionHelper.EncryptAesGcm("test", validKey);
            string invalidKey = Convert.ToBase64String(new byte[16]); // 128-bit key instead of 256-bit
            
            // Act & Assert
            Assert.ThrowsExactly<ArgumentException>(() => 
                EncryptionHelper.DecryptAesGcm(encrypted, invalidKey));
        }

        [TestMethod]
        public void AesGcmDecryption_InvalidDataFormat_ShouldThrowException()
        {
            // Arrange
            string key = EncryptionHelper.KeyGenAES256();
            string invalidData = "invalid:data:format";
            
            // Act & Assert
            Assert.ThrowsExactly<ArgumentException>(() => 
                EncryptionHelper.DecryptAesGcm(invalidData, key));
        }

        [TestMethod]
        public void AesGcmDecryption_InvalidNonceLength_ShouldThrowException()
        {
            // Arrange
            string key = EncryptionHelper.KeyGenAES256();
            string invalidNonce = Convert.ToBase64String(new byte[8]); // 64-bit nonce instead of 96-bit
            string dummyData = Convert.ToBase64String(new byte[32]);
            string invalidData = invalidNonce + ":" + dummyData;
            
            // Act & Assert
            Assert.ThrowsExactly<ArgumentException>(() => 
                EncryptionHelper.DecryptAesGcm(invalidData, key));
        }

        [TestMethod]
        public void AesGcmDecryption_CorruptedData_ShouldThrowException()
        {
            // Arrange
            string key = EncryptionHelper.KeyGenAES256();
            string encrypted = EncryptionHelper.EncryptAesGcm("test", key);
            
            // Corrupt the encrypted data
            string[] parts = encrypted.Split(':');
            string corruptedData = parts[0] + ":" + parts[1].Substring(0, parts[1].Length - 1) + "X";
            
            // Act & Assert
            Assert.ThrowsExactly<CryptographicException>(() => 
                EncryptionHelper.DecryptAesGcm(corruptedData, key));
        }

        [TestMethod]
        public void AesGcmEncryption_NullInputs_ShouldThrowException()
        {
            // Arrange
            string key = EncryptionHelper.KeyGenAES256();
            
            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => 
                EncryptionHelper.EncryptAesGcm(null, key));
            Assert.ThrowsExactly<ArgumentNullException>(() => 
                EncryptionHelper.EncryptAesGcm("test", null));
        }

        [TestMethod]
        public void AesGcmDecryption_NullInputs_ShouldThrowException()
        {
            // Arrange
            string key = EncryptionHelper.KeyGenAES256();
            string encrypted = EncryptionHelper.EncryptAesGcm("test", key);
            
            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => 
                EncryptionHelper.DecryptAesGcm(null, key));
            Assert.ThrowsExactly<ArgumentNullException>(() => 
                EncryptionHelper.DecryptAesGcm(encrypted, null));
        }

        [TestMethod]
        public void KeyGenAES256_GeneratesUniqueKeys_ShouldWork()
        {
            // Act
            string key1 = EncryptionHelper.KeyGenAES256();
            string key2 = EncryptionHelper.KeyGenAES256();
            string key3 = EncryptionHelper.KeyGenAES256();
            
            // Assert
            Assert.AreNotEqual(key1, key2);
            Assert.AreNotEqual(key2, key3);
            Assert.AreNotEqual(key1, key3);
            
            // Validate all keys are valid base64 and correct length
            byte[] key1Bytes = Convert.FromBase64String(key1);
            byte[] key2Bytes = Convert.FromBase64String(key2);
            byte[] key3Bytes = Convert.FromBase64String(key3);
            
            Assert.AreEqual(32, key1Bytes.Length);
            Assert.AreEqual(32, key2Bytes.Length);
            Assert.AreEqual(32, key3Bytes.Length);
        }

        [TestMethod]
        public void BcryptEncoding_CustomWorkFactor_ShouldWork()
        {
            // Arrange
            string password = "TestPassword123";
            int workFactor = 14;
            
            // Act
            string hashedPassword = EncryptionHelper.BcryptEncoding(password, workFactor);
            
            // Assert
            Assert.IsNotNull(hashedPassword);
            Assert.IsTrue(hashedPassword.StartsWith("$2"));
            
            // Verify the password
            bool isValid = EncryptionHelper.VerifyBcryptPassword(password, hashedPassword);
            Assert.IsTrue(isValid);
        }

        [TestMethod]
        public void BcryptEncoding_InvalidWorkFactor_ShouldThrowException()
        {
            // Arrange
            string password = "TestPassword123";
            int tooLow = 3;   // Below minimum of 4
            int tooHigh = 32; // Above maximum of 31
            
            // Act & Assert
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => 
                EncryptionHelper.BcryptEncoding(password, tooLow));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => 
                EncryptionHelper.BcryptEncoding(password, tooHigh));
        }

        [TestMethod]
        public void BcryptEncoding_NullPassword_ShouldThrowException()
        {
            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => 
                EncryptionHelper.BcryptEncoding(null));
        }

        [TestMethod]
        public void VerifyBcryptPassword_NullInputs_ShouldThrowException()
        {
            // Arrange
            string hashedPassword = EncryptionHelper.BcryptEncoding("test");
            
            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => 
                EncryptionHelper.VerifyBcryptPassword(null, hashedPassword));
            Assert.ThrowsExactly<ArgumentNullException>(() => 
                EncryptionHelper.VerifyBcryptPassword("test", null));
        }

        [TestMethod]
        public void GenerateDiffieHellmanKeys_KeyFormatValidation_ShouldWork()
        {
            // Arrange
            string[] keys = EncryptionHelper.GenerateDiffieHellmanKeys();
            
            // Act & Assert
            Assert.AreEqual(2, keys.Length);
            Assert.IsNotNull(keys[0]); // Public key
            Assert.IsNotNull(keys[1]); // Private key
            
            // Validate they are valid base64
            byte[] publicKey = Convert.FromBase64String(keys[0]);
            byte[] privateKey = Convert.FromBase64String(keys[1]);
            
            Assert.IsTrue(publicKey.Length > 0);
            Assert.IsTrue(privateKey.Length > 0);
        }

        [TestMethod]
        public void DeriveSharedKey_InvalidInputs_ShouldThrowException()
        {
            // Arrange
            string[] keys = EncryptionHelper.GenerateDiffieHellmanKeys();
            
            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => 
                EncryptionHelper.DeriveSharedKey(null, keys[1]));
            Assert.ThrowsExactly<ArgumentNullException>(() => 
                EncryptionHelper.DeriveSharedKey(keys[0], null));
        }

        [TestMethod]
        public void GenerateSalt_Uniqueness_ShouldWork()
        {
            // Act
            string salt1 = EncryptionHelper.GenerateSalt();
            string salt2 = EncryptionHelper.GenerateSalt();
            string salt3 = EncryptionHelper.GenerateSalt();
            
            // Assert
            Assert.AreNotEqual(salt1, salt2);
            Assert.AreNotEqual(salt2, salt3);
            Assert.AreNotEqual(salt1, salt3);
            
            // Validate all salts are valid base64 and correct length
            byte[] salt1Bytes = Convert.FromBase64String(salt1);
            byte[] salt2Bytes = Convert.FromBase64String(salt2);
            byte[] salt3Bytes = Convert.FromBase64String(salt3);
            
            Assert.AreEqual(16, salt1Bytes.Length);
            Assert.AreEqual(16, salt2Bytes.Length);
            Assert.AreEqual(16, salt3Bytes.Length);
        }

        [TestMethod]
        public void PasswordBasedEncryption_EmptyString_ShouldWork()
        {
            // Arrange
            string emptyText = "";
            string password = "myPassword";
            
            // Act
            string encrypted = EncryptionHelper.EncryptAesGcmWithPassword(emptyText, password);
            string decrypted = EncryptionHelper.DecryptAesGcmWithPassword(encrypted, password);
            
            // Assert
            Assert.AreEqual(emptyText, decrypted);
        }

        [TestMethod]
        public void PasswordBasedEncryption_EmptyPassword_ShouldWork()
        {
            // Arrange
            string plainText = "Test with empty password";
            string emptyPassword = "";
            
            // Act
            string encrypted = EncryptionHelper.EncryptAesGcmWithPassword(plainText, emptyPassword);
            string decrypted = EncryptionHelper.DecryptAesGcmWithPassword(encrypted, emptyPassword);
            
            // Assert
            Assert.AreEqual(plainText, decrypted);
        }

        [TestMethod]
        public void DerivedKeyEncryption_EmptyString_ShouldWork()
        {
            // Arrange
            string emptyText = "";
            string password = "myPassword";
            string salt = EncryptionHelper.GenerateSalt();
            string derivedKey = EncryptionHelper.DeriveKeyFromPassword(password, salt);
            
            // Act
            string encrypted = EncryptionHelper.EncryptAesGcmWithDerivedKey(emptyText, derivedKey, salt);
            string decrypted = EncryptionHelper.DecryptAesGcmWithDerivedKey(encrypted, derivedKey);
            
            // Assert
            Assert.AreEqual(emptyText, decrypted);
        }

        [TestMethod]
        public void CrossFrameworkCompatibility_KeyDerivation_ShouldWork()
        {
            // Test that key derivation produces consistent results across different calls
            string password = "CrossFrameworkTest123";
            string salt = EncryptionHelper.GenerateSalt();
            int iterations = 2000;
            
            // Derive key multiple times
            string key1 = EncryptionHelper.DeriveKeyFromPassword(password, salt, iterations);
            string key2 = EncryptionHelper.DeriveKeyFromPassword(password, salt, iterations);
            string key3 = EncryptionHelper.DeriveKeyFromPassword(password, salt, iterations);
            
            // All should be identical
            Assert.AreEqual(key1, key2);
            Assert.AreEqual(key2, key3);
            Assert.AreEqual(key1, key3);
            
            // Use the key for encryption/decryption
            string plainText = "Cross-framework compatibility test";
            string encrypted = EncryptionHelper.EncryptAesGcmWithDerivedKey(plainText, key1, salt);
            string decrypted = EncryptionHelper.DecryptAesGcmWithDerivedKey(encrypted, key1);
            
            Assert.AreEqual(plainText, decrypted);
        }
    }
}
#endif