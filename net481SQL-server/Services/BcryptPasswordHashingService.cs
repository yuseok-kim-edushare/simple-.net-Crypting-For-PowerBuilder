using System;
using System.Security;
using SecureLibrary.SQL.Interfaces;

namespace SecureLibrary.SQL.Services
{
    /// <summary>
    /// Implementation of password hashing service using Bcrypt
    /// Provides secure password hashing and verification functionality
    /// </summary>
    [SecuritySafeCritical]
    public class BcryptPasswordHashingService : IPasswordHashingService
    {
        /// <summary>
        /// Default work factor for Bcrypt (12 is a good balance between security and performance)
        /// </summary>
        public int DefaultWorkFactor => 12;

        /// <summary>
        /// Minimum work factor allowed by Bcrypt
        /// </summary>
        public int MinimumWorkFactor => 4;

        /// <summary>
        /// Maximum work factor allowed by Bcrypt
        /// </summary>
        public int MaximumWorkFactor => 31;

        /// <summary>
        /// Hashes a password using Bcrypt with default work factor
        /// </summary>
        /// <param name="password">Password to hash</param>
        /// <returns>Hashed password string</returns>
        /// <exception cref="ArgumentNullException">Thrown when password is null</exception>
        public string HashPassword(string password)
        {
            return HashPassword(password, DefaultWorkFactor);
        }

        /// <summary>
        /// Hashes a password using Bcrypt with specified work factor
        /// </summary>
        /// <param name="password">Password to hash</param>
        /// <param name="workFactor">Bcrypt work factor (4-31, default 12)</param>
        /// <returns>Hashed password string</returns>
        /// <exception cref="ArgumentNullException">Thrown when password is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when work factor is invalid</exception>
        public string HashPassword(string password, int workFactor)
        {
            if (password == null)
                throw new ArgumentNullException(nameof(password));

            if (workFactor < MinimumWorkFactor || workFactor > MaximumWorkFactor)
                throw new ArgumentOutOfRangeException(nameof(workFactor), 
                    $"Work factor must be between {MinimumWorkFactor} and {MaximumWorkFactor}");

            try
            {
                return BCrypt.Net.BCrypt.HashPassword(password, workFactor);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to hash password: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Verifies a password against a hashed password
        /// </summary>
        /// <param name="password">Password to verify</param>
        /// <param name="hashedPassword">Hashed password to verify against</param>
        /// <returns>True if password matches, false otherwise</returns>
        /// <exception cref="ArgumentNullException">Thrown when password or hashedPassword is null</exception>
        public bool VerifyPassword(string password, string hashedPassword)
        {
            if (password == null)
                throw new ArgumentNullException(nameof(password));

            if (hashedPassword == null)
                throw new ArgumentNullException(nameof(hashedPassword));

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to verify password: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Generates a salt for password hashing
        /// </summary>
        /// <param name="workFactor">Work factor for salt generation</param>
        /// <returns>Generated salt string</returns>
        public string GenerateSalt(int workFactor = 12)
        {
            if (workFactor < MinimumWorkFactor || workFactor > MaximumWorkFactor)
                throw new ArgumentOutOfRangeException(nameof(workFactor), 
                    $"Work factor must be between {MinimumWorkFactor} and {MaximumWorkFactor}");

            try
            {
                return BCrypt.Net.BCrypt.GenerateSalt(workFactor);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to generate salt: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets information about a hashed password
        /// </summary>
        /// <param name="hashedPassword">Hashed password to analyze</param>
        /// <returns>Password hash information</returns>
        public PasswordHashInfo GetHashInfo(string hashedPassword)
        {
            if (hashedPassword == null)
                throw new ArgumentNullException(nameof(hashedPassword));

            try
            {
                var info = new PasswordHashInfo
                {
                    WorkFactor = BCrypt.Net.BCrypt.GetWorkFactor(hashedPassword),
                    IsValid = BCrypt.Net.BCrypt.ValidateAndReplacePassword(hashedPassword, hashedPassword) != null
                };

                return info;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to analyze hash: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Information about a password hash
    /// </summary>
    public class PasswordHashInfo
    {
        /// <summary>
        /// Work factor used in the hash
        /// </summary>
        public int WorkFactor { get; set; }

        /// <summary>
        /// Whether the hash is valid
        /// </summary>
        public bool IsValid { get; set; }
    }
} 