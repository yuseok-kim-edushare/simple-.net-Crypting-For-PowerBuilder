using System;
using System.Data;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureLibrary.SQL.Services;

namespace SecureLibrary.SQL.Tests
{
    [TestClass]
    public class SpaceHandlingTest
    {
        private SqlXmlConverter _converter;

        [TestInitialize]
        public void Setup()
        {
            _converter = new SqlXmlConverter();
        }

        [TestMethod]
        public void TestSpaceOnlyStringPreservation()
        {
            // Create a DataTable with a string column
            var table = new DataTable();
            table.Columns.Add("TestColumn", typeof(string));

            // Add a row with a space-only string
            var row = table.NewRow();
            row["TestColumn"] = "   "; // Three spaces
            table.Rows.Add(row);

            // Convert to XML and back
            var xml = _converter.ToXml(table);
            var reconstructedTable = _converter.FromXml(xml);

            // Verify the space-only string is preserved
            Assert.AreEqual("   ", reconstructedTable.Rows[0]["TestColumn"]);
        }

        [TestMethod]
        public void TestTrailingSpacePreservation()
        {
            // Create a DataTable with a string column
            var table = new DataTable();
            table.Columns.Add("TestColumn", typeof(string));

            // Add a row with trailing spaces
            var row = table.NewRow();
            row["TestColumn"] = "Hello   "; // "Hello" with three trailing spaces
            table.Rows.Add(row);

            // Convert to XML and back
            var xml = _converter.ToXml(table);
            var reconstructedTable = _converter.FromXml(xml);

            // Verify the trailing spaces are preserved
            Assert.AreEqual("Hello   ", reconstructedTable.Rows[0]["TestColumn"]);
        }

        [TestMethod]
        public void TestEmptyStringHandling()
        {
            // Create a DataTable with a string column
            var table = new DataTable();
            table.Columns.Add("TestColumn", typeof(string));

            // Add a row with an empty string
            var row = table.NewRow();
            row["TestColumn"] = ""; // Empty string
            table.Rows.Add(row);

            // Convert to XML and back
            var xml = _converter.ToXml(table);
            var reconstructedTable = _converter.FromXml(xml);

            // Verify empty string is preserved (not converted to DBNull)
            Assert.AreEqual("", reconstructedTable.Rows[0]["TestColumn"]);
        }

        [TestMethod]
        public void TestNullHandling()
        {
            // Create a DataTable with a nullable string column
            var table = new DataTable();
            var column = table.Columns.Add("TestColumn", typeof(string));
            column.AllowDBNull = true;

            // Add a row with DBNull.Value
            var row = table.NewRow();
            row["TestColumn"] = DBNull.Value;
            table.Rows.Add(row);

            // Convert to XML and back
            var xml = _converter.ToXml(table);
            var reconstructedTable = _converter.FromXml(xml);

            // Verify null is preserved
            Assert.AreEqual(DBNull.Value, reconstructedTable.Rows[0]["TestColumn"]);
        }

        [TestMethod]
        public void TestMixedSpaceHandling()
        {
            // Create a DataTable with a string column
            var table = new DataTable();
            table.Columns.Add("TestColumn", typeof(string));

            // Add multiple rows with different space scenarios
            var row1 = table.NewRow();
            row1["TestColumn"] = "   "; // Space only
            table.Rows.Add(row1);

            var row2 = table.NewRow();
            row2["TestColumn"] = "Hello   "; // With trailing spaces
            table.Rows.Add(row2);

            var row3 = table.NewRow();
            row3["TestColumn"] = "   World"; // With leading spaces
            table.Rows.Add(row3);

            var row4 = table.NewRow();
            row4["TestColumn"] = ""; // Empty string
            table.Rows.Add(row4);

            // Convert to XML and back
            var xml = _converter.ToXml(table);
            var reconstructedTable = _converter.FromXml(xml);

            // Verify all space scenarios are preserved
            Assert.AreEqual("   ", reconstructedTable.Rows[0]["TestColumn"]);
            Assert.AreEqual("Hello   ", reconstructedTable.Rows[1]["TestColumn"]);
            Assert.AreEqual("   World", reconstructedTable.Rows[2]["TestColumn"]);
            Assert.AreEqual("", reconstructedTable.Rows[3]["TestColumn"]);
        }
    }
} 