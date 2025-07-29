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
                    
                    // First, check if table exists
                    string tableExistsQuery = $@"
                        SELECT COUNT(*) 
                        FROM INFORMATION_SCHEMA.TABLES 
                        WHERE TABLE_SCHEMA = '{schemaName}' AND TABLE_NAME = '{tableNameOnly}'";
                    
                    using (var existsCmd = new System.Data.SqlClient.SqlCommand(tableExistsQuery, connection))
                    {
                        int tableExists = (int)existsCmd.ExecuteScalar();
                        if (tableExists == 0)
                        {
                            throw new Exception($"Table [{schemaName}].[{tableNameOnly}] does not exist.");
                        }
                    }
                    
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
                        bool hasColumns = false;
                        while (reader.Read())
                        {
                            hasColumns = true;
                            result.AppendLine($"      <Column name=\"{reader["COLUMN_NAME"]}\" type=\"{reader["DATA_TYPE"]}\" nullable=\"{reader["IS_NULLABLE"]}\"");
                            
                            if (!reader.IsDBNull(reader.GetOrdinal("CHARACTER_MAXIMUM_LENGTH")))
                                result.Append($" maxLength=\"{reader["CHARACTER_MAXIMUM_LENGTH"]}\"");
                            
                            if (!reader.IsDBNull(reader.GetOrdinal("NUMERIC_PRECISION")))
                                result.Append($" precision=\"{reader["NUMERIC_PRECISION"]}\"");
                            
                            if (!reader.IsDBNull(reader.GetOrdinal("NUMERIC_SCALE")))
                                result.Append($" scale=\"{reader["NUMERIC_SCALE"]}\"");
                            
                            result.AppendLine(" />");
                        }
                        
                        if (!hasColumns)
                        {
                            throw new Exception($"Table [{schemaName}].[{tableNameOnly}] has no columns.");
                        }
                    }

                    result.AppendLine("    </Columns>");
                    result.AppendLine("  </Metadata>");

                    // Get data - FIXED: Preserve original XML structure
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
                                // FIXED: Preserve original XML structure without transformation
                                result.AppendLine("  " + row.ToString(SaveOptions.DisableFormatting));
                            }
                        }
                        else
                        {
                            // Table exists but has no data - add empty row for structure
                            result.AppendLine("  <!-- Table has no data -->");
                        }
                    }
                }

                result.AppendLine("</Root>");
                return result.ToString();
            }
            catch (Exception ex)
            {
                // Return a more descriptive error instead of null
                return $"<Root><Error>Failed to build metadata: {ex.Message}</Error></Root>";
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

                // Add all data rows - FIXED: Preserve original XML structure
                foreach (var row in doc.Root.Elements("Row"))
                {
                    result.AppendLine("  " + row.ToString(SaveOptions.DisableFormatting));
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
        /// Universal column expression builder that handles both attribute and element-based XML
        /// </summary>
        public static string BuildColumnExpression(ColumnInfo column)
        {
            string columnName = column.Name ?? "Column";
            
            // FIXED: Universal approach - try both attribute and element
            string rawValue = $@"
        COALESCE(
            T.c.value('@{columnName}', 'NVARCHAR(MAX)'),
            T.c.value('{columnName}[1]', 'NVARCHAR(MAX)')
        )";
            
            // Use the existing SqlTypeMapping utility to determine the SQL type
            var metaData = SqlTypeMapping.ToMetaData(column);
            string sqlType = SqlTypeMapping.GetSqlTypeString(metaData.SqlDbType, metaData.Precision, metaData.Scale, metaData.MaxLength);
            
            return $"CAST({rawValue} AS {sqlType}) AS [{columnName}]";
        }

        /// <summary>
        /// Universal XML parsing that handles both attribute and element-based structures
        /// </summary>
        public static List<ColumnInfo> ParseColumnsFromXml(XElement rootElement)
        {
            var columns = new List<ColumnInfo>();

            // Try to read embedded metadata first
            var metadataElement = rootElement.Element("Metadata");
            if (metadataElement != null)
            {
                // Parse embedded schema metadata
                var columnsElement = metadataElement.Element("Columns");
                if (columnsElement != null)
                {
                    columns = columnsElement.Elements("Column")
                        .Select(x => new ColumnInfo {
                            Name = (string)x.Attribute("name") ?? "Column",
                            TypeName = (string)x.Attribute("type") ?? "nvarchar",
                            MaxLength = XmlUtilities.GetIntAttribute(x, "maxLength"),
                            Precision = XmlUtilities.GetByteAttribute(x, "precision"),
                            Scale = XmlUtilities.GetByteAttribute(x, "scale"),
                            IsNullable = (bool?)x.Attribute("nullable") ?? true
                        })
                        .ToList();
                }
            }

            // Fallback: Infer schema from first data row if no metadata
            if (columns.Count == 0)
            {
                var firstRow = rootElement.Elements("Row").FirstOrDefault();
                if (firstRow != null)
                {
                    // FIXED: Universal parsing - try both attributes and elements
                    var attributes = firstRow.Attributes().ToList();
                    var elements = firstRow.Elements().ToList();

                    if (attributes.Count > 0)
                    {
                        // Attribute-based XML
                        columns = attributes
                            .Select(attr => new ColumnInfo {
                                Name = attr.Name.LocalName,
                                TypeName = XmlUtilities.InferDataType(attr.Value),
                                MaxLength = null,
                                Precision = null,
                                Scale = null,
                                IsNullable = true
                            })
                            .ToList();
                    }
                    else if (elements.Count > 0)
                    {
                        // Element-based XML
                        columns = elements
                            .Select(elem => new ColumnInfo {
                                Name = elem.Name.LocalName,
                                TypeName = XmlUtilities.InferDataType(elem.Value),
                                MaxLength = null,
                                Precision = null,
                                Scale = null,
                                IsNullable = true
                            })
                            .ToList();
                    }
                }
            }

            return columns;
        }

        /// <summary>
        /// Validates XML structure and provides detailed error information
        /// </summary>
        public static (bool isValid, string errorMessage) ValidateXmlStructure(string xmlData)
        {
            try
            {
                var doc = XDocument.Parse(xmlData);
                var root = doc.Root;

                if (root == null)
                    return (false, "XML document has no root element");

                // Check for error element first
                var errorElement = root.Element("Error");
                if (errorElement != null)
                    return (false, "XML contains error: " + errorElement.Value);

                // Allow both "Root" and other root names for flexibility
                if (root.Name != "Root")
                {
                    // If it's not "Root", check if it has the expected structure
                    var metadata = root.Element("Metadata");
                    var rows = root.Elements("Row").ToList();
                    
                    if (metadata == null && rows.Count == 0)
                        return (false, $"Unexpected root element '{root.Name}'. Expected 'Root' or valid structure.");
                }

                var metadataElement = root.Element("Metadata");
                if (metadataElement == null)
                {
                    // If no metadata, check if we have data rows to infer structure
                    var rows = root.Elements("Row").ToList();
                    if (rows.Count == 0)
                        return (false, "No Metadata element found and no data rows to infer structure");
                    
                    // If we have data rows, that's acceptable
                    return (true, null);
                }

                var dataRows = root.Elements("Row").ToList();
                if (dataRows.Count == 0)
                {
                    // Allow empty tables - just warn
                    return (true, "Warning: No data rows found, but structure is valid");
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, "XML parsing error: " + ex.Message);
            }
        }
    }
} 