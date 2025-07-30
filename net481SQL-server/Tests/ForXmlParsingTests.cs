using System;
using System.Data;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureLibrary.SQL.Services;

namespace SecureLibrary.SQL.Tests
{
    [TestClass]
    public class ForXmlParsingTests
    {
        private SqlXmlConverter _converter;

        [TestInitialize]
        public void Setup()
        {
            _converter = new SqlXmlConverter();
        }

        [TestMethod]
        public void TestParseForXmlOutput_WithSchema()
        {
            // Sample FOR XML output from the provided XML files
            var forXmlOutput = @"<rows xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <xsd:schema xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:sqltypes=""http://schemas.microsoft.com/sqlserver/2004/sqltypes"" targetNamespace=""urn:schemas-microsoft-com:sql:SqlRowSet4"" elementFormDefault=""qualified"">
    <xsd:import namespace=""http://schemas.microsoft.com/sqlserver/2004/sqltypes"" schemaLocation=""http://schemas.microsoft.com/sqlserver/2004/sqltypes/sqltypes.xsd"" />
    <xsd:element name=""Row"">
      <xsd:complexType>
        <xsd:sequence>
          <xsd:element name=""CustomerID"" type=""sqltypes:int"" nillable=""1"" />
          <xsd:element name=""FirstName"" nillable=""1"">
            <xsd:simpleType>
              <xsd:restriction base=""sqltypes:nvarchar"" sqltypes:localeId=""1042"" sqltypes:sqlCompareOptions=""IgnoreCase IgnoreKanaType IgnoreWidth"">
                <xsd:maxLength value=""50"" />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name=""LastName"" nillable=""1"">
            <xsd:simpleType>
              <xsd:restriction base=""sqltypes:nvarchar"" sqltypes:localeId=""1042"" sqltypes:sqlCompareOptions=""IgnoreCase IgnoreKanaType IgnoreWidth"">
                <xsd:maxLength value=""50"" />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name=""Email"" nillable=""1"">
            <xsd:simpleType>
              <xsd:restriction base=""sqltypes:nvarchar"" sqltypes:localeId=""1042"" sqltypes:sqlCompareOptions=""IgnoreCase IgnoreKanaType IgnoreWidth"">
                <xsd:maxLength value=""100"" />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name=""Phone"" nillable=""1"">
            <xsd:simpleType>
              <xsd:restriction base=""sqltypes:nvarchar"" sqltypes:localeId=""1042"" sqltypes:sqlCompareOptions=""IgnoreCase IgnoreKanaType IgnoreWidth"">
                <xsd:maxLength value=""20"" />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name=""SSN"" nillable=""1"">
            <xsd:simpleType>
              <xsd:restriction base=""sqltypes:nvarchar"" sqltypes:localeId=""1042"" sqltypes:sqlCompareOptions=""IgnoreCase IgnoreKanaType IgnoreWidth"">
                <xsd:maxLength value=""11"" />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name=""CreditCard"" nillable=""1"">
            <xsd:simpleType>
              <xsd:restriction base=""sqltypes:nvarchar"" sqltypes:localeId=""1042"" sqltypes:sqlCompareOptions=""IgnoreCase IgnoreKanaType IgnoreWidth"">
                <xsd:maxLength value=""20"" />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name=""Salary"" nillable=""1"">
            <xsd:simpleType>
              <xsd:restriction base=""sqltypes:decimal"">
                <xsd:totalDigits value=""10"" />
                <xsd:fractionDigits value=""2"" />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name=""CreatedDate"" nillable=""1"">
            <xsd:simpleType>
              <xsd:restriction base=""sqltypes:datetime2"">
                <xsd:pattern value=""((000[1-9])|(00[1-9][0-9])|(0[1-9][0-9]{2})|([1-9][0-9]{3}))-((0[1-9])|(1[012]))-((0[1-9])|([12][0-9])|(3[01]))T(([01][0-9])|(2[0-3]))(:[0-5][0-9]){2}(\.[0-9]{7})?"" />
                <xsd:maxInclusive value=""9999-12-31T12:59:59.9999999"" />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
        </xsd:sequence>
      </xsd:complexType>
    </xsd:element>
  </xsd:schema>
  <Row xmlns=""urn:schemas-microsoft-com:sql:SqlRowSet4"">
    <CustomerID>1</CustomerID>
    <FirstName>John</FirstName>
    <LastName>Doe</LastName>
    <Email>john.doe@email.com</Email>
    <Phone>+1-555-0101</Phone>
    <SSN>123-45-6789</SSN>
    <CreditCard>4111-1111-1111-1111</CreditCard>
    <Salary>75000.00</Salary>
    <CreatedDate>2025-07-30T15:48:58.9066667</CreatedDate>
  </Row>
</rows>";

            // Parse the FOR XML output
            var dataTable = _converter.ParseForXmlOutput(forXmlOutput);

            // Verify the schema was parsed correctly
            Assert.IsNotNull(dataTable);
            Assert.AreEqual(9, dataTable.Columns.Count);

            // Verify column types
            Assert.AreEqual(typeof(int), dataTable.Columns["CustomerID"].DataType);
            Assert.AreEqual(typeof(string), dataTable.Columns["FirstName"].DataType);
            Assert.AreEqual(typeof(string), dataTable.Columns["LastName"].DataType);
            Assert.AreEqual(typeof(string), dataTable.Columns["Email"].DataType);
            Assert.AreEqual(typeof(string), dataTable.Columns["Phone"].DataType);
            Assert.AreEqual(typeof(string), dataTable.Columns["SSN"].DataType);
            Assert.AreEqual(typeof(string), dataTable.Columns["CreditCard"].DataType);
            Assert.AreEqual(typeof(decimal), dataTable.Columns["Salary"].DataType);
            Assert.AreEqual(typeof(DateTime), dataTable.Columns["CreatedDate"].DataType);

            // Verify column properties
            Assert.AreEqual(50, dataTable.Columns["FirstName"].MaxLength);
            Assert.AreEqual(50, dataTable.Columns["LastName"].MaxLength);
            Assert.AreEqual(100, dataTable.Columns["Email"].MaxLength);
            Assert.AreEqual(20, dataTable.Columns["Phone"].MaxLength);
            Assert.AreEqual(11, dataTable.Columns["SSN"].MaxLength);
            Assert.AreEqual(20, dataTable.Columns["CreditCard"].MaxLength);

            // Verify data was parsed correctly
            Assert.AreEqual(1, dataTable.Rows.Count);
            var row = dataTable.Rows[0];
            Assert.AreEqual(1, row["CustomerID"]);
            Assert.AreEqual("John", row["FirstName"]);
            Assert.AreEqual("Doe", row["LastName"]);
            Assert.AreEqual("john.doe@email.com", row["Email"]);
            Assert.AreEqual("+1-555-0101", row["Phone"]);
            Assert.AreEqual("123-45-6789", row["SSN"]);
            Assert.AreEqual("4111-1111-1111-1111", row["CreditCard"]);
            Assert.AreEqual(75000.00m, row["Salary"]);
            Assert.AreEqual(DateTime.Parse("2025-07-30T15:48:58.9066667"), row["CreatedDate"]);
        }

        [TestMethod]
        public void TestToForXmlFormat_RoundTrip()
        {
            // Create a test DataTable
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("CustomerID", typeof(int));
            dataTable.Columns.Add("FirstName", typeof(string));
            dataTable.Columns.Add("LastName", typeof(string));
            dataTable.Columns.Add("Email", typeof(string));
            dataTable.Columns.Add("Salary", typeof(decimal));

            // Add test data
            var row = dataTable.NewRow();
            row["CustomerID"] = 1;
            row["FirstName"] = "John";
            row["LastName"] = "Doe";
            row["Email"] = "john.doe@email.com";
            row["Salary"] = 75000.00m;
            dataTable.Rows.Add(row);

            // Convert to FOR XML format
            var forXmlOutput = _converter.ToForXmlFormat(dataTable);

            // Verify the output contains expected elements
            Assert.IsTrue(forXmlOutput.Contains("<rows"));
            Assert.IsTrue(forXmlOutput.Contains("<xsd:schema"));
            Assert.IsTrue(forXmlOutput.Contains("<Row"));
            Assert.IsTrue(forXmlOutput.Contains("<CustomerID>1</CustomerID>"));
            Assert.IsTrue(forXmlOutput.Contains("<FirstName>John</FirstName>"));

            // Parse back to DataTable
            var parsedTable = _converter.ParseForXmlOutput(forXmlOutput);

            // Verify round-trip data integrity
            Assert.AreEqual(1, parsedTable.Rows.Count);
            var parsedRow = parsedTable.Rows[0];
            Assert.AreEqual(1, parsedRow["CustomerID"]);
            Assert.AreEqual("John", parsedRow["FirstName"]);
            Assert.AreEqual("Doe", parsedRow["LastName"]);
            Assert.AreEqual("john.doe@email.com", parsedRow["Email"]);
            Assert.AreEqual(75000.00m, parsedRow["Salary"]);
        }

        [TestMethod]
        public void TestParseForXmlRow_SingleRow()
        {
            // Sample single row FOR XML output
            var forXmlRow = @"<rows xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <xsd:schema xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:sqltypes=""http://schemas.microsoft.com/sqlserver/2004/sqltypes"" targetNamespace=""urn:schemas-microsoft-com:sql:SqlRowSet4"" elementFormDefault=""qualified"">
    <xsd:import namespace=""http://schemas.microsoft.com/sqlserver/2004/sqltypes"" schemaLocation=""http://schemas.microsoft.com/sqlserver/2004/sqltypes/sqltypes.xsd"" />
    <xsd:element name=""Row"">
      <xsd:complexType>
        <xsd:sequence>
          <xsd:element name=""CustomerID"" type=""sqltypes:int"" nillable=""1"" />
          <xsd:element name=""FirstName"" nillable=""1"">
            <xsd:simpleType>
              <xsd:restriction base=""sqltypes:nvarchar"" sqltypes:localeId=""1042"" sqltypes:sqlCompareOptions=""IgnoreCase IgnoreKanaType IgnoreWidth"">
                <xsd:maxLength value=""50"" />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
        </xsd:sequence>
      </xsd:complexType>
    </xsd:element>
  </xsd:schema>
  <Row xmlns=""urn:schemas-microsoft-com:sql:SqlRowSet4"">
    <CustomerID>1</CustomerID>
    <FirstName>John</FirstName>
  </Row>
</rows>";

            // Parse the single row
            var dataRow = _converter.ParseForXmlRow(forXmlRow);

            // Verify the row was parsed correctly
            Assert.IsNotNull(dataRow);
            Assert.AreEqual(1, dataRow["CustomerID"]);
            Assert.AreEqual("John", dataRow["FirstName"]);
        }

        [TestMethod]
        public void TestToForXmlFormat_DataRow()
        {
            // Create a test DataTable and row
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("CustomerID", typeof(int));
            dataTable.Columns.Add("FirstName", typeof(string));

            var row = dataTable.NewRow();
            row["CustomerID"] = 1;
            row["FirstName"] = "John";
            dataTable.Rows.Add(row);

            // Convert DataRow to FOR XML format
            var forXmlOutput = _converter.ToForXmlFormat(row);

            // Verify the output
            Assert.IsTrue(forXmlOutput.Contains("<rows"));
            Assert.IsTrue(forXmlOutput.Contains("<Row"));
            Assert.IsTrue(forXmlOutput.Contains("<CustomerID>1</CustomerID>"));
            Assert.IsTrue(forXmlOutput.Contains("<FirstName>John</FirstName>"));

            // Parse back to DataRow
            var parsedRow = _converter.ParseForXmlRow(forXmlOutput);

            // Verify round-trip data integrity
            Assert.AreEqual(1, parsedRow["CustomerID"]);
            Assert.AreEqual("John", parsedRow["FirstName"]);
        }
    }
} 