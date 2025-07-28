using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Text;
using Microsoft.SqlServer.Server;
using System.Security;

namespace SecureLibrary.SQL
{
    /// <summary>
    /// Dynamic temp-table wrapper functionality for automatically discovering stored procedure result sets
    /// and creating matching temp tables without manual column declarations.
    /// </summary>
    public static class DynamicTempTableWrapper
    {
        /// <summary>
        /// Dynamic temp-table wrapper that automatically discovers any stored procedure's result set structure
        /// and creates a matching temp table. This eliminates the need for manual column declarations.
        /// 
        /// Usage: EXEC dbo.WrapDecryptProcedure 'dbo.RestoreEncryptedTable', '@encryptedData=''data'', @password=''pass'''
        /// </summary>
        [SqlProcedure]
        [SecuritySafeCritical]
        public static void WrapDecryptProcedure(SqlString procedureName, SqlString parameters)
        {
            if (procedureName.IsNull)
            {
                SqlContext.Pipe.Send("Error: Procedure name cannot be null");
                return;
            }

            try
            {
                using (var connection = new System.Data.SqlClient.SqlConnection("context connection=true"))
                {
                    connection.Open();
                    
                    // Step 1: Discover the result set structure using sys.dm_exec_describe_first_result_set_for_object
                    string discoverQuery = @"
                        SELECT 
                            QUOTENAME(name) + ' ' + system_type_name AS ColumnDefinition
                        FROM sys.dm_exec_describe_first_result_set_for_object(
                            OBJECT_ID(@ProcedureName), 
                            0
                        )
                        WHERE is_hidden = 0 AND error_state IS NULL
                        ORDER BY column_ordinal";

                    var columnDefinitions = new List<string>();
                    using (var discoverCmd = new System.Data.SqlClient.SqlCommand(discoverQuery, connection))
                    {
                        discoverCmd.Parameters.AddWithValue("@ProcedureName", procedureName.Value);
                        using (var reader = discoverCmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                columnDefinitions.Add(reader.GetString(0));
                            }
                        }
                    }

                    if (columnDefinitions.Count == 0)
                    {
                        SqlContext.Pipe.Send($"Error: Unable to discover result set for procedure '{procedureName.Value}'");
                        return;
                    }

                    // Step 2: Build the dynamic SQL with automatic temp table creation
                    var dynamicSql = new StringBuilder();
                    var columnsClause = string.Join(", ", columnDefinitions);
                    
                    dynamicSql.AppendLine("-- Auto-generated temp table wrapper");
                    dynamicSql.AppendLine("-- Created by WrapDecryptProcedure for: " + procedureName.Value);
                    dynamicSql.AppendLine();
                    dynamicSql.AppendLine("-- Step 1: Create temp table with discovered structure");
                    dynamicSql.AppendLine("CREATE TABLE #Decrypted (" + columnsClause + ");");
                    dynamicSql.AppendLine();
                    dynamicSql.AppendLine("-- Step 2: Execute the target procedure and capture results");
                    dynamicSql.Append("INSERT INTO #Decrypted EXEC " + procedureName.Value);
                    
                    if (!parameters.IsNull && !string.IsNullOrEmpty(parameters.Value))
                    {
                        dynamicSql.Append(" " + parameters.Value);
                    }
                    dynamicSql.AppendLine(";");
                    dynamicSql.AppendLine();
                    dynamicSql.AppendLine("-- Step 3: Return the results");
                    dynamicSql.AppendLine("SELECT * FROM #Decrypted;");
                    dynamicSql.AppendLine();
                    dynamicSql.AppendLine("-- Step 4: Clean up");
                    dynamicSql.AppendLine("DROP TABLE #Decrypted;");

                    // Step 3: Execute the dynamic SQL
                    using (var execCmd = new System.Data.SqlClient.SqlCommand(dynamicSql.ToString(), connection))
                    {
                        SqlContext.Pipe.ExecuteAndSend(execCmd);
                    }
                }
            }
            catch (Exception ex)
            {
                SqlContext.Pipe.Send($"Error in WrapDecryptProcedure: {ex.Message}");
            }
        }

        /// <summary>
        /// Enhanced version of WrapDecryptProcedure that provides more detailed metadata information
        /// and supports custom temp table names for better integration with existing workflows.
        /// </summary>
        [SqlProcedure]
        [SecuritySafeCritical]
        public static void WrapDecryptProcedureAdvanced(SqlString procedureName, SqlString parameters, SqlString tempTableName)
        {
            if (procedureName.IsNull)
            {
                SqlContext.Pipe.Send("Error: Procedure name cannot be null");
                return;
            }

            string tableName = tempTableName.IsNull ? "#Decrypted" : tempTableName.Value;
            
            try
            {
                using (var connection = new System.Data.SqlClient.SqlConnection("context connection=true"))
                {
                    connection.Open();
                    
                    // Step 1: Get detailed metadata about the result set
                    string metadataQuery = @"
                        SELECT 
                            name,
                            system_type_name,
                            max_length,
                            precision,
                            scale,
                            is_nullable,
                            column_ordinal
                        FROM sys.dm_exec_describe_first_result_set_for_object(
                            OBJECT_ID(@ProcedureName), 
                            0
                        )
                        WHERE is_hidden = 0 AND error_state IS NULL
                        ORDER BY column_ordinal";

                    var columns = new List<ColumnMetadata>();
                    using (var metadataCmd = new System.Data.SqlClient.SqlCommand(metadataQuery, connection))
                    {
                        metadataCmd.Parameters.AddWithValue("@ProcedureName", procedureName.Value);
                        using (var reader = metadataCmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                columns.Add(new ColumnMetadata
                                {
                                    Name = reader.GetString(0),
                                    SystemTypeName = reader.GetString(1),
                                    MaxLength = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                                    Precision = reader.IsDBNull(3) ? (byte?)null : reader.GetByte(3),
                                    Scale = reader.IsDBNull(4) ? (byte?)null : reader.GetByte(4),
                                    IsNullable = reader.GetBoolean(5),
                                    Ordinal = reader.GetInt32(6)
                                });
                            }
                        }
                    }

                    if (columns.Count == 0)
                    {
                        SqlContext.Pipe.Send($"Error: Unable to discover result set for procedure '{procedureName.Value}'");
                        return;
                    }

                    // Step 2: Build enhanced column definitions with proper type handling
                    var columnDefinitions = new List<string>();
                    foreach (var column in columns)
                    {
                        string columnDef = BuildEnhancedColumnDefinition(column);
                        columnDefinitions.Add(columnDef);
                    }

                    // Step 3: Build the dynamic SQL with enhanced features
                    var dynamicSql = new StringBuilder();
                    var columnsClause = string.Join(", ", columnDefinitions);
                    
                    dynamicSql.AppendLine("-- Enhanced auto-generated temp table wrapper");
                    dynamicSql.AppendLine("-- Created by WrapDecryptProcedureAdvanced");
                    dynamicSql.AppendLine("-- Target procedure: " + procedureName.Value);
                    dynamicSql.AppendLine("-- Discovered columns: " + columns.Count);
                    dynamicSql.AppendLine("-- Temp table: " + tableName);
                    dynamicSql.AppendLine();
                    dynamicSql.AppendLine("-- Step 1: Create temp table with discovered structure");
                    dynamicSql.AppendLine("CREATE TABLE " + tableName + " (" + columnsClause + ");");
                    dynamicSql.AppendLine();
                    dynamicSql.AppendLine("-- Step 2: Execute the target procedure and capture results");
                    dynamicSql.Append("INSERT INTO " + tableName + " EXEC " + procedureName.Value);
                    
                    if (!parameters.IsNull && !string.IsNullOrEmpty(parameters.Value))
                    {
                        dynamicSql.Append(" " + parameters.Value);
                    }
                    dynamicSql.AppendLine(";");
                    dynamicSql.AppendLine();
                    dynamicSql.AppendLine("-- Step 3: Return the results");
                    dynamicSql.AppendLine("SELECT * FROM " + tableName + ";");
                    dynamicSql.AppendLine();
                    dynamicSql.AppendLine("-- Step 4: Clean up (only if using default temp table)");
                    if (tableName.StartsWith("#"))
                    {
                        dynamicSql.AppendLine("DROP TABLE " + tableName + ";");
                    }

                    // Step 4: Execute the dynamic SQL
                    using (var execCmd = new System.Data.SqlClient.SqlCommand(dynamicSql.ToString(), connection))
                    {
                        SqlContext.Pipe.ExecuteAndSend(execCmd);
                    }
                }
            }
            catch (Exception ex)
            {
                SqlContext.Pipe.Send($"Error in WrapDecryptProcedureAdvanced: {ex.Message}");
            }
        }

        /// <summary>
        /// Builds an enhanced column definition with proper type handling for temp table creation
        /// </summary>
        private static string BuildEnhancedColumnDefinition(ColumnMetadata column)
        {
            var definition = new StringBuilder();
            definition.Append(XmlUtilities.QUOTENAME(column.Name));
            definition.Append(" ");
            definition.Append(column.SystemTypeName);

            // Add length/precision/scale for appropriate types
            switch (column.SystemTypeName.ToLower())
            {
                case "varchar":
                case "nvarchar":
                case "char":
                case "nchar":
                    if (column.MaxLength.HasValue && column.MaxLength.Value > 0)
                    {
                        if (column.MaxLength.Value == -1)
                            definition.Append("(MAX)");
                        else
                            definition.Append("(" + column.MaxLength.Value + ")");
                    }
                    break;

                case "decimal":
                case "numeric":
                    if (column.Precision.HasValue && column.Scale.HasValue)
                    {
                        definition.Append("(" + column.Precision.Value + "," + column.Scale.Value + ")");
                    }
                    break;

                case "datetime2":
                case "time":
                case "datetimeoffset":
                    if (column.Scale.HasValue)
                    {
                        definition.Append("(" + column.Scale.Value + ")");
                    }
                    break;
            }

            // Add nullable constraint
            if (!column.IsNullable)
            {
                definition.Append(" NOT NULL");
            }

            return definition.ToString();
        }
    }
} 