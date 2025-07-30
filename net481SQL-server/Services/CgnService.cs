using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using SecureLibrary.SQL.Interfaces;

namespace SecureLibrary.SQL.Services
{
    /// <summary>
    /// Thread-safe implementation of Windows CGN (Cryptographic Next Generation) API wrapper
    /// Provides high-level methods for cryptographic operations using native Windows APIs
    /// </summary>
    [SecuritySafeCritical]
    public class CgnService : ICgnService, IDisposable
    {
        private static readonly object _lockObject = new object();
        private bool _disposed = false;

        // Windows CGN API constants
        private const string BCRYPT_AES_ALGORITHM = "AES";
        private const string BCRYPT_CHAINING_MODE = "ChainingMode";
        private const string BCRYPT_CHAIN_MODE_GCM = "ChainingModeGCM";
        private const int STATUS_SUCCESS = 0;
        private const int BCRYPT_ALG_HANDLE_HMAC_FLAG = 0x00000008;

        // P/Invoke declarations for Windows CGN API
        [DllImport("bcrypt.dll")]
        private static extern int BCryptOpenAlgorithmProvider(
            out IntPtr phAlgorithm,
            [MarshalAs(UnmanagedType.LPWStr)] string pszAlgId,
            [MarshalAs(UnmanagedType.LPWStr)] string pszImplementation,
            uint dwFlags);

        [DllImport("bcrypt.dll")]
        private static extern int BCryptSetProperty(
            IntPtr hObject,
            [MarshalAs(UnmanagedType.LPWStr)] string pszProperty,
            byte[] pbInput,
            int cbInput,
            int dwFlags);

        [DllImport("bcrypt.dll")]
        private static extern int BCryptGenerateSymmetricKey(
            IntPtr hAlgorithm,
            out IntPtr phKey,
            IntPtr pbKeyObject,
            int cbKeyObject,
            byte[] pbSecret,
            int cbSecret,
            int dwFlags);

        [DllImport("bcrypt.dll")]
        private static extern int BCryptEncrypt(
            IntPtr hKey,
            byte[] pbInput,
            int cbInput,
            ref BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO pPaddingInfo,
            byte[] pbIV,
            int cbIV,
            byte[] pbOutput,
            int cbOutput,
            out int pcbResult,
            int dwFlags);

        [DllImport("bcrypt.dll")]
        private static extern int BCryptDecrypt(
            IntPtr hKey,
            byte[] pbInput,
            int cbInput,
            ref BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO pPaddingInfo,
            byte[] pbIV,
            int cbIV,
            byte[] pbOutput,
            int cbOutput,
            out int pcbResult,
            int dwFlags);

        [DllImport("bcrypt.dll")]
        private static extern int BCryptDestroyKey(IntPtr hKey);

        [DllImport("bcrypt.dll")]
        private static extern int BCryptCloseAlgorithmProvider(IntPtr hAlgorithm, int dwFlags);

        [DllImport("bcrypt.dll")]
        private static extern int BCryptGenRandom(
            IntPtr hAlgorithm,
            byte[] pbBuffer,
            int cbBuffer,
            int dwFlags);

        // CGN API structures
        [StructLayout(LayoutKind.Sequential)]
        private struct BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO
        {
            public int cbSize;
            public int dwInfoVersion;
            public IntPtr pbNonce;
            public int cbNonce;
            public IntPtr pbAuthData;
            public int cbAuthData;
            public IntPtr pbTag;
            public int cbTag;
            public IntPtr pbMacContext;
            public int cbMacContext;
            public int cbAAD;
            public long cbData;
            public int dwFlags;

            public static BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO Initialize()
            {
                return new BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO
                {
                    cbSize = Marshal.SizeOf(typeof(BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO)),
                    dwInfoVersion = 1
                };
            }
        }

        /// <summary>
        /// Invokes a CGN operation with the provided input data
        /// </summary>
        /// <param name="inputData">Input data for the CGN operation</param>
        /// <param name="operationType">Type of CGN operation to perform</param>
        /// <returns>Result of the CGN operation</returns>
        /// <exception cref="CryptographicException">Thrown when CGN operation fails</exception>
        public byte[] InvokeCgnOperation(byte[] inputData, CgnOperationType operationType)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CgnService));

            if (inputData == null)
                throw new ArgumentNullException(nameof(inputData));

            lock (_lockObject)
            {
                try
                {
                    switch (operationType)
                    {
                        case CgnOperationType.AesGcmEncrypt:
                            return PerformAesGcmEncryption(inputData);
                        case CgnOperationType.AesGcmDecrypt:
                            return PerformAesGcmDecryption(inputData);
                        case CgnOperationType.KeyGeneration:
                            return GenerateKey(inputData.Length * 8); // Convert bytes to bits
                        case CgnOperationType.RandomGeneration:
                            return GenerateNonce(inputData.Length);
                        case CgnOperationType.KeyDerivation:
                            return PerformKeyDerivation(inputData);
                        default:
                            throw new ArgumentException($"Unsupported CGN operation type: {operationType}", nameof(operationType));
                    }
                }
                catch (Exception ex)
                {
                    throw new CryptographicException($"CGN operation failed: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// Generates a cryptographically secure random key
        /// </summary>
        /// <param name="keySize">Size of the key in bits</param>
        /// <returns>Generated key as byte array</returns>
        public byte[] GenerateKey(int keySize)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CgnService));

            if (keySize <= 0 || keySize % 8 != 0)
                throw new ArgumentException("Key size must be positive and divisible by 8", nameof(keySize));

            lock (_lockObject)
            {
                byte[] key = new byte[keySize / 8];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(key);
                }
                return key;
            }
        }

        /// <summary>
        /// Generates a cryptographically secure random nonce/IV
        /// </summary>
        /// <param name="nonceSize">Size of the nonce in bytes</param>
        /// <returns>Generated nonce as byte array</returns>
        public byte[] GenerateNonce(int nonceSize)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CgnService));

            if (nonceSize <= 0)
                throw new ArgumentException("Nonce size must be positive", nameof(nonceSize));

            lock (_lockObject)
            {
                byte[] nonce = new byte[nonceSize];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(nonce);
                }
                return nonce;
            }
        }

        /// <summary>
        /// Performs AES-GCM encryption using CGN API
        /// </summary>
        /// <param name="plainData">Data to encrypt</param>
        /// <param name="key">Encryption key</param>
        /// <param name="nonce">Nonce for GCM mode</param>
        /// <returns>Encrypted data with authentication tag</returns>
        [SecuritySafeCritical]
        public byte[] EncryptAesGcm(byte[] plainData, byte[] key, byte[] nonce)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CgnService));

            if (plainData == null) throw new ArgumentNullException(nameof(plainData));
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (nonce == null) throw new ArgumentNullException(nameof(nonce));

            if (key.Length != 32) throw new ArgumentException("Key must be 32 bytes", nameof(key));
            if (nonce.Length != 12) throw new ArgumentException("Nonce must be 12 bytes", nameof(nonce));

            return ProtectedMemoryHelper.ExecuteWithProtection(new byte[][] { key, nonce }, (protectedArrays) =>
            {
                lock (_lockObject)
                {
                    IntPtr hAlg = IntPtr.Zero;
                    IntPtr hKey = IntPtr.Zero;

                    try
                    {
                        // Initialize algorithm provider
                        int status = BCryptOpenAlgorithmProvider(out hAlg, BCRYPT_AES_ALGORITHM, null, 0);
                        if (status != STATUS_SUCCESS) 
                            throw new CryptographicException($"BCryptOpenAlgorithmProvider failed with status {status}");

                        // Set GCM mode
                        status = BCryptSetProperty(hAlg, BCRYPT_CHAINING_MODE, 
                            Encoding.Unicode.GetBytes(BCRYPT_CHAIN_MODE_GCM), 
                            Encoding.Unicode.GetBytes(BCRYPT_CHAIN_MODE_GCM).Length, 0);
                        if (status != STATUS_SUCCESS) 
                            throw new CryptographicException($"BCryptSetProperty failed with status {status}");

                        // Generate key
                        status = BCryptGenerateSymmetricKey(hAlg, out hKey, IntPtr.Zero, 0, protectedArrays[0], protectedArrays[0].Length, 0);
                        if (status != STATUS_SUCCESS) 
                            throw new CryptographicException($"BCryptGenerateSymmetricKey failed with status {status}");

                        const int tagLength = 16;  // GCM tag length

                        var authInfo = BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO.Initialize();
                        var nonceHandle = GCHandle.Alloc(protectedArrays[1], GCHandleType.Pinned);
                        var tagBuffer = new byte[tagLength];
                        var tagHandle = GCHandle.Alloc(tagBuffer, GCHandleType.Pinned);

                        try
                        {
                            authInfo.pbNonce = nonceHandle.AddrOfPinnedObject();
                            authInfo.cbNonce = protectedArrays[1].Length;
                            authInfo.pbTag = tagHandle.AddrOfPinnedObject();
                            authInfo.cbTag = tagLength;

                            // Get required size
                            int cipherLength;
                            status = BCryptEncrypt(hKey, plainData, plainData.Length, ref authInfo,
                                null, 0, null, 0, out cipherLength, 0);
                            if (status != STATUS_SUCCESS) 
                                throw new CryptographicException($"BCryptEncrypt size failed with status {status}");

                            byte[] cipherText = new byte[cipherLength];

                            // Encrypt
                            int bytesWritten;
                            status = BCryptEncrypt(hKey, plainData, plainData.Length, ref authInfo,
                                null, 0, cipherText, cipherText.Length, out bytesWritten, 0);
                            if (status != STATUS_SUCCESS) 
                                throw new CryptographicException($"BCryptEncrypt failed with status {status}");

                            // Combine ciphertext and tag
                            byte[] result = new byte[bytesWritten + tagLength];
                            Buffer.BlockCopy(cipherText, 0, result, 0, bytesWritten);
                            Buffer.BlockCopy(tagBuffer, 0, result, bytesWritten, tagLength);

                            return result;
                        }
                        finally
                        {
                            if (nonceHandle.IsAllocated) nonceHandle.Free();
                            if (tagHandle.IsAllocated) tagHandle.Free();
                        }
                    }
                    finally
                    {
                        if (hKey != IntPtr.Zero) BCryptDestroyKey(hKey);
                        if (hAlg != IntPtr.Zero) BCryptCloseAlgorithmProvider(hAlg, 0);
                    }
                }
            });
        }

        /// <summary>
        /// Performs AES-GCM decryption using CGN API
        /// </summary>
        /// <param name="cipherData">Data to decrypt (including authentication tag)</param>
        /// <param name="key">Decryption key</param>
        /// <param name="nonce">Nonce for GCM mode</param>
        /// <returns>Decrypted data</returns>
        [SecuritySafeCritical]
        public byte[] DecryptAesGcm(byte[] cipherData, byte[] key, byte[] nonce)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CgnService));

            if (cipherData == null) throw new ArgumentNullException(nameof(cipherData));
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (nonce == null) throw new ArgumentNullException(nameof(nonce));

            if (key.Length != 32) throw new ArgumentException("Key must be 32 bytes", nameof(key));
            if (nonce.Length != 12) throw new ArgumentException("Nonce must be 12 bytes", nameof(nonce));

            const int tagLength = 16;
            if (cipherData.Length < tagLength)
                throw new ArgumentException("Encrypted data too short", nameof(cipherData));

            return ProtectedMemoryHelper.ExecuteWithProtection(new byte[][] { key, nonce }, (protectedArrays) =>
            {
                lock (_lockObject)
                {
                    IntPtr hAlg = IntPtr.Zero;
                    IntPtr hKey = IntPtr.Zero;

                    try
                    {
                        // Initialize algorithm provider
                        int status = BCryptOpenAlgorithmProvider(out hAlg, BCRYPT_AES_ALGORITHM, null, 0);
                        if (status != STATUS_SUCCESS) 
                            throw new CryptographicException($"BCryptOpenAlgorithmProvider failed with status {status}");

                        // Set GCM mode
                        status = BCryptSetProperty(hAlg, BCRYPT_CHAINING_MODE,
                            Encoding.Unicode.GetBytes(BCRYPT_CHAIN_MODE_GCM),
                            Encoding.Unicode.GetBytes(BCRYPT_CHAIN_MODE_GCM).Length, 0);
                        if (status != STATUS_SUCCESS) 
                            throw new CryptographicException($"BCryptSetProperty failed with status {status}");

                        // Generate key
                        status = BCryptGenerateSymmetricKey(hAlg, out hKey, IntPtr.Zero, 0, protectedArrays[0], protectedArrays[0].Length, 0);
                        if (status != STATUS_SUCCESS) 
                            throw new CryptographicException($"BCryptGenerateSymmetricKey failed with status {status}");

                        // Separate ciphertext and tag
                        int encryptedDataLength = cipherData.Length - tagLength;
                        byte[] encryptedData = new byte[encryptedDataLength];
                        byte[] tag = new byte[tagLength];
                        Buffer.BlockCopy(cipherData, 0, encryptedData, 0, encryptedDataLength);
                        Buffer.BlockCopy(cipherData, encryptedDataLength, tag, 0, tagLength);

                        var authInfo = BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO.Initialize();
                        var nonceHandle = GCHandle.Alloc(protectedArrays[1], GCHandleType.Pinned);
                        var tagHandle = GCHandle.Alloc(tag, GCHandleType.Pinned);

                        try
                        {
                            authInfo.pbNonce = nonceHandle.AddrOfPinnedObject();
                            authInfo.cbNonce = protectedArrays[1].Length;
                            authInfo.pbTag = tagHandle.AddrOfPinnedObject();
                            authInfo.cbTag = tagLength;

                            // Get required size
                            int plainTextLength;
                            status = BCryptDecrypt(hKey, encryptedData, encryptedData.Length, ref authInfo,
                                null, 0, null, 0, out plainTextLength, 0);
                            if (status != STATUS_SUCCESS) 
                                throw new CryptographicException($"BCryptDecrypt size failed with status {status}");

                            byte[] plainText = new byte[plainTextLength];

                            // Decrypt
                            int bytesWritten;
                            status = BCryptDecrypt(hKey, encryptedData, encryptedData.Length, ref authInfo,
                                null, 0, plainText, plainText.Length, out bytesWritten, 0);
                            if (status != STATUS_SUCCESS) 
                                throw new CryptographicException($"BCryptDecrypt failed with status {status}");

                            byte[] result = new byte[bytesWritten];
                            Buffer.BlockCopy(plainText, 0, result, 0, bytesWritten);

                            return result;
                        }
                        finally
                        {
                            if (nonceHandle.IsAllocated) nonceHandle.Free();
                            if (tagHandle.IsAllocated) tagHandle.Free();
                        }
                    }
                    finally
                    {
                        if (hKey != IntPtr.Zero) BCryptDestroyKey(hKey);
                        if (hAlg != IntPtr.Zero) BCryptCloseAlgorithmProvider(hAlg, 0);
                    }
                }
            });
        }

        /// <summary>
        /// Derives a key from a password using PBKDF2
        /// </summary>
        /// <param name="password">Password to derive key from</param>
        /// <param name="salt">Salt for key derivation</param>
        /// <param name="iterations">Number of iterations</param>
        /// <param name="keySize">Size of the derived key in bytes</param>
        /// <returns>Derived key</returns>
        public byte[] DeriveKeyFromPassword(string password, byte[] salt, int iterations, int keySize)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CgnService));

            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));
            if (salt == null) throw new ArgumentNullException(nameof(salt));
            if (iterations <= 0) throw new ArgumentException("Iterations must be positive", nameof(iterations));
            if (keySize <= 0) throw new ArgumentException("Key size must be positive", nameof(keySize));

            lock (_lockObject)
            {
                using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
                {
                    return pbkdf2.GetBytes(keySize);
                }
            }
        }

        // Private helper methods
        private byte[] PerformAesGcmEncryption(byte[] inputData)
        {
            // This is a simplified implementation - in practice, you'd need to parse the input data
            // to extract plaintext, key, and nonce
            throw new NotImplementedException("Direct CGN operation for AES-GCM encryption not implemented. Use EncryptAesGcm method instead.");
        }

        private byte[] PerformAesGcmDecryption(byte[] inputData)
        {
            // This is a simplified implementation - in practice, you'd need to parse the input data
            // to extract ciphertext, key, and nonce
            throw new NotImplementedException("Direct CGN operation for AES-GCM decryption not implemented. Use DecryptAesGcm method instead.");
        }

        private byte[] PerformKeyDerivation(byte[] inputData)
        {
            // This is a simplified implementation - in practice, you'd need to parse the input data
            // to extract password, salt, and iteration count
            throw new NotImplementedException("Direct CGN operation for key derivation not implemented. Use DeriveKeyFromPassword method instead.");
        }

        /// <summary>
        /// Disposes the CGN service and releases unmanaged resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected dispose method
        /// </summary>
        /// <param name="disposing">True if called from Dispose, false if called from finalizer</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources here if any
                }

                // Dispose unmanaged resources here if any
                _disposed = true;
            }
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~CgnService()
        {
            Dispose(false);
        }
    }
} 