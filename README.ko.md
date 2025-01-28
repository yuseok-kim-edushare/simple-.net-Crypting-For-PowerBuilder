# simple-.NET-Crypting-For-PB
이 프로젝트는 Powerbuilder 프로그래밍에 사용하기 위한 간단한 암호화, 복호화 등을 위한 .NET dll을 생성하는 프로젝트입니다.

*다른 언어로 읽기: [English](README.md)*

[![CI tests](https://github.com/yuseok-kim-edushare/simple-.net-Crypting-For-PowerBuilder/actions/workflows/ci.yaml/badge.svg)](https://github.com/yuseok-kim-edushare/simple-.net-Crypting-For-PowerBuilder/actions/workflows/ci.yaml)

## 목적
- Powerbuilder 프로그래밍에 사용하기 위한 간단한 암호화, 복호화 등을 위한 .NET dll을 생성하는 프로젝트입니다.
- 또한 SQL Server에서 사용하기 위한 어셈블리도 개발합니다.

## 정보
- 대상 프레임워크
  - .NET Framework 4.8.1
    - PB 버전 간의 호환성을 보장합니다.
    - 또한 SQL Server CLR 어셈블리도 제공합니다.
    - [.NET Framework 4.8.1 다운로드](https://dotnet.microsoft.com/ko-kr/download/dotnet-framework/net481)
      - DLL 실행을 위해 런타임을 설치해야 하고, Powerbuilder 개발자는 Windows PC에 SDK를 설치해야 합니다.
      - 필요한 환경: windows 10 21H2 이상 또는 windows server 2022 이상
  - .NET 8
    - Powerbuilder 2022 R3 최신 버전 또는 2025 이후 버전
    - [.NET 8 다운로드](https://dotnet.microsoft.com/ko-kr/download/dotnet/8.0)
      - 클라이언트는 런타임을 설치해야 하고, Powerbuilder 개발자는 SDK를 설치해야 합니다.
      - 우리는 Windows용 .NET Desktop Runtime이 필요합니다.
      - 필요한 환경: windows server 2012 이상 또는 windows 10 이상

- **[예제 코드](https://github.com/yuseok-kim-edushare/simple-.net-Crypting-For-PowerBuilder/tree/main/Examples)**
  - [PB with .NET 4.8.1 DLL](https://github.com/yuseok-kim-edushare/simple-.net-Crypting-For-PowerBuilder/tree/main/Examples/Powerbuilder-Net%204.8)
  - [PB with .NET 8 DLL](https://github.com/yuseok-kim-edushare/simple-.net-Crypting-For-PowerBuilder/tree/main/Examples/Powerbuilder-Net%208) - PB 2025 버전으로 생성할 예정입니다. (PB2025는 25년 상반기 출시 예정)
  - [MS-SQL with .NET 4.8.1 DLL](https://github.com/yuseok-kim-edushare/simple-.net-Crypting-For-PowerBuilder/tree/main/Examples/SQL-server-Net%204.8)

## Powerbuilder에서 몇몇 암호화 옵션이 구현되지 않아서 이 프로젝트를 수행하게 되었습니다.
예를 들어
- AES Encryption with GCM mode
  - 현대 대칭 암호화에서 주요한 옵션 중 하나입니다.
- Diffie Hellman Key Exchange or else equivalent
  - 민감한 데이터를 전송하기 위한 2차 보안입니다.
  - (TLS에서 이미 사용되지만 이중 암호화를 위해 사용됩니다.)
- Bcrypt Password encoding or and so on to PW encryption
  - 이것은 단방향 암호화를 잘 하는 것에 대한 문제입니다.
  - SHA-512 해시 함수를 1회만 사용하는 것은 충분한 보안이 아닙니다.
  
## 마일스톤

- 🌧️ 예제 코드 생성
  - 🌧️ Powerbuilder with .NET 4.8.1 DLL
  - ✅ MS-SQL with .NET 4.8.1 DLL
  - Powerbuilder with .NET 8 DLL
