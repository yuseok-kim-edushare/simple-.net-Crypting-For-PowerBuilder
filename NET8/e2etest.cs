#if !RELEASE_WITHOUT_TESTS
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureLibrary.Core; // Use the .NET 8 SecureLibrary
using System.Security.Cryptography;
using System;

namespace SecureLibrary.Tests
{
    [TestClass]
    public class CrossFrameworkTests
    {
        [TestMethod]
        public void Can_Decrypt_Legacy_Encrypted_Data_From_Net481PB()
        {
            // Arrange
            var secureLibrary = new SecureLibrary.Core.SecureLibrary();
            string originalText = "This is a secret message in the old format!";
            string password = "my-legacy-password";
            int legacyIterations = 2000;

            // This sample string is encrypted using the net481PB's EncryptAesGcmWithPasswordLegacy method.
            // It needs to be generated from a successful run of the net481PB test.
            // Example placeholder - replace with actual encrypted string from net481PB test run.
            string legacyEncryptedData = "EAAAAAC/p01GgTz4p5GjXwA+bT9x9V2FwWjAAAAAElFTkSuQmCC1r/u8Yl5/wB/ODY5YjA3ZGM2YjE0ZDYxOGU3ZGUzYmEwYjM5ZTY2Mg==";

            // Act
            string decryptedText = secureLibrary.DecryptAesGcmWithPasswordLegacy(legacyEncryptedData, password, legacyIterations);

            // Assert
            Assert.AreEqual(originalText, decryptedText, "Decrypted legacy text from net481PB should match the original.");
        }

        [TestMethod]
        public void Can_Encrypt_And_Decrypt_With_Standard_Format()
        {
            // Arrange
            var secureLibrary = new SecureLibrary.Core.SecureLibrary();
            string originalText = "This is a standard secret message!";
            string password = "my-standard-password";
            int iterations = 5000; // Lower for testing speed

            // Act
            string encryptedText = secureLibrary.EncryptAesGcmWithPassword(originalText, password, iterations);
            string decryptedText = secureLibrary.DecryptAesGcmWithPassword(encryptedText, password, iterations);

            // Assert
            Assert.AreEqual(originalText, decryptedText, "Standard encryption/decryption should work correctly.");
        }
    }
} 
#endif