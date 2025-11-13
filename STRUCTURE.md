# Repository Structure

This document describes the organization of the NuGet Update Bot repository.

## Directory Layout

```
nuget-update-bot/
├── src/                           # Source code
│   ├── NugetUpdateBot/           # Main application
│   └── NugetUpdateBot.Tests/     # Test project
├── docs/                          # Documentation
│   ├── CONFIGURATION.md          # Configuration guide
│   ├── EXAMPLES.md               # Usage examples
│   ├── GETTING_STARTED.md        # Getting started guide
│   └── TROUBLESHOOTING.md        # Troubleshooting guide
├── .editorconfig                  # Editor configuration
├── .gitignore                     # Git ignore rules
├── CONTRIBUTING.md                # Contribution guidelines
├── Directory.Build.props          # MSBuild properties
├── Directory.Packages.props       # Central Package Management
├── LICENSE                        # MIT License
├── NugetUpdateBot.sln            # Solution file
└── README.md                      # Main documentation
```

## Key Files

### Root Level
- **NugetUpdateBot.sln**: Solution file containing all projects
- **README.md**: Main documentation and quick start guide
- **LICENSE**: MIT License file
- **CONTRIBUTING.md**: Guidelines for contributors
- **Directory.Build.props**: Shared MSBuild properties for all projects
- **Directory.Packages.props**: Central Package Management configuration
- **.editorconfig**: Code style and formatting rules
- **.gitignore**: Git ignore patterns

### Source Code (`src/`)
- **NugetUpdateBot/**: Main application project
  - Console application for updating NuGet packages
  - Services, handlers, models, configuration, and validation
  
- **NugetUpdateBot.Tests/**: Test project
  - Integration tests
  - Unit tests
  - Test utilities

### Documentation (`docs/`)
- **CONFIGURATION.md**: Detailed configuration options
- **EXAMPLES.md**: Usage examples and scenarios
- **GETTING_STARTED.md**: Installation and setup guide
- **TROUBLESHOOTING.md**: Common issues and solutions

## Build and Development

### Building
```bash
dotnet build NugetUpdateBot.sln
```

### Running Tests
```bash
dotnet test NugetUpdateBot.sln
```

### Running the Application
```bash
dotnet run --project src/NugetUpdateBot/NugetUpdateBot.csproj -- <command> <args>
```

## Best Practices Followed

1. **Separation of Concerns**: Source code separated from documentation and configuration
2. **Standard .NET Structure**: Solution file at root, source in `src/` directory
3. **Central Package Management**: Version management in `Directory.Packages.props`
4. **Documentation**: Comprehensive docs in dedicated directory
5. **Open Source Ready**: LICENSE, CONTRIBUTING.md, and clear README at root
6. **Editor Support**: `.editorconfig` for consistent code style
7. **Version Control**: Proper `.gitignore` configuration

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for detailed contribution guidelines.
