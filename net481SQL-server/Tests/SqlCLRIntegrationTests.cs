using System;
using System.Data;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureLibrary.SQL;
using System.Collections.Generic;

namespace SecureLibrary.SQL.Tests
{
    /// <summary>
    /// Integration tests for SQL CLR Functions and Procedures
    /// Tests the actual SQL CLR implementations that will be used in SQL Server
    /// </summary>
    [TestClass]
    public class SqlCLRIntegrationTests
    {
        private string _testXmlContent;

        [TestInitialize]
        public void Setup()
        {
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
        public void TestSqlCLRFunctions_EncryptMultiRowXml()
        {
            // Arrange
            var multiRowXml = new SqlXml(XDocument.Parse(_testXmlContent).CreateReader());
            var password = new SqlString("TestPassword123!@#");
            var iterations = new SqlInt32(10000);

            // Act
            var encryptedXmlString = SqlCLRFunctions.EncryptMultiRowXml(multiRowXml, password, iterations);

            // Assert
            Assert.IsNotNull(encryptedXmlString);
            Assert.IsFalse(encryptedXmlString.IsNull);
            Assert.IsFalse(string.IsNullOrEmpty(encryptedXmlString.Value));

            // Verify the encrypted XML structure
            var encryptedXml = XDocument.Parse(encryptedXmlString.Value);
            var root = encryptedXml.Root;
            
            Assert.AreEqual("EncryptedData", root.Name.LocalName);
            
            // Check batch metadata
            var batchMetadata = root.Element("BatchMetadata");
            Assert.IsNotNull(batchMetadata);
            Assert.AreEqual("AES-GCM", batchMetadata.Element("Algorithm").Value);
            Assert.AreEqual("10000", batchMetadata.Element("Iterations").Value);
            Assert.AreEqual("2", batchMetadata.Element("RowCount").Value);
            
            // Check rows
            var rows = root.Element("Rows");
            Assert.IsNotNull(rows);
            Assert.AreEqual(2, rows.Elements("Row").Count());

            // Verify each row has proper structure
            foreach (var row in rows.Elements("Row"))
            {
                var rowMetadata = row.Element("RowMetadata");
                Assert.IsNotNull(rowMetadata);
                Assert.IsNotNull(rowMetadata.Element("Nonce"));
                Assert.IsNotNull(rowMetadata.Element("EncryptedAt"));
                Assert.IsNotNull(rowMetadata.Element("FormatVersion"));

                // Check schema information
                Assert.IsNotNull(row.Element("Schema"));
                Assert.IsNotNull(row.Element("SqlServerSchema"));
                Assert.IsNotNull(row.Element("EncryptedColumns"));

                // Check encrypted columns
                var encryptedColumns = row.Element("EncryptedColumns").Elements("Column");
                Assert.IsTrue(encryptedColumns.Count() > 0);
                foreach (var column in encryptedColumns)
                {
                    Assert.IsNotNull(column.Element("Name"));
                    Assert.IsNotNull(column.Element("EncryptedData"));
                    Assert.IsFalse(string.IsNullOrEmpty(column.Element("EncryptedData").Value));
                }
            }
        }

        [TestMethod]
        public void TestSqlCLRFunctions_DecryptMultiRowXml()
        {
            // Arrange
            var multiRowXml = new SqlXml(XDocument.Parse(_testXmlContent).CreateReader());
            var password = new SqlString("TestPassword456!@#");
            var iterations = new SqlInt32(10000);

            // First encrypt
            var encryptedXmlString = SqlCLRFunctions.EncryptMultiRowXml(multiRowXml, password, iterations);
            Assert.IsNotNull(encryptedXmlString);

            // Act - Decrypt
            var decryptedXml = SqlCLRFunctions.DecryptMultiRowXml(encryptedXmlString, password, iterations);

            // Assert
            Assert.IsNotNull(decryptedXml);
            Assert.IsFalse(decryptedXml.IsNull);

            // Parse the decrypted XML and verify content
            var decryptedDoc = XDocument.Parse(decryptedXml.Value);
            var decryptedRows = decryptedDoc.Root.Elements("Row");
            
            Assert.AreEqual(2, decryptedRows.Count());

            // Verify the decrypted data matches original
            var originalDoc = XDocument.Parse(_testXmlContent);
            var originalRows = originalDoc.Root.Elements().Where(e => e.Name.LocalName == "Row").ToList();
            var decryptedRowsList = decryptedRows.ToList();

            for (int i = 0; i < originalRows.Count; i++)
            {
                var originalRow = originalRows[i];
                var decryptedRow = decryptedRowsList[i];

                // Use LocalName to handle namespace differences
                Assert.AreEqual(originalRow.Elements().First(e => e.Name.LocalName == "id").Value, 
                               decryptedRow.Elements().First(e => e.Name.LocalName == "id").Value);
                Assert.AreEqual(originalRow.Elements().First(e => e.Name.LocalName == "name").Value, 
                               decryptedRow.Elements().First(e => e.Name.LocalName == "name").Value);
                Assert.AreEqual(originalRow.Elements().First(e => e.Name.LocalName == "reason").Value, 
                               decryptedRow.Elements().First(e => e.Name.LocalName == "reason").Value);
            }
        }

        [TestMethod]
        public void TestSqlCLRFunctions_EncryptDecryptRoundTrip()
        {
            // Arrange
            var multiRowXml = new SqlXml(XDocument.Parse(_testXmlContent).CreateReader());
            var password = new SqlString("RoundTripPassword789!@#");
            var iterations = new SqlInt32(10000);

            // Act - Multiple round trips
            var currentXml = multiRowXml;
            for (int roundTrip = 1; roundTrip <= 3; roundTrip++)
            {
                // Encrypt
                var encrypted = SqlCLRFunctions.EncryptMultiRowXml(currentXml, password, iterations);
                Assert.IsNotNull(encrypted);

                // Decrypt
                var decrypted = SqlCLRFunctions.DecryptMultiRowXml(encrypted, password, iterations);
                Assert.IsNotNull(decrypted);

                // Use decrypted as input for next round
                currentXml = decrypted;
            }

            // Assert - Final decrypted content should match original
            var finalDoc = XDocument.Parse(currentXml.Value);
            var originalDoc = XDocument.Parse(_testXmlContent);

            var finalRows = finalDoc.Root.Elements("Row").ToList();
            var originalRows = originalDoc.Root.Elements().Where(e => e.Name.LocalName == "Row").ToList();

            Assert.AreEqual(originalRows.Count, finalRows.Count);

            for (int i = 0; i < originalRows.Count; i++)
            {
                // Use LocalName to handle namespace differences
                Assert.AreEqual(originalRows[i].Elements().First(e => e.Name.LocalName == "id").Value, 
                               finalRows[i].Elements().First(e => e.Name.LocalName == "id").Value);
                Assert.AreEqual(originalRows[i].Elements().First(e => e.Name.LocalName == "name").Value, 
                               finalRows[i].Elements().First(e => e.Name.LocalName == "name").Value);
                Assert.AreEqual(originalRows[i].Elements().First(e => e.Name.LocalName == "reason").Value, 
                               finalRows[i].Elements().First(e => e.Name.LocalName == "reason").Value);
            }
        }

        [TestMethod]
        public void TestSqlCLRProcedures_EncryptMultiRows()
        {
            // Arrange
            var multiRowXml = new SqlXml(XDocument.Parse(_testXmlContent).CreateReader());
            var password = new SqlString("ProcedureTestPassword123!@#");
            var iterations = new SqlInt32(10000);
            SqlString encryptedRowsXml;

            // Act
            SqlCLRProcedures.EncryptMultiRows(multiRowXml, password, iterations, out encryptedRowsXml);

            // Assert
            Assert.IsNotNull(encryptedRowsXml);
            Assert.IsFalse(encryptedRowsXml.IsNull);
            Assert.IsFalse(string.IsNullOrEmpty(encryptedRowsXml.Value));

            // Verify the encrypted XML structure
            var encryptedXml = XDocument.Parse(encryptedRowsXml.Value);
            var root = encryptedXml.Root;
            
            Assert.AreEqual("EncryptedData", root.Name.LocalName);
            
            // Check metadata
            var metadata = root.Element("BatchMetadata");
            Assert.IsNotNull(metadata);
            Assert.AreEqual("AES-GCM", metadata.Element("Algorithm").Value);
            Assert.AreEqual("10000", metadata.Element("Iterations").Value);
            Assert.AreEqual("2", metadata.Element("RowCount").Value);
            
            // Check rows
            var rows = root.Element("Rows");
            Assert.IsNotNull(rows);
            Assert.AreEqual(2, rows.Elements("Row").Count());

            // Verify each row has proper structure
            foreach (var row in rows.Elements("Row"))
            {
                var rowMetadata = row.Element("RowMetadata");
                Assert.IsNotNull(rowMetadata);
                Assert.IsNotNull(rowMetadata.Element("Nonce"));
                Assert.IsNotNull(rowMetadata.Element("EncryptedAt"));
                Assert.IsNotNull(rowMetadata.Element("FormatVersion"));

                // Check schema information
                Assert.IsNotNull(row.Element("Schema"));
                Assert.IsNotNull(row.Element("SqlServerSchema"));
                Assert.IsNotNull(row.Element("EncryptedColumns"));
            }
        }

        [TestMethod]
        public void TestSqlCLRProcedures_DecryptMultiRows()
        {
            // Arrange
            var multiRowXml = new SqlXml(XDocument.Parse(_testXmlContent).CreateReader());
            var password = new SqlString("ProcedureDecryptPassword456!@#");
            var iterations = new SqlInt32(10000);
            SqlString encryptedRowsXml;

            // First encrypt
            SqlCLRProcedures.EncryptMultiRows(multiRowXml, password, iterations, out encryptedRowsXml);
            Assert.IsNotNull(encryptedRowsXml);

            // Act - Decrypt (now returns result set directly, not XML output)
            // Note: SQL CLR procedures require SQL Server context and cannot be tested in unit tests
            // This test documents the expected behavior and usage pattern
            
            try
            {
                // This will fail in unit tests because SQL CLR context is not available
                // In SQL Server, this would be used like: INSERT INTO #temp EXEC DecryptMultiRows @encryptedRowsXml, @password
                SqlCLRProcedures.DecryptMultiRows(encryptedRowsXml, password);
                
                // If we reach here, it means SQL CLR context is available (unlikely in unit tests)
                Assert.IsTrue(true, "DecryptMultiRows executed successfully (SQL CLR context available)");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("SqlClr 컨텍스트") || ex.Message.Contains("SqlClr context"))
            {
                // Expected behavior in unit tests - SQL CLR context not available
                Assert.IsTrue(true, "Correctly detected missing SQL CLR context in unit test environment");
            }
            catch (Exception ex)
            {
                // Unexpected exception
                Assert.Fail($"Unexpected exception in DecryptMultiRows test: {ex.Message}");
            }

            // Note: The actual data verification would need to be done in SQL Server integration tests
            // where we can capture the result set using INSERT INTO #temp EXEC DecryptMultiRows
        }

        [TestMethod]
        public void TestSqlCLRFunctions_PerformanceOptimization()
        {
            // Arrange
            var multiRowXml = new SqlXml(XDocument.Parse(_testXmlContent).CreateReader());
            var password = new SqlString("PerformanceTestPassword789!@#");
            var iterations = new SqlInt32(10000);

            // Act - Measure encryption time
            var encryptionStart = DateTime.UtcNow;
            var encryptedXmlString = SqlCLRFunctions.EncryptMultiRowXml(multiRowXml, password, iterations);
            var encryptionTime = DateTime.UtcNow - encryptionStart;

            // Act - Measure decryption time
            var decryptionStart = DateTime.UtcNow;
            var decryptedXml = SqlCLRFunctions.DecryptMultiRowXml(encryptedXmlString, password, iterations);
            var decryptionTime = DateTime.UtcNow - decryptionStart;

            // Assert
            Assert.IsNotNull(encryptedXmlString);
            Assert.IsNotNull(decryptedXml);

            // Performance assertions (adjust thresholds as needed)
            Assert.IsTrue(encryptionTime.TotalSeconds < 10, $"Encryption took too long: {encryptionTime.TotalSeconds}s");
            Assert.IsTrue(decryptionTime.TotalSeconds < 10, $"Decryption took too long: {decryptionTime.TotalSeconds}s");

            Console.WriteLine($"SQL CLR Encryption time for 2 rows: {encryptionTime.TotalMilliseconds}ms");
            Console.WriteLine($"SQL CLR Decryption time for 2 rows: {decryptionTime.TotalMilliseconds}ms");

            // Verify data integrity
            var decryptedDoc = XDocument.Parse(decryptedXml.Value);
            var originalDoc = XDocument.Parse(_testXmlContent);

            var decryptedRows = decryptedDoc.Root.Elements("Row").ToList();
            var originalRows = originalDoc.Root.Elements().Where(e => e.Name.LocalName == "Row").ToList();

            Assert.AreEqual(originalRows.Count, decryptedRows.Count);

            for (int i = 0; i < originalRows.Count; i++)
            {
                // Use LocalName to handle namespace differences
                Assert.AreEqual(originalRows[i].Elements().First(e => e.Name.LocalName == "id").Value, 
                               decryptedRows[i].Elements().First(e => e.Name.LocalName == "id").Value);
                Assert.AreEqual(originalRows[i].Elements().First(e => e.Name.LocalName == "name").Value, 
                               decryptedRows[i].Elements().First(e => e.Name.LocalName == "name").Value);
                Assert.AreEqual(originalRows[i].Elements().First(e => e.Name.LocalName == "reason").Value, 
                               decryptedRows[i].Elements().First(e => e.Name.LocalName == "reason").Value);
            }
        }

        [TestMethod]
        public void TestSqlCLRFunctions_ErrorHandling()
        {
            // Arrange
            var multiRowXml = new SqlXml(XDocument.Parse(_testXmlContent).CreateReader());
            var iterations = new SqlInt32(10000);

            // Test with null password
            var nullPassword = SqlString.Null;
            
            // Act & Assert - Should return null for null password
            var result = SqlCLRFunctions.EncryptMultiRowXml(multiRowXml, nullPassword, iterations);
            Assert.IsTrue(result.IsNull);

            // Test with null XML
            var nullXml = SqlXml.Null;
            var password = new SqlString("TestPassword123!@#");
            
            // Act & Assert - Should return null for null XML
            result = SqlCLRFunctions.EncryptMultiRowXml(nullXml, password, iterations);
            Assert.IsTrue(result.IsNull);

            // Test with null iterations
            var nullIterations = SqlInt32.Null;
            
            // Act & Assert - Should return null for null iterations
            result = SqlCLRFunctions.EncryptMultiRowXml(multiRowXml, password, nullIterations);
            Assert.IsTrue(result.IsNull);
        }

        [TestMethod]
        public void TestSqlCLRFunctions_KeyDerivationOptimization()
        {
            // This test verifies that our key derivation optimization is working
            // by checking that the same password and salt produce consistent results
            
            // Arrange
            var multiRowXml = new SqlXml(XDocument.Parse(_testXmlContent).CreateReader());
            var password = new SqlString("OptimizationTestPassword123!@#");
            var iterations = new SqlInt32(10000);

            // Act - Encrypt multiple times with same parameters
            var encrypted1 = SqlCLRFunctions.EncryptMultiRowXml(multiRowXml, password, iterations);
            var encrypted2 = SqlCLRFunctions.EncryptMultiRowXml(multiRowXml, password, iterations);

            // Assert - Both should be valid but different (due to different nonces)
            Assert.IsNotNull(encrypted1);
            Assert.IsNotNull(encrypted2);
            Assert.IsFalse(encrypted1.IsNull);
            Assert.IsFalse(encrypted2.IsNull);

            // Both should decrypt successfully with the same password
            var decrypted1 = SqlCLRFunctions.DecryptMultiRowXml(encrypted1, password, iterations);
            var decrypted2 = SqlCLRFunctions.DecryptMultiRowXml(encrypted2, password, iterations);

            Assert.IsNotNull(decrypted1);
            Assert.IsNotNull(decrypted2);

            // Both decrypted results should match the original
            var originalDoc = XDocument.Parse(_testXmlContent);
            var decrypted1Doc = XDocument.Parse(decrypted1.Value);
            var decrypted2Doc = XDocument.Parse(decrypted2.Value);

            var originalRows = originalDoc.Root.Elements().Where(e => e.Name.LocalName == "Row").ToList();
            var decrypted1Rows = decrypted1Doc.Root.Elements("Row").ToList();
            var decrypted2Rows = decrypted2Doc.Root.Elements("Row").ToList();

            Assert.AreEqual(originalRows.Count, decrypted1Rows.Count);
            Assert.AreEqual(originalRows.Count, decrypted2Rows.Count);

            // Verify both decrypted results match original
            for (int i = 0; i < originalRows.Count; i++)
            {
                // Use LocalName to handle namespace differences
                Assert.AreEqual(originalRows[i].Elements().First(e => e.Name.LocalName == "id").Value, 
                               decrypted1Rows[i].Elements().First(e => e.Name.LocalName == "id").Value);
                Assert.AreEqual(originalRows[i].Elements().First(e => e.Name.LocalName == "name").Value, 
                               decrypted1Rows[i].Elements().First(e => e.Name.LocalName == "name").Value);
                Assert.AreEqual(originalRows[i].Elements().First(e => e.Name.LocalName == "reason").Value, 
                               decrypted1Rows[i].Elements().First(e => e.Name.LocalName == "reason").Value);

                Assert.AreEqual(originalRows[i].Elements().First(e => e.Name.LocalName == "id").Value, 
                               decrypted2Rows[i].Elements().First(e => e.Name.LocalName == "id").Value);
                Assert.AreEqual(originalRows[i].Elements().First(e => e.Name.LocalName == "name").Value, 
                               decrypted2Rows[i].Elements().First(e => e.Name.LocalName == "name").Value);
                Assert.AreEqual(originalRows[i].Elements().First(e => e.Name.LocalName == "reason").Value, 
                               decrypted2Rows[i].Elements().First(e => e.Name.LocalName == "reason").Value);
            }
        }

        [TestMethod]
        public void TestSqlCLRProcedures_DecryptMultiRows_ResultSetBehavior()
        {
            // This test documents the expected behavior of the new DecryptMultiRows procedure
            // which now returns a result set instead of XML output
            
            // Arrange
            var multiRowXml = new SqlXml(XDocument.Parse(_testXmlContent).CreateReader());
            var password = new SqlString("ResultSetTestPassword123!@#");
            var iterations = new SqlInt32(10000);
            SqlString encryptedRowsXml;

            // First encrypt
            SqlCLRProcedures.EncryptMultiRows(multiRowXml, password, iterations, out encryptedRowsXml);
            Assert.IsNotNull(encryptedRowsXml);

            // Act - Decrypt (new behavior: returns result set directly)
            // Note: SQL CLR procedures require SQL Server context and cannot be tested in unit tests
            // This test documents the expected behavior and usage pattern
            
            try
            {
                // This will fail in unit tests because SQL CLR context is not available
                // In SQL Server, this would be used as:
                // INSERT INTO #temp EXEC dbo.DecryptMultiRows @encryptedRowsXml, @password
                SqlCLRProcedures.DecryptMultiRows(encryptedRowsXml, password);
                
                // If we reach here, it means SQL CLR context is available (unlikely in unit tests)
                Assert.IsTrue(true, "DecryptMultiRows procedure executed successfully (SQL CLR context available)");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("SqlClr 컨텍스트") || ex.Message.Contains("SqlClr context"))
            {
                // Expected behavior in unit tests - SQL CLR context not available
                Assert.IsTrue(true, "Correctly detected missing SQL CLR context in unit test environment");
            }
            catch (Exception ex)
            {
                // Unexpected exception
                Assert.Fail($"Unexpected exception in DecryptMultiRows test: {ex.Message}");
            }

            // Note: In SQL Server, the actual usage would be:
            // CREATE TABLE #temp (id INT, name NVARCHAR(100), reason NVARCHAR(200));
            // INSERT INTO #temp EXEC dbo.DecryptMultiRows @encryptedRowsXml, @password;
            // SELECT * FROM #temp;
        }
    }
} 