using System;
using System.Data;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureLibrary.SQL.Services;
using SecureLibrary.SQL.Interfaces;

namespace SecureLibrary.SQL.Tests
{
    [TestClass]
    public class DecryptionDebugTest
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

        public class TestLogger : ILogger
        {
            public void LogInformation(string message) { }
            public void LogWarning(string message) { }
            public void LogError(string message) { }
        }

        [TestMethod]
        public void TestDecryptProvidedEncryptedRow()
        {
            // The provided EncryptedRow XML and password
            string encryptedRowXml = @"<EncryptedRow>
  <Schema>
    <Column Name=""id_no"" Type=""System.String"" SqlDbType=""NVarChar"" MaxLength=""13"" IsNullable=""true"" />
    <Column Name=""name"" Type=""System.String"" SqlDbType=""NVarChar"" MaxLength=""50"" IsNullable=""true"" />
    <Column Name=""age"" Type=""System.Int32"" SqlDbType=""Int"" IsNullable=""true"" />
  </Schema>
  <Metadata>
    <Key>test123</Key>
    <Salt>AQIDBAUGBwgJCgsMDQ4PEA==</Salt>
    <Iterations>10000</Iterations>
    <Nonce>AQIDBAUGBwgJCgsMDQ4PEA==</Nonce>
  </Metadata>
  <EncryptedData>
    <Column Name=""RowData"">AQIDBAUGBwgJCgsMDQ4PEA==</Column>
    <Column Name=""Nonce"">AQIDBAUGBwgJCgsMDQ4PEA==</Column>
  </EncryptedData>
</EncryptedRow>";

            try
            {
                // Parse the encrypted row XML
                var xmlDoc = XDocument.Parse(encryptedRowXml);
                var schemaElement = xmlDoc.Root.Element("Schema");
                var metadataElement = xmlDoc.Root.Element("Metadata");
                var encryptedDataElement = xmlDoc.Root.Element("EncryptedData");

                if (schemaElement == null || metadataElement == null || encryptedDataElement == null)
                {
                    Assert.Fail("Invalid encrypted row format - missing required elements");
                }

                // Reconstruct the schema
                var dataTable = new DataTable();
                foreach (var columnElement in schemaElement.Elements("Column"))
                {
                    var columnName = columnElement.Attribute("Name")?.Value;
                    var typeName = columnElement.Attribute("Type")?.Value;
                    var isNullable = columnElement.Attribute("IsNullable")?.Value == "true";

                    if (!string.IsNullOrEmpty(columnName) && !string.IsNullOrEmpty(typeName))
                    {
                        var dataType = Type.GetType(typeName) ?? typeof(string);
                        var column = new DataColumn(columnName, dataType)
                        {
                            AllowDBNull = isNullable
                        };
                        dataTable.Columns.Add(column);
                    }
                }

                // Reconstruct the metadata
                var metadata = new EncryptionMetadata
                {
                    Key = metadataElement.Element("Key")?.Value,
                    Salt = Convert.FromBase64String(metadataElement.Element("Salt")?.Value ?? ""),
                    Iterations = int.Parse(metadataElement.Element("Iterations")?.Value ?? "10000"),
                    Nonce = Convert.FromBase64String(metadataElement.Element("Nonce")?.Value ?? "")
                };

                // Reconstruct the encrypted data
                var encryptedRowData = new EncryptedRowData
                {
                    Schema = dataTable
                };

                foreach (var columnElement in encryptedDataElement.Elements("Column"))
                {
                    var columnName = columnElement.Attribute("Name")?.Value;
                    var columnValue = columnElement.Value;

                    if (!string.IsNullOrEmpty(columnName) && !string.IsNullOrEmpty(columnValue))
                    {
                        encryptedRowData.EncryptedColumns[columnName] = Convert.FromBase64String(columnValue);
                    }
                }

                // Attempt to decrypt
                var decryptedRow = _encryptionEngine.DecryptRow(encryptedRowData, metadata);

                // Check the id_no column specifically
                var idNoValue = decryptedRow["id_no"];
                Console.WriteLine($"id_no value type: {idNoValue?.GetType()}");
                Console.WriteLine($"id_no value: {idNoValue}");
                Console.WriteLine($"id_no is DBNull: {idNoValue == DBNull.Value}");

                // Assert that id_no is not DBNull
                Assert.AreNotEqual(DBNull.Value, idNoValue, "id_no column should not be DBNull after decryption");

                // Print all column values for debugging
                foreach (DataColumn column in dataTable.Columns)
                {
                    var value = decryptedRow[column.ColumnName];
                    Console.WriteLine($"{column.ColumnName}: {value} (Type: {value?.GetType()}, IsDBNull: {value == DBNull.Value})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Decryption failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        [TestMethod]
        public void TestDecryptWithRealData()
        {
            // Create a test table with the same schema
            var dataTable = new DataTable();
            dataTable.Columns.Add("id_no", typeof(string));
            dataTable.Columns.Add("name", typeof(string));
            dataTable.Columns.Add("age", typeof(int));

            // Add a test row
            var row = dataTable.NewRow();
            row["id_no"] = "1234567890123";
            row["name"] = "Test User";
            row["age"] = 30;
            dataTable.Rows.Add(row);

            // Create encryption metadata
            var metadata = new EncryptionMetadata
            {
                Key = "test123",
                Salt = new byte[16], // Will be generated
                Iterations = 10000
            };

            // Encrypt the row
            var encryptedData = _encryptionEngine.EncryptRow(row, metadata);

            // Decrypt the row
            var decryptedRow = _encryptionEngine.DecryptRow(encryptedData, metadata);

            // Check the id_no column
            var idNoValue = decryptedRow["id_no"];
            Console.WriteLine($"Encrypted and decrypted id_no: {idNoValue}");
            Assert.AreEqual("1234567890123", idNoValue);
            Assert.AreNotEqual(DBNull.Value, idNoValue);
        }

        [TestMethod]
        public void TestIdNoBecomesDBNullScenario()
        {
            // This test simulates the scenario where id_no might become DBNull
            // Create a test table with the same schema as the provided XML
            var dataTable = new DataTable();
            dataTable.Columns.Add("id_no", typeof(string));
            dataTable.Columns.Add("name", typeof(string));
            dataTable.Columns.Add("age", typeof(int));

            // Test various scenarios that might cause id_no to become DBNull

            // Scenario 1: Empty string value
            var row1 = dataTable.NewRow();
            row1["id_no"] = ""; // Empty string
            row1["name"] = "Test User";
            row1["age"] = 30;
            dataTable.Rows.Add(row1);

            // Scenario 2: Null value
            var row2 = dataTable.NewRow();
            row2["id_no"] = DBNull.Value; // Explicit null
            row2["name"] = "Test User";
            row2["age"] = 30;
            dataTable.Rows.Add(row2);

            // Scenario 3: Whitespace-only string
            var row3 = dataTable.NewRow();
            row3["id_no"] = "   "; // Whitespace only
            row3["name"] = "Test User";
            row3["age"] = 30;
            dataTable.Rows.Add(row3);

            // Scenario 4: Normal value
            var row4 = dataTable.NewRow();
            row4["id_no"] = "1234567890123";
            row4["name"] = "Test User";
            row4["age"] = 30;
            dataTable.Rows.Add(row4);

            // Create encryption metadata
            var metadata = new EncryptionMetadata
            {
                Key = "test123",
                Salt = new byte[16],
                Iterations = 10000
            };

            // Test each scenario
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                var originalRow = dataTable.Rows[i];
                var originalIdNo = originalRow["id_no"];
                
                Console.WriteLine($"\nTesting scenario {i + 1}:");
                Console.WriteLine($"Original id_no: '{originalIdNo}' (Type: {originalIdNo?.GetType()}, IsDBNull: {originalIdNo == DBNull.Value})");

                // Encrypt the row
                var encryptedData = _encryptionEngine.EncryptRow(originalRow, metadata);

                // Decrypt the row
                var decryptedRow = _encryptionEngine.DecryptRow(encryptedData, metadata);

                // Check the id_no column
                var decryptedIdNo = decryptedRow["id_no"];
                Console.WriteLine($"Decrypted id_no: '{decryptedIdNo}' (Type: {decryptedIdNo?.GetType()}, IsDBNull: {decryptedIdNo == DBNull.Value})");

                // Assert that the value is preserved correctly
                if (originalIdNo == DBNull.Value)
                {
                    Assert.AreEqual(DBNull.Value, decryptedIdNo, $"Scenario {i + 1}: DBNull should remain DBNull");
                }
                else if (string.IsNullOrEmpty(originalIdNo?.ToString()))
                {
                    // Empty strings should be preserved as empty strings, not converted to DBNull
                    Assert.AreEqual("", decryptedIdNo, $"Scenario {i + 1}: Empty string should remain empty string");
                    Assert.AreNotEqual(DBNull.Value, decryptedIdNo, $"Scenario {i + 1}: Empty string should not become DBNull");
                }
                else
                {
                    Assert.AreEqual(originalIdNo, decryptedIdNo, $"Scenario {i + 1}: Value should be preserved");
                    Assert.AreNotEqual(DBNull.Value, decryptedIdNo, $"Scenario {i + 1}: Non-null value should not become DBNull");
                }
            }
        }

        [TestMethod]
        public void TestXmlConversionEdgeCases()
        {
            // Test the XML conversion process directly to see where DBNull might be introduced
            var dataTable = new DataTable();
            dataTable.Columns.Add("id_no", typeof(string));
            dataTable.Columns.Add("name", typeof(string));
            dataTable.Columns.Add("age", typeof(int));

            // Test various edge cases
            TestXmlConversionCase(dataTable, "1234567890123", "Normal value");
            TestXmlConversionCase(dataTable, "", "Empty string");
            TestXmlConversionCase(dataTable, "   ", "Whitespace only");
            TestXmlConversionCase(dataTable, DBNull.Value, "DBNull");
            TestXmlConversionCase(dataTable, null, "Null");
        }

        private void TestXmlConversionCase(DataTable dataTable, object value, string description)
        {
            Console.WriteLine($"\nTesting: {description}");
            
            var row = dataTable.NewRow();
            row["id_no"] = value;
            row["name"] = "Test User";
            row["age"] = 30;

            Console.WriteLine($"Original id_no: '{row["id_no"]}' (Type: {row["id_no"]?.GetType()}, IsDBNull: {row["id_no"] == DBNull.Value})");

            // Convert to XML
            var xmlDoc = _xmlConverter.ToXml(row);
            Console.WriteLine($"XML: {xmlDoc}");

            // Convert back from XML
            var convertedRow = _xmlConverter.FromXml(xmlDoc, dataTable);
            Console.WriteLine($"Converted id_no: '{convertedRow["id_no"]}' (Type: {convertedRow["id_no"]?.GetType()}, IsDBNull: {convertedRow["id_no"] == DBNull.Value})");

            // Assert that the conversion preserves the value correctly
            if (value == DBNull.Value)
            {
                Assert.AreEqual(DBNull.Value, convertedRow["id_no"], $"{description}: DBNull should remain DBNull");
            }
            else if (value == null)
            {
                Assert.AreEqual(DBNull.Value, convertedRow["id_no"], $"{description}: null should become DBNull");
            }
            else if (string.IsNullOrEmpty(value.ToString()))
            {
                Assert.AreEqual("", convertedRow["id_no"], $"{description}: Empty string should remain empty string");
                Assert.AreNotEqual(DBNull.Value, convertedRow["id_no"], $"{description}: Empty string should not become DBNull");
            }
            else
            {
                Assert.AreEqual(value, convertedRow["id_no"], $"{description}: Value should be preserved");
                Assert.AreNotEqual(DBNull.Value, convertedRow["id_no"], $"{description}: Non-null value should not become DBNull");
            }
        }

        [TestMethod]
        public void TestUserScenarioIdNoColumn()
        {
            // This test specifically simulates the user's scenario where id_no is NVARCHAR(13)
            // and should not become DBNull after decryption
            var dataTable = new DataTable();
            
            // Create the exact schema from the user's provided XML
            var idNoColumn = new DataColumn("id_no", typeof(string))
            {
                AllowDBNull = true,
                MaxLength = 13
            };
            dataTable.Columns.Add(idNoColumn);
            
            var nameColumn = new DataColumn("name", typeof(string))
            {
                AllowDBNull = true,
                MaxLength = 50
            };
            dataTable.Columns.Add(nameColumn);
            
            var ageColumn = new DataColumn("age", typeof(int))
            {
                AllowDBNull = true
            };
            dataTable.Columns.Add(ageColumn);

            // Test various scenarios that the user might encounter
            TestUserScenarioCase(dataTable, "1234567890123", "Full 13-character ID");
            TestUserScenarioCase(dataTable, "123456789", "Partial ID (9 characters)");
            TestUserScenarioCase(dataTable, "123", "Short ID (3 characters)");
            TestUserScenarioCase(dataTable, "", "Empty string ID");
            TestUserScenarioCase(dataTable, "   ", "Whitespace-only ID");
            TestUserScenarioCase(dataTable, " 123 ", "ID with leading/trailing spaces");
            TestUserScenarioCase(dataTable, DBNull.Value, "DBNull ID");
        }

        private void TestUserScenarioCase(DataTable dataTable, object idNoValue, string description)
        {
            Console.WriteLine($"\n=== Testing: {description} ===");
            
            var row = dataTable.NewRow();
            row["id_no"] = idNoValue;
            row["name"] = "Test User";
            row["age"] = 30;

            Console.WriteLine($"Original id_no: '{row["id_no"]}' (Type: {row["id_no"]?.GetType()}, IsDBNull: {row["id_no"] == DBNull.Value})");

            // Create encryption metadata
            var metadata = new EncryptionMetadata
            {
                Key = "test123",
                Salt = new byte[16],
                Iterations = 10000
            };

            // Encrypt the row
            var encryptedData = _encryptionEngine.EncryptRow(row, metadata);

            // Decrypt the row
            var decryptedRow = _encryptionEngine.DecryptRow(encryptedData, metadata);

            // Check the id_no column
            var decryptedIdNo = decryptedRow["id_no"];
            Console.WriteLine($"Decrypted id_no: '{decryptedIdNo}' (Type: {decryptedIdNo?.GetType()}, IsDBNull: {decryptedIdNo == DBNull.Value})");

            // Assert that the value is preserved correctly
            if (idNoValue == DBNull.Value)
            {
                Assert.AreEqual(DBNull.Value, decryptedIdNo, $"{description}: DBNull should remain DBNull");
            }
            else if (string.IsNullOrEmpty(idNoValue?.ToString()))
            {
                // Empty strings should be preserved as empty strings, not converted to DBNull
                Assert.AreEqual("", decryptedIdNo, $"{description}: Empty string should remain empty string");
                Assert.AreNotEqual(DBNull.Value, decryptedIdNo, $"{description}: Empty string should not become DBNull");
            }
            else
            {
                Assert.AreEqual(idNoValue, decryptedIdNo, $"{description}: Value should be preserved");
                Assert.AreNotEqual(DBNull.Value, decryptedIdNo, $"{description}: Non-null value should not become DBNull");
            }
        }
    }
} 