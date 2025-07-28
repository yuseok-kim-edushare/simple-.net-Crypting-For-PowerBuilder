# SQL Server CLR Examples - Complete Installation Guide

This folder contains the unified, comprehensive SQL CLR installation and usage examples for SecureLibrary-SQL, updated to include all functionality from PR #61.

## Quick Start

1. **Install**: Run `install.sql` 
   - Update the DLL path in the script
   - Update the target database name
   - Execute the script to install all functions

2. **Test**: Run `example.sql` for basic examples, or `practical-examples.sql` for enhanced developer-friendly demonstrations

3. **Uninstall**: Run `uninstall.sql` if needed

## Files Overview

### Installation and Setup
- **`install.sql`** - Complete installation script for the SecureLibrary-SQL assembly
- **`uninstall.sql`** - Removes the assembly and all related objects

### Examples and Testing  
- **`example.sql`** - Basic usage examples for all CLR functions
- **`practical-examples.sql`** - **NEW!** Enhanced, developer-friendly examples addressing Issue #65
- **`../SqlServerCLR/Deploy/TestScripts.sql`** - Basic test script
- **`../SqlServerCLR/Deploy/ImprovedTestScripts.sql`** - **NEW!** Improved test script

## What's New (PR #61)

✅ **Password-Based Table Encryption**
- `EncryptXmlWithPassword` - Encrypt entire tables with a password
- `RestoreEncryptedTable` - Universal stored procedure to restore any encrypted table

✅ **Enhanced PowerBuilder Integration**
- Simplified password-based workflows
- Korean character support
- Single-command table backup/restore

## Developer Experience Improvements (Issue #65)

The new `practical-examples.sql` addresses developer feedback by providing:

### ✅ **Dynamic Table Creation**
- No need to pre-define table structures
- Uses `SELECT INTO` for automatic temp table creation
- Works with any table schema dynamically

### ✅ **Schema Comparison**
- Uses `INFORMATION_SCHEMA` views for schema validation
- Automatic structure comparison between original and decrypted tables
- No manual column configuration required

### ✅ **Identical Query Support**
- Same `SELECT` queries work on both original and decrypted data
- Proves encryption/decryption maintains data integrity
- No complex casting needed for basic operations

### ✅ **Real-World Use Cases**
- Database backup encryption
- Sensitive data protection  
- Data migration scenarios
- PowerBuilder integration patterns

## Installation Notes

- **UNSAFE Permission Set**: Required for dynamic SQL execution in RestoreEncryptedTable
- **CLR Enabled**: Script automatically enables CLR integration
- **Trusted Assemblies**: Script handles assembly trust configuration
- **Comprehensive**: Single script installs all functions from the library

## Usage Examples

The `example.sql` file demonstrates:
- Complete table encryption/decryption workflows
- Individual data encryption examples
- Password hashing for authentication
- Diffie-Hellman key exchange
- PowerBuilder integration patterns

The `practical-examples.sql` file demonstrates:
- 3-line table encryption approach
- Dynamic restoration without pre-definition
- Schema comparison and validation
- Performance metrics and real-world use cases

Perfect for Korean small business applications using PowerBuilder!