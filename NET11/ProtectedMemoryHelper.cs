using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace SecureLibrary
{
    #pragma warning disable CA1416 // this class is for windows only so can ignore this warning
    /// <summary>
    /// Helper class for managing sensitive data protection using ProtectedData
    /// Cross-platform support with modern .NET APIs
    /// </summary>
    internal static class ProtectedMemoryHelper
    {
        /// <summary>
        /// Checks if ProtectedData is supported on the current platform
        /// </summary>
        public static bool IsSupported => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        /// <summary>
        /// Protects sensitive data using ProtectedData
        /// </summary>
        /// <param name="data">The data to protect</param>
        /// <param name="entropy">Optional entropy for additional security</param>
        /// <param name="scope">The protection scope (default: CurrentUser)</param>
        /// <returns>Protected data as byte array</returns>
        public static byte[] Protect(byte[] data, byte[]? entropy = null, DataProtectionScope scope = DataProtectionScope.CurrentUser)
        {
            if (!IsSupported)
                throw new PlatformNotSupportedException("ProtectedData is not supported on this platform");
            
            if (data == null) throw new ArgumentNullException(nameof(data));
            
            try
            {
                return ProtectedData.Protect(data, entropy, scope);
            }
            catch (CryptographicException ex)
            {
                throw new CryptographicException($"Failed to protect data: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Unprotects sensitive data using ProtectedData
        /// </summary>
        /// <param name="protectedData">The protected data to unprotect</param>
        /// <param name="entropy">Optional entropy used during protection</param>
        /// <param name="scope">The protection scope (default: CurrentUser)</param>
        /// <returns>Unprotected data as byte array</returns>
        public static byte[] Unprotect(byte[] protectedData, byte[]? entropy = null, DataProtectionScope scope = DataProtectionScope.CurrentUser)
        {
            if (!IsSupported)
                throw new PlatformNotSupportedException("ProtectedData is not supported on this platform");
            
            if (protectedData == null) throw new ArgumentNullException(nameof(protectedData));
            
            try
            {
                return ProtectedData.Unprotect(protectedData, entropy, scope);
            }
            catch (CryptographicException ex)
            {
                throw new CryptographicException($"Failed to unprotect data: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Safely executes an operation with protected data, ensuring cleanup
        /// </summary>
        /// <param name="sensitiveData">The sensitive data to protect during operation</param>
        /// <param name="operation">The operation to perform with unprotected data</param>
        /// <param name="entropy">Optional entropy for additional security</param>
        /// <param name="scope">The protection scope (default: CurrentUser)</param>
        /// <returns>The result of the operation</returns>
        public static T ExecuteWithProtection<T>(byte[] sensitiveData, Func<byte[], T> operation, byte[]? entropy = null, DataProtectionScope scope = DataProtectionScope.CurrentUser)
        {
            if (!IsSupported)
            {
                // On unsupported platforms, execute without protection
                return operation(sensitiveData);
            }

            if (sensitiveData == null) throw new ArgumentNullException(nameof(sensitiveData));
            if (operation == null) throw new ArgumentNullException(nameof(operation));

            byte[]? protectedData = null;
            byte[]? unprotectedData = null;

            try
            {
                // Protect the data
                protectedData = Protect(sensitiveData, entropy, scope);

                // Unprotect for operation
                unprotectedData = Unprotect(protectedData, entropy, scope);

                // Execute the operation
                T result = operation(unprotectedData);

                return result;
            }
            finally
            {
                // Ensure all sensitive data is cleared
                if (unprotectedData != null)
                {
                    Array.Clear(unprotectedData, 0, unprotectedData.Length);
                }
                if (protectedData != null)
                {
                    Array.Clear(protectedData, 0, protectedData.Length);
                }
            }
        }

        /// <summary>
        /// Safely executes an operation with protected data for multiple sensitive data arrays
        /// </summary>
        /// <param name="sensitiveDataArrays">Arrays of sensitive data to protect during operation</param>
        /// <param name="operation">The operation to perform with unprotected data</param>
        /// <param name="entropy">Optional entropy for additional security</param>
        /// <param name="scope">The protection scope (default: CurrentUser)</param>
        /// <returns>The result of the operation</returns>
        public static T ExecuteWithProtection<T>(byte[][] sensitiveDataArrays, Func<byte[][], T> operation, byte[]? entropy = null, DataProtectionScope scope = DataProtectionScope.CurrentUser)
        {
            if (!IsSupported)
            {
                // On unsupported platforms, execute without protection
                return operation(sensitiveDataArrays);
            }

            if (sensitiveDataArrays == null) throw new ArgumentNullException(nameof(sensitiveDataArrays));
            if (operation == null) throw new ArgumentNullException(nameof(operation));

            byte[][]? protectedArrays = null;
            byte[][]? unprotectedArrays = null;

            try
            {
                // Protect all data
                protectedArrays = new byte[sensitiveDataArrays.Length][];
                for (int i = 0; i < sensitiveDataArrays.Length; i++)
                {
                    protectedArrays[i] = Protect(sensitiveDataArrays[i], entropy, scope);
                }

                // Unprotect all data for operation
                unprotectedArrays = new byte[protectedArrays.Length][];
                for (int i = 0; i < protectedArrays.Length; i++)
                {
                    unprotectedArrays[i] = Unprotect(protectedArrays[i], entropy, scope);
                }

                // Execute the operation
                T result = operation(unprotectedArrays);

                return result;
            }
            finally
            {
                // Ensure all sensitive data is cleared
                if (unprotectedArrays != null)
                {
                    for (int i = 0; i < unprotectedArrays.Length; i++)
                    {
                        if (unprotectedArrays[i] != null)
                        {
                            Array.Clear(unprotectedArrays[i], 0, unprotectedArrays[i].Length);
                        }
                    }
                }
                if (protectedArrays != null)
                {
                    for (int i = 0; i < protectedArrays.Length; i++)
                    {
                        if (protectedArrays[i] != null)
                        {
                            Array.Clear(protectedArrays[i], 0, protectedArrays[i].Length);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Generates entropy for additional security (optional)
        /// </summary>
        /// <param name="length">Length of entropy bytes (default: 16)</param>
        /// <returns>Random entropy bytes</returns>
        public static byte[] GenerateEntropy(int length = 16)
        {
            if (length <= 0) throw new ArgumentException("Entropy length must be positive", nameof(length));
            
            byte[] entropy = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(entropy);
            }
            return entropy;
        }
    }
} 