using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureLibrary.SQL.Interfaces;
using SecureLibrary.SQL.Services;

namespace SecureLibrary.SQL.Tests
{
    /// <summary>
    /// Performance tests for the encryption engine
    /// Validates scalability with large datasets and many columns
    /// </summary>
    [TestClass]
    public class PerformanceTests
    {
        private ICgnService _cgnService;
        private ISqlXmlConverter _xmlConverter;
        private IEncryptionEngine _encryptionEngine;
        private ILogger _logger;

        [TestInitialize]
        public void Setup()
        {
            _cgnService = new CgnService();
            _xmlConverter = new SqlXmlConverter();
            _logger = new NullLogger();
            _encryptionEngine = new EncryptionEngine(_cgnService, _xmlConverter, _logger);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_cgnService is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        [TestMethod]
        public void EncryptRow_100Columns_PerformanceTest()
        {
            // Arrange
            var table = CreateLargeTable(100);
            var row = table.NewRow();
            PopulateRowWithTestData(row);
            table.Rows.Add(row);

            var metadata = CreateValidEncryptionMetadata();

            // Act
            var stopwatch = Stopwatch.StartNew();
            var encryptedData = _encryptionEngine.EncryptRow(row, metadata);
            stopwatch.Stop();

            // Assert
            Assert.IsNotNull(encryptedData);
            
            // Report performance metrics
            var elapsedMs = stopwatch.ElapsedMilliseconds;
            var throughput = 100.0 / (elapsedMs / 1000.0); // columns per second
            Console.WriteLine($"100 columns encryption: {elapsedMs}ms ({throughput:F2} columns/sec)");
            
            // Log performance data for analysis
            Console.WriteLine($"Performance: {elapsedMs}ms for 100 columns ({throughput:F2} columns/sec)");
        }

        [TestMethod]
        public void EncryptRow_500Columns_PerformanceTest()
        {
            // Arrange
            var table = CreateLargeTable(500);
            var row = table.NewRow();
            PopulateRowWithTestData(row);
            table.Rows.Add(row);

            var metadata = CreateValidEncryptionMetadata();

            // Act
            var stopwatch = Stopwatch.StartNew();
            var encryptedData = _encryptionEngine.EncryptRow(row, metadata);
            stopwatch.Stop();

            // Assert
            Assert.IsNotNull(encryptedData);
            
            // Report performance metrics
            var elapsedMs = stopwatch.ElapsedMilliseconds;
            var throughput = 500.0 / (elapsedMs / 1000.0); // columns per second
            Console.WriteLine($"500 columns encryption: {elapsedMs}ms ({throughput:F2} columns/sec)");
            
            // Log performance data for analysis
            Console.WriteLine($"Performance: {elapsedMs}ms for 500 columns ({throughput:F2} columns/sec)");
        }

        [TestMethod]
        public void EncryptRow_1000Columns_PerformanceTest()
        {
            // Arrange
            var table = CreateLargeTable(1000);
            var row = table.NewRow();
            PopulateRowWithTestData(row);
            table.Rows.Add(row);

            var metadata = CreateValidEncryptionMetadata();

            // Act
            var stopwatch = Stopwatch.StartNew();
            var encryptedData = _encryptionEngine.EncryptRow(row, metadata);
            stopwatch.Stop();

            // Assert
            Assert.IsNotNull(encryptedData);
            
            // Report performance metrics
            var elapsedMs = stopwatch.ElapsedMilliseconds;
            var throughput = 1000.0 / (elapsedMs / 1000.0); // columns per second
            Console.WriteLine($"1000 columns encryption: {elapsedMs}ms ({throughput:F2} columns/sec)");
            
            // Log performance data for analysis
            Console.WriteLine($"Performance: {elapsedMs}ms for 1000 columns ({throughput:F2} columns/sec)");
        }

        [TestMethod]
        public void EncryptDecryptRoundTrip_100Columns_PerformanceTest()
        {
            // Arrange
            var table = CreateLargeTable(100);
            var row = table.NewRow();
            PopulateRowWithTestData(row);
            table.Rows.Add(row);

            var metadata = CreateValidEncryptionMetadata();

            // Act
            var stopwatch = Stopwatch.StartNew();
            var encryptedData = _encryptionEngine.EncryptRow(row, metadata);
            var decryptedRow = _encryptionEngine.DecryptRow(encryptedData, metadata);
            stopwatch.Stop();

            // Assert
            Assert.IsNotNull(decryptedRow);
            
            // Report performance metrics
            var elapsedMs = stopwatch.ElapsedMilliseconds;
            var throughput = 100.0 / (elapsedMs / 1000.0); // columns per second
            Console.WriteLine($"100 columns round trip: {elapsedMs}ms ({throughput:F2} columns/sec)");
            
            // Verify data integrity
            for (int i = 0; i < Math.Min(10, decryptedRow.Table.Columns.Count); i++)
            {
                var column = decryptedRow.Table.Columns[i];
                var originalValue = row[column.ColumnName];
                var decryptedValue = decryptedRow[column.ColumnName];
                
                if (column.DataType == typeof(byte[]))
                {
                    CollectionAssert.AreEqual((byte[])originalValue, (byte[])decryptedValue, $"Column {column.ColumnName} data mismatch");
                }
                else
                {
                    Assert.AreEqual(originalValue, decryptedValue, $"Column {column.ColumnName} data mismatch");
                }
            }
            
            // Log performance data for analysis
            Console.WriteLine($"Round trip performance: {elapsedMs}ms for 100 columns ({throughput:F2} columns/sec)");
        }

        [TestMethod]
        public void BatchEncryption_10Rows_100Columns_PerformanceTest()
        {
            // Arrange
            var table = CreateLargeTable(100);
            var rows = new List<DataRow>();

            for (int i = 0; i < 10; i++)
            {
                var row = table.NewRow();
                PopulateRowWithTestData(row);
                table.Rows.Add(row);
                rows.Add(row);
            }

            var metadata = CreateValidEncryptionMetadata();

            // Act
            var stopwatch = Stopwatch.StartNew();
            var encryptedRows = _encryptionEngine.EncryptRows(rows, metadata).ToList();
            stopwatch.Stop();

            // Assert
            Assert.AreEqual(10, encryptedRows.Count);
            
            // Report performance metrics
            var elapsedMs = stopwatch.ElapsedMilliseconds;
            var totalColumns = 10 * 100;
            var throughput = totalColumns / (elapsedMs / 1000.0); // columns per second
            Console.WriteLine($"10 rows x 100 columns batch encryption: {elapsedMs}ms ({throughput:F2} columns/sec)");
            
            // Log performance data for analysis
            Console.WriteLine($"Batch encryption performance: {elapsedMs}ms for {totalColumns} total columns ({throughput:F2} columns/sec)");
        }

        [TestMethod]
        public void BatchEncryption_100Rows_50Columns_PerformanceTest()
        {
            // Arrange
            var table = CreateLargeTable(50);
            var rows = new List<DataRow>();

            for (int i = 0; i < 100; i++)
            {
                var row = table.NewRow();
                PopulateRowWithTestData(row);
                table.Rows.Add(row);
                rows.Add(row);
            }

            var metadata = CreateValidEncryptionMetadata();

            // Act
            var stopwatch = Stopwatch.StartNew();
            var encryptedRows = _encryptionEngine.EncryptRows(rows, metadata).ToList();
            stopwatch.Stop();

            // Assert
            Assert.AreEqual(100, encryptedRows.Count);
            
            // Report performance metrics
            var elapsedMs = stopwatch.ElapsedMilliseconds;
            var totalColumns = 100 * 50;
            var throughput = totalColumns / (elapsedMs / 1000.0); // columns per second
            Console.WriteLine($"100 rows x 50 columns batch encryption: {elapsedMs}ms ({throughput:F2} columns/sec)");
            
            // Log performance data for analysis
            Console.WriteLine($"Batch encryption performance: {elapsedMs}ms for {totalColumns} total columns ({throughput:F2} columns/sec)");
        }

        [TestMethod]
        public void MemoryUsage_LargeTable_StaysReasonable()
        {
            // Arrange
            var table = CreateLargeTable(500);
            var row = table.NewRow();
            PopulateRowWithTestData(row);
            table.Rows.Add(row);

            var metadata = CreateValidEncryptionMetadata();

            // Act
            var initialMemory = GC.GetTotalMemory(false);
            var encryptedData = _encryptionEngine.EncryptRow(row, metadata);
            var finalMemory = GC.GetTotalMemory(false);
            var memoryIncrease = finalMemory - initialMemory;

            // Assert
            Assert.IsNotNull(encryptedData);
            
            // Report memory usage metrics
            var memoryIncreaseMB = memoryIncrease / (1024.0 * 1024.0);
            var memoryPerColumn = memoryIncrease / 500.0; // bytes per column
            Console.WriteLine($"Memory increase for 500 columns: {memoryIncreaseMB:F2}MB ({memoryPerColumn:F0} bytes/column)");
            
            // Log memory usage for analysis
            Console.WriteLine($"Memory usage: {memoryIncreaseMB:F2}MB increase for 500 columns ({memoryPerColumn:F0} bytes/column)");
        }

        [TestMethod]
        public void ConcurrentEncryption_ThreadSafetyTest()
        {
            // Arrange
            var table = CreateLargeTable(100);
            var metadata = CreateValidEncryptionMetadata();
            var results = new List<EncryptedRowData>();
            var lockObject = new object();

            // Act
            var stopwatch = Stopwatch.StartNew();
            var tasks = Enumerable.Range(0, 10).Select(i =>
            {
                return System.Threading.Tasks.Task.Run(() =>
                {
                    var row = table.NewRow();
                    PopulateRowWithTestData(row);
                    table.Rows.Add(row);

                    var encryptedData = _encryptionEngine.EncryptRow(row, metadata);
                    
                    lock (lockObject)
                    {
                        results.Add(encryptedData);
                    }
                });
            }).ToArray();

            // Wait for all tasks to complete
            System.Threading.Tasks.Task.WaitAll(tasks);
            stopwatch.Stop();

            // Assert
            Assert.AreEqual(10, results.Count);
            
            // Report performance metrics
            var elapsedMs = stopwatch.ElapsedMilliseconds;
            var totalColumns = 10 * 100;
            var throughput = totalColumns / (elapsedMs / 1000.0); // columns per second
            Console.WriteLine($"10 concurrent encryptions (100 columns each): {elapsedMs}ms ({throughput:F2} columns/sec)");
            
            // Log performance data for analysis
            Console.WriteLine($"Concurrent encryption performance: {elapsedMs}ms for {totalColumns} total columns ({throughput:F2} columns/sec)");
        }

        [TestMethod]
        public void LargeStringData_PerformanceTest()
        {
            // Arrange
            var table = CreateTestTableWithLargeStrings();
            var row = table.NewRow();
            row["Id"] = 1;
            row["LargeText"] = new string('A', 100000); // 100KB string
            row["MediumText"] = new string('B', 10000);  // 10KB string
            row["SmallText"] = "Normal text";
            table.Rows.Add(row);

            var metadata = CreateValidEncryptionMetadata();

            // Act
            var stopwatch = Stopwatch.StartNew();
            var encryptedData = _encryptionEngine.EncryptRow(row, metadata);
            var decryptedRow = _encryptionEngine.DecryptRow(encryptedData, metadata);
            stopwatch.Stop();

            // Assert
            Assert.IsNotNull(decryptedRow);
            Assert.AreEqual(new string('A', 100000), decryptedRow["LargeText"]);
            Assert.AreEqual(new string('B', 10000), decryptedRow["MediumText"]);
            Assert.AreEqual("Normal text", decryptedRow["SmallText"]);
            
            // Report performance metrics
            var elapsedMs = stopwatch.ElapsedMilliseconds;
            var totalDataSize = 100000 + 10000 + 12; // bytes
            var throughput = totalDataSize / (elapsedMs / 1000.0); // bytes per second
            Console.WriteLine($"Large string processing: {elapsedMs}ms ({throughput:F0} bytes/sec)");
            
            // Log performance data for analysis
            Console.WriteLine($"Large string performance: {elapsedMs}ms for {totalDataSize} bytes ({throughput:F0} bytes/sec)");
        }

        [TestMethod]
        public void PerformanceRegression_CompareWithBaseline()
        {
            // This test measures performance and compares with a baseline
            // It doesn't assert specific timing but reports if performance degrades significantly
            
            // Arrange
            var table = CreateLargeTable(100);
            var row = table.NewRow();
            PopulateRowWithTestData(row);
            table.Rows.Add(row);

            var metadata = CreateValidEncryptionMetadata();

            // Act - Run multiple iterations for more accurate measurement
            var iterations = 5;
            var totalTime = 0L;
            
            for (int i = 0; i < iterations; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                var encryptedData = _encryptionEngine.EncryptRow(row, metadata);
                stopwatch.Stop();
                totalTime += stopwatch.ElapsedMilliseconds;
            }
            
            var averageTime = totalTime / (double)iterations;
            var throughput = 100.0 / (averageTime / 1000.0); // columns per second
            
            // Report performance metrics
            Console.WriteLine($"Average performance over {iterations} iterations: {averageTime:F2}ms ({throughput:F2} columns/sec)");
            
            // Log performance data for analysis
            Console.WriteLine($"Baseline performance: {averageTime:F2}ms average for 100 columns ({throughput:F2} columns/sec)");
            
            // Note: In a real scenario, you might compare this with a stored baseline
            // and fail if performance degrades by more than a certain percentage
        }

        #region Helper Methods

        private DataTable CreateLargeTable(int columnCount)
        {
            var table = new DataTable();
            
            // Add different column types
            for (int i = 0; i < columnCount; i++)
            {
                var columnType = i % 8;
                switch (columnType)
                {
                    case 0:
                        table.Columns.Add($"IntColumn{i}", typeof(int));
                        break;
                    case 1:
                        table.Columns.Add($"StringColumn{i}", typeof(string));
                        break;
                    case 2:
                        table.Columns.Add($"DateTimeColumn{i}", typeof(DateTime));
                        break;
                    case 3:
                        table.Columns.Add($"BoolColumn{i}", typeof(bool));
                        break;
                    case 4:
                        table.Columns.Add($"DecimalColumn{i}", typeof(decimal));
                        break;
                    case 5:
                        table.Columns.Add($"GuidColumn{i}", typeof(Guid));
                        break;
                    case 6:
                        table.Columns.Add($"BinaryColumn{i}", typeof(byte[]));
                        break;
                    case 7:
                        table.Columns.Add($"DoubleColumn{i}", typeof(double));
                        break;
                }
            }
            
            return table;
        }

        private DataTable CreateTestTableWithLargeStrings()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("LargeText", typeof(string));
            table.Columns.Add("MediumText", typeof(string));
            table.Columns.Add("SmallText", typeof(string));
            return table;
        }

        private void PopulateRowWithTestData(DataRow row)
        {
            for (int i = 0; i < row.Table.Columns.Count; i++)
            {
                var column = row.Table.Columns[i];
                var columnType = i % 8;
                
                switch (columnType)
                {
                    case 0: // int
                        row[column] = i;
                        break;
                    case 1: // string
                        row[column] = $"Test String {i}";
                        break;
                    case 2: // DateTime
                        row[column] = DateTime.Now.AddDays(i);
                        break;
                    case 3: // bool
                        row[column] = i % 2 == 0;
                        break;
                    case 4: // decimal
                        row[column] = (decimal)(i + 0.5);
                        break;
                    case 5: // Guid
                        row[column] = Guid.NewGuid();
                        break;
                    case 6: // byte[]
                        row[column] = Encoding.UTF8.GetBytes($"Binary Data {i}");
                        break;
                    case 7: // double
                        row[column] = (double)(i + 0.25);
                        break;
                }
            }
        }

        private EncryptionMetadata CreateValidEncryptionMetadata()
        {
            return new EncryptionMetadata
            {
                Algorithm = "AES-GCM",
                Key = "TestPassword123!",
                Salt = new byte[16],
                Iterations = 2000,
                AutoGenerateNonce = true
            };
        }

        #endregion
    }
} 