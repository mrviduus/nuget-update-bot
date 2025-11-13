# Real-World Examples

This guide shows you how to use NuGet Update Bot in real-world scenarios.

## Example 1: Your First Update

**Scenario:** You have a simple console app and want to update packages.

**Project:** `MyConsoleApp.csproj`

### Step-by-Step:

```bash
# 1. See what needs updating
nuget-update-bot scan MyConsoleApp.csproj
```

**Output:**
```
Scanning MyConsoleApp.csproj...

Package: Newtonsoft.Json
  Current: 12.0.1
  Latest:  13.0.3
  Status:  Update available

Summary: 1 package needs updating
```

```bash
# 2. Preview the changes
nuget-update-bot update MyConsoleApp.csproj --dry-run
```

**Output:**
```
DRY RUN - No changes will be made

Would update:
  - Newtonsoft.Json: 12.0.1 → 13.0.3

Nothing was changed.
```

```bash
# 3. Actually update
nuget-update-bot update MyConsoleApp.csproj
```

**Output:**
```
Creating backup...
Backup saved: MyConsoleApp.csproj.backup.20250113-143022

Updating packages...
  ✓ Newtonsoft.Json: 12.0.1 → 13.0.3

Successfully updated 1 package!
```

```bash
# 4. Test it works
dotnet build
dotnet run
```

## Example 2: Production App (Be Very Careful!)

**Scenario:** You have a production web API. You want ONLY security patches.

**Project:** `ProductionAPI.csproj`

### Step 1: Create a Safe Config

Create `production-config.json`:
```json
{
  "UpdatePolicy": "Patch",
  "IncludePrerelease": false,
  "ExcludePackages": [
    "Microsoft.EntityFrameworkCore",
    "CriticalAuthLibrary"
  ]
}
```

### Step 2: Scan with Config

```bash
nuget-update-bot scan ProductionAPI.csproj --config production-config.json --verbose
```

**Output:**
```
Loading config: production-config.json
Policy: Patch only
Exclude: Microsoft.EntityFrameworkCore, CriticalAuthLibrary

Scanning...

Package: Newtonsoft.Json
  Current: 13.0.1
  Latest:  13.0.3
  Update:  13.0.3 (patch)

Package: Microsoft.EntityFrameworkCore
  Current: 7.0.0
  Latest:  8.0.0
  Update:  EXCLUDED

Summary: 1 update available (1 excluded)
```

### Step 3: Safe Update

```bash
# Always dry-run first for production!
nuget-update-bot update ProductionAPI.csproj --config production-config.json --dry-run

# If it looks good, update
nuget-update-bot update ProductionAPI.csproj --config production-config.json
```

### Step 4: Test Thoroughly

```bash
dotnet build
dotnet test
# Run your integration tests
# Test manually
```

## Example 3: Multiple Projects in a Solution

**Scenario:** You have a solution with 5 projects.

**Structure:**
```
MySolution/
  ├── MySolution.sln
  ├── API/
  │   └── API.csproj
  ├── Core/
  │   └── Core.csproj
  ├── Data/
  │   └── Data.csproj
  ├── Tests/
  │   └── Tests.csproj
  └── Worker/
      └── Worker.csproj
```

### Option A: Update Each Project

```bash
# Scan all projects first
nuget-update-bot scan API/API.csproj
nuget-update-bot scan Core/Core.csproj
nuget-update-bot scan Data/Data.csproj
nuget-update-bot scan Tests/Tests.csproj
nuget-update-bot scan Worker/Worker.csproj

# Update them one by one
nuget-update-bot update API/API.csproj
nuget-update-bot update Core/Core.csproj
# ... etc
```

### Option B: Use a Script

Create `update-all.sh`:
```bash
#!/bin/bash

PROJECTS=(
  "API/API.csproj"
  "Core/Core.csproj"
  "Data/Data.csproj"
  "Tests/Tests.csproj"
  "Worker/Worker.csproj"
)

for project in "${PROJECTS[@]}"
do
  echo "Updating $project..."
  nuget-update-bot update "$project" --config prod-config.json
done

echo "All projects updated!"
```

Run it:
```bash
chmod +x update-all.sh
./update-all.sh
```

## Example 4: Different Configs for Different Projects

**Scenario:** Production code needs patch-only, test code can have major updates.

### Create Two Configs:

**prod-config.json:**
```json
{
  "UpdatePolicy": "Patch",
  "IncludePrerelease": false,
  "ExcludePackages": ["EntityFramework"]
}
```

**test-config.json:**
```json
{
  "UpdatePolicy": "Major",
  "IncludePrerelease": false,
  "ExcludePackages": []
}
```

### Use Different Configs:

```bash
# Production projects - careful!
nuget-update-bot update API/API.csproj --config prod-config.json
nuget-update-bot update Core/Core.csproj --config prod-config.json

# Test projects - more aggressive
nuget-update-bot update Tests/Tests.csproj --config test-config.json
```

## Example 5: Generating Reports for Your Team

**Scenario:** You want to show your team what packages need updating.

### Generate JSON Report:

```bash
nuget-update-bot report MyApp.csproj \
  --output package-report.json \
  --format json \
  --verbose
```

**Output file (package-report.json):**
```json
{
  "ProjectPath": "MyApp.csproj",
  "ScanDate": "2025-01-13T14:30:22Z",
  "Packages": [
    {
      "Name": "Newtonsoft.Json",
      "CurrentVersion": "12.0.1",
      "LatestVersion": "13.0.3",
      "UpdateAvailable": true
    }
  ],
  "Summary": {
    "TotalPackages": 10,
    "UpdatesAvailable": 3,
    "UpToDate": 7
  }
}
```

### Share with Team:

```bash
# Email it
mail -s "Package Update Report" team@company.com < package-report.json

# Or commit to repo
git add package-report.json
git commit -m "Weekly package update report"
```

## Example 6: CI/CD Pipeline Integration

**Scenario:** Automatically check for updates in your CI pipeline.

### GitHub Actions Example:

Create `.github/workflows/package-check.yml`:
```yaml
name: Check Package Updates

on:
  schedule:
    - cron: '0 9 * * MON'  # Every Monday at 9 AM
  workflow_dispatch:

jobs:
  check-updates:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'

      - name: Install NuGet Update Bot
        run: dotnet tool install --global NugetUpdateBot

      - name: Scan for updates
        run: |
          nuget-update-bot scan MyApp.csproj \
            --config .github/prod-config.json \
            --verbose

      - name: Generate report
        run: |
          nuget-update-bot report MyApp.csproj \
            --output package-report.json \
            --format json

      - name: Upload report
        uses: actions/upload-artifact@v3
        with:
          name: package-report
          path: package-report.json
```

## Example 7: Recovering from a Bad Update

**Scenario:** You updated packages and something broke!

### The Mistake:
```bash
# Oops, updated too aggressively
nuget-update-bot update MyApp.csproj

# Project doesn't build anymore!
dotnet build
# Error: Breaking changes in NewLibrary 2.0.0
```

### The Recovery:

#### Step 1: Find the Backup
```bash
# List backups
ls -la *.backup.*
# Shows: MyApp.csproj.backup.20250113-143022
```

#### Step 2: Restore
```bash
# On Mac/Linux
cp MyApp.csproj.backup.20250113-143022 MyApp.csproj

# On Windows
copy MyApp.csproj.backup.20250113-143022 MyApp.csproj
```

#### Step 3: Test
```bash
dotnet build
# Success!
```

#### Step 4: Update More Carefully
Create `careful-config.json`:
```json
{
  "UpdatePolicy": "Minor",
  "IncludePrerelease": false,
  "ExcludePackages": ["NewLibrary"]
}
```

```bash
nuget-update-bot update MyApp.csproj --config careful-config.json
```

## Example 8: Experimenting with Prerelease

**Scenario:** You want to test a beta version of a library.

### Create Experimental Config:

**experiment-config.json:**
```json
{
  "UpdatePolicy": "Major",
  "IncludePrerelease": true,
  "ExcludePackages": []
}
```

### Create an Experiment Branch:

```bash
# Create experiment branch
git checkout -b experiment/test-new-versions

# Update with prerelease
nuget-update-bot update MyApp.csproj --config experiment-config.json --verbose
```

**Output:**
```
Package: AwesomeLibrary
  Current: 1.0.0
  Latest Stable: 1.5.0
  Latest Prerelease: 2.0.0-beta3
  Updating to: 2.0.0-beta3
```

### Test:
```bash
dotnet build
dotnet test

# If it works, great!
# If not, just delete the branch
git checkout main
git branch -D experiment/test-new-versions
```

## Example 9: Weekly Maintenance Routine

**Scenario:** You want a weekly routine for keeping packages updated.

### Monday Morning Routine:

```bash
# 1. Check what's available
nuget-update-bot scan MyApp.csproj --verbose > updates.txt

# 2. Review the file
cat updates.txt

# 3. If patches available, apply them
nuget-update-bot update MyApp.csproj --config patch-only.json

# 4. Test
dotnet build && dotnet test

# 5. If all good, commit
git add MyApp.csproj
git commit -m "Weekly package updates (patches only)"
git push
```

**patch-only.json:**
```json
{
  "UpdatePolicy": "Patch",
  "IncludePrerelease": false,
  "ExcludePackages": []
}
```

## Example 10: Large Enterprise Project

**Scenario:** You have 50+ projects with strict update policies.

### Create Policy Configs:

**critical-config.json:** (for mission-critical services)
```json
{
  "UpdatePolicy": "Patch",
  "IncludePrerelease": false,
  "ExcludePackages": [
    "EntityFramework",
    "IdentityServer",
    "PaymentGateway"
  ]
}
```

**standard-config.json:** (for regular services)
```json
{
  "UpdatePolicy": "Minor",
  "IncludePrerelease": false,
  "ExcludePackages": [
    "EntityFramework"
  ]
}
```

**test-config.json:** (for test projects)
```json
{
  "UpdatePolicy": "Major",
  "IncludePrerelease": false,
  "ExcludePackages": []
}
```

### Create Master Script:

**update-all-projects.sh:**
```bash
#!/bin/bash

echo "=== Updating Critical Services ==="
for project in Services/Payment Services/Auth Services/Billing
do
  nuget-update-bot update "$project/$project.csproj" \
    --config critical-config.json
done

echo "=== Updating Standard Services ==="
for project in Services/Catalog Services/Inventory Services/Shipping
do
  nuget-update-bot update "$project/$project.csproj" \
    --config standard-config.json
done

echo "=== Updating Test Projects ==="
for project in Tests/*
do
  nuget-update-bot update "$project/$project.csproj" \
    --config test-config.json
done

echo "=== Generating Reports ==="
nuget-update-bot report Services/Payment/Payment.csproj \
  --output reports/payment-updates.json --format json

echo "Done!"
```

## Tips from Real Usage

### Tip 1: Always Scan First
```bash
# Good workflow
nuget-update-bot scan MyApp.csproj
nuget-update-bot update MyApp.csproj --dry-run
nuget-update-bot update MyApp.csproj

# Bad workflow
nuget-update-bot update MyApp.csproj  # Too risky!
```

### Tip 2: Keep Backups
The tool creates backups automatically, but don't delete them immediately:
```bash
# Keep backups for at least a week
find . -name "*.backup.*" -mtime +7 -delete
```

### Tip 3: Use Git Branches
```bash
git checkout -b updates/weekly-packages
nuget-update-bot update MyApp.csproj
dotnet test
# If all good:
git commit -am "Update packages"
git checkout main
git merge updates/weekly-packages
```

### Tip 4: Document Your Policies
Create a `PACKAGE-UPDATE-POLICY.md` in your repo explaining which config to use.

## Next Steps

- Learn about [Advanced Features](ADVANCED.md)
- Read [Troubleshooting](TROUBLESHOOTING.md) guide
- See [Configuration](CONFIGURATION.md) options
