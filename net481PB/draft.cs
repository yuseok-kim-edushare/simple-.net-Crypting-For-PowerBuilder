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

        public static byte[] EncryptAesGcm(string plainText, byte[] key, byte[] nonce)
        {
            if (plainText == null) throw new ArgumentNullException(nameof(plainText));
            if (key == null || key.Length != 32) throw new ArgumentException("Key must be 32 bytes", nameof(key));
            if (nonce == null || nonce.Length != 12) throw new ArgumentException("Nonce must be 12 bytes", nameof(nonce));

            IntPtr hAlg = IntPtr.Zero;
            IntPtr hKey = IntPtr.Zero;

            try
            {
                // Initialize algorithm provider
                int status = BCryptOpenAlgorithmProvider(out hAlg, BCRYPT_AES_ALGORITHM, null, 0);
                if (status != STATUS_SUCCESS) throw new CryptographicException($"BCryptOpenAlgorithmProvider failed with status {status}");

                // Set GCM mode
                status = BCryptSetProperty(hAlg, BCRYPT_CHAINING_MODE, 
                    Encoding.Unicode.GetBytes(BCRYPT_CHAIN_MODE_GCM), 
                    Encoding.Unicode.GetBytes(BCRYPT_CHAIN_MODE_GCM).Length, 0);
                if (status != STATUS_SUCCESS) throw new CryptographicException($"BCryptSetProperty failed with status {status}");

                // Generate key
                status = BCryptGenerateSymmetricKey(hAlg, out hKey, IntPtr.Zero, 0, key, key.Length, 0);
                if (status != STATUS_SUCCESS) throw new CryptographicException($"BCryptGenerateSymmetricKey failed with status {status}");

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
                    status = BCryptEncrypt(hKey, plainBytes, plainBytes.Length, ref authInfo,
                        null, 0, null, 0, out int cipherLength, 0);
                    if (status != STATUS_SUCCESS) throw new CryptographicException($"BCryptEncrypt size failed with status {status}");

                    byte[] cipherText = new byte[cipherLength];

                    // Encrypt
                    status = BCryptEncrypt(hKey, plainBytes, plainBytes.Length, ref authInfo,
                        null, 0, cipherText, cipherText.Length, out int bytesWritten, 0);
                    if (status != STATUS_SUCCESS) throw new CryptographicException($"BCryptEncrypt failed with status {status}");

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

        public static string DecryptAesGcm(byte[] cipherText, byte[] key, byte[] nonce)
        {
            if (cipherText == null) throw new ArgumentNullException(nameof(cipherText));
            if (key == null || key.Length != 32) throw new ArgumentException("Key must be 32 bytes", nameof(key));
            if (nonce == null || nonce.Length != 12) throw new ArgumentException("Nonce must be 12 bytes", nameof(nonce));

            const int tagLength = 16;
            if (cipherText.Length < tagLength)
                throw new ArgumentException("Encrypted data too short", nameof(cipherText));

            IntPtr hAlg = IntPtr.Zero;
            IntPtr hKey = IntPtr.Zero;

            try
            {
                // Initialize algorithm provider
                int status = BCryptOpenAlgorithmProvider(out hAlg, BCRYPT_AES_ALGORITHM, null, 0);
                if (status != STATUS_SUCCESS) throw new CryptographicException($"BCryptOpenAlgorithmProvider failed with status {status}");

                // Set GCM mode
                status = BCryptSetProperty(hAlg, BCRYPT_CHAINING_MODE,
                    Encoding.Unicode.GetBytes(BCRYPT_CHAIN_MODE_GCM),
                    Encoding.Unicode.GetBytes(BCRYPT_CHAIN_MODE_GCM).Length, 0);
                if (status != STATUS_SUCCESS) throw new CryptographicException($"BCryptSetProperty failed with status {status}");

                // Generate key
                status = BCryptGenerateSymmetricKey(hAlg, out hKey, IntPtr.Zero, 0, key, key.Length, 0);
                if (status != STATUS_SUCCESS) throw new CryptographicException($"BCryptGenerateSymmetricKey failed with status {status}");

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
                    status = BCryptDecrypt(hKey, encryptedData, encryptedData.Length, ref authInfo,
                        null, 0, null, 0, out int plainTextLength, 0);
                    if (status != STATUS_SUCCESS) throw new CryptographicException($"BCryptDecrypt size failed with status {status}");

                    byte[] plainText = new byte[plainTextLength];

                    // Decrypt
                    status = BCryptDecrypt(hKey, encryptedData, encryptedData.Length, ref authInfo,
                        null, 0, plainText, plainText.Length, out int bytesWritten, 0);
                    if (status != STATUS_SUCCESS) throw new CryptographicException($"BCryptDecrypt failed with status {status}");

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

        // this section for Symmetric Encryption with AES CBC mode
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
                return new string[] { base64CipherText, base64IV };
            }
        }
        public static string DecryptAesCbcWithIv(string base64CipherText, string base64Key, string base64IV)
        {
            byte[] key = Convert.FromBase64String(base64Key);
            byte[] cipherText = Convert.FromBase64String(base64CipherText);
            byte[] iv = Convert.FromBase64String(base64IV);
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
            
            using (ECDiffieHellmanCng dh = new ECDiffieHellmanCng(CngKey.Import(privateKey, CngKeyBlobFormat.EccPrivateBlob)))
            {
                using (CngKey otherKey = CngKey.Import(otherPartyPublicKey, CngKeyBlobFormat.EccPublicBlob))
                {
                    byte[] sharedKey = dh.DeriveKeyMaterial(otherKey);
                    return Convert.ToBase64String(sharedKey);
                }
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
            return BCrypt.Net.BCrypt.HashPassword(password, 10);
        }
        public static bool VerifyBcryptPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
    }
}