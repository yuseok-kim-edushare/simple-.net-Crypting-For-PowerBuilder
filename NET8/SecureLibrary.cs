using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace SecureLibrary.Core
{
    [ComVisible(true)]
    [Guid("A6F7A6A9-953D-442A-8A5C-4A43F8D832E9")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface ISecureLibrary
    {
        string EncryptAesGcmWithPassword(string plainText, string password, int iterations = 200000);
        string DecryptAesGcmWithPassword(string base64EncryptedData, string password, int iterations = 200000);
        string DecryptAesGcmWithPasswordLegacy(string base64EncryptedData, string password, int iterations = 2000);
        string HashPassword(string password, int workFactor = 12);
        bool VerifyPassword(string password, string hashedPassword);
    }

    [ComVisible(true)]
    [Guid("D9B7A6A9-953D-442A-8A5C-4A43F8D832E9")]
    [ClassInterface(ClassInterfaceType.None)]
    public class SecureLibrary : ISecureLibrary
    {
        private const int SaltSize = 32; // Standard salt size (SQL Server compatible)
        private const int NonceSize = 12; // AES-GCM standard nonce size
        private const int TagSize = 16;   // AES-GCM standard tag size

        /// <summary>
        /// Encrypts string using AES-GCM with password-based key derivation (SQL Server Compatible).
        /// Uses a high iteration count suitable for modern systems.
        /// </summary>
        public string EncryptAesGcmWithPassword(string plainText, string password, int iterations = 200000)
        {
            if (string.IsNullOrEmpty(plainText)) throw new ArgumentNullException(nameof(plainText));
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));

            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] tag = new byte[TagSize];
            byte[] cipherText = new byte[plainBytes.Length];

            // Derive key from password
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                byte[] key = pbkdf2.GetBytes(32); // 256-bit key

                // Encrypt using AesGcm
                using (var aesGcm = new AesGcm(key, TagSize))
                {
                    aesGcm.Encrypt(nonce, plainBytes, cipherText, tag);
                }
                Array.Clear(key, 0, key.Length);
            }

            // Combine: salt + nonce + ciphertext + tag
            byte[] result = new byte[SaltSize + NonceSize + cipherText.Length + TagSize];
            Buffer.BlockCopy(salt, 0, result, 0, SaltSize);
            Buffer.BlockCopy(nonce, 0, result, SaltSize, NonceSize);
            Buffer.BlockCopy(cipherText, 0, result, SaltSize + NonceSize, cipherText.Length);
            Buffer.BlockCopy(tag, 0, result, SaltSize + NonceSize + cipherText.Length, TagSize);

            return Convert.ToBase64String(result);
        }

        /// <summary>
        /// Decrypts string using AES-GCM with password-based key derivation (SQL Server Compatible).
        /// </summary>
        public string DecryptAesGcmWithPassword(string base64EncryptedData, string password, int iterations = 200000)
        {
            if (string.IsNullOrEmpty(base64EncryptedData)) throw new ArgumentNullException(nameof(base64EncryptedData));
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));

            byte[] encryptedData = Convert.FromBase64String(base64EncryptedData);

            if (encryptedData.Length < SaltSize + NonceSize + TagSize)
                throw new ArgumentException("Encrypted data is too short.", nameof(base64EncryptedData));

            // Extract components
            byte[] salt = new byte[SaltSize];
            byte[] nonce = new byte[NonceSize];
            byte[] tag = new byte[TagSize];
            byte[] cipherText = new byte[encryptedData.Length - SaltSize - NonceSize - TagSize];

            Buffer.BlockCopy(encryptedData, 0, salt, 0, SaltSize);
            Buffer.BlockCopy(encryptedData, SaltSize, nonce, 0, NonceSize);
            Buffer.BlockCopy(encryptedData, encryptedData.Length - TagSize, tag, 0, TagSize);
            Buffer.BlockCopy(encryptedData, SaltSize + NonceSize, cipherText, 0, cipherText.Length);

            byte[] decryptedBytes = new byte[cipherText.Length];

            // Derive key from password
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                byte[] key = pbkdf2.GetBytes(32);

                // Decrypt using AesGcm
                using (var aesGcm = new AesGcm(key, TagSize))
                {
                    aesGcm.Decrypt(nonce, cipherText, tag, decryptedBytes);
                }
                Array.Clear(key, 0, key.Length);
            }

            return Encoding.UTF8.GetString(decryptedBytes);
        }

        /// <summary>
        /// Decrypts string using the legacy format (from net481PB).
        /// </summary>
        public string DecryptAesGcmWithPasswordLegacy(string base64EncryptedData, string password, int iterations = 2000)
        {
            if (string.IsNullOrEmpty(base64EncryptedData)) throw new ArgumentNullException(nameof(base64EncryptedData));
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));

            byte[] encryptedData = Convert.FromBase64String(base64EncryptedData);

            const int headerLength = 4;
            if (encryptedData.Length < headerLength + NonceSize + TagSize)
                throw new ArgumentException("Legacy encrypted data is too short.", nameof(base64EncryptedData));

            // Extract legacy components
            int saltLength = BitConverter.ToInt32(encryptedData, 0);
            if (saltLength <= 0 || encryptedData.Length < headerLength + saltLength + NonceSize + TagSize)
                throw new ArgumentException("Invalid salt length in legacy encrypted data.", nameof(base64EncryptedData));

            byte[] salt = new byte[saltLength];
            byte[] nonce = new byte[NonceSize];
            byte[] cipherWithTag = new byte[encryptedData.Length - headerLength - saltLength - NonceSize];

            Buffer.BlockCopy(encryptedData, headerLength, salt, 0, saltLength);
            Buffer.BlockCopy(encryptedData, headerLength + saltLength, nonce, 0, NonceSize);
            Buffer.BlockCopy(encryptedData, headerLength + saltLength + NonceSize, cipherWithTag, 0, cipherWithTag.Length);

            // Separate ciphertext and tag
            byte[] tag = new byte[TagSize];
            byte[] cipherText = new byte[cipherWithTag.Length - TagSize];
            Buffer.BlockCopy(cipherWithTag, cipherWithTag.Length - TagSize, tag, 0, TagSize);
            Buffer.BlockCopy(cipherWithTag, 0, cipherText, 0, cipherText.Length);

            byte[] decryptedBytes = new byte[cipherText.Length];

            // Derive key and decrypt
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                byte[] key = pbkdf2.GetBytes(32);
                using (var aesGcm = new AesGcm(key, TagSize))
                {
                    aesGcm.Decrypt(nonce, cipherText, tag, decryptedBytes);
                }
                Array.Clear(key, 0, key.Length);
            }

            return Encoding.UTF8.GetString(decryptedBytes);
        }

        /// <summary>
        /// Hashes a password using BCrypt.Net.
        /// </summary>
        public string HashPassword(string password, int workFactor = 12)
        {
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor);
        }

        /// <summary>
        /// Verifies a password against a BCrypt hash.
        /// </summary>
        public bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));
            if (string.IsNullOrEmpty(hashedPassword)) throw new ArgumentNullException(nameof(hashedPassword));
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
    }
}
