# simple-.NET-Crypting-For-PB
This Project will create .NET dll to implement crypto object en|decrypter for Powerbuilder

## Purpose
- Implement simple implementation dll for Powerbuilder Programming
- Also Supplement Assembly for SQL Server will used in SP for Powerbuilder Clients

## Cause of Not Implemented some cryptographic options In Powerbuilder
For Example
- AES Encryption with GCM mode
  - This is important to secure using Symmetric Encrypting Function in morden
- Diffie Hellman Key Exchange or else equivalent
  - for 2nd layer securing to transport sensitive data 
  - (In TLS already used, but bi-layered encrypt for more secure handling)
- Bcrypt Password encoding or and so on to PW encryption
  - this is the matter of one-way password encryption well
  - just using 1 pass of hash function, eg. SHA-512 isn't secure enough

## Informations
- Target Frameworks
  - .NET Framework 4.8.1
    - This is ensure Cross compatibility between PB versions
  - .NET 8
    - This is for Powerbuilder 2022 R3 Latest or 2025
- Implemented Methods
  - Not yet
  
## Milestone
- 🌧️Create .NET 4.8.1 DLL for Powerbuilder
  - ✅ AES-256-CBC with .NET Framework Native API
    - this isn't needed job but practice for me
  - AES-GCM with using Windows's CGN API DLL
  - 🌧️ Diffie Helman with using .NET Framework native api
  - ✅ Bcrypt Password hashing with Bcrypt.NET-NEXT Project
- Create .NET 4.8.1 DLL for MS SQL Server
  - AES-GCM with using Windows's CGN API DLL
  - Diffie Helman with using .NET Framework native api
  - Bcrypt Password hashing with Bcrypt.NET-NEXT Project
- Create .NET 8 DLL for Powerbuilder
  - AES-GCM and DH with .NET native api
  - Bcrypt Password hashing with Bcrypt.NET-NEXT
- Make Example code and app for user
  - 🌧️Powerbuilder with .NET 4.8.1 DLL
  - MS-SQL with .NET 4.8.1 DLL
  - Powerbuilder with .NET 8 DLL
