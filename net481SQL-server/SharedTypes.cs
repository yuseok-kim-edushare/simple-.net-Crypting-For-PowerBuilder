using System;
using System.Xml.Linq;

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
    /// Column metadata discovered from stored procedure result sets
    /// </summary>
    public class ColumnMetadata
    {
        public string Name { get; set; }
        public string SystemTypeName { get; set; }
        public int? MaxLength { get; set; }
        public byte? Precision { get; set; }
        public byte? Scale { get; set; }
        public bool IsNullable { get; set; }
        public int Ordinal { get; set; }
    }

    /// <summary>
    /// Utility class for XML parsing and data type inference
    /// </summary>
    public static class XmlUtilities
    {
        /// <summary>
        /// Helper method to safely parse int attributes from XML
        /// </summary>
        public static int? GetIntAttribute(XElement element, string attributeName)
        {
            var attr = element.Attribute(attributeName);
            if (attr == null || string.IsNullOrEmpty(attr.Value))
                return null;
            
            if (int.TryParse(attr.Value, out int result))
                return result;
            
            return null;
        }

        /// <summary>
        /// Helper method to safely parse byte attributes from XML
        /// </summary>
        public static byte? GetByteAttribute(XElement element, string attributeName)
        {
            var attr = element.Attribute(attributeName);
            if (attr == null || string.IsNullOrEmpty(attr.Value))
                return null;
            
            if (byte.TryParse(attr.Value, out byte result))
                return result;
            
            return null;
        }

        /// <summary>
        /// Enhanced type inference from string values with better accuracy
        /// </summary>
        public static string InferDataType(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "nvarchar";

            // Try GUID first (most specific)
            if (Guid.TryParse(value, out _))
                return "uniqueidentifier";

            // Try boolean (specific pattern)
            if (value == "0" || value == "1" || 
                value.Equals("true", StringComparison.OrdinalIgnoreCase) || 
                value.Equals("false", StringComparison.OrdinalIgnoreCase))
                return "bit";

            // Try integer (check for range)
            if (int.TryParse(value, out int intValue))
            {
                if (intValue >= -32768 && intValue <= 32767)
                    return "smallint";
                else if (intValue >= 0 && intValue <= 255)
                    return "tinyint";
                else
                    return "int";
            }

            // Try bigint
            if (long.TryParse(value, out _))
                return "bigint";

            // Try decimal with precision detection
            if (decimal.TryParse(value, out decimal decimalValue))
            {
                // Check if it's actually an integer
                if (decimalValue == Math.Floor(decimalValue))
                {
                    if (decimalValue >= -32768 && decimalValue <= 32767)
                        return "smallint";
                    else if (decimalValue >= 0 && decimalValue <= 255)
                        return "tinyint";
                    else if (decimalValue >= int.MinValue && decimalValue <= int.MaxValue)
                        return "int";
                    else
                        return "bigint";
                }
                
                // Check decimal precision
                string[] parts = value.Split('.');
                if (parts.Length == 2)
                {
                    int precision = parts[0].Length + parts[1].Length;
                    int scale = parts[1].Length;
                    
                    if (precision <= 38) // SQL Server max precision
                        return "decimal";
                }
                
                return "decimal";
            }

            // Try datetime with multiple formats
            if (DateTime.TryParse(value, out _))
                return "datetime";

            // Try specific date formats
            if (value.Length == 10 && value.IndexOf("-", StringComparison.Ordinal) >= 0) // YYYY-MM-DD
                return "date";

            // Default to nvarchar
            return "nvarchar";
        }

        /// <summary>
        /// Helper method to properly quote identifiers
        /// </summary>
        public static string QUOTENAME(string identifier)
        {
            return "[" + identifier.Replace("]", "]]") + "]";
        }

        /// <summary>
        /// Safely extracts value from XML element or attribute
        /// </summary>
        public static string GetXmlValue(XElement element, string name)
        {
            // Try attribute first
            var attr = element.Attribute(name);
            if (attr != null)
                return attr.Value;

            // Try element
            var elem = element.Element(name);
            if (elem != null)
                return elem.Value;

            return null;
        }

        /// <summary>
        /// Validates if a string can be safely used as a SQL identifier
        /// </summary>
        public static bool IsValidSqlIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return false;

            // Check for SQL Server reserved words (basic check)
            string[] reservedWords = {
                "SELECT", "FROM", "WHERE", "INSERT", "UPDATE", "DELETE", "CREATE", "DROP", "ALTER",
                "TABLE", "VIEW", "PROCEDURE", "FUNCTION", "INDEX", "CONSTRAINT", "PRIMARY", "FOREIGN",
                "KEY", "DEFAULT", "NULL", "NOT", "AND", "OR", "ORDER", "BY", "GROUP", "HAVING",
                "UNION", "JOIN", "INNER", "LEFT", "RIGHT", "OUTER", "CROSS", "FULL", "ON", "AS"
            };

            if (Array.IndexOf(reservedWords, identifier.ToUpperInvariant()) >= 0)
                return false;

            // Check for valid characters
            return System.Text.RegularExpressions.Regex.IsMatch(identifier, @"^[a-zA-Z_][a-zA-Z0-9_]*$");
        }
    }
} 