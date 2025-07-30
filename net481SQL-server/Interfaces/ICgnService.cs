using System;
using System.Security;

namespace SecureLibrary.SQL.Interfaces
{
    /// <summary>
    /// Interface for Windows CGN (Cryptographic Next Generation) API operations
    /// Provides thread-safe access to native Windows cryptographic functions
    /// </summary>
    [SecuritySafeCritical]
    public interface ICgnService
    {
        /// <summary>
        /// Invokes a CGN operation with the provided input data
        /// </summary>
        /// <param name="inputData">Input data for the CGN operation</param>
        /// <param name="operationType">Type of CGN operation to perform</param>
        /// <returns>Result of the CGN operation</returns>
        /// <exception cref="CryptographicException">Thrown when CGN operation fails</exception>
        byte[] InvokeCgnOperation(byte[] inputData, CgnOperationType operationType);

        /// <summary>
        /// Generates a cryptographically secure random key
        /// </summary>
        /// <param name="keySize">Size of the key in bits</param>
        /// <returns>Generated key as byte array</returns>
        byte[] GenerateKey(int keySize);

        /// <summary>
        /// Generates a cryptographically secure random nonce/IV
        /// </summary>
        /// <param name="nonceSize">Size of the nonce in bytes</param>
        /// <returns>Generated nonce as byte array</returns>
        byte[] GenerateNonce(int nonceSize);

        /// <summary>
        /// Performs AES-GCM encryption using CGN API
        /// </summary>
        /// <param name="plainData">Data to encrypt</param>
        /// <param name="key">Encryption key</param>
        /// <param name="nonce">Nonce for GCM mode</param>
        /// <returns>Encrypted data with authentication tag</returns>
        byte[] EncryptAesGcm(byte[] plainData, byte[] key, byte[] nonce);

        /// <summary>
        /// Performs AES-GCM decryption using CGN API
        /// </summary>
        /// <param name="cipherData">Data to decrypt (including authentication tag)</param>
        /// <param name="key">Decryption key</param>
        /// <param name="nonce">Nonce for GCM mode</param>
        /// <returns>Decrypted data</returns>
        byte[] DecryptAesGcm(byte[] cipherData, byte[] key, byte[] nonce);

        /// <summary>
        /// Derives a key from a password using PBKDF2
        /// </summary>
        /// <param name="password">Password to derive key from</param>
        /// <param name="salt">Salt for key derivation</param>
        /// <param name="iterations">Number of iterations</param>
        /// <param name="keySize">Size of the derived key in bytes</param>
        /// <returns>Derived key</returns>
        byte[] DeriveKeyFromPassword(string password, byte[] salt, int iterations, int keySize);
    }

    /// <summary>
    /// Types of CGN operations supported by the service
    /// </summary>
    public enum CgnOperationType
    {
        /// <summary>
        /// AES-GCM encryption operation
        /// </summary>
        AesGcmEncrypt,

        /// <summary>
        /// AES-GCM decryption operation
        /// </summary>
        AesGcmDecrypt,

        /// <summary>
        /// Key generation operation
        /// </summary>
        KeyGeneration,

        /// <summary>
        /// Random number generation operation
        /// </summary>
        RandomGeneration,

        /// <summary>
        /// Key derivation operation
        /// </summary>
        KeyDerivation
    }
} 