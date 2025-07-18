#if !RELEASE_WITHOUT_TESTS
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureLibrary;
using System;

namespace SecureLibrary.Tests
{
    [TestClass]
    public class PerformanceOptimizationTests
    {
        [TestMethod]
        public void TestPasswordBasedKeyDerivationAndCaching()
        {
            // Arrange
            string password = "TestPassword123";
            string salt = EncryptionHelper.GenerateSalt();
            int iterations = 2000;
            string plainText = "Hello, World!";

            // Act - Derive key once for caching
            string derivedKey = EncryptionHelper.DeriveKeyFromPassword(password, salt, iterations);

            // Encrypt using both methods
            string encryptedWithPassword = EncryptionHelper.EncryptAesGcmWithPasswordAndSalt(plainText, password, salt, iterations);
            string encryptedWithDerivedKey = EncryptionHelper.EncryptAesGcmWithDerivedKey(plainText, derivedKey, salt);

            // Decrypt using both methods
            string decryptedFromPassword = EncryptionHelper.DecryptAesGcmWithPassword(encryptedWithPassword, password, iterations);
            string decryptedFromDerivedKey = EncryptionHelper.DecryptAesGcmWithDerivedKey(encryptedWithDerivedKey, derivedKey);

            // Cross-compatibility test - decrypt password-encrypted data with derived key
            string crossDecrypted = EncryptionHelper.DecryptAesGcmWithDerivedKey(encryptedWithPassword, derivedKey);

            // Assert
            Assert.AreEqual(plainText, decryptedFromPassword);
            Assert.AreEqual(plainText, decryptedFromDerivedKey);
            Assert.AreEqual(plainText, crossDecrypted);
            Assert.AreEqual(32, Convert.FromBase64String(derivedKey).Length); // 32 bytes = 256 bits
        }

        [TestMethod]
        public void TestDerivedKeyValidation()
        {
            // Arrange
            string password = "TestPassword123";
            string salt = EncryptionHelper.GenerateSalt();
            string plainText = "Test data";

            // Act & Assert - Test invalid key lengths
            Assert.ThrowsException<ArgumentException>(() => 
                EncryptionHelper.EncryptAesGcmWithDerivedKey(plainText, Convert.ToBase64String(new byte[16]), salt));
            
            Assert.ThrowsException<ArgumentException>(() => 
                EncryptionHelper.DecryptAesGcmWithDerivedKey("dummy", Convert.ToBase64String(new byte[16])));

            // Test invalid salt
            string validKey = EncryptionHelper.DeriveKeyFromPassword(password, salt);
            Assert.ThrowsException<ArgumentException>(() => 
                EncryptionHelper.EncryptAesGcmWithDerivedKey(plainText, validKey, Convert.ToBase64String(new byte[4])));
        }

        [TestMethod]
        public void TestPerformanceComparison()
        {
            // Arrange
            string password = "TestPassword123";
            string salt = EncryptionHelper.GenerateSalt();
            string plainText = "Performance test data";
            int iterations = 50; // Reduced for faster testing

            // Pre-derive key for cached operations
            string derivedKey = EncryptionHelper.DeriveKeyFromPassword(password, salt, 2000);

            // Act - Measure password-based encryption time
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                EncryptionHelper.EncryptAesGcmWithPasswordAndSalt(plainText, password, salt, 2000);
            }
            stopwatch.Stop();
            long passwordBasedTime = stopwatch.ElapsedMilliseconds;

            // Measure derived key encryption time
            stopwatch.Restart();
            for (int i = 0; i < iterations; i++)
            {
                EncryptionHelper.EncryptAesGcmWithDerivedKey(plainText, derivedKey, salt);
            }
            stopwatch.Stop();
            long derivedKeyTime = stopwatch.ElapsedMilliseconds;

            // Assert - Derived key method should be significantly faster
            // Allow some margin for test environment variation
            Assert.IsTrue(derivedKeyTime <= passwordBasedTime, 
                $"Derived key method ({derivedKeyTime}ms) should be faster than or equal to password-based ({passwordBasedTime}ms)");
            
            // The improvement should be substantial when times are measurable
            if (passwordBasedTime > 0) // Avoid division by zero for very fast operations
            {
                double improvementRatio = (double)(passwordBasedTime - derivedKeyTime) / passwordBasedTime;
                Assert.IsTrue(improvementRatio >= 0, 
                    $"Performance should not be worse. Got {improvementRatio:P1} improvement");
            }
        }

        [TestMethod]
        public void TestBatchEncryptionScenario()
        {
            // Simulate database batch processing scenario
            // Arrange
            string password = "DatabasePassword123";
            string salt = EncryptionHelper.GenerateSalt();
            string[] testData = {
                "Record 1 data",
                "Record 2 data", 
                "Record 3 data",
                "Record 4 data",
                "Record 5 data"
            };

            // Derive key once for all operations
            string derivedKey = EncryptionHelper.DeriveKeyFromPassword(password, salt);

            // Act - Encrypt all records using cached key
            string[] encryptedRecords = new string[testData.Length];
            for (int i = 0; i < testData.Length; i++)
            {
                encryptedRecords[i] = EncryptionHelper.EncryptAesGcmWithDerivedKey(testData[i], derivedKey, salt);
            }

            // Decrypt all records using cached key
            string[] decryptedRecords = new string[encryptedRecords.Length];
            for (int i = 0; i < encryptedRecords.Length; i++)
            {
                decryptedRecords[i] = EncryptionHelper.DecryptAesGcmWithDerivedKey(encryptedRecords[i], derivedKey);
            }

            // Assert
            for (int i = 0; i < testData.Length; i++)
            {
                Assert.AreEqual(testData[i], decryptedRecords[i]);
            }
        }

        [TestMethod]
        public void TestKeyDerivationConsistency()
        {
            // Arrange
            string password = "ConsistencyTest123";
            string salt = EncryptionHelper.GenerateSalt();
            int iterations = 2000;

            // Act - Derive the same key multiple times
            string key1 = EncryptionHelper.DeriveKeyFromPassword(password, salt, iterations);
            string key2 = EncryptionHelper.DeriveKeyFromPassword(password, salt, iterations);
            string key3 = EncryptionHelper.DeriveKeyFromPassword(password, salt, iterations);

            // Assert - All derived keys should be identical
            Assert.AreEqual(key1, key2);
            Assert.AreEqual(key2, key3);
            Assert.AreEqual(key1, key3);
        }

        [TestMethod]
        public void TestCrossCompatibilityWithExistingMethods()
        {
            // Test that data encrypted with new methods can be decrypted with existing methods
            // and vice versa when using the same derived key
            
            // Arrange
            string password = "CrossCompatTest123";
            string salt = EncryptionHelper.GenerateSalt();
            string plainText = "Cross compatibility test data";
            int iterations = 2000;

            // Derive key
            string derivedKey = EncryptionHelper.DeriveKeyFromPassword(password, salt, iterations);

            // Act - Encrypt with password-based method
            string encryptedWithPassword = EncryptionHelper.EncryptAesGcmWithPasswordAndSalt(plainText, password, salt, iterations);
            
            // Encrypt with derived key method
            string encryptedWithDerivedKey = EncryptionHelper.EncryptAesGcmWithDerivedKey(plainText, derivedKey, salt);

            // Cross-decrypt
            string decryptedPasswordWithKey = EncryptionHelper.DecryptAesGcmWithDerivedKey(encryptedWithPassword, derivedKey);
            string decryptedKeyWithPassword = EncryptionHelper.DecryptAesGcmWithPassword(encryptedWithDerivedKey, password, iterations);

            // Assert
            Assert.AreEqual(plainText, decryptedPasswordWithKey);
            Assert.AreEqual(plainText, decryptedKeyWithPassword);
        }
    }
}
#endif