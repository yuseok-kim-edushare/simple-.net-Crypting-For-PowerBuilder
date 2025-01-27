using System;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Security.Cryptography;
using System.Text;
using System.Security;
using SecureLibrary.SQL;
using System.Collections;

[assembly: AllowPartiallyTrustedCallers]
[assembly: SecurityRules(SecurityRuleSet.Level2)]

namespace SecureLibrary.SQL
{
    [SqlUserDefinedType(Format.Native)]
    public class SqlCLRCrypting
    {
        [SqlFunction(
            IsDeterministic = true,
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        public static SqlString GenerateAESKey()
        {
            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.KeySize = 256;
                    aes.GenerateKey();
                    return new SqlString(Convert.ToBase64String(aes.Key));
                }
            }
            catch (Exception)
            {
                return SqlString.Null;
            }
        }

        [SqlFunction(
            IsDeterministic = true,
            IsPrecise = true,
            DataAccess = DataAccessKind.None,
            FillRowMethodName = "FillEncryptAESRow"
        )]
        public static IEnumerable EncryptAES(SqlString plainText, SqlString base64Key)
        {
            if (plainText.IsNull || base64Key.IsNull)
                yield break;

            SqlString[] result = null;
            try
            {
                byte[] key = Convert.FromBase64String(base64Key.Value);
                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.GenerateIV();
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    byte[] plainBytes = Encoding.UTF8.GetBytes(plainText.Value);
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

                    result = new SqlString[] { 
                        new SqlString(Convert.ToBase64String(cipherText)),
                        new SqlString(Convert.ToBase64String(aes.IV))
                    };
                }
            }
            catch (Exception)
            {
                yield break;
            }

            if (result != null)
                yield return result;
        }

        public static void FillEncryptAESRow(object obj, out SqlString cipherText, out SqlString iv)
        {
            SqlString[] result = (SqlString[])obj;
            cipherText = result[0];
            iv = result[1];
        }

        [SqlFunction(
            IsDeterministic = true,
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        public static SqlString DecryptAES(SqlString base64CipherText, SqlString base64Key, SqlString base64IV)
        {
            try
            {
                if (base64CipherText.IsNull || base64Key.IsNull || base64IV.IsNull)
                    return SqlString.Null;

                byte[] key = Convert.FromBase64String(base64Key.Value);
                byte[] cipherText = Convert.FromBase64String(base64CipherText.Value);
                byte[] iv = Convert.FromBase64String(base64IV.Value);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var memoryStream = new System.IO.MemoryStream(cipherText))
                    using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    using (var reader = new System.IO.StreamReader(cryptoStream, Encoding.UTF8))
                    {
                        return new SqlString(reader.ReadToEnd());
                    }
                }
            }
            catch (Exception)
            {
                return SqlString.Null;
            }
        }

        // this section related about diffie hellman
        [SqlFunction(
            IsDeterministic = true,
            IsPrecise = true,
            DataAccess = DataAccessKind.None,
            FillRowMethodName = "FillDiffieHellmanKeysRow"
        )]
        public static IEnumerable GenerateDiffieHellmanKeys()
        {
            SqlString[] result = null;
            try
            {
                using (ECDiffieHellmanCng dh = new ECDiffieHellmanCng())
                {
                    dh.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
                    dh.HashAlgorithm = CngAlgorithm.Sha256;
                    byte[] publicKey = dh.PublicKey.ToByteArray();
                    byte[] privateKey = dh.Key.Export(CngKeyBlobFormat.EccPrivateBlob);
                    result = new SqlString[] { 
                        new SqlString(Convert.ToBase64String(publicKey)), 
                        new SqlString(Convert.ToBase64String(privateKey)) 
                    };
                }
            }
            catch (Exception)
            {
                yield break;
            }

            if (result != null)
                yield return result;
        }

        public static void FillDiffieHellmanKeysRow(object obj, out SqlString publicKey, out SqlString privateKey)
        {
            SqlString[] result = (SqlString[])obj;
            publicKey = result[0];
            privateKey = result[1];
        }

        [SqlFunction(
            IsDeterministic = true,
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        public static SqlString DeriveSharedKey(SqlString otherPartyPublicKeyBase64, SqlString privateKeyBase64)
        {
            byte[] otherPartyPublicKey = Convert.FromBase64String(otherPartyPublicKeyBase64.Value);
            byte[] privateKey = Convert.FromBase64String(privateKeyBase64.Value);
            
            using (ECDiffieHellmanCng dh = new ECDiffieHellmanCng(CngKey.Import(privateKey, CngKeyBlobFormat.EccPrivateBlob)))
            {
                using (CngKey otherKey = CngKey.Import(otherPartyPublicKey, CngKeyBlobFormat.EccPublicBlob))
                {
                    byte[] sharedKey = dh.DeriveKeyMaterial(otherKey);
                    return new SqlString(Convert.ToBase64String(sharedKey));
                }
            }
        }

        [SqlFunction(
            IsDeterministic = true,
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        [SecuritySafeCritical]
        public static SqlString HashPassword(SqlString password)
        {
            try
            {
                if (password.IsNull)
                    return SqlString.Null;

                return new SqlString(BCrypt.Net.BCrypt.HashPassword(password.Value, 10));
            }
            catch (Exception)
            {
                return SqlString.Null;
            }
        }

        [SqlFunction(
            IsDeterministic = true,
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        [SecuritySafeCritical]
        public static SqlBoolean VerifyPassword(SqlString password, SqlString hashedPassword)
        {
            try
            {
                if (password.IsNull || hashedPassword.IsNull)
                    return SqlBoolean.Null;

                return new SqlBoolean(BCrypt.Net.BCrypt.Verify(password.Value, hashedPassword.Value));
            }
            catch (Exception)
            {
                return SqlBoolean.Null;
            }
        }

        [SqlFunction(
            IsDeterministic = true,
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        [SecuritySafeCritical]
        public static SqlString EncryptAesGcm(SqlString plainText, SqlString base64Key)
        {
            try
            {
                if (plainText.IsNull || base64Key.IsNull)
                {
                    Console.WriteLine("Input is null");
                    return SqlString.Null;
                }

                byte[] keyBytes;
                try
                {
                    keyBytes = Convert.FromBase64String(base64Key.Value);
                    if (keyBytes.Length != 32)
                    {
                        Console.WriteLine(string.Format("Invalid key length: {0}", keyBytes.Length));
                        return SqlString.Null;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("Key conversion error: {0}", ex.Message));
                    return SqlString.Null;
                }

                // Generate new nonce
                byte[] nonce = new byte[12];
                using (var rng = new RNGCryptoServiceProvider())
                {
                    rng.GetBytes(nonce);
                }
                string base64Nonce = Convert.ToBase64String(nonce);

                try
                {
                    // Get the encrypted result
                    string encryptedBase64 = BcryptInterop.EncryptAesGcm(plainText.Value, base64Key.Value, base64Nonce);
                    if (string.IsNullOrEmpty(encryptedBase64))
                    {
                        Console.WriteLine("Encryption returned null or empty");
                        return SqlString.Null;
                    }

                    // Combine nonce and ciphertext
                    string combined = base64Nonce + ":" + encryptedBase64;
                    return new SqlString(combined);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("Encryption error: {0}", ex.Message));
                    return SqlString.Null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Outer encryption error: {0}", ex.Message));
                return SqlString.Null;
            }
        }

        [SqlFunction(
            IsDeterministic = true,
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        [SecuritySafeCritical]
        public static SqlString DecryptAesGcm(SqlString combinedData, SqlString base64Key)
        {
            try
            {
                if (combinedData.IsNull || base64Key.IsNull)
                    return SqlString.Null;

                // Validate key length
                if (Convert.FromBase64String(base64Key.Value).Length != 32)
                    return SqlString.Null;

                // Split the combined data
                string[] parts = combinedData.Value.Split(':');
                if (parts.Length != 2)
                {
                    Console.WriteLine("Invalid combined data format");
                    return SqlString.Null;
                }

                string base64Nonce = parts[0];
                string encryptedBase64 = parts[1];

                try
                {
                    // Validate nonce length
                    if (Convert.FromBase64String(base64Nonce).Length != 12)
                    {
                        Console.WriteLine("Invalid nonce length");
                        return SqlString.Null;
                    }

                    // Decrypt using the extracted nonce
                    string decrypted = BcryptInterop.DecryptAesGcm(encryptedBase64, base64Key.Value, base64Nonce);
                    if (string.IsNullOrEmpty(decrypted))
                    {
                        Console.WriteLine("Decryption returned null or empty string");
                        return SqlString.Null;
                    }

                    return new SqlString(decrypted);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("Decryption error: {0}", ex.Message));
                    return SqlString.Null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Outer decryption error: {0}", ex.Message));
                return SqlString.Null;
            }
        }
    }
}