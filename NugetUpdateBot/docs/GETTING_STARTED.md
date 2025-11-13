# Getting Started with NuGet Update Bot

Welcome! This guide will help you start using NuGet Update Bot in just a few minutes.

## Step 1: Install the Tool

Open your terminal and run:

```bash
dotnet tool install --global NugetUpdateBot
```

That's it! The tool is now installed.

## Step 2: Find Your Project File

Find your `.csproj` file. It looks like this:

```
MyAwesomeApp/
  ├── MyAwesomeApp.csproj  ← This is what you need!
  ├── Program.cs
  └── ...
```

## Step 3: Your First Scan

Navigate to your project folder:

```bash
cd /path/to/MyAwesomeApp
```

Then run:

```bash
nuget-update-bot scan MyAwesomeApp.csproj
```

You'll see something like:

```
Scanning packages...

Package: Newtonsoft.Json
  Current Version: 12.0.1
  Latest Version: 13.0.3
  Update Available: Yes

Package: xunit
  Current Version: 2.4.0
  Latest Version: 2.6.0
  Update Available: Yes

Summary: 2 packages can be updated
```

## Step 4: Try a Dry Run

Before actually updating, see what would happen:

```bash
nuget-update-bot update MyAwesomeApp.csproj --dry-run
```

Output:
```
DRY RUN MODE - No changes will be made

Would update:
  - Newtonsoft.Json: 12.0.1 → 13.0.3
  - xunit: 2.4.0 → 2.6.0

No files were changed.
```

## Step 5: Actually Update

If you're happy with what you saw, run:

```bash
nuget-update-bot update MyAwesomeApp.csproj
```

Output:
```
Creating backup: MyAwesomeApp.csproj.backup.20250113-143022

Updating packages...
  ✓ Newtonsoft.Json: 12.0.1 → 13.0.3
  ✓ xunit: 2.4.0 → 2.6.0

Successfully updated 2 packages!
```

## Step 6: Test Your Project

After updating, make sure everything still works:

```bash
dotnet build
dotnet test
```

If something breaks, don't panic! Read the next section.

## Oops! Something Broke?

No problem! We created a backup. To restore it:

```bash
# On Windows
copy MyAwesomeApp.csproj.backup.20250113-143022 MyAwesomeApp.csproj

# On Mac/Linux
cp MyAwesomeApp.csproj.backup.20250113-143022 MyAwesomeApp.csproj
```

## Next Steps

Now that you know the basics:

1. **Learn about configuration**: Check out [CONFIGURATION.md](CONFIGURATION.md)
2. **See more examples**: Read [EXAMPLES.md](EXAMPLES.md)
3. **Advanced features**: Explore [ADVANCED.md](ADVANCED.md)

## Quick Reference Card

Print this and keep it nearby:

```
┌─────────────────────────────────────────────┐
│     NuGet Update Bot Quick Reference        │
├─────────────────────────────────────────────┤
│                                             │
│  Scan (check only):                         │
│  $ nuget-update-bot scan MyApp.csproj       │
│                                             │
│  Dry run (preview):                         │
│  $ nuget-update-bot update MyApp.csproj \   │
│      --dry-run                              │
│                                             │
│  Update (for real):                         │
│  $ nuget-update-bot update MyApp.csproj     │
│                                             │
│  Generate report:                           │
│  $ nuget-update-bot report MyApp.csproj \   │
│      --output report.json --format json     │
│                                             │
│  With config:                               │
│  $ nuget-update-bot update MyApp.csproj \   │
│      --config myconfig.json                 │
│                                             │
└─────────────────────────────────────────────┘
```

## Common First-Time Questions

### Q: Will it break my project?
**A:** No! It creates a backup first, and you can always restore it.

### Q: Can I test without changing anything?
**A:** Yes! Use `--dry-run` flag.

### Q: What if I only want small updates?
**A:** Use a config file with `"UpdatePolicy": "Patch"`.

### Q: Can I exclude certain packages?
**A:** Yes! Use `"ExcludePackages": ["PackageName"]` in config.

### Q: How do I know what changed?
**A:** Run with `--verbose` flag for detailed output.

## Ready to Learn More?

Great! Check out these guides next:

- [Configuration Guide](CONFIGURATION.md) - How to customize behavior
- [Examples](EXAMPLES.md) - Real-world usage examples
- [Troubleshooting](TROUBLESHOOTING.md) - Fix common issues

Happy updating!
