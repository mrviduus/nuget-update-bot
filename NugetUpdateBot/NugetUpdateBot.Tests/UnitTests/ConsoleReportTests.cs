using Xunit;
using NuGet.Versioning;

namespace NugetUpdateBot.Tests.UnitTests;

[Collection("ConsoleTests")]
public class ConsoleReportTests
{
    [Fact]
    public void FormatConsoleReport_ValidUpdates_CreatesTable()
    {
        // Arrange
        var updates = new List<UpdateCandidate>
        {
            new UpdateCandidate("Newtonsoft.Json", NuGetVersion.Parse("12.0.3"), NuGetVersion.Parse("13.0.3"), null, UpdateType.Major, false),
            new UpdateCandidate("Serilog", NuGetVersion.Parse("2.10.0"), NuGetVersion.Parse("3.1.1"), null, UpdateType.Major, false)
        };
        var output = new StringWriter();
        Console.SetOut(output);

        try
        {
            // Act
            ConsoleReportFormatter.FormatConsoleReport(updates, "/test/project.csproj", UpdateReportType.Preview);

            // Assert
            var result = output.ToString();
            Assert.Contains("Newtonsoft.Json", result);
            Assert.Contains("Serilog", result);
            Assert.Contains("12.0.3", result);
            Assert.Contains("13.0.3", result);
            Assert.Contains("2.10.0", result);
            Assert.Contains("3.1.1", result);
        }
        finally
        {
            Console.SetOut(Console.Out);
        }
    }

    [Fact]
    public void FormatConsoleReport_EmptyUpdates_DisplaysNoUpdatesMessage()
    {
        // Arrange
        var updates = new List<UpdateCandidate>();
        var output = new StringWriter();
        Console.SetOut(output);

        try
        {
            // Act
            ConsoleReportFormatter.FormatConsoleReport(updates, "/test/project.csproj", UpdateReportType.Preview);

            // Assert
            var result = output.ToString();
            Assert.Contains("No outdated packages found", result);
        }
        finally
        {
            Console.SetOut(Console.Out);
        }
    }

    [Fact]
    public void FormatConsoleReport_IncludesSummary()
    {
        // Arrange
        var updates = new List<UpdateCandidate>
        {
            new UpdateCandidate("P1", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("2.0.0"), null, UpdateType.Major, false),
            new UpdateCandidate("P2", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("1.1.0"), null, UpdateType.Minor, false),
            new UpdateCandidate("P3", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("1.0.1"), null, UpdateType.Patch, false)
        };
        var output = new StringWriter();
        Console.SetOut(output);

        try
        {
            // Act
            ConsoleReportFormatter.FormatConsoleReport(updates, "/test/project.csproj", UpdateReportType.Preview);

            // Assert
            var result = output.ToString();
            Assert.Contains("Summary", result);
            Assert.Contains("Major", result);
            Assert.Contains("Minor", result);
            Assert.Contains("Patch", result);
        }
        finally
        {
            Console.SetOut(Console.Out);
        }
    }

    [Fact]
    public void FormatConsoleReport_ShowsUpdateType_ForEachPackage()
    {
        // Arrange
        var updates = new List<UpdateCandidate>
        {
            new UpdateCandidate("Package1", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("2.0.0"), null, UpdateType.Major, false),
            new UpdateCandidate("Package2", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("1.1.0"), null, UpdateType.Minor, false),
            new UpdateCandidate("Package3", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("1.0.1"), null, UpdateType.Patch, false)
        };
        var output = new StringWriter();
        Console.SetOut(output);

        try
        {
            // Act
            ConsoleReportFormatter.FormatConsoleReport(updates, "/test/project.csproj", UpdateReportType.Preview);

            // Assert
            var result = output.ToString();
            var lines = result.Split('\n');
            Assert.Contains(lines, l => l.Contains("Package1") && l.Contains("Major"));
            Assert.Contains(lines, l => l.Contains("Package2") && l.Contains("Minor"));
            Assert.Contains(lines, l => l.Contains("Package3") && l.Contains("Patch"));
        }
        finally
        {
            Console.SetOut(Console.Out);
        }
    }

    [Fact]
    public void FormatConsoleReport_HighlightsDeprecatedPackages()
    {
        // Arrange
        var updates = new List<UpdateCandidate>
        {
            new UpdateCandidate("NormalPackage", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("2.0.0"), null, UpdateType.Major, false),
            new UpdateCandidate("DeprecatedPackage", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("2.0.0"), null, UpdateType.Major, true)
        };
        var output = new StringWriter();
        Console.SetOut(output);

        try
        {
            // Act
            ConsoleReportFormatter.FormatConsoleReport(updates, "/test/project.csproj", UpdateReportType.Preview);

            // Assert
            var result = output.ToString();
            Assert.Contains("DEPRECATED", result);
            Assert.Contains("DeprecatedPackage", result);
        }
        finally
        {
            Console.SetOut(Console.Out);
        }
    }

    [Fact]
    public void FormatConsoleReport_ShowsReportType()
    {
        // Arrange
        var updates = new List<UpdateCandidate>
        {
            new UpdateCandidate("TestPackage", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("2.0.0"), null, UpdateType.Major, false)
        };
        var output = new StringWriter();
        Console.SetOut(output);

        try
        {
            // Act - Preview
            ConsoleReportFormatter.FormatConsoleReport(updates, "/test/project.csproj", UpdateReportType.Preview);
            var previewResult = output.ToString();

            // Reset
            output = new StringWriter();
            Console.SetOut(output);

            // Act - Applied
            ConsoleReportFormatter.FormatConsoleReport(updates, "/test/project.csproj", UpdateReportType.Applied);
            var appliedResult = output.ToString();

            // Assert
            Assert.Contains("Preview", previewResult);
            Assert.Contains("Applied", appliedResult);
        }
        finally
        {
            Console.SetOut(Console.Out);
        }
    }

    [Fact]
    public void FormatConsoleReport_IncludesProjectPath()
    {
        // Arrange
        var updates = new List<UpdateCandidate>
        {
            new UpdateCandidate("TestPackage", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("2.0.0"), null, UpdateType.Major, false)
        };
        var projectPath = "/path/to/MyProject.csproj";
        var output = new StringWriter();
        Console.SetOut(output);

        try
        {
            // Act
            ConsoleReportFormatter.FormatConsoleReport(updates, projectPath, UpdateReportType.Preview);

            // Assert
            var result = output.ToString();
            Assert.Contains("MyProject.csproj", result);
        }
        finally
        {
            Console.SetOut(Console.Out);
        }
    }

    [Fact]
    public void FormatConsoleReport_ShowsPrereleaseVersion_WhenAvailable()
    {
        // Arrange
        var updates = new List<UpdateCandidate>
        {
            new UpdateCandidate(
                "TestPackage",
                NuGetVersion.Parse("1.0.0"),
                NuGetVersion.Parse("1.5.0"),
                NuGetVersion.Parse("2.0.0-beta1"),
                UpdateType.Prerelease,
                false)
        };
        var output = new StringWriter();
        Console.SetOut(output);

        try
        {
            // Act
            ConsoleReportFormatter.FormatConsoleReport(updates, "/test/project.csproj", UpdateReportType.Preview);

            // Assert
            var result = output.ToString();
            Assert.Contains("2.0.0-beta1", result);
        }
        finally
        {
            Console.SetOut(Console.Out);
        }
    }

    [Fact]
    public void FormatConsoleReport_AlignmentIsConsistent()
    {
        // Arrange - packages with different name lengths
        var updates = new List<UpdateCandidate>
        {
            new UpdateCandidate("A", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("2.0.0"), null, UpdateType.Major, false),
            new UpdateCandidate("VeryLongPackageNameForTesting", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("2.0.0"), null, UpdateType.Major, false)
        };
        var output = new StringWriter();
        Console.SetOut(output);

        try
        {
            // Act
            ConsoleReportFormatter.FormatConsoleReport(updates, "/test/project.csproj", UpdateReportType.Preview);

            // Assert
            var result = output.ToString();
            var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            // Should have separator lines of consistent length
            var separatorLines = lines.Where(l => l.StartsWith("---") || l.StartsWith("===")).ToList();
            if (separatorLines.Count > 1)
            {
                var firstLength = separatorLines[0].Length;
                Assert.All(separatorLines, line => Assert.Equal(firstLength, line.Length));
            }
        }
        finally
        {
            Console.SetOut(Console.Out);
        }
    }
}
