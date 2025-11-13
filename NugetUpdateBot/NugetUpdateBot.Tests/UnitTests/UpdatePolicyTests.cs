using Xunit;
using NuGet.Versioning;

namespace NugetUpdateBot.Tests.UnitTests;

public class UpdatePolicyTests
{
    [Theory]
    [InlineData("1.0.0", "1.0.1", UpdatePolicy.Patch, true)]
    [InlineData("1.0.0", "1.1.0", UpdatePolicy.Patch, false)]
    [InlineData("1.0.0", "2.0.0", UpdatePolicy.Patch, false)]
    public void ShouldUpdate_PatchPolicy_OnlyAllowsPatchUpdates(string currentVersion, string latestVersion, UpdatePolicy policy, bool shouldUpdate)
    {
        // Arrange
        var current = NuGetVersion.Parse(currentVersion);
        var latest = NuGetVersion.Parse(latestVersion);

        // Act
        var result = PolicyEngine.ShouldUpdate(current, latest, policy);

        // Assert
        Assert.Equal(shouldUpdate, result);
    }

    [Theory]
    [InlineData("1.0.0", "1.0.1", UpdatePolicy.Minor, true)]
    [InlineData("1.0.0", "1.1.0", UpdatePolicy.Minor, true)]
    [InlineData("1.0.0", "2.0.0", UpdatePolicy.Minor, false)]
    public void ShouldUpdate_MinorPolicy_AllowsPatchAndMinor(string currentVersion, string latestVersion, UpdatePolicy policy, bool shouldUpdate)
    {
        // Arrange
        var current = NuGetVersion.Parse(currentVersion);
        var latest = NuGetVersion.Parse(latestVersion);

        // Act
        var result = PolicyEngine.ShouldUpdate(current, latest, policy);

        // Assert
        Assert.Equal(shouldUpdate, result);
    }

    [Theory]
    [InlineData("1.0.0", "1.0.1", UpdatePolicy.Major, true)]
    [InlineData("1.0.0", "1.1.0", UpdatePolicy.Major, true)]
    [InlineData("1.0.0", "2.0.0", UpdatePolicy.Major, true)]
    [InlineData("1.0.0", "10.5.3", UpdatePolicy.Major, true)]
    public void ShouldUpdate_MajorPolicy_AllowsAllUpdates(string currentVersion, string latestVersion, UpdatePolicy policy, bool shouldUpdate)
    {
        // Arrange
        var current = NuGetVersion.Parse(currentVersion);
        var latest = NuGetVersion.Parse(latestVersion);

        // Act
        var result = PolicyEngine.ShouldUpdate(current, latest, policy);

        // Assert
        Assert.Equal(shouldUpdate, result);
    }

    [Fact]
    public void FilterUpdatesByPolicy_PatchPolicy_FiltersCorrectly()
    {
        // Arrange
        var updates = new List<UpdateCandidate>
        {
            new UpdateCandidate("Package1", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("1.0.1"), null, UpdateType.Patch, false),
            new UpdateCandidate("Package2", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("1.1.0"), null, UpdateType.Minor, false),
            new UpdateCandidate("Package3", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("2.0.0"), null, UpdateType.Major, false)
        };

        // Act
        var filtered = PolicyEngine.FilterUpdatesByPolicy(updates, UpdatePolicy.Patch);

        // Assert
        Assert.Single(filtered);
        Assert.Equal("Package1", filtered[0].PackageId);
    }

    [Fact]
    public void FilterUpdatesByPolicy_MinorPolicy_FiltersCorrectly()
    {
        // Arrange
        var updates = new List<UpdateCandidate>
        {
            new UpdateCandidate("Package1", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("1.0.1"), null, UpdateType.Patch, false),
            new UpdateCandidate("Package2", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("1.1.0"), null, UpdateType.Minor, false),
            new UpdateCandidate("Package3", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("2.0.0"), null, UpdateType.Major, false)
        };

        // Act
        var filtered = PolicyEngine.FilterUpdatesByPolicy(updates, UpdatePolicy.Minor);

        // Assert
        Assert.Equal(2, filtered.Count);
        Assert.Contains(filtered, u => u.PackageId == "Package1");
        Assert.Contains(filtered, u => u.PackageId == "Package2");
        Assert.DoesNotContain(filtered, u => u.PackageId == "Package3");
    }

    [Fact]
    public void FilterUpdatesByPolicy_MajorPolicy_IncludesAll()
    {
        // Arrange
        var updates = new List<UpdateCandidate>
        {
            new UpdateCandidate("Package1", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("1.0.1"), null, UpdateType.Patch, false),
            new UpdateCandidate("Package2", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("1.1.0"), null, UpdateType.Minor, false),
            new UpdateCandidate("Package3", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("2.0.0"), null, UpdateType.Major, false)
        };

        // Act
        var filtered = PolicyEngine.FilterUpdatesByPolicy(updates, UpdatePolicy.Major);

        // Assert
        Assert.Equal(3, filtered.Count);
    }

    [Fact]
    public void FilterByExclusions_ExcludesMatchingPackages()
    {
        // Arrange
        var updates = new List<UpdateCandidate>
        {
            new UpdateCandidate("Microsoft.Extensions.Logging", NuGetVersion.Parse("7.0.0"), NuGetVersion.Parse("8.0.0"), null, UpdateType.Major, false),
            new UpdateCandidate("Newtonsoft.Json", NuGetVersion.Parse("12.0.3"), NuGetVersion.Parse("13.0.3"), null, UpdateType.Major, false),
            new UpdateCandidate("Serilog", NuGetVersion.Parse("2.10.0"), NuGetVersion.Parse("3.1.1"), null, UpdateType.Major, false)
        };
        var exclusions = new[] { "Microsoft.Extensions.Logging", "Serilog" };

        // Act
        var filtered = PolicyEngine.FilterByExclusions(updates, exclusions);

        // Assert
        Assert.Single(filtered);
        Assert.Equal("Newtonsoft.Json", filtered[0].PackageId);
    }

    [Fact]
    public void FilterByExclusions_EmptyExclusions_ReturnsAll()
    {
        // Arrange
        var updates = new List<UpdateCandidate>
        {
            new UpdateCandidate("Package1", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("2.0.0"), null, UpdateType.Major, false),
            new UpdateCandidate("Package2", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("2.0.0"), null, UpdateType.Major, false)
        };
        var exclusions = Array.Empty<string>();

        // Act
        var filtered = PolicyEngine.FilterByExclusions(updates, exclusions);

        // Assert
        Assert.Equal(2, filtered.Count);
    }

    [Fact]
    public void FilterByExclusions_CaseInsensitive_ExcludesCorrectly()
    {
        // Arrange
        var updates = new List<UpdateCandidate>
        {
            new UpdateCandidate("Newtonsoft.Json", NuGetVersion.Parse("12.0.3"), NuGetVersion.Parse("13.0.3"), null, UpdateType.Major, false),
            new UpdateCandidate("Serilog", NuGetVersion.Parse("2.10.0"), NuGetVersion.Parse("3.1.1"), null, UpdateType.Major, false)
        };
        var exclusions = new[] { "newtonsoft.json" }; // Lowercase

        // Act
        var filtered = PolicyEngine.FilterByExclusions(updates, exclusions);

        // Assert
        Assert.Single(filtered);
        Assert.Equal("Serilog", filtered[0].PackageId);
    }

    [Theory]
    [InlineData("Newtonsoft.Json", "Newtonsoft.Json", true)]
    [InlineData("Newtonsoft.Json", "newtonsoft.json", true)]
    [InlineData("Newtonsoft.Json", "Serilog", false)]
    public void IsPackageExcluded_ChecksCorrectly(string packageId, string exclusionPattern, bool shouldBeExcluded)
    {
        // Arrange
        var exclusions = new[] { exclusionPattern };

        // Act
        var result = PolicyEngine.IsPackageExcluded(packageId, exclusions);

        // Assert
        Assert.Equal(shouldBeExcluded, result);
    }

    [Fact]
    public void ApplyPolicies_CombinesFiltering_Correctly()
    {
        // Arrange
        var updates = new List<UpdateCandidate>
        {
            new UpdateCandidate("Package1", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("1.0.1"), null, UpdateType.Patch, false),
            new UpdateCandidate("Package2", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("1.1.0"), null, UpdateType.Minor, false),
            new UpdateCandidate("Package3", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("2.0.0"), null, UpdateType.Major, false),
            new UpdateCandidate("ExcludedPackage", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("1.0.1"), null, UpdateType.Patch, false)
        };
        var exclusions = new[] { "ExcludedPackage" };
        var policy = UpdatePolicy.Minor;

        // Act - should filter by both policy (Patch + Minor) AND exclusions
        var filtered = PolicyEngine.ApplyPolicies(updates, policy, exclusions);

        // Assert
        Assert.Equal(2, filtered.Count);
        Assert.Contains(filtered, u => u.PackageId == "Package1");
        Assert.Contains(filtered, u => u.PackageId == "Package2");
        Assert.DoesNotContain(filtered, u => u.PackageId == "Package3"); // Excluded by policy
        Assert.DoesNotContain(filtered, u => u.PackageId == "ExcludedPackage"); // Excluded by exclusion list
    }
}
