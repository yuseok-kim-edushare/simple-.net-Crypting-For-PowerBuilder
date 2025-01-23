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

                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
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
                    status = BCryptEncrypt(hKey, plainBytes, plainBytes.Length, ref authInfo,
                        null, 0, null, 0, out cipherLength, 0);
                    if (status != STATUS_SUCCESS) throw new CryptographicException("BCryptEncrypt size failed with status " + status);

                    byte[] cipherText = new byte[cipherLength];

                    // Encrypt
                    int bytesWritten;
                    status = BCryptEncrypt(hKey, plainBytes, plainBytes.Length, ref authInfo,
                        null, 0, cipherText, cipherText.Length, out bytesWritten, 0);
                    if (status != STATUS_SUCCESS) throw new CryptographicException("BCryptEncrypt failed with status " + status);

                    // Combine ciphertext and tag
                    byte[] result = new byte[bytesWritten + tagLength];
                    Buffer.BlockCopy(cipherText, 0, result, 0, bytesWritten);
                    Buffer.BlockCopy(tagBuffer, 0, result, bytesWritten, tagLength);

                    // Convert final result to Base64
                    return Convert.ToBase64String(result);
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
                int encryptedDataLength = cipherText.Length - tagLength;
                byte[] encryptedData = new byte[encryptedDataLength];
                byte[] tag = new byte[tagLength];
                Buffer.BlockCopy(cipherText, 0, encryptedData, 0, encryptedDataLength);
                Buffer.BlockCopy(cipherText, encryptedDataLength, tag, 0, tagLength);

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

                    return Encoding.UTF8.GetString(plainText, 0, bytesWritten);
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
    }
}
