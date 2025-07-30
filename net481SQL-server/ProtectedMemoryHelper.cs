using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace SecureLibrary.SQL.Services
{
    /// <summary>
    /// Helper class for managing sensitive data protection using ProtectedMemory
    /// Only available on Windows platforms
    /// </summary>
    internal static class ProtectedMemoryHelper
    {
        /// <summary>
        /// Checks if ProtectedMemory is supported on the current platform
        /// </summary>
        public static bool IsSupported => Environment.OSVersion.Platform == PlatformID.Win32NT;

        /// <summary>
        /// Ensures the byte array length is a multiple of 16 bytes (required for ProtectedMemory)
        /// </summary>
        /// <param name="data">The data to pad</param>
        /// <returns>Padded data with length multiple of 16</returns>
        public static byte[] EnsureValidLength(byte[] data)
        {
            if (data == null) throw new ArgumentNullException("data");
            
            int requiredLength = ((data.Length + 15) / 16) * 16; // Round up to nearest multiple of 16
            if (data.Length == requiredLength) return data;
            
            byte[] paddedData = new byte[requiredLength];
            Array.Copy(data, paddedData, data.Length);
            return paddedData;
        }

        /// <summary>
        /// Protects sensitive data in memory using ProtectedMemory
        /// </summary>
        /// <param name="data">The data to protect (must be multiple of 16 bytes)</param>
        public static void Protect(byte[] data)
        {
            if (!IsSupported)
                throw new PlatformNotSupportedException("ProtectedMemory is only supported on Windows platforms");
            
            if (data == null) throw new ArgumentNullException("data");
            if (data.Length % 16 != 0)
                throw new ArgumentException("Data length must be a multiple of 16 bytes", "data");
            
            try
            {
                ProtectedMemory.Protect(data, MemoryProtectionScope.SameProcess);
            }
            catch (CryptographicException ex)
            {
                throw new CryptographicException($"Failed to protect memory: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Unprotects sensitive data in memory using ProtectedMemory
        /// </summary>
        /// <param name="data">The data to unprotect (must be multiple of 16 bytes)</param>
        public static void Unprotect(byte[] data)
        {
            if (!IsSupported)
                throw new PlatformNotSupportedException("ProtectedMemory is only supported on Windows platforms");
            
            if (data == null) throw new ArgumentNullException("data");
            if (data.Length % 16 != 0)
                throw new ArgumentException("Data length must be a multiple of 16 bytes", "data");
            
            try
            {
                ProtectedMemory.Unprotect(data, MemoryProtectionScope.SameProcess);
            }
            catch (CryptographicException ex)
            {
                throw new CryptographicException($"Failed to unprotect memory: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Safely executes an operation with protected memory, ensuring cleanup
        /// </summary>
        /// <param name="sensitiveData">The sensitive data to protect during operation</param>
        /// <param name="operation">The operation to perform with unprotected data</param>
        /// <returns>The result of the operation</returns>
        public static T ExecuteWithProtection<T>(byte[] sensitiveData, Func<byte[], T> operation)
        {
            if (!IsSupported)
            {
                // On non-Windows platforms, execute without protection
                return operation(sensitiveData);
            }

            if (sensitiveData == null) throw new ArgumentNullException("sensitiveData");
            if (operation == null) throw new ArgumentNullException("operation");

            byte[] paddedData = EnsureValidLength(sensitiveData);
            bool isProtected = false;

            try
            {
                // Protect the data
                Protect(paddedData);
                isProtected = true;

                // Unprotect for operation
                Unprotect(paddedData);

                // Execute the operation
                T result = operation(paddedData);

                // Re-protect the data
                Protect(paddedData);
                isProtected = true;

                return result;
            }
            finally
            {
                // Ensure data is cleared and re-protected if needed
                if (isProtected)
                {
                    try
                    {
                        Unprotect(paddedData);
                        Array.Clear(paddedData, 0, paddedData.Length);
                    }
                    catch
                    {
                        // Ignore errors during cleanup
                    }
                }
                else
                {
                    Array.Clear(paddedData, 0, paddedData.Length);
                }
            }
        }

        /// <summary>
        /// Safely executes an operation with protected memory for multiple sensitive data arrays
        /// </summary>
        /// <param name="sensitiveDataArrays">Arrays of sensitive data to protect during operation</param>
        /// <param name="operation">The operation to perform with unprotected data</param>
        /// <returns>The result of the operation</returns>
        public static T ExecuteWithProtection<T>(byte[][] sensitiveDataArrays, Func<byte[][], T> operation)
        {
            if (!IsSupported)
            {
                // On non-Windows platforms, execute without protection
                return operation(sensitiveDataArrays);
            }

            if (sensitiveDataArrays == null) throw new ArgumentNullException("sensitiveDataArrays");
            if (operation == null) throw new ArgumentNullException("operation");

            // Pad all arrays to valid lengths
            byte[][] paddedArrays = new byte[sensitiveDataArrays.Length][];
            for (int i = 0; i < sensitiveDataArrays.Length; i++)
            {
                paddedArrays[i] = EnsureValidLength(sensitiveDataArrays[i]);
            }

            bool[] isProtected = new bool[paddedArrays.Length];

            try
            {
                // Protect all data
                for (int i = 0; i < paddedArrays.Length; i++)
                {
                    Protect(paddedArrays[i]);
                    isProtected[i] = true;
                }

                // Unprotect all data for operation
                for (int i = 0; i < paddedArrays.Length; i++)
                {
                    Unprotect(paddedArrays[i]);
                }

                // Execute the operation
                T result = operation(paddedArrays);

                // Re-protect all data
                for (int i = 0; i < paddedArrays.Length; i++)
                {
                    Protect(paddedArrays[i]);
                    isProtected[i] = true;
                }

                return result;
            }
            finally
            {
                // Ensure all data is cleared and re-protected if needed
                for (int i = 0; i < paddedArrays.Length; i++)
                {
                    if (isProtected[i])
                    {
                        try
                        {
                            Unprotect(paddedArrays[i]);
                            Array.Clear(paddedArrays[i], 0, paddedArrays[i].Length);
                        }
                        catch
                        {
                            // Ignore errors during cleanup
                        }
                    }
                    else
                    {
                        Array.Clear(paddedArrays[i], 0, paddedArrays[i].Length);
                    }
                }
            }
        }
    }
} 