using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Security;

namespace SecureLibrary.SQL
{
    [SecuritySafeCritical]
    public class BcryptInterop
    {
        private const string BCRYPT_AES_ALGORITHM = "AES";
        private const string BCRYPT_CHAINING_MODE = "ChainingMode";
        private const string BCRYPT_CHAIN_MODE_GCM = "ChainingModeGCM";
        private const int STATUS_SUCCESS = 0;

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
        /// Encrypts string using AES-GCM with password-based key derivation
        /// </summary>
        /// <param name="plainText">Text to encrypt</param>
        /// <param name="password">Password for key derivation</param>
        /// <param name="salt">Salt for key derivation (optional, will generate if null)</param>
        /// <param name="iterations">PBKDF2 iteration count (default: 2000)</param>
        /// <returns>Base64 encoded encrypted data with salt, nonce, and tag</returns>
        public static string EncryptAesGcmWithPassword(string plainText, string password, byte[] salt = null, int iterations = 2000)
        {
            if (plainText == null) throw new ArgumentNullException("plainText");
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException("password");

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = EncryptAesGcmBytes(plainBytes, password, salt, iterations);
            
            // Clear sensitive data
            Array.Clear(plainBytes, 0, plainBytes.Length);
            
            return Convert.ToBase64String(encryptedBytes);
        }

        /// <summary>
        /// Decrypts string using AES-GCM with password-based key derivation
        /// </summary>
        /// <param name="base64EncryptedData">Base64 encoded encrypted data</param>
        /// <param name="password">Password for key derivation</param>
        /// <param name="iterations">PBKDF2 iteration count (default: 2000)</param>
        /// <returns>Decrypted text</returns>
        public static string DecryptAesGcmWithPassword(string base64EncryptedData, string password, int iterations = 2000)
        {
            if (string.IsNullOrEmpty(base64EncryptedData)) throw new ArgumentNullException("base64EncryptedData");
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException("password");

            byte[] encryptedBytes = Convert.FromBase64String(base64EncryptedData);
            byte[] decryptedBytes = DecryptAesGcmBytes(encryptedBytes, password, iterations);
            
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
        public static byte[] EncryptAesGcmBytes(byte[] plainData, string password, byte[] salt = null, int iterations = 2000)
        {
            if (plainData == null) throw new ArgumentNullException("plainData");
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException("password");

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
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(nonce);
            }

            byte[] encryptedData = EncryptAesGcmBytes(plainData, key, nonce);

            // Combine salt + nonce + encrypted data for output
            byte[] result = new byte[salt.Length + nonce.Length + encryptedData.Length];
            Buffer.BlockCopy(salt, 0, result, 0, salt.Length);
            Buffer.BlockCopy(nonce, 0, result, salt.Length, nonce.Length);
            Buffer.BlockCopy(encryptedData, 0, result, salt.Length + nonce.Length, encryptedData.Length);

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
        public static byte[] DecryptAesGcmBytes(byte[] encryptedData, string password, int iterations = 2000)
        {
            if (encryptedData == null) throw new ArgumentNullException("encryptedData");
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException("password");

            const int nonceLength = 12;
            const int tagLength = 16;
            const int minSaltLength = 8;
            const int maxSaltLength = 64;

            if (encryptedData.Length < minSaltLength + nonceLength + tagLength)
                throw new ArgumentException("Encrypted data too short", "encryptedData");

            // Determine salt length by trying different lengths
            int saltLength = 0;
            byte[] salt = null;
            byte[] nonce = null;
            byte[] cipherWithTag = null;
            byte[] key = null;

            // Try different salt lengths from minimum to maximum
            for (int testSaltLength = minSaltLength; testSaltLength <= maxSaltLength && testSaltLength <= encryptedData.Length - nonceLength - tagLength; testSaltLength++)
            {
                try
                {
                    salt = new byte[testSaltLength];
                    nonce = new byte[nonceLength];
                    cipherWithTag = new byte[encryptedData.Length - testSaltLength - nonceLength];

                    Buffer.BlockCopy(encryptedData, 0, salt, 0, testSaltLength);
                    Buffer.BlockCopy(encryptedData, testSaltLength, nonce, 0, nonceLength);
                    Buffer.BlockCopy(encryptedData, testSaltLength + nonceLength, cipherWithTag, 0, cipherWithTag.Length);

                    // Try to derive key and decrypt
                    using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
                    {
                        key = pbkdf2.GetBytes(32);
                    }

                    byte[] result = DecryptAesGcmBytes(cipherWithTag, key, nonce);
                    
                    // If we get here, decryption succeeded, so this is the correct salt length
                    saltLength = testSaltLength;
                    
                    // Clear sensitive data
                    Array.Clear(salt, 0, salt.Length);
                    Array.Clear(nonce, 0, nonce.Length);
                    Array.Clear(cipherWithTag, 0, cipherWithTag.Length);
                    Array.Clear(key, 0, key.Length);
                    
                    return result;
                }
                catch
                {
                    // Clear arrays before trying next salt length
                    if (salt != null) Array.Clear(salt, 0, salt.Length);
                    if (nonce != null) Array.Clear(nonce, 0, nonce.Length);
                    if (cipherWithTag != null) Array.Clear(cipherWithTag, 0, cipherWithTag.Length);
                    if (key != null) Array.Clear(key, 0, key.Length);
                    
                    // Continue to next salt length
                }
            }

            // If we get here, no salt length worked
            throw new CryptographicException("Failed to decrypt data with any valid salt length");
        }

        /// <summary>
        /// Encrypts byte array using AES-GCM with provided key and nonce
        /// </summary>
        /// <param name="plainData">Data to encrypt</param>
        /// <param name="key">32-byte encryption key</param>
        /// <param name="nonce">12-byte nonce</param>
        /// <returns>Encrypted data with authentication tag</returns>
        public static byte[] EncryptAesGcmBytes(byte[] plainData, byte[] key, byte[] nonce)
        {
            if (plainData == null) throw new ArgumentNullException("plainData");
            if (key == null) throw new ArgumentNullException("key");
            if (nonce == null) throw new ArgumentNullException("nonce");

            if (key.Length != 32) throw new ArgumentException("Key must be 32 bytes", "key");
            if (nonce.Length != 12) throw new ArgumentException("Nonce must be 12 bytes", "nonce");

            IntPtr hAlg = IntPtr.Zero;
            IntPtr hKey = IntPtr.Zero;

            try
            {
                // Initialize algorithm provider
                int status = BCryptOpenAlgorithmProvider(out hAlg, BCRYPT_AES_ALGORITHM, null, 0);
                if (status != STATUS_SUCCESS) throw new CryptographicException("BCryptOpenAlgorithmProvider failed with status " + status);

                // Set GCM mode
                status = BCryptSetProperty(hAlg, BCRYPT_CHAINING_MODE, 
                    Encoding.Unicode.GetBytes(BCRYPT_CHAIN_MODE_GCM), 
                    Encoding.Unicode.GetBytes(BCRYPT_CHAIN_MODE_GCM).Length, 0);
                if (status != STATUS_SUCCESS) throw new CryptographicException("BCryptSetProperty failed with status " + status);

                // Generate key
                status = BCryptGenerateSymmetricKey(hAlg, out hKey, IntPtr.Zero, 0, key, key.Length, 0);
                if (status != STATUS_SUCCESS) throw new CryptographicException("BCryptGenerateSymmetricKey failed with status " + status);

                const int tagLength = 16;  // GCM tag length

                var authInfo = BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO.Initialize();
                var nonceHandle = GCHandle.Alloc(nonce, GCHandleType.Pinned);
                var tagBuffer = new byte[tagLength];
                var tagHandle = GCHandle.Alloc(tagBuffer, GCHandleType.Pinned);

                try
                {
                    authInfo.pbNonce = nonceHandle.AddrOfPinnedObject();
                    authInfo.cbNonce = nonce.Length;
                    authInfo.pbTag = tagHandle.AddrOfPinnedObject();
                    authInfo.cbTag = tagLength;

                    // Get required size
                    int cipherLength;
                    status = BCryptEncrypt(hKey, plainData, plainData.Length, ref authInfo,
                        null, 0, null, 0, out cipherLength, 0);
                    if (status != STATUS_SUCCESS) throw new CryptographicException("BCryptEncrypt size failed with status " + status);

                    byte[] cipherText = new byte[cipherLength];

                    // Encrypt
                    int bytesWritten;
                    status = BCryptEncrypt(hKey, plainData, plainData.Length, ref authInfo,
                        null, 0, cipherText, cipherText.Length, out bytesWritten, 0);
                    if (status != STATUS_SUCCESS) throw new CryptographicException("BCryptEncrypt failed with status " + status);

                    // Combine ciphertext and tag
                    byte[] result = new byte[bytesWritten + tagLength];
                    Buffer.BlockCopy(cipherText, 0, result, 0, bytesWritten);
                    Buffer.BlockCopy(tagBuffer, 0, result, bytesWritten, tagLength);

                    // Clear sensitive data
                    Array.Clear(cipherText, 0, cipherText.Length);
                    Array.Clear(tagBuffer, 0, tagBuffer.Length);

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

        /// <summary>
        /// Decrypts byte array using AES-GCM with provided key and nonce
        /// </summary>
        /// <param name="cipherWithTag">Encrypted data with authentication tag</param>
        /// <param name="key">32-byte decryption key</param>
        /// <param name="nonce">12-byte nonce</param>
        /// <returns>Decrypted data</returns>
        public static byte[] DecryptAesGcmBytes(byte[] cipherWithTag, byte[] key, byte[] nonce)
        {
            if (cipherWithTag == null) throw new ArgumentNullException("cipherWithTag");
            if (key == null) throw new ArgumentNullException("key");
            if (nonce == null) throw new ArgumentNullException("nonce");

            if (key.Length != 32) throw new ArgumentException("Key must be 32 bytes", "key");
            if (nonce.Length != 12) throw new ArgumentException("Nonce must be 12 bytes", "nonce");

            const int tagLength = 16;
            if (cipherWithTag.Length < tagLength)
                throw new ArgumentException("Encrypted data too short", "cipherWithTag");

            IntPtr hAlg = IntPtr.Zero;
            IntPtr hKey = IntPtr.Zero;

            try
            {
                // Initialize algorithm provider
                int status = BCryptOpenAlgorithmProvider(out hAlg, BCRYPT_AES_ALGORITHM, null, 0);
                if (status != STATUS_SUCCESS) throw new CryptographicException("BCryptOpenAlgorithmProvider failed with status " + status);

                // Set GCM mode
                status = BCryptSetProperty(hAlg, BCRYPT_CHAINING_MODE,
                    Encoding.Unicode.GetBytes(BCRYPT_CHAIN_MODE_GCM),
                    Encoding.Unicode.GetBytes(BCRYPT_CHAIN_MODE_GCM).Length, 0);
                if (status != STATUS_SUCCESS) throw new CryptographicException("BCryptSetProperty failed with status " + status);

                // Generate key
                status = BCryptGenerateSymmetricKey(hAlg, out hKey, IntPtr.Zero, 0, key, key.Length, 0);
                if (status != STATUS_SUCCESS) throw new CryptographicException("BCryptGenerateSymmetricKey failed with status " + status);

                // Separate ciphertext and tag
                int encryptedDataLength = cipherWithTag.Length - tagLength;
                byte[] encryptedData = new byte[encryptedDataLength];
                byte[] tag = new byte[tagLength];
                Buffer.BlockCopy(cipherWithTag, 0, encryptedData, 0, encryptedDataLength);
                Buffer.BlockCopy(cipherWithTag, encryptedDataLength, tag, 0, tagLength);

                var authInfo = BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO.Initialize();
                var nonceHandle = GCHandle.Alloc(nonce, GCHandleType.Pinned);
                var tagHandle = GCHandle.Alloc(tag, GCHandleType.Pinned);

                try
                {
                    authInfo.pbNonce = nonceHandle.AddrOfPinnedObject();
                    authInfo.cbNonce = nonce.Length;
                    authInfo.pbTag = tagHandle.AddrOfPinnedObject();
                    authInfo.cbTag = tagLength;

                    // Get required size
                    int plainTextLength;
                    status = BCryptDecrypt(hKey, encryptedData, encryptedData.Length, ref authInfo,
                        null, 0, null, 0, out plainTextLength, 0);
                    if (status != STATUS_SUCCESS) throw new CryptographicException("BCryptDecrypt size failed with status " + status);

                    byte[] plainText = new byte[plainTextLength];

                    // Decrypt
                    int bytesWritten;
                    status = BCryptDecrypt(hKey, encryptedData, encryptedData.Length, ref authInfo,
                        null, 0, plainText, plainText.Length, out bytesWritten, 0);
                    if (status != STATUS_SUCCESS) throw new CryptographicException("BCryptDecrypt failed with status " + status);

                    byte[] result = new byte[bytesWritten];
                    Buffer.BlockCopy(plainText, 0, result, 0, bytesWritten);

                    // Clear sensitive data
                    Array.Clear(encryptedData, 0, encryptedData.Length);
                    Array.Clear(tag, 0, tag.Length);
                    Array.Clear(plainText, 0, plainText.Length);

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

        /// <summary>
        /// Encrypts string using AES-GCM with provided key and nonce (legacy method)
        /// </summary>
        /// <param name="plainText">Text to encrypt</param>
        /// <param name="base64Key">Base64 encoded 32-byte key</param>
        /// <param name="base64Nonce">Base64 encoded 12-byte nonce</param>
        /// <returns>Base64 encoded encrypted data with authentication tag</returns>
        public static string EncryptAesGcm(string plainText, string base64Key, string base64Nonce)
        {
            if (plainText == null) throw new ArgumentNullException("plainText");
            if (string.IsNullOrEmpty(base64Key)) throw new ArgumentNullException("base64Key");
            if (string.IsNullOrEmpty(base64Nonce)) throw new ArgumentNullException("base64Nonce");

            // Convert Base64 strings to byte arrays
            byte[] key = Convert.FromBase64String(base64Key);
            byte[] nonce = Convert.FromBase64String(base64Nonce);

            if (key.Length != 32) throw new ArgumentException("Key must be 32 bytes", "base64Key");
            if (nonce.Length != 12) throw new ArgumentException("Nonce must be 12 bytes", "base64Nonce");

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = EncryptAesGcmBytes(plainBytes, key, nonce);
            
            // Clear sensitive data
            Array.Clear(key, 0, key.Length);
            Array.Clear(nonce, 0, nonce.Length);
            Array.Clear(plainBytes, 0, plainBytes.Length);
            
            return Convert.ToBase64String(encryptedBytes);
        }

        /// <summary>
        /// Decrypts string using AES-GCM with provided key and nonce (legacy method)
        /// </summary>
        /// <param name="base64CipherText">Base64 encoded encrypted data</param>
        /// <param name="base64Key">Base64 encoded 32-byte key</param>
        /// <param name="base64Nonce">Base64 encoded 12-byte nonce</param>
        /// <returns>Decrypted text</returns>
        public static string DecryptAesGcm(string base64CipherText, string base64Key, string base64Nonce)
        {
            if (string.IsNullOrEmpty(base64CipherText)) throw new ArgumentNullException("base64CipherText");
            if (string.IsNullOrEmpty(base64Key)) throw new ArgumentNullException("base64Key");
            if (string.IsNullOrEmpty(base64Nonce)) throw new ArgumentNullException("base64Nonce");

            // Convert Base64 strings to byte arrays
            byte[] cipherText = Convert.FromBase64String(base64CipherText);
            byte[] key = Convert.FromBase64String(base64Key);
            byte[] nonce = Convert.FromBase64String(base64Nonce);

            if (key.Length != 32) throw new ArgumentException("Key must be 32 bytes", "base64Key");
            if (nonce.Length != 12) throw new ArgumentException("Nonce must be 12 bytes", "base64Nonce");

            const int tagLength = 16;
            if (cipherText.Length < tagLength)
                throw new ArgumentException("Encrypted data too short", "base64CipherText");

            byte[] decryptedBytes = DecryptAesGcmBytes(cipherText, key, nonce);
            string result = Encoding.UTF8.GetString(decryptedBytes);
            
            // Clear sensitive data
            Array.Clear(cipherText, 0, cipherText.Length);
            Array.Clear(key, 0, key.Length);
            Array.Clear(nonce, 0, nonce.Length);
            Array.Clear(decryptedBytes, 0, decryptedBytes.Length);
            
            return result;
        }
    }
}
