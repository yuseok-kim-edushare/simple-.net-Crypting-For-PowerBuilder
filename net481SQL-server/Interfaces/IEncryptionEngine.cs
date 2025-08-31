using System;
using System.Collections.Generic;
using System.Data;
using System.Security;

namespace SecureLibrary.SQL.Interfaces
{
    /// <summary>
    /// Interface for row-level encryption and decryption operations
    /// Handles encryption/decryption of individual DataRow objects while preserving schema
    /// </summary>
    [SecuritySafeCritical]
    public interface IEncryptionEngine
    {
        /// <summary>
        /// Encrypts a single DataRow with the specified encryption metadata
        /// </summary>
        /// <param name="row">DataRow to encrypt</param>
        /// <param name="metadata">Encryption metadata containing algorithm, key info, etc.</param>
        /// <returns>Encrypted row data with preserved schema information</returns>
        /// <exception cref="ArgumentNullException">Thrown when row or metadata is null</exception>
        /// <exception cref="CryptographicException">Thrown when encryption fails</exception>
        EncryptedRowData EncryptRow(DataRow row, EncryptionMetadata metadata);

        /// <summary>
        /// Decrypts a single encrypted row and restores the original DataRow
        /// </summary>
        /// <param name="encryptedData">Encrypted row data</param>
        /// <param name="metadata">Encryption metadata used for decryption</param>
        /// <returns>Decrypted DataRow with original schema and values</returns>
        /// <exception cref="ArgumentNullException">Thrown when encryptedData or metadata is null</exception>
        /// <exception cref="CryptographicException">Thrown when decryption fails</exception>
        DataRow DecryptRow(EncryptedRowData encryptedData, EncryptionMetadata metadata);

        /// <summary>
        /// Encrypts multiple DataRows in batch for better performance
        /// </summary>
        /// <param name="rows">Collection of DataRows to encrypt</param>
        /// <param name="metadata">Encryption metadata</param>
        /// <returns>Collection of encrypted row data</returns>
        IEnumerable<EncryptedRowData> EncryptRows(IEnumerable<DataRow> rows, EncryptionMetadata metadata);

        /// <summary>
        /// Decrypts multiple encrypted rows in batch for better performance
        /// </summary>
        /// <param name="encryptedRows">Collection of encrypted row data</param>
        /// <param name="metadata">Encryption metadata</param>
        /// <returns>Collection of decrypted DataRows</returns>
        IEnumerable<DataRow> DecryptRows(IEnumerable<EncryptedRowData> encryptedRows, EncryptionMetadata metadata);

        /// <summary>
        /// Validates encryption metadata for correctness and security
        /// </summary>
        /// <param name="metadata">Encryption metadata to validate</param>
        /// <param name="ignoreNonce">If true, nonce validation is skipped</param>
        /// <returns>Validation result with any error messages</returns>
        ValidationResult ValidateEncryptionMetadata(EncryptionMetadata metadata, bool ignoreNonce = false);

        /// <summary>
        /// Gets the maximum number of columns supported by this encryption engine
        /// </summary>
        int MaxSupportedColumns { get; }

        /// <summary>
        /// Gets the supported encryption algorithms
        /// </summary>
        IEnumerable<string> SupportedAlgorithms { get; }

        /// <summary>
        /// Encrypts a single value with the specified encryption metadata
        /// </summary>
        /// <param name="value">Value to encrypt</param>
        /// <param name="metadata">Encryption metadata</param>
        /// <returns>Encrypted value data</returns>
        EncryptedValueData EncryptValue(object value, EncryptionMetadata metadata);

        /// <summary>
        /// Decrypts a single encrypted value
        /// </summary>
        /// <param name="encryptedData">Encrypted value data</param>
        /// <param name="metadata">Encryption metadata</param>
        /// <returns>Decrypted value</returns>
        object DecryptValue(EncryptedValueData encryptedData, EncryptionMetadata metadata);
    }

    /// <summary>
    /// Column-specific encryption settings
    /// </summary>
    public class ColumnEncryptionSettings
    {
        /// <summary>
        /// Whether this column should be encrypted
        /// </summary>
        public bool Encrypt { get; set; } = true;

        /// <summary>
        /// Specific algorithm for this column (overrides global algorithm)
        /// </summary>
        public string Algorithm { get; set; }

        /// <summary>
        /// Specific key for this column (overrides global key)
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Whether to preserve null values (don't encrypt nulls)
        /// </summary>
        public bool PreserveNulls { get; set; } = true;
    }
} 