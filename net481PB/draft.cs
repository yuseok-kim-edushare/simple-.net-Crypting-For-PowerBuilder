using System;
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
        //// this section for Symmetric Encryption with AES GCM mode
        /// AES-GCM not suported in .NET Framework 4.8.1 (will implement in .NET version DLL)
        //public static byte[] EncryptAesGcm(string plainText, byte[] key, byte[] nonce)
        //{
        //    using (AesGcm aesGcm = new AesGcm(key))
        //    {
        //        byte[] encryptedData = new byte[plainText.Length];
        //        byte[] tag = new byte[32]; // 256-bit tag
        //        aesGcm.Encrypt(nonce, Encoding.UTF8.GetBytes(plainText), encryptedData, tag);
        //        return Combine(encryptedData, tag);
        //    }
        //}
        //public static string DecryptAesGcm(byte[] cipherText, byte[] key, byte[] nonce)
        //{
        //    using (AesGcm aesGcm = new AesGcm(key))
        //    {
        //        byte[] tag = new byte[32];
        //        byte[] encryptedData = new byte[cipherText.Length - 32];
        //        Array.Copy(cipherText, encryptedData, encryptedData.Length);
        //        Array.Copy(cipherText, encryptedData.Length, tag, 0, tag.Length);
        //        byte[] decryptedData = new byte[encryptedData.Length];
        //        aesGcm.Decrypt(nonce, encryptedData, tag, decryptedData);
        //        return Encoding.UTF8.GetString(decryptedData);
        //    }
        //}
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
        public static byte[][] GenerateDiffieHellmanKeys()
        {
            using (ECDiffieHellmanCng dh = new ECDiffieHellmanCng())
            {
                dh.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
                dh.HashAlgorithm = CngAlgorithm.Sha256;
                byte[] publicKey = dh.PublicKey.ToByteArray();
                byte[] privateKey = dh.Key.Export(CngKeyBlobFormat.EccPrivateBlob);
                return new byte[][] { publicKey, privateKey };
            }
        }
        public static byte[] DeriveSharedKey(byte[] otherPartyPublicKey, byte[] privateKey)
        {
            using (ECDiffieHellmanCng dh = new ECDiffieHellmanCng(CngKey.Import(privateKey, CngKeyBlobFormat.EccPrivateBlob)))
            {
                using (CngKey otherKey = CngKey.Import(otherPartyPublicKey, CngKeyBlobFormat.EccPublicBlob))
                {
                    return dh.DeriveKeyMaterial(otherKey);
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