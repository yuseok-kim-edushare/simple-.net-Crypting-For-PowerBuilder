#if !RELEASE_WITHOUT_TESTS
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureLibrary.SQL;
using System.Linq;
using System.Data.SqlTypes;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;

namespace SecureLibrary.Tests
{
    [TestClass]
    public class CrossFrameworkTests
    {
        [TestMethod]
        public void TestCrossCommunicationWithNet481()
        {
            // Step 1: Generate key pairs for both frameworks
            // NET 8 generates its key pair using ECDiffieHellmanCng for compatibility
            using (var dh = new ECDiffieHellmanCng())
            {
                dh.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
                dh.HashAlgorithm = CngAlgorithm.Sha256;
                
                // Export keys in EccPublicBlob format for compatibility with .NET 4.8.1
                string net8PublicKey = Convert.ToBase64String(dh.PublicKey.ToByteArray());
                string net8PrivateKey = Convert.ToBase64String(dh.Key.Export(CngKeyBlobFormat.EccPrivateBlob));

                // NET 4.8.1 generates its key pair
                var net481Keys = SqlCLRCrypting.GenerateDiffieHellmanKeys().Cast<SqlString[]>().First();
                Assert.IsFalse(net481Keys[0].IsNull && net481Keys[1].IsNull);

                // Step 2: Simulate key exchange and derive shared secrets
                // NET 8 uses NET 4.8.1's public key with its own private key
                string net8SharedSecret;
                using (var dhForDerivation = new ECDiffieHellmanCng(CngKey.Import(Convert.FromBase64String(net8PrivateKey), CngKeyBlobFormat.EccPrivateBlob)))
                {
                    dhForDerivation.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
                    dhForDerivation.HashAlgorithm = CngAlgorithm.Sha256;

                    // Import .NET 4.8.1's public key in EccPublicBlob format
                    using (var importedKey = CngKey.Import(Convert.FromBase64String(net481Keys[0].Value), CngKeyBlobFormat.EccPublicBlob))
                    {
                        net8SharedSecret = Convert.ToBase64String(dhForDerivation.DeriveKeyMaterial(importedKey));
                    }
                }

                // NET 4.8.1 uses NET 8's public key with its own private key
                SqlString net481SharedSecret = SqlCLRCrypting.DeriveSharedKey(
                    new SqlString(net8PublicKey),  // NET 8's public key
                    net481Keys[1]                  // NET 4.8.1's private key
                );

                // Verify both sides derived the same shared secret
                Assert.AreEqual(net8SharedSecret, net481SharedSecret.Value);

                // Step 3: Test cross-framework encryption/decryption
                string testMessage = "Hello from cross-framework test!";
                string aesKey = EncryptionHelper.KeyGenAES256();

                // NET 8 encrypts, NET 4.8.1 decrypts
                string net8Encrypted = EncryptionHelper.EncryptAesGcm(testMessage, aesKey);
                SqlString net481Decrypted = SqlCLRCrypting.DecryptAesGcm(
                    new SqlString(net8Encrypted),
                    new SqlString(aesKey)
                );
                Assert.AreEqual(testMessage, net481Decrypted.Value);

                // NET 4.8.1 encrypts, NET 8 decrypts
                SqlString net481Encrypted = SqlCLRCrypting.EncryptAesGcm(
                    new SqlString(testMessage),
                    new SqlString(aesKey)
                );
                string net8Decrypted = EncryptionHelper.DecryptAesGcm(net481Encrypted.Value, aesKey);
                Assert.AreEqual(testMessage, net8Decrypted);
            }
        }

        // Extra tests for SQL CLR, this created to test the SQL CLR code in the SecureLibrary-SQL project
        // These tests are not part of the main test suite, but are included for testing the SQL CLR code
        // CLR requires special security permissions, so these tests are included here separately
        // Thus, these tests are included here to test the SQL CLR code in the SecureLibrary-SQL project
        [TestMethod]
        [TestCategory("SQLCLR")]
        public void TestSqlCLR_GenerateAESKey()
        {
            // Act
            var generatedKey = SqlCLRCrypting.GenerateAESKey();
            
            // Assert
            Assert.IsNotNull(generatedKey, "Generated key should not be null");
            Assert.AreEqual(44, generatedKey.Value.Length, "Key length should be 44 characters");
        }
        
        [TestMethod]
        [TestCategory("SQLCLR")]
        public void TestSqlCLR_EncryptAES()
        {
            // Arrange
            var plainText = new SqlString("This is a test string");
            var key = SqlCLRCrypting.GenerateAESKey();
            
            // Act
            var encryptResult = SqlCLRCrypting.EncryptAES(plainText, key).Cast<SqlString[]>().First();
            var decrypted = SqlCLRCrypting.DecryptAES(encryptResult[0], key, encryptResult[1]);
            
            // Assert
            Assert.AreEqual(plainText.Value, decrypted.Value, "Decrypted text should match original");
        }
        
        [TestMethod]
        [TestCategory("SQLCLR")]
        public void TestSqlCLR_HashPasswordDefault()
        {
            // Arrange
            var password = new SqlString("securePassword123");
            
            // Act
            var hashedPassword = SqlCLRCrypting.HashPasswordDefault(password);
            
            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(hashedPassword.Value), "Hashed password should not be null");
            Assert.IsTrue(hashedPassword.Value.StartsWith("$2"), "Hash should be in BCrypt format");
        }
        
        [TestMethod]
        [TestCategory("SQLCLR")]
        public void TestSqlCLR_HashPasswordWithWorkFactor()
        {
            // Arrange
            var password = new SqlString("securePassword123");
            var workFactor = new SqlInt32(14);
            
            // Act
            var hashedPassword = SqlCLRCrypting.HashPasswordWithWorkFactor(password, workFactor);
            
            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(hashedPassword.Value), "Hashed password should not be null");
            Assert.IsTrue(hashedPassword.Value.StartsWith("$2"), "Hash should be in BCrypt format");
        }
        
        [TestMethod]
        [TestCategory("SQLCLR")]
        public void TestSqlCLR_VerifyPassword()
        {
            // Arrange
            var password = new SqlString("securePassword123");
            var hashedPassword = SqlCLRCrypting.HashPasswordDefault(password);
            
            // Act
            var isValid = SqlCLRCrypting.VerifyPassword(password, hashedPassword);
            var isInvalid = SqlCLRCrypting.VerifyPassword(new SqlString("WrongPassword"), hashedPassword);
            
            // Assert
            Assert.IsTrue(isValid.IsTrue, "Password verification should succeed");
            Assert.IsFalse(isInvalid.IsTrue, "Wrong password should fail verification");
        }

        [TestMethod]
        [TestCategory("SQLCLR")]
        public void TestSqlCLR_GenerateDiffieHellmanKeys()
        {
            // Act
            var keysResult = SqlCLRCrypting.GenerateDiffieHellmanKeys().Cast<SqlString[]>().First();

            // Assert
            Assert.IsFalse(keysResult[0].IsNull, "Public key should not be null");
            Assert.IsFalse(keysResult[1].IsNull, "Private key should not be null");
        }

        [TestMethod]
        [TestCategory("SQLCLR")]
        public void TestSqlCLR_DeriveSharedKey()
        {
            // Arrange
            var keys1 = SqlCLRCrypting.GenerateDiffieHellmanKeys().Cast<SqlString[]>().First();
            var keys2 = SqlCLRCrypting.GenerateDiffieHellmanKeys().Cast<SqlString[]>().First();

            // Act
            var sharedKey1 = SqlCLRCrypting.DeriveSharedKey(keys2[0], keys1[1]);
            var sharedKey2 = SqlCLRCrypting.DeriveSharedKey(keys1[0], keys2[1]);

            // Assert
            Assert.IsFalse(sharedKey1.IsNull, "Shared key 1 should not be null");
            Assert.IsFalse(sharedKey2.IsNull, "Shared key 2 should not be null");
            Assert.AreEqual(sharedKey1.Value, sharedKey2.Value, "Shared keys should match");
        }

        [TestMethod]
        [TestCategory("SQLCLR")]
        public void TestSqlCLR_EncryptAesGcm()
        {
            // Arrange
            var plainText = new SqlString("This is a test string");
            var key = SqlCLRCrypting.GenerateAESKey();
            
            Assert.IsFalse(plainText.IsNull, "Input plainText should not be null");
            Assert.IsFalse(key.IsNull, "Input key should not be null");
            
            // Validate key length
            byte[] keyBytes = Convert.FromBase64String(key.Value);
            Assert.AreEqual(32, keyBytes.Length, "Key must be 32 bytes");
            
            // Act - Encryption
            var encrypted = SqlCLRCrypting.EncryptAesGcm(plainText, key);
            Assert.IsFalse(encrypted.IsNull, "Encrypted value should not be null");
            
            // Validate encrypted format
            string[] parts = encrypted.Value.Split(':');
            Assert.AreEqual(2, parts.Length, "Encrypted value should contain nonce and ciphertext");
            Assert.AreEqual(12, Convert.FromBase64String(parts[0]).Length, "Nonce should be 12 bytes");
            
            // Act - Decryption
            var decrypted = SqlCLRCrypting.DecryptAesGcm(encrypted, key);
            Assert.IsFalse(decrypted.IsNull, "Decrypted value should not be null");
            
            // Assert
            Assert.AreEqual(plainText.Value, decrypted.Value, "Decrypted text should match original");
        }

        // New tests for password-based AES-GCM encryption
        [TestMethod]
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        [TestCategory("SQLCLR")]
        public void TestSqlCLR_EncryptAesGcmWithPassword()
        {
            // Arrange
            var plainText = new SqlString("This is a test string for password-based encryption");
            var password = new SqlString("mySecurePassword123");
            
            Assert.IsFalse(plainText.IsNull, "Input plainText should not be null");
            Assert.IsFalse(password.IsNull, "Input password should not be null");
            
            // Act - Encryption
            var encrypted = SqlCLRCrypting.EncryptAesGcmWithPassword(plainText, password);
            Assert.IsFalse(encrypted.IsNull, "Encrypted value should not be null");
            
            // Validate encrypted format (should contain salt + nonce + encrypted data)
            byte[] encryptedBytes = Convert.FromBase64String(encrypted.Value);
            Assert.IsTrue(encryptedBytes.Length > 44, "Encrypted data should be longer than salt(16) + nonce(12) + tag(16)");
            
            // Act - Decryption
            var decrypted = SqlCLRCrypting.DecryptAesGcmWithPassword(encrypted, password);
            Assert.IsFalse(decrypted.IsNull, "Decrypted value should not be null");
            
            // Assert
            Assert.AreEqual(plainText.Value, decrypted.Value, "Decrypted text should match original");
        }

        [TestMethod]
        [TestCategory("SQLCLR")]
        public void TestSqlCLR_EncryptAesGcmWithPasswordIterations()
        {
            // Arrange
            var plainText = new SqlString("Test with custom iterations");
            var password = new SqlString("myPassword");
            var iterations = new SqlInt32(5000);
            
            // Act - Encryption with custom iterations
            var encrypted = SqlCLRCrypting.EncryptAesGcmWithPasswordIterations(plainText, password, iterations);
            Assert.IsFalse(encrypted.IsNull, "Encrypted value should not be null");
            
            // Act - Decryption with same iterations
            var decrypted = SqlCLRCrypting.DecryptAesGcmWithPasswordIterations(encrypted, password, iterations);
            Assert.IsFalse(decrypted.IsNull, "Decrypted value should not be null");
            
            // Assert
            Assert.AreEqual(plainText.Value, decrypted.Value, "Decrypted text should match original");
        }

        [TestMethod]
        [TestCategory("SQLCLR")]
        public void TestSqlCLR_EncryptAesGcmWithPassword_DifferentIterationsShouldFail()
        {
            // Arrange
            var plainText = new SqlString("Test with different iterations");
            var password = new SqlString("myPassword");
            var encryptIterations = new SqlInt32(2000);
            var decryptIterations = new SqlInt32(5000);
            
            // Act - Encryption
            var encrypted = SqlCLRCrypting.EncryptAesGcmWithPasswordIterations(plainText, password, encryptIterations);
            Assert.IsFalse(encrypted.IsNull, "Encrypted value should not be null");
            
            // Act - Decryption with different iterations (should fail)
            var decrypted = SqlCLRCrypting.DecryptAesGcmWithPasswordIterations(encrypted, password, decryptIterations);
            
            // Assert - Should return null due to decryption failure
            Assert.IsTrue(decrypted.IsNull, "Decryption with wrong iterations should fail");
        }

        [TestMethod]
        [TestCategory("SQLCLR")]
        public void TestSqlCLR_GenerateSalt()
        {
            // Act
            var salt = SqlCLRCrypting.GenerateSalt();
            
            // Assert
            Assert.IsFalse(salt.IsNull, "Generated salt should not be null");
            Assert.AreEqual(24, salt.Value.Length, "Default salt should be 16 bytes (24 chars in base64)");
            
            // Validate it's valid base64
            byte[] saltBytes = Convert.FromBase64String(salt.Value);
            Assert.AreEqual(16, saltBytes.Length, "Salt should be 16 bytes");
        }

        [TestMethod]
        [TestCategory("SQLCLR")]
        public void TestSqlCLR_GenerateSaltWithLength()
        {
            // Arrange
            var saltLength = new SqlInt32(32);
            
            // Act
            var salt = SqlCLRCrypting.GenerateSaltWithLength(saltLength);
            
            // Assert
            Assert.IsFalse(salt.IsNull, "Generated salt should not be null");
            
            // Validate it's valid base64 and correct length
            byte[] saltBytes = Convert.FromBase64String(salt.Value);
            Assert.AreEqual(32, saltBytes.Length, "Salt should be 32 bytes");
        }

        [TestMethod]
        [TestCategory("SQLCLR")]
        public void TestSqlCLR_GenerateSalt_InvalidLength()
        {
            // Arrange
            var tooSmall = new SqlInt32(4);  // Below minimum of 8
            var tooLarge = new SqlInt32(128); // Above maximum of 64
            
            // Act & Assert
            var smallSalt = SqlCLRCrypting.GenerateSaltWithLength(tooSmall);
            Assert.IsTrue(smallSalt.IsNull, "Salt with length < 8 should return null");
            
            var largeSalt = SqlCLRCrypting.GenerateSaltWithLength(tooLarge);
            Assert.IsTrue(largeSalt.IsNull, "Salt with length > 64 should return null");
        }

        [TestMethod]
        [TestCategory("SQLCLR")]
        public void TestSqlCLR_EncryptAesGcmWithPasswordAndSalt()
        {
            // Arrange
            var plainText = new SqlString("Test with custom salt");
            var password = new SqlString("myPassword");
            var salt = SqlCLRCrypting.GenerateSalt();
            
            Assert.IsFalse(salt.IsNull, "Generated salt should not be null");
            
            // Act - Encryption with custom salt
            var encrypted = SqlCLRCrypting.EncryptAesGcmWithPasswordAndSalt(plainText, password, salt);
            Assert.IsFalse(encrypted.IsNull, "Encrypted value should not be null");
            
            // Act - Decryption
            var decrypted = SqlCLRCrypting.DecryptAesGcmWithPassword(encrypted, password);
            Assert.IsFalse(decrypted.IsNull, "Decrypted value should not be null");
            
            // Assert
            Assert.AreEqual(plainText.Value, decrypted.Value, "Decrypted text should match original");
        }

        [TestMethod]
        [TestCategory("SQLCLR")]
        public void TestSqlCLR_EncryptAesGcmWithPasswordAndSaltIterations()
        {
            // Arrange
            var plainText = new SqlString("Test with custom salt and iterations");
            var password = new SqlString("myPassword");
            var salt = SqlCLRCrypting.GenerateSaltWithLength(new SqlInt32(24));
            var iterations = new SqlInt32(3000);
            
            Assert.IsFalse(salt.IsNull, "Generated salt should not be null");
            
            // Act - Encryption with custom salt and iterations
            var encrypted = SqlCLRCrypting.EncryptAesGcmWithPasswordAndSaltIterations(plainText, password, salt, iterations);
            Assert.IsFalse(encrypted.IsNull, "Encrypted value should not be null");
            
            // Act - Decryption with same iterations
            var decrypted = SqlCLRCrypting.DecryptAesGcmWithPasswordIterations(encrypted, password, iterations);
            Assert.IsFalse(decrypted.IsNull, "Decrypted value should not be null");
            
            // Assert
            Assert.AreEqual(plainText.Value, decrypted.Value, "Decrypted text should match original");
        }

        [TestMethod]
        [TestCategory("SQLCLR")]
        public void TestSqlCLR_PasswordBasedEncryption_CrossCompatibility()
        {
            // Test that the same password produces different encrypted results (due to random salt/nonce)
            var plainText = new SqlString("Same text, different encryption");
            var password = new SqlString("myPassword");
            
            // Act - Encrypt same text twice
            var encrypted1 = SqlCLRCrypting.EncryptAesGcmWithPassword(plainText, password);
            var encrypted2 = SqlCLRCrypting.EncryptAesGcmWithPassword(plainText, password);
            
            // Assert - Results should be different due to random salt/nonce
            Assert.IsFalse(encrypted1.IsNull, "First encryption should not be null");
            Assert.IsFalse(encrypted2.IsNull, "Second encryption should not be null");
            Assert.AreNotEqual(encrypted1.Value, encrypted2.Value, "Same text should produce different encrypted results");
            
            // But both should decrypt to the same plaintext
            var decrypted1 = SqlCLRCrypting.DecryptAesGcmWithPassword(encrypted1, password);
            var decrypted2 = SqlCLRCrypting.DecryptAesGcmWithPassword(encrypted2, password);
            
            Assert.AreEqual(plainText.Value, decrypted1.Value, "First decryption should match original");
            Assert.AreEqual(plainText.Value, decrypted2.Value, "Second decryption should match original");
        }

        [TestMethod]
        [TestCategory("SQLCLR")]
        public void TestSqlCLR_PasswordBasedEncryption_InvalidInputs()
        {
            // Test null inputs
            var nullResult1 = SqlCLRCrypting.EncryptAesGcmWithPassword(SqlString.Null, new SqlString("password"));
            var nullResult2 = SqlCLRCrypting.EncryptAesGcmWithPassword(new SqlString("text"), SqlString.Null);
            var nullResult3 = SqlCLRCrypting.DecryptAesGcmWithPassword(SqlString.Null, new SqlString("password"));
            var nullResult4 = SqlCLRCrypting.DecryptAesGcmWithPassword(new SqlString("data"), SqlString.Null);
            
            Assert.IsTrue(nullResult1.IsNull, "Null plainText should return null");
            Assert.IsTrue(nullResult2.IsNull, "Null password should return null");
            Assert.IsTrue(nullResult3.IsNull, "Null encrypted data should return null");
            Assert.IsTrue(nullResult4.IsNull, "Null password for decryption should return null");
        }

        [TestMethod]
        [TestCategory("SQLCLR")]
        public void TestSqlCLR_PasswordBasedEncryption_InvalidIterations()
        {
            // Arrange
            var plainText = new SqlString("Test invalid iterations");
            var password = new SqlString("myPassword");
            var tooLow = new SqlInt32(500);   // Below minimum of 1000
            var tooHigh = new SqlInt32(200000); // Above maximum of 100000
            
            // Act & Assert
            var lowResult = SqlCLRCrypting.EncryptAesGcmWithPasswordIterations(plainText, password, tooLow);
            Assert.IsTrue(lowResult.IsNull, "Iterations < 1000 should return null");
            
            var highResult = SqlCLRCrypting.EncryptAesGcmWithPasswordIterations(plainText, password, tooHigh);
            Assert.IsTrue(highResult.IsNull, "Iterations > 100000 should return null");
        }

        [TestMethod]
        [TestCategory("SQLCLR")]
        public void TestSqlCLR_PasswordBasedEncryption_LargeData()
        {
            // Arrange
            var largeText = new SqlString(new string('A', 10000)); // 10KB of data
            var password = new SqlString("myPassword");
            
            // Act
            var encrypted = SqlCLRCrypting.EncryptAesGcmWithPassword(largeText, password);
            Assert.IsFalse(encrypted.IsNull, "Large text encryption should not be null");
            
            var decrypted = SqlCLRCrypting.DecryptAesGcmWithPassword(encrypted, password);
            Assert.IsFalse(decrypted.IsNull, "Large text decryption should not be null");
            
            // Assert
            Assert.AreEqual(largeText.Value, decrypted.Value, "Large text should decrypt correctly");
        }

        [TestMethod]
        [TestCategory("SQLCLR")]
        public void TestSqlCLR_PasswordBasedEncryption_SpecialCharacters()
        {
            // Arrange
            var specialText = new SqlString("Special chars: !@#$%^&*()_+-=[]{}|;':\",./<>?`~");
            var password = new SqlString("Password with spaces and symbols: !@#");
            
            // Act
            var encrypted = SqlCLRCrypting.EncryptAesGcmWithPassword(specialText, password);
            Assert.IsFalse(encrypted.IsNull, "Special characters encryption should not be null");
            
            var decrypted = SqlCLRCrypting.DecryptAesGcmWithPassword(encrypted, password);
            Assert.IsFalse(decrypted.IsNull, "Special characters decryption should not be null");
            
            // Assert
            Assert.AreEqual(specialText.Value, decrypted.Value, "Special characters should decrypt correctly");
        }

        [TestMethod]
        [TestCategory("SQLCLR")]
        public void TestSqlCLR_PasswordBasedEncryption_UnicodeCharacters()
        {
            // Arrange
            var unicodeText = new SqlString("Unicode: 你好世界 🌍 🚀 こんにちは");
            var password = new SqlString("Unicode password: パスワード");
            
            // Act
            var encrypted = SqlCLRCrypting.EncryptAesGcmWithPassword(unicodeText, password);
            Assert.IsFalse(encrypted.IsNull, "Unicode encryption should not be null");
            
            var decrypted = SqlCLRCrypting.DecryptAesGcmWithPassword(encrypted, password);
            Assert.IsFalse(decrypted.IsNull, "Unicode decryption should not be null");
            
            // Assert
            Assert.AreEqual(unicodeText.Value, decrypted.Value, "Unicode text should decrypt correctly");
        }

        // SQL CLR Derived Key Tests
        [TestMethod]
        [TestCategory("SQLCLR")]
        public void TestSqlCLR_DeriveKeyFromPassword()
        {
            // Arrange
            var password = new SqlString("TestPassword123");
            var salt = SqlCLRCrypting.GenerateSalt();
            var iterations = new SqlInt32(2000);
            
            Assert.IsFalse(salt.IsNull, "Generated salt should not be null");
            
            // Act
            var derivedKey = SqlCLRCrypting.DeriveKeyFromPasswordIterations(password, salt, iterations);
            
            // Assert
            Assert.IsFalse(derivedKey.IsNull, "Derived key should not be null");
            Assert.AreEqual(44, derivedKey.Value.Length, "Key length should be 44 characters"); // 32 bytes = 44 chars in base64
            
            // Validate it's valid base64 and correct length
            byte[] keyBytes = Convert.FromBase64String(derivedKey.Value);
            Assert.AreEqual(32, keyBytes.Length, "Key should be 32 bytes"); // 256-bit key
        }

        [TestMethod]
        [TestCategory("SQLCLR")]
        public void TestSqlCLR_DeriveKeyFromPassword_DefaultIterations()
        {
            // Arrange
            var password = new SqlString("TestPassword123");
            var salt = SqlCLRCrypting.GenerateSaltWithLength(new SqlInt32(24));
            
            Assert.IsFalse(salt.IsNull, "Generated salt should not be null");
            
            // Act
            var derivedKey = SqlCLRCrypting.DeriveKeyFromPassword(password, salt);
            
            // Assert
            Assert.IsFalse(derivedKey.IsNull, "Derived key should not be null");
            byte[] keyBytes = Convert.FromBase64String(derivedKey.Value);
            Assert.AreEqual(32, keyBytes.Length, "Key should be 32 bytes");
        }

        [TestMethod]
        [TestCategory("SQLCLR")]
        public void TestSqlCLR_DeriveKeyFromPassword_Consistency()
        {
            // Arrange
            var password = new SqlString("TestPassword123");
            var salt = SqlCLRCrypting.GenerateSalt();
            var iterations = new SqlInt32(2000);
            
            Assert.IsFalse(salt.IsNull, "Generated salt should not be null");
            
            // Act - Derive key multiple times with same parameters
            var key1 = SqlCLRCrypting.DeriveKeyFromPasswordIterations(password, salt, iterations);
            var key2 = SqlCLRCrypting.DeriveKeyFromPasswordIterations(password, salt, iterations);
            var key3 = SqlCLRCrypting.DeriveKeyFromPasswordIterations(password, salt, iterations);
            
            // Assert - All should be identical
            Assert.IsFalse(key1.IsNull, "First key should not be null");
            Assert.IsFalse(key2.IsNull, "Second key should not be null");
            Assert.IsFalse(key3.IsNull, "Third key should not be null");
            Assert.AreEqual(key1.Value, key2.Value, "First and second keys should match");
            Assert.AreEqual(key2.Value, key3.Value, "Second and third keys should match");
            Assert.AreEqual(key1.Value, key3.Value, "First and third keys should match");
        }

        [TestMethod]
        [TestCategory("SQLCLR")]
        public void TestSqlCLR_DeriveKeyFromPassword_InvalidInputs()
        {
            // Test null inputs
            var nullResult1 = SqlCLRCrypting.DeriveKeyFromPassword(SqlString.Null, new SqlString("salt"));
            var nullResult2 = SqlCLRCrypting.DeriveKeyFromPassword(new SqlString("password"), SqlString.Null);
            
            Assert.IsTrue(nullResult1.IsNull, "Null password should return null");
            Assert.IsTrue(nullResult2.IsNull, "Null salt should return null");
            
            // Test invalid salt
            var invalidSalt = new SqlString(Convert.ToBase64String(new byte[4]));
            var invalidSaltResult = SqlCLRCrypting.DeriveKeyFromPassword(new SqlString("password"), invalidSalt);
            Assert.IsTrue(invalidSaltResult.IsNull, "Invalid salt should return null");
            
            // Test invalid iterations
            var tooLow = new SqlInt32(500);   // Below minimum of 1000
            var tooHigh = new SqlInt32(200000); // Above maximum of 100000
            var validSalt = SqlCLRCrypting.GenerateSalt();
            
            Assert.IsFalse(validSalt.IsNull, "Valid salt should not be null");
            
            var lowResult = SqlCLRCrypting.DeriveKeyFromPasswordIterations(new SqlString("password"), validSalt, tooLow);
            var highResult = SqlCLRCrypting.DeriveKeyFromPasswordIterations(new SqlString("password"), validSalt, tooHigh);
            
            Assert.IsTrue(lowResult.IsNull, "Iterations < 1000 should return null");
            Assert.IsTrue(highResult.IsNull, "Iterations > 100000 should return null");
        }

        [TestMethod]
        [TestCategory("SQLCLR")]
        public void TestSqlCLR_EncryptAesGcmWithDerivedKey()
        {
            // Arrange
            var plainText = new SqlString("Test with derived key");
            var password = new SqlString("myPassword");
            var salt = SqlCLRCrypting.GenerateSalt();
            var iterations = new SqlInt32(2000);
            
            Assert.IsFalse(salt.IsNull, "Generated salt should not be null");
            
            var derivedKey = SqlCLRCrypting.DeriveKeyFromPasswordIterations(password, salt, iterations);
            Assert.IsFalse(derivedKey.IsNull, "Derived key should not be null");
            
            // Act
            var encrypted = SqlCLRCrypting.EncryptAesGcmWithDerivedKey(plainText, derivedKey, salt);
            
            // Assert
            Assert.IsFalse(encrypted.IsNull, "Encrypted value should not be null");
            Assert.AreNotEqual(plainText.Value, encrypted.Value, "Encrypted text should not match original");
        }

        [TestMethod]
        [TestCategory("SQLCLR")]
        public void TestSqlCLR_DecryptAesGcmWithDerivedKey()
        {
            // Arrange
            var plainText = new SqlString("Test decryption with derived key");
            var password = new SqlString("myPassword");
            var salt = SqlCLRCrypting.GenerateSalt();
            var iterations = new SqlInt32(2000);
            
            Assert.IsFalse(salt.IsNull, "Generated salt should not be null");
            
            var derivedKey = SqlCLRCrypting.DeriveKeyFromPasswordIterations(password, salt, iterations);
            Assert.IsFalse(derivedKey.IsNull, "Derived key should not be null");
            
            var encrypted = SqlCLRCrypting.EncryptAesGcmWithDerivedKey(plainText, derivedKey, salt);
            Assert.IsFalse(encrypted.IsNull, "Encrypted value should not be null");
            
            // Act
            var decrypted = SqlCLRCrypting.DecryptAesGcmWithDerivedKey(encrypted, derivedKey);
            
            // Assert
            Assert.IsFalse(decrypted.IsNull, "Decrypted value should not be null");
            Assert.AreEqual(plainText.Value, decrypted.Value, "Decrypted text should match original");
        }

        [TestMethod]
        [TestCategory("SQLCLR")]
        public void TestSqlCLR_DerivedKeyEncryptionDecryption_CrossCompatibility()
        {
            // Test that derived key encryption/decryption works with password-based methods
            var plainText = new SqlString("Cross compatibility test");
            var password = new SqlString("myPassword");
            var salt = SqlCLRCrypting.GenerateSalt();
            var iterations = new SqlInt32(2000);
            
            Assert.IsFalse(salt.IsNull, "Generated salt should not be null");
            
            // Derive key
            var derivedKey = SqlCLRCrypting.DeriveKeyFromPasswordIterations(password, salt, iterations);
            Assert.IsFalse(derivedKey.IsNull, "Derived key should not be null");
            
            // Encrypt with derived key
            var encryptedWithKey = SqlCLRCrypting.EncryptAesGcmWithDerivedKey(plainText, derivedKey, salt);
            Assert.IsFalse(encryptedWithKey.IsNull, "Encryption with derived key should not be null");
            
            // Decrypt with password-based method
            var decryptedWithPassword = SqlCLRCrypting.DecryptAesGcmWithPasswordIterations(encryptedWithKey, password, iterations);
            
            // Assert
            Assert.IsFalse(decryptedWithPassword.IsNull, "Decryption with password should not be null");
            Assert.AreEqual(plainText.Value, decryptedWithPassword.Value, "Cross-compatibility decryption should match original");
        }

        [TestMethod]
        [TestCategory("SQLCLR")]
        public void TestSqlCLR_DerivedKeyEncryptionDecryption_ReverseCrossCompatibility()
        {
            // Test that password-based encryption works with derived key decryption
            var plainText = new SqlString("Reverse cross compatibility test");
            var password = new SqlString("myPassword");
            var salt = SqlCLRCrypting.GenerateSalt();
            var iterations = new SqlInt32(2000);
            
            Assert.IsFalse(salt.IsNull, "Generated salt should not be null");
            
            // Derive key
            var derivedKey = SqlCLRCrypting.DeriveKeyFromPasswordIterations(password, salt, iterations);
            Assert.IsFalse(derivedKey.IsNull, "Derived key should not be null");
            
            // Encrypt with password-based method
            var encryptedWithPassword = SqlCLRCrypting.EncryptAesGcmWithPasswordAndSaltIterations(plainText, password, salt, iterations);
            Assert.IsFalse(encryptedWithPassword.IsNull, "Encryption with password should not be null");
            
            // Decrypt with derived key
            var decryptedWithKey = SqlCLRCrypting.DecryptAesGcmWithDerivedKey(encryptedWithPassword, derivedKey);
            
            // Assert
            Assert.IsFalse(decryptedWithKey.IsNull, "Decryption with derived key should not be null");
            Assert.AreEqual(plainText.Value, decryptedWithKey.Value, "Reverse cross-compatibility decryption should match original");
        }

        [TestMethod]
        [TestCategory("SQLCLR")]
        public void TestSqlCLR_DerivedKeyEncryption_InvalidInputs()
        {
            // Test null inputs for encryption with derived key
            var validKey = SqlCLRCrypting.DeriveKeyFromPassword(new SqlString("password"), SqlCLRCrypting.GenerateSalt());
            var validSalt = SqlCLRCrypting.GenerateSalt();
            
            Assert.IsFalse(validKey.IsNull, "Valid key should not be null");
            Assert.IsFalse(validSalt.IsNull, "Valid salt should not be null");
            
            var nullResult1 = SqlCLRCrypting.EncryptAesGcmWithDerivedKey(SqlString.Null, validKey, validSalt);
            var nullResult2 = SqlCLRCrypting.EncryptAesGcmWithDerivedKey(new SqlString("text"), SqlString.Null, validSalt);
            var nullResult3 = SqlCLRCrypting.EncryptAesGcmWithDerivedKey(new SqlString("text"), validKey, SqlString.Null);
            
            Assert.IsTrue(nullResult1.IsNull, "Null plainText should return null");
            Assert.IsTrue(nullResult2.IsNull, "Null derived key should return null");
            Assert.IsTrue(nullResult3.IsNull, "Null salt should return null");
            
            // Test invalid key length
            var invalidKey = new SqlString(Convert.ToBase64String(new byte[16]));
            var invalidKeyResult = SqlCLRCrypting.EncryptAesGcmWithDerivedKey(new SqlString("text"), invalidKey, validSalt);
            Assert.IsTrue(invalidKeyResult.IsNull, "Invalid key length should return null");
            
            // Test invalid salt
            var invalidSalt = new SqlString(Convert.ToBase64String(new byte[4]));
            var invalidSaltResult = SqlCLRCrypting.EncryptAesGcmWithDerivedKey(new SqlString("text"), validKey, invalidSalt);
            Assert.IsTrue(invalidSaltResult.IsNull, "Invalid salt should return null");
        }

        [TestMethod]
        [TestCategory("SQLCLR")]
        public void TestSqlCLR_DerivedKeyDecryption_InvalidInputs()
        {
            // Test null inputs for decryption with derived key
            var validKey = SqlCLRCrypting.DeriveKeyFromPassword(new SqlString("password"), SqlCLRCrypting.GenerateSalt());
            Assert.IsFalse(validKey.IsNull, "Valid key should not be null");
            
            var nullResult1 = SqlCLRCrypting.DecryptAesGcmWithDerivedKey(SqlString.Null, validKey);
            var nullResult2 = SqlCLRCrypting.DecryptAesGcmWithDerivedKey(new SqlString("data"), SqlString.Null);
            
            Assert.IsTrue(nullResult1.IsNull, "Null encrypted data should return null");
            Assert.IsTrue(nullResult2.IsNull, "Null derived key should return null");
            
            // Test invalid key length
            var invalidKey = new SqlString(Convert.ToBase64String(new byte[16]));
            var invalidKeyResult = SqlCLRCrypting.DecryptAesGcmWithDerivedKey(new SqlString("dummy"), invalidKey);
            Assert.IsTrue(invalidKeyResult.IsNull, "Invalid key length should return null");
        }

        [TestMethod]
        [TestCategory("SQLCLR")]
        public void TestSqlCLR_DerivedKeyEncryption_LargeData()
        {
            // Arrange
            var largeText = new SqlString(new string('A', 10000)); // 10KB of data
            var password = new SqlString("myPassword");
            var salt = SqlCLRCrypting.GenerateSalt();
            var iterations = new SqlInt32(2000);
            
            Assert.IsFalse(salt.IsNull, "Generated salt should not be null");
            
            var derivedKey = SqlCLRCrypting.DeriveKeyFromPasswordIterations(password, salt, iterations);
            Assert.IsFalse(derivedKey.IsNull, "Derived key should not be null");
            
            // Act
            var encrypted = SqlCLRCrypting.EncryptAesGcmWithDerivedKey(largeText, derivedKey, salt);
            Assert.IsFalse(encrypted.IsNull, "Large text encryption should not be null");
            
            var decrypted = SqlCLRCrypting.DecryptAesGcmWithDerivedKey(encrypted, derivedKey);
            Assert.IsFalse(decrypted.IsNull, "Large text decryption should not be null");
            
            // Assert
            Assert.AreEqual(largeText.Value, decrypted.Value, "Large text should decrypt correctly");
        }

        [TestMethod]
        [TestCategory("SQLCLR")]
        public void TestSqlCLR_DerivedKeyEncryption_SpecialCharacters()
        {
            // Arrange
            var specialText = new SqlString("Special chars: !@#$%^&*()_+-=[]{}|;':\",./<>?`~");
            var password = new SqlString("Password with spaces and symbols: !@#");
            var salt = SqlCLRCrypting.GenerateSalt();
            var iterations = new SqlInt32(2000);
            
            Assert.IsFalse(salt.IsNull, "Generated salt should not be null");
            
            var derivedKey = SqlCLRCrypting.DeriveKeyFromPasswordIterations(password, salt, iterations);
            Assert.IsFalse(derivedKey.IsNull, "Derived key should not be null");
            
            // Act
            var encrypted = SqlCLRCrypting.EncryptAesGcmWithDerivedKey(specialText, derivedKey, salt);
            Assert.IsFalse(encrypted.IsNull, "Special characters encryption should not be null");
            
            var decrypted = SqlCLRCrypting.DecryptAesGcmWithDerivedKey(encrypted, derivedKey);
            Assert.IsFalse(decrypted.IsNull, "Special characters decryption should not be null");
            
            // Assert
            Assert.AreEqual(specialText.Value, decrypted.Value, "Special characters should decrypt correctly");
        }

        [TestMethod]
        [TestCategory("SQLCLR")]
        public void TestSqlCLR_DerivedKeyEncryption_UnicodeCharacters()
        {
            // Arrange
            var unicodeText = new SqlString("Unicode: 你好世界 🌍 🚀 こんにちは");
            var password = new SqlString("Unicode password: パスワード");
            var salt = SqlCLRCrypting.GenerateSalt();
            var iterations = new SqlInt32(2000);
            
            Assert.IsFalse(salt.IsNull, "Generated salt should not be null");
            
            var derivedKey = SqlCLRCrypting.DeriveKeyFromPasswordIterations(password, salt, iterations);
            Assert.IsFalse(derivedKey.IsNull, "Derived key should not be null");
            
            // Act
            var encrypted = SqlCLRCrypting.EncryptAesGcmWithDerivedKey(unicodeText, derivedKey, salt);
            Assert.IsFalse(encrypted.IsNull, "Unicode encryption should not be null");
            
            var decrypted = SqlCLRCrypting.DecryptAesGcmWithDerivedKey(encrypted, derivedKey);
            Assert.IsFalse(decrypted.IsNull, "Unicode decryption should not be null");
            
            // Assert
            Assert.AreEqual(unicodeText.Value, decrypted.Value, "Unicode text should decrypt correctly");
        }

        [TestMethod]
        [TestCategory("SQLCLR")]
        public void TestSqlCLR_DerivedKeyEncryption_DifferentSaltsShouldFail()
        {
            // Arrange
            var plainText = new SqlString("Test with different salts");
            var password = new SqlString("myPassword");
            var salt1 = SqlCLRCrypting.GenerateSalt();
            var salt2 = SqlCLRCrypting.GenerateSalt();
            var iterations = new SqlInt32(2000);
            
            Assert.IsFalse(salt1.IsNull, "First salt should not be null");
            Assert.IsFalse(salt2.IsNull, "Second salt should not be null");
            
            var derivedKey1 = SqlCLRCrypting.DeriveKeyFromPasswordIterations(password, salt1, iterations);
            var derivedKey2 = SqlCLRCrypting.DeriveKeyFromPasswordIterations(password, salt2, iterations);
            
            Assert.IsFalse(derivedKey1.IsNull, "First derived key should not be null");
            Assert.IsFalse(derivedKey2.IsNull, "Second derived key should not be null");
            
            // Act
            var encrypted = SqlCLRCrypting.EncryptAesGcmWithDerivedKey(plainText, derivedKey1, salt1);
            Assert.IsFalse(encrypted.IsNull, "Encryption should not be null");
            
            // Assert - Should return null when trying to decrypt with key derived from different salt
            var decrypted = SqlCLRCrypting.DecryptAesGcmWithDerivedKey(encrypted, derivedKey2);
            Assert.IsTrue(decrypted.IsNull, "Decryption with key from different salt should return null");
        }

        [TestMethod]
        [TestCategory("SQLCLR")]
        public void TestSqlCLR_DerivedKeyEncryption_WrongKeyShouldFail()
        {
            // Arrange
            var plainText = new SqlString("Test with wrong key");
            var password2 = new SqlString("myPassword2");
            var salt = SqlCLRCrypting.GenerateSalt();
            var iterations = new SqlInt32(2000);
            
            Assert.IsFalse(salt.IsNull, "Salt should not be null");
            var password = new SqlString("myPassword1");
            var derivedKey1 = SqlCLRCrypting.DeriveKeyFromPasswordIterations(password, salt, iterations);
            var derivedKey2 = SqlCLRCrypting.DeriveKeyFromPasswordIterations(password2, salt, iterations);
            
            Assert.IsFalse(derivedKey1.IsNull, "First derived key should not be null");
            Assert.IsFalse(derivedKey2.IsNull, "Second derived key should not be null");
            
            // Act
            var encrypted = SqlCLRCrypting.EncryptAesGcmWithDerivedKey(plainText, derivedKey1, salt);
            Assert.IsFalse(encrypted.IsNull, "Encryption should not be null");
            
            // Assert - Should return null when trying to decrypt with wrong key
            var decrypted = SqlCLRCrypting.DecryptAesGcmWithDerivedKey(encrypted, derivedKey2);
            Assert.IsTrue(decrypted.IsNull, "Decryption with wrong key should return null");
        }

        [TestMethod]
        [TestCategory("SQLCLR")]
        public void TestSqlCLR_PasswordBasedEncryption_EmptyString()
        {
            // Arrange
            var emptyText = new SqlString("");
            var password = new SqlString("myPassword");

            // Act
            var encrypted = SqlCLRCrypting.EncryptAesGcmWithPassword(emptyText, password);
            Assert.IsFalse(encrypted.IsNull, "Encryption of empty string should not be null");

            var decrypted = SqlCLRCrypting.DecryptAesGcmWithPassword(encrypted, password);
            Assert.IsFalse(decrypted.IsNull, "Decryption of empty string should not be null");

            // Assert
            Assert.AreEqual(emptyText.Value, decrypted.Value, "Empty string should decrypt correctly");
        }

        [TestMethod]
        [TestCategory("SQLCLR")]
        public void TestSqlCLR_PasswordBasedEncryption_EmptyPassword()
        {
            // Arrange
            var plainText = new SqlString("Test with empty password");
            var emptyPassword = new SqlString("");

            // Act
            var encrypted = SqlCLRCrypting.EncryptAesGcmWithPassword(plainText, emptyPassword);
            Assert.IsFalse(encrypted.IsNull, "Encryption with empty password should not be null");

            var decrypted = SqlCLRCrypting.DecryptAesGcmWithPassword(encrypted, emptyPassword);
            Assert.IsFalse(decrypted.IsNull, "Decryption with empty password should not be null");

            // Assert
            Assert.AreEqual(plainText.Value, decrypted.Value, "Text should decrypt correctly with empty password");
        }
    }
} 
#endif