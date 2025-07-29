# simple-.NET-Crypting-For-PB
This Project will create .NET dll for Powerbuilder (Bcrypt PW hash, AES-GCM and ECDH) also for MS-SQL

*Read this in other languages: [한국어](README.ko.md)*

[![CI tests](https://github.com/yuseok-kim-edushare/simple-.net-Crypting-For-PowerBuilder/actions/workflows/ci.yaml/badge.svg)](https://github.com/yuseok-kim-edushare/simple-.net-Crypting-For-PowerBuilder/actions/workflows/ci.yaml)

## Purpose
- AES-GCM, Bcrypt PW hash, ECDH 
- Implement simple dll for Powerbuilder Programming
- Also Supplement Assembly for SQL Server will used in SP for Powerbuilder Clients

## Informations
- Target Frameworks
  - .NET Framework 4.8.1
    - This is ensure Cross compatibility between PB versions
    - Also for SQL Server CLR Assembly
    - [Download .NET Framework 4.8.1](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net481)
      - to run DLL, runtime needs to be installed, powerbuilder dev needs SDK installed windows PC
      - Required windows 10 21H2 or later and windows server 2022 or later
  - .NET 8
    - This is for Powerbuilder 2022 R3 Latest or 2025
    - [Download .NET 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
      - to run DLL, runtime needs to be installed, powerbuilder dev needs SDK installed windows PC
      - we need .NET Desktop Runtime for windows
      - Required windows server 2016 or later and windows 10 (LTSC 1607+ or 22H2) or laters

- **[Example Code](https://github.com/yuseok-kim-edushare/simple-.net-Crypting-For-PowerBuilder/tree/main/Examples)**
  - [PB with .NET 4.8.1 DLL](https://github.com/yuseok-kim-edushare/simple-.net-Crypting-For-PowerBuilder/tree/main/Examples/Powerbuilder-Net%204.8)
  - [PB with .NET 8 DLL](https://github.com/yuseok-kim-edushare/simple-.net-Crypting-For-PowerBuilder/tree/main/Examples/Powerbuilder-Net%208)
  - [MS-SQL with .NET 4.8.1 DLL](https://github.com/yuseok-kim-edushare/simple-.net-Crypting-For-PowerBuilder/tree/main/Examples/SQL-server-Net%204.8)
      - SQL server target dll also support table encryption and decryption, but this feature is under development
      - Powerbuilder can handle table but not implemented creation for PB (we need dynamic datawindow for decrypted data using)

## Cause of Not Implemented some cryptographic options In Powerbuilder
For Example
- AES Encryption with GCM mode
  - This is important to secure using Symmetric Encrypting Function in morden
- ECDH Key Exchange
  - for 2nd layer securing to transport sensitive data 
  - (In TLS already used, but bi-layered encrypt for more secure handling)
- Bcrypt Password encoding
  - this is the matter of one-way password encryption well
  - just using 1 pass of hash function, eg. SHA-512 isn't secure enough

## Build Information
You can build in supported windows environment,
and you can check required commands in
[github actions](https://github.com/yuseok-kim-edushare/simple-.net-Crypting-For-PowerBuilder/tree/main/.github/workflows)
you should see ci.yaml and cd.yaml

# Dependancy
- .NET or .NET Framework
- Windows Cryptography API: Next Generation
- [Bcrypt.NET-NEXT](https://github.com/BcryptNet/bcrypt.net)
