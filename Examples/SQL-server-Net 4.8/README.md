# SQL Server CLR Examples - Complete Installation Guide

This folder contains the unified, comprehensive SQL CLR installation and usage examples for SecureLibrary-SQL, updated to include all functionality from PR #61.

## Quick Start

1. **Install**: Run `install.sql` 
   - Update the DLL path in the script
   - Update the target database name
   - Execute the script to install all functions

2. **Test**: Run `example.sql`
   - Demonstrates all available functionality
   - Includes new password-based table encryption
   - Shows PowerBuilder integration examples

3. **Uninstall**: Run `uninstall.sql` if needed

## What's New (PR #61)

✅ **Password-Based Table Encryption**
- `EncryptXmlWithPassword` - Encrypt entire tables with a password
- `RestoreEncryptedTable` - Universal stored procedure to restore any encrypted table

✅ **Row-by-Row Encryption** 
- `EncryptRowDataAesGcm` - Encrypt individual JSON rows
- `DecryptRowDataAesGcm` - Decrypt individual rows
- `EncryptTableRowsAesGcm` - Table-Valued Function for bulk processing

✅ **Enhanced PowerBuilder Integration**
- Simplified password-based workflows
- Korean character support
- Single-command table backup/restore

## Installation Notes

- **UNSAFE Permission Set**: Required for dynamic SQL execution in RestoreEncryptedTable
- **CLR Enabled**: Script automatically enables CLR integration
- **Trusted Assemblies**: Script handles assembly trust configuration
- **Comprehensive**: Single script installs all functions from the library

## Usage Examples

The `example.sql` file demonstrates:
- Complete table encryption/decryption workflows
- Row-by-row processing examples
- Password hashing for authentication
- Diffie-Hellman key exchange
- PowerBuilder integration patterns

Perfect for Korean small business applications using PowerBuilder!