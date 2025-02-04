#if !RELEASE_WITHOUT_TESTS
using NUnit.Framework;
using SecureLibrary;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;

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

    [TestFixture]
    public class SecurityTests
    {
        [Test]
        public void RateLimiting_ShouldBlockAfterMaxAttempts()
        {
            // Arrange
            const string testPassword = "TestPassword123";
            const string wrongPassword = "WrongPassword123";
            string hashedPassword = EncryptionHelper.BcryptEncoding(testPassword);

            // Act & Assert
            // First 5 attempts should not throw
            for (int i = 0; i < 5; i++)
            {
                bool result = EncryptionHelper.VerifyBcryptPassword(wrongPassword, hashedPassword);
                Assert.That(result, Is.False);
            }

            // The 6th attempt should throw
            var ex = Assert.Throws<CryptographicOperationException>(() => 
                EncryptionHelper.VerifyBcryptPassword(wrongPassword, hashedPassword));
            Assert.That(ex.Message, Does.Contain("Too many failed verification attempts"));
        }

        [Test]
        public void KeyValidation_ShouldRejectInvalidKeys()
        {
            // Arrange
            string invalidKey = "InvalidBase64Key";
            string shortKey = Convert.ToBase64String(new byte[16]); // Too short
            string validKey = Convert.ToBase64String(new byte[32]); // Correct length

            // Act & Assert
            Assert.Throws<CryptographicOperationException>(() => 
                EncryptionHelper.EncryptAesGcm("test", invalidKey));
            
            Assert.Throws<CryptographicOperationException>(() => 
                EncryptionHelper.EncryptAesGcm("test", shortKey));
            
            Assert.DoesNotThrow(() => 
                EncryptionHelper.EncryptAesGcm("test", validKey));
        }

        [Test]
        public void SecureErase_ShouldClearSensitiveData()
        {
            // Arrange
            byte[] sensitiveData = new byte[] { 1, 2, 3, 4, 5 };
            
            // Act
            KeyManagement.SecureErase(sensitiveData);
            
            // Assert
            Assert.That(sensitiveData, Is.All.EqualTo(0));
        }

        [Test]
        public void BcryptWorkFactor_ShouldUseStrongerWorkFactor()
        {
            // Arrange
            string password = "TestPassword123";
            
            // Act
            string hashedPassword = EncryptionHelper.BcryptEncoding(password);
            
            // Assert
            // BCrypt hash format: $2a$[work factor]$[salt+hash]
            string[] parts = hashedPassword.Split('$');
            Assert.That(parts[2], Is.EqualTo("12")); // Check work factor is 12
        }
    }

    [TestFixture]
    public class KeyRotationTests
    {
        private static readonly Dictionary<string, object> _testKeyStore = new();

        [SetUp]
        public void Setup()
        {
            // Clear the key store before each test
            var keyStoreField = typeof(KeyRotationManager)
                .GetField("_keyStore", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            _testKeyStore.Clear();
            keyStoreField?.SetValue(null, _testKeyStore);
        }

        [TearDown]
        public void Cleanup()
        {
            _testKeyStore.Clear();
        }

        [Test]
        public void KeyRotation_ShouldGenerateNewKeyAfterExpiration()
        {
            // Arrange
            string keyId = "test-key-1";
            string initialKey = KeyRotationManager.GetCurrentKey(keyId);
            
            // Act - simulate time passing
            var keyInfo = _testKeyStore[keyId];
            var keyInfoType = keyInfo.GetType();
            keyInfoType.GetProperty("ExpirationDate")?.SetValue(keyInfo, DateTime.UtcNow.AddDays(-1));
            
            string newKey = KeyRotationManager.GetCurrentKey(keyId);
            
            // Assert
            Assert.That(newKey, Is.Not.Null);
            Assert.That(newKey, Is.Not.EqualTo(initialKey));
        }

        [Test]
        public void KeyRegistration_ShouldValidateKeyFormat()
        {
            // Arrange
            string keyId = "test-key-2";
            string invalidKey = "invalid-key";
            
            // Act & Assert
            Assert.Throws<CryptographicOperationException>(() => 
                KeyRotationManager.RegisterKey(keyId, invalidKey));
        }

        [Test]
        public void KeyDeactivation_ShouldPreventKeyUsage()
        {
            // Arrange
            string keyId = "test-key-3";
            KeyRotationManager.GetCurrentKey(keyId); // Ensure key exists
            
            // Act
            KeyRotationManager.DeactivateKey(keyId);
            
            // Assert
            Assert.That(KeyRotationManager.IsKeyActive(keyId), Is.False);
        }

        [Test]
        public void CleanupExpiredKeys_ShouldRemoveOldKeys()
        {
            // Arrange
            string keyId = "test-key-4";
            KeyRotationManager.GetCurrentKey(keyId);
            
            var keyInfo = _testKeyStore[keyId];
            var keyInfoType = keyInfo.GetType();
            keyInfoType.GetProperty("ExpirationDate")?.SetValue(keyInfo, DateTime.UtcNow.AddDays(-1));
            
            // Act
            KeyRotationManager.CleanupExpiredKeys();
            
            // Assert
            Assert.That(_testKeyStore.ContainsKey(keyId), Is.False);
        }
    }

    [TestFixture]
    public class ConcurrencyTests
    {
        private static readonly Dictionary<string, object> _testKeyStore = new();

        [SetUp]
        public void Setup()
        {
            var keyStoreField = typeof(KeyRotationManager)
                .GetField("_keyStore", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            _testKeyStore.Clear();
            keyStoreField?.SetValue(null, _testKeyStore);
        }

        [TearDown]
        public void Cleanup()
        {
            _testKeyStore.Clear();
        }

        [Test]
        public void ConcurrentKeyAccess_ShouldBeThreadSafe()
        {
            // Arrange
            const int threadCount = 10;
            const int operationsPerThread = 100;
            string keyId = "concurrent-test-key";
            var exceptions = new ConcurrentQueue<Exception>();
            
            // Act
            var tasks = Enumerable.Range(0, threadCount).Select(_ => Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < operationsPerThread; i++)
                    {
                        var key = KeyRotationManager.GetCurrentKey(keyId);
                        Assert.That(key, Is.Not.Null);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Enqueue(ex);
                }
            }));
            
            Task.WaitAll(tasks.ToArray());
            
            // Assert
            Assert.That(exceptions, Is.Empty);
        }

        [Test]
        public void ConcurrentEncryption_ShouldBeThreadSafe()
        {
            // Arrange
            const int threadCount = 10;
            const string plainText = "Test message";
            string key = EncryptionHelper.KeyGenAES256();
            var exceptions = new ConcurrentQueue<Exception>();
            var results = new ConcurrentBag<string>();
            
            // Act
            var tasks = Enumerable.Range(0, threadCount).Select(_ => Task.Run(() =>
            {
                try
                {
                    string encrypted = EncryptionHelper.EncryptAesGcm(plainText, key);
                    string decrypted = EncryptionHelper.DecryptAesGcm(encrypted, key);
                    results.Add(decrypted);
                }
                catch (Exception ex)
                {
                    exceptions.Enqueue(ex);
                }
            }));
            
            Task.WaitAll(tasks.ToArray());
            
            // Assert
            Assert.That(exceptions, Is.Empty);
            Assert.That(results, Is.All.EqualTo(plainText));
        }
    }
}
#endif