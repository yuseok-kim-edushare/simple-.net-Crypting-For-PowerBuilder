using System;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureLibrary.SQL.Services;

namespace SecureLibrary.SQL.Tests
{
    [TestClass]
    public class VarbinaryMaxLengthTest
    {
        [TestMethod]
        public void TestVarbinaryColumnWithoutMaxLengthInSchema()
        {
            // Create a test table with varbinary column
            var table = new DataTable("TestTable");
            table.Columns.Add("ID", typeof(int));
            table.Columns.Add("E_ID_No", typeof(byte[]));
            table.Columns.Add("Name", typeof(string));

            // Set MaxLength for string column only
            table.Columns["Name"].MaxLength = 100;
            // Note: MaxLength should not be set for byte[] columns as it's not applicable

            // Add test data
            var row = table.NewRow();
            row["ID"] = 1;
            row["E_ID_No"] = new byte[] { 1, 2, 3, 4, 5 };
            row["Name"] = "Test Name";
            table.Rows.Add(row);

            // Test the SqlXmlConverter
            var converter = new SqlXmlConverter();

            try
            {
                // This should not throw an exception now
                var xmlString = converter.ToForXmlFormat(table, includeSchema: true);
                
                // Verify that the XML schema doesn't contain maxLength for varbinary
                // The problematic pattern we're checking for is:
                // <xsd:element name="E_ID_No" ...>
                //   <xsd:simpleType>
                //     <xsd:restriction base="sqltypes:varbinary">
                //       <xsd:maxLength value="..." />
                //     </xsd:restriction>
                //   </xsd:simpleType>
                // </xsd:element>
                
                // Check that E_ID_No element doesn't have the problematic maxLength restriction
                var hasProblematicPattern = xmlString.Contains("<xsd:element name=\"E_ID_No\"") && 
                                          xmlString.Contains("<xsd:restriction base=\"sqltypes:varbinary\"") &&
                                          xmlString.Contains("<xsd:maxLength");
                Assert.IsFalse(hasProblematicPattern, "E_ID_No varbinary column should not have maxLength restriction in XML schema");
                
                // Also verify that E_ID_No element has the correct direct type attribute
                var hasCorrectType = xmlString.Contains("<xsd:element name=\"E_ID_No\"") && 
                                   xmlString.Contains("type=\"sqltypes:varbinary\"");
                Assert.IsTrue(hasCorrectType, "E_ID_No varbinary column should have direct type=\"sqltypes:varbinary\" attribute");
                
                Console.WriteLine("Generated XML Schema:");
                Console.WriteLine(xmlString);
                
                // The XML should be valid and parseable
                var parsedTable = converter.ParseForXmlOutput(xmlString);
                Assert.AreEqual(1, parsedTable.Rows.Count);
                Assert.AreEqual(1, parsedTable.Rows[0]["ID"]);
                Assert.AreEqual("Test Name", parsedTable.Rows[0]["Name"]);
                
                Console.WriteLine("Test passed: Varbinary column handled correctly without MaxLength in XML schema");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Test failed with exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void TestStringColumnWithMaxLengthInSchema()
        {
            // Create a test table with string column that should have MaxLength
            var table = new DataTable("TestTable");
            table.Columns.Add("ID", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Description", typeof(string));

            // Set MaxLength for string columns
            table.Columns["Name"].MaxLength = 50;
            table.Columns["Description"].MaxLength = 200;

            // Add test data
            var row = table.NewRow();
            row["ID"] = 1;
            row["Name"] = "Test Name";
            row["Description"] = "Test Description";
            table.Rows.Add(row);

            var converter = new SqlXmlConverter();

            try
            {
                var xmlString = converter.ToForXmlFormat(table, includeSchema: true);
                
                // Verify that the XML schema contains maxLength for string columns
                Assert.IsTrue(xmlString.Contains("Name") && xmlString.Contains("maxLength"));
                Assert.IsTrue(xmlString.Contains("Description") && xmlString.Contains("maxLength"));
                
                Console.WriteLine("Generated XML Schema for String Columns:");
                Console.WriteLine(xmlString);
                
                Console.WriteLine("Test passed: String columns correctly include MaxLength in XML schema");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Test failed with exception: {ex.Message}");
            }
        }
    }
} 