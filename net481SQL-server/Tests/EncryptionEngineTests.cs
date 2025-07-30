using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureLibrary.SQL.Interfaces;
using SecureLibrary.SQL.Services;

namespace SecureLibrary.SQL.Tests
{
    /// <summary>
    /// Comprehensive unit tests for the EncryptionEngine class
    /// Tests all major functionality including edge cases and error conditions
    /// </summary>
    [TestClass]
    public class EncryptionEngineTests
    {
        private ICgnService _cgnService;
        private ISqlXmlConverter _xmlConverter;
        private IEncryptionEngine _encryptionEngine;
        private ILogger _logger;

        [TestInitialize]
        public void Setup()
        {
            _cgnService = new CgnService();
            _xmlConverter = new SqlXmlConverter();
            _logger = new TestLogger();
            _encryptionEngine = new EncryptionEngine(_cgnService, _xmlConverter, _logger);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_cgnService is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        #region Single Row Encryption/Decryption Tests

        [TestMethod]
        public void EncryptRow_ValidData_ReturnsEncryptedRowData()
        {
            // Arrange
            var table = CreateTestTable();
            var row = table.NewRow();
            row["Id"] = 1;
            row["Name"] = "Test User";
            row["Email"] = "test@example.com";
            row["CreatedDate"] = DateTime.Now;
            row["IsActive"] = true;
            table.Rows.Add(row);

            var metadata = CreateValidEncryptionMetadata();

            // Act
            var encryptedData = _encryptionEngine.EncryptRow(row, metadata);

            // Assert
            Assert.IsNotNull(encryptedData);
            Assert.IsNotNull(encryptedData.Schema);
            Assert.AreEqual(5, encryptedData.Schema.Columns.Count);
            Assert.IsNotNull(encryptedData.EncryptedColumns);
            Assert.IsTrue(encryptedData.EncryptedColumns.ContainsKey("RowData"));
            Assert.IsNotNull(encryptedData.Metadata);
            Assert.AreEqual(DateTime.UtcNow.Date, encryptedData.EncryptedAt.Date);
            Assert.AreEqual(1, encryptedData.FormatVersion);
        }

        [TestMethod]
        public void DecryptRow_ValidEncryptedData_ReturnsOriginalRow()
        {
            // Arrange
            var table = CreateTestTable();
            var originalRow = table.NewRow();
            originalRow["Id"] = 1;
            originalRow["Name"] = "Test User";
            originalRow["Email"] = "test@example.com";
            originalRow["CreatedDate"] = DateTime.Now;
            originalRow["IsActive"] = true;
            table.Rows.Add(originalRow);

            var metadata = CreateValidEncryptionMetadata();
            var encryptedData = _encryptionEngine.EncryptRow(originalRow, metadata);

            // Act
            var decryptedRow = _encryptionEngine.DecryptRow(encryptedData, metadata);

            // Assert
            Assert.IsNotNull(decryptedRow);
            Assert.AreEqual(originalRow["Id"], decryptedRow["Id"]);
            Assert.AreEqual(originalRow["Name"], decryptedRow["Name"]);
            Assert.AreEqual(originalRow["Email"], decryptedRow["Email"]);
            Assert.AreEqual(((DateTime)originalRow["CreatedDate"]).Date, ((DateTime)decryptedRow["CreatedDate"]).Date);
            Assert.AreEqual(originalRow["IsActive"], decryptedRow["IsActive"]);
        }

        [TestMethod]
        public void EncryptRow_NullRow_ThrowsArgumentNullException()
        {
            // Arrange
            DataRow row = null;
            var metadata = CreateValidEncryptionMetadata();

            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => _encryptionEngine.EncryptRow(row, metadata));
        }

        [TestMethod]
        public void EncryptRow_NullMetadata_ThrowsArgumentNullException()
        {
            // Arrange
            var table = CreateTestTable();
            var row = table.NewRow();
            row["Id"] = 1;
            table.Rows.Add(row);

            EncryptionMetadata metadata = null;

            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => _encryptionEngine.EncryptRow(row, metadata));
        }

        [TestMethod]
        public void EncryptRow_TooManyColumns_ThrowsArgumentException()
        {
            // Arrange
            var table = new DataTable();
            for (int i = 0; i < 1001; i++) // Exceeds MAX_SUPPORTED_COLUMNS
            {
                table.Columns.Add($"Column{i}", typeof(string));
            }
            var row = table.NewRow();
            table.Rows.Add(row);

            var metadata = CreateValidEncryptionMetadata();

            // Act & Assert
            Assert.ThrowsExactly<ArgumentException>(() => _encryptionEngine.EncryptRow(row, metadata));
        }

        [TestMethod]
        public void EncryptRow_WithNullValues_PreservesNulls()
        {
            // Arrange
            var table = CreateTestTable();
            var row = table.NewRow();
            row["Id"] = 1;
            row["Name"] = DBNull.Value;
            row["Email"] = "test@example.com";
            row["CreatedDate"] = DBNull.Value;
            row["IsActive"] = true;
            table.Rows.Add(row);

            var metadata = CreateValidEncryptionMetadata();

            // Act
            var encryptedData = _encryptionEngine.EncryptRow(row, metadata);
            var decryptedRow = _encryptionEngine.DecryptRow(encryptedData, metadata);

            // Assert
            Assert.AreEqual(DBNull.Value, decryptedRow["Name"]);
            Assert.AreEqual(DBNull.Value, decryptedRow["CreatedDate"]);
            Assert.AreEqual(1, decryptedRow["Id"]);
            Assert.AreEqual("test@example.com", decryptedRow["Email"]);
            Assert.AreEqual(true, decryptedRow["IsActive"]);
        }

        #endregion

        #region Batch Encryption/Decryption Tests

        [TestMethod]
        public void EncryptRows_MultipleRows_ReturnsEncryptedDataForAllRows()
        {
            // Arrange
            var table = CreateTestTable();
            var rows = new List<DataRow>();

            for (int i = 1; i <= 5; i++)
            {
                var row = table.NewRow();
                row["Id"] = i;
                row["Name"] = $"User {i}";
                row["Email"] = $"user{i}@example.com";
                row["CreatedDate"] = DateTime.Now.AddDays(-i);
                row["IsActive"] = i % 2 == 0;
                table.Rows.Add(row);
                rows.Add(row);
            }

            var metadata = CreateValidEncryptionMetadata();

            // Act
            var encryptedRows = _encryptionEngine.EncryptRows(rows, metadata).ToList();

            // Assert
            Assert.AreEqual(5, encryptedRows.Count);
            foreach (var encryptedRow in encryptedRows)
            {
                Assert.IsNotNull(encryptedRow);
                Assert.IsNotNull(encryptedRow.EncryptedColumns);
                Assert.IsTrue(encryptedRow.EncryptedColumns.ContainsKey("RowData"));
            }
        }

        [TestMethod]
        public void DecryptRows_MultipleEncryptedRows_ReturnsAllOriginalRows()
        {
            // Arrange
            var table = CreateTestTable();
            var originalRows = new List<DataRow>();

            for (int i = 1; i <= 3; i++)
            {
                var row = table.NewRow();
                row["Id"] = i;
                row["Name"] = $"User {i}";
                row["Email"] = $"user{i}@example.com";
                row["CreatedDate"] = DateTime.Now.AddDays(-i);
                row["IsActive"] = i % 2 == 0;
                table.Rows.Add(row);
                originalRows.Add(row);
            }

            var metadata = CreateValidEncryptionMetadata();
            var encryptedRows = _encryptionEngine.EncryptRows(originalRows, metadata).ToList();

            // Act
            var decryptedRows = _encryptionEngine.DecryptRows(encryptedRows, metadata).ToList();

            // Assert
            Assert.AreEqual(3, decryptedRows.Count);
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(originalRows[i]["Id"], decryptedRows[i]["Id"]);
                Assert.AreEqual(originalRows[i]["Name"], decryptedRows[i]["Name"]);
                Assert.AreEqual(originalRows[i]["Email"], decryptedRows[i]["Email"]);
            }
        }

        [TestMethod]
        public void EncryptRows_EmptyCollection_ReturnsEmptyCollection()
        {
            // Arrange
            var rows = new List<DataRow>();
            var metadata = CreateValidEncryptionMetadata();

            // Act
            var encryptedRows = _encryptionEngine.EncryptRows(rows, metadata).ToList();

            // Assert
            Assert.AreEqual(0, encryptedRows.Count);
        }

        #endregion

        #region Metadata Validation Tests

        [TestMethod]
        public void ValidateEncryptionMetadata_ValidMetadata_ReturnsValidResult()
        {
            // Arrange
            var metadata = CreateValidEncryptionMetadata();

            // Act
            var result = _encryptionEngine.ValidateEncryptionMetadata(metadata);

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.Errors.Count);
        }

        [TestMethod]
        public void ValidateEncryptionMetadata_NullMetadata_ReturnsInvalidResult()
        {
            // Arrange
            EncryptionMetadata metadata = null;

            // Act
            var result = _encryptionEngine.ValidateEncryptionMetadata(metadata);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.IsTrue(result.Errors[0].Contains("cannot be null"));
        }

        [TestMethod]
        public void ValidateEncryptionMetadata_EmptyAlgorithm_ReturnsInvalidResult()
        {
            // Arrange
            var metadata = CreateValidEncryptionMetadata();
            metadata.Algorithm = "";

            // Act
            var result = _encryptionEngine.ValidateEncryptionMetadata(metadata);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.IsTrue(result.Errors[0].Contains("Algorithm must be specified"));
        }

        [TestMethod]
        public void ValidateEncryptionMetadata_UnsupportedAlgorithm_ReturnsInvalidResult()
        {
            // Arrange
            var metadata = CreateValidEncryptionMetadata();
            metadata.Algorithm = "UNSUPPORTED-ALGORITHM";

            // Act
            var result = _encryptionEngine.ValidateEncryptionMetadata(metadata);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.IsTrue(result.Errors[0].Contains("Unsupported algorithm"));
        }

        [TestMethod]
        public void ValidateEncryptionMetadata_EmptyKey_ReturnsInvalidResult()
        {
            // Arrange
            var metadata = CreateValidEncryptionMetadata();
            metadata.Key = "";

            // Act
            var result = _encryptionEngine.ValidateEncryptionMetadata(metadata);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.IsTrue(result.Errors[0].Contains("Key must be specified"));
        }

        [TestMethod]
        public void ValidateEncryptionMetadata_InvalidSaltLength_ReturnsInvalidResult()
        {
            // Arrange
            var metadata = CreateValidEncryptionMetadata();
            metadata.Salt = new byte[5]; // Too short

            // Act
            var result = _encryptionEngine.ValidateEncryptionMetadata(metadata);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.IsTrue(result.Errors[0].Contains("Salt length must be between"));
        }

        [TestMethod]
        public void ValidateEncryptionMetadata_InvalidIterations_ReturnsInvalidResult()
        {
            // Arrange
            var metadata = CreateValidEncryptionMetadata();
            metadata.Iterations = 500; // Too low

            // Act
            var result = _encryptionEngine.ValidateEncryptionMetadata(metadata);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.IsTrue(result.Errors[0].Contains("Iterations must be between"));
        }

        [TestMethod]
        public void ValidateEncryptionMetadata_InvalidNonce_ReturnsInvalidResult()
        {
            // Arrange
            var metadata = CreateValidEncryptionMetadata();
            metadata.AutoGenerateNonce = false;
            metadata.Nonce = new byte[8]; // Wrong length for AES-GCM

            // Act
            var result = _encryptionEngine.ValidateEncryptionMetadata(metadata);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.IsTrue(result.Errors[0].Contains("Nonce must be exactly 12 bytes"));
        }

        [TestMethod]
        public void ValidateEncryptionMetadata_LowIterations_ReturnsWarning()
        {
            // Arrange
            var metadata = CreateValidEncryptionMetadata();
            metadata.Iterations = 1500; // Below recommended threshold

            // Act
            var result = _encryptionEngine.ValidateEncryptionMetadata(metadata);

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.Errors.Count);
            Assert.AreEqual(1, result.Warnings.Count);
            Assert.IsTrue(result.Warnings[0].Contains("Low iteration count"));
        }

        #endregion

        #region Property Tests

        [TestMethod]
        public void MaxSupportedColumns_ReturnsCorrectValue()
        {
            // Act & Assert
            Assert.AreEqual(1000, _encryptionEngine.MaxSupportedColumns);
        }

        [TestMethod]
        public void SupportedAlgorithms_ReturnsCorrectAlgorithms()
        {
            // Act
            var algorithms = _encryptionEngine.SupportedAlgorithms.ToList();

            // Assert
            Assert.AreEqual(2, algorithms.Count);
            Assert.IsTrue(algorithms.Contains("AES-GCM"));
            Assert.IsTrue(algorithms.Contains("AES-CBC"));
        }

        #endregion

        #region Edge Case Tests

        [TestMethod]
        public void EncryptRow_LargeStringData_HandlesCorrectly()
        {
            // Arrange
            var table = CreateTestTable();
            var row = table.NewRow();
            row["Id"] = 1;
            row["Name"] = new string('A', 10000); // Large string
            row["Email"] = "test@example.com";
            row["CreatedDate"] = DateTime.Now;
            row["IsActive"] = true;
            table.Rows.Add(row);

            var metadata = CreateValidEncryptionMetadata();

            // Act
            var encryptedData = _encryptionEngine.EncryptRow(row, metadata);
            var decryptedRow = _encryptionEngine.DecryptRow(encryptedData, metadata);

            // Assert
            Assert.AreEqual(new string('A', 10000), decryptedRow["Name"]);
        }

        [TestMethod]
        public void EncryptRow_SpecialCharacters_HandlesCorrectly()
        {
            // Arrange
            var table = CreateTestTable();
            var row = table.NewRow();
            row["Id"] = 1;
            row["Name"] = "Test User with special chars: áéíóú ñ ç ß € £ ¥";
            row["Email"] = "test+tag@example.com";
            row["CreatedDate"] = DateTime.Now;
            row["IsActive"] = true;
            table.Rows.Add(row);

            var metadata = CreateValidEncryptionMetadata();

            // Act
            var encryptedData = _encryptionEngine.EncryptRow(row, metadata);
            var decryptedRow = _encryptionEngine.DecryptRow(encryptedData, metadata);

            // Assert
            Assert.AreEqual("Test User with special chars: áéíóú ñ ç ß € £ ¥", decryptedRow["Name"]);
            Assert.AreEqual("test+tag@example.com", decryptedRow["Email"]);
        }

        [TestMethod]
        public void EncryptRow_BinaryData_HandlesCorrectly()
        {
            // Arrange
            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("BinaryData", typeof(byte[]));
            table.Columns.Add("Name", typeof(string));

            var binaryData = Encoding.UTF8.GetBytes("Test binary data");
            var row = table.NewRow();
            row["Id"] = 1;
            row["BinaryData"] = binaryData;
            row["Name"] = "Test";
            table.Rows.Add(row);

            var metadata = CreateValidEncryptionMetadata();

            // Act
            var encryptedData = _encryptionEngine.EncryptRow(row, metadata);
            var decryptedRow = _encryptionEngine.DecryptRow(encryptedData, metadata);

            // Assert
            CollectionAssert.AreEqual(binaryData, (byte[])decryptedRow["BinaryData"]);
        }

        #endregion

        #region Single Value Encryption/Decryption Tests

        [TestMethod]
        public void EncryptValue_ValidData_ReturnsEncryptedValueData()
        {
            // Arrange
            var value = "This is a secret message";
            var metadata = CreateValidEncryptionMetadata();

            // Act
            var encryptedData = _encryptionEngine.EncryptValue(value, metadata);

            // Assert
            Assert.IsNotNull(encryptedData);
            Assert.IsNotNull(encryptedData.EncryptedValue);
            Assert.IsTrue(encryptedData.EncryptedValue.Length > 0);
            Assert.AreEqual(typeof(string).AssemblyQualifiedName, encryptedData.DataType);
            Assert.IsNotNull(encryptedData.Metadata);
            Assert.AreEqual(DateTime.UtcNow.Date, encryptedData.EncryptedAt.Date);
            Assert.AreEqual(1, encryptedData.FormatVersion);
        }

        [TestMethod]
        public void DecryptValue_ValidEncryptedData_ReturnsOriginalValue()
        {
            // Arrange
            var originalValue = "This is a secret message";
            var metadata = CreateValidEncryptionMetadata();
            var encryptedData = _encryptionEngine.EncryptValue(originalValue, metadata);

            // Act
            var decryptedValue = _encryptionEngine.DecryptValue(encryptedData, metadata);

            // Assert
            Assert.IsNotNull(decryptedValue);
            Assert.AreEqual(originalValue, decryptedValue);
        }

        [TestMethod]
        public void EncryptValue_IntegerData_HandlesCorrectly()
        {
            // Arrange
            var value = 12345;
            var metadata = CreateValidEncryptionMetadata();

            // Act
            var encryptedData = _encryptionEngine.EncryptValue(value, metadata);
            var decryptedValue = _encryptionEngine.DecryptValue(encryptedData, metadata);

            // Assert
            Assert.AreEqual(value, decryptedValue);
        }

        [TestMethod]
        public void EncryptValue_DateTimeData_HandlesCorrectly()
        {
            // Arrange
            var value = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var metadata = CreateValidEncryptionMetadata();
        
            // Act
            var encryptedData = _encryptionEngine.EncryptValue(value, metadata);
            var decryptedValue = (DateTime)_encryptionEngine.DecryptValue(encryptedData, metadata);
        
            // Assert
            Assert.AreEqual(value, decryptedValue);
        }

        #endregion

        #region Helper Methods

        private DataTable CreateTestTable()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Email", typeof(string));
            table.Columns.Add("CreatedDate", typeof(DateTime));
            table.Columns.Add("IsActive", typeof(bool));
            return table;
        }

        private EncryptionMetadata CreateValidEncryptionMetadata()
        {
            return new EncryptionMetadata
            {
                Algorithm = "AES-GCM",
                Key = "TestPassword123!",
                Salt = new byte[16],
                Iterations = 2000,
                AutoGenerateNonce = true
            };
        }

        #endregion
    }

    /// <summary>
    /// Test logger implementation for unit tests
    /// </summary>
    public class TestLogger : ILogger
    {
        public List<string> InformationMessages { get; } = new List<string>();
        public List<string> WarningMessages { get; } = new List<string>();
        public List<string> ErrorMessages { get; } = new List<string>();

        public void LogInformation(string message)
        {
            InformationMessages.Add(message);
        }

        public void LogWarning(string message)
        {
            WarningMessages.Add(message);
        }

        public void LogError(string message)
        {
            ErrorMessages.Add(message);
        }
    }
} 