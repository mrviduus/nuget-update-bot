# GitHub Actions Workflows

This directory contains GitHub Actions workflows for automating NuGet package updates across multiple repositories.

## Available Workflows

### 1. Update LoadSurge (`update-loadsurge.yml`)
- **Target Repository**: [mrviduus/LoadSurge](https://github.com/mrviduus/LoadSurge)
- **Schedule**: Every Saturday at 9:00 AM UTC
- **Purpose**: Automatically check and update NuGet packages in the LoadSurge repository

### 2. Update xUnitV3LoadFramework (`update-xunitv3loadframework.yml`)
- **Target Repository**: [mrviduus/xUnitV3LoadFramework](https://github.com/mrviduus/xUnitV3LoadFramework)
- **Schedule**: Every Monday at 9:00 AM UTC
- **Purpose**: Automatically check and update NuGet packages in the xUnitV3LoadFramework repository

## What These Workflows Do

Each workflow performs the following steps:

1. **Checkout Target Repository**: Clones the target repository
2. **Setup .NET Environment**: Installs .NET 9.0
3. **Install NugetUpdateBot**: Installs this tool as a global .NET tool
4. **Scan for Updates**: Checks all .csproj files for outdated packages
5. **Create Branch**: Creates a dated branch (e.g., `nuget-updates-20250113`)
6. **Update Packages**: Updates all outdated packages
7. **Commit Changes**: Commits the updates with descriptive message
8. **Create Pull Request**: Opens a PR with the updates for review

## Manual Triggering

All workflows can be manually triggered:

1. Go to the **Actions** tab in this repository
2. Select the desired workflow
3. Click **Run workflow**
4. Select branch and confirm

## Monitoring

### View Workflow Runs
- Navigate to: **Actions** tab → Select workflow → View runs

### Check Created PRs
Automated PRs will be created in the target repositories:
- LoadSurge: https://github.com/mrviduus/LoadSurge/pulls
- xUnitV3LoadFramework: https://github.com/mrviduus/xUnitV3LoadFramework/pulls

## Schedule Details

| Repository | Day | Time (UTC) | Cron Expression |
|------------|-----|------------|-----------------|
| LoadSurge | Saturday | 9:00 AM | `0 9 * * 6` |
| xUnitV3LoadFramework | Monday | 9:00 AM | `0 9 * * 1` |

## Requirements

These workflows require:
- NugetUpdateBot to be published as a .NET global tool on NuGet.org
- Appropriate GitHub permissions (automatically provided via `GITHUB_TOKEN`)

## Customization

To add a new repository or modify the schedule, see the detailed guide:
[docs/GITHUB_ACTIONS_SETUP.md](../../docs/GITHUB_ACTIONS_SETUP.md)

## Troubleshooting

If workflows fail:
1. Check the workflow run logs in the Actions tab
2. Verify NugetUpdateBot is published to NuGet.org
3. Ensure target repositories exist and are accessible
4. Check that .csproj files exist in target repositories

For more help, see [docs/GITHUB_ACTIONS_SETUP.md](../../docs/GITHUB_ACTIONS_SETUP.md)
