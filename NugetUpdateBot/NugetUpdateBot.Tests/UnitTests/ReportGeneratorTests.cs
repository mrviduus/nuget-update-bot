using Xunit;
using NuGet.Versioning;
using System.Text.Json;

namespace NugetUpdateBot.Tests.UnitTests;

public class ReportGeneratorTests
{
    [Fact]
    public void GenerateJsonReport_ValidUpdates_CreatesCorrectStructure()
    {
        // Arrange
        var updates = new List<UpdateCandidate>
        {
            new UpdateCandidate("Newtonsoft.Json", NuGetVersion.Parse("12.0.3"), NuGetVersion.Parse("13.0.3"), null, UpdateType.Major, false),
            new UpdateCandidate("Serilog", NuGetVersion.Parse("2.10.0"), NuGetVersion.Parse("3.1.1"), null, UpdateType.Major, false)
        };
        var projectPath = "/test/project.csproj";

        // Act
        var json = ReportGenerator.GenerateJsonReport(projectPath, updates, UpdateReportType.Preview);

        // Assert
        Assert.NotNull(json);
        Assert.Contains("\"ProjectPath\"", json);
        Assert.Contains("\"Updates\"", json);
        Assert.Contains("\"Summary\"", json);
        Assert.Contains("Newtonsoft.Json", json);
        Assert.Contains("Serilog", json);
    }

    [Fact]
    public void GenerateJsonReport_EmptyUpdates_CreatesValidJson()
    {
        // Arrange
        var updates = new List<UpdateCandidate>();
        var projectPath = "/test/project.csproj";

        // Act
        var json = ReportGenerator.GenerateJsonReport(projectPath, updates, UpdateReportType.Preview);

        // Assert
        Assert.NotNull(json);
        Assert.Contains("\"Updates\"", json);
        Assert.Contains("\"Summary\"", json);
        Assert.Contains("\"OutdatedCount\": 0", json);
    }

    [Fact]
    public void GenerateJsonReport_ValidJson_ContainsExpectedFields()
    {
        // Arrange
        var updates = new List<UpdateCandidate>
        {
            new UpdateCandidate("TestPackage", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("2.0.0"), null, UpdateType.Major, false)
        };
        var projectPath = "/test/project.csproj";

        // Act
        var json = ReportGenerator.GenerateJsonReport(projectPath, updates, UpdateReportType.Preview);

        // Assert
        Assert.NotNull(json);
        Assert.Contains("\"/test/project.csproj\"", json);
        Assert.Contains("\"TestPackage\"", json);
        Assert.Contains("\"Type\": 0", json); // Preview is enum value 0
    }

    [Fact]
    public void SerializeUpdateReport_PreservesAllFields()
    {
        // Arrange
        var summary = new UpdateSummary(
            TotalPackages: 10,
            OutdatedCount: 3,
            MajorUpdates: 1,
            MinorUpdates: 1,
            PatchUpdates: 1,
            ExcludedCount: 0
        );

        var updates = new List<UpdateCandidate>
        {
            new UpdateCandidate("Package1", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("2.0.0"), null, UpdateType.Major, false),
            new UpdateCandidate("Package2", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("1.1.0"), null, UpdateType.Minor, false),
            new UpdateCandidate("Package3", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("1.0.1"), null, UpdateType.Patch, false)
        };

        var report = new UpdateReport(
            GeneratedAt: DateTime.UtcNow,
            ProjectPath: "/test/project.csproj",
            Updates: updates,
            Type: UpdateReportType.Applied,
            Summary: summary
        );

        // Act
        var json = JsonSerializer.Serialize(report);

        // Assert - verify JSON contains all expected fields
        Assert.NotNull(json);
        Assert.Contains("\"/test/project.csproj\"", json);
        Assert.Contains("\"TotalPackages\":10", json);
        Assert.Contains("\"MajorUpdates\":1", json);
        Assert.Contains("\"MinorUpdates\":1", json);
        Assert.Contains("\"PatchUpdates\":1", json);
        Assert.Contains("\"Type\":1", json); // Applied is enum value 1
    }

    [Fact]
    public void GenerateJsonReport_WithPrereleaseVersions_IncludesPrerelease()
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
        var projectPath = "/test/project.csproj";

        // Act
        var json = ReportGenerator.GenerateJsonReport(projectPath, updates, UpdateReportType.Preview);

        // Assert
        Assert.Contains("2.0.0-beta1", json);
        Assert.Contains("1.5.0", json);
    }

    [Fact]
    public void GenerateJsonReport_WithDeprecatedPackages_MarksAsDeprecated()
    {
        // Arrange
        var updates = new List<UpdateCandidate>
        {
            new UpdateCandidate("OldPackage", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("2.0.0"), null, UpdateType.Major, true)
        };
        var projectPath = "/test/project.csproj";

        // Act
        var json = ReportGenerator.GenerateJsonReport(projectPath, updates, UpdateReportType.Preview);

        // Assert
        Assert.NotNull(json);
        Assert.Contains("\"IsDeprecated\": true", json);
        Assert.Contains("OldPackage", json);
    }

    [Fact]
    public void SaveReportToFile_CreatesFile_WithCorrectContent()
    {
        // Arrange
        var updates = new List<UpdateCandidate>
        {
            new UpdateCandidate("TestPackage", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("2.0.0"), null, UpdateType.Major, false)
        };
        var projectPath = "/test/project.csproj";
        var outputPath = Path.GetTempFileName();

        try
        {
            // Act
            var json = ReportGenerator.GenerateJsonReport(projectPath, updates, UpdateReportType.Preview);
            ReportGenerator.SaveReportToFile(json, outputPath);

            // Assert
            Assert.True(File.Exists(outputPath));
            var content = File.ReadAllText(outputPath);
            Assert.Equal(json, content);
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public void GenerateJsonReport_FormatsDateTime_AsIso8601()
    {
        // Arrange
        var updates = new List<UpdateCandidate>();
        var projectPath = "/test/project.csproj";

        // Act
        var json = ReportGenerator.GenerateJsonReport(projectPath, updates, UpdateReportType.Preview);
        var report = JsonSerializer.Deserialize<UpdateReport>(json);

        // Assert
        Assert.NotNull(report);
        Assert.True(report.GeneratedAt.Kind == DateTimeKind.Utc || report.GeneratedAt.Kind == DateTimeKind.Unspecified);
    }

    [Fact]
    public void GenerateJsonReport_PrettyPrint_IsReadable()
    {
        // Arrange
        var updates = new List<UpdateCandidate>
        {
            new UpdateCandidate("TestPackage", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("2.0.0"), null, UpdateType.Major, false)
        };
        var projectPath = "/test/project.csproj";

        // Act
        var json = ReportGenerator.GenerateJsonReport(projectPath, updates, UpdateReportType.Preview);

        // Assert - should be formatted with indentation
        Assert.Contains("\n", json); // Should have newlines for pretty printing
        Assert.Contains("  ", json); // Should have indentation
    }
}
