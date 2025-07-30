using System;
using System.Security;

namespace SecureLibrary.SQL.Interfaces
{
    /// <summary>
    /// Interface for password hashing operations using Bcrypt
    /// Provides secure password hashing and verification functionality
    /// </summary>
    [SecuritySafeCritical]
    public interface IPasswordHashingService
    {
        /// <summary>
        /// Hashes a password using Bcrypt with default work factor
        /// </summary>
        /// <param name="password">Password to hash</param>
        /// <returns>Hashed password string</returns>
        /// <exception cref="ArgumentNullException">Thrown when password is null</exception>
        string HashPassword(string password);

        /// <summary>
        /// Hashes a password using Bcrypt with specified work factor
        /// </summary>
        /// <param name="password">Password to hash</param>
        /// <param name="workFactor">Bcrypt work factor (4-31, default 12)</param>
        /// <returns>Hashed password string</returns>
        /// <exception cref="ArgumentNullException">Thrown when password is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when work factor is invalid</exception>
        string HashPassword(string password, int workFactor);

        /// <summary>
        /// Verifies a password against a hashed password
        /// </summary>
        /// <param name="password">Password to verify</param>
        /// <param name="hashedPassword">Hashed password to verify against</param>
        /// <returns>True if password matches, false otherwise</returns>
        /// <exception cref="ArgumentNullException">Thrown when password or hashedPassword is null</exception>
        bool VerifyPassword(string password, string hashedPassword);

        /// <summary>
        /// Gets the default work factor used for password hashing
        /// </summary>
        int DefaultWorkFactor { get; }

        /// <summary>
        /// Gets the minimum work factor allowed
        /// </summary>
        int MinimumWorkFactor { get; }

        /// <summary>
        /// Gets the maximum work factor allowed
        /// </summary>
        int MaximumWorkFactor { get; }

        /// <summary>
        /// Generates a salt for password hashing
        /// </summary>
        /// <param name="workFactor">Work factor for salt generation</param>
        /// <returns>Generated salt string</returns>
        string GenerateSalt(int workFactor = 12);

    }
} 