using System;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Security.Cryptography;
using System.Text;
using System.Security;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Data;
using System.Data.SqlClient;
using SecureLibrary.SQL.Services;
using SecureLibrary.SQL.Interfaces;

namespace SecureLibrary.SQL
{
    /// <summary>
    /// SQL Server CLR Functions for cryptographic operations
    /// Provides T-SQL accessible functions for encryption, decryption, and password hashing
    /// </summary>
    [SqlUserDefinedType(Format.Native)]
    public class SqlCLRFunctions
    {
        private static readonly ICgnService _cgnService;
        private static readonly IEncryptionEngine _encryptionEngine;
        private static readonly ISqlXmlConverter _xmlConverter;
        private static readonly IPasswordHashingService _passwordHashingService;

        static SqlCLRFunctions()
        {
            try
            {
                _cgnService = new CgnService();
                _xmlConverter = new SqlXmlConverter();
                _encryptionEngine = new EncryptionEngine(_cgnService, _xmlConverter);
                _passwordHashingService = new BcryptPasswordHashingService();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize CLR services: {ex.Message}", ex);
            }
        }

        #region Password Hashing Functions

        /// <summary>
        /// Hashes a password using Bcrypt with default work factor (12)
        /// </summary>
        /// <param name="password">Password to hash</param>
        /// <returns>Hashed password string</returns>
        [SqlFunction(
            IsDeterministic = false, // Not deterministic due to random salt
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        [SecuritySafeCritical]
        public static SqlString HashPassword(SqlString password)
        {
            if (password.IsNull)
                return SqlString.Null;

            try
            {
                var hashedPassword = _passwordHashingService.HashPassword(password.Value);
                return new SqlString(hashedPassword);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Password hashing failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Hashes a password using Bcrypt with specified work factor
        /// </summary>
        /// <param name="password">Password to hash</param>
        /// <param name="workFactor">Bcrypt work factor (4-31)</param>
        /// <returns>Hashed password string</returns>
        [SqlFunction(
            IsDeterministic = false, // Not deterministic due to random salt
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        [SecuritySafeCritical]
        public static SqlString HashPasswordWithWorkFactor(SqlString password, SqlInt32 workFactor)
        {
            if (password.IsNull || workFactor.IsNull)
                return SqlString.Null;

            try
            {
                var hashedPassword = _passwordHashingService.HashPassword(password.Value, workFactor.Value);
                return new SqlString(hashedPassword);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Password hashing failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Verifies a password against a hashed password
        /// </summary>
        /// <param name="password">Password to verify</param>
        /// <param name="hashedPassword">Hashed password to verify against</param>
        /// <returns>True if password matches, false otherwise</returns>
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

            try
            {
                var isValid = _passwordHashingService.VerifyPassword(password.Value, hashedPassword.Value);
                return new SqlBoolean(isValid);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Password verification failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Generates a salt for password hashing
        /// </summary>
        /// <param name="workFactor">Work factor for salt generation</param>
        /// <returns>Generated salt string</returns>
        [SqlFunction(
            IsDeterministic = false, // Not deterministic due to random generation
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        [SecuritySafeCritical]
        public static SqlString GenerateSalt(SqlInt32 workFactor)
        {
            if (workFactor.IsNull)
                return SqlString.Null;

            try
            {
                var salt = _passwordHashingService.GenerateSalt(workFactor.Value);
                return new SqlString(salt);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Salt generation failed: {ex.Message}");
            }
        }



        #endregion

        #region AES-GCM Encryption Functions

        /// <summary>
        /// Encrypts data using AES-GCM with a provided key
        /// </summary>
        /// <param name="plainText">Text to encrypt</param>
        /// <param name="base64Key">Base64 encoded 32-byte AES key</param>
        /// <returns>Base64 encoded encrypted data with nonce and tag</returns>
        [SqlFunction(
            IsDeterministic = false, // Not deterministic due to random nonce
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        [SecuritySafeCritical]
        public static SqlString EncryptAesGcm(SqlString plainText, SqlString base64Key)
        {
            if (plainText.IsNull || base64Key.IsNull)
                return SqlString.Null;

            try
            {
                byte[] key = Convert.FromBase64String(base64Key.Value);
                byte[] nonce = _cgnService.GenerateNonce(12); // 12 bytes for AES-GCM
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText.Value);

                byte[] encryptedData = _cgnService.EncryptAesGcm(plainBytes, key, nonce);

                // Clear sensitive data
                Array.Clear(key, 0, key.Length);
                Array.Clear(plainBytes, 0, plainBytes.Length);

                return new SqlString(Convert.ToBase64String(encryptedData));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"AES-GCM encryption failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Decrypts data using AES-GCM with a provided key
        /// </summary>
        /// <param name="base64EncryptedData">Base64 encoded encrypted data</param>
        /// <param name="base64Key">Base64 encoded 32-byte AES key</param>
        /// <returns>Decrypted text</returns>
        [SqlFunction(
            IsDeterministic = true,
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        [SecuritySafeCritical]
        public static SqlString DecryptAesGcm(SqlString base64EncryptedData, SqlString base64Key)
        {
            if (base64EncryptedData.IsNull || base64Key.IsNull)
                return SqlString.Null;

            try
            {
                byte[] key = Convert.FromBase64String(base64Key.Value);
                byte[] encryptedData = Convert.FromBase64String(base64EncryptedData.Value);

                // Extract nonce from the encrypted data (first 12 bytes)
                if (encryptedData.Length < 28) // 12 bytes nonce + 16 bytes tag minimum
                    throw new ArgumentException("Encrypted data too short");

                byte[] nonce = new byte[12];
                Array.Copy(encryptedData, 0, nonce, 0, 12);

                // The actual encrypted data starts after the nonce
                byte[] cipherData = new byte[encryptedData.Length - 12];
                Array.Copy(encryptedData, 12, cipherData, 0, cipherData.Length);

                byte[] decryptedBytes = _cgnService.DecryptAesGcm(cipherData, key, nonce);
                string decryptedText = Encoding.UTF8.GetString(decryptedBytes);

                // Clear sensitive data
                Array.Clear(key, 0, key.Length);
                Array.Clear(encryptedData, 0, encryptedData.Length);
                Array.Clear(decryptedBytes, 0, decryptedBytes.Length);

                return new SqlString(decryptedText);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"AES-GCM decryption failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Encrypts data using AES-GCM with password-based key derivation
        /// </summary>
        /// <param name="plainText">Text to encrypt</param>
        /// <param name="password">Password for key derivation</param>
        /// <param name="iterations">Number of iterations for key derivation</param>
        /// <returns>Base64 encoded encrypted data with salt, nonce, and tag</returns>
        [SqlFunction(
            IsDeterministic = false, // Not deterministic due to random salt and nonce
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        [SecuritySafeCritical]
        public static SqlString EncryptAesGcmWithPassword(SqlString plainText, SqlString password, SqlInt32 iterations)
        {
            if (plainText.IsNull || password.IsNull || iterations.IsNull)
                return SqlString.Null;

            try
            {
                byte[] salt = _cgnService.GenerateNonce(32); // 32 bytes salt
                byte[] nonce = _cgnService.GenerateNonce(12); // 12 bytes nonce
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText.Value);

                // Derive key from password
                byte[] key = _cgnService.DeriveKeyFromPassword(password.Value, salt, iterations.Value, 32);

                // Encrypt data
                byte[] encryptedData = _cgnService.EncryptAesGcm(plainBytes, key, nonce);

                // Combine salt + nonce + encrypted data
                byte[] result = new byte[salt.Length + nonce.Length + encryptedData.Length];
                Array.Copy(salt, 0, result, 0, salt.Length);
                Array.Copy(nonce, 0, result, salt.Length, nonce.Length);
                Array.Copy(encryptedData, 0, result, salt.Length + nonce.Length, encryptedData.Length);

                // Clear sensitive data
                Array.Clear(key, 0, key.Length);
                Array.Clear(plainBytes, 0, plainBytes.Length);

                return new SqlString(Convert.ToBase64String(result));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Password-based AES-GCM encryption failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Decrypts data using AES-GCM with password-based key derivation
        /// </summary>
        /// <param name="base64EncryptedData">Base64 encoded encrypted data</param>
        /// <param name="password">Password for key derivation</param>
        /// <param name="iterations">Number of iterations for key derivation</param>
        /// <returns>Decrypted text</returns>
        [SqlFunction(
            IsDeterministic = true,
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        [SecuritySafeCritical]
        public static SqlString DecryptAesGcmWithPassword(SqlString base64EncryptedData, SqlString password, SqlInt32 iterations)
        {
            if (base64EncryptedData.IsNull || password.IsNull || iterations.IsNull)
                return SqlString.Null;

            try
            {
                byte[] encryptedData = Convert.FromBase64String(base64EncryptedData.Value);

                if (encryptedData.Length < 60) // 32 bytes salt + 12 bytes nonce + 16 bytes tag minimum
                    throw new ArgumentException("Encrypted data too short");

                // Extract salt, nonce, and cipher data
                byte[] salt = new byte[32];
                byte[] nonce = new byte[12];
                byte[] cipherData = new byte[encryptedData.Length - 44]; // 32 + 12

                Array.Copy(encryptedData, 0, salt, 0, 32);
                Array.Copy(encryptedData, 32, nonce, 0, 12);
                Array.Copy(encryptedData, 44, cipherData, 0, cipherData.Length);

                // Derive key from password
                byte[] key = _cgnService.DeriveKeyFromPassword(password.Value, salt, iterations.Value, 32);

                // Decrypt data
                byte[] decryptedBytes = _cgnService.DecryptAesGcm(cipherData, key, nonce);
                string decryptedText = Encoding.UTF8.GetString(decryptedBytes);

                // Clear sensitive data
                Array.Clear(key, 0, key.Length);
                Array.Clear(encryptedData, 0, encryptedData.Length);
                Array.Clear(decryptedBytes, 0, decryptedBytes.Length);

                return new SqlString(decryptedText);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Password-based AES-GCM decryption failed: {ex.Message}");
            }
        }

        #endregion

        #region Key Generation Functions

        /// <summary>
        /// Generates a cryptographically secure random key
        /// </summary>
        /// <param name="keySizeBits">Size of the key in bits</param>
        /// <returns>Base64 encoded key</returns>
        [SqlFunction(
            IsDeterministic = false, // Not deterministic due to random generation
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        [SecuritySafeCritical]
        public static SqlString GenerateKey(SqlInt32 keySizeBits)
        {
            if (keySizeBits.IsNull)
                return SqlString.Null;

            try
            {
                byte[] key = _cgnService.GenerateKey(keySizeBits.Value);
                return new SqlString(Convert.ToBase64String(key));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Key generation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates a cryptographically secure random nonce/IV
        /// </summary>
        /// <param name="nonceSizeBytes">Size of the nonce in bytes</param>
        /// <returns>Base64 encoded nonce</returns>
        [SqlFunction(
            IsDeterministic = false, // Not deterministic due to random generation
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        [SecuritySafeCritical]
        public static SqlString GenerateNonce(SqlInt32 nonceSizeBytes)
        {
            if (nonceSizeBytes.IsNull)
                return SqlString.Null;

            try
            {
                byte[] nonce = _cgnService.GenerateNonce(nonceSizeBytes.Value);
                return new SqlString(Convert.ToBase64String(nonce));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Nonce generation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Derives a key from a password using PBKDF2
        /// </summary>
        /// <param name="password">Password to derive key from</param>
        /// <param name="base64Salt">Base64 encoded salt</param>
        /// <param name="iterations">Number of iterations</param>
        /// <param name="keySizeBytes">Size of the derived key in bytes</param>
        /// <returns>Base64 encoded derived key</returns>
        [SqlFunction(
            IsDeterministic = true,
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        [SecuritySafeCritical]
        public static SqlString DeriveKeyFromPassword(SqlString password, SqlString base64Salt, SqlInt32 iterations, SqlInt32 keySizeBytes)
        {
            if (password.IsNull || base64Salt.IsNull || iterations.IsNull || keySizeBytes.IsNull)
                return SqlString.Null;

            try
            {
                byte[] salt = Convert.FromBase64String(base64Salt.Value);
                byte[] key = _cgnService.DeriveKeyFromPassword(password.Value, salt, iterations.Value, keySizeBytes.Value);
                return new SqlString(Convert.ToBase64String(key));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Key derivation failed: {ex.Message}");
            }
        }

        #endregion

        #region XML Encryption Functions

        /// <summary>
        /// Encrypts XML data with password-based key derivation
        /// </summary>
        /// <param name="xmlData">XML data to encrypt</param>
        /// <param name="password">Password for key derivation</param>
        /// <param name="iterations">Number of iterations for key derivation</param>
        /// <returns>Base64 encoded encrypted XML data</returns>
        [SqlFunction(
            IsDeterministic = false, // Not deterministic due to random salt and nonce
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        [SecuritySafeCritical]
        public static SqlString EncryptXml(SqlXml xmlData, SqlString password, SqlInt32 iterations)
        {
            if (xmlData.IsNull || password.IsNull || iterations.IsNull)
                return SqlString.Null;

            try
            {
                string xmlString = xmlData.Value;
                byte[] salt = _cgnService.GenerateNonce(32);
                byte[] nonce = _cgnService.GenerateNonce(12);
                byte[] xmlBytes = Encoding.UTF8.GetBytes(xmlString);

                // Derive key from password
                byte[] key = _cgnService.DeriveKeyFromPassword(password.Value, salt, iterations.Value, 32);

                // Encrypt XML data
                byte[] encryptedData = _cgnService.EncryptAesGcm(xmlBytes, key, nonce);

                // Combine salt + nonce + encrypted data
                byte[] result = new byte[salt.Length + nonce.Length + encryptedData.Length];
                Array.Copy(salt, 0, result, 0, salt.Length);
                Array.Copy(nonce, 0, result, salt.Length, nonce.Length);
                Array.Copy(encryptedData, 0, result, salt.Length + nonce.Length, encryptedData.Length);

                // Clear sensitive data
                Array.Clear(key, 0, key.Length);
                Array.Clear(xmlBytes, 0, xmlBytes.Length);

                return new SqlString(Convert.ToBase64String(result));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"XML encryption failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Decrypts XML data with password-based key derivation
        /// </summary>
        /// <param name="base64EncryptedXml">Base64 encoded encrypted XML data</param>
        /// <param name="password">Password for key derivation</param>
        /// <param name="iterations">Number of iterations for key derivation</param>
        /// <returns>Decrypted XML data</returns>
        [SqlFunction(
            IsDeterministic = true,
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        [SecuritySafeCritical]
        public static SqlXml DecryptXml(SqlString base64EncryptedXml, SqlString password, SqlInt32 iterations)
        {
            if (base64EncryptedXml.IsNull || password.IsNull || iterations.IsNull)
                return SqlXml.Null;

            try
            {
                byte[] encryptedData = Convert.FromBase64String(base64EncryptedXml.Value);

                if (encryptedData.Length < 60) // 32 bytes salt + 12 bytes nonce + 16 bytes tag minimum
                    throw new ArgumentException("Encrypted data too short");

                // Extract salt, nonce, and cipher data
                byte[] salt = new byte[32];
                byte[] nonce = new byte[12];
                byte[] cipherData = new byte[encryptedData.Length - 44];

                Array.Copy(encryptedData, 0, salt, 0, 32);
                Array.Copy(encryptedData, 32, nonce, 0, 12);
                Array.Copy(encryptedData, 44, cipherData, 0, cipherData.Length);

                // Derive key from password
                byte[] key = _cgnService.DeriveKeyFromPassword(password.Value, salt, iterations.Value, 32);

                // Decrypt XML data
                byte[] decryptedBytes = _cgnService.DecryptAesGcm(cipherData, key, nonce);
                string xmlString = Encoding.UTF8.GetString(decryptedBytes);

                // Parse XML to validate it
                var xmlDoc = XDocument.Parse(xmlString);

                // Clear sensitive data
                Array.Clear(key, 0, key.Length);
                Array.Clear(encryptedData, 0, encryptedData.Length);
                Array.Clear(decryptedBytes, 0, decryptedBytes.Length);

                return new SqlXml(xmlDoc.CreateReader());
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"XML decryption failed: {ex.Message}");
            }
        }

        #endregion

        #region Single Value Encryption Functions

        /// <summary>
        /// Encrypts a single value with password-based key derivation
        /// </summary>
        /// <param name="value">Value to encrypt</param>
        /// <param name="password">Password for key derivation</param>
        /// <param name="iterations">Number of iterations for key derivation</param>
        /// <returns>Base64 encoded encrypted value data</returns>
        [SqlFunction(
            IsDeterministic = false, // Not deterministic due to random salt and nonce
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        [SecuritySafeCritical]
        public static SqlString EncryptValue(SqlString value, SqlString password, SqlInt32 iterations)
        {
            if (value.IsNull || password.IsNull || iterations.IsNull)
                return SqlString.Null;

            try
            {
                var metadata = new EncryptionMetadata
                {
                    Algorithm = "AES-GCM",
                    Key = password.Value,
                    Salt = _cgnService.GenerateNonce(32),
                    Iterations = iterations.Value,
                    AutoGenerateNonce = true
                };

                var encryptedData = _encryptionEngine.EncryptValue(value.Value, metadata);

                // Serialize the encrypted value data to XML using unified schema
                var resultXml = EncryptionXmlSchema.CreateSingleValueXml(
                    encryptedData.DataType,
                    Convert.ToBase64String(encryptedData.EncryptedValue),
                    metadata
                );

                return new SqlString(resultXml.ToString());
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Value encryption failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Decrypts a single value with password-based key derivation
        /// </summary>
        /// <param name="encryptedValue">Base64 encoded encrypted value data</param>
        /// <param name="password">Password for key derivation</param>
        /// <returns>Decrypted value</returns>
        [SqlFunction(
            IsDeterministic = true,
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        [SecuritySafeCritical]
        public static SqlString DecryptValue(SqlString encryptedValue, SqlString password)
        {
            if (encryptedValue.IsNull || password.IsNull)
                return SqlString.Null;

            try
            {
                // Parse the encrypted value XML
                var xmlDoc = XDocument.Parse(encryptedValue.Value);
                var root = xmlDoc.Root;

                var dataType = root.Element("DataType").Value;
                var encryptedDataBytes = Convert.FromBase64String(root.Element("EncryptedData").Value);
                var metadataElement = root.Element("Metadata");

                var metadata = new EncryptionMetadata
                {
                    Algorithm = metadataElement.Element("Algorithm").Value,
                    Key = password.Value,
                    Salt = Convert.FromBase64String(metadataElement.Element("Salt").Value),
                    Nonce = Convert.FromBase64String(metadataElement.Element("Nonce").Value),
                    Iterations = int.Parse(metadataElement.Element("Iterations").Value),
                    AutoGenerateNonce = false
                };

                var encryptedData = new EncryptedValueData
                {
                    EncryptedValue = encryptedDataBytes,
                    DataType = dataType,
                    Metadata = metadata
                };

                var decryptedValue = _encryptionEngine.DecryptValue(encryptedData, metadata);
                
                // Handle binary data specially - return as base64 string for compatibility
                if (decryptedValue is byte[] binaryData)
                {
                    return new SqlString(Convert.ToBase64String(binaryData));
                }
                else
                {
                    return new SqlString(decryptedValue.ToString());
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Value decryption failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Encrypts a single binary value with password-based key derivation
        /// </summary>
        /// <param name="binaryValue">Binary value to encrypt</param>
        /// <param name="password">Password for key derivation</param>
        /// <param name="iterations">Number of iterations for key derivation</param>
        /// <returns>Base64 encoded encrypted binary value data</returns>
        [SqlFunction(
            IsDeterministic = false, // Not deterministic due to random salt and nonce
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        [SecuritySafeCritical]
        public static SqlString EncryptBinaryValue(SqlBytes binaryValue, SqlString password, SqlInt32 iterations)
        {
            if (binaryValue.IsNull || password.IsNull || iterations.IsNull)
                return SqlString.Null;

            try
            {
                // Convert SqlBytes to byte array
                byte[] valueBytes = binaryValue.Value;

                var metadata = new EncryptionMetadata
                {
                    Algorithm = "AES-GCM",
                    Key = password.Value,
                    Salt = _cgnService.GenerateNonce(32),
                    Iterations = iterations.Value,
                    AutoGenerateNonce = true
                };

                // Use EncryptionEngine to encrypt the binary data
                // Pass the byte array directly to ensure it's treated as binary
                var encryptedData = _encryptionEngine.EncryptValue(valueBytes, metadata);

                // Ensure the data type is explicitly marked as binary
                encryptedData.DataType = typeof(byte[]).AssemblyQualifiedName;

                // Serialize the encrypted value data to XML using unified schema
                var resultXml = EncryptionXmlSchema.CreateSingleValueXml(
                    encryptedData.DataType,
                    Convert.ToBase64String(encryptedData.EncryptedValue),
                    metadata
                );

                return new SqlString(resultXml.ToString());
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Binary value encryption failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Decrypts a single binary value with password-based key derivation
        /// </summary>
        /// <param name="encryptedValue">Base64 encoded encrypted value data</param>
        /// <param name="password">Password for key derivation</param>
        /// <returns>Decrypted binary value as SqlBytes</returns>
        [SqlFunction(
            IsDeterministic = true,
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        [SecuritySafeCritical]
        public static SqlBytes DecryptBinaryValue(SqlString encryptedValue, SqlString password)
        {
            if (encryptedValue.IsNull || password.IsNull)
                return SqlBytes.Null;

            try
            {
                // Parse the encrypted value XML
                var xmlDoc = XDocument.Parse(encryptedValue.Value);
                var root = xmlDoc.Root;

                var dataType = root.Element("DataType").Value;
                var encryptedDataBytes = Convert.FromBase64String(root.Element("EncryptedData").Value);
                var metadataElement = root.Element("Metadata");

                var metadata = new EncryptionMetadata
                {
                    Algorithm = metadataElement.Element("Algorithm").Value,
                    Key = password.Value,
                    Salt = Convert.FromBase64String(metadataElement.Element("Salt").Value),
                    Nonce = Convert.FromBase64String(metadataElement.Element("Nonce").Value),
                    Iterations = int.Parse(metadataElement.Element("Iterations").Value),
                    AutoGenerateNonce = false
                };

                var encryptedData = new EncryptedValueData
                {
                    EncryptedValue = encryptedDataBytes,
                    DataType = dataType,
                    Metadata = metadata
                };

                var decryptedValue = _encryptionEngine.DecryptValue(encryptedData, metadata);
                
                // Ensure we return binary data
                if (decryptedValue is byte[] binaryData)
                {
                    return new SqlBytes(binaryData);
                }
                else
                {
                    throw new InvalidOperationException($"Expected binary data but got {decryptedValue?.GetType().Name}");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Binary value decryption failed: {ex.Message}");
            }
        }

        #endregion

        #region Table Encryption Functions

        /// <summary>
        /// Encrypts a table with metadata preservation
        /// </summary>
        /// <param name="tableName">Name of the table to encrypt</param>
        /// <param name="password">Password for key derivation</param>
        /// <param name="iterations">Number of iterations for key derivation</param>
        /// <returns>Base64 encoded encrypted table data with metadata</returns>
        [SqlFunction(
            IsDeterministic = false, // Not deterministic due to random salt and nonce
            IsPrecise = true,
            DataAccess = DataAccessKind.Read
        )]
        [SecuritySafeCritical]
        public static SqlString EncryptTable(SqlString tableName, SqlString password, SqlInt32 iterations)
        {
            if (tableName.IsNull || password.IsNull || iterations.IsNull)
                return SqlString.Null;

            try
            {
                // This would require dynamic SQL to read the table
                // For now, return a placeholder implementation
                throw new NotImplementedException("Table encryption requires dynamic SQL access. Use stored procedures instead.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Table encryption failed: {ex.Message}");
            }
        }

        #endregion

        #region Utility Functions

        /// <summary>
        /// Validates encryption metadata
        /// </summary>
        /// <param name="metadataXml">XML containing encryption metadata</param>
        /// <returns>Validation result as XML</returns>
        [SqlFunction(
            IsDeterministic = true,
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        [SecuritySafeCritical]
        public static SqlXml ValidateEncryptionMetadata(SqlXml metadataXml)
        {
            if (metadataXml.IsNull)
                return SqlXml.Null;

            try
            {
                // Parse metadata XML and validate
                var xmlDoc = XDocument.Parse(metadataXml.Value);
                
                // Create a simple metadata object for validation
                // NEVER store or read keys from XML!
                var metadata = new EncryptionMetadata
                {
                    Algorithm = xmlDoc.Root?.Element("Algorithm")?.Value ?? "AES-GCM",
                    Key = null, // Key should never be stored in XML
                    Iterations = int.TryParse(xmlDoc.Root?.Element("Iterations")?.Value, out int iter) ? iter : 10000,
                    AutoGenerateNonce = true
                };

                var validationResult = _encryptionEngine.ValidateEncryptionMetadata(metadata);

                var resultXml = new XElement("ValidationResult",
                    new XElement("IsValid", validationResult.IsValid),
                    new XElement("Errors",
                        validationResult.Errors.Select(e => new XElement("Error", e))
                    ),
                    new XElement("Warnings",
                        validationResult.Warnings.Select(w => new XElement("Warning", w))
                    )
                );

                return new SqlXml(resultXml.CreateReader());
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Metadata validation failed: {ex.Message}");
            }
        }

        #endregion

        #region Multi-Row XML Functions

        /// <summary>
        /// Simple function to encrypt multi-row XML from FOR XML output
        /// Returns encrypted data as a single XML string - perfect for SQL Server integration
        /// </summary>
        /// <param name="multiRowXml">XML from SELECT ... FOR XML RAW, ELEMENTS</param>
        /// <param name="password">Password for encryption</param>
        /// <param name="iterations">Key derivation iterations</param>
        /// <returns>Encrypted XML as string</returns>
        [SqlFunction(
            IsDeterministic = false,
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        [SecuritySafeCritical]
        public static SqlString EncryptMultiRowXml(SqlXml multiRowXml, SqlString password, SqlInt32 iterations)
        {
            if (multiRowXml.IsNull || password.IsNull || iterations.IsNull)
                return SqlString.Null;

            try
            {
                // Parse the multi-row XML
                var dataTable = _xmlConverter.ParseForXmlOutput(multiRowXml.Value);

                // Create encryption metadata
                var metadata = new EncryptionMetadata
                {
                    Algorithm = "AES-GCM",
                    Key = password.Value,
                    Salt = _cgnService.GenerateNonce(32),
                    Iterations = iterations.Value,
                    AutoGenerateNonce = true
                };

                // Encrypt each row
                var encryptedRows = new List<EncryptedRowData>();
                foreach (DataRow row in dataTable.Rows)
                {
                    var encryptedRow = _encryptionEngine.EncryptRow(row, metadata);
                    encryptedRows.Add(encryptedRow);
                }

                // Create XML result using unified schema
                var resultXml = EncryptionXmlSchema.CreateMultiRowXml(metadata, encryptedRows, _xmlConverter);

                return new SqlString(resultXml.ToString());
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Multi-row XML encryption failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Simple function to decrypt multi-row XML
        /// Returns decrypted data as XML - perfect for SQL Server integration
        /// </summary>
        /// <param name="encryptedXml">Encrypted XML string</param>
        /// <param name="password">Password for decryption</param>
        /// <param name="iterations">Key derivation iterations</param>
        /// <returns>Decrypted XML</returns>
        [SqlFunction(
            IsDeterministic = true,
            IsPrecise = true,
            DataAccess = DataAccessKind.None
        )]
        [SecuritySafeCritical]
        public static SqlXml DecryptMultiRowXml(SqlString encryptedXml, SqlString password, SqlInt32 iterations)
        {
            if (encryptedXml.IsNull || password.IsNull || iterations.IsNull)
                return SqlXml.Null;

            try
            {
                // Parse the encrypted XML using unified schema
                var xmlDoc = XDocument.Parse(encryptedXml.Value);
                var root = xmlDoc.Root;

                var (batchMetadata, encryptedRows) = EncryptionXmlSchema.ParseMultiRowXml(root, password.Value, _xmlConverter);

                // Derive the key once for all rows (performance optimization)
                byte[] derivedKey = _cgnService.DeriveKeyFromPassword(
                    batchMetadata.Key, 
                    batchMetadata.Salt, 
                    batchMetadata.Iterations, 
                    32
                );

                // Decrypt rows using the pre-derived key
                // Note: Each encrypted row already has its own metadata with the correct nonce
                var decryptedRows = _encryptionEngine.DecryptRows(encryptedRows, encryptedRows.FirstOrDefault()?.Metadata);

                // Clear the derived key from memory
                Array.Clear(derivedKey, 0, derivedKey.Length);

                // Return as XML in the same format as input (root wrapper with Row children)
                var resultXml = new XElement("root",
                    decryptedRows.Select(dr => 
                    {
                        var forXmlString = _xmlConverter.ToCleanForXmlFormat(dr, "Row", false);
                        var forXmlDoc = XDocument.Parse(forXmlString);
                        return forXmlDoc.Root.Element("Row");
                    })
                );

                return new SqlXml(resultXml.CreateReader());
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Multi-row XML decryption failed: {ex.Message}");
            }
        }

        #endregion
    }
} 
