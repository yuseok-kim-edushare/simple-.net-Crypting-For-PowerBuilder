using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;

namespace SecureLibrary
{
    [ComVisible(true)]
    [Guid("9E506401-739E-402D-A11F-C77E7768362B")]
    [ClassInterface(ClassInterfaceType.None)]
    public class EncryptionHelper
    {
        // this section for Symmetric Encryption with AES GCM mode
        public static string EncryptAesGcm(string plainText, string base64Key)
        {
            if (string.IsNullOrEmpty(plainText)) throw new ArgumentNullException(nameof(plainText));
            if (string.IsNullOrEmpty(base64Key)) throw new ArgumentNullException(nameof(base64Key));

            // Validate key length
            byte[] key; 
            try {
                key = Convert.FromBase64String(base64Key);
                if (key.Length != 32) // 256 bits
                    throw new ArgumentException("Invalid key length", nameof(base64Key));
            }
            catch (FormatException) {
                throw new ArgumentException("Invalid base64 key format", nameof(base64Key));
            }

            try {
                // Generate new nonce
                byte[] nonce = new byte[12];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(nonce);
                }
                string base64Nonce = Convert.ToBase64String(nonce);

                using (var aesGcm = new AesGcm(key, 16))
                {
                    byte[] encryptedData = new byte[plainText.Length];
                    byte[] tag = new byte[16]; // 128-bit tag
                    aesGcm.Encrypt(nonce, Encoding.UTF8.GetBytes(plainText), encryptedData, tag);
                    // Combine encrypted data and tag
                    byte[] combinedData = new byte[encryptedData.Length + tag.Length];
                    Array.Copy(encryptedData, 0, combinedData, 0, encryptedData.Length);
                    Array.Copy(tag, 0, combinedData, encryptedData.Length, tag.Length);
                    string encryptedBase64 = Convert.ToBase64String(combinedData);
                    
                    // Combine nonce and ciphertext
                    return base64Nonce + ":" + encryptedBase64;
                }
            }
            finally {
                // Clear sensitive data
                Array.Clear(key, 0, key.Length);
            }
        }

        public static string DecryptAesGcm(string combinedData, string base64Key)
        {
            if (string.IsNullOrEmpty(combinedData)) throw new ArgumentNullException(nameof(combinedData));
            if (string.IsNullOrEmpty(base64Key)) throw new ArgumentNullException(nameof(base64Key));

            // Split the combined data
            string[] parts = combinedData.Split(':');
            if (parts.Length != 2)  // Expect 2 parts: nonce:data+tag
                throw new ArgumentException("Invalid encrypted data format", nameof(combinedData));

            string base64Nonce = parts[0];
            string encryptedBase64 = parts[1];

            byte[] key = Convert.FromBase64String(base64Key);
            if (key.Length != 32) // 256 bits
                throw new ArgumentException("Invalid key length", nameof(base64Key));
            byte[] cipherText = Convert.FromBase64String(encryptedBase64);
            byte[] nonce = Convert.FromBase64String(base64Nonce);
            if (nonce.Length != 12) // 96 bits
                throw new ArgumentException("Invalid nonce length", nameof(base64Nonce));

            try {
                using (var aesGcm = new AesGcm(key, 16))
                {
                    // Split cipherText into encrypted data and tag
                    byte[] tag = new byte[16];
                    byte[] encryptedData = new byte[cipherText.Length - 16];
                    Array.Copy(cipherText, encryptedData, encryptedData.Length);
                    Array.Copy(cipherText, encryptedData.Length, tag, 0, tag.Length);
                    byte[] decryptedData = new byte[encryptedData.Length];
                    aesGcm.Decrypt(nonce, encryptedData, tag, decryptedData);
                    return Encoding.UTF8.GetString(decryptedData);
                }
            }
            finally {
                Array.Clear(key, 0, key.Length);
                Array.Clear(nonce, 0, nonce.Length);
                Array.Clear(cipherText, 0, cipherText.Length);
            }
        }

        // Symmetric Encryption with AES CBC mode
        [Obsolete("This method is deprecated because AES-CBC without an authentication mechanism is insecure. Please use EncryptAesGcm instead.")]
        public static string[] EncryptAesCbcWithIv(string plainText, string base64Key)
        {
            byte[] key = Convert.FromBase64String(base64Key);
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.GenerateIV(); // Generate IV
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] cipherText;
                using (var memoryStream = new MemoryStream())
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
                return [ base64CipherText, base64IV ];
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
                    using (var memoryStream = new MemoryStream(cipherText))
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                        {
                            using (var reader = new StreamReader(cryptoStream, Encoding.UTF8))
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
                string base64key = Convert.ToBase64String(aes.Key);
                aes.Clear();
                return base64key;
            }
        }


        // this section related about diffie hellman
        public static string[] GenerateDiffieHellmanKeys()
        {
            using (ECDiffieHellmanCng dh = new ECDiffieHellmanCng())
            {
                dh.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
                dh.HashAlgorithm = CngAlgorithm.Sha256;
                byte[] publicKey = dh.PublicKey.ExportSubjectPublicKeyInfo();
                byte[] privateKey = dh.Key.Export(CngKeyBlobFormat.EccPrivateBlob);
                return [
                    Convert.ToBase64String(publicKey),
                    Convert.ToBase64String(privateKey)
                ];
            }
        }
        public static string DeriveSharedKey(string otherPartyPublicKeyBase64, string privateKeyBase64)
        {
            byte[] otherPartyPublicKey = Convert.FromBase64String(otherPartyPublicKeyBase64);
            byte[] privateKey = Convert.FromBase64String(privateKeyBase64);
            
            using (ECDiffieHellmanCng dh = new ECDiffieHellmanCng(CngKey.Import(privateKey, CngKeyBlobFormat.EccPrivateBlob)))
            {
                dh.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
                dh.HashAlgorithm = CngAlgorithm.Sha256;
                
                using (var otherPartyKey = ECDiffieHellmanCng.Create())
                {
                    try
                    {
                        // Try importing as EccPublicBlob first (for .NET 4.8.1 format)
                        using (var importedKey = CngKey.Import(otherPartyPublicKey, CngKeyBlobFormat.EccPublicBlob))
                        {
                            return Convert.ToBase64String(dh.DeriveKeyMaterial(importedKey));
                        }
                    }
                    catch
                    {
                        // If that fails, try importing as SubjectPublicKeyInfo (for .NET 8 format)
                        otherPartyKey.ImportSubjectPublicKeyInfo(otherPartyPublicKey, out _);
                        return Convert.ToBase64String(dh.DeriveKeyMaterial(otherPartyKey.PublicKey));
                    }
                    finally {
                        Array.Clear(otherPartyPublicKey, 0, otherPartyPublicKey.Length);
                        Array.Clear(privateKey, 0, privateKey.Length);
                    }
                }
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
            if (string.IsNullOrEmpty(plainText)) throw new ArgumentNullException(nameof(plainText));
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));

            // Validate iteration count
            if (iterations < 1000 || iterations > 100000)
                throw new ArgumentException("Iteration count must be between 1000 and 100000", nameof(iterations));

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = EncryptAesGcmBytesWithPassword(plainBytes, password, null, iterations);
            
            // Clear sensitive data
            Array.Clear(plainBytes, 0, plainBytes.Length);
            
            return Convert.ToBase64String(encryptedBytes);
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
            if (string.IsNullOrEmpty(base64EncryptedData)) throw new ArgumentNullException(nameof(base64EncryptedData));
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));

            // Validate iteration count
            if (iterations < 1000 || iterations > 100000)
                throw new ArgumentException("Iteration count must be between 1000 and 100000", nameof(iterations));

            byte[] encryptedBytes = Convert.FromBase64String(base64EncryptedData);
            byte[] decryptedBytes = DecryptAesGcmBytesWithPassword(encryptedBytes, password, iterations);
            
            string result = Encoding.UTF8.GetString(decryptedBytes);
            
            // Clear sensitive data
            Array.Clear(encryptedBytes, 0, encryptedBytes.Length);
            Array.Clear(decryptedBytes, 0, decryptedBytes.Length);
            
            return result;
        }

        /// <summary>
        /// Encrypts byte array using AES-GCM with password-based key derivation
        /// </summary>
        /// <param name="plainData">Data to encrypt</param>
        /// <param name="password">Password for key derivation</param>
        /// <param name="salt">Salt for key derivation (optional, will generate if null)</param>
        /// <param name="iterations">PBKDF2 iteration count (default: 2000)</param>
        /// <returns>Encrypted data with salt, nonce, and tag</returns>
        public static byte[] EncryptAesGcmBytesWithPassword(byte[] plainData, string password, byte[]? salt = null, int iterations = 2000)
        {
            if (plainData == null) throw new ArgumentNullException(nameof(plainData));
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));

            // Generate salt if not provided
            if (salt == null)
            {
                salt = new byte[16];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(salt);
                }
            }

            // Derive 32-byte key from password
            byte[] key;
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                key = pbkdf2.GetBytes(32);
            }

            // Generate 12-byte nonce
            byte[] nonce = new byte[12];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(nonce);
            }

            byte[] encryptedData = EncryptAesGcmBytesWithKey(plainData, key, nonce);

            // Combine salt length (4 bytes) + salt + nonce + encrypted data for output
            byte[] result = new byte[4 + salt.Length + nonce.Length + encryptedData.Length];
            Buffer.BlockCopy(BitConverter.GetBytes(salt.Length), 0, result, 0, 4); // Store salt length in first 4 bytes
            Buffer.BlockCopy(salt, 0, result, 4, salt.Length);
            Buffer.BlockCopy(nonce, 0, result, 4 + salt.Length, nonce.Length);
            Buffer.BlockCopy(encryptedData, 0, result, 4 + salt.Length + nonce.Length, encryptedData.Length);

            // Clear sensitive data
            Array.Clear(key, 0, key.Length);
            Array.Clear(nonce, 0, nonce.Length);
            Array.Clear(encryptedData, 0, encryptedData.Length);

            return result;
        }

        /// <summary>
        /// Decrypts byte array using AES-GCM with password-based key derivation
        /// </summary>
        /// <param name="encryptedData">Encrypted data with salt, nonce, and tag</param>
        /// <param name="password">Password for key derivation</param>
        /// <param name="iterations">PBKDF2 iteration count (default: 2000)</param>
        /// <returns>Decrypted data</returns>
        public static byte[] DecryptAesGcmBytesWithPassword(byte[] encryptedData, string password, int iterations = 2000)
        {
            if (encryptedData == null) throw new ArgumentNullException(nameof(encryptedData));
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));

            const int nonceLength = 12;
            const int tagLength = 16;
            const int headerLength = 4;
            if (encryptedData.Length < headerLength + nonceLength + tagLength)
                throw new ArgumentException("Encrypted data too short", nameof(encryptedData));
            // Extract salt length from the header
            int saltLength = BitConverter.ToInt32(encryptedData, 0);
            if (saltLength <= 0 || encryptedData.Length < headerLength + saltLength + nonceLength + tagLength)
                throw new ArgumentException("Invalid salt length in encrypted data", nameof(encryptedData));
            byte[] salt = new byte[saltLength];
            byte[] nonce = new byte[nonceLength];
            byte[] cipherWithTag = new byte[encryptedData.Length - headerLength - saltLength - nonceLength];
            Buffer.BlockCopy(encryptedData, headerLength, salt, 0, saltLength);
            Buffer.BlockCopy(encryptedData, headerLength + saltLength, nonce, 0, nonceLength);
            Buffer.BlockCopy(encryptedData, headerLength + saltLength + nonceLength, cipherWithTag, 0, cipherWithTag.Length);
            // Derive key and decrypt
            byte[] key;
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                key = pbkdf2.GetBytes(32);
            }
            byte[] result = DecryptAesGcmBytesWithKey(cipherWithTag, key, nonce);
            
            // Clear sensitive data
            Array.Clear(salt, 0, salt.Length);
            Array.Clear(nonce, 0, nonce.Length);
            Array.Clear(cipherWithTag, 0, cipherWithTag.Length);
            Array.Clear(key, 0, key.Length);
            
            return result;
        }

        /// <summary>
        /// Encrypts byte array using AES-GCM with provided key and nonce
        /// </summary>
        /// <param name="plainData">Data to encrypt</param>
        /// <param name="key">32-byte encryption key</param>
        /// <param name="nonce">12-byte nonce</param>
        /// <returns>Encrypted data with authentication tag</returns>
        private static byte[] EncryptAesGcmBytesWithKey(byte[] plainData, byte[] key, byte[] nonce)
        {
            if (plainData == null) throw new ArgumentNullException(nameof(plainData));
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (nonce == null) throw new ArgumentNullException(nameof(nonce));

            if (key.Length != 32) throw new ArgumentException("Key must be 32 bytes", nameof(key));
            if (nonce.Length != 12) throw new ArgumentException("Nonce must be 12 bytes", nameof(nonce));

            using (var aesGcm = new AesGcm(key, 16))
            {
                byte[] encryptedData = new byte[plainData.Length];
                byte[] tag = new byte[16]; // 128-bit tag
                aesGcm.Encrypt(nonce, plainData, encryptedData, tag);
                
                // Combine encrypted data and tag
                byte[] result = new byte[encryptedData.Length + tag.Length];
                Buffer.BlockCopy(encryptedData, 0, result, 0, encryptedData.Length);
                Buffer.BlockCopy(tag, 0, result, encryptedData.Length, tag.Length);

                // Clear sensitive data
                Array.Clear(encryptedData, 0, encryptedData.Length);
                Array.Clear(tag, 0, tag.Length);

                return result;
            }
        }

        /// <summary>
        /// Decrypts byte array using AES-GCM with provided key and nonce
        /// </summary>
        /// <param name="cipherWithTag">Encrypted data with authentication tag</param>
        /// <param name="key">32-byte decryption key</param>
        /// <param name="nonce">12-byte nonce</param>
        /// <returns>Decrypted data</returns>
        private static byte[] DecryptAesGcmBytesWithKey(byte[] cipherWithTag, byte[] key, byte[] nonce)
        {
            if (cipherWithTag == null) throw new ArgumentNullException(nameof(cipherWithTag));
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (nonce == null) throw new ArgumentNullException(nameof(nonce));

            if (key.Length != 32) throw new ArgumentException("Key must be 32 bytes", nameof(key));
            if (nonce.Length != 12) throw new ArgumentException("Nonce must be 12 bytes", nameof(nonce));

            const int tagLength = 16;
            if (cipherWithTag.Length < tagLength)
                throw new ArgumentException("Encrypted data too short", nameof(cipherWithTag));

            using (var aesGcm = new AesGcm(key, 16))
            {
                // Split cipherWithTag into encrypted data and tag
                byte[] tag = new byte[tagLength];
                byte[] encryptedData = new byte[cipherWithTag.Length - tagLength];
                Buffer.BlockCopy(cipherWithTag, 0, encryptedData, 0, encryptedData.Length);
                Buffer.BlockCopy(cipherWithTag, encryptedData.Length, tag, 0, tag.Length);
                
                byte[] decryptedData = new byte[encryptedData.Length];
                aesGcm.Decrypt(nonce, encryptedData, tag, decryptedData);

                // Clear sensitive data
                Array.Clear(tag, 0, tag.Length);
                Array.Clear(encryptedData, 0, encryptedData.Length);

                return decryptedData;
            }
        }

        /// <summary>
        /// Generates a cryptographically secure random salt for key derivation
        /// </summary>
        /// <param name="saltLength">Length of salt in bytes (optional, default: 16)</param>
        /// <returns>Base64 encoded salt</returns>
        public static string GenerateSalt(int saltLength = 16)
        {
            if (saltLength < 8 || saltLength > 64)
                throw new ArgumentException("Salt length must be between 8 and 64 bytes", nameof(saltLength));

            byte[] salt = new byte[saltLength];
            using (var rng = RandomNumberGenerator.Create())
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
            if (string.IsNullOrEmpty(plainText)) throw new ArgumentNullException(nameof(plainText));
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));
            if (string.IsNullOrEmpty(base64Salt)) throw new ArgumentNullException(nameof(base64Salt));

            byte[] saltBytes = Convert.FromBase64String(base64Salt);
            
            // Validate salt length
            if (saltBytes.Length < 8 || saltBytes.Length > 64)
                throw new ArgumentException("Salt length must be between 8 and 64 bytes", nameof(base64Salt));

            // Validate iteration count
            if (iterations < 1000 || iterations > 100000)
                throw new ArgumentException("Iteration count must be between 1000 and 100000", nameof(iterations));

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = EncryptAesGcmBytesWithPassword(plainBytes, password, saltBytes, iterations);
            
            // Clear sensitive data
            Array.Clear(plainBytes, 0, plainBytes.Length);
            Array.Clear(saltBytes, 0, saltBytes.Length);
            
            return Convert.ToBase64String(encryptedBytes);
        }
    }
}
