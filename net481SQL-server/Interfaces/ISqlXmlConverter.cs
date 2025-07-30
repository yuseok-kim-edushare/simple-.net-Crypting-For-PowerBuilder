using System;
using System.Data;
using System.Data.SqlTypes;
using System.Xml.Linq;
using Microsoft.SqlServer.Server;

namespace SecureLibrary.SQL.Interfaces
{
    /// <summary>
    /// Interface for SQL type to XML conversion operations
    /// Provides robust handling of all SQL CLR types and nulls with round-trip capability
    /// </summary>
    public interface ISqlXmlConverter
    {
        /// <summary>
        /// Converts a SqlDataRecord to XML representation
        /// </summary>
        /// <param name="record">SqlDataRecord to convert</param>
        /// <returns>XDocument containing the record data</returns>
        /// <exception cref="ArgumentNullException">Thrown when record is null</exception>
        XDocument ToXml(SqlDataRecord record);

        /// <summary>
        /// Converts XML back to a SqlDataRecord with proper type casting
        /// </summary>
        /// <param name="xml">XDocument containing the record data</param>
        /// <param name="metadata">SqlMetaData array defining the expected schema</param>
        /// <returns>SqlDataRecord with properly typed values</returns>
        /// <exception cref="ArgumentNullException">Thrown when xml or metadata is null</exception>
        SqlDataRecord FromXml(XDocument xml, SqlMetaData[] metadata);

        /// <summary>
        /// Converts a DataRow to XML representation
        /// </summary>
        /// <param name="row">DataRow to convert</param>
        /// <returns>XDocument containing the row data</returns>
        /// <exception cref="ArgumentNullException">Thrown when row is null</exception>
        XDocument ToXml(DataRow row);

        /// <summary>
        /// Converts XML back to a DataRow with proper type casting
        /// </summary>
        /// <param name="xml">XDocument containing the row data</param>
        /// <param name="table">DataTable defining the expected schema</param>
        /// <returns>DataRow with properly typed values</returns>
        /// <exception cref="ArgumentNullException">Thrown when xml or table is null</exception>
        DataRow FromXml(XDocument xml, DataTable table);

        /// <summary>
        /// Converts a DataTable to XML representation with schema metadata
        /// </summary>
        /// <param name="table">DataTable to convert</param>
        /// <returns>XDocument containing the table data and schema</returns>
        /// <exception cref="ArgumentNullException">Thrown when table is null</exception>
        XDocument ToXml(DataTable table);

        /// <summary>
        /// Converts XML back to a DataTable with schema restoration
        /// </summary>
        /// <param name="xml">XDocument containing the table data and schema</param>
        /// <returns>DataTable with restored schema and data</returns>
        /// <exception cref="ArgumentNullException">Thrown when xml is null</exception>
        DataTable FromXml(XDocument xml);

        /// <summary>
        /// Validates that a value can be safely converted to the specified SQL type
        /// </summary>
        /// <param name="value">Value to validate</param>
        /// <param name="sqlType">Target SQL type</param>
        /// <returns>True if the value can be converted, false otherwise</returns>
        bool CanConvertToSqlType(object value, SqlDbType sqlType);

        /// <summary>
        /// Gets the default value for a SQL type
        /// </summary>
        /// <param name="sqlType">SQL type to get default value for</param>
        /// <returns>Default value for the SQL type</returns>
        object GetDefaultValue(SqlDbType sqlType);

        /// <summary>
        /// Converts a .NET value to the appropriate SQL type
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <param name="sqlType">Target SQL type</param>
        /// <returns>Converted value</returns>
        /// <exception cref="InvalidCastException">Thrown when conversion is not possible</exception>
        object ConvertToSqlType(object value, SqlDbType sqlType);

        /// <summary>
        /// Converts a SQL type value to .NET type
        /// </summary>
        /// <param name="value">SQL type value to convert</param>
        /// <param name="targetType">Target .NET type</param>
        /// <returns>Converted value</returns>
        /// <exception cref="InvalidCastException">Thrown when conversion is not possible</exception>
        object ConvertFromSqlType(object value, Type targetType);

        /// <summary>
        /// Gets the list of supported SQL types
        /// </summary>
        /// <returns>Array of supported SqlDbType values</returns>
        SqlDbType[] GetSupportedSqlTypes();

        /// <summary>
        /// Validates XML structure for SQL type conversion
        /// </summary>
        /// <param name="xml">XML to validate</param>
        /// <returns>Validation result with any error messages</returns>
        ValidationResult ValidateXmlStructure(XDocument xml);
    }
} 