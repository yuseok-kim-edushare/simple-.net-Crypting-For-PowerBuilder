using System;
using System.Data;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureLibrary.SQL;
using SecureLibrary.SQL.Services;

namespace SecureLibrary.SQL.Tests
{
    /// <summary>
    /// Debug tests for multi-row encryption/decryption process
    /// </summary>
    [TestClass]
    public class MultiRowDebugTests
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
        public void TestMultiRowEncryption_Debug()
        {
            // Arrange
            var multiRowXml = new SqlXml(XDocument.Parse(_testXmlContent).CreateReader());
            var password = new SqlString("DebugPassword123!@#");
            var iterations = new SqlInt32(10000);

            // Act - Encrypt
            var encryptedXmlString = SqlCLRFunctions.EncryptMultiRowXml(multiRowXml, password, iterations);

            // Assert
            Assert.IsNotNull(encryptedXmlString);
            Assert.IsFalse(encryptedXmlString.IsNull);
            Assert.IsFalse(string.IsNullOrEmpty(encryptedXmlString.Value));

            // Debug: Print the encrypted XML structure
            Console.WriteLine("=== ENCRYPTED XML STRUCTURE ===");
            Console.WriteLine(encryptedXmlString.Value);
            Console.WriteLine("=== END ENCRYPTED XML ===");

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

            // Debug: Check each row structure
            foreach (var row in rows.Elements("Row"))
            {
                Console.WriteLine($"=== ROW STRUCTURE ===");
                Console.WriteLine(row.ToString());
                Console.WriteLine($"=== END ROW ===");
                
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
        public void TestMultiRowDecryption_Debug()
        {
            // Arrange
            var multiRowXml = new SqlXml(XDocument.Parse(_testXmlContent).CreateReader());
            var password = new SqlString("DebugDecryptPassword456!@#");
            var iterations = new SqlInt32(10000);

            // First encrypt
            var encryptedXmlString = SqlCLRFunctions.EncryptMultiRowXml(multiRowXml, password, iterations);
            Assert.IsNotNull(encryptedXmlString);

            // Debug: Print the encrypted XML before decryption
            Console.WriteLine("=== ENCRYPTED XML BEFORE DECRYPTION ===");
            Console.WriteLine(encryptedXmlString.Value);
            Console.WriteLine("=== END ENCRYPTED XML ===");

            // Act - Decrypt
            var decryptedXml = SqlCLRFunctions.DecryptMultiRowXml(encryptedXmlString, password, iterations);

            // Debug: Check if decryption returned null
            if (decryptedXml.IsNull)
            {
                Console.WriteLine("=== DECRYPTION RETURNED NULL ===");
                Assert.Fail("Decryption returned null");
            }

            // Debug: Print the decrypted XML structure
            Console.WriteLine("=== DECRYPTED XML STRUCTURE ===");
            Console.WriteLine(decryptedXml.Value);
            Console.WriteLine("=== END DECRYPTED XML ===");

            // Assert
            Assert.IsNotNull(decryptedXml);
            Assert.IsFalse(decryptedXml.IsNull);

            // Parse the decrypted XML and verify content
            var decryptedDoc = XDocument.Parse(decryptedXml.Value);
            var decryptedRows = decryptedDoc.Root.Elements().Where(e => e.Name.LocalName == "Row");
            
            Assert.AreEqual(2, decryptedRows.Count());

            // Debug: Check each decrypted row
            foreach (var row in decryptedRows)
            {
                Console.WriteLine($"=== DECRYPTED ROW ===");
                Console.WriteLine(row.ToString());
                Console.WriteLine($"=== END DECRYPTED ROW ===");
            }

            // Verify the decrypted data matches original
            var originalDoc = XDocument.Parse(_testXmlContent);
            var originalRows = originalDoc.Root.Elements().Where(e => e.Name.LocalName == "Row").ToList();
            var decryptedRowsList = decryptedRows.ToList();

            Assert.AreEqual(originalRows.Count, decryptedRowsList.Count);

            for (int i = 0; i < originalRows.Count; i++)
            {
                var originalRow = originalRows[i];
                var decryptedRow = decryptedRowsList[i];

                Console.WriteLine($"=== COMPARING ROW {i} ===");
                Console.WriteLine($"Original: {originalRow}");
                Console.WriteLine($"Decrypted: {decryptedRow}");
                Console.WriteLine($"=== END COMPARISON ===");

                Assert.AreEqual(originalRow.Elements().First(e => e.Name.LocalName == "id").Value, 
                               decryptedRow.Elements().First(e => e.Name.LocalName == "id").Value);
                Assert.AreEqual(originalRow.Elements().First(e => e.Name.LocalName == "name").Value, 
                               decryptedRow.Elements().First(e => e.Name.LocalName == "name").Value);
                Assert.AreEqual(originalRow.Elements().First(e => e.Name.LocalName == "reason").Value, 
                               decryptedRow.Elements().First(e => e.Name.LocalName == "reason").Value);
            }
        }

        [TestMethod]
        public void TestMultiRowRoundTrip_Debug()
        {
            // Arrange
            var multiRowXml = new SqlXml(XDocument.Parse(_testXmlContent).CreateReader());
            var password = new SqlString("RoundTripDebugPassword789!@#");
            var iterations = new SqlInt32(10000);

            // Act - Single round trip
            Console.WriteLine("=== STARTING ROUND TRIP ===");
            
            // Encrypt
            var encrypted = SqlCLRFunctions.EncryptMultiRowXml(multiRowXml, password, iterations);
            Assert.IsNotNull(encrypted);
            Console.WriteLine("=== ENCRYPTION SUCCESSFUL ===");

            // Decrypt
            var decrypted = SqlCLRFunctions.DecryptMultiRowXml(encrypted, password, iterations);
            
            if (decrypted.IsNull)
            {
                Console.WriteLine("=== DECRYPTION FAILED - RETURNED NULL ===");
                Assert.Fail("Decryption returned null");
            }
            
            Assert.IsNotNull(decrypted);
            Console.WriteLine("=== DECRYPTION SUCCESSFUL ===");

            // Use decrypted as input for next round
            var currentXml = decrypted;
            Console.WriteLine("=== ROUND TRIP COMPLETED ===");

            // Assert - Final decrypted content should match original
            var finalDoc = XDocument.Parse(currentXml.Value);
            var originalDoc = XDocument.Parse(_testXmlContent);

            var finalRows = finalDoc.Root.Elements().Where(e => e.Name.LocalName == "Row").ToList();
            var originalRows = originalDoc.Root.Elements().Where(e => e.Name.LocalName == "Row").ToList();

            Console.WriteLine($"=== ROW COUNT COMPARISON ===");
            Console.WriteLine($"Original rows: {originalRows.Count}");
            Console.WriteLine($"Final rows: {finalRows.Count}");
            Console.WriteLine($"=== END ROW COUNT COMPARISON ===");

            Assert.AreEqual(originalRows.Count, finalRows.Count);

            for (int i = 0; i < originalRows.Count; i++)
            {
                Console.WriteLine($"=== COMPARING ROW {i} ===");
                Console.WriteLine($"Original: {originalRows[i]}");
                Console.WriteLine($"Final: {finalRows[i]}");
                Console.WriteLine($"=== END COMPARISON ===");

                Assert.AreEqual(originalRows[i].Elements().First(e => e.Name.LocalName == "id").Value, 
                               finalRows[i].Elements().First(e => e.Name.LocalName == "id").Value);
                Assert.AreEqual(originalRows[i].Elements().First(e => e.Name.LocalName == "name").Value, 
                               finalRows[i].Elements().First(e => e.Name.LocalName == "name").Value);
                Assert.AreEqual(originalRows[i].Elements().First(e => e.Name.LocalName == "reason").Value, 
                               finalRows[i].Elements().First(e => e.Name.LocalName == "reason").Value);
            }
        }
    }
} 