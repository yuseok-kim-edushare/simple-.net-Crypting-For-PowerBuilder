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
            if (plainText == null) throw new ArgumentNullException(nameof(plainText));
            if (string.IsNullOrEmpty(base64Key)) throw new ArgumentNullException(nameof(base64Key));

            byte[] key = Convert.FromBase64String(base64Key);
            
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
                string encryptedBase64 = Convert.ToBase64String(Combine(encryptedData, tag));
                
                // Combine nonce and ciphertext
                return base64Nonce + ":" + encryptedBase64;
            }
        }

        public static string DecryptAesGcm(string combinedData, string base64Key)
        {
            if (string.IsNullOrEmpty(combinedData)) throw new ArgumentNullException(nameof(combinedData));
            if (string.IsNullOrEmpty(base64Key)) throw new ArgumentNullException(nameof(base64Key));

            // Split the combined data
            string[] parts = combinedData.Split(':');
            if (parts.Length != 2)
                throw new ArgumentException("Invalid encrypted data format", nameof(combinedData));

            string base64Nonce = parts[0];
            string encryptedBase64 = parts[1];

            byte[] key = Convert.FromBase64String(base64Key);
            byte[] cipherText = Convert.FromBase64String(encryptedBase64);
            byte[] nonce = Convert.FromBase64String(base64Nonce);

            using (var aesGcm = new AesGcm(key, 16))
            {
                byte[] tag = new byte[16];
                byte[] encryptedData = new byte[cipherText.Length - 16];
                Array.Copy(cipherText, encryptedData, encryptedData.Length);
                Array.Copy(cipherText, encryptedData.Length, tag, 0, tag.Length);
                byte[] decryptedData = new byte[encryptedData.Length];
                aesGcm.Decrypt(nonce, encryptedData, tag, decryptedData);
                return Encoding.UTF8.GetString(decryptedData);
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
                return [ base64CipherText, base64IV ];
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
