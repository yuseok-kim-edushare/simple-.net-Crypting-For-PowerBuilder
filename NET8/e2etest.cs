#if !RELEASE_WITHOUT_TESTS
using NUnit.Framework;
using SecureLibrary.SQL;
using System.Linq;
using System.Data.SqlTypes;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;

namespace SecureLibrary.Tests
{
    [TestFixture]
    public class CrossFrameworkTests
    {
        [Test]
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
                Assert.That(!net481Keys[0].IsNull && !net481Keys[1].IsNull);

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
                Assert.That(net8SharedSecret, Is.EqualTo(net481SharedSecret.Value));

                // Step 3: Test cross-framework encryption/decryption
                string testMessage = "Hello from cross-framework test!";
                string aesKey = EncryptionHelper.KeyGenAES256();

                // NET 8 encrypts, NET 4.8.1 decrypts
                string net8Encrypted = EncryptionHelper.EncryptAesGcm(testMessage, aesKey);
                SqlString net481Decrypted = SqlCLRCrypting.DecryptAesGcm(
                    new SqlString(net8Encrypted),
                    new SqlString(aesKey)
                );
                Assert.That(net481Decrypted.Value, Is.EqualTo(testMessage));

                // NET 4.8.1 encrypts, NET 8 decrypts
                SqlString net481Encrypted = SqlCLRCrypting.EncryptAesGcm(
                    new SqlString(testMessage),
                    new SqlString(aesKey)
                );
                string net8Decrypted = EncryptionHelper.DecryptAesGcm(net481Encrypted.Value, aesKey);
                Assert.That(net8Decrypted, Is.EqualTo(testMessage));
            }
        }
    }
} 
#endif