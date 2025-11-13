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
    string? PackagePattern,
    UpdatePolicy Policy,
    bool AllowPrerelease = false,
    string? MaxVersion = null
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
    string[] ExcludedPackages,
    UpdatePolicy DefaultPolicy,
    bool IncludePrerelease,
    int TimeoutSeconds,
    OutputFormat OutputFormat,
    List<UpdateRule> Rules
);
