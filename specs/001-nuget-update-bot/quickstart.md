# Quick Start Guide: NuGet Update Bot

## Installation

### Prerequisites
- .NET 8.0 SDK or later
- Internet connection for accessing NuGet.org

### Build from Source
```bash
# Clone the repository
git clone [repository-url]
cd NugetUpdateBot

# Build the project
dotnet build

# Run the tool
dotnet run -- scan --project ./MyProject.csproj
```

### Install as Global Tool (Future)
```bash
# Once published to NuGet
dotnet tool install -g nuget-update-bot
```

## Basic Usage

### 1. Scan for Outdated Packages

Check which packages have updates available:

```bash
# Scan a specific project
nuget-update-bot scan --project ./src/MyApp/MyApp.csproj

# Scan all projects in a directory
nuget-update-bot scan --project ./src/

# Include pre-release versions
nuget-update-bot scan --project ./MyApp.csproj --include-prerelease

# Force fresh data (bypass cache)
nuget-update-bot scan --project ./MyApp.csproj --no-cache
```

**Example Output:**
```
Scanning project: ./src/MyApp/MyApp.csproj
Found 15 package references

Found 3 outdated packages:
--------------------------------------------------------------------------------
Package                                    Current    →     Latest
--------------------------------------------------------------------------------
Newtonsoft.Json                            12.0.3     →     13.0.3
Serilog                                    2.10.0     →     3.1.1
Microsoft.Extensions.Logging               7.0.0      →     8.0.0
```

### 2. Update Packages

Apply updates to outdated packages:

```bash
# Preview updates without applying (dry run)
nuget-update-bot update --project ./MyApp.csproj --dry-run

# Apply all updates
nuget-update-bot update --project ./MyApp.csproj

# Update with specific policy (patch updates only)
nuget-update-bot update --project ./MyApp.csproj --policy patch

# Exclude specific packages from updates
nuget-update-bot update --project ./MyApp.csproj --exclude Newtonsoft.Json Legacy.Package

# Verbose output for debugging
nuget-update-bot update --project ./MyApp.csproj --verbose
```

### 3. Generate Reports

Create detailed update reports:

```bash
# Console output (default)
nuget-update-bot report --project ./MyApp.csproj

# JSON format for automation
nuget-update-bot report --project ./MyApp.csproj --format json

# Save to file
nuget-update-bot report --project ./MyApp.csproj --format json --output updates.json

# Include up-to-date packages in report
nuget-update-bot report --project ./MyApp.csproj --include-up-to-date
```

**JSON Report Example:**
```json
{
  "generatedAt": "2025-11-13T10:00:00Z",
  "projectPath": "./MyApp.csproj",
  "outdatedPackages": [
    {
      "name": "Newtonsoft.Json",
      "currentVersion": "12.0.3",
      "latestVersion": "13.0.3",
      "updateType": "major"
    }
  ],
  "summary": {
    "totalPackages": 15,
    "outdatedCount": 3,
    "majorUpdates": 1,
    "minorUpdates": 1,
    "patchUpdates": 1
  }
}
```

## Configuration

### Configuration File (Optional)

Create `appsettings.json` in the application directory:

```json
{
  "UpdatePolicy": "Minor",
  "ExcludePackages": [
    "Microsoft.EntityFrameworkCore",
    "Newtonsoft.Json"
  ],
  "IncludePrerelease": false,
  "MaxParallelism": 5,
  "UpdateRules": [
    {
      "Pattern": "Microsoft.*",
      "Policy": "Minor"
    },
    {
      "Pattern": "System.Text.*",
      "Policy": "Patch"
    }
  ]
}
```

See `appsettings.example.json` for a complete example with documentation.

### Environment Variables

Override settings using environment variables:

```bash
export NUGET_BOT_UPDATE_POLICY=Minor
export NUGET_BOT_INCLUDE_PRERELEASE=false
export NUGET_BOT_MAX_PARALLELISM=5
export NUGET_BOT_EXCLUDE_PACKAGES="Microsoft.EntityFrameworkCore,Newtonsoft.Json"
```

### Priority Order

1. Command-line arguments (highest priority)
2. Environment variables
3. Configuration file
4. Default values

## Common Scenarios

### Update Only Security Patches
```bash
nuget-update-bot update --project ./MyApp.csproj --policy patch
```

### Check Multiple Projects in Solution
```bash
# Scan entire solution directory
nuget-update-bot scan --project ./

# Update all projects
nuget-update-bot update --project ./
```

### Automated CI/CD Integration
```bash
#!/bin/bash
# In your CI pipeline

# Generate report
nuget-update-bot report --project ./ --format json --output updates.json

# Check if updates are available
if [ $(cat updates.json | jq '.summary.outdatedCount') -gt 0 ]; then
  echo "Updates available!"
  # Create PR or notification
fi
```

### Conservative Update Strategy
```bash
# Only patch updates, exclude major dependencies
nuget-update-bot update \
  --project ./MyApp.csproj \
  --policy patch \
  --exclude Microsoft.AspNetCore.* EntityFramework.*
```

## Command Reference

### Global Options
- `--help, -h`: Show help information
- `--version`: Show version information

### scan Command
- `--project, -p` (required): Project file or directory path
- `--include-prerelease`: Include pre-release versions
- `--verbose, -v`: Enable verbose output
- `--no-cache`: Bypass cache for fresh data

### update Command
- `--project, -p` (required): Project file or directory path
- `--dry-run`: Preview without applying changes
- `--policy`: Update policy (patch|minor|major)
- `--exclude, -e`: Packages to exclude (multiple allowed)
- `--verbose, -v`: Enable verbose output

### report Command
- `--project, -p` (required): Project file or directory path
- `--format, -f`: Output format (console|json)
- `--output, -o`: Output file path
- `--include-up-to-date`: Include current packages

## Exit Codes

- `0`: Success
- `1`: General error
- `2`: Invalid arguments
- `3`: File not found
- `4`: Network error
- `5`: Parse error

## Troubleshooting

### Issue: "Network error: Unable to connect to NuGet.org"
**Solution**: Check internet connection and proxy settings. The tool requires access to https://api.nuget.org.

### Issue: "Parse error: Invalid project file"
**Solution**: Ensure the project file is valid XML and uses SDK-style format (.NET Core/5+).

### Issue: "No updates found" when updates exist
**Solution**: Try `--no-cache` flag to bypass the 30-minute cache.

### Issue: Updates not applying
**Solution**: Check file permissions and ensure project files are not read-only.

## Best Practices

1. **Always use --dry-run first** to preview changes before applying updates
2. **Start with patch updates** to minimize risk
3. **Exclude critical dependencies** that require careful testing
4. **Use configuration files** for consistent team settings
5. **Integrate into CI/CD** for automated dependency monitoring
6. **Review major updates** carefully for breaking changes
7. **Test after updates** even for minor version changes

## Performance Tips

- Cache is enabled by default (30 minutes) for faster repeated scans
- Parallel processing handles up to 5 packages simultaneously
- Use `--verbose` only when debugging to reduce output overhead
- For large solutions, scan individual projects for faster results

## Support

For issues, feature requests, or contributions:
- GitHub: [repository-url]/issues
- Documentation: [repository-url]/wiki

## License

[License information]