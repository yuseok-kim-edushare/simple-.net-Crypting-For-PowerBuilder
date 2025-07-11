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
        [Obsolete("This function is deprecated because AES-CBC without an authentication mechanism is insecure. Please use EncryptAES_GCM instead.")]
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
        [Obsolete("This function is deprecated because AES-CBC without an authentication mechanism is insecure. Please use DecryptAES_GCM instead.")]
        public static SqlString DecryptAES(SqlString base64CipherText, SqlString base64Key, SqlString base64IV)
        {
            if (base64CipherText.IsNull || base64Key.IsNull || base64IV.IsNull)
                return SqlString.Null;

            byte[] key = null;
            byte[] cipherText = null;
            byte[] iv = null;
            
            try
            {
                key = Convert.FromBase64String(base64Key.Value);
                cipherText = Convert.FromBase64String(base64CipherText.Value);
                iv = Convert.FromBase64String(base64IV.Value);

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
            finally
            {
                if (key != null) Array.Clear(key, 0, key.Length);
                if (cipherText != null) Array.Clear(cipherText, 0, cipherText.Length);
                if (iv != null) Array.Clear(iv, 0, iv.Length);
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
            if (otherPartyPublicKeyBase64.IsNull || privateKeyBase64.IsNull)
                return SqlString.Null;
            
            byte[] otherPartyPublicKey = null;
            byte[] privateKey = null;

            try
            {
                otherPartyPublicKey = Convert.FromBase64String(otherPartyPublicKeyBase64.Value);
                privateKey = Convert.FromBase64String(privateKeyBase64.Value);
                
                using (ECDiffieHellmanCng dh = new ECDiffieHellmanCng(CngKey.Import(privateKey, CngKeyBlobFormat.EccPrivateBlob)))
                {
                    dh.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
                    dh.HashAlgorithm = CngAlgorithm.Sha256;

                    try
                    {
                        // Try importing as EccPublicBlob first
                        using (var importedKey = CngKey.Import(otherPartyPublicKey, CngKeyBlobFormat.EccPublicBlob))
                        {
                            return new SqlString(Convert.ToBase64String(dh.DeriveKeyMaterial(importedKey)));
                        }
                    }
                    catch
                    {
                        // If EccPublicBlob fails, try as GenericPublicBlob
                        using (var importedKey = CngKey.Import(otherPartyPublicKey, CngKeyBlobFormat.GenericPublicBlob))
                        {
                            return new SqlString(Convert.ToBase64String(dh.DeriveKeyMaterial(importedKey)));
                        }
                    }
                }
            }
            finally
            {
                if (privateKey != null) Array.Clear(privateKey, 0, privateKey.Length);
                if (otherPartyPublicKey != null) Array.Clear(otherPartyPublicKey, 0, otherPartyPublicKey.Length);
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
            // Overload for backward compatibility, defaults to a work factor of 12
            return HashPassword(password, 12);
        }

        [SqlFunction(
            IsDeterministic = true,
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        [SecuritySafeCritical]
        public static SqlString HashPassword(SqlString password, SqlInt32 workFactor)
        {
            if (password.IsNull)
                return SqlString.Null;
            
            if (workFactor.IsNull)
                return HashPassword(password); // Call overload to use default

            return new SqlString(BCrypt.Net.BCrypt.HashPassword(password.Value, workFactor.Value));
        }

        [SqlFunction(
            IsDeterministic = true,
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        [SecuritySafeCritical]
        public static SqlBoolean VerifyPassword(SqlString password, SqlString hashedPassword)
        {
            if (password.IsNull || hashedPassword.IsNull)
                return SqlBoolean.Null;

            return new SqlBoolean(BCrypt.Net.BCrypt.Verify(password.Value, hashedPassword.Value));
        }

        [SqlFunction(
            IsDeterministic = true,
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        [SecuritySafeCritical]
        public static SqlString EncryptAesGcm(SqlString plainText, SqlString base64Key)
        {
            if (plainText.IsNull || base64Key.IsNull)
                return SqlString.Null;

            byte[] keyBytes = null;
            try
            {
                keyBytes = Convert.FromBase64String(base64Key.Value);
                if (keyBytes.Length != 32)
                    throw new ArgumentException("Invalid key length", "base64Key");
                
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
                        throw new CryptographicException("Encryption returned null or empty");
                    }

                    // Combine nonce and ciphertext
                    string combined = base64Nonce + ":" + encryptedBase64;
                    return new SqlString(combined);
                }
                finally
                {
                    if (nonce != null) Array.Clear(nonce, 0, nonce.Length);
                }
            }
            finally
            {
                if (keyBytes != null)
                    Array.Clear(keyBytes, 0, keyBytes.Length);
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
            if (combinedData.IsNull || base64Key.IsNull)
                return SqlString.Null;

            byte[] keyBytes = null;
            byte[] nonceBytes = null;
            try
            {
                // Validate key length
                keyBytes = Convert.FromBase64String(base64Key.Value);
                if (keyBytes.Length != 32)
                    throw new ArgumentException("Invalid key length for AES-256", "base64Key");

                // Split the combined data
                string[] parts = combinedData.Value.Split(':');
                if (parts.Length != 2)
                    throw new ArgumentException("Invalid encrypted data format. Expected 'nonce:ciphertext'.", "combinedData");

                string base64Nonce = parts[0];
                string encryptedBase64 = parts[1];

                // Validate nonce length
                nonceBytes = Convert.FromBase64String(base64Nonce);
                if (nonceBytes.Length != 12)
                    throw new ArgumentException("Invalid nonce length. Must be 12 bytes.", "combinedData");

                // Decrypt using the extracted nonce
                string decrypted = BcryptInterop.DecryptAesGcm(encryptedBase64, base64Key.Value, base64Nonce);
                if (decrypted == null) // BcryptInterop could still return null on non-exception failure
                {
                    throw new CryptographicException("Decryption returned null");
                }

                return new SqlString(decrypted);
            }
            finally
            {
                if (keyBytes != null) Array.Clear(keyBytes, 0, keyBytes.Length);
                if (nonceBytes != null) Array.Clear(nonceBytes, 0, nonceBytes.Length);
            }
        }
    }
}