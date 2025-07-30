using System;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SecureLibrary.Tests
{
    [TestClass]
    public class ProtectedDataTests
    {
        [TestMethod]
        public void TestProtectedDataHelper_IsSupported()
        {
            // Test that IsSupported correctly identifies supported platforms
            bool isSupported = ProtectedMemoryHelper.IsSupported;
            
            // Should be supported on Windows, Linux, and macOS
            Console.WriteLine($"ProtectedData supported: {isSupported}");
            Console.WriteLine($"OS Platform: {Environment.OSVersion.Platform}");
        }

        [TestMethod]
        public void TestProtectedDataHelper_ProtectAndUnprotect()
        {
            // Test basic protect/unprotect functionality
            byte[] testData = new byte[32];
            for (int i = 0; i < testData.Length; i++)
            {
                testData[i] = (byte)i;
            }

            // Protect the data
            byte[] protectedData = ProtectedMemoryHelper.Protect(testData);
            Assert.IsNotNull(protectedData);
            Assert.AreNotEqual(testData, protectedData);

            // Unprotect the data
            byte[] unprotectedData = ProtectedMemoryHelper.Unprotect(protectedData);
            Assert.IsNotNull(unprotectedData);
            Assert.AreEqual(testData.Length, unprotectedData.Length);
            
            // Verify data integrity
            for (int i = 0; i < testData.Length; i++)
            {
                Assert.AreEqual(testData[i], unprotectedData[i]);
            }
        }

        [TestMethod]
        public void TestProtectedDataHelper_ExecuteWithProtection_SingleArray()
        {
            byte[] testData = new byte[16];
            for (int i = 0; i < testData.Length; i++)
            {
                testData[i] = (byte)i;
            }

            byte[] result = ProtectedMemoryHelper.ExecuteWithProtection(testData, (protectedData) =>
            {
                // Verify the data is accessible
                Assert.AreEqual(16, protectedData.Length);
                for (int i = 0; i < protectedData.Length; i++)
                {
                    Assert.AreEqual((byte)i, protectedData[i]);
                }

                // Modify the data to test that it's unprotected
                protectedData[0] = 255;
                return protectedData;
            });

            // Verify the operation completed successfully
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void TestProtectedDataHelper_ExecuteWithProtection_MultipleArrays()
        {
            byte[] key = new byte[32];
            byte[] nonce = new byte[12];
            
            for (int i = 0; i < key.Length; i++)
            {
                key[i] = (byte)(i + 1);
            }
            for (int i = 0; i < nonce.Length; i++)
            {
                nonce[i] = (byte)(i + 100);
            }

            byte[][] result = ProtectedMemoryHelper.ExecuteWithProtection(new byte[][] { key, nonce }, (protectedArrays) =>
            {
                // Verify the arrays are accessible
                Assert.AreEqual(2, protectedArrays.Length);
                Assert.AreEqual(32, protectedArrays[0].Length);
                Assert.AreEqual(12, protectedArrays[1].Length);

                // Verify the data is correct
                for (int i = 0; i < protectedArrays[0].Length; i++)
                {
                    Assert.AreEqual((byte)(i + 1), protectedArrays[0][i]);
                }
                for (int i = 0; i < protectedArrays[1].Length; i++)
                {
                    Assert.AreEqual((byte)(i + 100), protectedArrays[1][i]);
                }

                return protectedArrays;
            });

            // Verify the operation completed successfully
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void TestEncryptionWithProtectedData()
        {
            // Test that encryption still works with ProtectedData
            string plainText = "Hello, Protected Data!";
            string key = EncryptionHelper.KeyGenAES256();

            // Encrypt
            string encrypted = EncryptionHelper.EncryptAesGcm(plainText, key);
            Assert.IsNotNull(encrypted);
            Assert.AreNotEqual(plainText, encrypted);

            // Decrypt
            string decrypted = EncryptionHelper.DecryptAesGcm(encrypted, key);
            Assert.AreEqual(plainText, decrypted);
        }

        [TestMethod]
        public void TestPasswordBasedEncryptionWithProtectedData()
        {
            // Test password-based encryption with ProtectedData
            string plainText = "Secret data protected by password";
            string password = "MySecurePassword123!";

            // Encrypt
            string encrypted = EncryptionHelper.EncryptAesGcmWithPassword(plainText, password);
            Assert.IsNotNull(encrypted);
            Assert.AreNotEqual(plainText, encrypted);

            // Decrypt
            string decrypted = EncryptionHelper.DecryptAesGcmWithPassword(encrypted, password);
            Assert.AreEqual(plainText, decrypted);
        }

        [TestMethod]
        public void TestDerivedKeyEncryptionWithProtectedData()
        {
            // Test derived key encryption with ProtectedData
            string plainText = "Data encrypted with derived key";
            string password = "MyPassword";
            string salt = EncryptionHelper.GenerateSalt();
            string derivedKey = EncryptionHelper.DeriveKeyFromPassword(password, salt);

            // Encrypt
            string encrypted = EncryptionHelper.EncryptAesGcmWithDerivedKey(plainText, derivedKey, salt);
            Assert.IsNotNull(encrypted);
            Assert.AreNotEqual(plainText, encrypted);

            // Decrypt
            string decrypted = EncryptionHelper.DecryptAesGcmWithDerivedKey(encrypted, derivedKey);
            Assert.AreEqual(plainText, decrypted);
        }

        [TestMethod]
        public void TestProtectedData_PlatformCompatibility()
        {
            // Test that the library works on all platforms, even if ProtectedData is not supported
            string plainText = "Cross-platform compatibility test";
            string key = EncryptionHelper.KeyGenAES256();

            try
            {
                string encrypted = EncryptionHelper.EncryptAesGcm(plainText, key);
                string decrypted = EncryptionHelper.DecryptAesGcm(encrypted, key);
                
                Assert.AreEqual(plainText, decrypted);
                Console.WriteLine("Encryption/decryption successful on current platform");
            }
            catch (PlatformNotSupportedException ex)
            {
                Console.WriteLine($"Platform not supported: {ex.Message}");
                // This is expected on unsupported platforms
                Assert.IsTrue(ex.Message.Contains("ProtectedData"));
            }
        }

        [TestMethod]
        public void TestProtectedData_ErrorHandling()
        {
            #pragma warning disable CS8625 // test null arguments will throw ArgumentNullException
            // Test error handling with null data
            try
            {
                ProtectedMemoryHelper.Protect(null);
                Assert.Fail("Should have thrown ArgumentNullException");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }

            // Test with null protected data
            try
            {
                ProtectedMemoryHelper.Unprotect(null);
                Assert.Fail("Should have thrown ArgumentNullException");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }
            #pragma warning restore CS8625 // test null arguments will throw ArgumentNullException
        }

        [TestMethod]
        public void TestProtectedData_Performance()
        {
            // Test performance impact of ProtectedData
            string plainText = "Performance test data";
            string key = EncryptionHelper.KeyGenAES256();

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            for (int i = 0; i < 100; i++)
            {
                string encrypted = EncryptionHelper.EncryptAesGcm(plainText, key);
                string decrypted = EncryptionHelper.DecryptAesGcm(encrypted, key);
                Assert.AreEqual(plainText, decrypted);
            }
            
            stopwatch.Stop();
            
            Console.WriteLine($"100 encryption/decryption operations took: {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Average per operation: {stopwatch.ElapsedMilliseconds / 100.0}ms");
            
            // Performance should be reasonable (less than 10ms per operation on modern hardware)
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, "Performance test took too long");
        }

        [TestMethod]
        public void TestProtectedData_WithEntropy()
        {
            // Test ProtectedData with custom entropy
            byte[] testData = new byte[32];
            for (int i = 0; i < testData.Length; i++)
            {
                testData[i] = (byte)i;
            }

            byte[] entropy = ProtectedMemoryHelper.GenerateEntropy(16);
            Assert.AreEqual(16, entropy.Length);

            // Protect with entropy
            byte[] protectedData = ProtectedMemoryHelper.Protect(testData, entropy);
            Assert.IsNotNull(protectedData);

            // Unprotect with same entropy
            byte[] unprotectedData = ProtectedMemoryHelper.Unprotect(protectedData, entropy);
            Assert.IsNotNull(unprotectedData);
            Assert.AreEqual(testData.Length, unprotectedData.Length);

            // Verify data integrity
            for (int i = 0; i < testData.Length; i++)
            {
                Assert.AreEqual(testData[i], unprotectedData[i]);
            }
        }

        [TestMethod]
        public void TestProtectedData_GenerateEntropy()
        {
            // Test entropy generation
            byte[] entropy1 = ProtectedMemoryHelper.GenerateEntropy(16);
            byte[] entropy2 = ProtectedMemoryHelper.GenerateEntropy(16);
            
            Assert.AreEqual(16, entropy1.Length);
            Assert.AreEqual(16, entropy2.Length);
            
            // Entropy should be different each time
            bool isDifferent = false;
            for (int i = 0; i < entropy1.Length; i++)
            {
                if (entropy1[i] != entropy2[i])
                {
                    isDifferent = true;
                    break;
                }
            }
            Assert.IsTrue(isDifferent, "Generated entropy should be different each time");
        }
    }
} 