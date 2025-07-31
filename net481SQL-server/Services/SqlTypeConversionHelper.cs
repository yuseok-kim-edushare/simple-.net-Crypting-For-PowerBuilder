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
                // Integer types
                case SqlDbType.Int: return "Int32";
                case SqlDbType.BigInt: return "Int64";
                case SqlDbType.SmallInt: return "Int16";
                case SqlDbType.TinyInt: return "Byte";
                
                // Decimal/Numeric types
                case SqlDbType.Decimal: return "Decimal";
                case SqlDbType.Money: return "Decimal";
                case SqlDbType.SmallMoney: return "Decimal";
                case SqlDbType.Float: return "Double";
                case SqlDbType.Real: return "Single";
                
                // Boolean type
                case SqlDbType.Bit: return "Boolean";
                
                // Date/Time types
                case SqlDbType.DateTime: return "DateTime";
                case SqlDbType.DateTime2: return "DateTime";
                case SqlDbType.SmallDateTime: return "DateTime";
                case SqlDbType.Date: return "DateTime";
                case SqlDbType.Time: return "TimeSpan";
                case SqlDbType.DateTimeOffset: return "DateTimeOffset";
                
                // Unique identifier
                case SqlDbType.UniqueIdentifier: return "Guid";
                
                // Binary types
                case SqlDbType.Binary: return "Byte[]";
                case SqlDbType.VarBinary: return "Byte[]";
                case SqlDbType.Image: return "Byte[]";
                case SqlDbType.Timestamp: return "Byte[]";
                
                // String types
                case SqlDbType.Char: return "String";
                case SqlDbType.VarChar: return "String";
                case SqlDbType.Text: return "String";
                case SqlDbType.NChar: return "String";
                case SqlDbType.NVarChar: return "String";
                case SqlDbType.NText: return "String";
                
                // XML type
                case SqlDbType.Xml: return "String";
                
                // SQL Server specific types
                case SqlDbType.Variant: return "Object";
                case SqlDbType.Udt: return "Object";
                case SqlDbType.Structured: return "Object";
                
                default: return "Object";
            }
        }

        /// <summary>
        /// Parses SqlDbType from string with error handling
        /// </summary>
        /// <param name="sqlDbTypeString">SqlDbType string representation</param>
        /// <returns>SqlDbType value</returns>
        public static SqlDbType ParseSqlDbType(string sqlDbTypeString)
        {
            if (string.IsNullOrEmpty(sqlDbTypeString)) 
                return SqlDbType.NVarChar;
            
            try
            {
                // Try direct enum parsing first
                if (Enum.TryParse<SqlDbType>(sqlDbTypeString, true, out SqlDbType result))
                    return result;
                
                // Fallback for common variations
                var typeLower = sqlDbTypeString.ToLowerInvariant();
                switch (typeLower)
                {
                    case "numeric": return SqlDbType.Decimal;
                    case "timestamp": return SqlDbType.Timestamp;
                    case "rowversion": return SqlDbType.Timestamp;
                    default: return SqlDbType.NVarChar;
                }
            }
            catch
            {
                return SqlDbType.NVarChar;
            }
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

            // Special handling for SQL types
            if (value is SqlXml sqlXml)
            {
                return sqlXml.IsNull ? string.Empty : sqlXml.Value;
            }

            // Binary data handling
            if (value is byte[] bytes)
            {
                return Convert.ToBase64String(bytes);
            }

            // Date/Time handling with ISO format for round-trip compatibility
            if (value is DateTime dateTime)
            {
                return dateTime.ToString("O", CultureInfo.InvariantCulture);
            }

            if (value is DateTimeOffset dateTimeOffset)
            {
                return dateTimeOffset.ToString("O", CultureInfo.InvariantCulture);
            }

            if (value is TimeSpan timeSpan)
            {
                return timeSpan.ToString("c", CultureInfo.InvariantCulture);
            }

            // GUID handling with consistent format
            if (value is Guid guid)
            {
                return guid.ToString("D", CultureInfo.InvariantCulture);
            }

            // Numeric types with invariant culture for consistency
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

            // Boolean handling
            if (value is bool boolValue)
            {
                return boolValue.ToString().ToLowerInvariant();
            }

            return value.ToString();
        }

        /// <summary>
        /// Converts a value to string representation based on SQL type for XML serialization
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <param name="sqlType">SQL type for specialized handling</param>
        /// <returns>String representation</returns>
        public static string ConvertValueToString(object value, SqlDbType sqlType)
        {
            if (value == null || value == DBNull.Value)
                return string.Empty;

            switch (sqlType)
            {
                case SqlDbType.Binary:
                case SqlDbType.VarBinary:
                case SqlDbType.Image:
                case SqlDbType.Timestamp:
                    if (value is byte[] bytes)
                        return Convert.ToBase64String(bytes);
                    break;
                    
                case SqlDbType.DateTime:
                case SqlDbType.DateTime2:
                case SqlDbType.SmallDateTime:
                case SqlDbType.Date:
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
                    
                case SqlDbType.Bit:
                    if (value is bool boolValue)
                        return boolValue ? "1" : "0";
                    break;
                    
                case SqlDbType.Decimal:
                case SqlDbType.Money:
                case SqlDbType.SmallMoney:
                case SqlDbType.Float:
                case SqlDbType.Real:
                    return value.ToString();
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
            // Only convert to DBNull if truly null, preserve empty strings
            if (stringValue == null)
            {
                if (targetType == typeof(SqlXml))
                    return SqlXml.Null;
                return DBNull.Value;
            }

            try
            {
                if (targetType == typeof(string))
                    return stringValue;

                // Handle empty strings for value types
                if (string.IsNullOrEmpty(stringValue))
                {
                    if (targetType == typeof(SqlXml))
                        return SqlXml.Null;
                    // For value types, return DBNull for empty strings
                    if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                        return DBNull.Value;
                    return stringValue; // Return empty string for string types
                }

                // Integer types
                if (targetType == typeof(int))
                    return int.Parse(stringValue, CultureInfo.InvariantCulture);
                if (targetType == typeof(long))
                    return long.Parse(stringValue, CultureInfo.InvariantCulture);
                if (targetType == typeof(short))
                    return short.Parse(stringValue, CultureInfo.InvariantCulture);
                if (targetType == typeof(byte))
                    return byte.Parse(stringValue, CultureInfo.InvariantCulture);

                // Decimal types
                if (targetType == typeof(decimal))
                    return decimal.Parse(stringValue, CultureInfo.InvariantCulture);
                if (targetType == typeof(double))
                    return double.Parse(stringValue, CultureInfo.InvariantCulture);
                if (targetType == typeof(float))
                    return float.Parse(stringValue, CultureInfo.InvariantCulture);

                // Boolean with SQL Server compatibility
                if (targetType == typeof(bool))
                {
                    if (stringValue == "1" || stringValue.Equals("true", StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (stringValue == "0" || stringValue.Equals("false", StringComparison.OrdinalIgnoreCase))
                        return false;
                    return bool.Parse(stringValue);
                }

                // Date/Time types with round-trip support
                if (targetType == typeof(DateTime))
                    return DateTime.Parse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                if (targetType == typeof(DateTimeOffset))
                    return DateTimeOffset.Parse(stringValue, CultureInfo.InvariantCulture);
                if (targetType == typeof(TimeSpan))
                    return TimeSpan.Parse(stringValue, CultureInfo.InvariantCulture);

                // GUID
                if (targetType == typeof(Guid))
                    return Guid.Parse(stringValue);

                // Binary data
                if (targetType == typeof(byte[]))
                    return Convert.FromBase64String(stringValue);

                // XML handling
                if (targetType == typeof(SqlXml))
                {
                    try
                    {
                        return new SqlXml(XDocument.Parse(stringValue).CreateReader());
                    }
                    catch
                    {
                        // If XML parsing fails, return as string
                        return stringValue;
                    }
                }

                // Fallback to string
                return stringValue;
            }
            catch
            {
                // If conversion fails, return the original string or DBNull based on target type
                if (targetType == typeof(string))
                    return stringValue;
                return DBNull.Value;
            }
        }

        /// <summary>
        /// Converts a string value back to the appropriate value based on SQL type
        /// </summary>
        /// <param name="stringValue">String value to convert</param>
        /// <param name="sqlType">Target SQL type</param>
        /// <returns>Converted value</returns>
        public static object ConvertStringToValue(string stringValue, SqlDbType sqlType)
        {
            // Only convert to DBNull if truly null, preserve empty strings
            if (stringValue == null)
                return DBNull.Value;

            try
            {
                switch (sqlType)
                {
                    case SqlDbType.Bit:
                        // Handle SQL Server boolean format (1/0) as well as true/false
                        if (stringValue == "1" || stringValue.Equals("true", StringComparison.OrdinalIgnoreCase))
                            return true;
                        if (stringValue == "0" || stringValue.Equals("false", StringComparison.OrdinalIgnoreCase))
                            return false;
                        return bool.Parse(stringValue);
                        
                    case SqlDbType.TinyInt:
                        return byte.Parse(stringValue, CultureInfo.InvariantCulture);
                    case SqlDbType.SmallInt:
                        return short.Parse(stringValue, CultureInfo.InvariantCulture);
                    case SqlDbType.Int:
                        return int.Parse(stringValue, CultureInfo.InvariantCulture);
                    case SqlDbType.BigInt:
                        return long.Parse(stringValue, CultureInfo.InvariantCulture);
                        
                    case SqlDbType.Decimal:
                    case SqlDbType.Money:
                    case SqlDbType.SmallMoney:
                        return decimal.Parse(stringValue, CultureInfo.InvariantCulture);
                    case SqlDbType.Float:
                        return double.Parse(stringValue, CultureInfo.InvariantCulture);
                    case SqlDbType.Real:
                        return float.Parse(stringValue, CultureInfo.InvariantCulture);
                        
                    case SqlDbType.Date:
                    case SqlDbType.DateTime:
                    case SqlDbType.DateTime2:
                    case SqlDbType.SmallDateTime:
                        return DateTime.Parse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                    case SqlDbType.Time:
                        return TimeSpan.Parse(stringValue, CultureInfo.InvariantCulture);
                    case SqlDbType.DateTimeOffset:
                        return DateTimeOffset.Parse(stringValue, CultureInfo.InvariantCulture);
                        
                    case SqlDbType.UniqueIdentifier:
                        return Guid.Parse(stringValue);
                        
                    case SqlDbType.Binary:
                    case SqlDbType.VarBinary:
                    case SqlDbType.Image:
                    case SqlDbType.Timestamp:
                        return Convert.FromBase64String(stringValue);
                        
                    case SqlDbType.Char:
                    case SqlDbType.VarChar:
                    case SqlDbType.Text:
                    case SqlDbType.NChar:
                    case SqlDbType.NVarChar:
                    case SqlDbType.NText:
                    case SqlDbType.Xml:
                    default:
                        return stringValue;
                }
            }
            catch
            {
                // If conversion fails, return the original string
                return stringValue;
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

        /// <summary>
        /// Converts original SQL type string to SqlDbType
        /// </summary>
        /// <param name="originalSqlType">Original SQL type (e.g., "char", "varchar", "nvarchar")</param>
        /// <returns>SqlDbType enum value</returns>
        public static SqlDbType GetSqlDbTypeFromOriginalType(string originalSqlType)
        {
            if (string.IsNullOrEmpty(originalSqlType))
                return SqlDbType.NVarChar;

            var typeLower = originalSqlType.ToLowerInvariant();
            
            // String types
            if (typeLower == "char") return SqlDbType.Char;
            if (typeLower == "varchar") return SqlDbType.VarChar;
            if (typeLower == "nchar") return SqlDbType.NChar;
            if (typeLower == "nvarchar") return SqlDbType.NVarChar;
            if (typeLower == "text") return SqlDbType.Text;
            if (typeLower == "ntext") return SqlDbType.NText;
            
            // Numeric types
            if (typeLower == "int") return SqlDbType.Int;
            if (typeLower == "bigint") return SqlDbType.BigInt;
            if (typeLower == "smallint") return SqlDbType.SmallInt;
            if (typeLower == "tinyint") return SqlDbType.TinyInt;
            if (typeLower == "decimal" || typeLower == "numeric") return SqlDbType.Decimal;
            if (typeLower == "float") return SqlDbType.Float;
            if (typeLower == "real") return SqlDbType.Real;
            if (typeLower == "money") return SqlDbType.Money;
            if (typeLower == "smallmoney") return SqlDbType.SmallMoney;
            
            // Date/Time types
            if (typeLower == "datetime") return SqlDbType.DateTime;
            if (typeLower == "datetime2") return SqlDbType.DateTime2;
            if (typeLower == "smalldatetime") return SqlDbType.SmallDateTime;
            if (typeLower == "date") return SqlDbType.Date;
            if (typeLower == "time") return SqlDbType.Time;
            if (typeLower == "datetimeoffset") return SqlDbType.DateTimeOffset;
            
            // Binary types
            if (typeLower == "binary") return SqlDbType.Binary;
            if (typeLower == "varbinary") return SqlDbType.VarBinary;
            if (typeLower == "image") return SqlDbType.Image;
            
            // Other types
            if (typeLower == "bit") return SqlDbType.Bit;
            if (typeLower == "uniqueidentifier") return SqlDbType.UniqueIdentifier;
            if (typeLower == "xml") return SqlDbType.Xml;
            
            // Default fallback
            return SqlDbType.NVarChar;
        }

        /// <summary>
        /// Gets the full SQL type name from original type with length/precision/scale
        /// </summary>
        /// <param name="originalSqlType">Original SQL type</param>
        /// <param name="maxLength">Maximum length</param>
        /// <returns>Full SQL type name string</returns>
        public static string GetSqlTypeNameFromOriginalType(string originalSqlType, int maxLength)
        {
            if (string.IsNullOrEmpty(originalSqlType))
                return "NVARCHAR(MAX)";

            var typeLower = originalSqlType.ToLowerInvariant();
            
            // String types with length
            if (typeLower == "char") return maxLength > 0 ? $"CHAR({maxLength})" : "CHAR(1)";
            if (typeLower == "varchar") return maxLength > 0 && maxLength <= 8000 ? $"VARCHAR({maxLength})" : "VARCHAR(MAX)";
            if (typeLower == "nchar") return maxLength > 0 ? $"NCHAR({maxLength})" : "NCHAR(1)";
            if (typeLower == "nvarchar") return maxLength > 0 && maxLength <= 4000 ? $"NVARCHAR({maxLength})" : "NVARCHAR(MAX)";
            if (typeLower == "text") return "TEXT";
            if (typeLower == "ntext") return "NTEXT";
            
            // Binary types with length
            if (typeLower == "binary") return maxLength > 0 ? $"BINARY({maxLength})" : "BINARY(1)";
            if (typeLower == "varbinary") return maxLength > 0 && maxLength <= 8000 ? $"VARBINARY({maxLength})" : "VARBINARY(MAX)";
            if (typeLower == "image") return "IMAGE";
            
            // Numeric types
            if (typeLower == "int") return "INT";
            if (typeLower == "bigint") return "BIGINT";
            if (typeLower == "smallint") return "SMALLINT";
            if (typeLower == "tinyint") return "TINYINT";
            if (typeLower == "decimal" || typeLower == "numeric") return "DECIMAL(18,2)";
            if (typeLower == "float") return "FLOAT";
            if (typeLower == "real") return "REAL";
            if (typeLower == "money") return "MONEY";
            if (typeLower == "smallmoney") return "SMALLMONEY";
            
            // Date/Time types
            if (typeLower == "datetime") return "DATETIME";
            if (typeLower == "datetime2") return "DATETIME2";
            if (typeLower == "smalldatetime") return "SMALLDATETIME";
            if (typeLower == "date") return "DATE";
            if (typeLower == "time") return "TIME";
            if (typeLower == "datetimeoffset") return "DATETIMEOFFSET";
            
            // Other types
            if (typeLower == "bit") return "BIT";
            if (typeLower == "uniqueidentifier") return "UNIQUEIDENTIFIER";
            if (typeLower == "xml") return "XML";
            
            // Default fallback
            return "NVARCHAR(MAX)";
        }

        /// <summary>
        /// Converts original SQL type string to SQL Server type namespace format
        /// </summary>
        /// <param name="originalSqlType">Original SQL type (e.g., "char", "varchar", "nvarchar")</param>
        /// <returns>SQL Server type string with namespace</returns>
        public static string GetSqlServerTypeFromOriginalType(string originalSqlType)
        {
            if (string.IsNullOrEmpty(originalSqlType))
                return "sqltypes:nvarchar";

            var typeLower = originalSqlType.ToLowerInvariant();
            
            // String types
            if (typeLower == "char") return "sqltypes:char";
            if (typeLower == "varchar") return "sqltypes:varchar";
            if (typeLower == "nchar") return "sqltypes:nchar";
            if (typeLower == "nvarchar") return "sqltypes:nvarchar";
            if (typeLower == "text") return "sqltypes:text";
            if (typeLower == "ntext") return "sqltypes:ntext";
            
            // Numeric types
            if (typeLower == "int") return "sqltypes:int";
            if (typeLower == "bigint") return "sqltypes:bigint";
            if (typeLower == "smallint") return "sqltypes:smallint";
            if (typeLower == "tinyint") return "sqltypes:tinyint";
            if (typeLower == "decimal") return "sqltypes:decimal";
            if (typeLower == "numeric") return "sqltypes:decimal";
            if (typeLower == "float") return "sqltypes:float";
            if (typeLower == "real") return "sqltypes:real";
            if (typeLower == "money") return "sqltypes:money";
            if (typeLower == "smallmoney") return "sqltypes:smallmoney";
            
            // Date/Time types
            if (typeLower == "datetime") return "sqltypes:datetime";
            if (typeLower == "datetime2") return "sqltypes:datetime2";
            if (typeLower == "smalldatetime") return "sqltypes:smalldatetime";
            if (typeLower == "date") return "sqltypes:date";
            if (typeLower == "time") return "sqltypes:time";
            if (typeLower == "datetimeoffset") return "sqltypes:datetimeoffset";
            
            // Binary types
            if (typeLower == "binary") return "sqltypes:binary";
            if (typeLower == "varbinary") return "sqltypes:varbinary";
            if (typeLower == "image") return "sqltypes:image";
            
            // Other types
            if (typeLower == "bit") return "sqltypes:bit";
            if (typeLower == "uniqueidentifier") return "sqltypes:uniqueidentifier";
            if (typeLower == "xml") return "sqltypes:xml";
            
            // Default fallback for unknown types
            return "sqltypes:nvarchar";
        }

        /// <summary>
        /// Converts CLR type to SQL Server type
        /// </summary>
        /// <param name="clrType">CLR type</param>
        /// <returns>SQL Server type string</returns>
        public static string GetSqlServerTypeFromClrType(Type clrType)
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
            if (clrType == typeof(SqlXml)) return "sqltypes:xml";
            
            return "sqltypes:nvarchar";
        }
    }
} 