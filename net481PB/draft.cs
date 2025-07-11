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
    }
}