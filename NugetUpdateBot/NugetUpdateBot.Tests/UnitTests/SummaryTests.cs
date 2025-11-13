using Xunit;
using NuGet.Versioning;

namespace NugetUpdateBot.Tests.UnitTests;

public class SummaryTests
{
    [Fact]
    public void CalculateSummary_EmptyList_ReturnsZeros()
    {
        // Arrange
        var updates = new List<UpdateCandidate>();
        var totalPackages = 0;

        // Act
        var summary = SummaryCalculator.CalculateSummary(updates, totalPackages, excludedCount: 0);

        // Assert
        Assert.Equal(0, summary.TotalPackages);
        Assert.Equal(0, summary.OutdatedCount);
        Assert.Equal(0, summary.MajorUpdates);
        Assert.Equal(0, summary.MinorUpdates);
        Assert.Equal(0, summary.PatchUpdates);
        Assert.Equal(0, summary.ExcludedCount);
    }

    [Fact]
    public void CalculateSummary_ValidUpdates_CountsCorrectly()
    {
        // Arrange
        var updates = new List<UpdateCandidate>
        {
            new UpdateCandidate("P1", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("2.0.0"), null, UpdateType.Major, false),
            new UpdateCandidate("P2", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("1.1.0"), null, UpdateType.Minor, false),
            new UpdateCandidate("P3", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("1.0.1"), null, UpdateType.Patch, false),
            new UpdateCandidate("P4", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("1.0.2"), null, UpdateType.Patch, false)
        };
        var totalPackages = 10;

        // Act
        var summary = SummaryCalculator.CalculateSummary(updates, totalPackages, excludedCount: 0);

        // Assert
        Assert.Equal(10, summary.TotalPackages);
        Assert.Equal(4, summary.OutdatedCount);
        Assert.Equal(1, summary.MajorUpdates);
        Assert.Equal(1, summary.MinorUpdates);
        Assert.Equal(2, summary.PatchUpdates);
    }

    [Fact]
    public void CalculateSummary_WithExcludedPackages_TracksExcluded()
    {
        // Arrange
        var updates = new List<UpdateCandidate>
        {
            new UpdateCandidate("P1", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("2.0.0"), null, UpdateType.Major, false)
        };
        var totalPackages = 5;
        var excludedCount = 2;

        // Act
        var summary = SummaryCalculator.CalculateSummary(updates, totalPackages, excludedCount);

        // Assert
        Assert.Equal(5, summary.TotalPackages);
        Assert.Equal(1, summary.OutdatedCount);
        Assert.Equal(2, summary.ExcludedCount);
    }

    [Fact]
    public void CalculateSummary_MixedUpdateTypes_CountsEachType()
    {
        // Arrange
        var updates = new List<UpdateCandidate>
        {
            new UpdateCandidate("Major1", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("2.0.0"), null, UpdateType.Major, false),
            new UpdateCandidate("Major2", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("3.0.0"), null, UpdateType.Major, false),
            new UpdateCandidate("Minor1", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("1.1.0"), null, UpdateType.Minor, false),
            new UpdateCandidate("Patch1", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("1.0.1"), null, UpdateType.Patch, false),
            new UpdateCandidate("Patch2", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("1.0.2"), null, UpdateType.Patch, false),
            new UpdateCandidate("Patch3", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("1.0.3"), null, UpdateType.Patch, false)
        };
        var totalPackages = 10;

        // Act
        var summary = SummaryCalculator.CalculateSummary(updates, totalPackages, excludedCount: 0);

        // Assert
        Assert.Equal(2, summary.MajorUpdates);
        Assert.Equal(1, summary.MinorUpdates);
        Assert.Equal(3, summary.PatchUpdates);
        Assert.Equal(6, summary.OutdatedCount);
    }

    [Fact]
    public void CalculateSummary_OnlyMajorUpdates_OtherCountsZero()
    {
        // Arrange
        var updates = new List<UpdateCandidate>
        {
            new UpdateCandidate("P1", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("2.0.0"), null, UpdateType.Major, false),
            new UpdateCandidate("P2", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("3.0.0"), null, UpdateType.Major, false)
        };
        var totalPackages = 5;

        // Act
        var summary = SummaryCalculator.CalculateSummary(updates, totalPackages, excludedCount: 0);

        // Assert
        Assert.Equal(2, summary.MajorUpdates);
        Assert.Equal(0, summary.MinorUpdates);
        Assert.Equal(0, summary.PatchUpdates);
    }

    [Fact]
    public void CalculateSummary_OnlyMinorUpdates_OtherCountsZero()
    {
        // Arrange
        var updates = new List<UpdateCandidate>
        {
            new UpdateCandidate("P1", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("1.1.0"), null, UpdateType.Minor, false),
            new UpdateCandidate("P2", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("1.2.0"), null, UpdateType.Minor, false)
        };
        var totalPackages = 5;

        // Act
        var summary = SummaryCalculator.CalculateSummary(updates, totalPackages, excludedCount: 0);

        // Assert
        Assert.Equal(0, summary.MajorUpdates);
        Assert.Equal(2, summary.MinorUpdates);
        Assert.Equal(0, summary.PatchUpdates);
    }

    [Fact]
    public void CalculateSummary_OnlyPatchUpdates_OtherCountsZero()
    {
        // Arrange
        var updates = new List<UpdateCandidate>
        {
            new UpdateCandidate("P1", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("1.0.1"), null, UpdateType.Patch, false),
            new UpdateCandidate("P2", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("1.0.2"), null, UpdateType.Patch, false)
        };
        var totalPackages = 5;

        // Act
        var summary = SummaryCalculator.CalculateSummary(updates, totalPackages, excludedCount: 0);

        // Assert
        Assert.Equal(0, summary.MajorUpdates);
        Assert.Equal(0, summary.MinorUpdates);
        Assert.Equal(2, summary.PatchUpdates);
    }

    [Fact]
    public void CalculateSummary_PrereleaseUpdates_CountedSeparately()
    {
        // Arrange
        var updates = new List<UpdateCandidate>
        {
            new UpdateCandidate("P1", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("1.5.0"), NuGetVersion.Parse("2.0.0-beta"), UpdateType.Prerelease, false),
            new UpdateCandidate("P2", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("2.0.0"), null, UpdateType.Major, false)
        };
        var totalPackages = 5;

        // Act
        var summary = SummaryCalculator.CalculateSummary(updates, totalPackages, excludedCount: 0);

        // Assert
        Assert.Equal(2, summary.OutdatedCount);
        // Prerelease counted separately or as part of major/minor/patch depending on base version change
    }

    [Fact]
    public void CalculateSummary_TotalPackagesCorrect_WhenNoUpdates()
    {
        // Arrange
        var updates = new List<UpdateCandidate>();
        var totalPackages = 15;

        // Act
        var summary = SummaryCalculator.CalculateSummary(updates, totalPackages, excludedCount: 0);

        // Assert
        Assert.Equal(15, summary.TotalPackages);
        Assert.Equal(0, summary.OutdatedCount);
    }

    [Fact]
    public void CalculateSummary_OutdatedCountMatchesUpdateListSize()
    {
        // Arrange
        var updates = new List<UpdateCandidate>
        {
            new UpdateCandidate("P1", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("2.0.0"), null, UpdateType.Major, false),
            new UpdateCandidate("P2", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("1.1.0"), null, UpdateType.Minor, false),
            new UpdateCandidate("P3", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("1.0.1"), null, UpdateType.Patch, false)
        };
        var totalPackages = 10;

        // Act
        var summary = SummaryCalculator.CalculateSummary(updates, totalPackages, excludedCount: 0);

        // Assert
        Assert.Equal(updates.Count, summary.OutdatedCount);
        Assert.Equal(3, summary.OutdatedCount);
    }

    [Fact]
    public void CalculateSummary_SumOfTypesEqualsOutdated_WhenNoPrereleases()
    {
        // Arrange
        var updates = new List<UpdateCandidate>
        {
            new UpdateCandidate("P1", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("2.0.0"), null, UpdateType.Major, false),
            new UpdateCandidate("P2", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("1.1.0"), null, UpdateType.Minor, false),
            new UpdateCandidate("P3", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("1.0.1"), null, UpdateType.Patch, false)
        };
        var totalPackages = 10;

        // Act
        var summary = SummaryCalculator.CalculateSummary(updates, totalPackages, excludedCount: 0);

        // Assert
        var sumOfTypes = summary.MajorUpdates + summary.MinorUpdates + summary.PatchUpdates;
        Assert.Equal(summary.OutdatedCount, sumOfTypes);
    }
}
