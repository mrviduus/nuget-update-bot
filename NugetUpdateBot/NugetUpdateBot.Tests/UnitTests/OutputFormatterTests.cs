using Xunit;
using NuGet.Versioning;
using System.IO;

namespace NugetUpdateBot.Tests.UnitTests;

public class OutputFormatterTests
{
    [Fact]
    public void DisplayScanResults_NoUpdates_DisplaysUpToDateMessage()
    {
        // Arrange
        var updates = new List<UpdateCandidate>();
        var output = new StringWriter();
        Console.SetOut(output);

        // Act
        PackageScanner.DisplayScanResults(updates);

        // Assert
        var result = output.ToString();
        Assert.Contains("All packages are up to date!", result);

        // Cleanup
        Console.SetOut(Console.Out);
    }

    [Fact]
    public void DisplayScanResults_WithUpdates_DisplaysFormattedTable()
    {
        // Arrange
        var updates = new List<UpdateCandidate>
        {
            new UpdateCandidate(
                "Newtonsoft.Json",
                NuGetVersion.Parse("12.0.3"),
                NuGetVersion.Parse("13.0.3"),
                null,
                UpdateType.Major,
                false
            ),
            new UpdateCandidate(
                "Serilog",
                NuGetVersion.Parse("2.10.0"),
                NuGetVersion.Parse("3.1.1"),
                null,
                UpdateType.Major,
                false
            )
        };
        var output = new StringWriter();
        Console.SetOut(output);

        // Act
        PackageScanner.DisplayScanResults(updates);

        // Assert
        var result = output.ToString();
        Assert.Contains("Found 2 outdated packages:", result);
        Assert.Contains("Newtonsoft.Json", result);
        Assert.Contains("Serilog", result);
        Assert.Contains("12.0.3", result);
        Assert.Contains("13.0.3", result);
        Assert.Contains("2.10.0", result);
        Assert.Contains("3.1.1", result);
        Assert.Contains("→", result); // Arrow separator
        Assert.Contains(new string('-', 80), result); // Separator line

        // Cleanup
        Console.SetOut(Console.Out);
    }

    [Fact]
    public void DisplayScanResults_SingleUpdate_DisplaysSingularCount()
    {
        // Arrange
        var updates = new List<UpdateCandidate>
        {
            new UpdateCandidate(
                "TestPackage",
                NuGetVersion.Parse("1.0.0"),
                NuGetVersion.Parse("2.0.0"),
                null,
                UpdateType.Major,
                false
            )
        };
        var output = new StringWriter();
        Console.SetOut(output);

        // Act
        PackageScanner.DisplayScanResults(updates);

        // Assert
        var result = output.ToString();
        Assert.Contains("Found 1 outdated package", result);
        Assert.Contains("TestPackage", result);

        // Cleanup
        Console.SetOut(Console.Out);
    }

    [Fact]
    public void DisplayScanResults_WithPrereleaseVersion_DisplaysPrereleaseInsteadOfStable()
    {
        // Arrange
        var updates = new List<UpdateCandidate>
        {
            new UpdateCandidate(
                "TestPackage",
                NuGetVersion.Parse("1.0.0"),
                NuGetVersion.Parse("1.5.0"),
                NuGetVersion.Parse("2.0.0-beta1"), // Prerelease is newer
                UpdateType.Prerelease,
                false
            )
        };
        var output = new StringWriter();
        Console.SetOut(output);

        // Act
        PackageScanner.DisplayScanResults(updates);

        // Assert
        var result = output.ToString();
        Assert.Contains("2.0.0-beta1", result); // Should show prerelease version
        Assert.Contains("TestPackage", result);

        // Cleanup
        Console.SetOut(Console.Out);
    }

    [Fact]
    public void DisplayScanResults_WithOnlyStableVersion_DisplaysStableVersion()
    {
        // Arrange
        var updates = new List<UpdateCandidate>
        {
            new UpdateCandidate(
                "TestPackage",
                NuGetVersion.Parse("1.0.0"),
                NuGetVersion.Parse("2.0.0"),
                null, // No prerelease
                UpdateType.Major,
                false
            )
        };
        var output = new StringWriter();
        Console.SetOut(output);

        // Act
        PackageScanner.DisplayScanResults(updates);

        // Assert
        var result = output.ToString();
        Assert.Contains("2.0.0", result); // Should show stable version
        Assert.DoesNotContain("beta", result);
        Assert.DoesNotContain("alpha", result);

        // Cleanup
        Console.SetOut(Console.Out);
    }

    [Fact]
    public void DisplayScanResults_MultipleUpdates_DisplaysCorrectAlignment()
    {
        // Arrange - packages with different name lengths
        var updates = new List<UpdateCandidate>
        {
            new UpdateCandidate(
                "A", // Short name
                NuGetVersion.Parse("1.0.0"),
                NuGetVersion.Parse("2.0.0"),
                null,
                UpdateType.Major,
                false
            ),
            new UpdateCandidate(
                "VeryLongPackageNameForTesting", // Long name
                NuGetVersion.Parse("1.0.0"),
                NuGetVersion.Parse("2.0.0"),
                null,
                UpdateType.Major,
                false
            )
        };
        var output = new StringWriter();
        Console.SetOut(output);

        // Act
        PackageScanner.DisplayScanResults(updates);

        // Assert
        var result = output.ToString();
        var lines = result.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        // Should have header lines + separator + package lines
        Assert.True(lines.Length >= 5);

        // All separator lines should be 80 characters
        var separatorLines = lines.Where(l => l.StartsWith("----")).ToList();
        Assert.All(separatorLines, line => Assert.Equal(80, line.Length));

        // Cleanup
        Console.SetOut(Console.Out);
    }

    [Fact]
    public void DisplayScanResults_EmptyList_DoesNotDisplayTable()
    {
        // Arrange
        var updates = new List<UpdateCandidate>();
        var output = new StringWriter();
        Console.SetOut(output);

        // Act
        PackageScanner.DisplayScanResults(updates);

        // Assert
        var result = output.ToString();
        Assert.DoesNotContain("Package", result); // Should not display table header
        Assert.DoesNotContain("Current", result);
        Assert.DoesNotContain("Latest", result);
        Assert.DoesNotContain(new string('-', 80), result);

        // Cleanup
        Console.SetOut(Console.Out);
    }
}
