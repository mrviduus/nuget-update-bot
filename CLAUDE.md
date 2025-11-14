# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

NuGet Update Bot is a .NET CLI tool that automatically manages NuGet package updates in .NET projects. It supports both traditional PackageReference and Central Package Management (CPM) patterns.

## Build and Test Commands

### Build
```bash
dotnet build NugetUpdateBot.sln
```

### Run Tests
```bash
# All tests
dotnet test NugetUpdateBot.sln

# Specific test
dotnet test --filter "FullyQualifiedName~TestName"

# With verbosity
dotnet test --verbosity normal
```

### Run Application Locally
```bash
# Scan command
dotnet run --project src/NugetUpdateBot/NugetUpdateBot.csproj -- scan --project test.csproj

# Update command with dry-run
dotnet run --project src/NugetUpdateBot/NugetUpdateBot.csproj -- update --project test.csproj --dry-run

# Report command
dotnet run --project src/NugetUpdateBot/NugetUpdateBot.csproj -- report --project test.csproj --format json
```

## Architecture

### Command Pattern
The application uses a command handler pattern with three main commands:
- **ScanCommandHandler**: Scans projects for outdated packages
- **UpdateCommandHandler**: Updates packages in project files
- **ReportCommandHandler**: Generates reports of available updates

All command handlers follow dependency injection and Single Responsibility Principle.

### Service Layer Architecture

**PackageScannerService** (`src/NugetUpdateBot/Services/PackageScannerService.cs`)
- Parses .csproj files to extract PackageReference elements
- **CPM Support**: Automatically detects CPM and reads package versions from Directory.Packages.props
- Queries NuGet.org API for available versions
- Compares current vs. latest versions
- Uses semaphore-based throttling for API calls

**PackageUpdaterService** (`src/NugetUpdateBot/Services/PackageUpdaterService.cs`)
- Detects Central Package Management (CPM) usage
- Updates package versions in .csproj OR Directory.Packages.props
- CPM Detection: Checks for `ManagePackageVersionsCentrally` property or if 80%+ of PackageReferences lack Version attributes
- Searches up to 5 directory levels to find Directory.Packages.props

**PolicyEngineService** (`src/NugetUpdateBot/Services/PolicyEngineService.cs`)
- Applies update policies (Major, Minor, Patch)
- Filters updates based on configuration rules
- Handles package exclusion patterns

**ConfigurationLoaderService** (`src/NugetUpdateBot/Configuration/ConfigurationLoaderService.cs`)
- Loads JSON configuration files
- Merges configuration with command-line options
- Supports package-specific update rules

### Central Package Management (CPM)

The tool automatically detects and handles CPM projects. When CPM is detected:
1. PackageUpdaterService searches parent directories for Directory.Packages.props
2. Updates are applied to Directory.Packages.props instead of .csproj
3. Both files are backed up before updates
4. Validation occurs on both files after updates

CPM detection checks:
- Presence of `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>`
- Percentage of PackageReferences without Version attributes (>80% = CPM)

### Configuration Model

Configuration is defined in `BotConfiguration` record:
- **UpdatePolicy**: Major, Minor, or Patch
- **ExcludePackages**: List of package name patterns
- **IncludePrerelease**: Boolean for prerelease version handling
- **UpdateRules**: Package-specific rules that override default policy

### Exit Codes
- `0`: Success (no updates available)
- `1`: Updates found
- `-1`: Error occurred

## Project Structure

```
src/NugetUpdateBot/
├── Configuration/      # Config loading and update rules
├── Handlers/          # Command handlers (Scan, Update, Report)
├── Models/            # Data models (PackageReference, UpdateCandidate, etc.)
├── Reporting/         # Report generation and formatting
├── Services/          # Core business logic (Scanner, Updater, PolicyEngine)
├── Validation/        # Input validation
└── Program.cs         # Entry point with System.CommandLine setup
```

## Important Implementation Details

### Backup Strategy
Every update operation creates timestamped backups:
- Traditional: `MyProject.csproj.backup.20250113-143022`
- CPM: Both `.csproj.backup.*` and `Directory.Packages.backup.*.props`

### NuGet API Integration
Uses `NuGet.Protocol` library:
- Repository: `https://api.nuget.org/v3/index.json`
- Throttled with SemaphoreSlim (default: 5 concurrent requests)
- Supports caching and cache bypass with `--no-cache`

### XML Manipulation
All .csproj and Directory.Packages.props modifications use `System.Xml.Linq`:
- Preserves formatting and structure
- Validates XML after updates
- Automatically restores backup if validation fails

## Development Notes

### Central Package Management
This repository uses CPM. Package versions are defined in `Directory.Packages.props` at the root. When adding new dependencies, add them without version numbers in `.csproj` files:
```xml
<PackageReference Include="Newtonsoft.Json" />
```
Then add/update version in `Directory.Packages.props`:
```xml
<PackageVersion Include="Newtonsoft.Json" Version="13.0.3" />
```

### Target Framework
- Target: .NET 9.0
- Language: C# 12.0
- Nullable reference types enabled
- TreatWarningsAsErrors: false (globally in Directory.Build.props)
- TreatWarningsAsErrors: true (in NugetUpdateBot.csproj for main application)

### Testing
Tests are integration-focused and create temporary project files. They test:
- Traditional PackageReference updates
- CPM detection and updates
- End-to-end workflows (scan → update → report)
- Error handling and backup/restore scenarios

### Packaging and Publishing
The project is configured as a .NET global tool:
```bash
# Pack the tool
dotnet pack src/NugetUpdateBot/NugetUpdateBot.csproj

# Install locally for testing
dotnet tool install --global --add-source ./src/NugetUpdateBot/bin/Release NugetUpdateBot

# Uninstall
dotnet tool uninstall --global NugetUpdateBot
```

Tool configuration in NugetUpdateBot.csproj:
- **PackAsTool**: true
- **ToolCommandName**: nuget-update-bot
- **Version**: Managed in project file

### Dependency Injection Setup
The application uses manual dependency injection in [Program.cs:15-30](src/NugetUpdateBot/Program.cs#L15-L30). All services are constructed once at startup:
1. Core dependencies (NuGet repository, throttler, logger)
2. Services (validator, scanner, updater, policy engine, etc.)
3. Command handlers (injected with all required services)

This approach keeps dependencies explicit and aids in testing through `InternalsVisibleTo` assembly attribute.
