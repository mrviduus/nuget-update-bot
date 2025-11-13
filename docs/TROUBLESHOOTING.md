# Troubleshooting Guide

Having problems? This guide will help you fix common issues.

## Quick Fixes (Try These First!)

### Problem: Command Not Found

**Error:**
```bash
nuget-update-bot: command not found
```

**Solutions:**

#### Solution 1: Tool Not Installed
```bash
# Install it
dotnet tool install --global NugetUpdateBot

# Verify installation
dotnet tool list --global
```

#### Solution 2: PATH Not Updated
```bash
# Close and reopen your terminal
# Or add to PATH manually (Mac/Linux)
export PATH="$PATH:$HOME/.dotnet/tools"
```

### Problem: File Not Found

**Error:**
```bash
Error: Could not find project file 'MyApp.csproj'
```

**Solutions:**

#### Solution 1: Wrong Directory
```bash
# Check where you are
pwd

# List files
ls

# Navigate to correct folder
cd /path/to/your/project

# Try again
nuget-update-bot scan MyApp.csproj
```

#### Solution 2: Wrong Filename
```bash
# List all .csproj files
ls *.csproj

# Use the correct name
nuget-update-bot scan CorrectName.csproj
```

#### Solution 3: Use Full Path
```bash
nuget-update-bot scan /full/path/to/MyApp.csproj
```

### Problem: Updates Broke My Project

**Error:**
```bash
dotnet build
# Build FAILED
```

**Solution: Restore from Backup**

```bash
# Find the backup
ls *.backup.*

# Example output:
# MyApp.csproj.backup.20250113-143022

# Restore it
cp MyApp.csproj.backup.20250113-143022 MyApp.csproj

# Verify build works
dotnet build
```

### Problem: Invalid JSON Config

**Error:**
```bash
Error: Invalid configuration file
```

**Common Mistakes:**

#### Mistake 1: Missing Comma
```json
{
  "UpdatePolicy": "Major"     ❌ Missing comma
  "IncludePrerelease": false
}
```

**Fix:**
```json
{
  "UpdatePolicy": "Major",    ✅ Added comma
  "IncludePrerelease": false
}
```

#### Mistake 2: Extra Comma
```json
{
  "UpdatePolicy": "Major",
  "IncludePrerelease": false,  ❌ Extra comma
}
```

**Fix:**
```json
{
  "UpdatePolicy": "Major",
  "IncludePrerelease": false   ✅ Removed extra comma
}
```

#### Mistake 3: Wrong Quotes
```json
{
  "UpdatePolicy": 'Major'      ❌ Single quotes
}
```

**Fix:**
```json
{
  "UpdatePolicy": "Major"      ✅ Double quotes
}
```

#### Validate Your JSON:
Use an online JSON validator: https://jsonlint.com/

## Common Errors and Solutions

### Error: "Package Not Found"

**Full Error:**
```
Error: Package 'NonExistentPackage' not found in project
```

**What it means:** You're trying to update a package that doesn't exist in your project.

**Solution:**
```bash
# First, scan to see what packages you have
nuget-update-bot scan MyApp.csproj --verbose
```

### Error: "Invalid XML in Project File"

**Full Error:**
```
Error: Project file contains invalid XML
```

**What it means:** Your .csproj file has broken XML.

**Solution 1: Restore from Backup**
```bash
cp MyApp.csproj.backup.20250113-143022 MyApp.csproj
```

**Solution 2: Check for Common XML Mistakes**

Look for:
- Unclosed tags: `<Project>` without `</Project>`
- Mismatched tags: `<ItemGroup>` closed with `</PropertyGroup>`
- Special characters not escaped: `&` should be `&amp;`

**Solution 3: Use Visual Studio or VS Code**
Open the file in an IDE - it will show you where the XML is broken.

### Error: "Access Denied" or "Permission Denied"

**Full Error:**
```
Error: Access denied when writing to MyApp.csproj
```

**Solutions:**

#### Solution 1: File is Read-Only
```bash
# Mac/Linux
chmod +w MyApp.csproj

# Windows
attrib -r MyApp.csproj
```

#### Solution 2: File is Open in Another Program
Close Visual Studio, VS Code, or any text editor that has the file open.

#### Solution 3: Run with Proper Permissions
```bash
# Mac/Linux (use carefully!)
sudo nuget-update-bot update MyApp.csproj
```

### Error: "Connection Failed" or "Network Error"

**Full Error:**
```
Error: Failed to connect to NuGet.org
```

**Solutions:**

#### Solution 1: Check Internet Connection
```bash
# Test connection to NuGet.org
ping api.nuget.org
```

#### Solution 2: Check Firewall
Make sure your firewall allows connections to `api.nuget.org`

#### Solution 3: Use --no-cache
```bash
# Force fresh data
nuget-update-bot scan MyApp.csproj --no-cache
```

#### Solution 4: Corporate Proxy
If you're behind a corporate proxy, configure NuGet:
```bash
# Set proxy
dotnet nuget config --set http_proxy=http://proxy.company.com:8080
```

### Error: "Backup Failed"

**Full Error:**
```
Error: Could not create backup file
```

**Solutions:**

#### Solution 1: Disk Full
```bash
# Check disk space
df -h

# Free up space if needed
```

#### Solution 2: Permission Issues
```bash
# Check if you can write to the directory
touch test.txt
rm test.txt
```

## Project-Specific Issues

### Issue: .NET Version Mismatch

**Symptom:**
```bash
dotnet build
# error: The current .NET SDK does not support targeting .NET 9.0
```

**Solution:**
```bash
# Check installed .NET versions
dotnet --list-sdks

# Install .NET 9.0 if needed
# Download from: https://dotnet.microsoft.com/download
```

### Issue: Package Conflicts After Update

**Symptom:**
```bash
dotnet build
# error: Package 'PackageA 2.0.0' is not compatible with 'PackageB 1.0.0'
```

**Solution 1: Check Dependencies**
Some packages depend on specific versions of other packages.

**Solution 2: Restore from Backup**
```bash
cp MyApp.csproj.backup.20250113-143022 MyApp.csproj
```

**Solution 3: Update in Stages**
```bash
# Use patch updates only
nuget-update-bot update MyApp.csproj --config patch-only.json
```

**patch-only.json:**
```json
{
  "UpdatePolicy": "Patch",
  "IncludePrerelease": false
}
```

### Issue: Breaking Changes After Update

**Symptom:**
```bash
dotnet build
# error: 'OldMethod' does not exist in 'NewLibrary'
```

**What it means:** The new version has breaking changes.

**Solution 1: Exclude That Package**

Create config:
```json
{
  "UpdatePolicy": "Minor",
  "ExcludePackages": ["NewLibrary"]
}
```

**Solution 2: Update Your Code**
Check the package's release notes and update your code to use the new API.

**Solution 3: Use Minor Updates Only**
```json
{
  "UpdatePolicy": "Minor"
}
```

## Performance Issues

### Issue: Scanning is Very Slow

**Symptom:** Scanning takes several minutes.

**Solutions:**

#### Solution 1: Network is Slow
```bash
# Use cache if available (remove --no-cache)
nuget-update-bot scan MyApp.csproj
```

#### Solution 2: Many Packages
This is normal for projects with 50+ packages. Be patient!

#### Solution 3: Check Network
```bash
# Test download speed to NuGet.org
curl -o /dev/null https://api.nuget.org/v3/index.json
```

### Issue: Update Takes Forever

**Symptom:** Update command hangs or takes very long.

**Solutions:**

#### Solution 1: Verbose Mode
See what it's doing:
```bash
nuget-update-bot update MyApp.csproj --verbose
```

#### Solution 2: Check File Locks
Make sure no other program is using the .csproj file.

#### Solution 3: Simplify
Update one package at a time manually to find the problematic one.

## Configuration Issues

### Issue: Config File Not Found

**Error:**
```
Error: Configuration file 'config.json' not found
```

**Solutions:**

#### Solution 1: Wrong Path
```bash
# Use full path
nuget-update-bot scan MyApp.csproj --config /full/path/to/config.json
```

#### Solution 2: Check Filename
```bash
# List config files
ls *.json

# Use correct name
nuget-update-bot scan MyApp.csproj --config myconfig.json
```

### Issue: Config Not Taking Effect

**Symptom:** Updates ignore your configuration.

**Solution: Verify Config Loads**
```bash
# Use verbose to see config loading
nuget-update-bot scan MyApp.csproj --config myconfig.json --verbose
```

Output should show:
```
Loading configuration from: myconfig.json
Update policy: Patch
Include prerelease: false
Excluded packages: PackageA, PackageB
```

## Getting More Help

### Enable Verbose Mode

For any problem, try verbose mode first:
```bash
nuget-update-bot scan MyApp.csproj --verbose
```

This shows detailed information about what's happening.

### Check Exit Codes

```bash
nuget-update-bot scan MyApp.csproj
echo $?  # Shows exit code
```

Exit codes:
- `0` = Success
- `1` = Updates available
- `-1` = Error occurred

### Collect Debug Information

When reporting bugs, include:

```bash
# 1. Version information
dotnet --version
dotnet tool list --global

# 2. Your command
nuget-update-bot scan MyApp.csproj --verbose

# 3. Project file (sanitized)
cat MyApp.csproj

# 4. Config file (if used)
cat config.json
```

### Create a Minimal Reproduction

If you found a bug:

1. Create a tiny test project:
```bash
dotnet new console -n TestProject
cd TestProject
```

2. Try to reproduce the issue:
```bash
nuget-update-bot scan TestProject.csproj --verbose
```

3. Report it with this minimal example.

## Still Having Problems?

### Check These:

1. ✅ .NET SDK installed and working?
   ```bash
   dotnet --version
   ```

2. ✅ Tool installed correctly?
   ```bash
   dotnet tool list --global | grep NugetUpdateBot
   ```

3. ✅ Project file is valid?
   ```bash
   dotnet build MyApp.csproj
   ```

4. ✅ Internet connection works?
   ```bash
   ping api.nuget.org
   ```

5. ✅ Config file is valid JSON?
   Use https://jsonlint.com/

### Get Support:

- GitHub Issues: Report bugs
- Documentation: Re-read the guides
- Community: Ask other users

## Prevention Tips

### Tip 1: Always Use Dry Run First
```bash
nuget-update-bot update MyApp.csproj --dry-run
```

### Tip 2: Start with Patch Updates
```bash
nuget-update-bot update MyApp.csproj --config patch-only.json
```

### Tip 3: Keep Backups
Don't delete `.backup.*` files immediately.

### Tip 4: Use Version Control
```bash
git checkout -b package-updates
nuget-update-bot update MyApp.csproj
# Test
# If broken: git checkout main
```

### Tip 5: Read Release Notes
Before major updates, check the package's release notes for breaking changes.

## Quick Checklist

When something goes wrong:

- [ ] Did I use the correct project file path?
- [ ] Is the project file valid XML?
- [ ] Do I have internet connection?
- [ ] Is my config file valid JSON?
- [ ] Did I try `--verbose` mode?
- [ ] Did I try `--dry-run` first?
- [ ] Can I restore from a backup?
- [ ] Did I check the exit code?
- [ ] Have I tried restarting my terminal?

If you checked all these and still have problems, it might be a bug - please report it!
