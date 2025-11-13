# NuGet Update Bot

A simple tool that updates your .NET packages automatically.

## What Does It Do?

Imagine you have a toy box with old toys. This tool checks if there are newer, better versions of your toys and helps you replace them!

For your .NET projects, it:
- Checks which packages (libraries) you're using
- Finds newer versions available
- Updates them for you
- Creates a backup first (so you can undo if needed)
- **Works with both traditional projects and Central Package Management (CPM)**

## Installation

```bash
dotnet tool install --global NugetUpdateBot
```

## Basic Usage

### 1. Check What Needs Updating (Scan)

Just like checking which toys are old:

```bash
nuget-update-bot scan MyProject.csproj
```

This shows you what packages can be updated, but doesn't change anything yet.

### 2. Update Everything (Update)

This actually updates your packages:

```bash
nuget-update-bot update MyProject.csproj
```

Don't worry! It creates a backup first.

### 3. See Before Changing (Dry Run)

Want to see what would happen without actually doing it?

```bash
nuget-update-bot update MyProject.csproj --dry-run
```

This is like pretending to clean your room - you see what needs to be done, but nothing actually changes!

### 4. Get a Report

Want a nice report file?

```bash
nuget-update-bot report MyProject.csproj --output report.json --format json
```

## Simple Examples

### Example 1: Just Check
```bash
# See what can be updated
nuget-update-bot scan MyProject.csproj
```

Output:
```
Package: Newtonsoft.Json
Current: 12.0.1
Latest: 13.0.3
Can update!
```

### Example 2: Update Everything
```bash
# Update all packages
nuget-update-bot update MyProject.csproj
```

Output:
```
Updating Newtonsoft.Json from 12.0.1 to 13.0.3...
Done! Backup saved to MyProject.csproj.backup.20250113-143022
```

### Example 3: Be Careful (Only Small Updates)
```bash
# Only update tiny changes (like 13.0.1 to 13.0.2)
nuget-update-bot update MyProject.csproj --config myconfig.json
```

With this config file (`myconfig.json`):
```json
{
  "UpdatePolicy": "Patch",
  "IncludePrerelease": false,
  "ExcludePackages": []
}
```

## Configuration File

Create a file called `config.json`:

```json
{
  "UpdatePolicy": "Major",
  "IncludePrerelease": false,
  "ExcludePackages": ["MySpecialPackage"]
}
```

### What Does Each Setting Mean?

| Setting | What It Means | Example |
|---------|---------------|---------|
| `"Major"` | Update everything, even big changes | 1.0.0 → 2.0.0 ✅ |
| `"Minor"` | Update medium changes only | 1.0.0 → 1.5.0 ✅ |
| `"Patch"` | Update tiny changes only | 1.0.0 → 1.0.1 ✅ |
| `IncludePrerelease` | Include beta/test versions | true or false |
| `ExcludePackages` | Packages to never update | List of names |

## All Commands

### Scan Command
```bash
nuget-update-bot scan <project-file> [options]
```

**Options:**
- `--config <file>` - Use a config file
- `--include-prerelease` - Include beta versions
- `--no-cache` - Don't use cached data (slower but fresh)
- `--verbose` - Show more details

### Update Command
```bash
nuget-update-bot update <project-file> [options]
```

**Options:**
- `--config <file>` - Use a config file
- `--dry-run` - Just show what would happen
- `--include-prerelease` - Include beta versions
- `--no-cache` - Don't use cached data
- `--verbose` - Show more details

### Report Command
```bash
nuget-update-bot report <project-file> [options]
```

**Options:**
- `--output <file>` - Where to save the report
- `--format <type>` - `json` or `console`
- `--config <file>` - Use a config file
- `--include-prerelease` - Include beta versions
- `--no-cache` - Don't use cached data
- `--verbose` - Show more details

## Exit Codes

After running, the tool tells you how it went with a number:

| Code | What It Means |
|------|---------------|
| 0 | Everything is perfect! |
| 1 | Found updates available |
| -1 | Something went wrong |

## Safety Features

### 1. Automatic Backups
Every time you update, we save your old file:
```
MyProject.csproj.backup.20250113-143022
```

### 2. Validation
After updating, we check if the file is still valid. If not, we restore the backup automatically!

### 3. Dry Run
Always use `--dry-run` first if you're nervous:
```bash
nuget-update-bot update MyProject.csproj --dry-run
```

## Real-World Examples

### Example 1: Safe First-Time User
```bash
# Step 1: See what's available
nuget-update-bot scan MyProject.csproj --verbose

# Step 2: Test without changing
nuget-update-bot update MyProject.csproj --dry-run

# Step 3: Actually update
nuget-update-bot update MyProject.csproj
```

### Example 2: Careful Updates
Create `safe-config.json`:
```json
{
  "UpdatePolicy": "Patch",
  "IncludePrerelease": false,
  "ExcludePackages": ["Microsoft.EntityFrameworkCore"]
}
```

Then run:
```bash
nuget-update-bot update MyProject.csproj --config safe-config.json
```

### Example 3: Get a Report for Your Team
```bash
nuget-update-bot report MyProject.csproj \
  --output team-report.json \
  --format json
```

Share `team-report.json` with your team!

## Tips and Tricks

### Tip 1: Always Scan First
```bash
nuget-update-bot scan MyProject.csproj
```

### Tip 2: Use Dry Run for Big Projects
```bash
nuget-update-bot update BigProject.csproj --dry-run
```

### Tip 3: Exclude Breaking Packages
Some packages break things when updated. Exclude them:
```json
{
  "ExcludePackages": ["PackageThatBreaksStuff"]
}
```

### Tip 4: Check Your Backups
Backups are in the same folder as your project:
```
MyProject.csproj.backup.20250113-143022
```

## Troubleshooting

### Problem: "File not found"
**Solution:** Make sure you're in the right folder:
```bash
cd /path/to/your/project
nuget-update-bot scan MyProject.csproj
```

### Problem: "Invalid XML"
**Solution:** Your project file might be broken. Check the backup:
```
MyProject.csproj.backup.20250113-143022
```

### Problem: Updates broke my project
**Solution:** Restore from backup:
```bash
cp MyProject.csproj.backup.20250113-143022 MyProject.csproj
```

### Problem: Too many updates at once
**Solution:** Use Patch policy for smaller updates:
```json
{
  "UpdatePolicy": "Patch"
}
```

## Central Package Management (CPM) Support

NugetUpdateBot fully supports Central Package Management! It automatically detects if your project uses CPM and updates the right files.

### What is CPM?

Central Package Management keeps all package versions in one file (`Directory.Packages.props`) instead of in each project file.

**Traditional project:**
```xml
<PackageReference Include="Newtonsoft.Json" Version="13.0.1" />
```

**CPM project:**

Project file (`MyApp.csproj`):
```xml
<PackageReference Include="Newtonsoft.Json" />
```

Versions file (`Directory.Packages.props`):
```xml
<PackageVersion Include="Newtonsoft.Json" Version="13.0.1" />
```

### How It Works

The tool automatically:
1. Detects if your project uses CPM
2. Finds your `Directory.Packages.props` file (searches up to 5 levels up)
3. Updates versions in the correct file
4. Creates backups of both files

### Example with CPM

```bash
# Same commands work for both CPM and traditional projects!
nuget-update-bot scan MyApp.csproj
nuget-update-bot update MyApp.csproj
```

Output for CPM project:
```
Detected Central Package Management
Found Directory.Packages.props at ../Directory.Packages.props
Creating backups...
  - MyApp.csproj.backup.20250113-143022
  - Directory.Packages.backup.20250113-143022.props

Updating Newtonsoft.Json from 13.0.1 to 13.0.3...
Updated in Directory.Packages.props

Done!
```

### CPM Detection

The tool detects CPM by checking:
1. `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>` in project file
2. If 80%+ of `PackageReference` elements lack `Version` attributes

### Safety with CPM

- Backs up both `.csproj` and `Directory.Packages.props`
- Validates both files after updates
- Searches parent directories to find `Directory.Packages.props`
- Works with nested project structures

### Restoring CPM Backups

If something goes wrong:

```bash
# Restore both files
cp MyApp.csproj.backup.20250113-143022 MyApp.csproj
cp ../Directory.Packages.backup.20250113-143022.props ../Directory.Packages.props
```

The tool works seamlessly with CPM - you don't need to do anything special!

## Need Help?

- Found a bug? Report it on GitHub
- Have a question? Check the docs folder
- Want a feature? Open an issue

## License

MIT License - Use it freely!
