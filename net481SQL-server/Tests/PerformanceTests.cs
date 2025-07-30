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
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, $"Encryption took {stopwatch.ElapsedMilliseconds}ms, expected < 1000ms");
            
            Console.WriteLine($"100 columns encryption: {stopwatch.ElapsedMilliseconds}ms");
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
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 5000, $"Encryption took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");
            
            Console.WriteLine($"500 columns encryption: {stopwatch.ElapsedMilliseconds}ms");
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
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 10000, $"Encryption took {stopwatch.ElapsedMilliseconds}ms, expected < 10000ms");
            
            Console.WriteLine($"1000 columns encryption: {stopwatch.ElapsedMilliseconds}ms");
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
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 2000, $"Round trip took {stopwatch.ElapsedMilliseconds}ms, expected < 2000ms");
            
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
            
            Console.WriteLine($"100 columns round trip: {stopwatch.ElapsedMilliseconds}ms");
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
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 5000, $"Batch encryption took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");
            
            Console.WriteLine($"10 rows x 100 columns batch encryption: {stopwatch.ElapsedMilliseconds}ms");
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
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 15000, $"Batch encryption took {stopwatch.ElapsedMilliseconds}ms, expected < 15000ms");
            
            Console.WriteLine($"100 rows x 50 columns batch encryption: {stopwatch.ElapsedMilliseconds}ms");
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
            Assert.IsTrue(memoryIncrease < 50 * 1024 * 1024, $"Memory increase: {memoryIncrease / (1024 * 1024)}MB, expected < 50MB");
            
            Console.WriteLine($"Memory increase for 500 columns: {memoryIncrease / (1024 * 1024)}MB");
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
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 10000, $"Concurrent encryption took {stopwatch.ElapsedMilliseconds}ms, expected < 10000ms");
            
            Console.WriteLine($"10 concurrent encryptions (100 columns each): {stopwatch.ElapsedMilliseconds}ms");
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
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 5000, $"Large string processing took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");
            
            Console.WriteLine($"Large string processing: {stopwatch.ElapsedMilliseconds}ms");
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