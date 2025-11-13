using System.Xml.Linq;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NugetUpdateBot.Models;

namespace NugetUpdateBot.Services;

/// <summary>
/// Service responsible for scanning projects and checking for package updates.
/// Follows Single Responsibility Principle - only handles package scanning logic.
/// </summary>
public class PackageScannerService
{
    private readonly SourceRepository _repository;
    private readonly SemaphoreSlim _throttler;
    private readonly ILogger _logger;

    public PackageScannerService(
        SourceRepository repository,
        SemaphoreSlim throttler,
        ILogger logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _throttler = throttler ?? throw new ArgumentNullException(nameof(throttler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Parses a project file and extracts package references.
    /// </summary>
    public List<PackageReference> ParseProjectFile(string projectPath)
    {
        var packages = new List<PackageReference>();

        try
        {
            var doc = XDocument.Load(projectPath);
            var packageReferences = doc.Descendants("PackageReference");

            foreach (var packageRef in packageReferences)
            {
                var name = packageRef.Attribute("Include")?.Value;
                var versionStr = packageRef.Attribute("Version")?.Value;

                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(versionStr))
                {
                    if (NuGetVersion.TryParse(versionStr, out var version))
                    {
                        packages.Add(new PackageReference(name, version));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse project file: {ex.Message}", ex);
        }

        return packages;
    }

    /// <summary>
    /// Checks packages for available updates.
    /// </summary>
    public async Task<List<UpdateCandidate>> CheckPackagesAsync(
        List<PackageReference> packages,
        bool includePrerelease,
        bool noCache,
        bool verbose,
        CancellationToken cancellationToken = default)
    {
        var updates = new List<UpdateCandidate>();

        foreach (var package in packages)
        {
            try
            {
                if (verbose)
                {
                    Console.WriteLine($"Checking {package.Name}...");
                }

                var versions = await GetAllVersionsAsync(package.Name, noCache, cancellationToken);
                var allVersions = versions.ToList();

                if (!allVersions.Any())
                {
                    continue;
                }

                var latestStable = allVersions
                    .Where(v => !v.IsPrerelease)
                    .OrderByDescending(v => v)
                    .FirstOrDefault();

                var latestPrerelease = includePrerelease
                    ? allVersions
                        .Where(v => v.IsPrerelease)
                        .OrderByDescending(v => v)
                        .FirstOrDefault()
                    : null;

                if (latestStable == null && latestPrerelease == null)
                {
                    continue;
                }

                latestStable ??= package.Version;

                if (latestStable > package.Version || (latestPrerelease != null && latestPrerelease > package.Version))
                {
                    var updateType = DetermineUpdateType(package.Version, latestStable);

                    updates.Add(new UpdateCandidate(
                        package.Name,
                        package.Version,
                        latestStable,
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

    /// <summary>
    /// Gets all available versions for a package from NuGet.
    /// </summary>
    private async Task<IEnumerable<NuGetVersion>> GetAllVersionsAsync(
        string packageId,
        bool noCache,
        CancellationToken cancellationToken)
    {
        await _throttler.WaitAsync(cancellationToken);
        try
        {
            var resource = await _repository.GetResourceAsync<FindPackageByIdResource>();
            using var cache = new SourceCacheContext { NoCache = noCache };

            return await resource.GetAllVersionsAsync(
                packageId,
                cache,
                _logger,
                cancellationToken) ?? Enumerable.Empty<NuGetVersion>();
        }
        finally
        {
            _throttler.Release();
        }
    }

    /// <summary>
    /// Determines the type of update between two versions.
    /// </summary>
    private static UpdateType DetermineUpdateType(NuGetVersion current, NuGetVersion latest)
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

    /// <summary>
    /// Displays scan results to the console.
    /// </summary>
    public void DisplayScanResults(List<UpdateCandidate> updates)
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
}
