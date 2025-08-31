using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Linq;
using System.Security;
using System.Text;
using System.Security.Cryptography;
using System.Xml.Linq;
using SecureLibrary.SQL.Interfaces;

namespace SecureLibrary.SQL.Services
{
    /// <summary>
    /// Implementation of row-level encryption and decryption operations
    /// Handles encryption/decryption of individual DataRow objects while preserving schema
    /// Supports tables with up to 1000 columns with optimized memory usage
    /// </summary>
    [SecuritySafeCritical]
    public class EncryptionEngine : IEncryptionEngine
    {
        private readonly ICgnService _cgnService;
        private readonly ISqlXmlConverter _xmlConverter;
        private readonly ILogger _logger;

        // Configuration constants
        private const int MAX_SUPPORTED_COLUMNS = 1000;
        private const int BATCH_SIZE = 100; // Process rows in batches for memory efficiency
        private const int MIN_ITERATIONS = 1000;
        private const int MAX_ITERATIONS = 100000;
        private const int MIN_SALT_LENGTH = 8;
        private const int MAX_SALT_LENGTH = 64;

        /// <summary>
        /// Gets the maximum number of columns supported by this encryption engine
        /// </summary>
        public int MaxSupportedColumns => MAX_SUPPORTED_COLUMNS;

        /// <summary>
        /// Gets the supported encryption algorithms
        /// </summary>
        public IEnumerable<string> SupportedAlgorithms => new[] { "AES-GCM", "AES-CBC" };

        /// <summary>
        /// Initializes a new instance of the EncryptionEngine
        /// </summary>
        /// <param name="cgnService">CGN service for cryptographic operations</param>
        /// <param name="xmlConverter">XML converter for data serialization</param>
        /// <param name="logger">Logger for diagnostic information</param>
        public EncryptionEngine(ICgnService cgnService, ISqlXmlConverter xmlConverter, ILogger logger = null)
        {
            _cgnService = cgnService ?? throw new ArgumentNullException(nameof(cgnService));
            _xmlConverter = xmlConverter ?? throw new ArgumentNullException(nameof(xmlConverter));
            _logger = logger;
        }

        /// <summary>
        /// Encrypts a single DataRow with the specified encryption metadata
        /// </summary>
        /// <param name="row">DataRow to encrypt</param>
        /// <param name="metadata">Encryption metadata containing algorithm, key info, etc.</param>
        /// <returns>Encrypted row data with preserved schema information</returns>
        /// <exception cref="ArgumentNullException">Thrown when row or metadata is null</exception>
        /// <exception cref="CryptographicException">Thrown when encryption fails</exception>
        public EncryptedRowData EncryptRow(DataRow row, EncryptionMetadata metadata)
        {
            if (row == null)
                throw new ArgumentNullException(nameof(row));
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            _logger?.LogInformation($"Starting encryption of row with {row.Table.Columns.Count} columns");

            // Validate metadata
            var validationResult = ValidateEncryptionMetadata(metadata);
            if (!validationResult.IsValid)
            {
                var errorMessage = string.Join("; ", validationResult.Errors);
                throw new CryptographicException($"Invalid encryption metadata: {errorMessage}");
            }

            // Check column count limit
            if (row.Table.Columns.Count > MaxSupportedColumns)
            {
                throw new ArgumentException($"Row has {row.Table.Columns.Count} columns, which exceeds the maximum supported limit of {MaxSupportedColumns}");
            }

            try
            {
                // Convert row to XML for processing
                var xmlDoc = _xmlConverter.ToXml(row);
                var xmlString = xmlDoc.ToString();

                // Generate nonce if auto-generate is enabled
                byte[] nonce = null;
                if (metadata.AutoGenerateNonce)
                {
                    nonce = _cgnService.GenerateNonce(12); // 12 bytes for AES-GCM
                    metadata.Nonce = nonce;
                }
                else if (metadata.Nonce == null)
                {
                    throw new CryptographicException("Nonce is required when AutoGenerateNonce is false");
                }

                // Derive key if password-based encryption
                byte[] key = null;
                if (!string.IsNullOrEmpty(metadata.Key) && metadata.Salt != null)
                {
                    key = _cgnService.DeriveKeyFromPassword(metadata.Key, metadata.Salt, metadata.Iterations, 32);
                }
                else
                {
                    throw new CryptographicException("Either Key+Salt or direct key must be provided");
                }

                // Encrypt the XML data
                var encryptedXmlBytes = _cgnService.EncryptAesGcm(Encoding.UTF8.GetBytes(xmlString), key, metadata.Nonce);

                // Create encrypted row data with enhanced schema preservation
                // Create a thread-safe copy of the table schema to avoid concurrent modification issues
                DataTable schemaCopy;
                lock (row.Table)
                {
                    schemaCopy = row.Table.Copy();
                }
                
                var encryptedData = new EncryptedRowData
                {
                    Schema = schemaCopy,
                    Metadata = metadata,
                    EncryptedAt = DateTime.UtcNow,
                    FormatVersion = 1
                };

                // Build SQL Server specific schema information
                // Use the copied schema to avoid concurrent modification issues
                foreach (DataColumn column in schemaCopy.Columns)
                {
                    SqlDbType sqlDbType;
                    string sqlTypeName;
                    
                    // Check if we have the original SQL type stored
                    if (column.ExtendedProperties.ContainsKey("OriginalSqlType"))
                    {
                        var originalSqlType = column.ExtendedProperties["OriginalSqlType"] as string;
                        sqlDbType = GetSqlDbTypeFromOriginalType(originalSqlType);
                        sqlTypeName = GetSqlTypeNameFromOriginalType(originalSqlType, column.MaxLength);
                    }
                    else
                    {
                        // Fallback to CLR type mapping
                        sqlDbType = GetSqlDbTypeFromClrType(column.DataType);
                        sqlTypeName = GetSqlTypeName(column.DataType, column.MaxLength);
                    }
                    
                    var sqlServerColumn = new SqlServerColumnSchema
                    {
                        Name = column.ColumnName,
                        SqlDbType = sqlDbType,
                        SqlTypeName = sqlTypeName,
                        MaxLength = column.MaxLength,
                        IsNullable = column.AllowDBNull,
                        OrdinalPosition = column.Ordinal
                    };

                    // Add precision and scale for decimal types
                    if (column.DataType == typeof(decimal))
                    {
                        sqlServerColumn.Precision = 18;
                        sqlServerColumn.Scale = 2;
                    }

                    encryptedData.SqlServerSchema.Add(sqlServerColumn);
                }

                // Store encrypted data by column name for easy access
                encryptedData.EncryptedColumns["RowData"] = encryptedXmlBytes;
                
                // Store the nonce used for encryption to ensure it's available during decryption
                if (metadata.Nonce != null)
                {
                    encryptedData.EncryptedColumns["Nonce"] = metadata.Nonce;
                }

                // Clear sensitive data
                Array.Clear(key, 0, key.Length);
                if (nonce != null && nonce != metadata.Nonce) Array.Clear(nonce, 0, nonce.Length);

                _logger?.LogInformation($"Successfully encrypted row with {row.Table.Columns.Count} columns");
                return encryptedData;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Encryption failed: {ex.Message}");
                throw new CryptographicException($"Row encryption failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Decrypts a single encrypted row and restores the original DataRow
        /// </summary>
        /// <param name="encryptedData">Encrypted row data</param>
        /// <param name="metadata">Encryption metadata used for decryption</param>
        /// <returns>Decrypted DataRow with original schema and values</returns>
        /// <exception cref="ArgumentNullException">Thrown when encryptedData or metadata is null</exception>
        /// <exception cref="CryptographicException">Thrown when decryption fails</exception>
        public DataRow DecryptRow(EncryptedRowData encryptedData, EncryptionMetadata metadata)
        {
            if (encryptedData == null)
                throw new ArgumentNullException(nameof(encryptedData));
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            _logger?.LogInformation($"Starting decryption of row with {encryptedData.Schema.Columns.Count} columns");

            try
            {
                // Validate metadata, but allow missing nonce as it should be in EncryptedRowData
                var validationResult = ValidateEncryptionMetadata(metadata, true);
                if (!validationResult.IsValid)
                {
                    var errorMessage = string.Join("; ", validationResult.Errors);
                    throw new CryptographicException($"Invalid encryption metadata: {errorMessage}");
                }

                // Derive key if password-based encryption
                byte[] key;
                if (!string.IsNullOrEmpty(metadata.Key) && metadata.Salt != null)
                {
                    key = _cgnService.DeriveKeyFromPassword(metadata.Key, metadata.Salt, metadata.Iterations, 32);
                }
                else
                {
                    throw new CryptographicException("Either Key+Salt or direct key must be provided");
                }

                // Decrypt using the helper method
                var decryptedRow = DecryptRowWithKey(encryptedData, key);

                // Clear sensitive data
                Array.Clear(key, 0, key.Length);

                return decryptedRow;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Decryption failed: {ex.Message}");
                // Avoid wrapping CryptographicException in another one
                if (ex is CryptographicException) throw;
                throw new CryptographicException($"Row decryption failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Encrypts multiple DataRows in batch for better performance
        /// </summary>
        /// <param name="rows">Collection of DataRows to encrypt</param>
        /// <param name="metadata">Encryption metadata</param>
        /// <returns>Collection of encrypted row data</returns>
        public IEnumerable<EncryptedRowData> EncryptRows(IEnumerable<DataRow> rows, EncryptionMetadata metadata)
        {
            if (rows == null)
                throw new ArgumentNullException(nameof(rows));
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            var rowList = rows.ToList();
            _logger?.LogInformation($"Starting batch encryption of {rowList.Count} rows");

            var results = new List<EncryptedRowData>();

            // Process rows in batches for memory efficiency
            for (int i = 0; i < rowList.Count; i += BATCH_SIZE)
            {
                var batch = rowList.Skip(i).Take(BATCH_SIZE);
                var batchResults = new List<EncryptedRowData>();

                foreach (var row in batch)
                {
                    try
                    {
                        var encryptedRow = EncryptRow(row, metadata);
                        batchResults.Add(encryptedRow);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError($"Failed to encrypt row {i}: {ex.Message}");
                        throw new CryptographicException($"Batch encryption failed at row {i}: {ex.Message}", ex);
                    }
                }

                results.AddRange(batchResults);
                _logger?.LogInformation($"Completed batch {i / BATCH_SIZE + 1}, processed {batchResults.Count} rows");
            }

            _logger?.LogInformation($"Successfully encrypted {results.Count} rows in batch");
            return results;
        }

        /// <summary>
        /// Decrypts multiple encrypted rows in batch for better performance
        /// </summary>
        /// <param name="encryptedRows">Collection of encrypted row data</param>
        /// <param name="metadata">Encryption metadata</param>
        /// <returns>Collection of decrypted DataRows</returns>
        public IEnumerable<DataRow> DecryptRows(IEnumerable<EncryptedRowData> encryptedRows, EncryptionMetadata metadata)
        {
            if (encryptedRows == null)
                throw new ArgumentNullException(nameof(encryptedRows));
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            var encryptedRowList = encryptedRows.ToList();
            _logger?.LogInformation($"Starting batch decryption of {encryptedRowList.Count} rows");

            // Validate metadata once, but allow missing nonce as it should be in each EncryptedRowData
            var validationResult = ValidateEncryptionMetadata(metadata, true);
            if (!validationResult.IsValid)
            {
                var errorMessage = string.Join("; ", validationResult.Errors);
                throw new CryptographicException($"Invalid encryption metadata for batch operation: {errorMessage}");
            }

            // Derive key once for the entire batch
            byte[] key;
            if (!string.IsNullOrEmpty(metadata.Key) && metadata.Salt != null)
            {
                key = _cgnService.DeriveKeyFromPassword(metadata.Key, metadata.Salt, metadata.Iterations, 32);
            }
            else
            {
                throw new CryptographicException("Either Key+Salt or direct key must be provided for batch decryption");
            }

            var results = new List<DataRow>();
            try
            {
                // Process rows in batches for memory efficiency
                for (int i = 0; i < encryptedRowList.Count; i += BATCH_SIZE)
                {
                    var batch = encryptedRowList.Skip(i).Take(BATCH_SIZE);
                    var batchResults = new List<DataRow>();

                    foreach (var encryptedRow in batch)
                    {
                        try
                        {
                            // Decrypt each row using the pre-derived key
                            var decryptedRow = DecryptRowWithKey(encryptedRow, key);
                            batchResults.Add(decryptedRow);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError($"Failed to decrypt row {i}: {ex.Message}");
                            // Immediately re-throw to stop the batch operation on failure
                            throw new CryptographicException($"Batch decryption failed at row {i}: {ex.Message}", ex);
                        }
                    }

                    results.AddRange(batchResults);
                    _logger?.LogInformation($"Completed batch {i / BATCH_SIZE + 1}, processed {batchResults.Count} rows");
                }
            }
            finally
            {
                // Ensure the key is cleared even if an exception occurs
                Array.Clear(key, 0, key.Length);
            }

            _logger?.LogInformation($"Successfully decrypted {results.Count} rows in batch");
            return results;
        }

        /// <summary>
        /// Validates encryption metadata for correctness and security. This overload calls the one with the ignoreNonce parameter.
        /// </summary>
        /// <param name="metadata">Encryption metadata to validate</param>
        /// <returns>Validation result with any error messages</returns>
        public ValidationResult ValidateEncryptionMetadata(EncryptionMetadata metadata)
        {
            return ValidateEncryptionMetadata(metadata, false);
        }

        /// <summary>
        /// Validates encryption metadata for correctness and security
        /// </summary>
        /// <param name="metadata">Encryption metadata to validate</param>
        /// <param name="ignoreNonce">If true, nonce validation is skipped</param>
        /// <returns>Validation result with any error messages</returns>
        public ValidationResult ValidateEncryptionMetadata(EncryptionMetadata metadata, bool ignoreNonce = false)
        {
            var result = new ValidationResult { IsValid = true };

            if (metadata == null)
            {
                result.IsValid = false;
                result.Errors.Add("Encryption metadata cannot be null");
                return result;
            }

            // Validate algorithm
            if (string.IsNullOrEmpty(metadata.Algorithm))
            {
                result.IsValid = false;
                result.Errors.Add("Algorithm must be specified");
            }
            else if (!SupportedAlgorithms.Contains(metadata.Algorithm))
            {
                result.IsValid = false;
                result.Errors.Add($"Unsupported algorithm: {metadata.Algorithm}. Supported algorithms: {string.Join(", ", SupportedAlgorithms)}");
            }

            // Validate key
            if (string.IsNullOrEmpty(metadata.Key))
            {
                result.IsValid = false;
                result.Errors.Add("Key must be specified");
            }

            // Validate salt for password-based encryption
            if (metadata.Salt != null)
            {
                if (metadata.Salt.Length < MIN_SALT_LENGTH || metadata.Salt.Length > MAX_SALT_LENGTH)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Salt length must be between {MIN_SALT_LENGTH} and {MAX_SALT_LENGTH} bytes");
                }
            }

            // Validate iterations
            if (metadata.Iterations < MIN_ITERATIONS || metadata.Iterations > MAX_ITERATIONS)
            {
                result.IsValid = false;
                result.Errors.Add($"Iterations must be between {MIN_ITERATIONS} and {MAX_ITERATIONS}");
            }

            // Validate nonce unless ignored
            if (!ignoreNonce)
            {
                if (!metadata.AutoGenerateNonce && metadata.Nonce == null)
                {
                    result.IsValid = false;
                    result.Errors.Add("Nonce must be provided when AutoGenerateNonce is false");
                }

                if (metadata.Nonce != null && metadata.Nonce.Length != 12)
                {
                    result.IsValid = false;
                    result.Errors.Add("Nonce must be exactly 12 bytes for AES-GCM");
                }
            }

            // Security warnings
            if (metadata.Iterations < 2000)
            {
                result.Warnings.Add("Low iteration count may reduce security. Consider using at least 2000 iterations.");
            }

            if (metadata.Salt != null && metadata.Salt.Length < 16)
            {
                result.Warnings.Add("Short salt length may reduce security. Consider using at least 16 bytes.");
            }

            return result;
        }

        /// <summary>
        /// Encrypts a single value with the specified encryption metadata
        /// </summary>
        /// <param name="value">Value to encrypt</param>
        /// <param name="metadata">Encryption metadata</param>
        /// <returns>Encrypted value data</returns>
        public EncryptedValueData EncryptValue(object value, EncryptionMetadata metadata)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            _logger?.LogInformation($"Starting encryption of single value of type {value.GetType().Name}");

            // Validate metadata
            var validationResult = ValidateEncryptionMetadata(metadata);
            if (!validationResult.IsValid)
            {
                var errorMessage = string.Join("; ", validationResult.Errors);
                throw new CryptographicException($"Invalid encryption metadata: {errorMessage}");
            }

            try
            {
                // Convert value to string for encryption
                var stringValue = _xmlConverter.ConvertValueToString(value, value.GetType());

                // Generate nonce if auto-generate is enabled
                byte[] nonce = null;
                if (metadata.AutoGenerateNonce)
                {
                    nonce = _cgnService.GenerateNonce(12); // 12 bytes for AES-GCM
                    metadata.Nonce = nonce;
                }
                else if (metadata.Nonce == null)
                {
                    throw new CryptographicException("Nonce is required when AutoGenerateNonce is false");
                }

                // Derive key if password-based encryption
                byte[] key = null;
                if (!string.IsNullOrEmpty(metadata.Key) && metadata.Salt != null)
                {
                    key = _cgnService.DeriveKeyFromPassword(metadata.Key, metadata.Salt, metadata.Iterations, 32);
                }
                else
                {
                    throw new CryptographicException("Either Key+Salt or direct key must be provided");
                }

                // Encrypt the string value
                var encryptedBytes = _cgnService.EncryptAesGcm(Encoding.UTF8.GetBytes(stringValue), key, metadata.Nonce);

                // Create encrypted value data
                var encryptedData = new EncryptedValueData
                {
                    EncryptedValue = encryptedBytes,
                    DataType = value.GetType().AssemblyQualifiedName,
                    Metadata = metadata,
                    EncryptedAt = DateTime.UtcNow,
                    FormatVersion = 1
                };

                // Clear sensitive data
                Array.Clear(key, 0, key.Length);
                if (nonce != null && nonce != metadata.Nonce) Array.Clear(nonce, 0, nonce.Length);

                _logger?.LogInformation($"Successfully encrypted single value of type {value.GetType().Name}");
                return encryptedData;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Value encryption failed: {ex.Message}");
                throw new CryptographicException($"Value encryption failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Decrypts a single encrypted value
        /// </summary>
        /// <param name="encryptedData">Encrypted value data</param>
        /// <param name="metadata">Encryption metadata</param>
        /// <returns>Decrypted value</returns>
        public object DecryptValue(EncryptedValueData encryptedData, EncryptionMetadata metadata)
        {
            if (encryptedData == null)
                throw new ArgumentNullException(nameof(encryptedData));
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            _logger?.LogInformation($"Starting decryption of single value of type {encryptedData.DataType}");

            try
            {
                // Validate metadata
                var validationResult = ValidateEncryptionMetadata(metadata);
                if (!validationResult.IsValid)
                {
                    var errorMessage = string.Join("; ", validationResult.Errors);
                    throw new CryptographicException($"Invalid encryption metadata: {errorMessage}");
                }

                // Get the nonce used during encryption
                byte[] nonce = encryptedData.Metadata.Nonce;
                if (nonce == null)
                {
                    throw new CryptographicException("Nonce not found in encrypted data metadata");
                }

                // Derive key if password-based encryption
                byte[] key = null;
                if (!string.IsNullOrEmpty(metadata.Key) && metadata.Salt != null)
                {
                    key = _cgnService.DeriveKeyFromPassword(metadata.Key, metadata.Salt, metadata.Iterations, 32);
                }
                else
                {
                    throw new CryptographicException("Either Key+Salt or direct key must be provided");
                }

                // Decrypt the value
                var decryptedBytes = _cgnService.DecryptAesGcm(encryptedData.EncryptedValue, key, nonce);
                var stringValue = Encoding.UTF8.GetString(decryptedBytes);

                // Convert string back to original type
                var dataType = Type.GetType(encryptedData.DataType);
                if (dataType == null)
                {
                    throw new CryptographicException($"Could not find type {encryptedData.DataType}");
                }

                // Clear sensitive data
                Array.Clear(key, 0, key.Length);
                Array.Clear(decryptedBytes, 0, decryptedBytes.Length);

                _logger?.LogInformation($"Successfully decrypted single value of type {encryptedData.DataType}");
                // Convert base64 encoded binary to original type
                if (encryptedData.DataType.Contains("Byte[]") || encryptedData.DataType.Contains("Binary"))
                {
                    var decryptedValue = Convert.FromBase64String(stringValue);
                    return decryptedValue;
                }
                else
                {
                    var decryptedValue = _xmlConverter.ConvertStringToValue(stringValue, dataType);
                    return decryptedValue;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Value decryption failed: {ex.Message}");
                throw new CryptographicException($"Value decryption failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Decrypts a single encrypted row using a pre-derived key.
        /// This is an internal helper to optimize batch decryption.
        /// </summary>
        private DataRow DecryptRowWithKey(EncryptedRowData encryptedData, byte[] key)
        {
            if (encryptedData == null)
                throw new ArgumentNullException(nameof(encryptedData));
            if (key == null || key.Length == 0)
                throw new ArgumentNullException(nameof(key));

            _logger?.LogInformation($"Starting decryption of row with {encryptedData.Schema.Columns.Count} columns using pre-derived key.");

            try
            {
                // Get encrypted data
                if (!encryptedData.EncryptedColumns.TryGetValue("RowData", out byte[] encryptedXmlBytes))
                {
                    throw new CryptographicException("Encrypted row data not found");
                }

                // Get the nonce used during encryption (stored in encrypted data)
                byte[] nonce;
                if (encryptedData.EncryptedColumns.TryGetValue("Nonce", out byte[] storedNonce))
                {
                    nonce = storedNonce;
                }
                else if (encryptedData.Metadata?.Nonce != null) // Fallback to metadata if available
                {
                    nonce = encryptedData.Metadata.Nonce;
                }
                else
                {
                    throw new CryptographicException("Nonce not found in encrypted data or its metadata");
                }

                // Decrypt the XML data using the provided key and stored nonce
                var decryptedXmlBytes = _cgnService.DecryptAesGcm(encryptedXmlBytes, key, nonce);
                var xmlString = Encoding.UTF8.GetString(decryptedXmlBytes);

                // Parse XML and restore row with enhanced schema handling
                var xmlDoc = XDocument.Parse(xmlString);
                var decryptedRow = _xmlConverter.FromXml(xmlDoc, encryptedData.Schema);

                // Clear sensitive data from memory
                Array.Clear(decryptedXmlBytes, 0, decryptedXmlBytes.Length);

                _logger?.LogInformation($"Successfully decrypted row with {encryptedData.Schema.Columns.Count} columns");
                return decryptedRow;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Decryption with key failed: {ex.Message}");
                throw new CryptographicException($"Row decryption with pre-derived key failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Converts CLR type to SqlDbType
        /// </summary>
        /// <param name="clrType">CLR type</param>
        /// <returns>SqlDbType value</returns>
        private SqlDbType GetSqlDbTypeFromClrType(Type clrType)
        {
            return SqlTypeConversionHelper.GetSqlDbTypeFromClrType(clrType);
        }

        /// <summary>
        /// Gets the full SQL type name with length/precision/scale
        /// </summary>
        /// <param name="clrType">CLR type</param>
        /// <param name="maxLength">Maximum length</param>
        /// <returns>Full SQL type name</returns>
        private string GetSqlTypeName(Type clrType, int maxLength)
        {
            return SqlTypeConversionHelper.GetSqlTypeName(clrType, maxLength);
        }

        /// <summary>
        /// Converts original SQL type string to SqlDbType
        /// </summary>
        /// <param name="originalSqlType">Original SQL type (e.g., "char", "varchar", "nvarchar")</param>
        /// <returns>SqlDbType enum value</returns>
        private SqlDbType GetSqlDbTypeFromOriginalType(string originalSqlType)
        {
            return SqlTypeConversionHelper.GetSqlDbTypeFromOriginalType(originalSqlType);
        }

        /// <summary>
        /// Gets the full SQL type name from original type with length/precision/scale
        /// </summary>
        /// <param name="originalSqlType">Original SQL type</param>
        /// <param name="maxLength">Maximum length</param>
        /// <returns>Full SQL type name string</returns>
        private string GetSqlTypeNameFromOriginalType(string originalSqlType, int maxLength)
        {
            return SqlTypeConversionHelper.GetSqlTypeNameFromOriginalType(originalSqlType, maxLength);
        }
    }

    /// <summary>
    /// Simple logging interface for diagnostic information
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs an informational message
        /// </summary>
        /// <param name="message">Message to log</param>
        void LogInformation(string message);

        /// <summary>
        /// Logs a warning message
        /// </summary>
        /// <param name="message">Message to log</param>
        void LogWarning(string message);

        /// <summary>
        /// Logs an error message
        /// </summary>
        /// <param name="message">Message to log</param>
        void LogError(string message);
    }

    /// <summary>
    /// Null logger implementation that does nothing
    /// </summary>
    public class NullLogger : ILogger
    {
        public void LogInformation(string message) { }
        public void LogWarning(string message) { }
        public void LogError(string message) { }
    }
}