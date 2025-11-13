# Configuration Guide

This guide explains how to customize NuGet Update Bot using configuration files.

## Why Use a Config File?

Instead of typing lots of options every time, you can save your preferences in a file:

```bash
# Without config (lots of typing!)
nuget-update-bot update MyApp.csproj --include-prerelease --verbose

# With config (simple!)
nuget-update-bot update MyApp.csproj --config myconfig.json
```

## Creating Your First Config File

Create a file called `nuget-update-bot.json`:

```json
{
  "UpdatePolicy": "Major",
  "IncludePrerelease": false,
  "ExcludePackages": []
}
```

Save it in your project folder, then use it:

```bash
nuget-update-bot update MyApp.csproj --config nuget-update-bot.json
```

## Configuration Options Explained

### UpdatePolicy

This controls how big the updates can be.

#### Option 1: "Major" (Everything)
```json
{
  "UpdatePolicy": "Major"
}
```

**What it does:** Updates everything, even big breaking changes.

**Example:**
- `1.0.0` → `2.0.0` ✅
- `2.5.0` → `3.0.0` ✅
- `3.0.1` → `3.0.2` ✅

**When to use:** For new projects or when you want the latest versions.

#### Option 2: "Minor" (Medium Changes)
```json
{
  "UpdatePolicy": "Minor"
}
```

**What it does:** Updates medium changes, but not major versions.

**Example:**
- `1.0.0` → `1.5.0` ✅
- `1.5.0` → `1.9.9` ✅
- `1.9.9` → `2.0.0` ❌ (too big!)

**When to use:** For stable projects where you want new features but not breaking changes.

#### Option 3: "Patch" (Small Changes Only)
```json
{
  "UpdatePolicy": "Patch"
}
```

**What it does:** Only bug fixes, no new features.

**Example:**
- `1.0.0` → `1.0.1` ✅
- `1.0.1` → `1.0.2` ✅
- `1.0.2` → `1.1.0` ❌ (too big!)

**When to use:** For production apps where stability is critical.

### IncludePrerelease

This controls whether to include beta/test versions.

```json
{
  "IncludePrerelease": false
}
```

**false (default):** Only stable versions
- Example: `1.0.0`, `2.5.0` ✅
- Skips: `2.0.0-beta`, `3.0.0-rc1` ❌

**true:** Include beta versions too
- Example: `1.0.0`, `2.0.0-beta`, `3.0.0-rc1` ✅

**When to use true:** Testing new features, working on development projects.

**When to use false:** Production apps, stable projects.

### ExcludePackages

List packages that should NEVER be updated.

```json
{
  "ExcludePackages": [
    "Microsoft.EntityFrameworkCore",
    "OldPackageThatWorks"
  ]
}
```

**Why exclude packages?**
- They break when updated
- You're using a specific old version on purpose
- You're waiting for a bug fix before updating

## Example Configurations

### Example 1: Safe Production Config
```json
{
  "UpdatePolicy": "Patch",
  "IncludePrerelease": false,
  "ExcludePackages": [
    "Microsoft.EntityFrameworkCore"
  ]
}
```

**Use case:** Production app that needs to be stable.

### Example 2: Development Config
```json
{
  "UpdatePolicy": "Major",
  "IncludePrerelease": true,
  "ExcludePackages": []
}
```

**Use case:** Testing app, want latest everything.

### Example 3: Balanced Config
```json
{
  "UpdatePolicy": "Minor",
  "IncludePrerelease": false,
  "ExcludePackages": [
    "PackageWithKnownBug"
  ]
}
```

**Use case:** Regular project, want new features but not breaking changes.

### Example 4: Very Conservative
```json
{
  "UpdatePolicy": "Patch",
  "IncludePrerelease": false,
  "ExcludePackages": [
    "CriticalPackage1",
    "CriticalPackage2",
    "PackageThatBreaks"
  ]
}
```

**Use case:** Mission-critical app, only security patches.

## Multiple Config Files

You can have different configs for different scenarios!

### dev-config.json
```json
{
  "UpdatePolicy": "Major",
  "IncludePrerelease": true,
  "ExcludePackages": []
}
```

### prod-config.json
```json
{
  "UpdatePolicy": "Patch",
  "IncludePrerelease": false,
  "ExcludePackages": ["EntityFramework"]
}
```

Then use them:
```bash
# For development
nuget-update-bot update MyApp.csproj --config dev-config.json

# For production
nuget-update-bot update MyApp.csproj --config prod-config.json
```

## Config File Location

You can put the config file anywhere:

```bash
# In the same folder
nuget-update-bot update MyApp.csproj --config config.json

# In a parent folder
nuget-update-bot update MyApp.csproj --config ../config.json

# Absolute path
nuget-update-bot update MyApp.csproj --config /Users/me/configs/config.json
```

## Understanding Version Numbers

Version numbers look like this: `MAJOR.MINOR.PATCH`

Example: `2.5.1`
- `2` = Major (big changes, might break things)
- `5` = Minor (new features, shouldn't break)
- `1` = Patch (bug fixes, safe)

### Examples:
- `1.0.0` → `1.0.1` = Patch (bug fix)
- `1.0.1` → `1.1.0` = Minor (new feature)
- `1.1.0` → `2.0.0` = Major (big change!)

### Prerelease Versions:
- `2.0.0-alpha` = Very early testing
- `2.0.0-beta` = Testing, might have bugs
- `2.0.0-rc1` = Release Candidate (almost ready)
- `2.0.0` = Stable release

## Tips for Choosing Settings

### For New Projects:
```json
{
  "UpdatePolicy": "Major",
  "IncludePrerelease": false
}
```
Get the latest stable versions.

### For Stable Projects:
```json
{
  "UpdatePolicy": "Minor",
  "IncludePrerelease": false
}
```
Get new features but avoid breaking changes.

### For Production:
```json
{
  "UpdatePolicy": "Patch",
  "IncludePrerelease": false,
  "ExcludePackages": ["CriticalPackages"]
}
```
Only get bug fixes, nothing else.

### For Experimentation:
```json
{
  "UpdatePolicy": "Major",
  "IncludePrerelease": true
}
```
Try everything new!

## Validating Your Config

Not sure if your config file is correct? Try running:

```bash
nuget-update-bot scan MyApp.csproj --config myconfig.json --verbose
```

The `--verbose` flag will show if the config loaded correctly.

## Common Mistakes

### Mistake 1: Typos
```json
{
  "UpdatePolcy": "Major"  ❌ Wrong!
}
```

**Fix:**
```json
{
  "UpdatePolicy": "Major"  ✅ Correct!
}
```

### Mistake 2: Wrong Values
```json
{
  "UpdatePolicy": "All"  ❌ Not valid!
}
```

**Fix:** Use only "Major", "Minor", or "Patch"
```json
{
  "UpdatePolicy": "Major"  ✅
}
```

### Mistake 3: Wrong Boolean
```json
{
  "IncludePrerelease": "false"  ❌ Should be boolean, not string!
}
```

**Fix:**
```json
{
  "IncludePrerelease": false  ✅ No quotes!
}
```

### Mistake 4: Missing Comma
```json
{
  "UpdatePolicy": "Major"  ❌ Missing comma!
  "IncludePrerelease": false
}
```

**Fix:**
```json
{
  "UpdatePolicy": "Major",  ✅ Added comma!
  "IncludePrerelease": false
}
```

## Next Steps

Now you know how to configure the tool! Check out:

- [Examples](EXAMPLES.md) - See real-world usage
- [Advanced Features](ADVANCED.md) - Power user tricks
- [Troubleshooting](TROUBLESHOOTING.md) - Fix issues
