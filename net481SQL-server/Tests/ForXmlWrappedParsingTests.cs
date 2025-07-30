using System;
using System.Data;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureLibrary.SQL.Services;

namespace SecureLibrary.SQL.Tests
{
    /// <summary>
    /// Tests for parsing wrapped FOR XML XMLSCHEMA output
    /// </summary>
    [TestClass]
    public class ForXmlWrappedParsingTests
    {
        private SqlXmlConverter _converter;

        [TestInitialize]
        public void Initialize()
        {
            _converter = new SqlXmlConverter();
        }

        [TestMethod]
        public void ParseForXmlOutput_WithWrappedSingleRow_ParsesCorrectly()
        {
            // Arrange
            var wrappedXml = @"<root>
  <RowData>
    <xsd:schema targetNamespace=""urn:schemas-microsoft-com:sql:SqlRowSet4"" xmlns:schema=""urn:schemas-microsoft-com:sql:SqlRowSet4"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:sqltypes=""http://schemas.microsoft.com/sqlserver/2004/sqltypes"" elementFormDefault=""qualified"">
      <xsd:import namespace=""http://schemas.microsoft.com/sqlserver/2004/sqltypes"" schemaLocation=""http://schemas.microsoft.com/sqlserver/2004/sqltypes/sqltypes.xsd"" />
      <xsd:element name=""Row"">
        <xsd:complexType>
          <xsd:sequence>
            <xsd:element name=""cust_id"" type=""sqltypes:int"" />
            <xsd:element name=""cust_name"" nillable=""1"">
              <xsd:simpleType>
                <xsd:restriction base=""sqltypes:nvarchar"" sqltypes:localeId=""1033"" sqltypes:sqlCompareOptions=""IgnoreCase IgnoreKanaType IgnoreWidth"">
                  <xsd:maxLength value=""100"" />
                </xsd:restriction>
              </xsd:simpleType>
            </xsd:element>
            <xsd:element name=""is_active"" type=""sqltypes:bit"" nillable=""1"" />
          </xsd:sequence>
        </xsd:complexType>
      </xsd:element>
    </xsd:schema>
    <Row xmlns=""urn:schemas-microsoft-com:sql:SqlRowSet4"">
      <cust_id>16424</cust_id>
      <cust_name>John Doe</cust_name>
      <is_active>1</is_active>
    </Row>
  </RowData>
</root>";

            // Act
            var result = _converter.ParseForXmlOutput(wrappedXml);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Rows.Count);
            Assert.AreEqual(3, result.Columns.Count);
            
            // Check column types
            Assert.AreEqual(typeof(int), result.Columns["cust_id"].DataType);
            Assert.AreEqual(typeof(string), result.Columns["cust_name"].DataType);
            Assert.AreEqual(typeof(bool), result.Columns["is_active"].DataType);
            
            // Check values
            var row = result.Rows[0];
            Assert.AreEqual(16424, row["cust_id"]);
            Assert.AreEqual("John Doe", row["cust_name"]);
            Assert.AreEqual(true, row["is_active"]); // Should parse "1" as true
        }

        [TestMethod]
        public void ParseForXmlOutput_WithWrappedMultipleRows_ParsesCorrectly()
        {
            // Arrange
            var wrappedXml = @"<root>
  <RowsData>
    <Rows xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
      <xsd:schema targetNamespace=""urn:schemas-microsoft-com:sql:SqlRowSet4"" xmlns:schema=""urn:schemas-microsoft-com:sql:SqlRowSet4"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:sqltypes=""http://schemas.microsoft.com/sqlserver/2004/sqltypes"" elementFormDefault=""qualified"">
        <xsd:import namespace=""http://schemas.microsoft.com/sqlserver/2004/sqltypes"" schemaLocation=""http://schemas.microsoft.com/sqlserver/2004/sqltypes/sqltypes.xsd"" />
        <xsd:element name=""Row"">
          <xsd:complexType>
            <xsd:sequence>
              <xsd:element name=""id"" type=""sqltypes:int"" />
              <xsd:element name=""value"" type=""sqltypes:decimal"" nillable=""1"" />
              <xsd:element name=""is_valid"" type=""sqltypes:bit"" />
            </xsd:sequence>
          </xsd:complexType>
        </xsd:element>
      </xsd:schema>
      <Row xmlns=""urn:schemas-microsoft-com:sql:SqlRowSet4"">
        <id>1</id>
        <value>123.45</value>
        <is_valid>1</is_valid>
      </Row>
      <Row xmlns=""urn:schemas-microsoft-com:sql:SqlRowSet4"">
        <id>2</id>
        <value xsi:nil=""true"" />
        <is_valid>0</is_valid>
      </Row>
    </Rows>
  </RowsData>
</root>";

            // Act
            var result = _converter.ParseForXmlOutput(wrappedXml);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Rows.Count);
            Assert.AreEqual(3, result.Columns.Count);
            
            // Check first row
            Assert.AreEqual(1, result.Rows[0]["id"]);
            Assert.AreEqual(123.45m, result.Rows[0]["value"]);
            Assert.AreEqual(true, result.Rows[0]["is_valid"]);
            
            // Check second row with NULL value
            Assert.AreEqual(2, result.Rows[1]["id"]);
            Assert.AreEqual(DBNull.Value, result.Rows[1]["value"]);
            Assert.AreEqual(false, result.Rows[1]["is_valid"]);
        }

        [TestMethod]
        public void ParseForXmlRow_WithWrappedRow_ParsesCorrectly()
        {
            // Arrange
            var wrappedXml = @"<root>
  <RowData>
    <xsd:schema targetNamespace=""urn:schemas-microsoft-com:sql:SqlRowSet4"" xmlns:schema=""urn:schemas-microsoft-com:sql:SqlRowSet4"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:sqltypes=""http://schemas.microsoft.com/sqlserver/2004/sqltypes"" elementFormDefault=""qualified"">
      <xsd:import namespace=""http://schemas.microsoft.com/sqlserver/2004/sqltypes"" schemaLocation=""http://schemas.microsoft.com/sqlserver/2004/sqltypes/sqltypes.xsd"" />
      <xsd:element name=""Row"">
        <xsd:complexType>
          <xsd:sequence>
            <xsd:element name=""test_bit"" type=""sqltypes:bit"" />
          </xsd:sequence>
        </xsd:complexType>
      </xsd:element>
    </xsd:schema>
    <Row xmlns=""urn:schemas-microsoft-com:sql:SqlRowSet4"">
      <test_bit>0</test_bit>
    </Row>
  </RowData>
</root>";

            // Act
            var result = _converter.ParseForXmlRow(wrappedXml);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Table.Columns.Count);
            Assert.AreEqual(typeof(bool), result.Table.Columns["test_bit"].DataType);
            Assert.AreEqual(false, result["test_bit"]); // Should parse "0" as false
        }

        [TestMethod]
        public void ParseForXmlOutput_BooleanValueHandling_ParsesCorrectly()
        {
            // Arrange - Test various boolean representations
            var xmlTemplate = @"<root>
  <RowData>
    <Row xmlns=""urn:schemas-microsoft-com:sql:SqlRowSet4"">
      <test_value>{0}</test_value>
    </Row>
  </RowData>
</root>";

            // Test "1" -> true
            var xml1 = string.Format(xmlTemplate, "1");
            var result1 = _converter.ParseForXmlOutput(xml1);
            Assert.AreEqual(true, bool.Parse(result1.Rows[0]["test_value"].ToString()));

            // Test "0" -> false
            var xml0 = string.Format(xmlTemplate, "0");
            var result0 = _converter.ParseForXmlOutput(xml0);
            Assert.AreEqual(false, bool.Parse(result0.Rows[0]["test_value"].ToString()));
        }

        [TestMethod]
        public void ParseForXmlOutput_WithoutWrapper_StillWorks()
        {
            // Arrange - Test backward compatibility without wrapper
            var unwrappedXml = @"<Row xmlns=""urn:schemas-microsoft-com:sql:SqlRowSet4"">
  <id>123</id>
  <name>Test</name>
</Row>";

            // Act
            var result = _converter.ParseForXmlOutput(unwrappedXml);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Rows.Count);
            Assert.AreEqual("123", result.Rows[0]["id"].ToString());
            Assert.AreEqual("Test", result.Rows[0]["name"].ToString());
        }
    }
}