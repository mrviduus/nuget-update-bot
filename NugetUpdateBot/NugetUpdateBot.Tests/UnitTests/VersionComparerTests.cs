using Xunit;
using NuGet.Versioning;

namespace NugetUpdateBot.Tests.UnitTests;

public class VersionComparerTests
{
    [Theory]
    [InlineData("1.0.0", "2.0.0", UpdateType.Major)]
    [InlineData("1.5.0", "2.0.0", UpdateType.Major)]
    [InlineData("1.9.9", "2.0.0", UpdateType.Major)]
    public void DetermineUpdateType_MajorVersionChange_ReturnsMajor(string currentVersion, string latestVersion, UpdateType expected)
    {
        // Arrange
        var current = NuGetVersion.Parse(currentVersion);
        var latest = NuGetVersion.Parse(latestVersion);

        // Act
        var result = PackageScanner.DetermineUpdateType(current, latest);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("1.0.0", "1.1.0", UpdateType.Minor)]
    [InlineData("1.5.0", "1.6.0", UpdateType.Minor)]
    [InlineData("2.0.0", "2.5.0", UpdateType.Minor)]
    public void DetermineUpdateType_MinorVersionChange_ReturnsMinor(string currentVersion, string latestVersion, UpdateType expected)
    {
        // Arrange
        var current = NuGetVersion.Parse(currentVersion);
        var latest = NuGetVersion.Parse(latestVersion);

        // Act
        var result = PackageScanner.DetermineUpdateType(current, latest);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("1.0.0", "1.0.1", UpdateType.Patch)]
    [InlineData("1.5.3", "1.5.4", UpdateType.Patch)]
    [InlineData("2.0.0", "2.0.1", UpdateType.Patch)]
    public void DetermineUpdateType_PatchVersionChange_ReturnsPatch(string currentVersion, string latestVersion, UpdateType expected)
    {
        // Arrange
        var current = NuGetVersion.Parse(currentVersion);
        var latest = NuGetVersion.Parse(latestVersion);

        // Act
        var result = PackageScanner.DetermineUpdateType(current, latest);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("1.0.0", "2.0.0-beta1", UpdateType.Prerelease)]
    [InlineData("1.5.0", "1.6.0-alpha", UpdateType.Prerelease)]
    [InlineData("2.0.0", "2.0.1-rc1", UpdateType.Prerelease)]
    public void DetermineUpdateType_StableToPrerelease_ReturnsPrerelease(string currentVersion, string latestVersion, UpdateType expected)
    {
        // Arrange
        var current = NuGetVersion.Parse(currentVersion);
        var latest = NuGetVersion.Parse(latestVersion);

        // Act
        var result = PackageScanner.DetermineUpdateType(current, latest);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("1.0.0-beta1", "1.0.0-beta2", UpdateType.Patch)]
    [InlineData("1.0.0-alpha", "1.1.0-beta", UpdateType.Minor)]
    [InlineData("1.0.0-rc1", "2.0.0-beta1", UpdateType.Major)]
    public void DetermineUpdateType_PrereleaseToPrerelease_ReturnsCorrectType(string currentVersion, string latestVersion, UpdateType expected)
    {
        // Arrange
        var current = NuGetVersion.Parse(currentVersion);
        var latest = NuGetVersion.Parse(latestVersion);

        // Act
        var result = PackageScanner.DetermineUpdateType(current, latest);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void DetermineUpdateType_MajorTakesPrecedenceOverMinor()
    {
        // Arrange - major change (2.x -> 3.x) should be detected as Major, not Minor
        var current = NuGetVersion.Parse("2.5.3");
        var latest = NuGetVersion.Parse("3.1.0");

        // Act
        var result = PackageScanner.DetermineUpdateType(current, latest);

        // Assert
        Assert.Equal(UpdateType.Major, result);
    }

    [Fact]
    public void DetermineUpdateType_MinorTakesPrecedenceOverPatch()
    {
        // Arrange - minor change (1.5.x -> 1.6.x) should be detected as Minor, not Patch
        var current = NuGetVersion.Parse("1.5.3");
        var latest = NuGetVersion.Parse("1.6.1");

        // Act
        var result = PackageScanner.DetermineUpdateType(current, latest);

        // Assert
        Assert.Equal(UpdateType.Minor, result);
    }

    [Theory]
    [InlineData("1.0.0.0", "2.0.0.0", UpdateType.Major)]
    [InlineData("1.2.3.4", "1.3.0.0", UpdateType.Minor)]
    [InlineData("1.2.3.4", "1.2.3.5", UpdateType.Patch)]
    public void DetermineUpdateType_FourPartVersion_HandlesCorrectly(string currentVersion, string latestVersion, UpdateType expected)
    {
        // Arrange
        var current = NuGetVersion.Parse(currentVersion);
        var latest = NuGetVersion.Parse(latestVersion);

        // Act
        var result = PackageScanner.DetermineUpdateType(current, latest);

        // Assert
        Assert.Equal(expected, result);
    }
}
