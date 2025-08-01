using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Data;
using SecureLibrary.SQL.Interfaces;

namespace SecureLibrary.SQL.Services
{
    /// <summary>
    /// Unified XML Schema for encryption operations
    /// Maintains backward compatibility with existing single-row format
    /// Extends to support multi-row encryption
    /// </summary>
    public static class EncryptionXmlSchema
    {
        #region Constants

        // Current baseline format (existing xmlresult18.xml format)
        public const string SingleRowRootElementName = "EncryptedRow";
        public const string SchemaElementName = "Schema";
        public const string MetadataElementName = "Metadata";
        public const string EncryptedDataElementName = "EncryptedData";
        public const string ColumnElementName = "Column";

        // Multi-row extension format
        public const string MultiRowRootElementName = "EncryptedData";
        public const string BatchMetadataElementName = "BatchMetadata";
        public const string RowsElementName = "Rows";
        public const string RowElementName = "Row";
        public const string RowMetadataElementName = "RowMetadata";
        public const string SqlServerSchemaElementName = "SqlServerSchema";
        public const string EncryptedColumnsElementName = "EncryptedColumns";
        public const string EncryptedValueElementName = "EncryptedValue";

        // Metadata field names (shared between formats)
        public const string AlgorithmField = "Algorithm";
        public const string SaltField = "Salt";
        public const string NonceField = "Nonce";
        public const string IterationsField = "Iterations";
        public const string EncryptedAtField = "EncryptedAt";
        public const string FormatVersionField = "FormatVersion";
        public const string RowCountField = "RowCount";
        public const string DataTypeField = "DataType";
        public const string EncryptedDataField = "EncryptedData";

        // Column schema field names (shared between formats)
        public const string NameField = "Name";
        public const string TypeField = "Type";
        public const string SqlDbTypeField = "SqlDbType";
        public const string SqlTypeNameField = "SqlTypeName";
        public const string MaxLengthField = "MaxLength";
        public const string IsNullableField = "IsNullable";
        public const string OrdinalField = "Ordinal";

        #endregion

        #region Single Row Encryption Schema (Current Baseline)

        /// <summary>
        /// Creates XML for single row encryption (baseline format)
        /// Compatible with existing xmlresult18.xml format
        /// </summary>
        /// <param name="schema">DataTable schema</param>
        /// <param name="encryptedData">Base64 encoded encrypted data</param>
        /// <param name="metadata">Encryption metadata</param>
        /// <returns>XElement representing the encrypted row</returns>
        public static XElement CreateSingleRowXml(DataTable schema, string encryptedData, EncryptionMetadata metadata)
        {
            return new XElement(SingleRowRootElementName,
                CreateSchemaXml(schema),
                CreateMetadataXml(metadata),
                new XElement(EncryptedDataElementName, encryptedData)
            );
        }

        /// <summary>
        /// Parses single row encryption XML (baseline format)
        /// </summary>
        /// <param name="xmlElement">XML element containing encrypted row</param>
        /// <param name="password">Password for key derivation</param>
        /// <returns>Tuple containing schema, encrypted data, and metadata</returns>
        public static (DataTable Schema, string EncryptedData, EncryptionMetadata Metadata) ParseSingleRowXml(XElement xmlElement, string password)
        {
            var schemaElement = xmlElement.Element(SchemaElementName);
            var metadataElement = xmlElement.Element(MetadataElementName);
            var encryptedDataElement = xmlElement.Element(EncryptedDataElementName);

            if (schemaElement == null || metadataElement == null || encryptedDataElement == null)
                throw new ArgumentException("Invalid single row encryption XML structure");

            var schema = ParseSchemaXml(schemaElement);
            var metadata = ParseMetadataXml(metadataElement, password);
            var encryptedData = encryptedDataElement.Value;

            return (schema, encryptedData, metadata);
        }

        /// <summary>
        /// Creates schema XML (baseline format)
        /// </summary>
        /// <param name="schema">DataTable schema</param>
        /// <returns>XElement representing schema</returns>
        public static XElement CreateSchemaXml(DataTable schema)
        {
            return new XElement(SchemaElementName,
                schema.Columns.Cast<DataColumn>().Select(col => new XElement(ColumnElementName,
                    new XAttribute(NameField, col.ColumnName),
                    new XAttribute(TypeField, col.DataType.Name),
                    new XAttribute(SqlDbTypeField, GetSqlDbType(col.DataType).ToString()),
                    new XAttribute(SqlTypeNameField, GetSqlTypeName(col.DataType)),
                    new XAttribute(MaxLengthField, col.MaxLength),
                    new XAttribute(IsNullableField, col.AllowDBNull),
                    new XAttribute(OrdinalField, col.Ordinal)
                )).ToArray()
            );
        }

        /// <summary>
        /// Parses schema XML (baseline format)
        /// </summary>
        /// <param name="xmlElement">XML element containing schema</param>
        /// <returns>DataTable with schema</returns>
        public static DataTable ParseSchemaXml(XElement xmlElement)
        {
            var schema = new DataTable();
            foreach (var columnElement in xmlElement.Elements(ColumnElementName))
            {
                var name = columnElement.Attribute(NameField)?.Value ?? "";
                var typeName = columnElement.Attribute(TypeField)?.Value ?? "String";
                var maxLength = int.Parse(columnElement.Attribute(MaxLengthField)?.Value ?? "-1");
                var isNullable = bool.Parse(columnElement.Attribute(IsNullableField)?.Value ?? "true");

                var dataType = GetClrType(typeName);
                var column = new DataColumn(name, dataType)
                {
                    MaxLength = maxLength,
                    AllowDBNull = isNullable
                };
                schema.Columns.Add(column);
            }
            return schema;
        }

        #endregion

        #region Multi-Row Encryption Schema (Extension)

        /// <summary>
        /// Creates XML for multi-row encryption (extension format)
        /// </summary>
        /// <param name="batchMetadata">Batch-level encryption metadata</param>
        /// <param name="encryptedRows">List of encrypted row data</param>
        /// <param name="xmlConverter">XML converter for schema serialization</param>
        /// <returns>XElement representing the encrypted multi-row data</returns>
        public static XElement CreateMultiRowXml(EncryptionMetadata batchMetadata, List<EncryptedRowData> encryptedRows, ISqlXmlConverter xmlConverter)
        {
            return new XElement(MultiRowRootElementName,
                CreateBatchMetadataXml(batchMetadata, encryptedRows.Count),
                new XElement(RowsElementName,
                    encryptedRows.Select(er => CreateRowXml(er, xmlConverter)).ToArray()
                )
            );
        }

        /// <summary>
        /// Creates batch metadata XML
        /// </summary>
        /// <param name="metadata">Batch metadata</param>
        /// <param name="rowCount">Number of rows</param>
        /// <returns>XElement representing batch metadata</returns>
        public static XElement CreateBatchMetadataXml(EncryptionMetadata metadata, int rowCount)
        {
            return new XElement(BatchMetadataElementName,
                new XElement(AlgorithmField, metadata.Algorithm),
                new XElement(SaltField, Convert.ToBase64String(metadata.Salt)),
                new XElement(IterationsField, metadata.Iterations),
                new XElement(RowCountField, rowCount)
            );
        }

        /// <summary>
        /// Creates XML for a single encrypted row (multi-row format)
        /// </summary>
        /// <param name="encryptedRow">Encrypted row data</param>
        /// <param name="xmlConverter">XML converter for schema serialization</param>
        /// <returns>XElement representing the encrypted row</returns>
        public static XElement CreateRowXml(EncryptedRowData encryptedRow, ISqlXmlConverter xmlConverter)
        {
            return new XElement(RowElementName,
                CreateRowMetadataXml(encryptedRow.Metadata, encryptedRow.EncryptedAt, encryptedRow.FormatVersion),
                new XElement(SchemaElementName, xmlConverter.ToXml(encryptedRow.Schema).ToString()),
                CreateSqlServerSchemaXml(encryptedRow.SqlServerSchema),
                CreateEncryptedColumnsXml(encryptedRow.EncryptedColumns)
            );
        }

        /// <summary>
        /// Creates row metadata XML
        /// </summary>
        /// <param name="metadata">Row metadata</param>
        /// <param name="encryptedAt">Encryption timestamp</param>
        /// <param name="formatVersion">Format version</param>
        /// <returns>XElement representing row metadata</returns>
        public static XElement CreateRowMetadataXml(EncryptionMetadata metadata, DateTime encryptedAt, int formatVersion)
        {
            return new XElement(RowMetadataElementName,
                new XElement(NonceField, Convert.ToBase64String(metadata.Nonce)),
                new XElement(EncryptedAtField, encryptedAt.ToString("O")),
                new XElement(FormatVersionField, formatVersion)
            );
        }

        /// <summary>
        /// Creates SQL Server schema XML
        /// </summary>
        /// <param name="sqlServerSchema">SQL Server column schema</param>
        /// <returns>XElement representing SQL Server schema</returns>
        public static XElement CreateSqlServerSchemaXml(List<SqlServerColumnSchema> sqlServerSchema)
        {
            return new XElement(SqlServerSchemaElementName,
                sqlServerSchema.Select(ss => new XElement(ColumnElementName,
                    new XElement(NameField, ss.Name),
                    new XElement(SqlDbTypeField, ss.SqlDbType.ToString()),
                    new XElement(SqlTypeNameField, ss.SqlTypeName),
                    new XElement(MaxLengthField, ss.MaxLength),
                    new XElement(IsNullableField, ss.IsNullable)
                )).ToArray()
            );
        }

        /// <summary>
        /// Creates encrypted columns XML
        /// </summary>
        /// <param name="encryptedColumns">Dictionary of encrypted column data</param>
        /// <returns>XElement representing encrypted columns</returns>
        public static XElement CreateEncryptedColumnsXml(Dictionary<string, byte[]> encryptedColumns)
        {
            return new XElement(EncryptedColumnsElementName,
                encryptedColumns.Select(ec => new XElement(ColumnElementName,
                    new XElement(NameField, ec.Key),
                    new XElement(EncryptedDataField, Convert.ToBase64String(ec.Value))
                )).ToArray()
            );
        }

        /// <summary>
        /// Parses multi-row encryption XML
        /// </summary>
        /// <param name="xmlElement">XML element containing encrypted multi-row data</param>
        /// <param name="password">Password for key derivation</param>
        /// <param name="xmlConverter">XML converter for schema deserialization</param>
        /// <returns>Tuple containing batch metadata and list of encrypted rows</returns>
        public static (EncryptionMetadata BatchMetadata, List<EncryptedRowData> EncryptedRows) ParseMultiRowXml(XElement xmlElement, string password, ISqlXmlConverter xmlConverter)
        {
            var batchMetadataElement = xmlElement.Element(BatchMetadataElementName);
            var rowsElement = xmlElement.Element(RowsElementName);

            if (batchMetadataElement == null || rowsElement == null)
                throw new ArgumentException("Invalid multi-row encryption XML structure");

            var batchMetadata = ParseBatchMetadataXml(batchMetadataElement, password);
            var encryptedRows = new List<EncryptedRowData>();

            foreach (var rowElement in rowsElement.Elements(RowElementName))
            {
                var encryptedRow = ParseRowXml(rowElement, batchMetadata, xmlConverter);
                encryptedRows.Add(encryptedRow);
            }

            return (batchMetadata, encryptedRows);
        }

        /// <summary>
        /// Parses batch metadata XML
        /// </summary>
        /// <param name="xmlElement">XML element containing batch metadata</param>
        /// <param name="password">Password for key derivation</param>
        /// <returns>EncryptionMetadata object</returns>
        public static EncryptionMetadata ParseBatchMetadataXml(XElement xmlElement, string password)
        {
            return new EncryptionMetadata
            {
                Algorithm = xmlElement.Element(AlgorithmField)?.Value ?? "AES-GCM",
                Key = password,
                Salt = Convert.FromBase64String(xmlElement.Element(SaltField)?.Value ?? ""),
                Iterations = int.Parse(xmlElement.Element(IterationsField)?.Value ?? "10000"),
                AutoGenerateNonce = false
            };
        }

        /// <summary>
        /// Parses row XML
        /// </summary>
        /// <param name="xmlElement">XML element containing encrypted row data</param>
        /// <param name="batchMetadata">Batch metadata for key derivation</param>
        /// <param name="xmlConverter">XML converter for schema deserialization</param>
        /// <returns>EncryptedRowData object</returns>
        public static EncryptedRowData ParseRowXml(XElement xmlElement, EncryptionMetadata batchMetadata, ISqlXmlConverter xmlConverter)
        {
            var rowMetadataElement = xmlElement.Element(RowMetadataElementName);
            var schemaElement = xmlElement.Element(SchemaElementName);
            var sqlServerSchemaElement = xmlElement.Element(SqlServerSchemaElementName);
            var encryptedColumnsElement = xmlElement.Element(EncryptedColumnsElementName);

            if (rowMetadataElement == null || schemaElement == null || sqlServerSchemaElement == null || encryptedColumnsElement == null)
                throw new ArgumentException("Invalid row encryption XML structure");

            var rowMetadata = ParseRowMetadataXml(rowMetadataElement, batchMetadata);
            var schema = xmlConverter.FromXml(XDocument.Parse(schemaElement.Value));
            var sqlServerSchema = ParseSqlServerSchemaXml(sqlServerSchemaElement);
            var encryptedColumns = ParseEncryptedColumnsXml(encryptedColumnsElement);
            var encryptedAt = DateTime.Parse(rowMetadataElement.Element(EncryptedAtField)?.Value ?? DateTime.UtcNow.ToString("O"));
            var formatVersion = int.Parse(rowMetadataElement.Element(FormatVersionField)?.Value ?? "1");

            return new EncryptedRowData
            {
                Metadata = rowMetadata,
                Schema = schema,
                SqlServerSchema = sqlServerSchema,
                EncryptedColumns = encryptedColumns,
                EncryptedAt = encryptedAt,
                FormatVersion = formatVersion
            };
        }

        /// <summary>
        /// Parses row metadata XML
        /// </summary>
        /// <param name="xmlElement">XML element containing row metadata</param>
        /// <param name="batchMetadata">Batch metadata for key derivation</param>
        /// <returns>EncryptionMetadata object</returns>
        public static EncryptionMetadata ParseRowMetadataXml(XElement xmlElement, EncryptionMetadata batchMetadata)
        {
            return new EncryptionMetadata
            {
                Algorithm = batchMetadata.Algorithm,
                Key = batchMetadata.Key,
                Salt = batchMetadata.Salt, // Shared salt
                Nonce = Convert.FromBase64String(xmlElement.Element(NonceField)?.Value ?? ""), // Individual nonce
                Iterations = batchMetadata.Iterations,
                AutoGenerateNonce = false
            };
        }

        /// <summary>
        /// Parses SQL Server schema XML
        /// </summary>
        /// <param name="xmlElement">XML element containing SQL Server schema</param>
        /// <returns>List of SqlServerColumnSchema objects</returns>
        public static List<SqlServerColumnSchema> ParseSqlServerSchemaXml(XElement xmlElement)
        {
            var schema = new List<SqlServerColumnSchema>();
            foreach (var columnElement in xmlElement.Elements(ColumnElementName))
            {
                schema.Add(new SqlServerColumnSchema
                {
                    Name = columnElement.Element(NameField)?.Value ?? "",
                    SqlDbType = (SqlDbType)Enum.Parse(typeof(SqlDbType), columnElement.Element(SqlDbTypeField)?.Value ?? "NVarChar"),
                    SqlTypeName = columnElement.Element(SqlTypeNameField)?.Value ?? "nvarchar",
                    MaxLength = int.Parse(columnElement.Element(MaxLengthField)?.Value ?? "-1"),
                    IsNullable = bool.Parse(columnElement.Element(IsNullableField)?.Value ?? "true")
                });
            }
            return schema;
        }

        /// <summary>
        /// Parses encrypted columns XML
        /// </summary>
        /// <param name="xmlElement">XML element containing encrypted columns</param>
        /// <returns>Dictionary of encrypted column data</returns>
        public static Dictionary<string, byte[]> ParseEncryptedColumnsXml(XElement xmlElement)
        {
            var columns = new Dictionary<string, byte[]>();
            foreach (var columnElement in xmlElement.Elements(ColumnElementName))
            {
                var name = columnElement.Element(NameField)?.Value ?? "";
                var encryptedData = Convert.FromBase64String(columnElement.Element(EncryptedDataField)?.Value ?? "");
                columns[name] = encryptedData;
            }
            return columns;
        }

        #endregion

        #region Single Value Encryption Schema

        /// <summary>
        /// Creates XML for single value encryption
        /// </summary>
        /// <param name="dataType">Data type of the encrypted value</param>
        /// <param name="encryptedData">Base64 encoded encrypted data</param>
        /// <param name="metadata">Encryption metadata</param>
        /// <returns>XElement representing the encrypted value</returns>
        public static XElement CreateSingleValueXml(string dataType, string encryptedData, EncryptionMetadata metadata)
        {
            return new XElement(EncryptedValueElementName,
                new XElement(DataTypeField, dataType),
                new XElement(EncryptedDataField, encryptedData),
                CreateMetadataXml(metadata)
            );
        }

        /// <summary>
        /// Parses single value encryption XML
        /// </summary>
        /// <param name="xmlElement">XML element containing encrypted value</param>
        /// <param name="password">Password for key derivation</param>
        /// <returns>Tuple containing data type, encrypted data, and metadata</returns>
        public static (string DataType, string EncryptedData, EncryptionMetadata Metadata) ParseSingleValueXml(XElement xmlElement, string password)
        {
            var dataType = xmlElement.Element(DataTypeField)?.Value;
            var encryptedData = xmlElement.Element(EncryptedDataField)?.Value;
            var metadataElement = xmlElement.Element(MetadataElementName);

            if (string.IsNullOrEmpty(dataType) || string.IsNullOrEmpty(encryptedData) || metadataElement == null)
                throw new ArgumentException("Invalid single value encryption XML structure");

            var metadata = ParseMetadataXml(metadataElement, password);

            return (dataType, encryptedData, metadata);
        }

        #endregion

        #region Metadata Schema (Shared)

        /// <summary>
        /// Creates metadata XML (shared between formats)
        /// </summary>
        /// <param name="metadata">Encryption metadata</param>
        /// <returns>XElement representing metadata</returns>
        public static XElement CreateMetadataXml(EncryptionMetadata metadata)
        {
            return new XElement(MetadataElementName,
                new XElement(AlgorithmField, metadata.Algorithm),
                new XElement(IterationsField, metadata.Iterations),
                new XElement(SaltField, Convert.ToBase64String(metadata.Salt)),
                new XElement(NonceField, Convert.ToBase64String(metadata.Nonce)),
                new XElement(EncryptedAtField, DateTime.UtcNow.ToString("O")),
                new XElement(FormatVersionField, 1)
            );
        }

        /// <summary>
        /// Parses metadata XML (shared between formats)
        /// </summary>
        /// <param name="xmlElement">XML element containing metadata</param>
        /// <param name="password">Password for key derivation</param>
        /// <returns>EncryptionMetadata object</returns>
        public static EncryptionMetadata ParseMetadataXml(XElement xmlElement, string password)
        {
            return new EncryptionMetadata
            {
                Algorithm = xmlElement.Element(AlgorithmField)?.Value ?? "AES-GCM",
                Key = password,
                Salt = Convert.FromBase64String(xmlElement.Element(SaltField)?.Value ?? ""),
                Nonce = Convert.FromBase64String(xmlElement.Element(NonceField)?.Value ?? ""),
                Iterations = int.Parse(xmlElement.Element(IterationsField)?.Value ?? "10000"),
                AutoGenerateNonce = false
            };
        }

        #endregion

        #region Format Detection and Validation

        /// <summary>
        /// Detects the XML format type
        /// </summary>
        /// <param name="xmlElement">XML element to analyze</param>
        /// <returns>Format type</returns>
        public static XmlFormatType DetectFormat(XElement xmlElement)
        {
            if (xmlElement?.Name.LocalName == SingleRowRootElementName)
                return XmlFormatType.SingleRow;
            else if (xmlElement?.Name.LocalName == MultiRowRootElementName)
                return XmlFormatType.MultiRow;
            else if (xmlElement?.Name.LocalName == EncryptedValueElementName)
                return XmlFormatType.SingleValue;
            else
                return XmlFormatType.Unknown;
        }

        /// <summary>
        /// Validates XML structure for single row encryption (baseline format)
        /// </summary>
        /// <param name="xmlElement">XML element to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool ValidateSingleRowXml(XElement xmlElement)
        {
            return xmlElement?.Name.LocalName == SingleRowRootElementName &&
                   xmlElement.Element(SchemaElementName) != null &&
                   xmlElement.Element(MetadataElementName) != null &&
                   xmlElement.Element(EncryptedDataElementName) != null;
        }

        /// <summary>
        /// Validates XML structure for multi-row encryption
        /// </summary>
        /// <param name="xmlElement">XML element to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool ValidateMultiRowXml(XElement xmlElement)
        {
            return xmlElement?.Name.LocalName == MultiRowRootElementName &&
                   xmlElement.Element(BatchMetadataElementName) != null &&
                   xmlElement.Element(RowsElementName) != null;
        }

        /// <summary>
        /// Validates XML structure for single value encryption
        /// </summary>
        /// <param name="xmlElement">XML element to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool ValidateSingleValueXml(XElement xmlElement)
        {
            return xmlElement?.Name.LocalName == EncryptedValueElementName &&
                   xmlElement.Element(DataTypeField) != null &&
                   xmlElement.Element(EncryptedDataField) != null &&
                   xmlElement.Element(MetadataElementName) != null;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets SQL DbType from CLR type
        /// </summary>
        /// <param name="clrType">CLR type</param>
        /// <returns>SQL DbType</returns>
        private static SqlDbType GetSqlDbType(Type clrType)
        {
            if (clrType == typeof(string)) return SqlDbType.NVarChar;
            if (clrType == typeof(int)) return SqlDbType.Int;
            if (clrType == typeof(DateTime)) return SqlDbType.DateTime;
            if (clrType == typeof(bool)) return SqlDbType.Bit;
            if (clrType == typeof(decimal)) return SqlDbType.Decimal;
            if (clrType == typeof(double)) return SqlDbType.Float;
            if (clrType == typeof(byte[])) return SqlDbType.VarBinary;
            if (clrType == typeof(Guid)) return SqlDbType.UniqueIdentifier;
            return SqlDbType.NVarChar; // Default
        }

        /// <summary>
        /// Gets SQL type name from CLR type
        /// </summary>
        /// <param name="clrType">CLR type</param>
        /// <returns>SQL type name</returns>
        private static string GetSqlTypeName(Type clrType)
        {
            if (clrType == typeof(string)) return "nvarchar";
            if (clrType == typeof(int)) return "int";
            if (clrType == typeof(DateTime)) return "datetime";
            if (clrType == typeof(bool)) return "bit";
            if (clrType == typeof(decimal)) return "decimal";
            if (clrType == typeof(double)) return "float";
            if (clrType == typeof(byte[])) return "varbinary";
            if (clrType == typeof(Guid)) return "uniqueidentifier";
            return "nvarchar"; // Default
        }

        /// <summary>
        /// Gets CLR type from type name
        /// </summary>
        /// <param name="typeName">Type name</param>
        /// <returns>CLR type</returns>
        private static Type GetClrType(string typeName)
        {
            switch (typeName.ToLower())
            {
                case "string": return typeof(string);
                case "int32": return typeof(int);
                case "datetime": return typeof(DateTime);
                case "boolean": return typeof(bool);
                case "decimal": return typeof(decimal);
                case "double": return typeof(double);
                case "byte[]": return typeof(byte[]);
                case "guid": return typeof(Guid);
                default: return typeof(string);
            }
        }

        #endregion
    }

    /// <summary>
    /// XML format types
    /// </summary>
    public enum XmlFormatType
    {
        Unknown,
        SingleRow,    // Current baseline format (xmlresult18.xml)
        MultiRow,     // Extension format for multiple rows
        SingleValue   // Single value format
    }
} 