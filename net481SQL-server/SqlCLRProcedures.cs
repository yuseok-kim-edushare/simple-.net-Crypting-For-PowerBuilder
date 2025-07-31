using System;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Security.Cryptography;
using System.Text;
using System.Security;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Data;
using System.Data.SqlClient;
using SecureLibrary.SQL.Services;
using SecureLibrary.SQL.Interfaces;

namespace SecureLibrary.SQL
{
    /// <summary>
    /// SQL Server CLR Stored Procedures for table-level cryptographic operations
    /// Provides T-SQL accessible procedures for encrypting and decrypting entire tables
    /// </summary>
    public class SqlCLRProcedures
    {
        private static readonly ICgnService _cgnService;
        private static readonly IEncryptionEngine _encryptionEngine;
        private static readonly ISqlXmlConverter _xmlConverter;
        private static readonly IPasswordHashingService _passwordHashingService;

        static SqlCLRProcedures()
        {
            try
            {
                _cgnService = new CgnService();
                _xmlConverter = new SqlXmlConverter();
                _encryptionEngine = new EncryptionEngine(_cgnService, _xmlConverter);
                _passwordHashingService = new BcryptPasswordHashingService();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize CLR services: {ex.Message}", ex);
            }
        }

        #region Enhanced Table Encryption Procedures

        /// <summary>
        /// Encrypts an entire table with metadata preservation using SQL Server native types
        /// </summary>
        /// <param name="tableName">Name of the table to encrypt</param>
        /// <param name="password">Password for key derivation</param>
        /// <param name="iterations">Number of iterations for key derivation</param>
        /// <param name="encryptedData">Output parameter containing encrypted table data</param>
        [SqlProcedure]
        [SecuritySafeCritical]
        public static void EncryptTableWithMetadata(
            SqlString tableName, 
            SqlString password, 
            SqlInt32 iterations, 
            out SqlString encryptedData)
        {
            encryptedData = SqlString.Null;

            if (tableName.IsNull || password.IsNull || iterations.IsNull)
                return;

            try
            {
                // Get the current connection context
                using (var connection = new SqlConnection("context connection=true"))
                {
                    connection.Open();

                    // Read the table data
                    var dataTable = ReadTableData(connection, tableName.Value);
                    
                    // Convert table to XML
                    var xmlDoc = _xmlConverter.ToXml(dataTable);
                    var xmlString = xmlDoc.ToString();

                    // Generate encryption metadata
                    var metadata = new EncryptionMetadata
                    {
                        Algorithm = "AES-GCM",
                        Key = password.Value,
                        Salt = _cgnService.GenerateNonce(32),
                        Iterations = iterations.Value,
                        AutoGenerateNonce = true
                    };

                    // Encrypt the XML data
                    var encryptedXmlBytes = _cgnService.EncryptAesGcm(
                        Encoding.UTF8.GetBytes(xmlString), 
                        _cgnService.DeriveKeyFromPassword(password.Value, metadata.Salt, iterations.Value, 32),
                        metadata.Nonce);

                    // Create encrypted data structure
                    var encryptedTableData = new EncryptedTableData
                    {
                        Schema = dataTable.Copy(),
                        Metadata = metadata,
                        EncryptedAt = DateTime.UtcNow,
                        FormatVersion = 1,
                        EncryptedXml = Convert.ToBase64String(encryptedXmlBytes)
                    };

                    // Serialize to JSON or XML for storage
                    var resultXml = new XElement("EncryptedTable",
                        new XElement("Schema",
                            encryptedTableData.Schema.Columns.Cast<DataColumn>().Select(col =>
                                new XElement("Column",
                                    new XAttribute("Name", col.ColumnName),
                                    new XAttribute("Type", col.DataType.Name),
                                    new XAttribute("MaxLength", col.MaxLength)
                                )
                            )
                        ),
                        new XElement("Metadata",
                            new XElement("Algorithm", metadata.Algorithm),
                            new XElement("Iterations", metadata.Iterations),
                            new XElement("Salt", Convert.ToBase64String(metadata.Salt)),
                            new XElement("Nonce", Convert.ToBase64String(metadata.Nonce)),
                            new XElement("EncryptedAt", encryptedTableData.EncryptedAt.ToString("O")),
                            new XElement("FormatVersion", encryptedTableData.FormatVersion)
                        ),
                        new XElement("EncryptedData", encryptedTableData.EncryptedXml)
                    );

                    encryptedData = new SqlString(resultXml.ToString());
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Table encryption failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Decrypts an encrypted table and restores it to a temporary table
        /// </summary>
        /// <param name="encryptedData">Encrypted table data</param>
        /// <param name="password">Password for key derivation</param>
        /// <param name="targetTableName">Name of the target table to create</param>
        [SqlProcedure]
        [SecuritySafeCritical]
        public static void DecryptTableWithMetadata(
            SqlString encryptedData, 
            SqlString password, 
            SqlString targetTableName)
        {
            if (encryptedData.IsNull || password.IsNull || targetTableName.IsNull)
                return;

            try
            {
                // Parse the encrypted data XML
                var xmlDoc = XDocument.Parse(encryptedData.Value);
                var schemaElement = xmlDoc.Root.Element("Schema");
                var metadataElement = xmlDoc.Root.Element("Metadata");
                var encryptedDataElement = xmlDoc.Root.Element("EncryptedData");

                if (schemaElement == null || metadataElement == null || encryptedDataElement == null)
                    throw new ArgumentException("Invalid encrypted data format");

                // Extract metadata
                var salt = Convert.FromBase64String(metadataElement.Element("Salt").Value);
                var nonce = Convert.FromBase64String(metadataElement.Element("Nonce").Value);
                var iterations = int.Parse(metadataElement.Element("Iterations").Value);
                var encryptedXml = encryptedDataElement.Value;

                // Decrypt the XML data
                var key = _cgnService.DeriveKeyFromPassword(password.Value, salt, iterations, 32);
                var decryptedXmlBytes = _cgnService.DecryptAesGcm(
                    Convert.FromBase64String(encryptedXml), key, nonce);
                var xmlString = Encoding.UTF8.GetString(decryptedXmlBytes);

                // Parse the decrypted XML
                var decryptedXmlDoc = XDocument.Parse(xmlString);
                var decryptedTable = _xmlConverter.FromXml(decryptedXmlDoc);

                // Create the target table and insert data
                using (var connection = new SqlConnection("context connection=true"))
                {
                    connection.Open();
                    CreateTableFromSchema(connection, targetTableName.Value, decryptedTable);
                    InsertTableData(connection, targetTableName.Value, decryptedTable);
                }

                // Clear sensitive data
                Array.Clear(key, 0, key.Length);
                Array.Clear(decryptedXmlBytes, 0, decryptedXmlBytes.Length);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Table decryption failed: {ex.Message}", ex);
            }
        }

        #endregion

        #region Enhanced Row-Level Encryption Procedures

        /// <summary>
        /// Encrypts a single row from FOR XML query output using SQL Server native types
        /// This procedure accepts XML generated by FOR XML RAW, ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE
        /// </summary>
        /// <param name="rowXml">Row data as XML from FOR XML query</param>
        /// <param name="password">Password for key derivation</param>
        /// <param name="iterations">Number of iterations for key derivation</param>
        /// <param name="encryptedRow">Output parameter containing encrypted row data</param>
        [SqlProcedure]
        [SecuritySafeCritical]
        public static void EncryptRowWithMetadata(
            SqlXml rowXml, 
            SqlString password, 
            SqlInt32 iterations, 
            out SqlString encryptedRow)
        {
            encryptedRow = SqlString.Null;

            if (rowXml.IsNull || password.IsNull || iterations.IsNull)
                return;

            try
            {
                // Parse the FOR XML output using the converter
                var dataRow = _xmlConverter.ParseForXmlRow(rowXml.Value);

                // Encrypt the row using the encryption engine
                var metadata = new EncryptionMetadata
                {
                    Algorithm = "AES-GCM",
                    Key = password.Value,
                    Salt = _cgnService.GenerateNonce(32),
                    Iterations = iterations.Value,
                    AutoGenerateNonce = true
                };

                var encryptedRowData = _encryptionEngine.EncryptRow(dataRow, metadata);

                // Serialize the encrypted row data with enhanced schema information
                var resultXml = new XElement("EncryptedRow",
                    new XElement("Schema",
                        encryptedRowData.SqlServerSchema.Select(col =>
                            new XElement("Column",
                                new XAttribute("Name", col.Name),
                                new XAttribute("Type", GetClrTypeName(col.SqlDbType)),
                                new XAttribute("SqlDbType", col.SqlDbType.ToString()),
                                new XAttribute("SqlTypeName", col.SqlTypeName),
                                new XAttribute("MaxLength", col.MaxLength),
                                new XAttribute("IsNullable", col.IsNullable),
                                new XAttribute("Ordinal", col.OrdinalPosition),
                                col.Precision.HasValue ? new XAttribute("Precision", col.Precision.Value) : null,
                                col.Scale.HasValue ? new XAttribute("Scale", col.Scale.Value) : null
                            )
                        )
                    ),
                    new XElement("Metadata",
                        new XElement("Algorithm", metadata.Algorithm),
                        new XElement("Iterations", metadata.Iterations),
                        new XElement("Salt", Convert.ToBase64String(metadata.Salt)),
                        new XElement("Nonce", Convert.ToBase64String(metadata.Nonce)),
                        new XElement("EncryptedAt", encryptedRowData.EncryptedAt.ToString("O")),
                        new XElement("FormatVersion", encryptedRowData.FormatVersion)
                    ),
                    new XElement("EncryptedData", 
                        Convert.ToBase64String(encryptedRowData.EncryptedColumns["RowData"]))
                );

                encryptedRow = new SqlString(resultXml.ToString());
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Row encryption failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Decrypts an encrypted row and returns it as a result set
        /// This procedure returns the decrypted row data directly
        /// </summary>
        /// <param name="encryptedRow">Encrypted row data</param>
        /// <param name="password">Password for key derivation</param>
        [SqlProcedure]
        [SecuritySafeCritical]
        public static void DecryptRowWithMetadata(
            SqlString encryptedRow, 
            SqlString password)
        {
            if (encryptedRow.IsNull || password.IsNull)
                return;

            try
            {
                // Parse the encrypted row XML
                var xmlDoc = XDocument.Parse(encryptedRow.Value);
                var schemaElement = xmlDoc.Root.Element("Schema");
                var metadataElement = xmlDoc.Root.Element("Metadata");
                var encryptedDataElement = xmlDoc.Root.Element("EncryptedData");

                if (schemaElement == null || metadataElement == null || encryptedDataElement == null)
                    throw new ArgumentException("Invalid encrypted row format");

                // Reconstruct the encrypted row data with enhanced schema
                var encryptedRowData = new EncryptedRowData
                {
                    Schema = ReconstructDataTableFromEnhancedSchema(schemaElement),
                    Metadata = new EncryptionMetadata
                    {
                        Algorithm = metadataElement.Element("Algorithm")?.Value ?? "AES-GCM",
                        Key = password.Value,
                        Salt = Convert.FromBase64String(metadataElement.Element("Salt").Value),
                        Nonce = Convert.FromBase64String(metadataElement.Element("Nonce").Value),
                        Iterations = int.Parse(metadataElement.Element("Iterations").Value),
                        AutoGenerateNonce = false
                    },
                    EncryptedAt = DateTime.Parse(metadataElement.Element("EncryptedAt").Value),
                    FormatVersion = int.Parse(metadataElement.Element("FormatVersion").Value),
                    EncryptedColumns = new Dictionary<string, byte[]>
                    {
                        ["RowData"] = Convert.FromBase64String(encryptedDataElement.Value)
                    }
                };

                // Reconstruct SQL Server schema information
                foreach (var columnElement in schemaElement.Elements("Column"))
                {
                    var sqlServerColumn = new SqlServerColumnSchema
                    {
                        Name = columnElement.Attribute("Name")?.Value,
                        SqlDbType = ParseSqlDbType(columnElement.Attribute("SqlDbType")?.Value),
                        SqlTypeName = columnElement.Attribute("SqlTypeName")?.Value,
                        MaxLength = int.TryParse(columnElement.Attribute("MaxLength")?.Value, out int maxLen) ? maxLen : -1,
                        IsNullable = columnElement.Attribute("IsNullable")?.Value == "true",
                        OrdinalPosition = int.TryParse(columnElement.Attribute("Ordinal")?.Value, out int ordinal) ? ordinal : 0
                    };

                    if (byte.TryParse(columnElement.Attribute("Precision")?.Value, out byte precision))
                        sqlServerColumn.Precision = precision;
                    if (byte.TryParse(columnElement.Attribute("Scale")?.Value, out byte scale))
                        sqlServerColumn.Scale = scale;

                    encryptedRowData.SqlServerSchema.Add(sqlServerColumn);
                }

                // Decrypt the row
                var decryptedRowData = _encryptionEngine.DecryptRow(encryptedRowData, encryptedRowData.Metadata);

                // Return the decrypted row as a result set using enhanced schema
                ReturnDecryptedRowAsResultSetWithEnhancedSchema(decryptedRowData, encryptedRowData.SqlServerSchema);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Row decryption failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Encrypts multiple rows from FOR XML query output in batch
        /// This procedure processes XML generated by FOR XML RAW, ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE
        /// </summary>
        /// <param name="rowsXml">Rows data as XML from FOR XML query</param>
        /// <param name="password">Password for key derivation</param>
        /// <param name="iterations">Number of iterations for key derivation</param>
        /// <param name="batchId">Batch identifier for grouping encrypted rows</param>
        [SqlProcedure]
        [SecuritySafeCritical]
        public static void EncryptRowsBatch(
            SqlXml rowsXml, 
            SqlString password, 
            SqlInt32 iterations, 
            SqlString batchId)
        {
            if (rowsXml.IsNull || password.IsNull || iterations.IsNull || batchId.IsNull)
                return;

            try
            {
                // Parse the FOR XML output using the converter
                var dataTable = _xmlConverter.ParseForXmlOutput(rowsXml.Value);

                // Encrypt each row
                foreach (DataRow dataRow in dataTable.Rows)
                {
                    var metadata = new EncryptionMetadata
                    {
                        Algorithm = "AES-GCM",
                        Key = password.Value,
                        Salt = _cgnService.GenerateNonce(32),
                        Iterations = iterations.Value,
                        AutoGenerateNonce = true
                    };

                    var encryptedRowData = _encryptionEngine.EncryptRow(dataRow, metadata);

                    // Store encrypted row in a temporary table or return as result set
                    StoreEncryptedRowInBatch(encryptedRowData, batchId.Value);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Batch row encryption failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Decrypts multiple encrypted rows and returns them as a result set
        /// This procedure can process a batch of encrypted rows
        /// </summary>
        /// <param name="batchId">Batch identifier for the encrypted rows</param>
        /// <param name="password">Password for key derivation</param>
        [SqlProcedure]
        [SecuritySafeCritical]
        public static void DecryptRowsBatch(
            SqlString batchId, 
            SqlString password)
        {
            if (batchId.IsNull || password.IsNull)
                return;

            try
            {
                // Retrieve encrypted rows for the batch
                var encryptedRows = RetrieveEncryptedRowsFromBatch(batchId.Value);

                // Decrypt each row and return as result set
                foreach (var encryptedRow in encryptedRows)
                {
                    var decryptedRow = _encryptionEngine.DecryptRow(encryptedRow, encryptedRow.Metadata);
                    ReturnDecryptedRowAsResultSet(decryptedRow);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Batch row decryption failed: {ex.Message}");
            }
        }

        #endregion

        #region FOR XML Parsing Methods (Simplified - Using SqlXmlConverter)

        /// <summary>
        /// Parses a single row from FOR XML RAW, ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE output
        /// Uses the centralized SqlXmlConverter for consistent parsing
        /// </summary>
        /// <param name="rowXml">XML containing a single row</param>
        /// <returns>DataRow with the parsed data</returns>
        private static DataRow ParseForXmlRow(SqlXml rowXml)
        {
            return _xmlConverter.ParseForXmlRow(rowXml.Value);
        }

        /// <summary>
        /// Parses multiple rows from FOR XML RAW, ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE output
        /// Uses the centralized SqlXmlConverter for consistent parsing
        /// </summary>
        /// <param name="rowsXml">XML containing multiple rows</param>
        /// <returns>DataTable with the parsed data</returns>
        private static DataTable ParseForXmlRows(SqlXml rowsXml)
        {
            return _xmlConverter.ParseForXmlOutput(rowsXml.Value);
        }

        #endregion

        #region Helper Methods

        private static void ReturnDecryptedRowAsResultSet(DataRow decryptedRow)
        {
            // Create SqlDataRecord with the same schema as the decrypted row
            var metadata = new List<SqlMetaData>();
            foreach (DataColumn column in decryptedRow.Table.Columns)
            {
                var sqlType = GetSqlTypeFromClrType(column.DataType);
                
                // Handle string types that require length specification
                if (sqlType == SqlDbType.NVarChar || sqlType == SqlDbType.VarChar || 
                    sqlType == SqlDbType.NChar || sqlType == SqlDbType.Char)
                {
                    var maxLength = column.MaxLength > 0 ? column.MaxLength : -1; // -1 for MAX
                    metadata.Add(new SqlMetaData(column.ColumnName, sqlType, maxLength));
                }
                else if (sqlType == SqlDbType.VarBinary || sqlType == SqlDbType.Binary)
                {
                    var maxLength = column.MaxLength > 0 ? column.MaxLength : -1; // -1 for MAX
                    metadata.Add(new SqlMetaData(column.ColumnName, sqlType, maxLength));
                }
                else if (sqlType == SqlDbType.Decimal)
                {
                    // For decimal types, use default precision and scale
                    metadata.Add(new SqlMetaData(column.ColumnName, sqlType, 18, 2));
                }
                else
                {
                    metadata.Add(new SqlMetaData(column.ColumnName, sqlType));
                }
            }

            var record = new SqlDataRecord(metadata.ToArray());

            // Set values in the record
            for (int i = 0; i < decryptedRow.Table.Columns.Count; i++)
            {
                var value = decryptedRow[i];
                if (value == DBNull.Value)
                {
                    record.SetDBNull(i);
                }
                else
                {
                    SetRecordValue(record, i, value, metadata[i].SqlDbType);
                }
            }

            // Send the record to the client
            SqlContext.Pipe.Send(record);
        }

        private static void StoreEncryptedRowInBatch(EncryptedRowData encryptedRow, string batchId)
        {
            // Store encrypted row in a temporary table or session state
            // This is a simplified implementation - in practice, you might want to use
            // a more sophisticated storage mechanism
            var encryptedData = Convert.ToBase64String(encryptedRow.EncryptedColumns["RowData"]);
            var metadata = CreateMetadataXml(encryptedRow.Metadata);

            // Store in session state or temporary table
            // For now, we'll just return it as a result set
            var record = new SqlDataRecord(
                new SqlMetaData("BatchId", SqlDbType.NVarChar, 50),
                new SqlMetaData("EncryptedData", SqlDbType.NVarChar, -1),
                new SqlMetaData("Metadata", SqlDbType.Xml)
            );

            record.SetString(0, batchId);
            record.SetString(1, encryptedData);
            record.SetSqlXml(2, new SqlXml(XDocument.Parse(metadata).CreateReader()));

            SqlContext.Pipe.Send(record);
        }

        private static List<EncryptedRowData> RetrieveEncryptedRowsFromBatch(string batchId)
        {
            // Retrieve encrypted rows for the batch
            // This is a simplified implementation
            var encryptedRows = new List<EncryptedRowData>();
            
            // In practice, you would query a temporary table or session state
            // to retrieve the encrypted rows for the given batch ID
            
            return encryptedRows;
        }

        private static string CreateMetadataXml(EncryptionMetadata metadata)
        {
            var xml = new XElement("Metadata",
                new XElement("Algorithm", metadata.Algorithm),
                new XElement("Iterations", metadata.Iterations),
                new XElement("Salt", Convert.ToBase64String(metadata.Salt)),
                new XElement("Nonce", Convert.ToBase64String(metadata.Nonce)),
                new XElement("EncryptedAt", DateTime.UtcNow.ToString("O")),
                new XElement("FormatVersion", 1)
            );

            return xml.ToString();
        }

        private static SqlDbType GetSqlTypeFromClrType(Type clrType)
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
            if (clrType == typeof(DateTimeOffset)) return SqlDbType.DateTimeOffset;
            if (clrType == typeof(TimeSpan)) return SqlDbType.Time;
            if (clrType == typeof(Guid)) return SqlDbType.UniqueIdentifier;
            if (clrType == typeof(byte[])) return SqlDbType.VarBinary;
            if (clrType == typeof(string)) return SqlDbType.NVarChar;
            
            return SqlDbType.NVarChar;
        }

        private static void SetRecordValue(SqlDataRecord record, int ordinal, object value, SqlDbType sqlType)
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

        private static DataTable ReadTableData(SqlConnection connection, string tableName)
        {
            var dataTable = new DataTable();
            
            using (var command = new SqlCommand($"SELECT * FROM {tableName}", connection))
            using (var reader = command.ExecuteReader())
            {
                dataTable.Load(reader);
            }

            return dataTable;
        }

        private static void CreateTableFromSchema(SqlConnection connection, string tableName, DataTable schema)
        {
            var createTableSql = $"CREATE TABLE {tableName} (";
            var columns = new List<string>();

            foreach (DataColumn column in schema.Columns)
            {
                var columnDef = $"{column.ColumnName} {GetSqlType(column.DataType)}";
                
                if (column.MaxLength > 0 && (column.DataType == typeof(string) || column.DataType == typeof(byte[])))
                {
                    columnDef += $"({column.MaxLength})";
                }
                
                if (!column.AllowDBNull)
                {
                    columnDef += " NOT NULL";
                }
                
                columns.Add(columnDef);
            }

            createTableSql += string.Join(", ", columns) + ")";

            using (var command = new SqlCommand(createTableSql, connection))
            {
                command.ExecuteNonQuery();
            }
        }

        private static void InsertTableData(SqlConnection connection, string tableName, DataTable data)
        {
            if (data.Rows.Count == 0) return;

            var columns = string.Join(", ", data.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
            var placeholders = string.Join(", ", data.Columns.Cast<DataColumn>().Select(c => "@" + c.ColumnName));
            var insertSql = $"INSERT INTO {tableName} ({columns}) VALUES ({placeholders})";

            using (var command = new SqlCommand(insertSql, connection))
            {
                // Add parameters
                foreach (DataColumn column in data.Columns)
                {
                    command.Parameters.Add("@" + column.ColumnName, GetSqlDbType(column.DataType));
                }

                // Insert each row
                foreach (DataRow row in data.Rows)
                {
                    for (int i = 0; i < data.Columns.Count; i++)
                    {
                        command.Parameters[i].Value = row[i] ?? DBNull.Value;
                    }
                    command.ExecuteNonQuery();
                }
            }
        }

        private static DataTable ReconstructDataTable(XElement schemaElement)
        {
            var table = new DataTable();
            
            foreach (var columnElement in schemaElement.Elements("Column"))
            {
                var name = columnElement.Attribute("Name")?.Value;
                var typeName = columnElement.Attribute("Type")?.Value;
                var maxLength = columnElement.Attribute("MaxLength")?.Value;

                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(typeName))
                {
                    // Handle type reconstruction properly
                    Type dataType = GetTypeFromName(typeName);
                    var column = new DataColumn(name, dataType);

                    if (!string.IsNullOrEmpty(maxLength) && int.TryParse(maxLength, out int maxLen))
                        column.MaxLength = maxLen;

                    table.Columns.Add(column);
                }
            }

            return table;
        }

        private static DataTable ReconstructDataTableFromEnhancedSchema(XElement schemaElement)
        {
            var table = new DataTable();
            
            foreach (var columnElement in schemaElement.Elements("Column"))
            {
                var name = columnElement.Attribute("Name")?.Value;
                var typeName = columnElement.Attribute("Type")?.Value;
                var maxLength = columnElement.Attribute("MaxLength")?.Value;

                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(typeName))
                {
                    // Handle type reconstruction properly
                    Type dataType = GetTypeFromName(typeName);
                    var column = new DataColumn(name, dataType);

                    if (!string.IsNullOrEmpty(maxLength) && int.TryParse(maxLength, out int maxLen))
                        column.MaxLength = maxLen;

                    table.Columns.Add(column);
                }
            }

            return table;
        }

        private static Type GetTypeFromName(string typeName)
        {
            // Handle common type names that might not have full assembly qualification
            switch (typeName)
            {
                case "Int32":
                case "int":
                    return typeof(int);
                case "Int64":
                case "long":
                    return typeof(long);
                case "Int16":
                case "short":
                    return typeof(short);
                case "Byte":
                case "byte":
                    return typeof(byte);
                case "Decimal":
                case "decimal":
                    return typeof(decimal);
                case "Double":
                case "double":
                    return typeof(double);
                case "Single":
                case "float":
                    return typeof(float);
                case "Boolean":
                case "bool":
                    return typeof(bool);
                case "DateTime":
                    return typeof(DateTime);
                case "DateTimeOffset":
                    return typeof(DateTimeOffset);
                case "TimeSpan":
                    return typeof(TimeSpan);
                case "Guid":
                    return typeof(Guid);
                case "Byte[]":
                case "byte[]":
                    return typeof(byte[]);
                case "String":
                case "string":
                    return typeof(string);
                default:
                    // Try to get the type using Type.GetType
                    var dataType = Type.GetType(typeName);
                    if (dataType != null)
                        return dataType;
                    
                    // If all else fails, default to string
                    return typeof(string);
            }
        }

        private static string GetClrTypeName(SqlDbType sqlDbType)
        {
            return SecureLibrary.SQL.Services.SqlTypeConversionHelper.GetClrTypeName(sqlDbType);
        }

        private static SqlDbType ParseSqlDbType(string sqlDbTypeString)
        {
            return SecureLibrary.SQL.Services.SqlTypeConversionHelper.ParseSqlDbType(sqlDbTypeString);
        }

        private static string GetSqlType(Type dataType)
        {
            if (dataType == typeof(int)) return "INT";
            if (dataType == typeof(long)) return "BIGINT";
            if (dataType == typeof(short)) return "SMALLINT";
            if (dataType == typeof(byte)) return "TINYINT";
            if (dataType == typeof(decimal)) return "DECIMAL(18,2)";
            if (dataType == typeof(double)) return "FLOAT";
            if (dataType == typeof(float)) return "REAL";
            if (dataType == typeof(bool)) return "BIT";
            if (dataType == typeof(DateTime)) return "DATETIME2";
            if (dataType == typeof(DateTimeOffset)) return "DATETIMEOFFSET";
            if (dataType == typeof(TimeSpan)) return "TIME";
            if (dataType == typeof(Guid)) return "UNIQUEIDENTIFIER";
            if (dataType == typeof(byte[])) return "VARBINARY(MAX)";
            if (dataType == typeof(string)) return "NVARCHAR(MAX)";
            
            return "NVARCHAR(MAX)";
        }

        private static SqlDbType GetSqlDbType(Type dataType)
        {
            if (dataType == typeof(int)) return SqlDbType.Int;
            if (dataType == typeof(long)) return SqlDbType.BigInt;
            if (dataType == typeof(short)) return SqlDbType.SmallInt;
            if (dataType == typeof(byte)) return SqlDbType.TinyInt;
            if (dataType == typeof(decimal)) return SqlDbType.Decimal;
            if (dataType == typeof(double)) return SqlDbType.Float;
            if (dataType == typeof(float)) return SqlDbType.Real;
            if (dataType == typeof(bool)) return SqlDbType.Bit;
            if (dataType == typeof(DateTime)) return SqlDbType.DateTime2;
            if (dataType == typeof(DateTimeOffset)) return SqlDbType.DateTimeOffset;
            if (dataType == typeof(TimeSpan)) return SqlDbType.Time;
            if (dataType == typeof(Guid)) return SqlDbType.UniqueIdentifier;
            if (dataType == typeof(byte[])) return SqlDbType.VarBinary;
            if (dataType == typeof(string)) return SqlDbType.NVarChar;
            
            return SqlDbType.NVarChar;
        }

        private static void ReturnDecryptedRowAsResultSetWithEnhancedSchema(DataRow decryptedRow, List<SqlServerColumnSchema> schema)
        {
            // Create SqlDataRecord with the enhanced schema
            var metadata = new List<SqlMetaData>();
            foreach (var sqlServerColumn in schema)
            {
                var sqlType = sqlServerColumn.SqlDbType;
                
                // Handle string types that require length specification
                if (sqlType == SqlDbType.NVarChar || sqlType == SqlDbType.VarChar || 
                    sqlType == SqlDbType.NChar || sqlType == SqlDbType.Char)
                {
                    var maxLength = sqlServerColumn.MaxLength > 0 ? sqlServerColumn.MaxLength : -1; // -1 for MAX
                    metadata.Add(new SqlMetaData(sqlServerColumn.Name, sqlType, maxLength));
                }
                else if (sqlType == SqlDbType.VarBinary || sqlType == SqlDbType.Binary)
                {
                    var maxLength = sqlServerColumn.MaxLength > 0 ? sqlServerColumn.MaxLength : -1; // -1 for MAX
                    metadata.Add(new SqlMetaData(sqlServerColumn.Name, sqlType, maxLength));
                }
                else if (sqlType == SqlDbType.Decimal)
                {
                    // For decimal types, use precision and scale from schema
                    var precision = sqlServerColumn.Precision ?? 18;
                    var scale = sqlServerColumn.Scale ?? 2;
                    metadata.Add(new SqlMetaData(sqlServerColumn.Name, sqlType, precision, scale));
                }
                else
                {
                    metadata.Add(new SqlMetaData(sqlServerColumn.Name, sqlType));
                }
            }

            var record = new SqlDataRecord(metadata.ToArray());

            // Set values in the record
            for (int i = 0; i < decryptedRow.Table.Columns.Count; i++)
            {
                var value = decryptedRow[i];
                if (value == DBNull.Value)
                {
                    record.SetDBNull(i);
                }
                else
                {
                    SetRecordValue(record, i, value, metadata[i].SqlDbType);
                }
            }

            // Send the record to the client
            SqlContext.Pipe.Send(record);
        }

        #endregion
    }

    /// <summary>
    /// Data structure for encrypted table data
    /// </summary>
    public class EncryptedTableData
    {
        public DataTable Schema { get; set; }
        public EncryptionMetadata Metadata { get; set; }
        public DateTime EncryptedAt { get; set; }
        public int FormatVersion { get; set; }
        public string EncryptedXml { get; set; }
    }
} 