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
                return bool.Parse(value);
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