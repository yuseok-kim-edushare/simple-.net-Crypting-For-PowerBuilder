using System;
using System.Data;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.SqlServer.Server;
using SecureLibrary.SQL.Interfaces;

namespace SecureLibrary.SQL.Services
{
    /// <summary>
    /// Implementation of SQL type to XML conversion operations
    /// Provides robust handling of all SQL CLR types and nulls with round-trip capability
    /// Includes specialized methods for FOR XML output handling
    /// </summary>
    public class SqlXmlConverter : ISqlXmlConverter
    {
        private static readonly SqlDbType[] _supportedSqlTypes = {
            SqlDbType.BigInt, SqlDbType.Binary, SqlDbType.Bit, SqlDbType.Char,
            SqlDbType.Date, SqlDbType.DateTime, SqlDbType.DateTime2, SqlDbType.DateTimeOffset,
            SqlDbType.Decimal, SqlDbType.Float, SqlDbType.Image, SqlDbType.Int,
            SqlDbType.Money, SqlDbType.NChar, SqlDbType.NText, SqlDbType.NVarChar,
            SqlDbType.Real, SqlDbType.SmallDateTime, SqlDbType.SmallInt, SqlDbType.SmallMoney,
            SqlDbType.Text, SqlDbType.Time, SqlDbType.TinyInt, SqlDbType.UniqueIdentifier,
            SqlDbType.VarBinary, SqlDbType.VarChar, SqlDbType.Xml
        };

        /// <summary>
        /// Converts a SqlDataRecord to XML representation
        /// </summary>
        /// <param name="record">SqlDataRecord to convert</param>
        /// <returns>XDocument containing the record data</returns>
        /// <exception cref="ArgumentNullException">Thrown when record is null</exception>
        public XDocument ToXml(SqlDataRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            var doc = new XDocument();
            var root = new XElement("Record");
            doc.Add(root);

            for (int i = 0; i < record.FieldCount; i++)
            {
                var columnName = record.GetName(i);
                var value = record.GetValue(i);
                var sqlType = record.GetSqlMetaData(i).SqlDbType;

                var element = new XElement("Column",
                    new XAttribute("Name", columnName),
                    new XAttribute("Type", sqlType.ToString()),
                    new XAttribute("Ordinal", i)
                );

                if (value == DBNull.Value || value == null)
                {
                    element.Add(new XAttribute("IsNull", "true"));
                }
                else
                {
                    element.Add(new XAttribute("IsNull", "false"));
                    element.Value = ConvertValueToString(value, sqlType);
                }

                root.Add(element);
            }

            return doc;
        }

        /// <summary>
        /// Converts XML back to a SqlDataRecord with proper type casting
        /// </summary>
        /// <param name="xml">XDocument containing the record data</param>
        /// <param name="metadata">SqlMetaData array defining the expected schema</param>
        /// <returns>SqlDataRecord with properly typed values</returns>
        /// <exception cref="ArgumentNullException">Thrown when xml or metadata is null</exception>
        public SqlDataRecord FromXml(XDocument xml, SqlMetaData[] metadata)
        {
            if (xml == null)
                throw new ArgumentNullException(nameof(xml));
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            var record = new SqlDataRecord(metadata);
            var columns = xml.Root?.Elements("Column").ToList();

            if (columns == null || columns.Count == 0)
                throw new ArgumentException("No Column elements found in XML", nameof(xml));

            for (int i = 0; i < Math.Min(columns.Count, metadata.Length); i++)
            {
                var column = columns[i];
                var isNull = column.Attribute("IsNull")?.Value == "true";
                var value = column.Value;

                if (isNull || string.IsNullOrEmpty(value))
                {
                    record.SetDBNull(i);
                }
                else
                {
                    var convertedValue = ConvertStringToValue(value, metadata[i].SqlDbType);
                    SetRecordValue(record, i, convertedValue, metadata[i].SqlDbType);
                }
            }

            return record;
        }

        /// <summary>
        /// Converts a DataRow to XML representation
        /// </summary>
        /// <param name="row">DataRow to convert</param>
        /// <returns>XDocument containing the row data</returns>
        /// <exception cref="ArgumentNullException">Thrown when row is null</exception>
        public XDocument ToXml(DataRow row)
        {
            if (row == null)
                throw new ArgumentNullException(nameof(row));

            var doc = new XDocument();
            var root = new XElement("Row");
            doc.Add(root);

            foreach (DataColumn column in row.Table.Columns)
            {
                var value = row[column];
                var element = new XElement("Column",
                    new XAttribute("Name", column.ColumnName),
                    new XAttribute("Type", column.DataType.Name),
                    new XAttribute("MaxLength", column.MaxLength)
                );

                if (value == DBNull.Value || value == null)
                {
                    element.Add(new XAttribute("IsNull", "true"));
                }
                else
                {
                    element.Add(new XAttribute("IsNull", "false"));
                    element.Value = ConvertValueToString(value, column.DataType);
                }

                root.Add(element);
            }

            return doc;
        }

        /// <summary>
        /// Converts XML back to a DataRow with proper type casting
        /// </summary>
        /// <param name="xml">XDocument containing the row data</param>
        /// <param name="table">DataTable defining the expected schema</param>
        /// <returns>DataRow with properly typed values</returns>
        /// <exception cref="ArgumentNullException">Thrown when xml or table is null</exception>
        public DataRow FromXml(XDocument xml, DataTable table)
        {
            if (xml == null)
                throw new ArgumentNullException(nameof(xml));
            if (table == null)
                throw new ArgumentNullException(nameof(table));

            var row = table.NewRow();
            var columns = xml.Root?.Elements("Column").ToList();

            if (columns == null || columns.Count == 0)
                throw new ArgumentException("No Column elements found in XML", nameof(xml));

            foreach (var column in columns)
            {
                var columnName = column.Attribute("Name")?.Value;
                if (string.IsNullOrEmpty(columnName) || !table.Columns.Contains(columnName))
                    continue;

                var isNull = column.Attribute("IsNull")?.Value == "true";
                var value = column.Value;

                if (isNull || string.IsNullOrEmpty(value))
                {
                    row[columnName] = DBNull.Value;
                }
                else
                {
                    var dataColumn = table.Columns[columnName];
                    var convertedValue = ConvertStringToValue(value, dataColumn.DataType);
                    row[columnName] = convertedValue;
                }
            }

            return row;
        }

        /// <summary>
        /// Converts a DataTable to XML representation with schema metadata
        /// </summary>
        /// <param name="table">DataTable to convert</param>
        /// <returns>XDocument containing the table data and schema</returns>
        /// <exception cref="ArgumentNullException">Thrown when table is null</exception>
        public XDocument ToXml(DataTable table)
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table));

            var doc = new XDocument();
            var root = new XElement("Table");
            doc.Add(root);

            // Add schema metadata
            var schemaElement = new XElement("Schema");
            foreach (DataColumn column in table.Columns)
            {
                var columnElement = new XElement("Column",
                    new XAttribute("Name", column.ColumnName),
                    new XAttribute("Type", column.DataType.Name),
                    new XAttribute("MaxLength", column.MaxLength),
                    new XAttribute("AllowNull", column.AllowDBNull),
                    new XAttribute("AutoIncrement", column.AutoIncrement),
                    new XAttribute("ReadOnly", column.ReadOnly)
                );
                schemaElement.Add(columnElement);
            }
            root.Add(schemaElement);

            // Add data rows
            var dataElement = new XElement("Data");
            foreach (DataRow row in table.Rows)
            {
                var rowElement = new XElement("Row");
                foreach (DataColumn column in table.Columns)
                {
                    var value = row[column];
                    var columnElement = new XElement("Column",
                        new XAttribute("Name", column.ColumnName)
                    );

                    if (value == DBNull.Value || value == null)
                    {
                        columnElement.Add(new XAttribute("IsNull", "true"));
                    }
                    else
                    {
                        columnElement.Add(new XAttribute("IsNull", "false"));
                        columnElement.Value = ConvertValueToString(value, column.DataType);
                    }

                    rowElement.Add(columnElement);
                }
                dataElement.Add(rowElement);
            }
            root.Add(dataElement);

            return doc;
        }

        /// <summary>
        /// Converts XML back to a DataTable with schema restoration
        /// </summary>
        /// <param name="xml">XDocument containing the table data and schema</param>
        /// <returns>DataTable with restored schema and data</returns>
        /// <exception cref="ArgumentNullException">Thrown when xml is null</exception>
        public DataTable FromXml(XDocument xml)
        {
            if (xml == null)
                throw new ArgumentNullException(nameof(xml));

            var table = new DataTable();
            var schemaElement = xml.Root?.Element("Schema");
            var dataElement = xml.Root?.Element("Data");

            if (schemaElement == null)
                throw new ArgumentException("Schema element not found in XML", nameof(xml));

            // Restore schema
            foreach (var columnElement in schemaElement.Elements("Column"))
            {
                var columnName = columnElement.Attribute("Name")?.Value;
                var typeName = columnElement.Attribute("Type")?.Value;
                var maxLength = columnElement.Attribute("MaxLength")?.Value;
                var allowNull = columnElement.Attribute("AllowNull")?.Value == "true";
                var autoIncrement = columnElement.Attribute("AutoIncrement")?.Value == "true";
                var readOnly = columnElement.Attribute("ReadOnly")?.Value == "true";

                if (string.IsNullOrEmpty(columnName) || string.IsNullOrEmpty(typeName))
                    continue;

                var dataType = Type.GetType(typeName) ?? typeof(string);
                var column = new DataColumn(columnName, dataType);

                if (!string.IsNullOrEmpty(maxLength) && int.TryParse(maxLength, out int maxLen))
                    column.MaxLength = maxLen;

                column.AllowDBNull = allowNull;
                column.AutoIncrement = autoIncrement;
                column.ReadOnly = readOnly;

                table.Columns.Add(column);
            }

            // Restore data
            if (dataElement != null)
            {
                foreach (var rowElement in dataElement.Elements("Row"))
                {
                    var row = table.NewRow();
                    foreach (var columnElement in rowElement.Elements("Column"))
                    {
                        var columnName = columnElement.Attribute("Name")?.Value;
                        if (string.IsNullOrEmpty(columnName) || !table.Columns.Contains(columnName))
                            continue;

                        var isNull = columnElement.Attribute("IsNull")?.Value == "true";
                        var value = columnElement.Value;

                        if (isNull || string.IsNullOrEmpty(value))
                        {
                            row[columnName] = DBNull.Value;
                        }
                        else
                        {
                            var dataColumn = table.Columns[columnName];
                            var convertedValue = ConvertStringToValue(value, dataColumn.DataType);
                            row[columnName] = convertedValue;
                        }
                    }
                    table.Rows.Add(row);
                }
            }

            return table;
        }

        /// <summary>
        /// Validates that a value can be safely converted to the specified SQL type
        /// </summary>
        /// <param name="value">Value to validate</param>
        /// <param name="sqlType">Target SQL type</param>
        /// <returns>True if the value can be converted, false otherwise</returns>
        public bool CanConvertToSqlType(object value, SqlDbType sqlType)
        {
            if (value == null || value == DBNull.Value)
                return true;

            try
            {
                ConvertStringToValue(value.ToString(), sqlType);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the default value for a SQL type
        /// </summary>
        /// <param name="sqlType">SQL type to get default value for</param>
        /// <returns>Default value for the SQL type</returns>
        public object GetDefaultValue(SqlDbType sqlType)
        {
            switch (sqlType)
            {
                case SqlDbType.Bit:
                    return false;
                case SqlDbType.TinyInt:
                case SqlDbType.SmallInt:
                case SqlDbType.Int:
                case SqlDbType.BigInt:
                    return 0;
                case SqlDbType.Decimal:
                case SqlDbType.Money:
                case SqlDbType.SmallMoney:
                case SqlDbType.Float:
                case SqlDbType.Real:
                    return 0.0m;
                case SqlDbType.Date:
                case SqlDbType.DateTime:
                case SqlDbType.DateTime2:
                case SqlDbType.SmallDateTime:
                    return DateTime.MinValue;
                case SqlDbType.Time:
                    return TimeSpan.Zero;
                case SqlDbType.DateTimeOffset:
                    return DateTimeOffset.MinValue;
                case SqlDbType.UniqueIdentifier:
                    return Guid.Empty;
                case SqlDbType.Binary:
                case SqlDbType.VarBinary:
                case SqlDbType.Image:
                    return new byte[0];
                case SqlDbType.Char:
                case SqlDbType.VarChar:
                case SqlDbType.Text:
                case SqlDbType.NChar:
                case SqlDbType.NVarChar:
                case SqlDbType.NText:
                case SqlDbType.Xml:
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Converts a .NET value to the appropriate SQL type
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <param name="sqlType">Target SQL type</param>
        /// <returns>Converted value</returns>
        /// <exception cref="InvalidCastException">Thrown when conversion is not possible</exception>
        public object ConvertToSqlType(object value, SqlDbType sqlType)
        {
            if (value == null || value == DBNull.Value)
                return DBNull.Value;

            try
            {
                switch (sqlType)
                {
                    case SqlDbType.Bit:
                        return Convert.ToBoolean(value);
                    case SqlDbType.TinyInt:
                        return Convert.ToByte(value);
                    case SqlDbType.SmallInt:
                        return Convert.ToInt16(value);
                    case SqlDbType.Int:
                        return Convert.ToInt32(value);
                    case SqlDbType.BigInt:
                        return Convert.ToInt64(value);
                    case SqlDbType.Decimal:
                        return Convert.ToDecimal(value);
                    case SqlDbType.Money:
                    case SqlDbType.SmallMoney:
                        return Convert.ToDecimal(value);
                    case SqlDbType.Float:
                        return Convert.ToDouble(value);
                    case SqlDbType.Real:
                        return Convert.ToSingle(value);
                    case SqlDbType.Date:
                    case SqlDbType.DateTime:
                    case SqlDbType.DateTime2:
                    case SqlDbType.SmallDateTime:
                        return Convert.ToDateTime(value);
                    case SqlDbType.Time:
                        return TimeSpan.Parse(value.ToString());
                    case SqlDbType.DateTimeOffset:
                        return DateTimeOffset.Parse(value.ToString());
                    case SqlDbType.UniqueIdentifier:
                        return Guid.Parse(value.ToString());
                    case SqlDbType.Binary:
                    case SqlDbType.VarBinary:
                    case SqlDbType.Image:
                        if (value is byte[] bytes)
                            return bytes;
                        return Convert.FromBase64String(value.ToString());
                    case SqlDbType.Char:
                    case SqlDbType.VarChar:
                    case SqlDbType.Text:
                    case SqlDbType.NChar:
                    case SqlDbType.NVarChar:
                    case SqlDbType.NText:
                    case SqlDbType.Xml:
                    default:
                        return value.ToString();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidCastException($"Cannot convert value '{value}' to SqlDbType.{sqlType}", ex);
            }
        }

        /// <summary>
        /// Converts a SQL type value to .NET type
        /// </summary>
        /// <param name="value">SQL type value to convert</param>
        /// <param name="targetType">Target .NET type</param>
        /// <returns>Converted value</returns>
        /// <exception cref="InvalidCastException">Thrown when conversion is not possible</exception>
        public object ConvertFromSqlType(object value, Type targetType)
        {
            if (value == null || value == DBNull.Value)
                return null;

            try
            {
                return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                throw new InvalidCastException($"Cannot convert value '{value}' to type {targetType.Name}", ex);
            }
        }

        /// <summary>
        /// Gets the list of supported SQL types
        /// </summary>
        /// <returns>Array of supported SqlDbType values</returns>
        public SqlDbType[] GetSupportedSqlTypes()
        {
            return (SqlDbType[])_supportedSqlTypes.Clone();
        }

        /// <summary>
        /// Validates XML structure for SQL type conversion
        /// </summary>
        /// <param name="xml">XML to validate</param>
        /// <returns>Validation result with any error messages</returns>
        public ValidationResult ValidateXmlStructure(XDocument xml)
        {
            var result = new ValidationResult { IsValid = true };

            if (xml?.Root == null)
            {
                result.IsValid = false;
                result.Errors.Add("XML document is null or has no root element");
                return result;
            }

            // Check for required elements based on root name
            switch (xml.Root.Name.LocalName)
            {
                case "Record":
                    ValidateRecordStructure(xml.Root, result);
                    break;
                case "Row":
                    ValidateRowStructure(xml.Root, result);
                    break;
                case "Table":
                    ValidateTableStructure(xml.Root, result);
                    break;
                default:
                    result.Warnings.Add($"Unknown root element: {xml.Root.Name.LocalName}");
                    break;
            }

            return result;
        }

        #region FOR XML Specific Methods

        /// <summary>
        /// Parses FOR XML output (RAW, ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE) into DataTable
        /// </summary>
        /// <param name="forXmlOutput">XML string from FOR XML query</param>
        /// <returns>DataTable with parsed data and schema</returns>
        /// <exception cref="ArgumentNullException">Thrown when forXmlOutput is null</exception>
        public DataTable ParseForXmlOutput(string forXmlOutput)
        {
            if (string.IsNullOrEmpty(forXmlOutput))
                throw new ArgumentNullException(nameof(forXmlOutput));

            var xmlDoc = XDocument.Parse(forXmlOutput);
            var root = xmlDoc.Root;

            if (root == null)
                throw new ArgumentException("Invalid XML structure: no root element");

            var dataTable = new DataTable();
            XElement workingRoot = root;

            // Check if this is wrapped XML (root > RowData/RowsData > actual content)
            if (root.Name.LocalName == "root" || root.Name.LocalName == "Rows")
            {
                var dataElement = root.Element("RowData") ?? root.Element("RowsData") ?? root.Element("Data");
                if (dataElement != null && dataElement.HasElements)
                {
                    // Use the inner content
                    workingRoot = dataElement;
                    
                    // Check for nested structure like RowsData > Rows > Row
                    var nestedRowsElement = dataElement.Element("Rows");
                    if (nestedRowsElement != null && nestedRowsElement.HasElements)
                    {
                        workingRoot = nestedRowsElement;
                    }
                }
            }
            // Check if the root element itself is a Row (without wrapper)
            else if (root.Name.LocalName == "Row")
            {
                // Root is already a Row element, use it directly
                workingRoot = root;
            }

            // Parse XML schema if present
            var schemaElement = workingRoot.Element(XName.Get("schema", "http://www.w3.org/2001/XMLSchema"));
            if (schemaElement != null)
            {
                ParseSqlServerXmlSchema(schemaElement, dataTable);
            }

            // If no schema was parsed, create default columns from the first row
            if (dataTable.Columns.Count == 0)
            {
                var firstRow = workingRoot.Elements().FirstOrDefault(e => e.Name.LocalName == "Row");
                if (firstRow != null)
                {
                    foreach (var element in firstRow.Elements())
                    {
                        var columnName = element.Name.LocalName;
                        if (!dataTable.Columns.Contains(columnName))
                        {
                            dataTable.Columns.Add(columnName, typeof(string));
                        }
                    }
                }
                // If workingRoot itself is a Row, use it to create columns
                else if (workingRoot.Name.LocalName == "Row")
                {
                    foreach (var element in workingRoot.Elements())
                    {
                        var columnName = element.Name.LocalName;
                        if (!dataTable.Columns.Contains(columnName))
                        {
                            dataTable.Columns.Add(columnName, typeof(string));
                        }
                    }
                }
            }

            // Parse row data - look for Row elements at any level within workingRoot
            var rowElements = workingRoot.Descendants().Where(e => e.Name.LocalName == "Row").ToList();
            // If no Row elements found and workingRoot itself is a Row, use it
            if (rowElements.Count == 0 && workingRoot.Name.LocalName == "Row")
            {
                rowElements.Add(workingRoot);
            }
            
            foreach (var rowElement in rowElements)
            {
                var dataRow = dataTable.NewRow();
                ParseForXmlRow(rowElement, dataRow, dataTable);
                dataTable.Rows.Add(dataRow);
            }

            return dataTable;
        }

        /// <summary>
        /// Parses a single row from FOR XML output
        /// </summary>
        /// <param name="forXmlRow">Single row XML from FOR XML query</param>
        /// <returns>DataRow with parsed data</returns>
        /// <exception cref="ArgumentNullException">Thrown when forXmlRow is null</exception>
        public DataRow ParseForXmlRow(string forXmlRow)
        {
            if (string.IsNullOrEmpty(forXmlRow))
                throw new ArgumentNullException(nameof(forXmlRow));

            var xmlDoc = XDocument.Parse(forXmlRow);
            var root = xmlDoc.Root;

            if (root == null)
                throw new ArgumentException("Invalid XML structure: no root element");

            var dataTable = new DataTable();
            XElement workingRoot = root;

            // Check if this is wrapped XML (root > RowData > actual content)
            if (root.Name.LocalName == "root")
            {
                var dataElement = root.Element("RowData") ?? root.Element("Data");
                if (dataElement != null && dataElement.HasElements)
                {
                    // Use the inner content
                    workingRoot = dataElement;
                    
                    // Check for nested structure like RowData > Rows > Row
                    var nestedRowsElement = dataElement.Element("Rows");
                    if (nestedRowsElement != null && nestedRowsElement.HasElements)
                    {
                        workingRoot = nestedRowsElement;
                    }
                }
            }

            // Parse XML schema if present
            var schemaElement = workingRoot.Element(XName.Get("schema", "http://www.w3.org/2001/XMLSchema"));
            if (schemaElement != null)
            {
                ParseSqlServerXmlSchema(schemaElement, dataTable);
            }

            // Find the actual row element - look for Row elements at any level within workingRoot
            var rowElement = workingRoot.Descendants().FirstOrDefault(e => e.Name.LocalName == "Row");
            if (rowElement == null)
            {
                throw new ArgumentException("No Row element found in XML");
            }

            // If no schema was parsed, create columns from the row
            if (dataTable.Columns.Count == 0)
            {
                foreach (var element in rowElement.Elements())
                {
                    var columnName = element.Name.LocalName;
                    if (!dataTable.Columns.Contains(columnName))
                    {
                        dataTable.Columns.Add(columnName, typeof(string));
                    }
                }
            }

            // Now create and parse the row
            var dataRow = dataTable.NewRow();
            ParseForXmlRow(rowElement, dataRow, dataTable);
            dataTable.Rows.Add(dataRow);

            return dataRow;
        }

        /// <summary>
        /// Converts DataTable back to FOR XML compatible format
        /// </summary>
        /// <param name="table">DataTable to convert</param>
        /// <param name="rootName">Root element name (default: "rows")</param>
        /// <param name="rowName">Row element name (default: "Row")</param>
        /// <param name="includeSchema">Whether to include XML schema</param>
        /// <returns>XML string in FOR XML format</returns>
        /// <exception cref="ArgumentNullException">Thrown when table is null</exception>
        public string ToForXmlFormat(DataTable table, string rootName = "rows", string rowName = "Row", bool includeSchema = true)
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table));

            var xsiNamespace = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");
            var rowNamespace = XNamespace.Get("urn:schemas-microsoft-com:sql:SqlRowSet4");
            
            var root = new XElement(rootName);
            
            // Add XML namespace for schema instance
            root.Add(new XAttribute(XNamespace.Xmlns + "xsi", xsiNamespace));

            if (includeSchema)
            {
                var schemaElement = CreateSqlServerXmlSchema(table);
                root.Add(schemaElement);
            }

            // Add row data
            foreach (DataRow row in table.Rows)
            {
                var rowElement = new XElement(rowNamespace + rowName);

                foreach (DataColumn column in table.Columns)
                {
                    var value = row[column];
                    var columnElement = new XElement(rowNamespace + column.ColumnName);

                    if (value == DBNull.Value || value == null)
                    {
                        columnElement.Add(new XAttribute(xsiNamespace + "nil", "true"));
                    }
                    else
                    {
                        columnElement.Value = ConvertValueToString(value, column.DataType);
                    }

                    rowElement.Add(columnElement);
                }

                root.Add(rowElement);
            }

            return root.ToString();
        }

        /// <summary>
        /// Converts DataRow back to FOR XML compatible format
        /// </summary>
        /// <param name="row">DataRow to convert</param>
        /// <param name="rowName">Row element name (default: "Row")</param>
        /// <param name="includeSchema">Whether to include XML schema</param>
        /// <returns>XML string in FOR XML format</returns>
        /// <exception cref="ArgumentNullException">Thrown when row is null</exception>
        public string ToForXmlFormat(DataRow row, string rowName = "Row", bool includeSchema = true)
        {
            if (row == null)
                throw new ArgumentNullException(nameof(row));

            var xsiNamespace = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");
            var rowNamespace = XNamespace.Get("urn:schemas-microsoft-com:sql:SqlRowSet4");
            
            var root = new XElement("rows");
            root.Add(new XAttribute(XNamespace.Xmlns + "xsi", xsiNamespace));

            if (includeSchema)
            {
                var schemaElement = CreateSqlServerXmlSchema(row.Table);
                root.Add(schemaElement);
            }

            var rowElement = new XElement(rowNamespace + rowName);

            foreach (DataColumn column in row.Table.Columns)
            {
                var value = row[column];
                var columnElement = new XElement(rowNamespace + column.ColumnName);

                if (value == DBNull.Value || value == null)
                {
                    columnElement.Add(new XAttribute(xsiNamespace + "nil", "true"));
                }
                else
                {
                    columnElement.Value = ConvertValueToString(value, column.DataType);
                }

                rowElement.Add(columnElement);
            }

            root.Add(rowElement);
            return root.ToString();
        }

        #endregion

        #region SQL Server XML Schema Methods

        /// <summary>
        /// Parses SQL Server XML schema to extract column definitions
        /// </summary>
        /// <param name="schemaElement">XML schema element</param>
        /// <param name="dataTable">DataTable to populate with schema</param>
        private void ParseSqlServerXmlSchema(XElement schemaElement, DataTable dataTable)
        {
            var xsdNamespace = "http://www.w3.org/2001/XMLSchema";

            // Find the Row element definition in the schema
            var rowElement = schemaElement.Element(XName.Get("element", xsdNamespace));
            if (rowElement == null || rowElement.Attribute("name")?.Value != "Row") return;

            var complexType = rowElement.Element(XName.Get("complexType", xsdNamespace));
            if (complexType == null) return;

            var sequence = complexType.Element(XName.Get("sequence", xsdNamespace));
            if (sequence == null) return;

            foreach (var element in sequence.Elements(XName.Get("element", xsdNamespace)))
            {
                var name = element.Attribute("name")?.Value;
                var type = element.Attribute("type")?.Value;
                var nillable = element.Attribute("nillable")?.Value == "1";

                if (string.IsNullOrEmpty(name)) continue;

                Type dataType = typeof(string); // Default to string

                // Check if there's a direct type attribute
                if (!string.IsNullOrEmpty(type))
                {
                    dataType = GetClrTypeFromSqlServerType(type);
                }
                else
                {
                    // Check for simpleType with restriction
                    var simpleType = element.Element(XName.Get("simpleType", xsdNamespace));
                    if (simpleType != null)
                    {
                        var restriction = simpleType.Element(XName.Get("restriction", xsdNamespace));
                        if (restriction != null)
                        {
                            var baseType = restriction.Attribute("base")?.Value;
                            if (!string.IsNullOrEmpty(baseType))
                            {
                                dataType = GetClrTypeFromSqlServerType(baseType);
                            }
                        }
                    }
                }

                var column = new DataColumn(name, dataType);
                column.AllowDBNull = nillable;

                // Extract max length only for string types (not for varbinary/binary types)
                if (dataType == typeof(string))
                {
                    var simpleTypeForLength = element.Element(XName.Get("simpleType", xsdNamespace));
                    if (simpleTypeForLength != null)
                    {
                        var restriction = simpleTypeForLength.Element(XName.Get("restriction", xsdNamespace));
                        if (restriction != null)
                        {
                            var maxLength = restriction.Element(XName.Get("maxLength", xsdNamespace));
                            if (maxLength != null)
                            {
                                var lengthValue = maxLength.Attribute("value")?.Value;
                                if (int.TryParse(lengthValue, out int maxLen))
                                {
                                    column.MaxLength = maxLen;
                                }
                            }
                        }
                    }
                }

                // Only add column if it doesn't already exist
                if (!dataTable.Columns.Contains(name))
                {
                    dataTable.Columns.Add(column);
                }
            }
        }

        /// <summary>
        /// Creates SQL Server XML schema from DataTable
        /// </summary>
        /// <param name="table">DataTable to create schema for</param>
        /// <returns>XML schema element</returns>
        private XElement CreateSqlServerXmlSchema(DataTable table)
        {
            var xsdNamespace = XNamespace.Get("http://www.w3.org/2001/XMLSchema");
            var sqlTypesNamespace = XNamespace.Get("http://schemas.microsoft.com/sqlserver/2004/sqltypes");

            var schema = new XElement(xsdNamespace + "schema",
                new XAttribute(XNamespace.Xmlns + "xsd", xsdNamespace),
                new XAttribute(XNamespace.Xmlns + "sqltypes", sqlTypesNamespace),
                new XAttribute("targetNamespace", "urn:schemas-microsoft-com:sql:SqlRowSet4"),
                new XAttribute("elementFormDefault", "qualified"),
                new XElement(xsdNamespace + "import",
                    new XAttribute("namespace", sqlTypesNamespace.NamespaceName),
                    new XAttribute("schemaLocation", "http://schemas.microsoft.com/sqlserver/2004/sqltypes/sqltypes.xsd")
                ),
                new XElement(xsdNamespace + "element",
                    new XAttribute("name", "Row"),
                    new XElement(xsdNamespace + "complexType",
                        new XElement(xsdNamespace + "sequence",
                            table.Columns.Cast<DataColumn>().Select(col =>
                                CreateSchemaElement(col, xsdNamespace, sqlTypesNamespace)
                            )
                        )
                    )
                )
            );

            return schema;
        }

        /// <summary>
        /// Creates schema element for a column
        /// </summary>
        /// <param name="column">DataColumn to create schema for</param>
        /// <param name="xsdNamespace">XSD namespace</param>
        /// <param name="sqlTypesNamespace">SQL types namespace</param>
        /// <returns>Schema element</returns>
        private XElement CreateSchemaElement(DataColumn column, XNamespace xsdNamespace, XNamespace sqlTypesNamespace)
        {
            var element = new XElement(xsdNamespace + "element",
                new XAttribute("name", column.ColumnName),
                new XAttribute("nillable", column.AllowDBNull ? "1" : "0")
            );

            // Map CLR types to SQL Server types
            var sqlServerType = GetSqlServerTypeFromClrType(column.DataType);
            
            // For simple types like int, use direct type attribute
            if (column.DataType == typeof(int) || column.DataType == typeof(long) || 
                column.DataType == typeof(short) || column.DataType == typeof(byte) ||
                column.DataType == typeof(bool) || column.DataType == typeof(DateTime) ||
                column.DataType == typeof(decimal) || column.DataType == typeof(double) ||
                column.DataType == typeof(float) || column.DataType == typeof(TimeSpan) ||
                column.DataType == typeof(DateTimeOffset) || column.DataType == typeof(Guid))
            {
                element.Add(new XAttribute("type", sqlServerType));
            }
            else if (column.DataType == typeof(byte[]))
            {
                // For binary types, use direct type attribute without maxLength restriction
                element.Add(new XAttribute("type", sqlServerType));
            }
            else
            {
                // For string types only, create simpleType with restriction and maxLength
                element.Add(new XElement(xsdNamespace + "simpleType",
                    new XElement(xsdNamespace + "restriction",
                        new XAttribute("base", sqlServerType),
                        new XAttribute(sqlTypesNamespace + "localeId", "1042"),
                        new XAttribute(sqlTypesNamespace + "sqlCompareOptions", "IgnoreCase IgnoreKanaType IgnoreWidth"),
                        new XElement(xsdNamespace + "maxLength",
                            new XAttribute("value", column.MaxLength > 0 ? column.MaxLength.ToString() : "50")
                        )
                    )
                ));
            }

            return element;
        }

        /// <summary>
        /// Converts SQL Server type to CLR type
        /// </summary>
        /// <param name="sqlServerType">SQL Server type string</param>
        /// <returns>CLR type</returns>
        private Type GetClrTypeFromSqlServerType(string sqlServerType)
        {
            if (string.IsNullOrEmpty(sqlServerType))
                return typeof(string);

            // Remove namespace prefix if present
            var typeName = sqlServerType;
            if (sqlServerType.Contains(":"))
            {
                typeName = sqlServerType.Split(':').Last();
            }

            switch (typeName.ToLower())
            {
                case "int":
                    return typeof(int);
                case "bigint":
                    return typeof(long);
                case "smallint":
                    return typeof(short);
                case "tinyint":
                    return typeof(byte);
                case "decimal":
                case "money":
                case "smallmoney":
                    return typeof(decimal);
                case "float":
                    return typeof(double);
                case "real":
                    return typeof(float);
                case "bit":
                    return typeof(bool);
                case "datetime":
                case "datetime2":
                case "smalldatetime":
                    return typeof(DateTime);
                case "date":
                    return typeof(DateTime);
                case "time":
                    return typeof(TimeSpan);
                case "datetimeoffset":
                    return typeof(DateTimeOffset);
                case "uniqueidentifier":
                    return typeof(Guid);
                case "binary":
                case "varbinary":
                case "image":
                    return typeof(byte[]);
                case "char":
                case "varchar":
                case "text":
                case "nchar":
                case "nvarchar":
                case "ntext":
                case "xml":
                default:
                    return typeof(string);
            }
        }

        /// <summary>
        /// Converts CLR type to SQL Server type
        /// </summary>
        /// <param name="clrType">CLR type</param>
        /// <returns>SQL Server type string</returns>
        private string GetSqlServerTypeFromClrType(Type clrType)
        {
            if (clrType == typeof(int)) return "sqltypes:int";
            if (clrType == typeof(long)) return "sqltypes:bigint";
            if (clrType == typeof(short)) return "sqltypes:smallint";
            if (clrType == typeof(byte)) return "sqltypes:tinyint";
            if (clrType == typeof(decimal)) return "sqltypes:decimal";
            if (clrType == typeof(double)) return "sqltypes:float";
            if (clrType == typeof(float)) return "sqltypes:real";
            if (clrType == typeof(bool)) return "sqltypes:bit";
            if (clrType == typeof(DateTime)) return "sqltypes:datetime2";
            if (clrType == typeof(TimeSpan)) return "sqltypes:time";
            if (clrType == typeof(DateTimeOffset)) return "sqltypes:datetimeoffset";
            if (clrType == typeof(Guid)) return "sqltypes:uniqueidentifier";
            if (clrType == typeof(byte[])) return "sqltypes:varbinary";
            if (clrType == typeof(string)) return "sqltypes:nvarchar";
            
            return "sqltypes:nvarchar";
        }

        /// <summary>
        /// Parses a single row element from FOR XML output
        /// </summary>
        /// <param name="rowElement">Row XML element</param>
        /// <param name="dataRow">DataRow to populate</param>
        /// <param name="dataTable">DataTable containing the row</param>
        private void ParseForXmlRow(XElement rowElement, DataRow dataRow, DataTable dataTable)
        {
            foreach (var element in rowElement.Elements())
            {
                var columnName = element.Name.LocalName;
                
                // Add column if it doesn't exist (fallback)
                if (!dataTable.Columns.Contains(columnName))
                {
                    dataTable.Columns.Add(columnName, typeof(string));
                }

                var column = dataTable.Columns[columnName];
                var isNull = element.Attribute(XName.Get("nil", "http://www.w3.org/2001/XMLSchema-instance"))?.Value == "true";

                if (isNull || string.IsNullOrEmpty(element.Value))
                {
                    dataRow[columnName] = DBNull.Value;
                }
                else
                {
                    try
                    {
                        var convertedValue = ConvertStringToValue(element.Value, column.DataType);
                        dataRow[columnName] = convertedValue;
                    }
                    catch
                    {
                        // If conversion fails, store as string
                        dataRow[columnName] = element.Value;
                    }
                }
            }
        }

        #endregion

        // Private helper methods
        /// <summary>
        /// Converts a value to its string representation for XML serialization
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <param name="dataType">Data type of the value</param>
        /// <returns>String representation of the value</returns>
        public string ConvertValueToString(object value, Type dataType)
        {
            if (value == null || value == DBNull.Value)
                return string.Empty;

            if (dataType == typeof(byte[]))
                return Convert.ToBase64String((byte[])value);
            if (dataType == typeof(DateTime))
                return ((DateTime)value).ToString("O", CultureInfo.InvariantCulture);
            if (dataType == typeof(DateTimeOffset))
                return ((DateTimeOffset)value).ToString("O", CultureInfo.InvariantCulture);
            if (dataType == typeof(TimeSpan))
                return ((TimeSpan)value).ToString("c", CultureInfo.InvariantCulture);
            if (dataType == typeof(Guid))
                return ((Guid)value).ToString("D", CultureInfo.InvariantCulture);

            return value.ToString();
        }

        private string ConvertValueToString(object value, SqlDbType sqlType)
        {
            if (value == null || value == DBNull.Value)
                return string.Empty;

            switch (sqlType)
            {
                case SqlDbType.Binary:
                case SqlDbType.VarBinary:
                case SqlDbType.Image:
                    if (value is byte[] bytes)
                        return Convert.ToBase64String(bytes);
                    break;
                case SqlDbType.DateTime:
                case SqlDbType.DateTime2:
                case SqlDbType.SmallDateTime:
                    if (value is DateTime dt)
                        return dt.ToString("O", CultureInfo.InvariantCulture);
                    break;
                case SqlDbType.DateTimeOffset:
                    if (value is DateTimeOffset dto)
                        return dto.ToString("O", CultureInfo.InvariantCulture);
                    break;
                case SqlDbType.Time:
                    if (value is TimeSpan ts)
                        return ts.ToString("c", CultureInfo.InvariantCulture);
                    break;
                case SqlDbType.UniqueIdentifier:
                    if (value is Guid guid)
                        return guid.ToString("D", CultureInfo.InvariantCulture);
                    break;
            }

            return value.ToString();
        }

        /// <summary>
        /// Converts a string back to a value of the specified type
        /// </summary>
        /// <param name="value">String to convert</param>
        /// <param name="dataType">Target data type</param>
        /// <returns>Converted value</returns>
        public object ConvertStringToValue(string value, Type dataType)
        {
            if (string.IsNullOrEmpty(value))
                return DBNull.Value;

            if (dataType == typeof(bool))
            {
                // Handle SQL Server boolean format (1/0) as well as true/false
                if (value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (value == "0" || value.Equals("false", StringComparison.OrdinalIgnoreCase))
                    return false;
                return bool.Parse(value);
            }
            if (dataType == typeof(byte))
                return byte.Parse(value, CultureInfo.InvariantCulture);
            if (dataType == typeof(short))
                return short.Parse(value, CultureInfo.InvariantCulture);
            if (dataType == typeof(int))
                return int.Parse(value, CultureInfo.InvariantCulture);
            if (dataType == typeof(long))
                return long.Parse(value, CultureInfo.InvariantCulture);
            if (dataType == typeof(decimal))
                return decimal.Parse(value, CultureInfo.InvariantCulture);
            if (dataType == typeof(double))
                return double.Parse(value, CultureInfo.InvariantCulture);
            if (dataType == typeof(float))
                return float.Parse(value, CultureInfo.InvariantCulture);
            if (dataType == typeof(DateTime))
                return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            if (dataType == typeof(TimeSpan))
                return TimeSpan.Parse(value, CultureInfo.InvariantCulture);
            if (dataType == typeof(DateTimeOffset))
                return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
            if (dataType == typeof(Guid))
                return Guid.Parse(value);
            if (dataType == typeof(byte[]))
                return Convert.FromBase64String(value);

            return value;
        }

        private object ConvertStringToValue(string value, SqlDbType sqlType)
        {
            if (string.IsNullOrEmpty(value))
                return DBNull.Value;

            switch (sqlType)
            {
                case SqlDbType.Bit:
                    // Handle SQL Server boolean format (1/0) as well as true/false
                    if (value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (value == "0" || value.Equals("false", StringComparison.OrdinalIgnoreCase))
                        return false;
                    return bool.Parse(value);
                case SqlDbType.TinyInt:
                    return byte.Parse(value, CultureInfo.InvariantCulture);
                case SqlDbType.SmallInt:
                    return short.Parse(value, CultureInfo.InvariantCulture);
                case SqlDbType.Int:
                    return int.Parse(value, CultureInfo.InvariantCulture);
                case SqlDbType.BigInt:
                    return long.Parse(value, CultureInfo.InvariantCulture);
                case SqlDbType.Decimal:
                case SqlDbType.Money:
                case SqlDbType.SmallMoney:
                    return decimal.Parse(value, CultureInfo.InvariantCulture);
                case SqlDbType.Float:
                    return double.Parse(value, CultureInfo.InvariantCulture);
                case SqlDbType.Real:
                    return float.Parse(value, CultureInfo.InvariantCulture);
                case SqlDbType.Date:
                case SqlDbType.DateTime:
                case SqlDbType.DateTime2:
                case SqlDbType.SmallDateTime:
                    return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                case SqlDbType.Time:
                    return TimeSpan.Parse(value, CultureInfo.InvariantCulture);
                case SqlDbType.DateTimeOffset:
                    return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
                case SqlDbType.UniqueIdentifier:
                    return Guid.Parse(value);
                case SqlDbType.Binary:
                case SqlDbType.VarBinary:
                case SqlDbType.Image:
                    return Convert.FromBase64String(value);
                case SqlDbType.Char:
                case SqlDbType.VarChar:
                case SqlDbType.Text:
                case SqlDbType.NChar:
                case SqlDbType.NVarChar:
                case SqlDbType.NText:
                case SqlDbType.Xml:
                default:
                    return value;
            }
        }

        private void SetRecordValue(SqlDataRecord record, int ordinal, object value, SqlDbType sqlType)
        {
            if (value == null || value == DBNull.Value)
            {
                record.SetDBNull(ordinal);
                return;
            }

            switch (sqlType)
            {
                case SqlDbType.Bit:
                    record.SetBoolean(ordinal, (bool)value);
                    break;
                case SqlDbType.TinyInt:
                    record.SetByte(ordinal, (byte)value);
                    break;
                case SqlDbType.SmallInt:
                    record.SetInt16(ordinal, (short)value);
                    break;
                case SqlDbType.Int:
                    record.SetInt32(ordinal, (int)value);
                    break;
                case SqlDbType.BigInt:
                    record.SetInt64(ordinal, (long)value);
                    break;
                case SqlDbType.Decimal:
                case SqlDbType.Money:
                case SqlDbType.SmallMoney:
                    record.SetDecimal(ordinal, (decimal)value);
                    break;
                case SqlDbType.Float:
                    record.SetDouble(ordinal, (double)value);
                    break;
                case SqlDbType.Real:
                    record.SetFloat(ordinal, (float)value);
                    break;
                case SqlDbType.Date:
                case SqlDbType.DateTime:
                case SqlDbType.DateTime2:
                case SqlDbType.SmallDateTime:
                    record.SetDateTime(ordinal, (DateTime)value);
                    break;
                case SqlDbType.Time:
                    record.SetTimeSpan(ordinal, (TimeSpan)value);
                    break;
                case SqlDbType.DateTimeOffset:
                    record.SetDateTimeOffset(ordinal, (DateTimeOffset)value);
                    break;
                case SqlDbType.UniqueIdentifier:
                    record.SetGuid(ordinal, (Guid)value);
                    break;
                case SqlDbType.Binary:
                case SqlDbType.VarBinary:
                case SqlDbType.Image:
                    record.SetBytes(ordinal, 0, (byte[])value, 0, ((byte[])value).Length);
                    break;
                case SqlDbType.Char:
                case SqlDbType.VarChar:
                case SqlDbType.Text:
                case SqlDbType.NChar:
                case SqlDbType.NVarChar:
                case SqlDbType.NText:
                case SqlDbType.Xml:
                default:
                    record.SetString(ordinal, value.ToString());
                    break;
            }
        }

        private void ValidateRecordStructure(XElement root, ValidationResult result)
        {
            var columns = root.Elements("Column").ToList();
            if (columns.Count == 0)
            {
                result.IsValid = false;
                result.Errors.Add("Record must contain at least one Column element");
                return;
            }

            foreach (var column in columns)
            {
                if (column.Attribute("Name") == null)
                {
                    result.IsValid = false;
                    result.Errors.Add("Column element must have a Name attribute");
                }
                if (column.Attribute("Type") == null)
                {
                    result.IsValid = false;
                    result.Errors.Add("Column element must have a Type attribute");
                }
            }
        }

        private void ValidateRowStructure(XElement root, ValidationResult result)
        {
            var columns = root.Elements("Column").ToList();
            if (columns.Count == 0)
            {
                result.IsValid = false;
                result.Errors.Add("Row must contain at least one Column element");
                return;
            }

            foreach (var column in columns)
            {
                if (column.Attribute("Name") == null)
                {
                    result.IsValid = false;
                    result.Errors.Add("Column element must have a Name attribute");
                }
            }
        }

        private void ValidateTableStructure(XElement root, ValidationResult result)
        {
            var schema = root.Element("Schema");
            var data = root.Element("Data");

            if (schema == null)
            {
                result.IsValid = false;
                result.Errors.Add("Table must contain a Schema element");
            }
            else
            {
                var schemaColumns = schema.Elements("Column").ToList();
                if (schemaColumns.Count == 0)
                {
                    result.IsValid = false;
                    result.Errors.Add("Schema must contain at least one Column element");
                }
            }

            if (data != null)
            {
                var rows = data.Elements("Row").ToList();
                foreach (var row in rows)
                {
                    ValidateRowStructure(row, result);
                }
            }
        }
    }
} 