using System.CommandLine;
using System.CommandLine.Invocation;
using System.Runtime.CompilerServices;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System.Xml.Linq;

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
