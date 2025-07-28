using System;
using System.Data;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Globalization;

namespace SecureLibrary.SQL
{
    /// <summary>
    /// Column metadata information for SQL type mapping
    /// </summary>
    public class ColumnInfo
    {
        public string Name { get; set; }
        public string TypeName { get; set; }
        public int? MaxLength { get; set; }
        public byte? Precision { get; set; }
        public byte? Scale { get; set; }
        public bool IsNullable { get; set; }
    }

    /// <summary>
    /// Utility class for mapping SQL types to SqlMetaData and converting string values to proper SQL CLR types
    /// Handles all common SQL Server data types with robust error handling and fallback support
    /// </summary>
    public static class SqlTypeMapping
    {
        /// <summary>
        /// Converts a column metadata descriptor to SqlMetaData for use in TVF
        /// </summary>
        /// <param name="columnInfo">Column information with name, type, maxLength, precision, scale, nullable</param>
        /// <returns>SqlMetaData instance for the column</returns>
        public static SqlMetaData ToMetaData(ColumnInfo columnInfo)
        {
            string name = columnInfo.Name ?? "Column";
            string typeName = (columnInfo.TypeName ?? "nvarchar").ToLowerInvariant();
            int? maxLength = columnInfo.MaxLength;
            byte? precision = columnInfo.Precision;
            byte? scale = columnInfo.Scale;
            bool isNullable = columnInfo.IsNullable;

            try
            {
                switch (typeName)
                {
                    // Integer types
                    case "int":
                    case "integer":
                        return new SqlMetaData(name, SqlDbType.Int);
                    
                    case "bigint":
                        return new SqlMetaData(name, SqlDbType.BigInt);
                    
                    case "smallint":
                        return new SqlMetaData(name, SqlDbType.SmallInt);
                    
                    case "tinyint":
                        return new SqlMetaData(name, SqlDbType.TinyInt);
                    
                    case "bit":
                        return new SqlMetaData(name, SqlDbType.Bit);

                    // Decimal/Numeric types
                    case "decimal":
                    case "numeric":
                        byte prec = precision ?? 18;
                        byte scl = scale ?? 2;
                        return new SqlMetaData(name, SqlDbType.Decimal, prec, scl);
                    
                    case "money":
                        return new SqlMetaData(name, SqlDbType.Money);
                    
                    case "smallmoney":
                        return new SqlMetaData(name, SqlDbType.SmallMoney);

                    // Float types
                    case "float":
                        return new SqlMetaData(name, SqlDbType.Float);
                    
                    case "real":
                        return new SqlMetaData(name, SqlDbType.Real);

                    // String types
                    case "nvarchar":
                        long nvarcharLen = maxLength ?? 4000;
                        if (nvarcharLen > 4000 || nvarcharLen == -1)
                            return new SqlMetaData(name, SqlDbType.NVarChar, SqlMetaData.Max);
                        return new SqlMetaData(name, SqlDbType.NVarChar, nvarcharLen);
                    
                    case "varchar":
                        long varcharLen = maxLength ?? 8000;
                        if (varcharLen > 8000 || varcharLen == -1)
                            return new SqlMetaData(name, SqlDbType.VarChar, SqlMetaData.Max);
                        return new SqlMetaData(name, SqlDbType.VarChar, varcharLen);
                    
                    case "nchar":
                        return new SqlMetaData(name, SqlDbType.NChar, maxLength ?? 1);
                    
                    case "char":
                        return new SqlMetaData(name, SqlDbType.Char, maxLength ?? 1);
                    
                    case "ntext":
                        return new SqlMetaData(name, SqlDbType.NText);
                    
                    case "text":
                        return new SqlMetaData(name, SqlDbType.Text);

                    // Date/Time types
                    case "datetime":
                        return new SqlMetaData(name, SqlDbType.DateTime);
                    
                    case "datetime2":
                        return new SqlMetaData(name, SqlDbType.DateTime2, precision ?? 7);
                    
                    case "smalldatetime":
                        return new SqlMetaData(name, SqlDbType.SmallDateTime);
                    
                    case "date":
                        return new SqlMetaData(name, SqlDbType.Date);
                    
                    case "time":
                        return new SqlMetaData(name, SqlDbType.Time, precision ?? 7);
                    
                    case "datetimeoffset":
                        return new SqlMetaData(name, SqlDbType.DateTimeOffset, precision ?? 7);

                    // Binary types
                    case "varbinary":
                        long varbinaryLen = maxLength ?? 8000;
                        if (varbinaryLen > 8000 || varbinaryLen == -1)
                            return new SqlMetaData(name, SqlDbType.VarBinary, SqlMetaData.Max);
                        return new SqlMetaData(name, SqlDbType.VarBinary, varbinaryLen);
                    
                    case "binary":
                        return new SqlMetaData(name, SqlDbType.Binary, maxLength ?? 1);
                    
                    case "image":
                        return new SqlMetaData(name, SqlDbType.Image);

                    // GUID
                    case "uniqueidentifier":
                        return new SqlMetaData(name, SqlDbType.UniqueIdentifier);

                    // XML
                    case "xml":
                        return new SqlMetaData(name, SqlDbType.Xml);

                    // Timestamp/rowversion
                    case "timestamp":
                    case "rowversion":
                        return new SqlMetaData(name, SqlDbType.Timestamp);

                    // Spatial types (return as NVARCHAR for compatibility)
                    case "geography":
                    case "geometry":
                        return new SqlMetaData(name, SqlDbType.NVarChar, SqlMetaData.Max);

                    // User-defined types, hierarchyid, etc. - fallback to NVARCHAR
                    default:
                        return new SqlMetaData(name, SqlDbType.NVarChar, SqlMetaData.Max);
                }
            }
            catch (Exception)
            {
                // Fallback to NVARCHAR(MAX) if there's any issue with type mapping
                return new SqlMetaData(name, SqlDbType.NVarChar, SqlMetaData.Max);
            }
        }

        /// <summary>
        /// Sets a value in a SqlDataRecord with proper type conversion and error handling
        /// </summary>
        /// <param name="record">The SqlDataRecord to set the value in</param>
        /// <param name="ordinal">Column ordinal position</param>
        /// <param name="stringValue">String value from XML attribute</param>
        /// <param name="columnInfo">Column metadata information</param>
        public static void SetValue(SqlDataRecord record, int ordinal, string stringValue, ColumnInfo columnInfo)
        {
            if (string.IsNullOrEmpty(stringValue))
            {
                record.SetDBNull(ordinal);
                return;
            }

            string typeName = (columnInfo.TypeName ?? "nvarchar").ToLowerInvariant();

            try
            {
                switch (typeName)
                {
                    // Integer types
                    case "int":
                    case "integer":
                        record.SetInt32(ordinal, int.Parse(stringValue, CultureInfo.InvariantCulture));
                        break;
                    
                    case "bigint":
                        record.SetInt64(ordinal, long.Parse(stringValue, CultureInfo.InvariantCulture));
                        break;
                    
                    case "smallint":
                        record.SetInt16(ordinal, short.Parse(stringValue, CultureInfo.InvariantCulture));
                        break;
                    
                    case "tinyint":
                        record.SetByte(ordinal, byte.Parse(stringValue, CultureInfo.InvariantCulture));
                        break;
                    
                    case "bit":
                        bool bitValue = stringValue == "1" || stringValue.ToLowerInvariant() == "true";
                        record.SetBoolean(ordinal, bitValue);
                        break;

                    // Decimal/Numeric types
                    case "decimal":
                    case "numeric":
                        record.SetDecimal(ordinal, decimal.Parse(stringValue, CultureInfo.InvariantCulture));
                        break;
                    
                    case "money":
                    case "smallmoney":
                        record.SetDecimal(ordinal, decimal.Parse(stringValue, CultureInfo.InvariantCulture));
                        break;

                    // Float types
                    case "float":
                        record.SetDouble(ordinal, double.Parse(stringValue, CultureInfo.InvariantCulture));
                        break;
                    
                    case "real":
                        record.SetFloat(ordinal, float.Parse(stringValue, CultureInfo.InvariantCulture));
                        break;

                    // String types
                    case "nvarchar":
                    case "varchar":
                    case "nchar":
                    case "char":
                    case "ntext":
                    case "text":
                        record.SetString(ordinal, stringValue);
                        break;

                    // Date/Time types
                    case "datetime":
                    case "datetime2":
                    case "smalldatetime":
                        record.SetDateTime(ordinal, DateTime.Parse(stringValue, CultureInfo.InvariantCulture));
                        break;
                    
                    case "date":
                        record.SetDateTime(ordinal, DateTime.Parse(stringValue, CultureInfo.InvariantCulture).Date);
                        break;
                    
                    case "time":
                        TimeSpan timeValue = TimeSpan.Parse(stringValue, CultureInfo.InvariantCulture);
                        record.SetTimeSpan(ordinal, timeValue);
                        break;
                    
                    case "datetimeoffset":
                        record.SetDateTimeOffset(ordinal, DateTimeOffset.Parse(stringValue, CultureInfo.InvariantCulture));
                        break;

                    // Binary types - expect Base64 encoded strings
                    case "varbinary":
                    case "binary":
                    case "image":
                        byte[] binaryData = Convert.FromBase64String(stringValue);
                        record.SetBytes(ordinal, 0, binaryData, 0, binaryData.Length);
                        break;

                    // GUID
                    case "uniqueidentifier":
                        record.SetGuid(ordinal, Guid.Parse(stringValue));
                        break;

                    // XML
                    case "xml":
                        record.SetString(ordinal, stringValue); // Store as string, SQL Server will handle XML conversion
                        break;

                    // Timestamp/rowversion - expect Base64 encoded
                    case "timestamp":
                    case "rowversion":
                        byte[] timestampData = Convert.FromBase64String(stringValue);
                        record.SetBytes(ordinal, 0, timestampData, 0, timestampData.Length);
                        break;

                    // Spatial types and fallback
                    case "geography":
                    case "geometry":
                    default:
                        record.SetString(ordinal, stringValue);
                        break;
                }
            }
            catch (Exception)
            {
                // If conversion fails, set as NULL to ensure partial recovery
                record.SetDBNull(ordinal);
            }
        }
    }
}