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

        [TestMethod]
        public void TestCharNCharSpacePadding()
        {
            // Create a test table with char and nchar columns
            var table = new DataTable("CharPaddingTest");
            table.Columns.Add("Id", typeof(int));
            
            // Add char column with maxLength 10
            table.Columns.Add("CharField", typeof(string));
            table.Columns["CharField"].MaxLength = 10;
            table.Columns["CharField"].AllowDBNull = false;
            
            // Add nchar column with maxLength 8
            table.Columns.Add("NCharField", typeof(string));
            table.Columns["NCharField"].MaxLength = 8;
            table.Columns["NCharField"].AllowDBNull = false;

            // Test case 1: Short values that need padding
            var row1 = table.NewRow();
            row1["Id"] = 1;
            row1["CharField"] = "ABC"; // Should be padded to "ABC       " (7 spaces)
            row1["NCharField"] = "XY"; // Should be padded to "XY      " (6 spaces)
            table.Rows.Add(row1);

            // Test encryption and decryption with char/nchar padding
            var metadata = new EncryptionMetadata
            {
                Algorithm = "AES-GCM",
                Key = "TestPassword123!",
                Salt = _cgnService.GenerateNonce(32),
                Iterations = 10000,
                AutoGenerateNonce = true
            };

            var encryptedData = _encryptionEngine.EncryptRow(row1, metadata);
            
            // Verify SQL Server schema is preserved with correct types
            Assert.IsNotNull(encryptedData.SqlServerSchema);
            Assert.AreEqual(3, encryptedData.SqlServerSchema.Count);

            // Verify the schema has the correct SQL types
            var charFieldSchema = encryptedData.SqlServerSchema.FirstOrDefault(s => s.Name == "CharField");
            Assert.IsNotNull(charFieldSchema);
            Assert.AreEqual(SqlDbType.NVarChar, charFieldSchema.SqlDbType); // This will be NVarChar since we can't distinguish from CLR type
            
            var ncharFieldSchema = encryptedData.SqlServerSchema.FirstOrDefault(s => s.Name == "NCharField");
            Assert.IsNotNull(ncharFieldSchema);
            Assert.AreEqual(SqlDbType.NVarChar, ncharFieldSchema.SqlDbType); // This will be NVarChar since we can't distinguish from CLR type

            var decryptedRow = _encryptionEngine.DecryptRow(encryptedData, metadata);

            // Verify padding is preserved through encryption/decryption
            // Note: Since we can't distinguish char/nchar from CLR type, the values won't be padded
            // This test demonstrates the current limitation
            Assert.AreEqual("ABC", decryptedRow["CharField"]);
            Assert.AreEqual("XY", decryptedRow["NCharField"]);
        }

        [TestMethod]
        public void TestCharNCharSpacePaddingWithCustomSchema()
        {
            // This test demonstrates how to handle char/nchar types with custom schema information
            // Create a test table with char and nchar columns
            var table = new DataTable("CharPaddingTest");
            table.Columns.Add("Id", typeof(int));
            
            // Add char column with maxLength 10
            table.Columns.Add("CharField", typeof(string));
            table.Columns["CharField"].MaxLength = 10;
            table.Columns["CharField"].AllowDBNull = false;
            
            // Add nchar column with maxLength 8
            table.Columns.Add("NCharField", typeof(string));
            table.Columns["NCharField"].MaxLength = 8;
            table.Columns["NCharField"].AllowDBNull = false;

            // Test case 1: Short values that need padding
            var row1 = table.NewRow();
            row1["Id"] = 1;
            row1["CharField"] = "ABC"; // Should be padded to "ABC       " (7 spaces)
            row1["NCharField"] = "XY"; // Should be padded to "XY      " (6 spaces)
            table.Rows.Add(row1);

            // Create custom XML with explicit SQL type information
            var doc = new XDocument();
            var root = new XElement("Row");
            doc.Add(root);

            // Add columns with explicit SQL type information
            var idColumn = new XElement("Column",
                new XAttribute("Name", "Id"),
                new XAttribute("Type", "Int32"),
                new XAttribute("SqlDbType", "Int"),
                new XAttribute("SqlTypeName", "INT"),
                new XAttribute("MaxLength", "-1"),
                new XAttribute("IsNullable", "false"),
                new XAttribute("Ordinal", "0"),
                new XAttribute("IsNull", "false")
            );
            idColumn.Value = "1";
            root.Add(idColumn);

            var charColumn = new XElement("Column",
                new XAttribute("Name", "CharField"),
                new XAttribute("Type", "String"),
                new XAttribute("SqlDbType", "Char"), // Explicitly set as Char
                new XAttribute("SqlTypeName", "CHAR(10)"),
                new XAttribute("MaxLength", "10"),
                new XAttribute("IsNullable", "false"),
                new XAttribute("Ordinal", "1"),
                new XAttribute("IsNull", "false")
            );
            charColumn.Value = "ABC";
            root.Add(charColumn);

            var ncharColumn = new XElement("Column",
                new XAttribute("Name", "NCharField"),
                new XAttribute("Type", "String"),
                new XAttribute("SqlDbType", "NChar"), // Explicitly set as NChar
                new XAttribute("SqlTypeName", "NCHAR(8)"),
                new XAttribute("MaxLength", "8"),
                new XAttribute("IsNullable", "false"),
                new XAttribute("Ordinal", "2"),
                new XAttribute("IsNull", "false")
            );
            ncharColumn.Value = "XY";
            root.Add(ncharColumn);

            // Convert from XML with explicit SQL type information
            var restoredRow1 = _xmlConverter.FromXml(doc, table);

            // Verify char field is padded with spaces
            Assert.AreEqual("ABC       ", restoredRow1["CharField"]);
            Assert.AreEqual(10, restoredRow1["CharField"].ToString().Length);
            
            // Verify nchar field is padded with spaces
            Assert.AreEqual("XY      ", restoredRow1["NCharField"]);
            Assert.AreEqual(8, restoredRow1["NCharField"].ToString().Length);

            // Test case 2: Empty string should be padded to full length
            var doc2 = new XDocument();
            var root2 = new XElement("Row");
            doc2.Add(root2);

            var idColumn2 = new XElement("Column",
                new XAttribute("Name", "Id"),
                new XAttribute("Type", "Int32"),
                new XAttribute("SqlDbType", "Int"),
                new XAttribute("SqlTypeName", "INT"),
                new XAttribute("MaxLength", "-1"),
                new XAttribute("IsNullable", "false"),
                new XAttribute("Ordinal", "0"),
                new XAttribute("IsNull", "false")
            );
            idColumn2.Value = "2";
            root2.Add(idColumn2);

            var charColumn2 = new XElement("Column",
                new XAttribute("Name", "CharField"),
                new XAttribute("Type", "String"),
                new XAttribute("SqlDbType", "Char"),
                new XAttribute("SqlTypeName", "CHAR(10)"),
                new XAttribute("MaxLength", "10"),
                new XAttribute("IsNullable", "false"),
                new XAttribute("Ordinal", "1"),
                new XAttribute("IsNull", "false")
            );
            charColumn2.Value = "";
            root2.Add(charColumn2);

            var ncharColumn2 = new XElement("Column",
                new XAttribute("Name", "NCharField"),
                new XAttribute("Type", "String"),
                new XAttribute("SqlDbType", "NChar"),
                new XAttribute("SqlTypeName", "NCHAR(8)"),
                new XAttribute("MaxLength", "8"),
                new XAttribute("IsNullable", "false"),
                new XAttribute("Ordinal", "2"),
                new XAttribute("IsNull", "false")
            );
            ncharColumn2.Value = "";
            root2.Add(ncharColumn2);

            var restoredRow2 = _xmlConverter.FromXml(doc2, table);

            // Verify empty strings are padded to full length
            Assert.AreEqual("          ", restoredRow2["CharField"]);
            Assert.AreEqual(10, restoredRow2["CharField"].ToString().Length);
            
            Assert.AreEqual("        ", restoredRow2["NCharField"]);
            Assert.AreEqual(8, restoredRow2["NCharField"].ToString().Length);
        }

        [TestMethod]
        public void TestRealWorldCharNCharScenario()
        {
            // This test simulates a real-world scenario where data comes from SQL Server
            // with char/nchar columns and needs to be processed through the encryption system
            
            // Simulate data coming from SQL Server with explicit SQL type information
            var table = new DataTable("CustomerTable");
            table.Columns.Add("CustomerId", typeof(int));
            table.Columns.Add("CustomerCode", typeof(string)); // CHAR(10) in SQL Server
            table.Columns["CustomerCode"].MaxLength = 10;
            table.Columns["CustomerCode"].AllowDBNull = false;
            
            table.Columns.Add("CustomerName", typeof(string)); // NCHAR(20) in SQL Server
            table.Columns["CustomerName"].MaxLength = 20;
            table.Columns["CustomerName"].AllowDBNull = false;
            
            table.Columns.Add("Status", typeof(string)); // CHAR(1) in SQL Server
            table.Columns["Status"].MaxLength = 1;
            table.Columns["Status"].AllowDBNull = false;

            // Add test data
            var row = table.NewRow();
            row["CustomerId"] = 1001;
            row["CustomerCode"] = "CUST001"; // 7 chars, should be padded to 10
            row["CustomerName"] = "John Doe"; // 8 chars, should be padded to 20
            row["Status"] = "A"; // 1 char, already at max length
            table.Rows.Add(row);

            // Create XML with explicit SQL type information (as would come from SQL Server)
            var doc = new XDocument();
            var root = new XElement("Row");
            doc.Add(root);

            var customerIdColumn = new XElement("Column",
                new XAttribute("Name", "CustomerId"),
                new XAttribute("Type", "Int32"),
                new XAttribute("SqlDbType", "Int"),
                new XAttribute("SqlTypeName", "INT"),
                new XAttribute("MaxLength", "-1"),
                new XAttribute("IsNullable", "false"),
                new XAttribute("Ordinal", "0"),
                new XAttribute("IsNull", "false")
            );
            customerIdColumn.Value = "1001";
            root.Add(customerIdColumn);

            var customerCodeColumn = new XElement("Column",
                new XAttribute("Name", "CustomerCode"),
                new XAttribute("Type", "String"),
                new XAttribute("SqlDbType", "Char"), // Explicitly CHAR type
                new XAttribute("SqlTypeName", "CHAR(10)"),
                new XAttribute("MaxLength", "10"),
                new XAttribute("IsNullable", "false"),
                new XAttribute("Ordinal", "1"),
                new XAttribute("IsNull", "false")
            );
            customerCodeColumn.Value = "CUST001";
            root.Add(customerCodeColumn);

            var customerNameColumn = new XElement("Column",
                new XAttribute("Name", "CustomerName"),
                new XAttribute("Type", "String"),
                new XAttribute("SqlDbType", "NChar"), // Explicitly NCHAR type
                new XAttribute("SqlTypeName", "NCHAR(20)"),
                new XAttribute("MaxLength", "20"),
                new XAttribute("IsNullable", "false"),
                new XAttribute("Ordinal", "2"),
                new XAttribute("IsNull", "false")
            );
            customerNameColumn.Value = "John Doe";
            root.Add(customerNameColumn);

            var statusColumn = new XElement("Column",
                new XAttribute("Name", "Status"),
                new XAttribute("Type", "String"),
                new XAttribute("SqlDbType", "Char"), // Explicitly CHAR type
                new XAttribute("SqlTypeName", "CHAR(1)"),
                new XAttribute("MaxLength", "1"),
                new XAttribute("IsNullable", "false"),
                new XAttribute("Ordinal", "3"),
                new XAttribute("IsNull", "false")
            );
            statusColumn.Value = "A";
            root.Add(statusColumn);

            // Convert from XML with proper SQL type information
            var restoredRow = _xmlConverter.FromXml(doc, table);

            // Verify proper padding is applied
            Assert.AreEqual(1001, restoredRow["CustomerId"]);
            Assert.AreEqual("CUST001   ", restoredRow["CustomerCode"]); // Padded to 10 chars
            Assert.AreEqual(10, restoredRow["CustomerCode"].ToString().Length);
            Assert.AreEqual("John Doe            ", restoredRow["CustomerName"]); // Padded to 20 chars
            Assert.AreEqual(20, restoredRow["CustomerName"].ToString().Length);
            Assert.AreEqual("A", restoredRow["Status"]); // No padding needed, already at max length
            Assert.AreEqual(1, restoredRow["Status"].ToString().Length);

            // Test encryption and decryption with proper padding
            var metadata = new EncryptionMetadata
            {
                Algorithm = "AES-GCM",
                Key = "TestPassword123!",
                Salt = _cgnService.GenerateNonce(32),
                Iterations = 10000,
                AutoGenerateNonce = true
            };

            var encryptedData = _encryptionEngine.EncryptRow(restoredRow, metadata);
            var decryptedRow = _encryptionEngine.DecryptRow(encryptedData, metadata);

            // Verify padding is preserved through encryption/decryption
            Assert.AreEqual(1001, decryptedRow["CustomerId"]);
            Assert.AreEqual("CUST001   ", decryptedRow["CustomerCode"]);
            Assert.AreEqual(10, decryptedRow["CustomerCode"].ToString().Length);
            Assert.AreEqual("John Doe            ", decryptedRow["CustomerName"]);
            Assert.AreEqual(20, decryptedRow["CustomerName"].ToString().Length);
            Assert.AreEqual("A", decryptedRow["Status"]);
            Assert.AreEqual(1, decryptedRow["Status"].ToString().Length);
        }
    }
} 