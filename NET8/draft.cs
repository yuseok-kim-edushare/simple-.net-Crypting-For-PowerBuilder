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
        public static string BcryptEncoding(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, 12);
        }
        public static bool VerifyBcryptPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
    }
}
