using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace SecureLibrary.SQL
{
    /// <summary>
    /// Handles XML metadata operations for table encryption and decryption
    /// </summary>
    public static class XmlMetadataHandler
    {
        /// <summary>
        /// Helper method to build metadata-enhanced XML by querying table schema information
        /// </summary>
        public static string BuildMetadataEnhancedXml(string tableName)
        {
            try
            {
                // Parse schema and table name
                string schemaName = "dbo";
                string tableNameOnly = tableName;
                
                if (tableName.Contains("."))
                {
                    var parts = tableName.Split('.');
                    if (parts.Length == 2)
                    {
                        schemaName = parts[0];
                        tableNameOnly = parts[1];
                    }
                }

                var result = new StringBuilder();
                result.AppendLine("<Root>");
                
                // Add metadata section
                result.AppendLine("  <Metadata>");
                result.AppendLine($"    <Schema>{schemaName}</Schema>");
                result.AppendLine($"    <Table>{tableNameOnly}</Table>");
                result.AppendLine("    <Columns>");

                using (var connection = new System.Data.SqlClient.SqlConnection("context connection=true"))
                {
                    connection.Open();
                    
                    // Get schema information
                    string schemaQuery = $@"
                        SELECT 
                            COLUMN_NAME,
                            DATA_TYPE,
                            IS_NULLABLE,
                            CHARACTER_MAXIMUM_LENGTH,
                            NUMERIC_PRECISION,
                            NUMERIC_SCALE,
                            ORDINAL_POSITION
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_SCHEMA = '{schemaName}' AND TABLE_NAME = '{tableNameOnly}'
                        ORDER BY ORDINAL_POSITION";

                    using (var schemaCmd = new System.Data.SqlClient.SqlCommand(schemaQuery, connection))
                    using (var reader = schemaCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.AppendLine($"      <Column name=\"{reader["COLUMN_NAME"]}\" type=\"{reader["DATA_TYPE"]}\" nullable=\"{reader["IS_NULLABLE"]}\"");
                            
                            if (!reader.IsDBNull(reader.GetOrdinal("CHARACTER_MAXIMUM_LENGTH")))
                                result.Append($" maxLength=\"{reader["CHARACTER_MAXIMUM_LENGTH"]}\"");
                            
                            if (!reader.IsDBNull(reader.GetOrdinal("NUMERIC_PRECISION")))
                                result.Append($" precision=\"{reader["NUMERIC_PRECISION"]}\"");
                            
                            if (!reader.IsDBNull(reader.GetOrdinal("NUMERIC_SCALE")))
                                result.Append($" scale=\"{reader["NUMERIC_SCALE"]}\"");
                            
                            result.AppendLine(" />");
                        }
                    }

                    result.AppendLine("    </Columns>");
                    result.AppendLine("  </Metadata>");

                    // Get data
                    string dataQuery = $"SELECT * FROM [{schemaName}].[{tableNameOnly}] FOR XML PATH('Row'), ROOT('Data')";
                    using (var dataCmd = new System.Data.SqlClient.SqlCommand(dataQuery, connection))
                    {
                        var dataXml = dataCmd.ExecuteScalar() as string;
                        if (!string.IsNullOrEmpty(dataXml))
                        {
                            // Extract just the Row elements from the Data root
                            var doc = XDocument.Parse(dataXml);
                            foreach (var row in doc.Root.Elements("Row"))
                            {
                                result.AppendLine("  " + row.ToString());
                            }
                        }
                    }
                }

                result.AppendLine("</Root>");
                return result.ToString();
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Helper method to build metadata-enhanced XML from existing XML data by inferring types
        /// </summary>
        public static string BuildMetadataEnhancedXmlFromData(string xmlData)
        {
            try
            {
                var doc = XDocument.Parse(xmlData);
                var result = new StringBuilder();
                
                result.AppendLine("<Root>");
                result.AppendLine("  <Metadata>");
                result.AppendLine("    <Schema>dbo</Schema>");
                result.AppendLine("    <Table>InferredFromData</Table>");
                result.AppendLine("    <Columns>");

                // Get first row to infer schema
                var firstRow = doc.Root.Elements("Row").FirstOrDefault();
                if (firstRow != null)
                {
                    foreach (var attr in firstRow.Attributes())
                    {
                        string inferredType = XmlUtilities.InferDataType(attr.Value);
                        result.AppendLine($"      <Column name=\"{attr.Name}\" type=\"{inferredType}\" nullable=\"true\" />");
                    }
                }

                result.AppendLine("    </Columns>");
                result.AppendLine("  </Metadata>");

                // Add all data rows
                foreach (var row in doc.Root.Elements("Row"))
                {
                    result.AppendLine("  " + row.ToString());
                }

                result.AppendLine("</Root>");
                return result.ToString();
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Builds a column expression with automatic type casting based on column metadata
        /// Uses the existing SqlTypeMapping utility to avoid code duplication
        /// </summary>
        public static string BuildColumnExpression(ColumnInfo column)
        {
            string columnName = column.Name ?? "Column";
            string rawValue = $"T.c.value('@{columnName}', 'NVARCHAR(MAX)')";
            
            // Use the existing SqlTypeMapping utility to determine the SQL type
            var metaData = SqlTypeMapping.ToMetaData(column);
            string sqlType = SqlTypeMapping.GetSqlTypeString(metaData.SqlDbType, metaData.Precision, metaData.Scale, metaData.MaxLength);
            
            return $"CAST({rawValue} AS {sqlType}) AS [{columnName}]";
        }
    }
} 