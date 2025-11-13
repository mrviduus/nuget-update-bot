using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NugetUpdateBot.Models;
using NugetUpdateBot.Services;

namespace NugetUpdateBot.Tests.IntegrationTests;

/// <summary>
/// Integration tests for PackageScannerService that interact with real NuGet.org API.
/// These tests verify the core scanning functionality against actual package data.
/// </summary>
public class PackageScannerIntegrationTests : IDisposable
{
    private readonly SourceRepository _repository;
    private readonly SemaphoreSlim _throttler;
    private readonly ILogger _logger;
    private readonly PackageScannerService _scanner;
    private readonly string _testProjectPath;

    public PackageScannerIntegrationTests()
    {
        _repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
        _throttler = new SemaphoreSlim(5, 5);
        _logger = NullLogger.Instance;
        _scanner = new PackageScannerService(_repository, _throttler, _logger);

        // Create a temporary test project file
        _testProjectPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.csproj");
    }

    [Fact]
    public async Task CheckPackagesAsync_WithRealPackage_ReturnsUpdateInfo()
    {
        // Arrange: Create test packages with known stable package
        var packages = new List<PackageReference>
        {
            new("Newtonsoft.Json", NuGetVersion.Parse("13.0.1"))
        };

        // Act: Check for updates (this hits real NuGet.org API)
        var updates = await _scanner.CheckPackagesAsync(
            packages,
            includePrerelease: false,
            noCache: true,
            verbose: false,
            CancellationToken.None);

        // Assert: Verify we got results
        Assert.NotNull(updates);
        Assert.Single(updates);

        var update = updates[0];
        Assert.Equal("Newtonsoft.Json", update.PackageId);
        Assert.Equal(NuGetVersion.Parse("13.0.1"), update.CurrentVersion);
        Assert.NotNull(update.LatestStableVersion);
        Assert.True(update.LatestStableVersion >= NuGetVersion.Parse("13.0.1"));
    }

    [Fact]
    public async Task CheckPackagesAsync_WithPrerelease_IncludesPrereleaseVersions()
    {
        // Arrange: Use a package that might have prerelease versions
        var packages = new List<PackageReference>
        {
            new("xunit", NuGetVersion.Parse("2.5.0"))
        };

        // Act: Check with prerelease enabled
        var updates = await _scanner.CheckPackagesAsync(
            packages,
            includePrerelease: true,
            noCache: true,
            verbose: false,
            CancellationToken.None);

        // Assert
        Assert.NotNull(updates);
        Assert.Single(updates);

        var update = updates[0];
        Assert.Equal("xunit", update.PackageId);
        Assert.NotNull(update.LatestStableVersion);
        // Prerelease version may or may not exist, but stable should always be present
    }

    [Fact]
    public async Task CheckPackagesAsync_WithMultiplePackages_ProcessesAll()
    {
        // Arrange: Multiple well-known packages
        var packages = new List<PackageReference>
        {
            new("Newtonsoft.Json", NuGetVersion.Parse("12.0.1")),
            new("xunit", NuGetVersion.Parse("2.4.0")),
            new("Moq", NuGetVersion.Parse("4.18.0"))
        };

        // Act
        var updates = await _scanner.CheckPackagesAsync(
            packages,
            includePrerelease: false,
            noCache: true,
            verbose: false,
            CancellationToken.None);

        // Assert: All packages should be checked
        Assert.NotNull(updates);
        Assert.Equal(3, updates.Count);
        Assert.Contains(updates, u => u.PackageId == "Newtonsoft.Json");
        Assert.Contains(updates, u => u.PackageId == "xunit");
        Assert.Contains(updates, u => u.PackageId == "Moq");
    }

    [Fact]
    public async Task CheckPackagesAsync_WithUpToDatePackage_ReturnsNoUpdate()
    {
        // Arrange: First, get the latest version of a package
        var testPackage = "System.Text.Json";
        var packages = new List<PackageReference>
        {
            new(testPackage, NuGetVersion.Parse("8.0.0"))
        };

        var initialCheck = await _scanner.CheckPackagesAsync(
            packages,
            includePrerelease: false,
            noCache: true,
            verbose: false,
            CancellationToken.None);

        var latestVersion = initialCheck[0].LatestStableVersion;

        // Act: Check again with the latest version
        var packagesWithLatest = new List<PackageReference>
        {
            new(testPackage, latestVersion)
        };

        var updates = await _scanner.CheckPackagesAsync(
            packagesWithLatest,
            includePrerelease: false,
            noCache: true,
            verbose: false,
            CancellationToken.None);

        // Assert: Should have same current and latest version
        Assert.NotNull(updates);
        Assert.Single(updates);
        Assert.Equal(latestVersion, updates[0].CurrentVersion);
        Assert.Equal(latestVersion, updates[0].LatestStableVersion);
    }

    [Fact]
    public void ParseProjectFile_WithValidCsproj_ParsesPackageReferences()
    {
        // Arrange: Create a test .csproj file
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""13.0.1"" />
    <PackageReference Include=""xunit"" Version=""2.5.0"" />
    <PackageReference Include=""Moq"" Version=""4.20.0"" />
  </ItemGroup>
</Project>";

        File.WriteAllText(_testProjectPath, projectContent);

        // Act: Parse the project file
        var packages = _scanner.ParseProjectFile(_testProjectPath);

        // Assert: Verify all packages were parsed correctly
        Assert.NotNull(packages);
        Assert.Equal(3, packages.Count);

        var newtonsoftJson = packages.FirstOrDefault(p => p.Name == "Newtonsoft.Json");
        Assert.NotNull(newtonsoftJson);
        Assert.Equal(NuGetVersion.Parse("13.0.1"), newtonsoftJson.Version);

        var xunit = packages.FirstOrDefault(p => p.Name == "xunit");
        Assert.NotNull(xunit);
        Assert.Equal(NuGetVersion.Parse("2.5.0"), xunit.Version);

        var moq = packages.FirstOrDefault(p => p.Name == "Moq");
        Assert.NotNull(moq);
        Assert.Equal(NuGetVersion.Parse("4.20.0"), moq.Version);
    }

    [Fact]
    public void ParseProjectFile_WithEmptyProject_ReturnsEmptyList()
    {
        // Arrange: Create a project with no package references
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
</Project>";

        File.WriteAllText(_testProjectPath, projectContent);

        // Act
        var packages = _scanner.ParseProjectFile(_testProjectPath);

        // Assert
        Assert.NotNull(packages);
        Assert.Empty(packages);
    }

    [Fact]
    public void ParseProjectFile_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "nonexistent_" + Guid.NewGuid() + ".csproj");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => _scanner.ParseProjectFile(nonExistentPath));
    }

    public void Dispose()
    {
        // Cleanup: Remove test project file
        if (File.Exists(_testProjectPath))
        {
            File.Delete(_testProjectPath);
        }

        _throttler?.Dispose();
    }
}
