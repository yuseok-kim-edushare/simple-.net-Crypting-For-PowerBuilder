using System;
using System.Data;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureLibrary.SQL.Services;

namespace SecureLibrary.SQL.Tests
{
    [TestClass]
    public class SchemaPreservationTest
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void TestSchemaPreservationForCharAndVarchar()
        {
            // Create a test DataTable with char and varchar columns
            var table = new DataTable("TestTable");
            
            // Add columns with original SQL types stored in ExtendedProperties
            var charColumn = table.Columns.Add("char_col", typeof(string));
            charColumn.ExtendedProperties["OriginalSqlType"] = "char";
            charColumn.ExtendedProperties["OriginalSqlTypeName"] = "CHAR(5)";
            charColumn.MaxLength = 5;
            
            var varcharColumn = table.Columns.Add("varchar_col", typeof(string));
            varcharColumn.ExtendedProperties["OriginalSqlType"] = "varchar";
            varcharColumn.ExtendedProperties["OriginalSqlTypeName"] = "VARCHAR(20)";
            varcharColumn.MaxLength = 20;
            
            var nvarcharColumn = table.Columns.Add("nvarchar_col", typeof(string));
            nvarcharColumn.ExtendedProperties["OriginalSqlType"] = "nvarchar";
            nvarcharColumn.ExtendedProperties["OriginalSqlTypeName"] = "NVARCHAR(15)";
            nvarcharColumn.MaxLength = 15;

            // Create SqlXmlConverter and generate schema
            var converter = new SqlXmlConverter();
            var forXmlOutput = converter.ToForXmlFormat(table, "rows", "Row", true);
            
            TestContext.WriteLine("Generated FOR XML output:");
            TestContext.WriteLine(forXmlOutput);
            
            // Parse the output to extract schema information
            var doc = XDocument.Parse(forXmlOutput);
            var schemaElement = doc.Root?.Element(XName.Get("schema", "http://www.w3.org/2001/XMLSchema"));
            
            Assert.IsNotNull(schemaElement, "Schema element should be present");
            
            // Navigate to the Row element first
            var rowElement = schemaElement.Element(XName.Get("element", "http://www.w3.org/2001/XMLSchema"));
            Assert.IsNotNull(rowElement, "Row element should be present");
            Assert.AreEqual("Row", rowElement.Attribute("name")?.Value, "First element should be Row");
            
            // Navigate to the complexType > sequence to find column definitions
            var complexType = rowElement.Element(XName.Get("complexType", "http://www.w3.org/2001/XMLSchema"));
            Assert.IsNotNull(complexType, "ComplexType should be present");
            
            var sequence = complexType.Element(XName.Get("sequence", "http://www.w3.org/2001/XMLSchema"));
            Assert.IsNotNull(sequence, "Sequence should be present");
            
            // Find the char_col element specifically
            var charElement = sequence.Elements(XName.Get("element", "http://www.w3.org/2001/XMLSchema"))
                .Where(e => e.Attribute("name")?.Value == "char_col")
                .FirstOrDefault();
            Assert.IsNotNull(charElement, "char_col element should be present");
            
            // Look for the restriction element that contains the base type
            var restrictionElement = charElement.Element(XName.Get("simpleType", "http://www.w3.org/2001/XMLSchema"))
                ?.Element(XName.Get("restriction", "http://www.w3.org/2001/XMLSchema"));
            
            Assert.IsNotNull(restrictionElement, "Restriction element should be present for char column");
            
            var baseAttribute = restrictionElement.Attribute("base");
            Assert.IsNotNull(baseAttribute, "Base attribute should be present");
            
            TestContext.WriteLine($"Char column base type: {baseAttribute.Value}");
            
            // The base type should be sqltypes:char, not sqltypes:nvarchar
            Assert.IsTrue(baseAttribute.Value.Contains("sqltypes:char"), 
                $"Char column should be sqltypes:char, but was {baseAttribute.Value}");
            
            TestContext.WriteLine("✓ Char column type preservation verified");
            
            // Test that we can parse this back and the types are preserved
            var parsedTable = converter.ParseForXmlOutput(forXmlOutput);
            
            Assert.IsNotNull(parsedTable, "Parsed table should not be null");
            Assert.AreEqual(3, parsedTable.Columns.Count, "Should have 3 columns");
            
            // Check that the original SQL types are preserved in ExtendedProperties
            var parsedCharColumn = parsedTable.Columns["char_col"];
            Assert.IsTrue(parsedCharColumn.ExtendedProperties.ContainsKey("OriginalSqlType"), 
                "OriginalSqlType should be preserved for char column");
            Assert.AreEqual("char", parsedCharColumn.ExtendedProperties["OriginalSqlType"], 
                "Char column should preserve 'char' as OriginalSqlType");
            
            var parsedVarcharColumn = parsedTable.Columns["varchar_col"];
            Assert.IsTrue(parsedVarcharColumn.ExtendedProperties.ContainsKey("OriginalSqlType"), 
                "OriginalSqlType should be preserved for varchar column");
            Assert.AreEqual("varchar", parsedVarcharColumn.ExtendedProperties["OriginalSqlType"], 
                "Varchar column should preserve 'varchar' as OriginalSqlType");
            
            TestContext.WriteLine("✓ Original SQL types preserved in ExtendedProperties");
        }

        [TestMethod]
        public void TestCharPaddingPreservation()
        {
            // Arrange
            var xmlConverter = new SqlXmlConverter();
            var forXmlOutput = @"<rows xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <xsd:schema xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:sqltypes=""http://schemas.microsoft.com/sqlserver/2004/sqltypes"" targetNamespace=""urn:schemas-microsoft-com:sql:SqlRowSet4"" elementFormDefault=""qualified"">
    <xsd:import namespace=""http://schemas.microsoft.com/sqlserver/2004/sqltypes"" schemaLocation=""http://schemas.microsoft.com/sqlserver/2004/sqltypes/sqltypes.xsd"" />
    <xsd:element name=""Row"">
      <xsd:complexType>
        <xsd:sequence>
          <xsd:element name=""emp_id"" nillable=""1"">
            <xsd:simpleType>
              <xsd:restriction base=""sqltypes:char"" sqltypes:localeId=""1042"" sqltypes:sqlCompareOptions=""IgnoreCase IgnoreKanaType IgnoreWidth"">
                <xsd:maxLength value=""5"" />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name=""emp_nm"" nillable=""1"">
            <xsd:simpleType>
              <xsd:restriction base=""sqltypes:varchar"" sqltypes:localeId=""1042"" sqltypes:sqlCompareOptions=""IgnoreCase IgnoreKanaType IgnoreWidth"">
                <xsd:maxLength value=""20"" />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name=""id_no"" nillable=""1"">
            <xsd:simpleType>
              <xsd:restriction base=""sqltypes:char"" sqltypes:localeId=""1042"" sqltypes:sqlCompareOptions=""IgnoreCase IgnoreKanaType IgnoreWidth"">
                <xsd:maxLength value=""20"" />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
        </xsd:sequence>
      </xsd:complexType>
    </xsd:element>
  </xsd:schema>
  <Row xmlns=""urn:schemas-microsoft-com:sql:SqlRowSet4"">
    <emp_id>E001 </emp_id>
    <emp_nm>John Doe</emp_nm>
    <id_no>12345               </id_no>
  </Row>
</rows>";

            // Act
            var dataTable = xmlConverter.ParseForXmlOutput(forXmlOutput);
            
            // Assert
            Assert.IsNotNull(dataTable, "DataTable should not be null");
            Assert.AreEqual(1, dataTable.Rows.Count, "Should have 1 row");
            
            TestContext.WriteLine("Parsed DataTable with columns:");
            foreach (DataColumn col in dataTable.Columns)
            {
                var originalType = col.ExtendedProperties.ContainsKey("OriginalSqlType") 
                    ? col.ExtendedProperties["OriginalSqlType"].ToString() 
                    : "N/A";
                TestContext.WriteLine($"  - {col.ColumnName}: CLR Type={col.DataType.Name}, Original SQL Type={originalType}, MaxLength={col.MaxLength}");
            }
            
            // Check that original SQL types are preserved
            var empIdColumn = dataTable.Columns["emp_id"];
            var empNmColumn = dataTable.Columns["emp_nm"];
            var idNoColumn = dataTable.Columns["id_no"];
            
            Assert.AreEqual("char", empIdColumn.ExtendedProperties["OriginalSqlType"], "emp_id should be char");
            Assert.AreEqual("varchar", empNmColumn.ExtendedProperties["OriginalSqlType"], "emp_nm should be varchar");
            Assert.AreEqual("char", idNoColumn.ExtendedProperties["OriginalSqlType"], "id_no should be char");
            
            // Check that CHAR values preserve padding
            var row = dataTable.Rows[0];
            var empIdValue = row["emp_id"].ToString();
            var empNmValue = row["emp_nm"].ToString();
            var idNoValue = row["id_no"].ToString();
            
            TestContext.WriteLine($"\nRow values:");
            TestContext.WriteLine($"  emp_id: '{empIdValue}' (length={empIdValue.Length})");
            TestContext.WriteLine($"  emp_nm: '{empNmValue}' (length={empNmValue.Length})");
            TestContext.WriteLine($"  id_no: '{idNoValue}' (length={idNoValue.Length})");
            
            Assert.AreEqual("E001 ", empIdValue, "CHAR(5) should preserve trailing space");
            Assert.AreEqual("John Doe", empNmValue, "VARCHAR doesn't pad");
            Assert.AreEqual("12345               ", idNoValue, "CHAR(20) should preserve all trailing spaces");
            
            // Convert to XML and verify types are preserved
            var xmlDoc = xmlConverter.ToXml(row);
            var empIdElement = xmlDoc.Root.Elements("Column").First(e => e.Attribute("Name")?.Value == "emp_id");
            var empNmElement = xmlDoc.Root.Elements("Column").First(e => e.Attribute("Name")?.Value == "emp_nm");
            var idNoElement = xmlDoc.Root.Elements("Column").First(e => e.Attribute("Name")?.Value == "id_no");
            
            TestContext.WriteLine($"\nXML SqlDbType attributes:");
            TestContext.WriteLine($"  emp_id: SqlDbType={empIdElement.Attribute("SqlDbType")?.Value}, SqlTypeName={empIdElement.Attribute("SqlTypeName")?.Value}");
            TestContext.WriteLine($"  emp_nm: SqlDbType={empNmElement.Attribute("SqlDbType")?.Value}, SqlTypeName={empNmElement.Attribute("SqlTypeName")?.Value}");
            TestContext.WriteLine($"  id_no: SqlDbType={idNoElement.Attribute("SqlDbType")?.Value}, SqlTypeName={idNoElement.Attribute("SqlTypeName")?.Value}");
            
            Assert.AreEqual("Char", empIdElement.Attribute("SqlDbType")?.Value, "emp_id should have SqlDbType=Char");
            Assert.AreEqual("CHAR(5)", empIdElement.Attribute("SqlTypeName")?.Value, "emp_id should have SqlTypeName=CHAR(5)");
            
            Assert.AreEqual("VarChar", empNmElement.Attribute("SqlDbType")?.Value, "emp_nm should have SqlDbType=VarChar");
            Assert.AreEqual("VARCHAR(20)", empNmElement.Attribute("SqlTypeName")?.Value, "emp_nm should have SqlTypeName=VARCHAR(20)");
            
            Assert.AreEqual("Char", idNoElement.Attribute("SqlDbType")?.Value, "id_no should have SqlDbType=Char");
            Assert.AreEqual("CHAR(20)", idNoElement.Attribute("SqlTypeName")?.Value, "id_no should have SqlTypeName=CHAR(20)");
            
            TestContext.WriteLine("\n✓ CHAR padding and SQL type preservation verified successfully!");
        }
    }
} 