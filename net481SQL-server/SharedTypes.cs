using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Xml.Linq;

namespace SecureLibrary.SQL
{
    /// <summary>
    /// Information about a database column
    /// </summary>
    public class ColumnInfo
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public string TypeName { get; set; } // Added for backward compatibility
        public int MaxLength { get; set; }
        public bool IsNullable { get; set; }
        public int OrdinalPosition { get; set; }
        public byte? Precision { get; set; } // Added for backward compatibility
        public byte? Scale { get; set; } // Added for backward compatibility
    }

    /// <summary>
    /// Metadata for a column including encryption settings
    /// </summary>
    public class ColumnMetadata
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public int MaxLength { get; set; }
        public bool IsNullable { get; set; }
        public bool IsEncrypted { get; set; }
        public string EncryptionAlgorithm { get; set; }
        public string EncryptionKey { get; set; }
    }

    /// <summary>
    /// Encryption metadata for cryptographic operations
    /// </summary>
    public class EncryptionMetadata
    {
        public string Algorithm { get; set; } = "AES-GCM";
        public string Key { get; set; }
        public byte[] Salt { get; set; }
        public byte[] Nonce { get; set; }
        public int Iterations { get; set; } = 10000;
        public bool AutoGenerateNonce { get; set; } = true;
    }

    /// <summary>
    /// Encrypted value data structure
    /// </summary>
    public class EncryptedValueData
    {
        public byte[] EncryptedValue { get; set; }
        public string DataType { get; set; }
        public EncryptionMetadata Metadata { get; set; }

        public DateTime EncryptedAt { get; set; }
        public int FormatVersion { get; set; } = 1;
    }

    /// <summary>
    /// Encrypted row data structure
    /// </summary>
    public class EncryptedRowData
    {
        public DataTable Schema { get; set; }
        public EncryptionMetadata Metadata { get; set; }
        public DateTime EncryptedAt { get; set; }
        public int FormatVersion { get; set; } = 1;
        public Dictionary<string, byte[]> EncryptedColumns { get; set; } = new Dictionary<string, byte[]>();
    }

    /// <summary>
    /// Result of validation operations
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Whether the validation was successful
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Error messages if validation failed
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Warning messages
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();
    }

    /// <summary>
    /// Utility methods for XML operations
    /// </summary>
    public static class XmlUtilities
    {
        /// <summary>
        /// Parses XML and extracts column information
        /// </summary>
        /// <param name="xml">XML string to parse</param>
        /// <returns>List of column information</returns>
        public static List<ColumnInfo> ParseColumnsFromXml(string xml)
        {
            var columns = new List<ColumnInfo>();
            
            try
            {
                var doc = XDocument.Parse(xml);
                var columnElements = doc.Descendants("Column");
                
                foreach (var element in columnElements)
                {
                    var column = new ColumnInfo
                    {
                        Name = element.Attribute("Name")?.Value ?? "",
                        DataType = element.Attribute("Type")?.Value ?? "",
                        TypeName = element.Attribute("Type")?.Value ?? "", // For backward compatibility
                        MaxLength = int.TryParse(element.Attribute("MaxLength")?.Value, out int maxLen) ? maxLen : -1,
                        IsNullable = element.Attribute("IsNullable")?.Value == "true",
                        OrdinalPosition = int.TryParse(element.Attribute("Ordinal")?.Value, out int ordinal) ? ordinal : 0
                    };
                    
                    columns.Add(column);
                }
            }
            catch (Exception ex)
            {
                // Log error or handle as needed
                throw new ArgumentException($"Failed to parse XML: {ex.Message}", nameof(xml), ex);
            }
            
            return columns;
        }

        /// <summary>
        /// Infers SQL data type from .NET type
        /// </summary>
        /// <param name="netType">.NET type to convert</param>
        /// <returns>Corresponding SQL data type</returns>
        public static string InferSqlDataType(Type netType)
        {
            if (netType == null) return "NVARCHAR(MAX)";
            
            if (netType == typeof(int)) return "INT";
            if (netType == typeof(long)) return "BIGINT";
            if (netType == typeof(short)) return "SMALLINT";
            if (netType == typeof(byte)) return "TINYINT";
            if (netType == typeof(decimal)) return "DECIMAL(18,2)";
            if (netType == typeof(double)) return "FLOAT";
            if (netType == typeof(float)) return "REAL";
            if (netType == typeof(bool)) return "BIT";
            if (netType == typeof(DateTime)) return "DATETIME2";
            if (netType == typeof(DateTimeOffset)) return "DATETIMEOFFSET";
            if (netType == typeof(TimeSpan)) return "TIME";
            if (netType == typeof(Guid)) return "UNIQUEIDENTIFIER";
            if (netType == typeof(byte[])) return "VARBINARY(MAX)";
            if (netType == typeof(string)) return "NVARCHAR(MAX)";
            
            return "NVARCHAR(MAX)"; // Default fallback
        }

        /// <summary>
        /// Validates XML structure for basic compliance
        /// </summary>
        /// <param name="xml">XML string to validate</param>
        /// <returns>Validation result</returns>
        public static ValidationResult ValidateXmlStructure(string xml)
        {
            var result = new ValidationResult { IsValid = true };
            
            if (string.IsNullOrWhiteSpace(xml))
            {
                result.IsValid = false;
                result.Errors.Add("XML string is null or empty");
                return result;
            }
            
            try
            {
                var doc = XDocument.Parse(xml);
                var root = doc.Root;
                
                if (root == null)
                {
                    result.IsValid = false;
                    result.Errors.Add("XML has no root element");
                    return result;
                }
                
                // Check for required elements based on root name
                switch (root.Name.LocalName)
                {
                    case "Root":
                    case "Table":
                    case "Record":
                    case "Row":
                        // These are valid root elements
                        break;
                    default:
                        result.Warnings.Add($"Unknown root element: {root.Name.LocalName}");
                        break;
                }
                
                // Check for Column elements
                var columns = root.Descendants("Column");
                if (!columns.Any())
                {
                    result.Warnings.Add("No Column elements found in XML");
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add($"XML parsing failed: {ex.Message}");
            }
            
            return result;
        }
    }
} 