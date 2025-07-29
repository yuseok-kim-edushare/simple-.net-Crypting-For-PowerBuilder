using System;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Security.Cryptography;
using System.Text;
using System.Security;
using SecureLibrary.SQL;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Data;

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
        public static SqlString HashPasswordDefault(SqlString password)
        {
            // Default overload with work factor of 12
            return HashPasswordWithWorkFactor(password, 12);
        }

        [SqlFunction(
            IsDeterministic = true,
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        [SecuritySafeCritical]
        public static SqlString HashPasswordWithWorkFactor(SqlString password, SqlInt32 workFactor)
        {
            if (password.IsNull)
                return SqlString.Null;
            
            if (workFactor.IsNull)
                return HashPasswordDefault(password); // Call default overload

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

        /// <summary>
        /// Encrypts text using AES-GCM with password-based key derivation
        /// </summary>
        /// <param name="plainText">Text to encrypt</param>
        /// <param name="password">Password for key derivation</param>
        /// <param name="iterations">PBKDF2 iteration count (optional, default: 2000)</param>
        /// <returns>Base64 encoded encrypted data with salt, nonce, and tag</returns>
        [SqlFunction(
            IsDeterministic = false, // Not deterministic due to random salt/nonce generation
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        [SecuritySafeCritical]
        public static SqlString EncryptAesGcmWithPasswordIterations(SqlString plainText, SqlString password, SqlInt32 iterations)
        {
            if (plainText.IsNull || password.IsNull)
                return SqlString.Null;

            try
            {
                int iterationCount = iterations.IsNull ? 2000 : iterations.Value;
                
                // Validate iteration count
                if (iterationCount < 1000 || iterationCount > 100000)
                    throw new ArgumentException("Iteration count must be between 1000 and 100000", "iterations");

                string encrypted = BcryptInterop.EncryptAesGcmWithPassword(plainText.Value, password.Value, null, iterationCount);
                
                if (string.IsNullOrEmpty(encrypted))
                    throw new CryptographicException("Encryption returned null or empty");

                return new SqlString(encrypted);
            }
            catch (Exception)
            {
                return SqlString.Null;
            }
        }

        /// <summary>
        /// Encrypts text using AES-GCM with password-based key derivation (default iterations)
        /// </summary>
        /// <param name="plainText">Text to encrypt</param>
        /// <param name="password">Password for key derivation</param>
        /// <returns>Base64 encoded encrypted data with salt, nonce, and tag</returns>
        [SqlFunction(
            IsDeterministic = false, // Not deterministic due to random salt/nonce generation
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        [SecuritySafeCritical]
        public static SqlString EncryptAesGcmWithPassword(SqlString plainText, SqlString password)
        {
            return EncryptAesGcmWithPasswordIterations(plainText, password, SqlInt32.Null);
        }

        /// <summary>
        /// Decrypts text using AES-GCM with password-based key derivation
        /// </summary>
        /// <param name="base64EncryptedData">Base64 encoded encrypted data</param>
        /// <param name="password">Password for key derivation</param>
        /// <param name="iterations">PBKDF2 iteration count (optional, default: 2000)</param>
        /// <returns>Decrypted text</returns>
        [SqlFunction(
            IsDeterministic = true,
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        [SecuritySafeCritical]
        public static SqlString DecryptAesGcmWithPasswordIterations(SqlString base64EncryptedData, SqlString password, SqlInt32 iterations)
        {
            if (base64EncryptedData.IsNull || password.IsNull)
                return SqlString.Null;

            try
            {
                int iterationCount = iterations.IsNull ? 2000 : iterations.Value;
                
                // Validate iteration count
                if (iterationCount < 1000 || iterationCount > 100000)
                    throw new ArgumentException("Iteration count must be between 1000 and 100000", "iterations");

                string decrypted = BcryptInterop.DecryptAesGcmWithPassword(base64EncryptedData.Value, password.Value, iterationCount);
                
                if (decrypted == null)
                    throw new CryptographicException("Decryption returned null");

                return new SqlString(decrypted);
            }
            catch (Exception)
            {
                return SqlString.Null;
            }
        }

        /// <summary>
        /// Decrypts text using AES-GCM with password-based key derivation (default iterations)
        /// </summary>
        /// <param name="base64EncryptedData">Base64 encoded encrypted data</param>
        /// <param name="password">Password for key derivation</param>
        /// <returns>Decrypted text</returns>
        [SqlFunction(
            IsDeterministic = true,
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        [SecuritySafeCritical]
        public static SqlString DecryptAesGcmWithPassword(SqlString base64EncryptedData, SqlString password)
        {
            return DecryptAesGcmWithPasswordIterations(base64EncryptedData, password, SqlInt32.Null);
        }

        /// <summary>
        /// Generates a cryptographically secure random salt for key derivation
        /// </summary>
        /// <param name="saltLength">Length of salt in bytes (optional, default: 16)</param>
        /// <returns>Base64 encoded salt</returns>
        [SqlFunction(
            IsDeterministic = false, // Not deterministic due to random generation
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        [SecuritySafeCritical]
        public static SqlString GenerateSaltWithLength(SqlInt32 saltLength)
        {
            try
            {
                int length = saltLength.IsNull ? 16 : saltLength.Value;
                
                // Validate salt length
                if (length < 8 || length > 64)
                    throw new ArgumentException("Salt length must be between 8 and 64 bytes", "saltLength");

                byte[] salt = new byte[length];
                using (var rng = new RNGCryptoServiceProvider())
                {
                    rng.GetBytes(salt);
                }
                
                return new SqlString(Convert.ToBase64String(salt));
            }
            catch (Exception)
            {
                return SqlString.Null;
            }
        }

        /// <summary>
        /// Generates a cryptographically secure random salt (default 16 bytes)
        /// </summary>
        /// <returns>Base64 encoded salt</returns>
        [SqlFunction(
            IsDeterministic = false, // Not deterministic due to random generation
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        [SecuritySafeCritical]
        public static SqlString GenerateSalt()
        {
            return GenerateSaltWithLength(SqlInt32.Null);
        }

        /// <summary>
        /// Encrypts text using AES-GCM with password and custom salt
        /// </summary>
        /// <param name="plainText">Text to encrypt</param>
        /// <param name="password">Password for key derivation</param>
        /// <param name="base64Salt">Base64 encoded salt for key derivation</param>
        /// <param name="iterations">PBKDF2 iteration count (optional, default: 2000)</param>
        /// <returns>Base64 encoded encrypted data with salt, nonce, and tag</returns>
        [SqlFunction(
            IsDeterministic = false, // Not deterministic due to random nonce generation
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        [SecuritySafeCritical]
        public static SqlString EncryptAesGcmWithPasswordAndSaltIterations(SqlString plainText, SqlString password, SqlString base64Salt, SqlInt32 iterations)
        {
            if (plainText.IsNull || password.IsNull || base64Salt.IsNull)
                return SqlString.Null;

            byte[] saltBytes = null;
            try
            {
                saltBytes = Convert.FromBase64String(base64Salt.Value);
                
                // Validate salt length
                if (saltBytes.Length < 8 || saltBytes.Length > 64)
                    throw new ArgumentException("Salt length must be between 8 and 64 bytes", "base64Salt");

                int iterationCount = iterations.IsNull ? 2000 : iterations.Value;
                
                // Validate iteration count
                if (iterationCount < 1000 || iterationCount > 100000)
                    throw new ArgumentException("Iteration count must be between 1000 and 100000", "iterations");

                string encrypted = BcryptInterop.EncryptAesGcmWithPassword(plainText.Value, password.Value, saltBytes, iterationCount);
                
                if (string.IsNullOrEmpty(encrypted))
                    throw new CryptographicException("Encryption returned null or empty");

                return new SqlString(encrypted);
            }
            catch (Exception)
            {
                return SqlString.Null;
            }
            finally
            {
                if (saltBytes != null) Array.Clear(saltBytes, 0, saltBytes.Length);
            }
        }

        /// <summary>
        /// Encrypts text using AES-GCM with password and custom salt (default iterations)
        /// </summary>
        /// <param name="plainText">Text to encrypt</param>
        /// <param name="password">Password for key derivation</param>
        /// <param name="base64Salt">Base64 encoded salt for key derivation</param>
        /// <returns>Base64 encoded encrypted data with salt, nonce, and tag</returns>
        [SqlFunction(
            IsDeterministic = false, // Not deterministic due to random nonce generation
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        [SecuritySafeCritical]
        public static SqlString EncryptAesGcmWithPasswordAndSalt(SqlString plainText, SqlString password, SqlString base64Salt)
        {
            return EncryptAesGcmWithPasswordAndSaltIterations(plainText, password, base64Salt, SqlInt32.Null);
        }

        /// <summary>
        /// Derives an AES-256 key from a password using PBKDF2. 
        /// This key can be cached and reused for multiple encrypt/decrypt operations for performance.
        /// </summary>
        /// <param name="password">Password for key derivation</param>
        /// <param name="base64Salt">Base64 encoded salt for key derivation</param>
        /// <param name="iterations">PBKDF2 iteration count (default: 2000)</param>
        /// <returns>Base64 encoded 32-byte AES key that can be cached and reused</returns>
        [SqlFunction(
            IsDeterministic = true,
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        [SecuritySafeCritical]
        public static SqlString DeriveKeyFromPasswordIterations(SqlString password, SqlString base64Salt, SqlInt32 iterations)
        {
            if (password.IsNull || base64Salt.IsNull)
                return SqlString.Null;

            byte[] saltBytes = null;
            try
            {
                saltBytes = Convert.FromBase64String(base64Salt.Value);
                
                // Validate salt length
                if (saltBytes.Length < 8 || saltBytes.Length > 64)
                    throw new ArgumentException("Salt length must be between 8 and 64 bytes", "base64Salt");

                int iterationCount = iterations.IsNull ? 2000 : iterations.Value;
                
                // Validate iteration count
                if (iterationCount < 1000 || iterationCount > 100000)
                    throw new ArgumentException("Iteration count must be between 1000 and 100000", "iterations");

                byte[] key;
                using (var pbkdf2 = new Rfc2898DeriveBytes(password.Value, saltBytes, iterationCount, HashAlgorithmName.SHA256))
                {
                    key = pbkdf2.GetBytes(32);
                }

                string result = Convert.ToBase64String(key);
                
                // Clear sensitive data
                Array.Clear(key, 0, key.Length);
                
                return new SqlString(result);
            }
            catch (Exception)
            {
                return SqlString.Null;
            }
            finally
            {
                if (saltBytes != null) Array.Clear(saltBytes, 0, saltBytes.Length);
            }
        }

        /// <summary>
        /// Derives an AES-256 key from a password using PBKDF2 (default iterations)
        /// </summary>
        /// <param name="password">Password for key derivation</param>
        /// <param name="base64Salt">Base64 encoded salt for key derivation</param>
        /// <returns>Base64 encoded 32-byte AES key that can be cached and reused</returns>
        [SqlFunction(
            IsDeterministic = true,
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        [SecuritySafeCritical]
        public static SqlString DeriveKeyFromPassword(SqlString password, SqlString base64Salt)
        {
            return DeriveKeyFromPasswordIterations(password, base64Salt, SqlInt32.Null);
        }

        /// <summary>
        /// Encrypts text using AES-GCM with a pre-derived key. 
        /// This method produces the same output format as EncryptAesGcmWithPassword but avoids key derivation overhead.
        /// Use DeriveKeyFromPassword to get the key first, then cache and reuse it for multiple operations.
        /// </summary>
        /// <param name="plainText">Text to encrypt</param>
        /// <param name="base64DerivedKey">Base64 encoded 32-byte AES key from DeriveKeyFromPassword</param>
        /// <param name="base64Salt">Base64 encoded salt used for key derivation (needed for output format compatibility)</param>
        /// <returns>Base64 encoded encrypted data with salt, nonce, and tag (same format as password-based methods)</returns>
        [SqlFunction(
            IsDeterministic = false, // Not deterministic due to random nonce generation
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        [SecuritySafeCritical]
        /// <summary>
        /// Encrypts text using AES-GCM with a pre-derived key and the *same* salt that was used to derive the key.
        /// The salt parameter here must be exactly the same as the one used in DeriveKeyFromPassword/DeriveKeyFromPasswordIterations.
        /// Do NOT use a random salt here; using a different salt will make decryption impossible.
        /// The salt is included in the output for compatibility with password-based encryption formats.
        /// </summary>
        /// <param name="plainText">Text to encrypt</param>
        /// <param name="base64DerivedKey">Base64 encoded 32-byte AES key from DeriveKeyFromPassword</param>
        /// <param name="base64Salt">Base64 encoded salt used for key derivation (must be the same as used to derive the key)</param>
        /// <returns>Base64 encoded encrypted data with salt, nonce, and tag</returns>
        public static SqlString EncryptAesGcmWithDerivedKey(SqlString plainText, SqlString base64DerivedKey, SqlString base64Salt)
        {
            // The salt provided here MUST be the same as the one used to derive the key.
            // Do NOT use a random salt for this function.
            if (plainText.IsNull || base64DerivedKey.IsNull || base64Salt.IsNull)
                return SqlString.Null;

            byte[] key = null;
            byte[] saltBytes = null;
            try
            {
                key = Convert.FromBase64String(base64DerivedKey.Value);
                saltBytes = Convert.FromBase64String(base64Salt.Value);

                // Validate key length
                if (key.Length != 32)
                    throw new ArgumentException("Derived key must be 32 bytes", "base64DerivedKey");

                // Validate salt length
                if (saltBytes.Length < 8 || saltBytes.Length > 64)
                    throw new ArgumentException("Salt length must be between 8 and 64 bytes", "base64Salt");

                // The salt is included in the output for compatibility, but must match the key derivation salt.
                string encrypted = BcryptInterop.EncryptAesGcmWithDerivedKey(plainText.Value, key, saltBytes);

                if (string.IsNullOrEmpty(encrypted))
                    throw new CryptographicException("Encryption returned null or empty");

                return new SqlString(encrypted);
            }
            catch (Exception)
            {
                return SqlString.Null;
            }
            finally
            {
                if (key != null) Array.Clear(key, 0, key.Length);
                if (saltBytes != null) Array.Clear(saltBytes, 0, saltBytes.Length);
            }
        }

        /// <summary>
        /// Decrypts text using AES-GCM with a pre-derived key.
        /// This method can decrypt data encrypted with either EncryptAesGcmWithPassword or EncryptAesGcmWithDerivedKey.
        /// </summary>
        /// <param name="base64EncryptedData">Base64 encoded encrypted data</param>
        /// <param name="base64DerivedKey">Base64 encoded 32-byte AES key from DeriveKeyFromPassword</param>
        /// <returns>Decrypted text</returns>
        [SqlFunction(
            IsDeterministic = true,
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        [SecuritySafeCritical]
        public static SqlString DecryptAesGcmWithDerivedKey(SqlString base64EncryptedData, SqlString base64DerivedKey)
        {
            if (base64EncryptedData.IsNull || base64DerivedKey.IsNull)
                return SqlString.Null;

            byte[] key = null;
            try
            {
                key = Convert.FromBase64String(base64DerivedKey.Value);
                
                // Validate key length
                if (key.Length != 32)
                    throw new ArgumentException("Derived key must be 32 bytes", "base64DerivedKey");

                string decrypted = BcryptInterop.DecryptAesGcmWithDerivedKey(base64EncryptedData.Value, key);
                
                if (decrypted == null)
                    throw new CryptographicException("Decryption returned null");

                return new SqlString(decrypted);
            }
            catch (Exception)
            {
                return SqlString.Null;
            }
            finally
            {
                if (key != null) Array.Clear(key, 0, key.Length);
            }
        }

        // ======================================================================================
        // PASSWORD-BASED TABLE ENCRYPTION/DECRYPTION FUNCTIONS
        // ======================================================================================

        private const int DefaultIterations = 2000; // optimal for password-based encryption for blob or else large data

        /// <summary>
        /// Encrypts an XML document using a password. The salt and nonce are generated automatically
        /// and stored in the output. Legacy version without schema metadata embedding.
        /// </summary>
        [SqlFunction(IsDeterministic = false, IsPrecise = true, DataAccess = DataAccessKind.None)]
        [SecuritySafeCritical]
        public static SqlString EncryptXmlWithPassword(SqlXml xmlData, SqlString password)
        {
            return EncryptXmlWithPasswordIterations(xmlData, password, DefaultIterations);
        }

        /// <summary>
        /// Encrypts an XML document using a password and a specific iteration count for key derivation.
        /// Legacy version without schema metadata embedding.
        /// </summary>
        [SqlFunction(IsDeterministic = false, IsPrecise = true, DataAccess = DataAccessKind.None)]
        [SecuritySafeCritical]
        public static SqlString EncryptXmlWithPasswordIterations(SqlXml xmlData, SqlString password, SqlInt32 iterations)
        {
            if (xmlData.IsNull || password.IsNull)
                return SqlString.Null;

            try
            {
                int iterationCount = iterations.IsNull ? DefaultIterations : iterations.Value;
                // Use the BcryptInterop class to handle the encryption details.
                string encrypted = BcryptInterop.EncryptAesGcmWithPassword(xmlData.Value, password.Value, null, iterationCount);

                if (string.IsNullOrEmpty(encrypted))
                    throw new CryptographicException("Encryption returned null or empty.");

                return new SqlString(encrypted);
            }
            catch (Exception)
            {
                return SqlString.Null;
            }
        }

        /// <summary>
        /// Encrypts table data with embedded schema metadata for zero-cast decryption.
        /// This enhanced version embeds complete column information (name, type, nullable, maxLength, etc.)
        /// in the encrypted package, making it self-describing for the sophisticated TVF.
        /// </summary>
        [SqlFunction(IsDeterministic = false, IsPrecise = true, DataAccess = DataAccessKind.Read)]
        [SecuritySafeCritical]
        public static SqlString EncryptTableWithMetadata(SqlString tableName, SqlString password)
        {
            return EncryptTableWithMetadataIterations(tableName, password, DefaultIterations);
        }

        /// <summary>
        /// Encrypts table data with embedded schema metadata and specific iteration count.
        /// Automatically queries INFORMATION_SCHEMA to get complete column information.
        /// </summary>
        [SqlFunction(IsDeterministic = false, IsPrecise = true, DataAccess = DataAccessKind.Read)]
        [SecuritySafeCritical]
        public static SqlString EncryptTableWithMetadataIterations(SqlString tableName, SqlString password, SqlInt32 iterations)
        {
            if (tableName.IsNull || password.IsNull)
                return SqlString.Null;

            try
            {
                int iterationCount = iterations.IsNull ? DefaultIterations : iterations.Value;

                // Build the metadata-enhanced XML package
                string enhancedXml = XmlMetadataHandler.BuildMetadataEnhancedXml(tableName.Value);
                if (string.IsNullOrEmpty(enhancedXml))
                {
                    throw new Exception("Failed to build metadata-enhanced XML - returned null or empty.");
                }

                // Check if the XML contains an error
                if (enhancedXml.Contains("<Error>"))
                {
                    throw new Exception($"Error building metadata: {enhancedXml}");
                }

                // Debug: Log the XML being encrypted (first 500 characters)
                string debugXml = enhancedXml.Length > 500 ? enhancedXml.Substring(0, 500) + "..." : enhancedXml;
                SqlContext.Pipe.Send($"Debug: XML to encrypt (first 500 chars): {debugXml}");

                // Encrypt the enhanced package
                string encrypted = BcryptInterop.EncryptAesGcmWithPassword(enhancedXml, password.Value, null, iterationCount);

                if (string.IsNullOrEmpty(encrypted))
                    throw new CryptographicException("Encryption returned null or empty.");

                return new SqlString(encrypted);
            }
            catch (Exception ex)
            {
                SqlContext.Pipe.Send($"Error in EncryptTableWithMetadataIterations: {ex.Message}");
                return SqlString.Null;
            }
        }

        /// <summary>
        /// Encrypts XML data with embedded schema metadata extracted from the XML structure.
        /// Use this when you have XML data but want to embed inferred schema information.
        /// </summary>
        [SqlFunction(IsDeterministic = false, IsPrecise = true, DataAccess = DataAccessKind.None)]
        [SecuritySafeCritical]
        public static SqlString EncryptXmlWithMetadata(SqlXml xmlData, SqlString password)
        {
            return EncryptXmlWithMetadataIterations(xmlData, password, DefaultIterations);
        }

        /// <summary>
        /// Encrypts XML data with embedded schema metadata and specific iteration count.
        /// Infers column types from XML data patterns.
        /// </summary>
        [SqlFunction(IsDeterministic = false, IsPrecise = true, DataAccess = DataAccessKind.None)]
        [SecuritySafeCritical]
        public static SqlString EncryptXmlWithMetadataIterations(SqlXml xmlData, SqlString password, SqlInt32 iterations)
        {
            if (xmlData.IsNull || password.IsNull)
                return SqlString.Null;

            try
            {
                int iterationCount = iterations.IsNull ? DefaultIterations : iterations.Value;

                // Build metadata-enhanced XML from existing XML data
                string enhancedXml = XmlMetadataHandler.BuildMetadataEnhancedXmlFromData(xmlData.Value);
                if (string.IsNullOrEmpty(enhancedXml))
                    return SqlString.Null;

                // Encrypt the enhanced package
                string encrypted = BcryptInterop.EncryptAesGcmWithPassword(enhancedXml, password.Value, null, iterationCount);

                if (string.IsNullOrEmpty(encrypted))
                    throw new CryptographicException("Encryption returned null or empty.");

                return new SqlString(encrypted);
            }
            catch (Exception)
            {
                return SqlString.Null;
            }
        }

        // XML and metadata handling methods moved to XmlMetadataHandler.cs

        /// <summary>
        /// Decrypts data encrypted with a password and restores it to a fully structured result set with automatic type casting.
        /// This enhanced version reads embedded metadata to automatically cast columns to their proper types.
        /// </summary>
        [SqlProcedure]
        [SecuritySafeCritical]
        public static void DecryptTableWithMetadata(SqlString encryptedData, SqlString password)
        {
            if (encryptedData.IsNull || password.IsNull)
            {
                SqlContext.Pipe.Send("Error: Encrypted data or password is null.");
                return;
            }

            try
            {
                // 1. Decrypt the XML string
                string decryptedXml = BcryptInterop.DecryptAesGcmWithPassword(encryptedData.Value, password.Value, DefaultIterations);
                if (decryptedXml == null)
                {
                    SqlContext.Pipe.Send("Decryption returned null. Check password or data integrity.");
                    return;
                }

                // Debug: Log the decrypted XML (first 500 characters)
                string debugXml = decryptedXml.Length > 500 ? decryptedXml.Substring(0, 500) + "..." : decryptedXml;
                SqlContext.Pipe.Send($"Debug: Decrypted XML (first 500 chars): {debugXml}");

                // 2. Check for error in XML
                if (decryptedXml.Contains("<Error>"))
                {
                    SqlContext.Pipe.Send($"Error in encrypted data: {decryptedXml}");
                    return;
                }

                // 3. Validate XML structure
                var (isValid, errorMessage) = XmlMetadataHandler.ValidateXmlStructure(decryptedXml);
                if (!isValid)
                {
                    SqlContext.Pipe.Send($"XML validation failed: {errorMessage}");
                    return;
                }

                // 4. Parse the XML document to extract metadata and column information
                var doc = XDocument.Parse(decryptedXml);
                
                // FIXED: Use universal parsing approach
                var columns = XmlMetadataHandler.ParseColumnsFromXml(doc.Root);

                if (columns.Count == 0)
                {
                    SqlContext.Pipe.Send("No columns found in decrypted XML data.");
                    SqlContext.Pipe.Send($"Debug: Root element name: {doc.Root?.Name}");
                    SqlContext.Pipe.Send($"Debug: Root element children count: {doc.Root?.Elements().Count() ?? 0}");
                    
                    // Try to show what's in the XML for debugging
                    var metadataElement = doc.Root?.Element("Metadata");
                    if (metadataElement != null)
                    {
                        SqlContext.Pipe.Send($"Debug: Found Metadata element with {metadataElement.Elements().Count()} children");
                    }
                    
                    var dataRows = doc.Root?.Elements("Row").ToList();
                    if (dataRows != null && dataRows.Count > 0)
                    {
                        SqlContext.Pipe.Send($"Debug: Found {dataRows.Count} Row elements");
                        var firstRow = dataRows.First();
                        SqlContext.Pipe.Send($"Debug: First row attributes: {firstRow.Attributes().Count()}");
                        SqlContext.Pipe.Send($"Debug: First row elements: {firstRow.Elements().Count()}");
                    }
                    
                    return;
                }

                SqlContext.Pipe.Send($"Debug: Found {columns.Count} columns: " + string.Join(", ", columns.Select(c => c.Name)));

                // 5. Build the Dynamic SQL with automatic type casting
                var sql = new StringBuilder();
                var columnExpressions = new List<string>();

                foreach (var column in columns)
                {
                    string columnExpression = XmlMetadataHandler.BuildColumnExpression(column);
                    columnExpressions.Add(columnExpression);
                }

                var columnsClause = string.Join(", ", columnExpressions);
                
                sql.AppendLine("DECLARE @xml XML;");
                sql.AppendLine("SET @xml = @p_xml;");
                sql.AppendLine("SELECT");
                sql.AppendLine(columnsClause);
                sql.AppendLine("FROM @xml.nodes('/Root/Row') AS T(c);");

                // Debug: Show the generated SQL
                SqlContext.Pipe.Send($"Debug: Generated SQL: {sql.ToString()}");

                // 6. Execute the command and send the results to the client
                using (var connection = new System.Data.SqlClient.SqlConnection("context connection=true"))
                {
                    connection.Open();
                    using (var command = new System.Data.SqlClient.SqlCommand(sql.ToString(), connection))
                    {
                        command.Parameters.Add(new System.Data.SqlClient.SqlParameter("@p_xml", System.Data.SqlDbType.Xml) { Value = decryptedXml });
                        SqlContext.Pipe.ExecuteAndSend(command);
                    }
                }
            }
            catch (Exception ex)
            {
                SqlContext.Pipe.Send($"Error in DecryptTableWithMetadata: {ex.Message}");
                SqlContext.Pipe.Send($"Stack trace: {ex.StackTrace}");
            }
        }

        // Dynamic temp-table wrapper methods moved to DynamicTempTableWrapper.cs

        // FIXED: BuildColumnExpression method moved to XmlMetadataHandler.cs for better organization
        // and universal XML parsing support
    }
}