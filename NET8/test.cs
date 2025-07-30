#if !RELEASE_WITHOUT_TESTS
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureLibrary;
using System;
using System.Security.Cryptography;


#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
// this warning disable is for the test methods that are not nullable related issues like null reference exception
// so we need to disable the warning
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
            Assert.ThrowsExactly<AuthenticationTagMismatchException>(() => 
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
            Assert.ThrowsExactly<ArgumentException>(() => EncryptionHelper.GenerateSalt(tooSmall));
            Assert.ThrowsExactly<ArgumentException>(() => EncryptionHelper.GenerateSalt(tooLarge));
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
        public void TestPasswordBasedAesGcmInvalidIterations()
        {
            // Arrange
            string plainText = "Test invalid iterations";
            string password = "MyPassword";
            int tooLow = 500;   // Below minimum of 1000
            int tooHigh = 200000; // Above maximum of 100000
            
            // Act & Assert
            Assert.ThrowsExactly<ArgumentException>(() => 
                EncryptionHelper.EncryptAesGcmWithPassword(plainText, password, tooLow));
            Assert.ThrowsExactly<ArgumentException>(() => 
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

        // Derived Key Tests
        [TestMethod]
        public void TestDeriveKeyFromPassword()
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
        public void TestDeriveKeyFromPassword_CustomIterations()
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
        public void TestDeriveKeyFromPassword_Consistency()
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
        public void TestDeriveKeyFromPassword_InvalidInputs()
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
        public void TestEncryptAesGcmWithDerivedKey()
        {
            // Arrange
            string plainText = "Test with derived key";
            string password = "MyPassword";
            string salt = EncryptionHelper.GenerateSalt();
            string derivedKey = EncryptionHelper.DeriveKeyFromPassword(password, salt);
            
            // Act
            string encrypted = EncryptionHelper.EncryptAesGcmWithDerivedKey(plainText, derivedKey, salt);
            
            // Assert
            Assert.IsNotNull(encrypted);
            Assert.AreNotEqual(plainText, encrypted);
        }

        [TestMethod]
        public void TestDecryptAesGcmWithDerivedKey()
        {
            // Arrange
            string plainText = "Test decryption with derived key";
            string password = "MyPassword";
            string salt = EncryptionHelper.GenerateSalt();
            string derivedKey = EncryptionHelper.DeriveKeyFromPassword(password, salt);
            string encrypted = EncryptionHelper.EncryptAesGcmWithDerivedKey(plainText, derivedKey, salt);
            
            // Act
            string decrypted = EncryptionHelper.DecryptAesGcmWithDerivedKey(encrypted, derivedKey);
            
            // Assert
            Assert.AreEqual(plainText, decrypted);
        }

        [TestMethod]
        public void TestDerivedKeyEncryptionDecryption_CrossCompatibility()
        {
            // Test that derived key encryption/decryption works with password-based methods
            string plainText = "Cross compatibility test";
            string password = "MyPassword";
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
        public void TestDerivedKeyEncryptionDecryption_ReverseCrossCompatibility()
        {
            // Test that password-based encryption works with derived key decryption
            string plainText = "Reverse cross compatibility test";
            string password = "MyPassword";
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
        public void TestDerivedKeyEncryption_InvalidInputs()
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
        public void TestDerivedKeyDecryption_InvalidInputs()
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
        public void TestDerivedKeyEncryption_LargeData()
        {
            // Arrange
            string largeText = new string('A', 10000); // 10KB of data
            string password = "MyPassword";
            string salt = EncryptionHelper.GenerateSalt();
            string derivedKey = EncryptionHelper.DeriveKeyFromPassword(password, salt);
            
            // Act
            string encrypted = EncryptionHelper.EncryptAesGcmWithDerivedKey(largeText, derivedKey, salt);
            string decrypted = EncryptionHelper.DecryptAesGcmWithDerivedKey(encrypted, derivedKey);
            
            // Assert
            Assert.AreEqual(largeText, decrypted);
        }

        [TestMethod]
        public void TestDerivedKeyEncryption_SpecialCharacters()
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
        public void TestDerivedKeyEncryption_UnicodeCharacters()
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
        public void TestDerivedKeyEncryption_DifferentSaltsShouldFail()
        {
            // Arrange
            string plainText = "Test with different salts";
            string password = "MyPassword";
            string salt1 = EncryptionHelper.GenerateSalt();
            string salt2 = EncryptionHelper.GenerateSalt();
            string derivedKey1 = EncryptionHelper.DeriveKeyFromPassword(password, salt1);
            string derivedKey2 = EncryptionHelper.DeriveKeyFromPassword(password, salt2);
            
            // Act
            string encrypted = EncryptionHelper.EncryptAesGcmWithDerivedKey(plainText, derivedKey1, salt1);
            
            // Assert - Should throw exception when trying to decrypt with key derived from different salt
            Assert.ThrowsExactly<AuthenticationTagMismatchException>(() => 
                EncryptionHelper.DecryptAesGcmWithDerivedKey(encrypted, derivedKey2));
        }

        [TestMethod]
        public void TestDerivedKeyEncryption_WrongKeyShouldFail()
        {
            // Arrange
            string plainText = "Test with wrong key";
            string password1 = "MyPassword1";
            string password2 = "MyPassword2";
            string salt = EncryptionHelper.GenerateSalt();
            string derivedKey1 = EncryptionHelper.DeriveKeyFromPassword(password1, salt);
            string derivedKey2 = EncryptionHelper.DeriveKeyFromPassword(password2, salt);
            
            // Act
            string encrypted = EncryptionHelper.EncryptAesGcmWithDerivedKey(plainText, derivedKey1, salt);
            
            // Assert - Should throw exception when trying to decrypt with wrong key
            Assert.ThrowsExactly<AuthenticationTagMismatchException>(() => 
                EncryptionHelper.DecryptAesGcmWithDerivedKey(encrypted, derivedKey2));
        }

        // Additional Error Handling and Edge Case Tests
        [TestMethod]
        public void TestAesGcmEncryption_InvalidKeyLength()
        {
            // Arrange
            string plainText = "Test invalid key length";
            string invalidKey = Convert.ToBase64String(new byte[16]); // 128-bit key instead of 256-bit
            
            // Act & Assert
            Assert.ThrowsExactly<ArgumentException>(() => 
                EncryptionHelper.EncryptAesGcm(plainText, invalidKey));
        }

        [TestMethod]
        public void TestAesGcmDecryption_InvalidKeyLength()
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
        public void TestAesGcmDecryption_InvalidDataFormat()
        {
            // Arrange
            string key = EncryptionHelper.KeyGenAES256();
            string invalidData = "invalid:data:format";
            
            // Act & Assert
            Assert.ThrowsExactly<ArgumentException>(() => 
                EncryptionHelper.DecryptAesGcm(invalidData, key));
        }

        [TestMethod]
        public void TestAesGcmDecryption_InvalidNonceLength()
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
        public void TestAesGcmDecryption_CorruptedData()
        {
            // Arrange
            string key = EncryptionHelper.KeyGenAES256();
            string encrypted = EncryptionHelper.EncryptAesGcm("test", key);
            
            // Corrupt the encrypted data
            string[] parts = encrypted.Split(':');
            string corruptedData = parts[0] + ":" + parts[1].Substring(0, parts[1].Length - 1) + "X";
            
            // Act & Assert
            Assert.ThrowsExactly<AuthenticationTagMismatchException>(() => 
                EncryptionHelper.DecryptAesGcm(corruptedData, key));
        }

        [TestMethod]
        public void TestAesGcmEncryption_NullInputs()
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
        public void TestAesGcmDecryption_NullInputs()
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
        public void TestKeyGenAES256_GeneratesUniqueKeys()
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
        public void TestBcryptEncoding_CustomWorkFactor()
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
        public void TestBcryptEncoding_InvalidWorkFactor()
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
        public void TestBcryptEncoding_NullPassword()
        {
            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => 
                EncryptionHelper.BcryptEncoding(null));
        }

        [TestMethod]
        public void TestVerifyBcryptPassword_NullInputs()
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
        public void TestDiffieHellmanKeyExchange_KeyFormatValidation()
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
        public void TestDiffieHellmanKeyExchange_InvalidInputs()
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
        public void TestGenerateSalt_Uniqueness()
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
        public void TestPasswordBasedEncryption_EmptyString()
        {
            // Arrange
            string emptyText = "";
            string password = "MyPassword";
            
            // Act
            string encrypted = EncryptionHelper.EncryptAesGcmWithPassword(emptyText, password);
            string decrypted = EncryptionHelper.DecryptAesGcmWithPassword(encrypted, password);
            
            // Assert
            Assert.AreEqual(emptyText, decrypted);
        }

        [TestMethod]
        public void TestPasswordBasedEncryption_EmptyPassword()
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
        public void TestDerivedKeyEncryption_EmptyString()
        {
            // Arrange
            string emptyText = "";
            string password = "MyPassword";
            string salt = EncryptionHelper.GenerateSalt();
            string derivedKey = EncryptionHelper.DeriveKeyFromPassword(password, salt);
            
            // Act
            string encrypted = EncryptionHelper.EncryptAesGcmWithDerivedKey(emptyText, derivedKey, salt);
            string decrypted = EncryptionHelper.DecryptAesGcmWithDerivedKey(encrypted, derivedKey);
            
            // Assert
            Assert.AreEqual(emptyText, decrypted);
        }

        [TestMethod]
        public void TestCrossFrameworkCompatibility_KeyDerivation()
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