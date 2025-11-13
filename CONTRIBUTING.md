# Contributing to NuGet Update Bot

Thank you for your interest in contributing to NuGet Update Bot! This document provides guidelines and instructions for contributing.

## Code of Conduct

Be respectful and constructive in all interactions with the community.

## How Can I Contribute?

### Reporting Bugs

Before creating bug reports, please check existing issues to avoid duplicates. When creating a bug report, include:

- A clear and descriptive title
- Steps to reproduce the issue
- Expected behavior
- Actual behavior
- Your environment (OS, .NET version, etc.)
- Sample project files if applicable

### Suggesting Enhancements

Enhancement suggestions are tracked as GitHub issues. When creating an enhancement suggestion, include:

- A clear and descriptive title
- Detailed description of the proposed functionality
- Why this enhancement would be useful
- Possible implementation approach (optional)

### Pull Requests

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Add or update tests as needed
5. Ensure all tests pass (`dotnet test`)
6. Commit your changes (`git commit -m 'Add amazing feature'`)
7. Push to the branch (`git push origin feature/amazing-feature`)
8. Open a Pull Request

## Development Setup

### Prerequisites

- .NET 9.0 SDK or later
- Git
- Your favorite IDE (Visual Studio, VS Code, Rider, etc.)

### Building the Project

```bash
# Clone the repository
git clone https://github.com/yourusername/nuget-update-bot.git
cd nuget-update-bot

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test
```

### Project Structure

```
nuget-update-bot/
├── src/
│   ├── NugetUpdateBot/           # Main application
│   └── NugetUpdateBot.Tests/     # Test project
├── docs/                          # Documentation
├── Directory.Build.props          # MSBuild properties
├── Directory.Packages.props       # Central Package Management
├── NugetUpdateBot.sln            # Solution file
└── README.md                      # Main documentation
```

## Coding Guidelines

### Style

- Follow standard C# naming conventions
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Keep methods focused and concise
- Follow the existing code style in the project

### Testing

- Write unit tests for new functionality
- Ensure all existing tests pass
- Aim for meaningful test coverage
- Use descriptive test names that explain what is being tested

### Commits

- Write clear, concise commit messages
- Use present tense ("Add feature" not "Added feature")
- Reference issues and pull requests when applicable
- Keep commits focused on a single change

## Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run specific test
dotnet test --filter "FullyQualifiedName~YourTestName"
```

## Documentation

- Update documentation for any user-facing changes
- Add XML comments for public APIs
- Update README.md if adding new features
- Add examples in [docs/EXAMPLES.md](docs/EXAMPLES.md) for new functionality

## Release Process

Maintainers handle releases. The process includes:

1. Update version numbers
2. Update CHANGELOG.md
3. Create a release tag
4. Build and publish NuGet package
5. Create GitHub release with notes

## Questions?

If you have questions about contributing, feel free to:

- Open an issue for discussion
- Check existing documentation
- Reach out to maintainers

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

Thank you for contributing!
