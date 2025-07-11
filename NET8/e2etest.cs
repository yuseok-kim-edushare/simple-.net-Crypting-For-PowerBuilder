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
        public void TestSqlCLR_GenerateAESKey()
        {
            // Act
            var generatedKey = SqlCLRCrypting.GenerateAESKey();
            
            // Assert
            Assert.IsNotNull(generatedKey, "Generated key should not be null");
            Assert.AreEqual(44, generatedKey.Value.Length, "Key length should be 44 characters");
        }
        
        [TestMethod]
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
        public void TestSqlCLR_HashPassword()
        {
            // Arrange
            var password = new SqlString("securePassword123");
            
            // Act
            var hashedPassword = SqlCLRCrypting.HashPassword(password);
            
            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(hashedPassword.Value), "Hashed password should not be null");
            Assert.IsTrue(hashedPassword.Value.StartsWith("$2"), "Hash should be in BCrypt format");
        }
        
        [TestMethod]
        public void TestSqlCLR_VerifyPassword()
        {
            // Arrange
            var password = new SqlString("securePassword123");
            var hashedPassword = SqlCLRCrypting.HashPassword(password);
            
            // Act
            var isValid = SqlCLRCrypting.VerifyPassword(password, hashedPassword);
            var isInvalid = SqlCLRCrypting.VerifyPassword(new SqlString("WrongPassword"), hashedPassword);
            
            // Assert
            Assert.IsTrue(isValid.IsTrue, "Password verification should succeed");
            Assert.IsFalse(isInvalid.IsTrue, "Wrong password should fail verification");
        }

        [TestMethod]
        public void TestSqlCLR_GenerateDiffieHellmanKeys()
        {
            // Act
            var keysResult = SqlCLRCrypting.GenerateDiffieHellmanKeys().Cast<SqlString[]>().First();

            // Assert
            Assert.IsFalse(keysResult[0].IsNull, "Public key should not be null");
            Assert.IsFalse(keysResult[1].IsNull, "Private key should not be null");
        }

        [TestMethod]
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
    }
} 
#endif