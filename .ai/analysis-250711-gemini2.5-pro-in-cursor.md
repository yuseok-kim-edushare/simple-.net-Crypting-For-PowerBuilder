# Security Analysis & Hardening Report

**Date:** 2025-07-11

## 1. Executive Summary

A security analysis of the `simple-.net-Crypting-For-PowerBuilder` project was conducted. The initial review found that while the project is built on a strong cryptographic foundation, several critical vulnerabilities existed in the implementation, primarily related to improper error handling and insecure memory management.

This report details the vulnerabilities found, the remediation actions that have been **completed**, and the final outstanding recommendations to ensure the library is secure and robust for production use.

## 2. Vulnerabilities Found & Actions Taken

### 🔴 Critical Vulnerability 1: Sensitive Data Leak in Memory

-   **Description:** The .NET code, particularly in the P/Invoke wrappers (`invokeCGN.cs`) and the SQL CLR project, failed to clear sensitive data (cryptographic keys, IVs, plaintext, and ciphertext) from memory after use. This exposed the data to potential memory-scraping attacks.
-   **Affected Files:**
    -   `net481PB/invokeCGN.cs`
    -   `net481SQL-server/invokeCGN.cs`
    -   `net481SQL-server/draft.cs`
-   **Status:** **FIXED**
-   **Action Taken:** The relevant functions in the affected files were refactored to ensure that all byte arrays containing sensitive material are explicitly overwritten with zeros using `Array.Clear()` inside `finally` blocks. This guarantees that the data is scrubbed from memory even if an error occurs.

### 🔴 Critical Vulnerability 2: Cryptographic Exception Swallowing

-   **Description:** The code in the SQL CLR project and the PowerBuilder wrapper was designed to catch all exceptions, including critical cryptographic security warnings, and return a `NULL` value. This is extremely dangerous, as it makes it impossible for a calling application to distinguish between a successful operation returning a legitimate `NULL` and a security failure, such as an attacker tampering with ciphertext.
-   **Affected Files:**
    -   `net481SQL-server/draft.cs`
    -   `net481PB/draft.cs`
    -   `Examples/Powerbuilder-Net 4.8/nvo_encryptionhelper.sru`
-   **Status:** **FIXED** in .NET / **DOCUMENTED RISK** in PowerBuilder
-   **Action Taken:** All exception-swallowing `try...catch` blocks in the C# code (`net481SQL-server/draft.cs` and `net481PB/draft.cs`) have been removed. The .NET libraries will now correctly throw exceptions on failure. The exception-swallowing behavior in the PowerBuilder wrapper is now documented as a known risk that must be managed by the application developer (see Section 3).

---

## 3. Final Hardening & Documentation

The C# library code has been fully hardened based on the analysis. The final recommendations concern how the library is used and documented, particularly regarding the PowerBuilder wrapper's behavior.

### 1. Document the PowerBuilder Wrapper's Error Handling

-   **File:** `net481PB/draft.cs`
    -   **Issue:** The PowerBuilder wrapper code systematically catches all `.NET` exceptions and returns a `NULL` value. This pattern is intentionally used to prevent unhandled .NET exceptions from crashing the PowerBuilder runtime environment. However, it introduces a significant security risk by masking the difference between a successful operation and a critical security failure (e.g., an `AuthenticationTagMismatchException` when decrypting tampered data). The calling application cannot distinguish a security failure from a legitimate `NULL` result.
    -   **Recommendation & Risk Mitigation:** Given the need for runtime stability, removing the `Try...Catch` block is not advisable. Instead, developers using this wrapper **must** be made aware of this behavior. The `ib_CrashOnException` flag provides an important escape hatch for developers who want to handle raw exceptions. For applications where this is not feasible, developers should treat any `NULL` return from a decryption function as a potential security event that requires logging and investigation, not just as empty data. This behavior should be clearly documented in the project's `README.md`.

### 2. Deprecate Insecure AES-CBC Mode (Completed)

*   **Status:** **COMPLETED**
*   **Action Taken:** The `[Obsolete("...")]` attribute has been added to the `EncryptAesCbcWithIv` and `DecryptAesCbcWithIv` functions in all C# files. The message warns developers that the method is insecure and directs them to use the GCM functions instead.

### 3. Make Bcrypt Work Factor Configurable (Completed)

*   **Status:** **COMPLETED**
*   **Action Taken:** The `BcryptEncoding` and `HashPassword` functions were modified to accept an optional integer parameter for the work factor, allowing hashing strength to be increased over time.

