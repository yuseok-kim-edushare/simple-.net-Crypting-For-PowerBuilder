using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace SecureLibrary
{
    [ComVisible(true)]
    [Guid("9E506401-739E-402D-A11F-C77E7768362B")]
    [ClassInterface(ClassInterfaceType.None)]
    public class EncryptionHelper
    {
        public static string EncryptAesGcm(string plainText, string base64Key)
        {
            if (plainText == null) throw new ArgumentNullException("plainText");
            if (string.IsNullOrEmpty(base64Key)) throw new ArgumentNullException("base64Key");

            // Generate new nonce
            byte[] nonce = new byte[12];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(nonce);
            }
            string base64Nonce = Convert.ToBase64String(nonce);

            // Get the encrypted result
            string encryptedBase64 = BcryptInterop.EncryptAesGcm(plainText, base64Key, base64Nonce);

            // Combine nonce and ciphertext
            return base64Nonce + ":" + encryptedBase64;
        }

        public static string DecryptAesGcm(string combinedData, string base64Key)
        {
            if (string.IsNullOrEmpty(combinedData)) throw new ArgumentNullException("combinedData");
            if (string.IsNullOrEmpty(base64Key)) throw new ArgumentNullException("base64Key");

            // Split the combined data
            string[] parts = combinedData.Split(':');
            if (parts.Length != 2)
                throw new ArgumentException("Invalid encrypted data format", "combinedData");

            string base64Nonce = parts[0];
            string encryptedBase64 = parts[1];

            // Decrypt using the extracted nonce
            return BcryptInterop.DecryptAesGcm(encryptedBase64, base64Key, base64Nonce);
        }

        // this section for Symmetric Encryption with AES CBC mode
        [Obsolete("This method is deprecated because AES-CBC without an authentication mechanism is insecure. Please use EncryptAesGcm instead.")]
        public static string[] EncryptAesCbcWithIv(string plainText, string base64Key)
        {    
            byte[] key = Convert.FromBase64String(base64Key);
            if (key.Length != 32) // 256 bits
                throw new ArgumentException("Invalid key length", nameof(base64Key));
            
            try {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.GenerateIV(); // Generate IV
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                    byte[] cipherText;
                    using (var memoryStream = new System.IO.MemoryStream())
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            cryptoStream.Write(plainBytes, 0, plainBytes.Length);
                            cryptoStream.FlushFinalBlock();
                            cipherText = memoryStream.ToArray();
                        }
                    }
                    string base64CipherText = Convert.ToBase64String(cipherText);
                    string base64IV = Convert.ToBase64String(aes.IV);
                    Array.Clear(key, 0, key.Length);
                    aes.Clear();
                    return new string[] { base64CipherText, base64IV };
                }
            }
            finally {
                Array.Clear(key, 0, key.Length);
            }
        }
        [Obsolete("This method is deprecated because AES-CBC without an authentication mechanism is insecure. Please use DecryptAesGcm instead.")]
        public static string DecryptAesCbcWithIv(string base64CipherText, string base64Key, string base64IV)
        {
            byte[] key = Convert.FromBase64String(base64Key);
            if (key.Length != 32) // 256 bits
                throw new ArgumentException("Invalid key length", nameof(base64Key));
            byte[] cipherText = Convert.FromBase64String(base64CipherText);
            byte[] iv = Convert.FromBase64String(base64IV);
            if (iv.Length != 16) // 128 bits
                throw new ArgumentException("Invalid IV length", nameof(base64IV));
            try {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    byte[] decryptedBytes;
                    using (var memoryStream = new System.IO.MemoryStream(cipherText))
                    {
                    using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        using (var reader = new System.IO.StreamReader(cryptoStream, Encoding.UTF8))
                        {
                            decryptedBytes = Encoding.UTF8.GetBytes(reader.ReadToEnd());
                        }
                        }
                    }
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
            finally {
                Array.Clear(key, 0, key.Length);
                Array.Clear(cipherText, 0, cipherText.Length);
                Array.Clear(iv, 0, iv.Length);
            }
        }
        public static string KeyGenAES256()
        {
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.GenerateKey();
                return Convert.ToBase64String(aes.Key);
            }
        }

        
        // this section related about diffie hellman
        public static string[] GenerateDiffieHellmanKeys()
        {
            using (ECDiffieHellmanCng dh = new ECDiffieHellmanCng())
            {
                dh.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
                dh.HashAlgorithm = CngAlgorithm.Sha256;
                byte[] publicKey = dh.PublicKey.ToByteArray();
                byte[] privateKey = dh.Key.Export(CngKeyBlobFormat.EccPrivateBlob);
                return new string[] { 
                    Convert.ToBase64String(publicKey), 
                    Convert.ToBase64String(privateKey) 
                };
            }
        }

        public static string DeriveSharedKey(string otherPartyPublicKeyBase64, string privateKeyBase64)
        {
            byte[] otherPartyPublicKey = Convert.FromBase64String(otherPartyPublicKeyBase64);
            byte[] privateKey = Convert.FromBase64String(privateKeyBase64);
            
            try
            {
                using (ECDiffieHellmanCng dh = new ECDiffieHellmanCng(CngKey.Import(privateKey, CngKeyBlobFormat.EccPrivateBlob)))
                {
                    dh.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
                    dh.HashAlgorithm = CngAlgorithm.Sha256;

                    try
                    {
                        // Try importing as EccPublicBlob first
                        using (var importedKey = CngKey.Import(otherPartyPublicKey, CngKeyBlobFormat.EccPublicBlob))
                        {
                            return Convert.ToBase64String(dh.DeriveKeyMaterial(importedKey));
                        }
                    }
                    catch
                    {
                        // If EccPublicBlob fails, try as GenericPublicBlob
                        using (var importedKey = CngKey.Import(otherPartyPublicKey, CngKeyBlobFormat.GenericPublicBlob))
                        {
                            return Convert.ToBase64String(dh.DeriveKeyMaterial(importedKey));
                        }
                    }
                }
            }
            finally
            {
                Array.Clear(otherPartyPublicKey, 0, otherPartyPublicKey.Length);
                Array.Clear(privateKey, 0, privateKey.Length);
            }
        }
        // this section related about bcrypt
        public static string BcryptEncoding(string password, int workFactor = 12)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor);
        }
        public static bool VerifyBcryptPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }

        // Password-based AES-GCM encryption methods
        /// <summary>
        /// Encrypts text using AES-GCM with password-based key derivation
        /// </summary>
        /// <param name="plainText">Text to encrypt</param>
        /// <param name="password">Password for key derivation</param>
        /// <param name="iterations">PBKDF2 iteration count (default: 2000)</param>
        /// <returns>Base64 encoded encrypted data with salt, nonce, and tag</returns>
        public static string EncryptAesGcmWithPassword(string plainText, string password, int iterations = 2000)
        {
            if (plainText == null) throw new ArgumentNullException("plainText");
            if (password == null) throw new ArgumentNullException("password");

            // Validate iteration count
            if (iterations < 1000 || iterations > 100000)
                throw new ArgumentException("Iteration count must be between 1000 and 100000", "iterations");

            return BcryptInterop.EncryptAesGcmWithPassword(plainText, password, null, iterations);
        }

        /// <summary>
        /// Decrypts text using AES-GCM with password-based key derivation
        /// </summary>
        /// <param name="base64EncryptedData">Base64 encoded encrypted data</param>
        /// <param name="password">Password for key derivation</param>
        /// <param name="iterations">PBKDF2 iteration count (default: 2000)</param>
        /// <returns>Decrypted text</returns>
        public static string DecryptAesGcmWithPassword(string base64EncryptedData, string password, int iterations = 2000)
        {
            if (base64EncryptedData == null) throw new ArgumentNullException("base64EncryptedData");
            if (password == null) throw new ArgumentNullException("password");

            // Validate iteration count
            if (iterations < 1000 || iterations > 100000)
                throw new ArgumentException("Iteration count must be between 1000 and 100000", "iterations");

            return BcryptInterop.DecryptAesGcmWithPassword(base64EncryptedData, password, iterations);
        }

        /// <summary>
        /// Generates a cryptographically secure random salt for key derivation
        /// </summary>
        /// <param name="saltLength">Length of salt in bytes (optional, default: 16)</param>
        /// <returns>Base64 encoded salt</returns>
        public static string GenerateSalt(int saltLength = 16)
        {
            if (saltLength < 8 || saltLength > 64)
                throw new ArgumentException("Salt length must be between 8 and 64 bytes", "saltLength");

            byte[] salt = new byte[saltLength];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }
            
            return Convert.ToBase64String(salt);
        }

        /// <summary>
        /// Encrypts text using AES-GCM with password and custom salt
        /// </summary>
        /// <param name="plainText">Text to encrypt</param>
        /// <param name="password">Password for key derivation</param>
        /// <param name="base64Salt">Base64 encoded salt for key derivation</param>
        /// <param name="iterations">PBKDF2 iteration count (optional, default: 2000)</param>
        /// <returns>Base64 encoded encrypted data with salt, nonce, and tag</returns>
        public static string EncryptAesGcmWithPasswordAndSalt(string plainText, string password, string base64Salt, int iterations = 2000)
        {
            if (string.IsNullOrEmpty(plainText)) throw new ArgumentNullException("plainText");
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException("password");
            if (string.IsNullOrEmpty(base64Salt)) throw new ArgumentNullException("base64Salt");

            byte[] saltBytes = Convert.FromBase64String(base64Salt);
            
            // Validate salt length
            if (saltBytes.Length < 8 || saltBytes.Length > 64)
                throw new ArgumentException("Salt length must be between 8 and 64 bytes", "base64Salt");

            // Validate iteration count
            if (iterations < 1000 || iterations > 100000)
                throw new ArgumentException("Iteration count must be between 1000 and 100000", "iterations");

            try
            {
                return BcryptInterop.EncryptAesGcmWithPassword(plainText, password, saltBytes, iterations);
            }
            finally
            {
                Array.Clear(saltBytes, 0, saltBytes.Length);
            }
        }

        /// <summary>
        /// Derives an AES-256 key from a password using PBKDF2. 
        /// This key can be cached and reused for multiple encrypt/decrypt operations for performance.
        /// </summary>
        /// <param name="password">Password for key derivation</param>
        /// <param name="base64Salt">Base64 encoded salt for key derivation</param>
        /// <param name="iterations">PBKDF2 iteration count (default: 2000)</param>
        /// <returns>Base64 encoded 32-byte AES key that can be cached and reused</returns>
        public static string DeriveKeyFromPassword(string password, string base64Salt, int iterations = 2000)
        {
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException("password");
            if (string.IsNullOrEmpty(base64Salt)) throw new ArgumentNullException("base64Salt");

            byte[] saltBytes = Convert.FromBase64String(base64Salt);
            
            // Validate salt length
            if (saltBytes.Length < 8 || saltBytes.Length > 64)
                throw new ArgumentException("Salt length must be between 8 and 64 bytes", "base64Salt");

            // Validate iteration count
            if (iterations < 1000 || iterations > 100000)
                throw new ArgumentException("Iteration count must be between 1000 and 100000", "iterations");

            byte[] key;
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, iterations, HashAlgorithmName.SHA256))
            {
                key = pbkdf2.GetBytes(32);
            }

            string result = Convert.ToBase64String(key);
            
            // Clear sensitive data
            Array.Clear(key, 0, key.Length);
            Array.Clear(saltBytes, 0, saltBytes.Length);
            
            return result;
        }

        /// <summary>
        /// Encrypts text using AES-GCM with a pre-derived key. 
        /// This method produces the same output format as EncryptAesGcmWithPassword but avoids key derivation overhead.
        /// Use DeriveKeyFromPassword to get the key first, then cache and reuse it for multiple operations.
        /// </summary>
        /// <param name="plainText">Text to encrypt</param>
        /// <param name="base64DerivedKey">Base64 encoded 32-byte AES key from DeriveKeyFromPassword</param>
        /// <param name="base64Salt">Base64 encoded salt used for key derivation (needed for output format compatibility)</param>
        /// <returns>Base64 encoded encrypted data with salt, nonce, and tag (same format as password-based methods)</returns>
        public static string EncryptAesGcmWithDerivedKey(string plainText, string base64DerivedKey, string base64Salt)
        {
            if (plainText == null) throw new ArgumentNullException("plainText");
            if (base64DerivedKey == null) throw new ArgumentNullException("base64DerivedKey");
            if (base64Salt == null) throw new ArgumentNullException("base64Salt");

            byte[] key = Convert.FromBase64String(base64DerivedKey);
            byte[] saltBytes = Convert.FromBase64String(base64Salt);
            
            // Validate key length
            if (key.Length != 32)
                throw new ArgumentException("Derived key must be 32 bytes", "base64DerivedKey");
            
            // Validate salt length
            if (saltBytes.Length < 8 || saltBytes.Length > 64)
                throw new ArgumentException("Salt length must be between 8 and 64 bytes", "base64Salt");

            try
            {
                return BcryptInterop.EncryptAesGcmWithDerivedKey(plainText, key, saltBytes);
            }
            finally
            {
                Array.Clear(key, 0, key.Length);
                Array.Clear(saltBytes, 0, saltBytes.Length);
            }
        }

        /// <summary>
        /// Decrypts text using AES-GCM with a pre-derived key.
        /// This method can decrypt data encrypted with either EncryptAesGcmWithPassword or EncryptAesGcmWithDerivedKey.
        /// </summary>
        /// <param name="base64EncryptedData">Base64 encoded encrypted data</param>
        /// <param name="base64DerivedKey">Base64 encoded 32-byte AES key from DeriveKeyFromPassword</param>
        /// <returns>Decrypted text</returns>
        public static string DecryptAesGcmWithDerivedKey(string base64EncryptedData, string base64DerivedKey)
        {
            if (base64EncryptedData == null) throw new ArgumentNullException("base64EncryptedData");
            if (base64DerivedKey == null) throw new ArgumentNullException("base64DerivedKey");

            byte[] key = Convert.FromBase64String(base64DerivedKey);
            
            // Validate key length
            if (key.Length != 32)
                throw new ArgumentException("Derived key must be 32 bytes", "base64DerivedKey");

            try
            {
                return BcryptInterop.DecryptAesGcmWithDerivedKey(base64EncryptedData, key);
            }
            finally
            {
                Array.Clear(key, 0, key.Length);
            }
        }
    }
}