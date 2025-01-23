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