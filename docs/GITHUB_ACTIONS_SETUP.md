# GitHub Actions Setup for Automated NuGet Updates

This guide explains how to set up automated NuGet package updates for your repositories using GitHub Actions.

## Overview

The NugetUpdateBot can be integrated into GitHub Actions workflows to automatically:
1. Scan repositories for outdated NuGet packages
2. Update packages to their latest versions
3. Create Pull Requests with the updates
4. Run on a schedule (e.g., weekly)

## Recommended Approach

**Add the workflow directly to each repository** that needs automated updates. This is simpler and follows standard practices.

### Example Repositories
- **LoadSurge**: Schedule every Saturday at 9:00 AM UTC
- **xUnitV3LoadFramework**: Schedule every Monday at 9:00 AM UTC

## How It Works

1. **Scheduled Trigger**: GitHub Actions runs the workflow on the specified schedule
2. **Checkout Repository**: The workflow checks out the target repository
3. **Install NugetUpdateBot**: Installs the latest version as a global .NET tool
4. **Scan for Updates**: Scans project files for outdated packages
5. **Create Branch**: If updates are found, creates a new branch (e.g., `nuget-updates-20250113`)
6. **Update Packages**: Updates package versions
   - **With Central Package Management (CPM)**: ONLY updates Directory.Packages.props for safety
   - **Without CPM**: Updates Version attributes in .csproj files
7. **Commit & Push**: Commits changes and pushes to the new branch
8. **Create PR**: Creates a Pull Request with the updates

### Central Package Management (CPM) Support

**IMPORTANT**: If your repository uses Directory.Packages.props (Central Package Management), NugetUpdateBot will:
- ✅ **ONLY** update the Directory.Packages.props file
- ❌ **NEVER** touch individual .csproj files
- 🛡️ **Ensure** version consistency across your entire solution

This safety feature prevents accidental version conflicts and maintains CPM integrity.

## Setup Instructions

### For New Repositories

1. **Copy Workflow File**

   Copy one of the workflow files from `.github/workflows/` and customize it:

   ```yaml
   name: Update NuGet Packages - YourRepo

   on:
     schedule:
       - cron: '0 9 * * 1'  # Every Monday at 9 AM UTC
     workflow_dispatch:

   jobs:
     update-packages:
       runs-on: ubuntu-latest
       permissions:
         contents: write
         pull-requests: write
       # ... rest of the workflow
   ```

2. **Update Repository References**

   Change these values in the workflow:
   - `repository: mrviduus/YourRepo`
   - `owner: 'mrviduus'`
   - `repo: 'YourRepo'`
   - Workflow file name: `update-yourrepo.yml`

3. **Customize Schedule**

   Cron schedule examples:
   ```yaml
   # Every Monday at 9 AM UTC
   - cron: '0 9 * * 1'

   # Every Saturday at 9 AM UTC
   - cron: '0 9 * * 6'

   # Every day at midnight UTC
   - cron: '0 0 * * *'

   # First day of every month
   - cron: '0 9 1 * *'
   ```

4. **Enable Workflows**

   Commit the workflow file to your repository:
   ```bash
   git add .github/workflows/update-yourrepo.yml
   git commit -m "Add automated NuGet update workflow"
   git push
   ```

### Permissions Required

The workflow needs these permissions:
- **contents: write** - To push commits to branches
- **pull-requests: write** - To create Pull Requests

These are automatically provided by `${{ secrets.GITHUB_TOKEN }}`.

### Manual Trigger

All workflows include `workflow_dispatch` which allows manual triggering:

1. Go to your repository on GitHub
2. Click "Actions" tab
3. Select the workflow (e.g., "Update NuGet Packages - LoadSurge")
4. Click "Run workflow" button
5. Select branch and click "Run workflow"

## Workflow Configuration

### Scanning Strategy

The workflow finds the first `.csproj` file to scan:
```bash
PROJECTS=$(find . -name "*.csproj" | head -1)
```

### Update Strategy

Updates all `.csproj` files found:
```bash
for project in $(find . -name "*.csproj"); do
  nuget-update-bot update "$project" --verbose
done
```

### Branch Naming

Branches are named with the date:
```bash
nuget-updates-20250113
```

### Pull Request Content

PRs include:
- Title: "🔄 Update NuGet Packages"
- Checklist for reviewers
- Timestamp and trigger information
- Link to NugetUpdateBot

## Customization Options

### Update Policy

Add a configuration file to control update behavior:

1. Create `.nuget-update-bot.json` in repository root:
   ```json
   {
     "UpdatePolicy": "Minor",
     "IncludePrerelease": false,
     "ExcludePackages": [
       "SomePackage.ThatBreaks"
     ]
   }
   ```

2. Update workflow to use config:
   ```bash
   nuget-update-bot update "$project" --config .nuget-update-bot.json --verbose
   ```

### Update Policies

- **Major**: Update all packages (1.0.0 → 2.0.0)
- **Minor**: Update minor and patch only (1.0.0 → 1.5.0)
- **Patch**: Update patch versions only (1.0.0 → 1.0.1)

### Exclude Packages

Exclude specific packages from updates:
```json
{
  "ExcludePackages": [
    "Microsoft.EntityFrameworkCore",
    "Newtonsoft.Json"
  ]
}
```

## Monitoring

### Check Workflow Runs

1. Go to repository → Actions tab
2. View workflow runs and their status
3. Click on a run to see detailed logs

### Check for PRs

Automated PRs will appear in the Pull Requests tab with:
- Title: "🔄 Update NuGet Packages"
- Branch: `nuget-updates-YYYYMMDD`
- Author: `github-actions[bot]`

## Troubleshooting

### Workflow Not Running

**Check:**
- Workflow file is in `.github/workflows/`
- Workflow file has `.yml` or `.yaml` extension
- Cron syntax is correct
- Repository has Actions enabled (Settings → Actions)

### No PR Created

**Possible reasons:**
- No updates available (check workflow logs)
- No changes after update (packages already current)
- Permission issues (check workflow permissions)

### Failed to Install NugetUpdateBot

**Solution:**
The workflow includes fallback logic:
```bash
dotnet tool install --global NugetUpdateBot || dotnet tool update --global NugetUpdateBot
```

If it fails, NugetUpdateBot might not be published to NuGet.org yet.

### Updates Not Applied

**Check:**
- .csproj files exist in repository
- Project files are valid XML
- Packages exist on NuGet.org
- Network connectivity to NuGet.org

## Security Considerations

### GitHub Token

The workflow uses `${{ secrets.GITHUB_TOKEN }}`:
- Automatically provided by GitHub
- Scoped to the repository
- Expires after the workflow completes
- No manual setup required

### Dependencies

The workflow installs NugetUpdateBot from NuGet.org:
- Verify the package is from trusted source
- Review NugetUpdateBot code (open source)
- Pin to specific version if needed:
  ```bash
  dotnet tool install --global NugetUpdateBot --version 1.0.0
  ```

## Best Practices

1. **Review PRs**: Always review automated PRs before merging
2. **Run Tests**: Ensure CI/CD tests pass on the PR
3. **Check Breaking Changes**: Review package changelogs for breaking changes
4. **Stagger Schedules**: Use different days for different repos to spread load
5. **Monitor Failures**: Set up notifications for workflow failures
6. **Keep Workflows Updated**: Periodically review and update workflow syntax

## Example: Complete Workflow

Here's a complete example with all features:

```yaml
name: Update NuGet Packages

on:
  schedule:
    - cron: '0 9 * * 1'  # Monday 9 AM UTC
  workflow_dispatch:

jobs:
  update-packages:
    runs-on: ubuntu-latest
    permissions:
      contents: write
      pull-requests: write

    steps:
      - name: Checkout repository
        uses: actions/checkout@v4
        with:
          token: ${{ secrets.GITHUB_TOKEN }}
          fetch-depth: 0

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Install NugetUpdateBot
        run: |
          dotnet tool install --global NugetUpdateBot || \
          dotnet tool update --global NugetUpdateBot

      - name: Scan and update
        run: |
          for project in $(find . -name "*.csproj"); do
            echo "Processing $project..."
            nuget-update-bot update "$project" --config .nuget-update-bot.json --verbose
          done

      # ... rest of steps (commit, PR, etc.)
```

## Support

For issues or questions:
- **NugetUpdateBot Issues**: https://github.com/mrviduus/nuget-update-bot/issues
- **GitHub Actions Docs**: https://docs.github.com/en/actions
- **Workflow Syntax**: https://docs.github.com/en/actions/using-workflows/workflow-syntax-for-github-actions
