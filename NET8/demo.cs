using System;
using System.Diagnostics;
using SecureLibrary;

namespace PerformanceDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Password-Based Encryption Performance Demo ===\n");
            
            // Setup
            string password = "DatabasePassword123";
            string salt = EncryptionHelper.GenerateSalt();
            string[] testData = {
                "Sample data record 1",
                "Sample data record 2", 
                "Sample data record 3",
                "Sample data record 4",
                "Sample data record 5",
                "Sample data record 6",
                "Sample data record 7",
                "Sample data record 8",
                "Sample data record 9",
                "Sample data record 10"
            };

            Console.WriteLine($"Testing with {testData.Length} records...\n");

            // Test 1: Traditional password-based encryption (slow)
            Console.WriteLine("1. Traditional Password-Based Encryption (with key derivation each time):");
            var stopwatch = Stopwatch.StartNew();
            
            string[] encryptedTraditional = new string[testData.Length];
            for (int i = 0; i < testData.Length; i++)
            {
                encryptedTraditional[i] = EncryptionHelper.EncryptAesGcmWithPasswordAndSalt(
                    testData[i], password, salt, 2000);
            }
            
            stopwatch.Stop();
            long traditionalTime = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"   Time: {traditionalTime} ms");

            // Test 2: Optimized cached key encryption (fast)
            Console.WriteLine("\n2. Optimized Cached Key Encryption (key derivation only once):");
            stopwatch.Restart();
            
            // Derive key once
            string cachedKey = EncryptionHelper.DeriveKeyFromPassword(password, salt, 2000);
            
            string[] encryptedOptimized = new string[testData.Length];
            for (int i = 0; i < testData.Length; i++)
            {
                encryptedOptimized[i] = EncryptionHelper.EncryptAesGcmWithDerivedKey(
                    testData[i], cachedKey, salt);
            }
            
            stopwatch.Stop();
            long optimizedTime = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"   Time: {optimizedTime} ms");

            // Performance comparison
            double improvement = traditionalTime > 0 ? 
                (double)(traditionalTime - optimizedTime) / traditionalTime * 100 : 0;
            
            Console.WriteLine($"\n=== Performance Results ===");
            Console.WriteLine($"Traditional method: {traditionalTime} ms");
            Console.WriteLine($"Optimized method:   {optimizedTime} ms");
            Console.WriteLine($"Performance improvement: {improvement:F1}%");
            Console.WriteLine($"Speed improvement: {(traditionalTime > 0 ? (double)traditionalTime / Math.Max(optimizedTime, 1) : 1):F1}x faster");

            // Verify compatibility - decrypt traditional with optimized method
            Console.WriteLine($"\n=== Compatibility Test ===");
            try
            {
                string decrypted = EncryptionHelper.DecryptAesGcmWithDerivedKey(
                    encryptedTraditional[0], cachedKey);
                
                bool compatible = decrypted == testData[0];
                Console.WriteLine($"Cross-compatibility: {(compatible ? "✓ PASSED" : "✗ FAILED")}");
                
                if (compatible)
                {
                    Console.WriteLine("   Traditional encryption can be decrypted with cached key method");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cross-compatibility: ✗ FAILED - {ex.Message}");
            }

            Console.WriteLine($"\n=== Usage Recommendations ===");
            Console.WriteLine("• Use traditional methods for single operations");
            Console.WriteLine("• Use cached key methods for batch processing");
            Console.WriteLine("• Cache keys securely and clear from memory when done");
            Console.WriteLine("• Both methods provide identical security guarantees");
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}