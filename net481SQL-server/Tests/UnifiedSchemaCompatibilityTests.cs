using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureLibrary.SQL.Services;

namespace SecureLibrary.SQL.Tests
{
    /// <summary>
    /// Tests for backward compatibility with existing xmlresult18.xml format
    /// </summary>
    [TestClass]
    public class UnifiedSchemaCompatibilityTests
    {
        private string _existingXmlContent;

        [TestInitialize]
        public void Setup()
        {
            // Load the existing xmlresult18.xml content
            var xmlFilePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "xmlresult18.xml");
            if (!File.Exists(xmlFilePath))
            {
                // Try alternative path
                xmlFilePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "xmlresult18.xml");
            }
            
            if (!File.Exists(xmlFilePath))
            {
                throw new FileNotFoundException($"Could not find xmlresult18.xml file. Tried paths: {xmlFilePath}");
            }

            _existingXmlContent = File.ReadAllText(xmlFilePath, Encoding.UTF8);
        }

        [TestMethod]
        public void TestUnifiedSchema_DetectExistingFormat()
        {
            // Arrange
            var xmlDoc = XDocument.Parse(_existingXmlContent);
            var root = xmlDoc.Root;

            // Act
            var formatType = EncryptionXmlSchema.DetectFormat(root);

            // Assert
            Assert.AreEqual(XmlFormatType.SingleRow, formatType);
        }

        [TestMethod]
        public void TestUnifiedSchema_ValidateExistingFormat()
        {
            // Arrange
            var xmlDoc = XDocument.Parse(_existingXmlContent);
            var root = xmlDoc.Root;

            // Act
            var isValid = EncryptionXmlSchema.ValidateSingleRowXml(root);

            // Assert
            Assert.IsTrue(isValid);
        }

        [TestMethod]
        public void TestUnifiedSchema_ParseExistingFormat()
        {
            // Arrange
            var xmlDoc = XDocument.Parse(_existingXmlContent);
            var root = xmlDoc.Root;
            var password = "TestPassword123!@#";

            // Act
            var (schema, encryptedData, metadata) = EncryptionXmlSchema.ParseSingleRowXml(root, password);

            // Assert
            Assert.IsNotNull(schema);
            Assert.IsNotNull(encryptedData);
            Assert.IsNotNull(metadata);
            
            // Verify schema has expected columns
            Assert.IsTrue(schema.Columns.Count > 0);
            Assert.IsTrue(schema.Columns.Contains("emp_id"));
            Assert.IsTrue(schema.Columns.Contains("emp_nm"));
            
            // Verify metadata has expected values
            Assert.AreEqual("AES-GCM", metadata.Algorithm);
            Assert.AreEqual(1000, metadata.Iterations);
            Assert.IsNotNull(metadata.Salt);
            Assert.IsNotNull(metadata.Nonce);
        }

        [TestMethod]
        public void TestUnifiedSchema_CreateExistingFormat()
        {
            // Arrange
            var schema = new DataTable();
            schema.Columns.Add("emp_id", typeof(string));
            schema.Columns.Add("emp_nm", typeof(string));
            
            var encryptedData = "test_encrypted_data_base64";
            var metadata = new EncryptionMetadata
            {
                Algorithm = "AES-GCM",
                Key = "TestPassword123!@#",
                Salt = new byte[16],
                Nonce = new byte[12],
                Iterations = 1000,
                AutoGenerateNonce = false
            };

            // Act
            var xml = EncryptionXmlSchema.CreateSingleRowXml(schema, encryptedData, metadata);

            // Assert
            Assert.IsNotNull(xml);
            Assert.AreEqual("EncryptedRow", xml.Name.LocalName);
            
            // Verify structure matches existing format
            Assert.IsNotNull(xml.Element("Schema"));
            Assert.IsNotNull(xml.Element("Metadata"));
            Assert.IsNotNull(xml.Element("EncryptedData"));
            
            // Verify metadata structure
            var metadataElement = xml.Element("Metadata");
            Assert.IsNotNull(metadataElement.Element("Algorithm"));
            Assert.IsNotNull(metadataElement.Element("Iterations"));
            Assert.IsNotNull(metadataElement.Element("Salt"));
            Assert.IsNotNull(metadataElement.Element("Nonce"));
        }

        [TestMethod]
        public void TestUnifiedSchema_FormatDetection()
        {
            // Test single row format detection
            var singleRowXml = new XElement("EncryptedRow",
                new XElement("Schema"),
                new XElement("Metadata"),
                new XElement("EncryptedData")
            );
            Assert.AreEqual(XmlFormatType.SingleRow, EncryptionXmlSchema.DetectFormat(singleRowXml));

            // Test multi-row format detection
            var multiRowXml = new XElement("EncryptedData",
                new XElement("BatchMetadata"),
                new XElement("Rows")
            );
            Assert.AreEqual(XmlFormatType.MultiRow, EncryptionXmlSchema.DetectFormat(multiRowXml));

            // Test single value format detection
            var singleValueXml = new XElement("EncryptedValue",
                new XElement("DataType"),
                new XElement("EncryptedData"),
                new XElement("Metadata")
            );
            Assert.AreEqual(XmlFormatType.SingleValue, EncryptionXmlSchema.DetectFormat(singleValueXml));

            // Test unknown format detection
            var unknownXml = new XElement("UnknownElement");
            Assert.AreEqual(XmlFormatType.Unknown, EncryptionXmlSchema.DetectFormat(unknownXml));
        }
    }
} 