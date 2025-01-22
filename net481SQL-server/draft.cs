using System;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using System.Security;

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
            DataAccess = DataAccessKind.None
        )]
        public static SqlString[] EncryptAES(SqlString plainText, SqlString base64Key)
        {
            try
            {
                if (plainText.IsNull || base64Key.IsNull)
                    return null;

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

                    return new SqlString[] 
                    { 
                        new SqlString(Convert.ToBase64String(cipherText)),
                        new SqlString(Convert.ToBase64String(aes.IV))
                    };
                }
            }
            catch (Exception)
            {
                return null;
            }
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

        [SqlFunction(
            IsDeterministic = true,
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
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
    }
}