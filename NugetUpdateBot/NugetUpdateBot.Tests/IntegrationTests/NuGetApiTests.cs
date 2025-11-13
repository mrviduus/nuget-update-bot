using Xunit;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace NugetUpdateBot.Tests.IntegrationTests;

public class NuGetApiTests
{
    private readonly SourceRepository _repository;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _throttler;

    public NuGetApiTests()
    {
        _repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
        _logger = NullLogger.Instance;
        _throttler = new SemaphoreSlim(5, 5);
    }

    [Fact]
    public async Task GetAllVersionsAsync_ValidPackage_ReturnsVersions()
    {
        // Arrange - use a well-known package
        var packageId = "Newtonsoft.Json";

        // Act
        var versions = await PackageScanner.GetAllVersionsAsync(
            _repository,
            _throttler,
            _logger,
            packageId,
            noCache: true);

        // Assert
        Assert.NotNull(versions);
        Assert.NotEmpty(versions);
        // Newtonsoft.Json should have many versions including 13.0.3
        Assert.Contains(versions, v => v.ToString() == "13.0.3");
    }

    [Fact]
    public async Task GetAllVersionsAsync_NonExistentPackage_ReturnsEmpty()
    {
        // Arrange - use a package name that definitely doesn't exist
        var packageId = "ThisPackageDefinitelyDoesNotExist" + Guid.NewGuid().ToString();

        // Act
        var versions = await PackageScanner.GetAllVersionsAsync(
            _repository,
            _throttler,
            _logger,
            packageId,
            noCache: true);

        // Assert
        Assert.NotNull(versions);
        Assert.Empty(versions);
    }

    [Fact]
    public async Task GetAllVersionsAsync_WithCaching_WorksCorrectly()
    {
        // Arrange
        var packageId = "Serilog";

        // Act - first call with cache
        var versionsWithCache = await PackageScanner.GetAllVersionsAsync(
            _repository,
            _throttler,
            _logger,
            packageId,
            noCache: false);

        // Act - second call without cache
        var versionsNoCache = await PackageScanner.GetAllVersionsAsync(
            _repository,
            _throttler,
            _logger,
            packageId,
            noCache: true);

        // Assert - both should return the same versions
        Assert.NotNull(versionsWithCache);
        Assert.NotNull(versionsNoCache);
        Assert.Equal(versionsWithCache.Count(), versionsNoCache.Count());
    }

    [Fact]
    public async Task GetAllVersionsAsync_ThrottlingWorks_NoExceptions()
    {
        // Arrange - make multiple concurrent calls to test throttling
        var packages = new[] { "Newtonsoft.Json", "Serilog", "NUnit", "xunit", "Moq" };
        var tasks = new List<Task<IEnumerable<NuGetVersion>>>();

        // Act - fire off multiple requests concurrently
        foreach (var package in packages)
        {
            tasks.Add(PackageScanner.GetAllVersionsAsync(
                _repository,
                _throttler,
                _logger,
                package,
                noCache: true));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - all should complete successfully
        Assert.Equal(packages.Length, results.Length);
        foreach (var result in results)
        {
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }
    }

    [Fact]
    public async Task CheckPackagesAsync_WithValidPackages_ReturnsUpdates()
    {
        // Arrange - use packages with known older versions
        var packages = new List<PackageReference>
        {
            new PackageReference("Newtonsoft.Json", NuGetVersion.Parse("10.0.0")), // Older version
            new PackageReference("Serilog", NuGetVersion.Parse("1.0.0")) // Older version
        };

        // Act
        var updates = await PackageScanner.CheckPackagesAsync(
            packages,
            _repository,
            _throttler,
            _logger,
            includePrerelease: false,
            noCache: true,
            verbose: false);

        // Assert
        Assert.NotNull(updates);
        Assert.NotEmpty(updates);
        // Both packages should have updates available
        Assert.Equal(2, updates.Count);
        Assert.All(updates, u => Assert.True(u.LatestStableVersion > u.CurrentVersion));
    }

    [Fact]
    public async Task CheckPackagesAsync_WithLatestVersions_ReturnsNoUpdates()
    {
        // Arrange - get the actual latest version first
        var newtonsoftVersions = await PackageScanner.GetAllVersionsAsync(
            _repository,
            _throttler,
            _logger,
            "Newtonsoft.Json",
            noCache: true);

        var latestStable = newtonsoftVersions
            .Where(v => !v.IsPrerelease)
            .OrderByDescending(v => v)
            .First();

        var packages = new List<PackageReference>
        {
            new PackageReference("Newtonsoft.Json", latestStable)
        };

        // Act
        var updates = await PackageScanner.CheckPackagesAsync(
            packages,
            _repository,
            _throttler,
            _logger,
            includePrerelease: false,
            noCache: true,
            verbose: false);

        // Assert - should be empty since we're using the latest version
        Assert.NotNull(updates);
        Assert.Empty(updates);
    }

    [Fact]
    public async Task CheckPackagesAsync_WithPrereleaseFlag_IncludesPrereleases()
    {
        // Arrange - use an older version
        var packages = new List<PackageReference>
        {
            new PackageReference("Newtonsoft.Json", NuGetVersion.Parse("10.0.0"))
        };

        // Act - with prerelease flag
        var updatesWithPrerelease = await PackageScanner.CheckPackagesAsync(
            packages,
            _repository,
            _throttler,
            _logger,
            includePrerelease: true,
            noCache: true,
            verbose: false);

        // Assert
        Assert.NotNull(updatesWithPrerelease);
        // Should have at least one update
        Assert.NotEmpty(updatesWithPrerelease);
    }
}
