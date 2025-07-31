using System;
using System.Data;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace SecureLibrary.SQL.Services
{
    /// <summary>
    /// Centralized utility class for SQL Server and CLR type conversions
    /// Consolidates all type conversion helper functions used across the library
    /// </summary>
    public static class SqlTypeConversionHelper
    {
        /// <summary>
        /// Converts CLR type to SqlDbType
        /// </summary>
        /// <param name="clrType">CLR type</param>
        /// <returns>SqlDbType value</returns>
        public static SqlDbType GetSqlDbTypeFromClrType(Type clrType)
        {
            if (clrType == typeof(int)) return SqlDbType.Int;
            if (clrType == typeof(long)) return SqlDbType.BigInt;
            if (clrType == typeof(short)) return SqlDbType.SmallInt;
            if (clrType == typeof(byte)) return SqlDbType.TinyInt;
            if (clrType == typeof(decimal)) return SqlDbType.Decimal;
            if (clrType == typeof(double)) return SqlDbType.Float;
            if (clrType == typeof(float)) return SqlDbType.Real;
            if (clrType == typeof(bool)) return SqlDbType.Bit;
            if (clrType == typeof(DateTime)) return SqlDbType.DateTime2;
            if (clrType == typeof(TimeSpan)) return SqlDbType.Time;
            if (clrType == typeof(DateTimeOffset)) return SqlDbType.DateTimeOffset;
            if (clrType == typeof(Guid)) return SqlDbType.UniqueIdentifier;
            if (clrType == typeof(byte[])) return SqlDbType.VarBinary;
            if (clrType == typeof(string)) return SqlDbType.NVarChar;
            if (clrType == typeof(SqlXml)) return SqlDbType.Xml;
            
            return SqlDbType.NVarChar;
        }

        /// <summary>
        /// Gets the full SQL type name with length/precision/scale
        /// </summary>
        /// <param name="clrType">CLR type</param>
        /// <param name="maxLength">Maximum length for string/binary types</param>
        /// <returns>SQL type name string</returns>
        public static string GetSqlTypeName(Type clrType, int maxLength)
        {
            if (clrType == typeof(int)) return "INT";
            if (clrType == typeof(long)) return "BIGINT";
            if (clrType == typeof(short)) return "SMALLINT";
            if (clrType == typeof(byte)) return "TINYINT";
            if (clrType == typeof(decimal)) return "DECIMAL(18,2)";
            if (clrType == typeof(double)) return "FLOAT";
            if (clrType == typeof(float)) return "REAL";
            if (clrType == typeof(bool)) return "BIT";
            if (clrType == typeof(DateTime)) return "DATETIME2";
            if (clrType == typeof(TimeSpan)) return "TIME";
            if (clrType == typeof(DateTimeOffset)) return "DATETIMEOFFSET";
            if (clrType == typeof(Guid)) return "UNIQUEIDENTIFIER";
            if (clrType == typeof(byte[])) return maxLength > 0 ? $"VARBINARY({maxLength})" : "VARBINARY(MAX)";
            if (clrType == typeof(string)) return maxLength > 0 ? $"NVARCHAR({maxLength})" : "NVARCHAR(MAX)";
            if (clrType == typeof(SqlXml)) return "XML";
            
            return "NVARCHAR(MAX)";
        }

        /// <summary>
        /// Converts SQL Server type to CLR type
        /// </summary>
        /// <param name="sqlServerType">SQL Server type string</param>
        /// <returns>CLR type</returns>
        public static Type GetClrTypeFromSqlServerType(string sqlServerType)
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
                    return typeof(string);
                case "xml":
                    return typeof(SqlXml);
                case "timestamp":
                case "rowversion":
                    return typeof(byte[]);
                default:
                    return typeof(string);
            }
        }

        /// <summary>
        /// Converts SqlDbType to CLR type name string
        /// </summary>
        /// <param name="sqlDbType">SqlDbType value</param>
        /// <returns>CLR type name string</returns>
        public static string GetClrTypeName(SqlDbType sqlDbType)
        {
            switch (sqlDbType)
            {
                case SqlDbType.Int: return "Int32";
                case SqlDbType.BigInt: return "Int64";
                case SqlDbType.SmallInt: return "Int16";
                case SqlDbType.TinyInt: return "Byte";
                case SqlDbType.Decimal: return "Decimal";
                case SqlDbType.Float: return "Double";
                case SqlDbType.Real: return "Single";
                case SqlDbType.Bit: return "Boolean";
                case SqlDbType.DateTime2: return "DateTime";
                case SqlDbType.DateTimeOffset: return "DateTimeOffset";
                case SqlDbType.Time: return "TimeSpan";
                case SqlDbType.UniqueIdentifier: return "Guid";
                case SqlDbType.VarBinary: return "Byte[]";
                case SqlDbType.NVarChar: return "String";
                case SqlDbType.NChar: return "String";
                case SqlDbType.NText: return "String";
                case SqlDbType.Xml: return "String";
                case SqlDbType.Binary: return "Byte[]";
                case SqlDbType.Image: return "Byte[]";
                default: return "Object";
            }
        }

        /// <summary>
        /// Parses SqlDbType from string
        /// </summary>
        /// <param name="sqlDbTypeString">SqlDbType string representation</param>
        /// <returns>SqlDbType value</returns>
        public static SqlDbType ParseSqlDbType(string sqlDbTypeString)
        {
            if (string.IsNullOrEmpty(sqlDbTypeString)) return SqlDbType.NVarChar;
            return (SqlDbType)Enum.Parse(typeof(SqlDbType), sqlDbTypeString);
        }

        /// <summary>
        /// Gets an appropriate default value for a non-nullable column based on its data type
        /// </summary>
        /// <param name="column">The DataColumn to get default value for</param>
        /// <returns>Default value appropriate for the column's data type</returns>
        public static object GetDefaultValueForColumn(DataColumn column)
        {
            if (column.DataType == typeof(string))
                return string.Empty;
            if (column.DataType == typeof(int) || column.DataType == typeof(long) || 
                column.DataType == typeof(short) || column.DataType == typeof(byte))
                return 0;
            if (column.DataType == typeof(decimal) || column.DataType == typeof(double) || 
                column.DataType == typeof(float))
                return 0.0m;
            if (column.DataType == typeof(DateTime))
                return DateTime.MinValue;
            if (column.DataType == typeof(bool))
                return false;
            if (column.DataType == typeof(Guid))
                return Guid.Empty;
            if (column.DataType == typeof(SqlXml))
                return SqlXml.Null;
            if (column.DataType == typeof(byte[]))
                return new byte[0];
            
            // For other types, try to get the default value
            return column.DefaultValue ?? Activator.CreateInstance(column.DataType);
        }

        /// <summary>
        /// Converts a value to string representation for XML serialization
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <returns>String representation</returns>
        public static string ConvertValueToString(object value)
        {
            if (value == null || value == DBNull.Value)
                return string.Empty;

            if (value is SqlXml sqlXml)
            {
                return sqlXml.IsNull ? string.Empty : sqlXml.Value;
            }

            if (value is byte[] bytes)
            {
                return Convert.ToBase64String(bytes);
            }

            if (value is DateTime dateTime)
            {
                return dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture);
            }

            if (value is DateTimeOffset dateTimeOffset)
            {
                return dateTimeOffset.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz", CultureInfo.InvariantCulture);
            }

            if (value is TimeSpan timeSpan)
            {
                return timeSpan.ToString(@"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture);
            }

            if (value is decimal decimalValue)
            {
                return decimalValue.ToString(CultureInfo.InvariantCulture);
            }

            if (value is double doubleValue)
            {
                return doubleValue.ToString(CultureInfo.InvariantCulture);
            }

            if (value is float floatValue)
            {
                return floatValue.ToString(CultureInfo.InvariantCulture);
            }

            return value.ToString();
        }

        /// <summary>
        /// Converts a string value back to the appropriate CLR type
        /// </summary>
        /// <param name="stringValue">String value to convert</param>
        /// <param name="targetType">Target CLR type</param>
        /// <returns>Converted value</returns>
        public static object ConvertStringToValue(string stringValue, Type targetType)
        {
            if (string.IsNullOrEmpty(stringValue))
            {
                if (targetType == typeof(SqlXml))
                    return SqlXml.Null;
                return DBNull.Value;
            }

            try
            {
                if (targetType == typeof(string))
                    return stringValue;

                if (targetType == typeof(int))
                    return int.Parse(stringValue, CultureInfo.InvariantCulture);

                if (targetType == typeof(long))
                    return long.Parse(stringValue, CultureInfo.InvariantCulture);

                if (targetType == typeof(short))
                    return short.Parse(stringValue, CultureInfo.InvariantCulture);

                if (targetType == typeof(byte))
                    return byte.Parse(stringValue, CultureInfo.InvariantCulture);

                if (targetType == typeof(decimal))
                    return decimal.Parse(stringValue, CultureInfo.InvariantCulture);

                if (targetType == typeof(double))
                    return double.Parse(stringValue, CultureInfo.InvariantCulture);

                if (targetType == typeof(float))
                    return float.Parse(stringValue, CultureInfo.InvariantCulture);

                if (targetType == typeof(bool))
                    return bool.Parse(stringValue);

                if (targetType == typeof(DateTime))
                    return DateTime.Parse(stringValue, CultureInfo.InvariantCulture);

                if (targetType == typeof(DateTimeOffset))
                    return DateTimeOffset.Parse(stringValue, CultureInfo.InvariantCulture);

                if (targetType == typeof(TimeSpan))
                    return TimeSpan.Parse(stringValue, CultureInfo.InvariantCulture);

                if (targetType == typeof(Guid))
                    return Guid.Parse(stringValue);

                if (targetType == typeof(byte[]))
                    return Convert.FromBase64String(stringValue);

                if (targetType == typeof(SqlXml))
                {
                    if (string.IsNullOrEmpty(stringValue))
                        return SqlXml.Null;
                    return new SqlXml(XDocument.Parse(stringValue).CreateReader());
                }

                // Fallback to string
                return stringValue;
            }
            catch
            {
                // If conversion fails, return DBNull.Value
                return DBNull.Value;
            }
        }

        /// <summary>
        /// Gets the SQL type string for CAST expressions
        /// </summary>
        /// <param name="sqlDbType">SqlDbType value</param>
        /// <param name="precision">Precision for decimal types</param>
        /// <param name="scale">Scale for decimal types</param>
        /// <param name="maxLength">Maximum length for string/binary types</param>
        /// <returns>SQL type string for CAST</returns>
        public static string GetSqlTypeString(SqlDbType sqlDbType, byte precision = 0, byte scale = 0, long maxLength = 0)
        {
            switch (sqlDbType)
            {
                case SqlDbType.Int:
                    return "INT";
                case SqlDbType.BigInt:
                    return "BIGINT";
                case SqlDbType.SmallInt:
                    return "SMALLINT";
                case SqlDbType.TinyInt:
                    return "TINYINT";
                case SqlDbType.Bit:
                    return "BIT";
                case SqlDbType.Decimal:
                case SqlDbType.Money:
                case SqlDbType.SmallMoney:
                    if (precision > 0 && scale > 0)
                        return $"DECIMAL({precision},{scale})";
                    else if (sqlDbType == SqlDbType.Money)
                        return "MONEY";
                    else if (sqlDbType == SqlDbType.SmallMoney)
                        return "SMALLMONEY";
                    else
                        return "DECIMAL(18,2)";
                case SqlDbType.Float:
                    return "FLOAT";
                case SqlDbType.Real:
                    return "REAL";
                case SqlDbType.Date:
                    return "DATE";
                case SqlDbType.DateTime:
                    return "DATETIME";
                case SqlDbType.DateTime2:
                    return $"DATETIME2({precision})";
                case SqlDbType.Time:
                    return $"TIME({precision})";
                case SqlDbType.DateTimeOffset:
                    return $"DATETIMEOFFSET({precision})";
                case SqlDbType.UniqueIdentifier:
                    return "UNIQUEIDENTIFIER";
                case SqlDbType.VarBinary:
                case SqlDbType.Binary:
                case SqlDbType.Image:
                    return "VARBINARY(MAX)";
                case SqlDbType.Xml:
                    return "XML";
                case SqlDbType.Char:
                    return maxLength > 0 ? $"CHAR({maxLength})" : "CHAR(1)";
                case SqlDbType.NChar:
                    return maxLength > 0 ? $"NCHAR({maxLength})" : "NCHAR(1)";
                case SqlDbType.VarChar:
                    return maxLength > 0 && maxLength <= 8000 ? $"VARCHAR({maxLength})" : "VARCHAR(MAX)";
                case SqlDbType.NVarChar:
                default:
                    return maxLength > 0 && maxLength <= 4000 ? $"NVARCHAR({maxLength})" : "NVARCHAR(MAX)";
            }
        }
    }
} 