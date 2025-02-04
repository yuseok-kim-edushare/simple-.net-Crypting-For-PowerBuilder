using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using System.Security;

namespace SecureLibrary
{
    internal static class KeyManagement
    {
        private const int KEY_ROTATION_INTERVAL_DAYS = 30;
        private const int AES_KEY_SIZE_BYTES = 32;
        private static readonly object _lock = new object();

        public static bool ValidateKey(string base64Key, out string error)
        {
            try
            {
                if (string.IsNullOrEmpty(base64Key))
                {
                    error = "Key cannot be null or empty";
                    return false;
                }

                byte[] key = Convert.FromBase64String(base64Key);
                if (key.Length != AES_KEY_SIZE_BYTES)
                {
                    error = $"Key must be {AES_KEY_SIZE_BYTES} bytes";
                    return false;
                }

                error = null;
                return true;
            }
            catch (FormatException)
            {
                error = "Invalid Base64 format";
                return false;
            }
            finally
            {
                // Clear sensitive data from memory
                GC.Collect();
            }
        }

        public static string GenerateSecureKey()
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.GenerateKey();
                return Convert.ToBase64String(aes.Key);
            }
        }

        public static void SecureErase(byte[] sensitiveData)
        {
            if (sensitiveData == null) return;
            
            lock (_lock)
            {
                Array.Clear(sensitiveData, 0, sensitiveData.Length);
            }
        }
    }

    public class CryptographicOperationException : SecurityException
    {
        public CryptographicOperationException(string message) : base(message) { }
        public CryptographicOperationException(string message, Exception inner) : base(message, inner) { }
    }

    internal static class CryptoLogger
    {
        private static readonly object _logLock = new object();
        private const int MAX_FAILED_ATTEMPTS = 5;
        private static readonly Dictionary<string, int> _failedAttempts = new Dictionary<string, int>();
        private static readonly Dictionary<string, DateTime> _lastFailure = new Dictionary<string, DateTime>();

        public static void LogOperation(string operation, bool success, string details = null)
        {
            var logEntry = new
            {
                Timestamp = DateTime.UtcNow,
                Operation = operation,
                Success = success,
                Details = details,
                ThreadId = Environment.CurrentManagedThreadId
            };

            lock (_logLock)
            {
                // In production, this should write to a secure log file or service
                System.Diagnostics.Debug.WriteLine($"[{logEntry.Timestamp:yyyy-MM-dd HH:mm:ss}] {logEntry.Operation} - Success: {logEntry.Success} {(details != null ? $"- {details}" : "")}");
            }
        }

        public static bool CheckFailedAttempts(string operation)
        {
            lock (_logLock)
            {
                if (!_failedAttempts.ContainsKey(operation))
                {
                    _failedAttempts[operation] = 0;
                    return true;
                }

                if (_failedAttempts[operation] >= MAX_FAILED_ATTEMPTS)
                {
                    if (_lastFailure.ContainsKey(operation))
                    {
                        var timeSinceLastFailure = DateTime.UtcNow - _lastFailure[operation];
                        if (timeSinceLastFailure.TotalMinutes < 15)
                        {
                            return false;
                        }
                        _failedAttempts[operation] = 0;
                    }
                }

                return true;
            }
        }

        public static void RecordFailedAttempt(string operation)
        {
            lock (_logLock)
            {
                if (!_failedAttempts.ContainsKey(operation))
                {
                    _failedAttempts[operation] = 0;
                }
                _failedAttempts[operation]++;
                _lastFailure[operation] = DateTime.UtcNow;
            }
        }
    }

    [ComVisible(true)]
    [Guid("9E506401-739E-402D-A11F-C77E7768362B")]
    [ClassInterface(ClassInterfaceType.None)]
    public class EncryptionHelper
    {
        // this section for Symmetric Encryption with AES GCM mode
        public static string EncryptAesGcm(string plainText, string base64Key)
        {
            const string OPERATION = "AES_GCM_ENCRYPT";
            
            try
            {
                if (!CryptoLogger.CheckFailedAttempts(OPERATION))
                {
                    throw new CryptographicOperationException("Too many failed encryption attempts. Please try again later.");
                }

                if (plainText == null)
                    throw new ArgumentNullException(nameof(plainText));

                if (!KeyManagement.ValidateKey(base64Key, out string error))
                {
                    CryptoLogger.RecordFailedAttempt(OPERATION);
                    throw new CryptographicOperationException($"Invalid key: {error}");
                }

                byte[] key = Convert.FromBase64String(base64Key);
                try
                {
                    // Generate new nonce
                    byte[] nonce = new byte[12];
                    using (var rng = RandomNumberGenerator.Create())
                    {
                        rng.GetBytes(nonce);
                    }
                    string base64Nonce = Convert.ToBase64String(nonce);

                    using (var aesGcm = new AesGcm(key, 16))
                    {
                        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                        byte[] ciphertext = new byte[plainBytes.Length];
                        byte[] tag = new byte[16];

                        aesGcm.Encrypt(nonce, plainBytes, ciphertext, tag);

                        // Combine ciphertext and tag for backwards compatibility
                        byte[] encryptedData = Combine(ciphertext, tag);
                        string encryptedBase64 = Convert.ToBase64String(encryptedData);

                        // Return in backwards-compatible format
                        string result = base64Nonce + ":" + encryptedBase64;
                        CryptoLogger.LogOperation(OPERATION, true);
                        return result;
                    }
                }
                finally
                {
                    KeyManagement.SecureErase(key);
                }
            }
            catch (Exception ex) when (ex is not CryptographicOperationException)
            {
                CryptoLogger.RecordFailedAttempt(OPERATION);
                CryptoLogger.LogOperation(OPERATION, false, ex.Message);
                throw new CryptographicOperationException("Encryption failed", ex);
            }
        }

        public static string DecryptAesGcm(string combinedData, string base64Key)
        {
            const string OPERATION = "AES_GCM_DECRYPT";
            
            try
            {
                if (!CryptoLogger.CheckFailedAttempts(OPERATION))
                {
                    throw new CryptographicOperationException("Too many failed decryption attempts. Please try again later.");
                }

                if (string.IsNullOrEmpty(combinedData))
                    throw new ArgumentNullException(nameof(combinedData));

                if (!KeyManagement.ValidateKey(base64Key, out string error))
                {
                    CryptoLogger.RecordFailedAttempt(OPERATION);
                    throw new CryptographicOperationException($"Invalid key: {error}");
                }

                // Split the combined data
                string[] parts = combinedData.Split(':');
                if (parts.Length != 2)
                    throw new ArgumentException("Invalid encrypted data format", nameof(combinedData));

                string base64Nonce = parts[0];
                string encryptedBase64 = parts[1];

                byte[] key = Convert.FromBase64String(base64Key);
                try
                {
                    byte[] nonce = Convert.FromBase64String(base64Nonce);
                    byte[] encryptedData = Convert.FromBase64String(encryptedBase64);

                    if (encryptedData.Length < 16) // Minimum tag length
                    {
                        throw new CryptographicOperationException("Invalid encrypted data length");
                    }

                    // Separate ciphertext and tag
                    int ciphertextLength = encryptedData.Length - 16; // subtract tag length
                    byte[] ciphertext = new byte[ciphertextLength];
                    byte[] tag = new byte[16];
                    Buffer.BlockCopy(encryptedData, 0, ciphertext, 0, ciphertextLength);
                    Buffer.BlockCopy(encryptedData, ciphertextLength, tag, 0, 16);

                    using (var aesGcm = new AesGcm(key, 16))
                    {
                        byte[] plaintext = new byte[ciphertextLength];
                        aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
                        
                        string result = Encoding.UTF8.GetString(plaintext);
                        CryptoLogger.LogOperation(OPERATION, true);
                        return result;
                    }
                }
                finally
                {
                    KeyManagement.SecureErase(key);
                }
            }
            catch (Exception ex) when (ex is not CryptographicOperationException)
            {
                CryptoLogger.RecordFailedAttempt(OPERATION);
                CryptoLogger.LogOperation(OPERATION, false, ex.Message);
                throw new CryptographicOperationException("Decryption failed", ex);
            }
        }

        // Symmetric Encryption with AES CBC mode
        public static string[] EncryptAesCbcWithIv(string plainText, string base64Key)
        {
            const string OPERATION = "AES_CBC_ENCRYPT";
            try
            {
                if (!CryptoLogger.CheckFailedAttempts(OPERATION))
                {
                    throw new CryptographicOperationException("Too many failed encryption attempts. Please try again later.");
                }

                if (plainText == null)
                    throw new ArgumentNullException(nameof(plainText));

                if (!KeyManagement.ValidateKey(base64Key, out string error))
                {
                    CryptoLogger.RecordFailedAttempt(OPERATION);
                    throw new CryptographicOperationException($"Invalid key: {error}");
                }

                byte[] key = Convert.FromBase64String(base64Key);
                try
                {
                    using (Aes aes = Aes.Create())
                    {
                        aes.Key = key;
                        // Generate cryptographically secure IV
                        using (var rng = RandomNumberGenerator.Create())
                        {
                            aes.IV = new byte[16];
                            rng.GetBytes(aes.IV);
                        }
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

                        CryptoLogger.LogOperation(OPERATION, true);
                        return [ base64CipherText, base64IV ];
                    }
                }
                finally
                {
                    KeyManagement.SecureErase(key);
                }
            }
            catch (Exception ex) when (ex is not CryptographicOperationException)
            {
                CryptoLogger.RecordFailedAttempt(OPERATION);
                CryptoLogger.LogOperation(OPERATION, false, ex.Message);
                throw new CryptographicOperationException("CBC encryption failed", ex);
            }
        }

        public static string DecryptAesCbcWithIv(string base64CipherText, string base64Key, string base64IV)
        {
            const string OPERATION = "AES_CBC_DECRYPT";
            try
            {
                if (!CryptoLogger.CheckFailedAttempts(OPERATION))
                {
                    throw new CryptographicOperationException("Too many failed decryption attempts. Please try again later.");
                }

                if (string.IsNullOrEmpty(base64CipherText))
                    throw new ArgumentNullException(nameof(base64CipherText));
                if (string.IsNullOrEmpty(base64IV))
                    throw new ArgumentNullException(nameof(base64IV));

                if (!KeyManagement.ValidateKey(base64Key, out string error))
                {
                    CryptoLogger.RecordFailedAttempt(OPERATION);
                    throw new CryptographicOperationException($"Invalid key: {error}");
                }

                byte[] key = Convert.FromBase64String(base64Key);
                byte[] iv = null;
                try
                {
                    byte[] cipherText = Convert.FromBase64String(base64CipherText);
                    iv = Convert.FromBase64String(base64IV);

                    if (iv.Length != 16)
                    {
                        throw new CryptographicOperationException("Invalid IV length");
                    }

                    using (Aes aes = Aes.Create())
                    {
                        aes.Key = key;
                        aes.IV = iv;
                        aes.Mode = CipherMode.CBC;
                        aes.Padding = PaddingMode.PKCS7;

                        using (var memoryStream = new MemoryStream(cipherText))
                        using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                        using (var reader = new StreamReader(cryptoStream, Encoding.UTF8))
                        {
                            string result = reader.ReadToEnd();
                            CryptoLogger.LogOperation(OPERATION, true);
                            return result;
                        }
                    }
                }
                finally
                {
                    KeyManagement.SecureErase(key);
                    if (iv != null) KeyManagement.SecureErase(iv);
                }
            }
            catch (Exception ex) when (ex is not CryptographicOperationException)
            {
                CryptoLogger.RecordFailedAttempt(OPERATION);
                CryptoLogger.LogOperation(OPERATION, false, ex.Message);
                throw new CryptographicOperationException("CBC decryption failed", ex);
            }
        }

        public static string KeyGenAES256()
        {
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.GenerateKey();
                string base64key = Convert.ToBase64String(aes.Key);
                return base64key;
            }
        }

        // this section related about diffie hellman
        public static string[] GenerateDiffieHellmanKeys()
        {
            const string OPERATION = "GENERATE_DH_KEYS";
            try
            {
                if (!CryptoLogger.CheckFailedAttempts(OPERATION))
                {
                    throw new CryptographicOperationException("Too many failed key generation attempts. Please try again later.");
                }

                using (ECDiffieHellmanCng dh = new ECDiffieHellmanCng())
                {
                    dh.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
                    dh.HashAlgorithm = CngAlgorithm.Sha256;
                    
                    byte[] publicKey = dh.PublicKey.ExportSubjectPublicKeyInfo();
                    byte[] privateKey = dh.Key.Export(CngKeyBlobFormat.EccPrivateBlob);
                    
                    string[] result = [
                        Convert.ToBase64String(publicKey),
                        Convert.ToBase64String(privateKey)
                    ];
                    
                    CryptoLogger.LogOperation(OPERATION, true);
                    return result;
                }
            }
            catch (Exception ex)
            {
                CryptoLogger.RecordFailedAttempt(OPERATION);
                CryptoLogger.LogOperation(OPERATION, false, ex.Message);
                throw new CryptographicOperationException("Key generation failed", ex);
            }
        }

        public static string DeriveSharedKey(string otherPartyPublicKeyBase64, string privateKeyBase64)
        {
            const string OPERATION = "DERIVE_SHARED_KEY";
            try
            {
                if (!CryptoLogger.CheckFailedAttempts(OPERATION))
                {
                    throw new CryptographicOperationException("Too many failed key derivation attempts. Please try again later.");
                }

                if (string.IsNullOrEmpty(otherPartyPublicKeyBase64))
                    throw new ArgumentNullException(nameof(otherPartyPublicKeyBase64));
                if (string.IsNullOrEmpty(privateKeyBase64))
                    throw new ArgumentNullException(nameof(privateKeyBase64));

                byte[] otherPartyPublicKey = null;
                byte[] privateKey = null;

                try
                {
                    otherPartyPublicKey = Convert.FromBase64String(otherPartyPublicKeyBase64);
                    privateKey = Convert.FromBase64String(privateKeyBase64);

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
                                    string result = Convert.ToBase64String(dh.DeriveKeyMaterial(importedKey));
                                    CryptoLogger.LogOperation(OPERATION, true);
                                    return result;
                                }
                            }
                            catch
                            {
                                // If that fails, try importing as SubjectPublicKeyInfo (for .NET 8 format)
                                otherPartyKey.ImportSubjectPublicKeyInfo(otherPartyPublicKey, out _);
                                string result = Convert.ToBase64String(dh.DeriveKeyMaterial(otherPartyKey.PublicKey));
                                CryptoLogger.LogOperation(OPERATION, true);
                                return result;
                            }
                        }
                    }
                }
                finally
                {
                    if (privateKey != null) KeyManagement.SecureErase(privateKey);
                }
            }
            catch (Exception ex) when (ex is not CryptographicOperationException)
            {
                CryptoLogger.RecordFailedAttempt(OPERATION);
                CryptoLogger.LogOperation(OPERATION, false, ex.Message);
                throw new CryptographicOperationException("Key derivation failed", ex);
            }
        }

        // this is common byte combine method for AES and DH implementation
        private static byte[] Combine(byte[] first, byte[] second)
        {
            byte[] combined = new byte[first.Length + second.Length];
            Array.Copy(first, combined, first.Length);
            Array.Copy(second, 0, combined, first.Length, second.Length);
            return combined;
        }
        // this section related about bcrypt
        public static string BcryptEncoding(string password)
        {
            const string OPERATION = "BCRYPT_HASH";
            try
            {
                if (!CryptoLogger.CheckFailedAttempts(OPERATION))
                {
                    throw new CryptographicOperationException("Too many failed hashing attempts. Please try again later.");
                }

                if (string.IsNullOrEmpty(password))
                    throw new ArgumentNullException(nameof(password));

                if (password.Length < 8)
                    throw new ArgumentException("Password must be at least 8 characters long", nameof(password));

                // Use a work factor of 12 for better security (increased from 10)
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, 12);
                CryptoLogger.LogOperation(OPERATION, true);
                return hashedPassword;
            }
            catch (Exception ex) when (ex is not CryptographicOperationException)
            {
                CryptoLogger.RecordFailedAttempt(OPERATION);
                CryptoLogger.LogOperation(OPERATION, false, ex.Message);
                throw new CryptographicOperationException("Password hashing failed", ex);
            }
        }
        public static bool VerifyBcryptPassword(string password, string hashedPassword)
        {
            const string OPERATION = "BCRYPT_VERIFY";
            try
            {
                if (!CryptoLogger.CheckFailedAttempts(OPERATION))
                {
                    throw new CryptographicOperationException("Too many failed verification attempts. Please try again later.");
                }

                if (string.IsNullOrEmpty(password))
                    throw new ArgumentNullException(nameof(password));
                if (string.IsNullOrEmpty(hashedPassword))
                    throw new ArgumentNullException(nameof(hashedPassword));

                bool result = BCrypt.Net.BCrypt.Verify(password, hashedPassword);
                
                if (!result)
                {
                    CryptoLogger.RecordFailedAttempt(OPERATION);
                    CryptoLogger.LogOperation(OPERATION, false, "Invalid password");
                }
                else
                {
                    CryptoLogger.LogOperation(OPERATION, true);
                }
                
                return result;
            }
            catch (Exception ex) when (ex is not CryptographicOperationException)
            {
                CryptoLogger.RecordFailedAttempt(OPERATION);
                CryptoLogger.LogOperation(OPERATION, false, ex.Message);
                throw new CryptographicOperationException("Password verification failed", ex);
            }
        }
    }

    internal class KeyRotationManager
    {
        private const int DEFAULT_ROTATION_DAYS = 30;
        private static readonly object _lock = new object();
        private static readonly Dictionary<string, KeyInfo> _keyStore = new();

        private class KeyInfo
        {
            public required string Key { get; set; }
            public required DateTime CreationDate { get; set; }
            public required DateTime ExpirationDate { get; set; }
            public required bool IsActive { get; set; }
        }

        public static string GetCurrentKey(string keyId)
        {
            ArgumentNullException.ThrowIfNull(keyId);

            lock (_lock)
            {
                if (!_keyStore.ContainsKey(keyId))
                {
                    // Generate new key if not exists
                    _keyStore[keyId] = GenerateNewKeyInfo();
                }

                var keyInfo = _keyStore[keyId];
                if (DateTime.UtcNow >= keyInfo.ExpirationDate)
                {
                    // Key has expired, generate new one
                    _keyStore[keyId] = GenerateNewKeyInfo();
                    keyInfo = _keyStore[keyId];
                    CryptoLogger.LogOperation("KEY_ROTATION", true, $"Rotated key: {keyId}");
                }

                return keyInfo.Key;
            }
        }

        public static void RegisterKey(string keyId, string base64Key, int rotationDays = DEFAULT_ROTATION_DAYS)
        {
            ArgumentNullException.ThrowIfNull(keyId);
            ArgumentNullException.ThrowIfNull(base64Key);

            if (!KeyManagement.ValidateKey(base64Key, out string error))
            {
                throw new CryptographicOperationException($"Invalid key for registration: {error}");
            }

            lock (_lock)
            {
                var now = DateTime.UtcNow;
                _keyStore[keyId] = new KeyInfo
                {
                    Key = base64Key,
                    CreationDate = now,
                    ExpirationDate = now.AddDays(rotationDays),
                    IsActive = true
                };
                CryptoLogger.LogOperation("KEY_REGISTRATION", true, $"Registered key: {keyId}");
            }
        }

        private static KeyInfo GenerateNewKeyInfo(int rotationDays = DEFAULT_ROTATION_DAYS)
        {
            var now = DateTime.UtcNow;
            return new KeyInfo
            {
                Key = KeyManagement.GenerateSecureKey(),
                CreationDate = now,
                ExpirationDate = now.AddDays(rotationDays),
                IsActive = true
            };
        }

        public static void DeactivateKey(string keyId)
        {
            ArgumentNullException.ThrowIfNull(keyId);

            lock (_lock)
            {
                if (_keyStore.ContainsKey(keyId))
                {
                    _keyStore[keyId].IsActive = false;
                    CryptoLogger.LogOperation("KEY_DEACTIVATION", true, $"Deactivated key: {keyId}");
                }
            }
        }

        public static bool IsKeyActive(string keyId)
        {
            ArgumentNullException.ThrowIfNull(keyId);

            lock (_lock)
            {
                return _keyStore.ContainsKey(keyId) && _keyStore[keyId].IsActive;
            }
        }

        public static void CleanupExpiredKeys()
        {
            lock (_lock)
            {
                var expiredKeys = _keyStore
                    .Where(kv => DateTime.UtcNow >= kv.Value.ExpirationDate)
                    .Select(kv => kv.Key)
                    .ToList();

                foreach (var keyId in expiredKeys)
                {
                    _keyStore.Remove(keyId);
                    CryptoLogger.LogOperation("KEY_CLEANUP", true, $"Removed expired key: {keyId}");
                }
            }
        }
    }
}

