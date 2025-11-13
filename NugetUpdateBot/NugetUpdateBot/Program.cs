using System.CommandLine;
using System.CommandLine.Invocation;
using System.Runtime.CompilerServices;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System.Xml.Linq;
using System.Text.Json;

[assembly: InternalsVisibleTo("NugetUpdateBot.Tests")]

// Exit codes
const int SUCCESS = 0;
const int GENERAL_ERROR = 1;
const int FILE_NOT_FOUND = 3;
const int NETWORK_ERROR = 4;

// Global state
var repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
var logger = NullLogger.Instance;
var throttler = new SemaphoreSlim(5, 5);

// Main command setup
var rootCommand = new RootCommand("NuGet Update Bot - Scan and update NuGet packages");

// Scan command
var scanCommand = new Command("scan", "Scan project for outdated NuGet packages");
var projectOption = new Option<string>("--project", "-p") { Description = "Path to .csproj file or directory", IsRequired = true };
var prereleaseOption = new Option<bool>("--include-prerelease") { Description = "Include pre-release versions" };
var verboseOption = new Option<bool>("--verbose", "-v") { Description = "Enable verbose output" };
var noCacheOption = new Option<bool>("--no-cache") { Description = "Bypass cache and fetch fresh data" };

scanCommand.AddOption(projectOption);
scanCommand.AddOption(prereleaseOption);
scanCommand.AddOption(verboseOption);
scanCommand.AddOption(noCacheOption);

scanCommand.SetHandler(async (string project, bool includePrerelease, bool verbose, bool noCache) =>
{
    Environment.ExitCode = await ScanCommandHandler(project, includePrerelease, verbose, noCache);
}, projectOption, prereleaseOption, verboseOption, noCacheOption);

rootCommand.AddCommand(scanCommand);

// Update command
var updateCommand = new Command("update", "Update outdated NuGet packages");
var updateProjectOption = new Option<string>("--project", "-p") { Description = "Path to .csproj file or directory", IsRequired = true };
var dryRunOption = new Option<bool>("--dry-run") { Description = "Preview updates without applying them" };
var policyOption = new Option<UpdatePolicy>("--policy", () => UpdatePolicy.Minor) { Description = "Update policy: Patch, Minor, or Major" };
var excludeOption = new Option<string[]>("--exclude", () => Array.Empty<string>()) { Description = "Packages to exclude from updates" };
var updateVerboseOption = new Option<bool>("--verbose", "-v") { Description = "Enable verbose output" };
var updatePrereleaseOption = new Option<bool>("--include-prerelease") { Description = "Include pre-release versions" };

updateCommand.AddOption(updateProjectOption);
updateCommand.AddOption(dryRunOption);
updateCommand.AddOption(policyOption);
updateCommand.AddOption(excludeOption);
updateCommand.AddOption(updateVerboseOption);
updateCommand.AddOption(updatePrereleaseOption);

updateCommand.SetHandler(async (string project, bool dryRun, UpdatePolicy policy, string[] exclude, bool verbose, bool includePrerelease) =>
{
    Environment.ExitCode = await UpdateCommandHandler(project, dryRun, policy, exclude, verbose, includePrerelease);
}, updateProjectOption, dryRunOption, policyOption, excludeOption, updateVerboseOption, updatePrereleaseOption);

rootCommand.AddCommand(updateCommand);

// Report command
var reportCommand = new Command("report", "Generate update report");
var reportProjectOption = new Option<string>("--project", "-p") { Description = "Path to .csproj file or directory", IsRequired = true };
var formatOption = new Option<OutputFormat>("--format", () => OutputFormat.Console) { Description = "Output format: Console or Json" };
var outputOption = new Option<string?>("--output", "-o") { Description = "Output file path (for JSON format)" };
var includeUpToDateOption = new Option<bool>("--include-up-to-date") { Description = "Include up-to-date packages in report" };
var reportVerboseOption = new Option<bool>("--verbose", "-v") { Description = "Enable verbose output" };
var reportPrereleaseOption = new Option<bool>("--include-prerelease") { Description = "Include pre-release versions" };

reportCommand.AddOption(reportProjectOption);
reportCommand.AddOption(formatOption);
reportCommand.AddOption(outputOption);
reportCommand.AddOption(includeUpToDateOption);
reportCommand.AddOption(reportVerboseOption);
reportCommand.AddOption(reportPrereleaseOption);

reportCommand.SetHandler(async (string project, OutputFormat format, string? output, bool includeUpToDate, bool verbose, bool includePrerelease) =>
{
    Environment.ExitCode = await ReportCommandHandler(project, format, output, includeUpToDate, verbose, includePrerelease);
}, reportProjectOption, formatOption, outputOption, includeUpToDateOption, reportVerboseOption, reportPrereleaseOption);

rootCommand.AddCommand(reportCommand);

// Execute
return await rootCommand.InvokeAsync(args);

// Command Handlers
async Task<int> ScanCommandHandler(string projectPath, bool includePrerelease, bool verbose, bool noCache)
{
    try
    {
        if (verbose)
        {
            Console.WriteLine($"Scanning project: {projectPath}");
        }

        if (!File.Exists(projectPath))
        {
            Console.Error.WriteLine($"Error: Project file not found: {projectPath}");
            return FILE_NOT_FOUND;
        }

        var packages = PackageScanner.ParseProjectFile(projectPath);
        if (verbose)
        {
            Console.WriteLine($"Found {packages.Count} package references");
        }

        var updates = await PackageScanner.CheckPackagesAsync(packages, repository, throttler, logger, includePrerelease, noCache, verbose);

        PackageScanner.DisplayScanResults(updates);
        return SUCCESS;
    }
    catch (HttpRequestException ex)
    {
        Console.Error.WriteLine($"Network error: {ex.Message}");
        return NETWORK_ERROR;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        return GENERAL_ERROR;
    }
}

async Task<int> UpdateCommandHandler(string projectPath, bool dryRun, UpdatePolicy policy, string[] exclude, bool verbose, bool includePrerelease)
{
    try
    {
        if (verbose)
        {
            Console.WriteLine($"Updating project: {projectPath}");
            Console.WriteLine($"Policy: {policy}");
            if (exclude.Length > 0)
            {
                Console.WriteLine($"Excluded packages: {string.Join(", ", exclude)}");
            }
        }

        if (!File.Exists(projectPath))
        {
            Console.Error.WriteLine($"Error: Project file not found: {projectPath}");
            return FILE_NOT_FOUND;
        }

        // Scan for updates
        var packages = PackageScanner.ParseProjectFile(projectPath);
        if (verbose)
        {
            Console.WriteLine($"Found {packages.Count} package references");
        }

        var updates = await PackageScanner.CheckPackagesAsync(packages, repository, throttler, logger, includePrerelease, noCache: true, verbose);

        // Apply policies and exclusions
        var filteredUpdates = PolicyEngine.ApplyPolicies(updates, policy, exclude);

        if (filteredUpdates.Count == 0)
        {
            Console.WriteLine("No updates to apply after filtering");
            return SUCCESS;
        }

        // Dry-run mode
        if (dryRun)
        {
            DryRunService.PreviewUpdates(projectPath, filteredUpdates);
            return SUCCESS;
        }

        // Apply updates
        if (verbose)
        {
            Console.WriteLine($"Creating backup...");
        }

        var backupPath = PackageUpdater.CreateBackup(projectPath);
        if (verbose)
        {
            Console.WriteLine($"Backup created: {backupPath}");
        }

        Console.WriteLine($"Applying {filteredUpdates.Count} update(s)...");
        var appliedCount = 0;

        foreach (var update in filteredUpdates)
        {
            try
            {
                var newVersion = update.LatestPrereleaseVersion ?? update.LatestStableVersion;
                PackageUpdater.UpdatePackageVersion(projectPath, update.PackageId, newVersion.ToString());

                if (verbose)
                {
                    Console.WriteLine($"Updated {update.PackageId}: {update.CurrentVersion} → {newVersion}");
                }

                appliedCount++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Warning: Failed to update {update.PackageId}: {ex.Message}");
            }
        }

        // Validate project file after updates
        if (!PackageUpdater.ValidateProjectFile(projectPath))
        {
            Console.Error.WriteLine("Error: Project file validation failed. Restoring from backup...");
            File.Copy(backupPath, projectPath, overwrite: true);
            Console.Error.WriteLine("Project file restored from backup");
            return GENERAL_ERROR;
        }

        Console.WriteLine($"\nSuccessfully updated {appliedCount} package(s)");
        Console.WriteLine($"Backup saved to: {backupPath}");

        return SUCCESS;
    }
    catch (HttpRequestException ex)
    {
        Console.Error.WriteLine($"Network error: {ex.Message}");
        return NETWORK_ERROR;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        return GENERAL_ERROR;
    }
}

async Task<int> ReportCommandHandler(string projectPath, OutputFormat format, string? output, bool includeUpToDate, bool verbose, bool includePrerelease)
{
    try
    {
        if (verbose)
        {
            Console.WriteLine($"Generating report for: {projectPath}");
            Console.WriteLine($"Format: {format}");
        }

        if (!File.Exists(projectPath))
        {
            Console.Error.WriteLine($"Error: Project file not found: {projectPath}");
            return FILE_NOT_FOUND;
        }

        // Scan for updates
        var packages = PackageScanner.ParseProjectFile(projectPath);
        if (verbose)
        {
            Console.WriteLine($"Found {packages.Count} package references");
        }

        var updates = await PackageScanner.CheckPackagesAsync(packages, repository, throttler, logger, includePrerelease, noCache: true, verbose);

        // Generate report based on format
        if (format == OutputFormat.Json)
        {
            var json = ReportGenerator.GenerateJsonReport(projectPath, updates, UpdateReportType.Preview);

            if (!string.IsNullOrEmpty(output))
            {
                ReportGenerator.SaveReportToFile(json, output);
                Console.WriteLine($"Report saved to: {output}");
            }
            else
            {
                Console.WriteLine(json);
            }
        }
        else // Console format
        {
            ConsoleReportFormatter.FormatConsoleReport(updates, projectPath, UpdateReportType.Preview);
        }

        return SUCCESS;
    }
    catch (HttpRequestException ex)
    {
        Console.Error.WriteLine($"Network error: {ex.Message}");
        return NETWORK_ERROR;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        return GENERAL_ERROR;
    }
}

internal static class PackageUpdater
{
    internal static void UpdatePackageVersion(string projectPath, string packageName, string newVersion)
    {
        try
        {
            var doc = XDocument.Load(projectPath);
            var packageRef = doc.Descendants("PackageReference")
                .FirstOrDefault(p => p.Attribute("Include")?.Value == packageName);

            if (packageRef == null)
            {
                throw new Exception($"Package '{packageName}' not found in project file");
            }

            var versionAttr = packageRef.Attribute("Version");
            if (versionAttr != null)
            {
                versionAttr.Value = newVersion;
            }

            doc.Save(projectPath);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to update package: {ex.Message}", ex);
        }
    }

    internal static string CreateBackup(string projectPath)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var directory = Path.GetDirectoryName(projectPath) ?? Path.GetTempPath();
        var fileName = Path.GetFileNameWithoutExtension(projectPath);
        var extension = Path.GetExtension(projectPath);
        var backupPath = Path.Combine(directory, $"{fileName}.backup.{timestamp}{extension}");

        File.Copy(projectPath, backupPath);
        return backupPath;
    }

    internal static bool ValidateProjectFile(string projectPath)
    {
        try
        {
            if (!File.Exists(projectPath))
            {
                return false;
            }

            var doc = XDocument.Load(projectPath);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

internal static class PolicyEngine
{
    internal static bool ShouldUpdate(NuGetVersion current, NuGetVersion latest, UpdatePolicy policy)
    {
        var updateType = PackageScanner.DetermineUpdateType(current, latest);

        return policy switch
        {
            UpdatePolicy.Patch => updateType == UpdateType.Patch,
            UpdatePolicy.Minor => updateType is UpdateType.Patch or UpdateType.Minor,
            UpdatePolicy.Major => true,
            _ => false
        };
    }

    internal static List<UpdateCandidate> FilterUpdatesByPolicy(List<UpdateCandidate> updates, UpdatePolicy policy)
    {
        return updates.Where(u =>
        {
            return policy switch
            {
                UpdatePolicy.Patch => u.UpdateType == UpdateType.Patch,
                UpdatePolicy.Minor => u.UpdateType is UpdateType.Patch or UpdateType.Minor,
                UpdatePolicy.Major => true,
                _ => false
            };
        }).ToList();
    }

    internal static List<UpdateCandidate> FilterByExclusions(List<UpdateCandidate> updates, string[] exclusions)
    {
        if (exclusions == null || exclusions.Length == 0)
        {
            return updates;
        }

        return updates.Where(u => !IsPackageExcluded(u.PackageId, exclusions)).ToList();
    }

    internal static bool IsPackageExcluded(string packageId, string[] exclusions)
    {
        return exclusions.Any(e => string.Equals(e, packageId, StringComparison.OrdinalIgnoreCase));
    }

    internal static List<UpdateCandidate> ApplyPolicies(List<UpdateCandidate> updates, UpdatePolicy policy, string[] exclusions)
    {
        var filtered = FilterUpdatesByPolicy(updates, policy);
        return FilterByExclusions(filtered, exclusions);
    }
}

internal static class DryRunService
{
    internal static void PreviewUpdates(string projectPath, List<UpdateCandidate> updates)
    {
        Console.WriteLine("DRY RUN - No changes will be made");
        Console.WriteLine();

        if (updates.Count == 0)
        {
            Console.WriteLine("No updates to apply");
            return;
        }

        Console.WriteLine($"The following updates would be applied to: {Path.GetFileName(projectPath)}");
        Console.WriteLine(new string('-', 80));
        Console.WriteLine($"{"Package",-40} {"Current",10} → {"New",10} {"Type",10}");
        Console.WriteLine(new string('-', 80));

        foreach (var update in updates)
        {
            var newVersion = update.LatestPrereleaseVersion ?? update.LatestStableVersion;
            Console.WriteLine($"{update.PackageId,-40} {update.CurrentVersion,10} → {newVersion,10} {update.UpdateType,10}");
        }

        Console.WriteLine(new string('-', 80));
        GenerateDryRunSummary(updates);
    }

    internal static string FormatDryRunOutput(List<UpdateCandidate> updates)
    {
        var lines = new List<string>();

        foreach (var update in updates)
        {
            var newVersion = update.LatestPrereleaseVersion ?? update.LatestStableVersion;
            lines.Add($"{update.PackageId}: {update.CurrentVersion} → {newVersion}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    internal static void GenerateDryRunSummary(List<UpdateCandidate> updates)
    {
        var patchCount = updates.Count(u => u.UpdateType == UpdateType.Patch);
        var minorCount = updates.Count(u => u.UpdateType == UpdateType.Minor);
        var majorCount = updates.Count(u => u.UpdateType == UpdateType.Major);

        Console.WriteLine($"Total updates: {updates.Count}");
        Console.WriteLine($"  Patch: {patchCount}");
        Console.WriteLine($"  Minor: {minorCount}");
        Console.WriteLine($"  Major: {majorCount}");
    }

    internal static void CompareBeforeAfter(string packageName, string oldVersion, string newVersion)
    {
        Console.WriteLine($"  - {packageName}: {oldVersion}");
        Console.WriteLine($"  + {packageName}: {newVersion}");
    }
}

internal static class PackageScanner
{
    internal static List<PackageReference> ParseProjectFile(string projectPath)
    {
        var packages = new List<PackageReference>();

        try
        {
            var doc = XDocument.Load(projectPath);
            var packageRefs = doc.Descendants("PackageReference");

            foreach (var packageRef in packageRefs)
            {
                var name = packageRef.Attribute("Include")?.Value;
                var versionStr = packageRef.Attribute("Version")?.Value;

                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(versionStr))
                {
                    continue;
                }

                if (NuGetVersion.TryParse(versionStr, out var version))
                {
                    packages.Add(new PackageReference(name, version));
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse project file: {ex.Message}", ex);
        }

        return packages;
    }

    internal static async Task<List<UpdateCandidate>> CheckPackagesAsync(
        List<PackageReference> packages,
        SourceRepository repository,
        SemaphoreSlim throttler,
        ILogger logger,
        bool includePrerelease,
        bool noCache,
        bool verbose)
{
    var updates = new List<UpdateCandidate>();

    foreach (var package in packages)
    {
        if (verbose)
        {
            Console.WriteLine($"Checking {package.Name}...");
        }

        try
        {
            var versions = await GetAllVersionsAsync(repository, throttler, logger, package.Name, noCache);
            var latestStable = versions
                .Where(v => !v.IsPrerelease)
                .Where(v => v > package.Version)
                .OrderByDescending(v => v)
                .FirstOrDefault();

            var latestPrerelease = includePrerelease
                ? versions
                    .Where(v => v.IsPrerelease)
                    .Where(v => v > package.Version)
                    .OrderByDescending(v => v)
                    .FirstOrDefault()
                : null;

            var latest = latestStable;
            if (latestPrerelease != null && (latestStable == null || latestPrerelease > latestStable))
            {
                latest = latestPrerelease;
            }

            if (latest != null)
            {
                var updateType = DetermineUpdateType(package.Version, latest);
                updates.Add(new UpdateCandidate(
                    package.Name,
                    package.Version,
                    latestStable ?? package.Version,
                    latestPrerelease,
                    updateType,
                    false
                ));
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: Failed to check {package.Name}: {ex.Message}");
        }
    }

    return updates;
    }

    internal static void DisplayScanResults(List<UpdateCandidate> updates)
    {
        if (updates.Count == 0)
        {
            Console.WriteLine("All packages are up to date!");
            return;
        }

        Console.WriteLine($"\nFound {updates.Count} outdated packages:");
        Console.WriteLine(new string('-', 80));
        Console.WriteLine($"{"Package",-40} {"Current",10} → {"Latest",10}");
        Console.WriteLine(new string('-', 80));

        foreach (var update in updates)
        {
            var latest = update.LatestPrereleaseVersion ?? update.LatestStableVersion;
            Console.WriteLine($"{update.PackageId,-40} {update.CurrentVersion,10} → {latest,10}");
        }
    }

    internal static async Task<IEnumerable<NuGetVersion>> GetAllVersionsAsync(
        SourceRepository repository,
        SemaphoreSlim throttler,
        ILogger logger,
        string packageId,
        bool noCache,
        CancellationToken cancellationToken = default)
    {
        await throttler.WaitAsync(cancellationToken);
        try
        {
            var resource = await repository.GetResourceAsync<FindPackageByIdResource>();
            using var cache = new SourceCacheContext { NoCache = noCache };

            return await resource.GetAllVersionsAsync(
                packageId,
                cache,
                logger,
                cancellationToken) ?? Enumerable.Empty<NuGetVersion>();
        }
        finally
        {
            throttler.Release();
        }
    }

    internal static UpdateType DetermineUpdateType(NuGetVersion current, NuGetVersion latest)
    {
        if (latest.IsPrerelease && !current.IsPrerelease)
        {
            return UpdateType.Prerelease;
        }

        if (latest.Major > current.Major)
        {
            return UpdateType.Major;
        }

        if (latest.Minor > current.Minor)
        {
            return UpdateType.Minor;
        }

        return UpdateType.Patch;
    }
}

internal static class ReportGenerator
{
    internal static string GenerateJsonReport(string projectPath, List<UpdateCandidate> updates, UpdateReportType reportType)
    {
        var totalPackages = updates.Count;
        var summary = SummaryCalculator.CalculateSummary(updates, totalPackages, excludedCount: 0);

        var report = new UpdateReport(
            GeneratedAt: DateTime.UtcNow,
            ProjectPath: projectPath,
            Updates: updates,
            Type: reportType,
            Summary: summary
        );

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = null // Use Pascal case as-is
        };

        return JsonSerializer.Serialize(report, options);
    }

    internal static void SaveReportToFile(string json, string outputPath)
    {
        File.WriteAllText(outputPath, json);
    }
}

internal static class ConsoleReportFormatter
{
    internal static void FormatConsoleReport(List<UpdateCandidate> updates, string projectPath, UpdateReportType reportType)
    {
        var projectName = Path.GetFileName(projectPath);

        Console.WriteLine();
        Console.WriteLine($"Update Report - {reportType}");
        Console.WriteLine($"Project: {projectName}");
        Console.WriteLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine(new string('=', 80));

        if (updates.Count == 0)
        {
            Console.WriteLine("No outdated packages found");
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"{"Package",-40} {"Current",10} → {"Latest",10} {"Type",10}");
        Console.WriteLine(new string('-', 80));

        foreach (var update in updates)
        {
            var latest = update.LatestPrereleaseVersion ?? update.LatestStableVersion;
            var packageName = update.PackageId;

            if (update.IsDeprecated)
            {
                packageName += " [DEPRECATED]";
            }

            Console.WriteLine($"{packageName,-40} {update.CurrentVersion,10} → {latest,10} {update.UpdateType,10}");
        }

        Console.WriteLine(new string('-', 80));
        Console.WriteLine();

        // Summary
        var summary = SummaryCalculator.CalculateSummary(updates, updates.Count, excludedCount: 0);
        Console.WriteLine("Summary:");
        Console.WriteLine($"  Total outdated: {summary.OutdatedCount}");
        Console.WriteLine($"  Major updates:  {summary.MajorUpdates}");
        Console.WriteLine($"  Minor updates:  {summary.MinorUpdates}");
        Console.WriteLine($"  Patch updates:  {summary.PatchUpdates}");
        Console.WriteLine();
    }
}

internal static class SummaryCalculator
{
    internal static UpdateSummary CalculateSummary(List<UpdateCandidate> updates, int totalPackages, int excludedCount)
    {
        var outdatedCount = updates.Count;
        var majorUpdates = updates.Count(u => u.UpdateType == UpdateType.Major);
        var minorUpdates = updates.Count(u => u.UpdateType == UpdateType.Minor);
        var patchUpdates = updates.Count(u => u.UpdateType == UpdateType.Patch);

        return new UpdateSummary(
            TotalPackages: totalPackages,
            OutdatedCount: outdatedCount,
            MajorUpdates: majorUpdates,
            MinorUpdates: minorUpdates,
            PatchUpdates: patchUpdates,
            ExcludedCount: excludedCount
        );
    }
}

internal static class ConfigurationLoader
{
    internal static BotConfiguration LoadFromFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return GetDefaultConfiguration();
            }

            var json = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(json) || json.Trim() == "{}")
            {
                return GetDefaultConfiguration();
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            };

            // Parse JSON manually to handle missing properties with defaults
            using var doc = JsonDocument.Parse(json, new JsonDocumentOptions { AllowTrailingCommas = true });
            var root = doc.RootElement;

            var updatePolicy = UpdatePolicy.Major;
            if (root.TryGetProperty("UpdatePolicy", out var policyElement) ||
                root.TryGetProperty("updatePolicy", out policyElement))
            {
                if (Enum.TryParse<UpdatePolicy>(policyElement.GetString(), ignoreCase: true, out var parsedPolicy))
                {
                    updatePolicy = parsedPolicy;
                }
            }

            var excludePackages = new List<string>();
            if (root.TryGetProperty("ExcludePackages", out var excludeElement) ||
                root.TryGetProperty("excludePackages", out excludeElement))
            {
                if (excludeElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in excludeElement.EnumerateArray())
                    {
                        var pkg = item.GetString();
                        if (!string.IsNullOrEmpty(pkg))
                        {
                            excludePackages.Add(pkg);
                        }
                    }
                }
            }

            var includePrerelease = false;
            if (root.TryGetProperty("IncludePrerelease", out var prereleaseElement) ||
                root.TryGetProperty("includePrerelease", out prereleaseElement))
            {
                includePrerelease = prereleaseElement.GetBoolean();
            }

            var maxParallelism = 5;
            if (root.TryGetProperty("MaxParallelism", out var parallelismElement) ||
                root.TryGetProperty("maxParallelism", out parallelismElement))
            {
                maxParallelism = parallelismElement.GetInt32();
            }

            var updateRules = new List<UpdateRule>();
            if (root.TryGetProperty("UpdateRules", out var rulesElement) ||
                root.TryGetProperty("updateRules", out rulesElement))
            {
                if (rulesElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var ruleElement in rulesElement.EnumerateArray())
                    {
                        var pattern = "";
                        if (ruleElement.TryGetProperty("Pattern", out var patternElement) ||
                            ruleElement.TryGetProperty("pattern", out patternElement))
                        {
                            pattern = patternElement.GetString() ?? "";
                        }

                        var policy = UpdatePolicy.Major;
                        if (ruleElement.TryGetProperty("Policy", out var rulePolicyElement) ||
                            ruleElement.TryGetProperty("policy", out rulePolicyElement))
                        {
                            if (Enum.TryParse<UpdatePolicy>(rulePolicyElement.GetString(), ignoreCase: true, out var parsedRulePolicy))
                            {
                                policy = parsedRulePolicy;
                            }
                        }

                        if (!string.IsNullOrEmpty(pattern))
                        {
                            updateRules.Add(new UpdateRule(pattern, policy));
                        }
                    }
                }
            }

            return new BotConfiguration(
                UpdatePolicy: updatePolicy,
                ExcludePackages: excludePackages,
                IncludePrerelease: includePrerelease,
                MaxParallelism: maxParallelism,
                UpdateRules: updateRules
            );
        }
        catch (JsonException)
        {
            // Invalid JSON - return defaults
            return GetDefaultConfiguration();
        }
        catch (Exception)
        {
            // Any other error - return defaults
            return GetDefaultConfiguration();
        }
    }

    internal static BotConfiguration LoadFromEnvironment()
    {
        var updatePolicy = UpdatePolicy.Major;
        var envPolicy = Environment.GetEnvironmentVariable("NUGET_BOT_UPDATE_POLICY");
        if (!string.IsNullOrEmpty(envPolicy))
        {
            if (Enum.TryParse<UpdatePolicy>(envPolicy, ignoreCase: true, out var parsedPolicy))
            {
                updatePolicy = parsedPolicy;
            }
        }

        var includePrerelease = false;
        var envPrerelease = Environment.GetEnvironmentVariable("NUGET_BOT_INCLUDE_PRERELEASE");
        if (!string.IsNullOrEmpty(envPrerelease))
        {
            bool.TryParse(envPrerelease, out includePrerelease);
        }

        var maxParallelism = 5;
        var envParallelism = Environment.GetEnvironmentVariable("NUGET_BOT_MAX_PARALLELISM");
        if (!string.IsNullOrEmpty(envParallelism))
        {
            if (int.TryParse(envParallelism, out var parsedParallelism) && parsedParallelism > 0)
            {
                maxParallelism = parsedParallelism;
            }
        }

        var excludePackages = new List<string>();
        var envExclude = Environment.GetEnvironmentVariable("NUGET_BOT_EXCLUDE_PACKAGES");
        if (!string.IsNullOrEmpty(envExclude))
        {
            excludePackages = envExclude
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p))
                .ToList();
        }

        return new BotConfiguration(
            UpdatePolicy: updatePolicy,
            ExcludePackages: excludePackages,
            IncludePrerelease: includePrerelease,
            MaxParallelism: maxParallelism,
            UpdateRules: new List<UpdateRule>()
        );
    }

    internal static BotConfiguration MergeConfigurations(BotConfiguration fileConfig, BotConfiguration overrideConfig)
    {
        // Override config takes precedence over file config
        return new BotConfiguration(
            UpdatePolicy: overrideConfig.UpdatePolicy,
            ExcludePackages: overrideConfig.ExcludePackages.Count > 0 ? overrideConfig.ExcludePackages : fileConfig.ExcludePackages,
            IncludePrerelease: overrideConfig.IncludePrerelease,
            MaxParallelism: overrideConfig.MaxParallelism,
            UpdateRules: overrideConfig.UpdateRules.Count > 0 ? overrideConfig.UpdateRules : fileConfig.UpdateRules
        );
    }

    internal static bool ValidateConfiguration(BotConfiguration config, out List<string> errors)
    {
        errors = new List<string>();

        if (config.MaxParallelism <= 0)
        {
            errors.Add("MaxParallelism must be greater than 0");
        }

        foreach (var rule in config.UpdateRules)
        {
            if (string.IsNullOrWhiteSpace(rule.Pattern))
            {
                errors.Add("UpdateRule Pattern cannot be empty");
            }
        }

        return errors.Count == 0;
    }

    private static BotConfiguration GetDefaultConfiguration()
    {
        return new BotConfiguration(
            UpdatePolicy: UpdatePolicy.Major,
            ExcludePackages: new List<string>(),
            IncludePrerelease: false,
            MaxParallelism: 5,
            UpdateRules: new List<UpdateRule>()
        );
    }
}

internal static class RuleMatcher
{
    internal static bool MatchesPattern(string packageName, string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return false;
        }

        // Convert glob pattern to regex
        // * matches any characters within a segment
        // Exact match if no wildcards
        if (!pattern.Contains('*'))
        {
            return packageName.Equals(pattern, StringComparison.OrdinalIgnoreCase);
        }

        // Convert glob to regex pattern
        var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace("\\*", ".*")
            + "$";

        return System.Text.RegularExpressions.Regex.IsMatch(
            packageName,
            regexPattern,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );
    }

    internal static UpdateRule? FindMatchingRule(string packageName, List<UpdateRule> rules)
    {
        foreach (var rule in rules)
        {
            if (rule.Pattern != null && MatchesPattern(packageName, rule.Pattern))
            {
                return rule;
            }
        }

        return null;
    }

    internal static UpdatePolicy GetEffectivePolicy(string packageName, List<UpdateRule> rules, UpdatePolicy defaultPolicy)
    {
        var matchingRule = FindMatchingRule(packageName, rules);
        return matchingRule?.Policy ?? defaultPolicy;
    }

    internal static bool IsPackageExcluded(string packageName, List<string> excludeList)
    {
        if (excludeList.Count == 0)
        {
            return false;
        }

        foreach (var pattern in excludeList)
        {
            if (MatchesPattern(packageName, pattern))
            {
                return true;
            }
        }

        return false;
    }
}

// Type Definitions
public enum UpdateType { Patch, Minor, Major, Prerelease }
public enum UpdatePolicy { Patch, Minor, Major }
public enum ProjectType { Console, ClassLibrary, Web, Test, Unknown }
public enum UpdateReportType { Preview, Applied }
public enum OutputFormat { Console, Json }

public record PackageReference(string Name, NuGetVersion Version, string? TargetFramework = null);

public record UpdateCandidate(
    string PackageId,
    NuGetVersion CurrentVersion,
    NuGetVersion LatestStableVersion,
    NuGetVersion? LatestPrereleaseVersion,
    UpdateType UpdateType,
    bool IsDeprecated = false
);

internal record UpdateRule(
    string Pattern,
    UpdatePolicy Policy
);

internal record ProjectContext(
    string FilePath,
    string ProjectName,
    List<PackageReference> Packages,
    string? TargetFramework,
    ProjectType Type
);

internal record UpdateReport(
    DateTime GeneratedAt,
    string ProjectPath,
    List<UpdateCandidate> Updates,
    UpdateReportType Type,
    UpdateSummary Summary
);

internal record UpdateSummary(
    int TotalPackages,
    int OutdatedCount,
    int MajorUpdates,
    int MinorUpdates,
    int PatchUpdates,
    int ExcludedCount
);

internal record BotConfiguration(
    UpdatePolicy UpdatePolicy,
    List<string> ExcludePackages,
    bool IncludePrerelease,
    int MaxParallelism,
    List<UpdateRule> UpdateRules
);
