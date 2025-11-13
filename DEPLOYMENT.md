# Deployment Guide

This guide covers how to deploy and use the NugetUpdateBot with GitHub Actions.

## Prerequisites

Before the GitHub Actions workflows can run, you need to:

1. **Publish NugetUpdateBot to NuGet.org** (required for workflows to install the tool)
2. **Copy workflow files to target repositories**

## Step 1: Publish to NuGet.org

### Option A: Manual Publishing (Recommended First Time)

1. **Update version in `.csproj`**

   Edit `src/NugetUpdateBot/NugetUpdateBot.csproj`:
   ```xml
   <PropertyGroup>
     <Version>1.0.0</Version>
     <PackAsTool>true</PackAsTool>
     <ToolCommandName>nuget-update-bot</ToolCommandName>
   </PropertyGroup>
   ```

2. **Add PackAsTool settings** (if not already present)

   Ensure these properties are in `src/NugetUpdateBot/NugetUpdateBot.csproj`:
   ```xml
   <PropertyGroup>
     <PackAsTool>true</PackAsTool>
     <ToolCommandName>nuget-update-bot</ToolCommandName>
     <PackageOutputPath>./nupkg</PackageOutputPath>
   </PropertyGroup>
   ```

3. **Build and Pack**
   ```bash
   dotnet build -c Release
   dotnet pack -c Release src/NugetUpdateBot/NugetUpdateBot.csproj
   ```

4. **Get NuGet API Key**
   - Go to https://www.nuget.org/
   - Sign in or create account
   - Go to Account → API Keys
   - Create new API key with "Push" permission
   - Copy the key (you'll only see it once!)

5. **Push to NuGet.org**
   ```bash
   dotnet nuget push src/NugetUpdateBot/bin/Release/*.nupkg \
     --api-key YOUR_API_KEY \
     --source https://api.nuget.org/v3/index.json
   ```

6. **Wait for indexing**
   - Package may take 10-15 minutes to appear on NuGet.org
   - Check status at: https://www.nuget.org/packages/NugetUpdateBot

### Option B: Automated Publishing with GitHub Actions

Create `.github/workflows/publish-nuget.yml`:

```yaml
name: Publish to NuGet

on:
  release:
    types: [published]

jobs:
  publish:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Build
        run: dotnet build -c Release

      - name: Pack
        run: dotnet pack -c Release src/NugetUpdateBot/NugetUpdateBot.csproj

      - name: Push to NuGet
        run: dotnet nuget push src/NugetUpdateBot/bin/Release/*.nupkg \
          --api-key ${{ secrets.NUGET_API_KEY }} \
          --source https://api.nuget.org/v3/index.json
```

Then:
1. Add `NUGET_API_KEY` to repository secrets (Settings → Secrets → Actions)
2. Create a GitHub release to trigger publishing

## Step 2: Deploy Workflows to Target Repositories

### For LoadSurge Repository

1. **Clone LoadSurge repository**
   ```bash
   cd ~/projects
   git clone https://github.com/mrviduus/LoadSurge.git
   cd LoadSurge
   ```

2. **Create workflows directory**
   ```bash
   mkdir -p .github/workflows
   ```

3. **Copy workflow file**
   ```bash
   cp ~/projects/nuget-update-bot/nuget-update-bot/.github/workflows/update-loadsurge.yml \
      .github/workflows/update-packages.yml
   ```

4. **Update workflow to run in same repository**

   Edit `.github/workflows/update-packages.yml` and change:
   ```yaml
   # FROM:
   - name: Checkout LoadSurge repository
     uses: actions/checkout@v4
     with:
       repository: mrviduus/LoadSurge
       token: ${{ secrets.GITHUB_TOKEN }}
       fetch-depth: 0

   # TO:
   - name: Checkout repository
     uses: actions/checkout@v4
     with:
       token: ${{ secrets.GITHUB_TOKEN }}
       fetch-depth: 0
   ```

   Also update PR creation step:
   ```yaml
   # Change:
   owner: 'mrviduus',
   repo: 'LoadSurge',

   # To:
   owner: context.repo.owner,
   repo: context.repo.repo,
   ```

5. **Commit and push**
   ```bash
   git add .github/workflows/update-packages.yml
   git commit -m "Add automated NuGet package updates workflow"
   git push
   ```

### For xUnitV3LoadFramework Repository

Repeat the same steps as LoadSurge, but use `update-xunitv3loadframework.yml` as the source.

## Step 3: Enable and Test Workflows

### Enable Actions

1. Go to repository → **Settings** → **Actions** → **General**
2. Ensure "Allow all actions and reusable workflows" is selected
3. Under "Workflow permissions", ensure:
   - "Read and write permissions" is selected
   - "Allow GitHub Actions to create and approve pull requests" is checked

### Test Manually

1. Go to repository → **Actions** tab
2. Select "Update NuGet Packages" workflow
3. Click **Run workflow** button
4. Select branch (usually `main`)
5. Click **Run workflow**
6. Watch the workflow run and check for:
   - ✅ Successful completion
   - 📝 Pull Request created (if updates available)
   - ❌ Errors (check logs if failed)

## Step 4: Monitor Automated Runs

### View Workflow Runs

- **LoadSurge**: https://github.com/mrviduus/LoadSurge/actions
- **xUnitV3LoadFramework**: https://github.com/mrviduus/xUnitV3LoadFramework/actions

### Check Pull Requests

When updates are found, check:
- **LoadSurge PRs**: https://github.com/mrviduus/LoadSurge/pulls
- **xUnitV3LoadFramework PRs**: https://github.com/mrviduus/xUnitV3LoadFramework/pulls

### Schedule

- **LoadSurge**: Every Saturday at 9:00 AM UTC
- **xUnitV3LoadFramework**: Every Monday at 9:00 AM UTC

## Troubleshooting

### Workflow Fails: "Command 'nuget-update-bot' not found"

**Cause**: NugetUpdateBot not published to NuGet.org or not yet indexed

**Solution**:
1. Verify package exists: https://www.nuget.org/packages/NugetUpdateBot
2. Wait 10-15 minutes after publishing for indexing
3. Try manual run after waiting

### Workflow Fails: "No .csproj files found"

**Cause**: Repository doesn't have .csproj files in expected location

**Solution**:
1. Verify .csproj files exist in repository
2. Update workflow to search in correct directory:
   ```bash
   PROJECTS=$(find ./src -name "*.csproj" | head -1)
   ```

### No Pull Request Created

**Possible Causes**:
- All packages are up to date (check workflow logs)
- No actual changes after update
- Permissions issue

**Solution**:
1. Check workflow logs for "updates_available" output
2. Verify workflow has correct permissions
3. Check repository settings allow PR creation by Actions

### Pull Request Created But Empty

**Cause**: Updates were scanned but not applied

**Solution**:
1. Check workflow logs for update step errors
2. Verify project files are valid XML
3. Try running locally:
   ```bash
   dotnet tool install --global NugetUpdateBot
   nuget-update-bot scan YourProject.csproj --verbose
   ```

## Configuration Options

### Update Policy

Create `.nuget-update-bot.json` in target repository:

```json
{
  "UpdatePolicy": "Minor",
  "IncludePrerelease": false,
  "ExcludePackages": [
    "Microsoft.EntityFrameworkCore"
  ]
}
```

Then update workflow to use it:
```bash
nuget-update-bot update "$project" --config .nuget-update-bot.json --verbose
```

### Change Schedule

Edit the cron expression in workflow file:

```yaml
on:
  schedule:
    # Run every day at 9 AM UTC
    - cron: '0 9 * * *'
```

Cron schedule reference:
- `0 9 * * 1` = Every Monday at 9 AM
- `0 9 * * 6` = Every Saturday at 9 AM
- `0 0 * * *` = Every day at midnight
- `0 9 1 * *` = First day of each month at 9 AM

## Next Steps

1. ✅ Publish NugetUpdateBot to NuGet.org
2. ✅ Deploy workflows to LoadSurge and xUnitV3LoadFramework
3. ✅ Test with manual workflow trigger
4. ✅ Monitor first scheduled runs
5. ✅ Review and merge first automated PRs

## Support

For issues:
- **NugetUpdateBot**: https://github.com/mrviduus/nuget-update-bot/issues
- **GitHub Actions**: https://docs.github.com/en/actions

## Additional Resources

- [GitHub Actions Setup Guide](docs/GITHUB_ACTIONS_SETUP.md)
- [Workflow README](.github/workflows/README.md)
- [Contributing Guide](CONTRIBUTING.md)
