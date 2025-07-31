using System;
using System.Data;
using System.Data.SqlTypes;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureLibrary.SQL.Services;
using SecureLibrary.SQL.Interfaces;

namespace SecureLibrary.SQL.Tests
{
    /// <summary>
    /// Tests for SQL Server schema preservation, XML handling, and nullable column handling
    /// </summary>
    [TestClass]
    public class SchemaPreservationTests
    {
        private ICgnService _cgnService;
        private ISqlXmlConverter _xmlConverter;
        private IEncryptionEngine _encryptionEngine;

        [TestInitialize]
        public void Setup()
        {
            _cgnService = new CgnService();
            _xmlConverter = new SqlXmlConverter();
            _encryptionEngine = new EncryptionEngine(_cgnService, _xmlConverter);
        }

        [TestMethod]
        public void TestSqlServerSchemaPreservation()
        {
            // Create a test table with various SQL Server types
            var table = new DataTable("TestTable");
            
            // Add columns with different SQL Server types
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Columns["Name"].MaxLength = 50;
            table.Columns["Name"].AllowDBNull = false; // Non-nullable
            
            table.Columns.Add("Description", typeof(string));
            table.Columns["Description"].MaxLength = 200;
            table.Columns["Description"].AllowDBNull = true; // Nullable
            
            table.Columns.Add("Amount", typeof(decimal));
            table.Columns.Add("CreatedDate", typeof(DateTime));
            table.Columns.Add("IsActive", typeof(bool));
            table.Columns.Add("BinaryData", typeof(byte[]));
            table.Columns.Add("XmlData", typeof(SqlXml));
            table.Columns["XmlData"].AllowDBNull = true;

            // Add test data
            var row = table.NewRow();
            row["Id"] = 1;
            row["Name"] = "Test Name"; // Non-nullable string
            row["Description"] = DBNull.Value; // Nullable string with null
            row["Amount"] = 123.45m;
            row["CreatedDate"] = DateTime.Now;
            row["IsActive"] = true;
            row["BinaryData"] = new byte[] { 1, 2, 3, 4, 5 };
            row["XmlData"] = new SqlXml(XDocument.Parse("<root><item>test</item></root>").CreateReader());
            table.Rows.Add(row);

            // Test XML conversion with schema preservation
            var xmlDoc = _xmlConverter.ToXml(row);
            var xmlString = xmlDoc.ToString();

            // Verify XML contains SQL Server type information
            Assert.IsTrue(xmlString.Contains("SqlDbType"));
            Assert.IsTrue(xmlString.Contains("SqlTypeName"));
            Assert.IsTrue(xmlString.Contains("IsNullable"));

            // Verify specific type information
            var doc = XDocument.Parse(xmlString);
            var idColumn = doc.Root.Elements("Column").FirstOrDefault(c => c.Attribute("Name")?.Value == "Id");
            Assert.IsNotNull(idColumn);
            Assert.AreEqual("Int", idColumn.Attribute("SqlDbType")?.Value);

            var nameColumn = doc.Root.Elements("Column").FirstOrDefault(c => c.Attribute("Name")?.Value == "Name");
            Assert.IsNotNull(nameColumn);
            Assert.AreEqual("NVarChar", nameColumn.Attribute("SqlDbType")?.Value);
            Assert.AreEqual("false", nameColumn.Attribute("IsNullable")?.Value); // Non-nullable

            var descColumn = doc.Root.Elements("Column").FirstOrDefault(c => c.Attribute("Name")?.Value == "Description");
            Assert.IsNotNull(descColumn);
            Assert.AreEqual("true", descColumn.Attribute("IsNullable")?.Value); // Nullable

            var xmlColumn = doc.Root.Elements("Column").FirstOrDefault(c => c.Attribute("Name")?.Value == "XmlData");
            Assert.IsNotNull(xmlColumn);
            Assert.AreEqual("Xml", xmlColumn.Attribute("SqlDbType")?.Value);
            Assert.AreEqual("true", xmlColumn.Attribute("IsXml")?.Value);

            // Test round-trip conversion
            var restoredRow = _xmlConverter.FromXml(doc, table);
            
            // Verify data integrity
            Assert.AreEqual(1, restoredRow["Id"]);
            Assert.AreEqual("Test Name", restoredRow["Name"]);
            Assert.AreEqual(DBNull.Value, restoredRow["Description"]);
            Assert.AreEqual(123.45m, restoredRow["Amount"]);
            Assert.AreEqual(true, restoredRow["IsActive"]);
            Assert.IsTrue(restoredRow["BinaryData"] is byte[]);
            Assert.IsTrue(restoredRow["XmlData"] is SqlXml);
        }

        [TestMethod]
        public void TestNullableColumnHandling()
        {
            // Create a test table with nullable and non-nullable columns
            var table = new DataTable("NullableTest");
            
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("RequiredName", typeof(string));
            table.Columns["RequiredName"].AllowDBNull = false; // Non-nullable
            
            table.Columns.Add("OptionalName", typeof(string));
            table.Columns["OptionalName"].AllowDBNull = true; // Nullable

            // Test case 1: Non-nullable column with empty string
            var row1 = table.NewRow();
            row1["Id"] = 1;
            row1["RequiredName"] = ""; // Empty string for non-nullable column
            row1["OptionalName"] = DBNull.Value; // Null for nullable column
            table.Rows.Add(row1);

            // Convert to XML and back
            var xmlDoc = _xmlConverter.ToXml(row1);
            var restoredRow1 = _xmlConverter.FromXml(xmlDoc, table);

            // Verify empty string is preserved for non-nullable column
            Assert.AreEqual("", restoredRow1["RequiredName"]);
            Assert.AreEqual(DBNull.Value, restoredRow1["OptionalName"]);

            // Test case 2: Non-nullable column with null (should be converted to empty string)
            var row2 = table.NewRow();
            row2["Id"] = 2;
            row2["RequiredName"] = DBNull.Value; // This should be converted to empty string
            row2["OptionalName"] = "Some Value";
            // Don't add to table since it has DBNull.Value for non-nullable column

            var xmlDoc2 = _xmlConverter.ToXml(row2);
            var restoredRow2 = _xmlConverter.FromXml(xmlDoc2, table);

            // Verify null is converted to empty string for non-nullable column
            Assert.AreEqual("", restoredRow2["RequiredName"]);
            Assert.AreEqual("Some Value", restoredRow2["OptionalName"]);
        }

        [TestMethod]
        public void TestXmlTypeHandling()
        {
            // Create a test table with XML column
            var table = new DataTable("XmlTest");
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("XmlData", typeof(SqlXml));
            table.Columns["XmlData"].AllowDBNull = true;
            table.Columns["XmlData"].DefaultValue = DBNull.Value;

            // Test XML data
            var xmlContent = "<root><item id=\"1\">Test Item</item><item id=\"2\">Another Item</item></root>";
            var sqlXml = new SqlXml(XDocument.Parse(xmlContent).CreateReader());

            var row = table.NewRow();
            row["Id"] = 1;
            row["XmlData"] = sqlXml;
            table.Rows.Add(row);

            // Convert to XML and back
            var xmlDoc = _xmlConverter.ToXml(row);
            var xmlString = xmlDoc.ToString();

            // Verify XML type is properly marked
            Assert.IsTrue(xmlString.Contains("IsXml=\"true\""));

            var restoredRow = _xmlConverter.FromXml(xmlDoc, table);

            // Verify XML data is preserved
            Assert.IsTrue(restoredRow["XmlData"] is SqlXml);
            var restoredXml = (SqlXml)restoredRow["XmlData"];
            Assert.AreEqual(xmlContent, restoredXml.Value);

            // Test null XML
            var row2 = table.NewRow();
            row2["Id"] = 2;
            row2["XmlData"] = DBNull.Value;
            table.Rows.Add(row2);

            var xmlDoc2 = _xmlConverter.ToXml(row2);
            var restoredRow2 = _xmlConverter.FromXml(xmlDoc2, table);

            // For XML columns, DBNull.Value gets converted to SqlXml.Null by the DataRow
            var restoredXmlValue = restoredRow2["XmlData"];
            Assert.IsTrue(restoredXmlValue is SqlXml);
            var restoredSqlXml = (SqlXml)restoredXmlValue;
            Assert.IsTrue(restoredSqlXml.IsNull);
        }

        [TestMethod]
        public void TestEncryptionWithSchemaPreservation()
        {
            // Create a test table with various types
            var table = new DataTable("EncryptionTest");
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Columns["Name"].MaxLength = 100;
            table.Columns["Name"].AllowDBNull = false;
            
            table.Columns.Add("Description", typeof(string));
            table.Columns["Description"].AllowDBNull = true;
            
            table.Columns.Add("Amount", typeof(decimal));
            table.Columns.Add("XmlData", typeof(SqlXml));
            table.Columns["XmlData"].AllowDBNull = true;

            // Add test data
            var row = table.NewRow();
            row["Id"] = 1;
            row["Name"] = "Test Name";
            row["Description"] = DBNull.Value;
            row["Amount"] = 999.99m;
            row["XmlData"] = new SqlXml(XDocument.Parse("<data><value>test</value></data>").CreateReader());
            table.Rows.Add(row);

            // Create encryption metadata
            var metadata = new EncryptionMetadata
            {
                Algorithm = "AES-GCM",
                Key = "TestPassword123!",
                Salt = _cgnService.GenerateNonce(32),
                Iterations = 10000,
                AutoGenerateNonce = true
            };

            // Encrypt the row
            var encryptedData = _encryptionEngine.EncryptRow(row, metadata);

            // Verify SQL Server schema is preserved
            Assert.IsNotNull(encryptedData.SqlServerSchema);
            Assert.AreEqual(5, encryptedData.SqlServerSchema.Count);

            // Verify specific schema information
            var idSchema = encryptedData.SqlServerSchema.FirstOrDefault(s => s.Name == "Id");
            Assert.IsNotNull(idSchema);
            Assert.AreEqual(SqlDbType.Int, idSchema.SqlDbType);
            Assert.AreEqual("INT", idSchema.SqlTypeName);

            var nameSchema = encryptedData.SqlServerSchema.FirstOrDefault(s => s.Name == "Name");
            Assert.IsNotNull(nameSchema);
            Assert.AreEqual(SqlDbType.NVarChar, nameSchema.SqlDbType);
            Assert.AreEqual("NVARCHAR(100)", nameSchema.SqlTypeName);
            Assert.IsFalse(nameSchema.IsNullable);

            var descSchema = encryptedData.SqlServerSchema.FirstOrDefault(s => s.Name == "Description");
            Assert.IsNotNull(descSchema);
            Assert.IsTrue(descSchema.IsNullable);

            var xmlSchema = encryptedData.SqlServerSchema.FirstOrDefault(s => s.Name == "XmlData");
            Assert.IsNotNull(xmlSchema);
            Assert.AreEqual(SqlDbType.Xml, xmlSchema.SqlDbType);
            Assert.AreEqual("XML", xmlSchema.SqlTypeName);

            // Decrypt the row
            var decryptedRow = _encryptionEngine.DecryptRow(encryptedData, metadata);

            // Verify data integrity
            Assert.AreEqual(1, decryptedRow["Id"]);
            Assert.AreEqual("Test Name", decryptedRow["Name"]);
            Assert.AreEqual(DBNull.Value, decryptedRow["Description"]);
            Assert.AreEqual(999.99m, decryptedRow["Amount"]);
            Assert.IsTrue(decryptedRow["XmlData"] is SqlXml);
        }

        [TestMethod]
        public void TestEmptyStringHandlingForNonNullableColumns()
        {
            // Create a test table with non-nullable string column
            var table = new DataTable("EmptyStringTest");
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("RequiredField", typeof(string));
            table.Columns["RequiredField"].AllowDBNull = false; // Non-nullable
            table.Columns["RequiredField"].MaxLength = 50;

            // Test with empty string
            var row = table.NewRow();
            row["Id"] = 1;
            row["RequiredField"] = ""; // Empty string
            table.Rows.Add(row);

            // Convert to XML and back
            var xmlDoc = _xmlConverter.ToXml(row);
            var restoredRow = _xmlConverter.FromXml(xmlDoc, table);

            // Verify empty string is preserved
            Assert.AreEqual("", restoredRow["RequiredField"]);

            // Test encryption and decryption with empty string
            var metadata = new EncryptionMetadata
            {
                Algorithm = "AES-GCM",
                Key = "TestPassword123!",
                Salt = _cgnService.GenerateNonce(32),
                Iterations = 10000,
                AutoGenerateNonce = true
            };

            var encryptedData = _encryptionEngine.EncryptRow(row, metadata);
            var decryptedRow = _encryptionEngine.DecryptRow(encryptedData, metadata);

            // Verify empty string is preserved through encryption/decryption
            Assert.AreEqual("", decryptedRow["RequiredField"]);
        }
    }
} 