# Contributing to simple-.NET-Crypting-For-PB

First off, thank you for considering contributing to this project! This is a cryptographic library targeting PowerBuilder and SQL Server integration, so we need to be especially careful with code quality and security.

## Code of Conduct

By participating in this project, you are expected to uphold our Code of Conduct (be respectful, professional, and collaborative).

## Security Considerations

Given this is a cryptographic library:
- Do not weaken any cryptographic parameters
- Do not remove security validations
- Always validate inputs
- Report security issues privately
- Do not commit sensitive keys or credentials

## Development Process

1. Fork the repository
2. Create a new branch for your feature/fix
3. Write your code
4. Add tests
5. Run the test suite
6. Submit a Pull Request

### Prerequisites

- Visual Studio 2022 or later
- .NET Framework 4.8.1 SDK
- .NET 8.0 SDK
- PowerBuilder (for testing PB integration)
- SQL Server (for testing SQL CLR integration)

### Building the Project

The project can be built using either Visual Studio or command line:

```bash
# Restore NuGet packages
nuget restore simple-.net-Crypting-For-PowerBuilder.sln

# Build solution
msbuild "simple-.net-Crypting-For-PowerBuilder.sln" /p:Configuration=Debug
```

### Running Tests

Tests must pass before any PR will be accepted:

```bash
# Run .NET Framework tests
.\testrunner\NUnit.ConsoleRunner.3.19.1\tools\nunit3-console.exe net481pb\bin\Debug\SecureLibrary.dll
.\testrunner\NUnit.ConsoleRunner.3.19.1\tools\nunit3-console.exe net481SQL-server\bin\Debug\SecureLibrary.SQL.dll

# Run .NET 8 tests
dotnet test NET8/NET8.csproj --configuration Debug
```

## Pull Request Process

1. Update the README.md with details of changes if needed
2. Update any example code or documentation
3. Add tests for new functionality
4. Ensure CI passes (GitHub Actions will run automatically)
5. Get approval from maintainers

### PR Requirements

- Must target the `main` branch
- Must include tests
- Must maintain or improve code coverage
- Must follow existing code style
- Must not introduce security vulnerabilities
- Must include documentation updates if needed

## Release Process

Releases are automated through GitHub Actions. The workflow:

1. Merges to main trigger CI
2. Successful CI triggers CD
3. CD creates release with versioned DLLs

## Documentation

When contributing, please update:
- Code comments
- README.md if needed
- Example code if relevant
- XML documentation for public APIs

## Questions?

Feel free to open an issue for:
- Feature requests
- Bug reports
- Documentation improvements
- General questions

For security issues, please report privately to the maintainers.

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
