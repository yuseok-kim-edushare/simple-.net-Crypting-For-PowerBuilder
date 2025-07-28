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
        /// Simple type inference from string values
        /// </summary>
        public static string InferDataType(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "nvarchar";

            // Try integer
            if (int.TryParse(value, out _))
                return "int";

            // Try decimal
            if (decimal.TryParse(value, out _))
                return "decimal";

            // Try boolean
            if (value == "0" || value == "1" || 
                value.Equals("true", StringComparison.OrdinalIgnoreCase) || 
                value.Equals("false", StringComparison.OrdinalIgnoreCase))
                return "bit";

            // Try datetime
            if (DateTime.TryParse(value, out _))
                return "datetime";

            // Try GUID
            if (Guid.TryParse(value, out _))
                return "uniqueidentifier";

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
    }
} 