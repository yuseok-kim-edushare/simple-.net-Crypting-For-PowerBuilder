# simple-.NET-Crypting-For-PB
This Project will create .NET dll to implement crypto object en|decrypter for Powerbuilder

*Read this in other languages: [한국어](README.ko.md)*

## Purpose
- Implement simple implementation dll for Powerbuilder Programming
- Also Supplement Assembly for SQL Server will used in SP for Powerbuilder Clients

## Cause of Not Implemented some cryptographic options In Powerbuilder
For Example
- AES Encryption with GCM mode
  - This is important to secure using Symmetric Encrypting Function in morden
  - Not native supported in .NET Framework 4.8.1
    - Fallback to AES-256-CBC (this will ensure cross compatibility between PB and SQL Server)
    - also import dll of windows's CGN (bcrypt.dll), this make dll can use aes-gcm
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
    - Also for SQL Server Assembly
  - .NET 8
    - This is for Powerbuilder 2022 R3 Latest or 2025

- **[Example Code](https://github.com/yuseok-kim-edushare/simple-.net-Crypting-For-PowerBuilder/tree/main/Examples)**
  - [PB with .NET 4.8.1 DLL](https://github.com/yuseok-kim-edushare/simple-.net-Crypting-For-PowerBuilder/tree/main/Examples/Powerbuilder-Net%204.8)
  - [PB with .NET 8 DLL](https://github.com/yuseok-kim-edushare/simple-.net-Crypting-For-PowerBuilder/tree/main/Examples/Powerbuilder-Net%208) -Not Created (will create with PB 2025)
  - [MS-SQL with .NET 4.8.1 DLL](https://github.com/yuseok-kim-edushare/simple-.net-Crypting-For-PowerBuilder/tree/main/Examples/MS-SQL-Net%204.8)

  
## Milestone

- 🌧️ Make Example code and app for user
  - 🌧️ Powerbuilder with .NET 4.8.1 DLL
  - 🌧️ MS-SQL with .NET 4.8.1 DLL
  - Powerbuilder with .NET 8 DLL

- 🌧️ Integration Test
  - 🌧️ PB with .NET 4.8.1 DLL
    - ✅ AES-256-CBC
    - ✅ AES-GCM
    - ✅ Diffie Helman in self
    - Diffie Helman to SQL Server
    - ✅ Bcrypt Password hashing
  - PB with .NET 8 DLL
    - AES-GCM
    - Diffie Helman in self
    - Diffie Helman to SQL Server
    - Bcrypt Password hashing
  - MS-SQL with .NET 4.8.1 DLL
    - AES-256-CBC
    - AES-GCM
    - Diffie Helman in self
    - Diffie Helman to PB
      - PB with .NET 4.8.1 DLL
      - PB with .NET 8 DLL
    - Bcrypt Password hashing


