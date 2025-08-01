using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureLibrary.SQL;
using SecureLibrary.SQL.Services;
using System.Collections.Generic;

namespace SecureLibrary.SQL.Tests
{
    /// <summary>
    /// Comprehensive tests for multi-row XML parsing and encryption functionality
    /// Tests the complete workflow from XML parsing to encryption and back
    /// </summary>
    [TestClass]
    public class MultiRowXmlEncryptionTests
    {
        private SqlXmlConverter _xmlConverter;
        private EncryptionEngine _encryptionEngine;
        private CgnService _cgnService;
        private string _testXmlContent;

        [TestInitialize]
        public void Setup()
        {
            _xmlConverter = new SqlXmlConverter();
            _cgnService = new CgnService();
            _encryptionEngine = new EncryptionEngine(_cgnService, _xmlConverter);

            // Load the test XML content from the actual file
            var xmlFilePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "test_multi_row.xml");
            if (!File.Exists(xmlFilePath))
            {
                // Try alternative path
                xmlFilePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "test_multi_row.xml");
            }
            
            if (!File.Exists(xmlFilePath))
            {
                throw new FileNotFoundException($"Could not find test_multi_row.xml file. Tried paths: {xmlFilePath}");
            }

            _testXmlContent = File.ReadAllText(xmlFilePath, Encoding.UTF8);
        }

        [TestMethod]
        public void TestMultiRowXmlParsing_BasicFunctionality()
        {
            // Arrange & Act
            var dataTable = _xmlConverter.ParseForXmlOutput(_testXmlContent);

            // Assert
            Assert.IsNotNull(dataTable);
            Assert.AreEqual(2, dataTable.Rows.Count);
            Assert.AreEqual(3, dataTable.Columns.Count);

            // Check column names and types
            Assert.IsTrue(dataTable.Columns.Contains("id"));
            Assert.IsTrue(dataTable.Columns.Contains("name"));
            Assert.IsTrue(dataTable.Columns.Contains("reason"));

            Assert.AreEqual(typeof(int), dataTable.Columns["id"].DataType);
            Assert.AreEqual(typeof(string), dataTable.Columns["name"].DataType);
            Assert.AreEqual(typeof(string), dataTable.Columns["reason"].DataType);

            // Check first row values
            var firstRow = dataTable.Rows[0];
            Assert.AreEqual(24, firstRow["id"]);
            Assert.AreEqual("도대체", firstRow["name"]);
            Assert.AreEqual("왜 문제가 생기는 걸까요?", firstRow["reason"]);

            // Check second row values
            var secondRow = dataTable.Rows[1];
            Assert.AreEqual(98, secondRow["id"]);
            Assert.AreEqual("XML Row가", secondRow["name"]);
            Assert.AreEqual("root - row 가 여럿 있어서 .net 기본 xml 파서가 메롱해져요.", secondRow["reason"]);
        }

        [TestMethod]
        public void TestMultiRowXmlEncryption_CompleteWorkflow()
        {
            // Arrange
            var dataTable = _xmlConverter.ParseForXmlOutput(_testXmlContent);
            var encryptionKey = "TestEncryptionKey123!@#";
            var metadata = new EncryptionMetadata
            {
                Algorithm = "AES-GCM",
                Key = encryptionKey,
                Salt = new byte[16],
                Iterations = 10000,
                AutoGenerateNonce = true
            };

            // Act - Encrypt each row
            var encryptedRows = new System.Collections.Generic.List<EncryptedRowData>();
            foreach (DataRow row in dataTable.Rows)
            {
                var encryptedRow = _encryptionEngine.EncryptRow(row, metadata);
                encryptedRows.Add(encryptedRow);
            }

            // Act - Decrypt each row
            var decryptedRows = new System.Collections.Generic.List<DataRow>();
            foreach (var encryptedRow in encryptedRows)
            {
                var decryptedRow = _encryptionEngine.DecryptRow(encryptedRow, metadata);
                decryptedRows.Add(decryptedRow);
            }

            // Assert
            Assert.AreEqual(dataTable.Rows.Count, encryptedRows.Count);
            Assert.AreEqual(dataTable.Rows.Count, decryptedRows.Count);

            // Verify decrypted data matches original
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                var originalRow = dataTable.Rows[i];
                var decryptedRow = decryptedRows[i];

                Assert.AreEqual(originalRow["id"], decryptedRow["id"]);
                Assert.AreEqual(originalRow["name"], decryptedRow["name"]);
                Assert.AreEqual(originalRow["reason"], decryptedRow["reason"]);
            }
        }

        [TestMethod]
        public void TestMultiRowXmlEncryption_BatchProcessing()
        {
            // Arrange
            var dataTable = _xmlConverter.ParseForXmlOutput(_testXmlContent);
            var encryptionKey = "BatchTestKey456!@#";
            var metadata = new EncryptionMetadata
            {
                Algorithm = "AES-GCM",
                Key = encryptionKey,
                Salt = new byte[16],
                Iterations = 10000,
                AutoGenerateNonce = true
            };

            var rows = new List<DataRow>();
            foreach (DataRow row in dataTable.Rows)
            {
                rows.Add(row);
            }

            // Act - Encrypt all rows in batch
            var encryptedRows = _encryptionEngine.EncryptRows(rows, metadata).ToList();

            // Act - Decrypt all rows in batch
            var decryptedRows = _encryptionEngine.DecryptRows(encryptedRows, metadata).ToList();

            // Assert
            Assert.AreEqual(rows.Count, encryptedRows.Count);
            Assert.AreEqual(rows.Count, decryptedRows.Count);

            // Verify all data matches
            for (int i = 0; i < rows.Count; i++)
            {
                var originalRow = rows[i];
                var decryptedRow = decryptedRows[i];

                Assert.AreEqual(originalRow["id"], decryptedRow["id"]);
                Assert.AreEqual(originalRow["name"], decryptedRow["name"]);
                Assert.AreEqual(originalRow["reason"], decryptedRow["reason"]);
            }
        }



        [TestMethod]
        public void TestMultiRowXmlEncryption_SchemaPreservation()
        {
            // Arrange
            var dataTable = _xmlConverter.ParseForXmlOutput(_testXmlContent);
            var encryptionKey = "SchemaTestKey101!@#";
            var metadata = new EncryptionMetadata
            {
                Algorithm = "AES-GCM",
                Key = encryptionKey,
                Salt = new byte[16],
                Iterations = 10000,
                AutoGenerateNonce = true
            };

            var rows = new List<DataRow>();
            foreach (DataRow row in dataTable.Rows)
            {
                rows.Add(row);
            }
            var encryptedRows = _encryptionEngine.EncryptRows(rows, metadata).ToList();

            // Act
            var decryptedRows = _encryptionEngine.DecryptRows(encryptedRows, metadata).ToList();

            // Assert - Verify schema is preserved
            foreach (var encryptedRow in encryptedRows)
            {
                Assert.IsNotNull(encryptedRow.Schema);
                Assert.AreEqual(3, encryptedRow.Schema.Columns.Count);
                Assert.IsTrue(encryptedRow.Schema.Columns.Contains("id"));
                Assert.IsTrue(encryptedRow.Schema.Columns.Contains("name"));
                Assert.IsTrue(encryptedRow.Schema.Columns.Contains("reason"));

                // Check SQL Server schema information
                Assert.IsNotNull(encryptedRow.SqlServerSchema);
                Assert.AreEqual(3, encryptedRow.SqlServerSchema.Count);
            }

            // Verify decrypted rows have correct schema
            foreach (var decryptedRow in decryptedRows)
            {
                Assert.AreEqual(3, decryptedRow.Table.Columns.Count);
                Assert.AreEqual(typeof(int), decryptedRow.Table.Columns["id"].DataType);
                Assert.AreEqual(typeof(string), decryptedRow.Table.Columns["name"].DataType);
                Assert.AreEqual(typeof(string), decryptedRow.Table.Columns["reason"].DataType);
            }
        }

        [TestMethod]
        public void TestMultiRowXmlEncryption_DifferentAlgorithms()
        {
            // Arrange
            var dataTable = _xmlConverter.ParseForXmlOutput(_testXmlContent);
            var encryptionKey = "AlgorithmTestKey202!@#";
            var rows = new List<DataRow>();
            foreach (DataRow row in dataTable.Rows)
            {
                rows.Add(row);
            }

            // Test AES-GCM
            var gcmMetadata = new EncryptionMetadata
            {
                Algorithm = "AES-GCM",
                Key = encryptionKey,
                Salt = new byte[16],
                Iterations = 10000,
                AutoGenerateNonce = true
            };

            // Test AES-CBC
            var cbcMetadata = new EncryptionMetadata
            {
                Algorithm = "AES-CBC",
                Key = encryptionKey,
                Salt = new byte[16],
                Iterations = 10000,
                AutoGenerateNonce = true
            };

            // Act & Assert - AES-GCM
            var gcmEncrypted = _encryptionEngine.EncryptRows(rows, gcmMetadata).ToList();
            var gcmDecrypted = _encryptionEngine.DecryptRows(gcmEncrypted, gcmMetadata).ToList();

            Assert.AreEqual(rows.Count, gcmDecrypted.Count);
            for (int i = 0; i < rows.Count; i++)
            {
                Assert.AreEqual(rows[i]["id"], gcmDecrypted[i]["id"]);
                Assert.AreEqual(rows[i]["name"], gcmDecrypted[i]["name"]);
                Assert.AreEqual(rows[i]["reason"], gcmDecrypted[i]["reason"]);
            }

            // Act & Assert - AES-CBC
            var cbcEncrypted = _encryptionEngine.EncryptRows(rows, cbcMetadata).ToList();
            var cbcDecrypted = _encryptionEngine.DecryptRows(cbcEncrypted, cbcMetadata).ToList();

            Assert.AreEqual(rows.Count, cbcDecrypted.Count);
            for (int i = 0; i < rows.Count; i++)
            {
                Assert.AreEqual(rows[i]["id"], cbcDecrypted[i]["id"]);
                Assert.AreEqual(rows[i]["name"], cbcDecrypted[i]["name"]);
                Assert.AreEqual(rows[i]["reason"], cbcDecrypted[i]["reason"]);
            }
        }

        [TestMethod]
        public void TestMultiRowXmlEncryption_PerformanceValidation()
        {
            // Arrange - Use the actual test file
            var dataTable = _xmlConverter.ParseForXmlOutput(_testXmlContent);
            var encryptionKey = "PerformanceTestKey303!@#";
            var metadata = new EncryptionMetadata
            {
                Algorithm = "AES-GCM",
                Key = encryptionKey,
                Salt = new byte[16],
                Iterations = 10000,
                AutoGenerateNonce = true
            };

            var rows = new List<DataRow>();
            foreach (DataRow row in dataTable.Rows)
            {
                rows.Add(row);
            }

            // Act - Measure encryption time
            var encryptionStart = DateTime.UtcNow;
            var encryptedRows = _encryptionEngine.EncryptRows(rows, metadata).ToList();
            var encryptionTime = DateTime.UtcNow - encryptionStart;

            // Act - Measure decryption time
            var decryptionStart = DateTime.UtcNow;
            var decryptedRows = _encryptionEngine.DecryptRows(encryptedRows, metadata).ToList();
            var decryptionTime = DateTime.UtcNow - decryptionStart;

            // Assert
            Assert.AreEqual(2, encryptedRows.Count);
            Assert.AreEqual(2, decryptedRows.Count);

            // Verify all data integrity
            for (int i = 0; i < rows.Count; i++)
            {
                Assert.AreEqual(rows[i]["id"], decryptedRows[i]["id"]);
                Assert.AreEqual(rows[i]["name"], decryptedRows[i]["name"]);
                Assert.AreEqual(rows[i]["reason"], decryptedRows[i]["reason"]);
            }

            // Performance assertions (adjust thresholds as needed)
            Assert.IsTrue(encryptionTime.TotalSeconds < 10, $"Encryption took too long: {encryptionTime.TotalSeconds}s");
            Assert.IsTrue(decryptionTime.TotalSeconds < 10, $"Decryption took too long: {decryptionTime.TotalSeconds}s");

            Console.WriteLine($"Encryption time for 2 rows: {encryptionTime.TotalMilliseconds}ms");
            Console.WriteLine($"Decryption time for 2 rows: {decryptionTime.TotalMilliseconds}ms");
        }

        [TestMethod]
        public void TestMultiRowXmlEncryption_ErrorHandling()
        {
            // Arrange
            var dataTable = _xmlConverter.ParseForXmlOutput(_testXmlContent);
            var rows = new List<DataRow>();
            foreach (DataRow row in dataTable.Rows)
            {
                rows.Add(row);
            }

            // Test with invalid key
            var invalidMetadata = new EncryptionMetadata
            {
                Algorithm = "AES-GCM",
                Key = "", // Empty key
                Iterations = 10000,
                AutoGenerateNonce = true
            };

            // Act & Assert - Should throw exception for invalid metadata
            Assert.ThrowsExactly<CryptographicException>(() =>
            {
                _encryptionEngine.EncryptRows(rows, invalidMetadata).ToList();
            });

            // Test with valid metadata but invalid algorithm
            var invalidAlgorithmMetadata = new EncryptionMetadata
            {
                Algorithm = "INVALID-ALGORITHM",
                Key = "ValidKey123!@#",
                Salt = new byte[16],
                Iterations = 10000,
                AutoGenerateNonce = true
            };

            // Act & Assert - Should throw exception for invalid algorithm
            Assert.ThrowsExactly<CryptographicException>(() =>
            {
                _encryptionEngine.EncryptRows(rows, invalidAlgorithmMetadata).ToList();
            });
        }

        [TestMethod]
        public void TestMultiRowXmlEncryption_RoundTripValidation()
        {
            // Arrange
            var dataTable = _xmlConverter.ParseForXmlOutput(_testXmlContent);
            var encryptionKey = "RoundTripTestKey404!@#";
            var metadata = new EncryptionMetadata
            {
                Algorithm = "AES-GCM",
                Key = encryptionKey,
                Salt = new byte[16],
                Iterations = 10000,
                AutoGenerateNonce = true
            };

            var rows = new List<DataRow>();
            foreach (DataRow row in dataTable.Rows)
            {
                rows.Add(row);
            }

            // Act - Multiple round trips
            var currentRows = rows;
            for (int roundTrip = 1; roundTrip <= 3; roundTrip++)
            {
                var encrypted = _encryptionEngine.EncryptRows(currentRows, metadata).ToList();
                var decrypted = _encryptionEngine.DecryptRows(encrypted, metadata).ToList();
                currentRows = decrypted.ToList();

                // Assert - Verify data integrity after each round trip
                Assert.AreEqual(rows.Count, decrypted.Count);
                for (int i = 0; i < rows.Count; i++)
                {
                    Assert.AreEqual(rows[i]["id"], decrypted[i]["id"]);
                    Assert.AreEqual(rows[i]["name"], decrypted[i]["name"]);
                    Assert.AreEqual(rows[i]["reason"], decrypted[i]["reason"]);
                }
            }
        }


    }
} 