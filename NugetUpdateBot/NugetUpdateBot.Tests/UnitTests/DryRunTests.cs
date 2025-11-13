using Xunit;
using NuGet.Versioning;
using System.IO;

namespace NugetUpdateBot.Tests.UnitTests;

[Collection("ConsoleTests")]
public class DryRunTests
{
    [Fact]
    public void PreviewUpdates_DisplaysUpdatesWithoutModifying()
    {
        // Arrange
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""12.0.3"" />
    <PackageReference Include=""Serilog"" Version=""2.10.0"" />
  </ItemGroup>
</Project>";
        var tempFile = Path.GetTempFileName();
        var output = new StringWriter();
        Console.SetOut(output);

        try
        {
            File.WriteAllText(tempFile, projectContent);
            var originalContent = File.ReadAllText(tempFile);

            var updates = new List<UpdateCandidate>
            {
                new UpdateCandidate("Newtonsoft.Json", NuGetVersion.Parse("12.0.3"), NuGetVersion.Parse("13.0.3"), null, UpdateType.Major, false),
                new UpdateCandidate("Serilog", NuGetVersion.Parse("2.10.0"), NuGetVersion.Parse("3.1.1"), null, UpdateType.Major, false)
            };

            // Act
            DryRunService.PreviewUpdates(tempFile, updates);

            // Assert
            var result = output.ToString();
            Assert.Contains("DRY RUN - No changes will be made", result);
            Assert.Contains("Newtonsoft.Json", result);
            Assert.Contains("12.0.3", result);
            Assert.Contains("13.0.3", result);
            Assert.Contains("Serilog", result);
            Assert.Contains("2.10.0", result);
            Assert.Contains("3.1.1", result);

            // Verify file wasn't modified
            var currentContent = File.ReadAllText(tempFile);
            Assert.Equal(originalContent, currentContent);
        }
        finally
        {
            Console.SetOut(Console.Out);
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void PreviewUpdates_NoUpdates_DisplaysNoChangesMessage()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var output = new StringWriter();
        Console.SetOut(output);

        try
        {
            File.WriteAllText(tempFile, @"<Project Sdk=""Microsoft.NET.Sdk"" />");
            var updates = new List<UpdateCandidate>();

            // Act
            DryRunService.PreviewUpdates(tempFile, updates);

            // Assert
            var result = output.ToString();
            Assert.Contains("No updates to apply", result);
        }
        finally
        {
            Console.SetOut(Console.Out);
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void PreviewUpdates_ShowsUpdateType_ForEachPackage()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var output = new StringWriter();
        Console.SetOut(output);

        try
        {
            File.WriteAllText(tempFile, @"<Project Sdk=""Microsoft.NET.Sdk"" />");
            var updates = new List<UpdateCandidate>
            {
                new UpdateCandidate("Package1", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("1.0.1"), null, UpdateType.Patch, false),
                new UpdateCandidate("Package2", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("1.1.0"), null, UpdateType.Minor, false),
                new UpdateCandidate("Package3", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("2.0.0"), null, UpdateType.Major, false)
            };

            // Act
            DryRunService.PreviewUpdates(tempFile, updates);

            // Assert
            var result = output.ToString();
            Assert.Contains("Patch", result);
            Assert.Contains("Minor", result);
            Assert.Contains("Major", result);
        }
        finally
        {
            Console.SetOut(Console.Out);
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void FormatDryRunOutput_FormatsCorrectly()
    {
        // Arrange
        var updates = new List<UpdateCandidate>
        {
            new UpdateCandidate("Newtonsoft.Json", NuGetVersion.Parse("12.0.3"), NuGetVersion.Parse("13.0.3"), null, UpdateType.Major, false)
        };

        // Act
        var output = DryRunService.FormatDryRunOutput(updates);

        // Assert
        Assert.Contains("Newtonsoft.Json", output);
        Assert.Contains("12.0.3", output);
        Assert.Contains("13.0.3", output);
        Assert.Contains("→", output);
    }

    [Fact]
    public void FormatDryRunOutput_ShowsPrereleaseWhenAvailable()
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

        // Act
        var output = DryRunService.FormatDryRunOutput(updates);

        // Assert
        Assert.Contains("2.0.0-beta1", output);
    }

    [Fact]
    public void GenerateDryRunSummary_CountsUpdateTypes()
    {
        // Arrange
        var updates = new List<UpdateCandidate>
        {
            new UpdateCandidate("P1", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("1.0.1"), null, UpdateType.Patch, false),
            new UpdateCandidate("P2", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("1.0.2"), null, UpdateType.Patch, false),
            new UpdateCandidate("P3", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("1.1.0"), null, UpdateType.Minor, false),
            new UpdateCandidate("P4", NuGetVersion.Parse("1.0.0"), NuGetVersion.Parse("2.0.0"), null, UpdateType.Major, false)
        };
        var output = new StringWriter();
        Console.SetOut(output);

        try
        {
            // Act
            DryRunService.GenerateDryRunSummary(updates);

            // Assert
            var result = output.ToString();
            Assert.Contains("Total updates: 4", result);
            Assert.Contains("Patch: 2", result);
            Assert.Contains("Minor: 1", result);
            Assert.Contains("Major: 1", result);
        }
        finally
        {
            Console.SetOut(Console.Out);
        }
    }

    [Fact]
    public void GenerateDryRunSummary_EmptyList_ShowsZeros()
    {
        // Arrange
        var updates = new List<UpdateCandidate>();
        var output = new StringWriter();
        Console.SetOut(output);

        try
        {
            // Act
            DryRunService.GenerateDryRunSummary(updates);

            // Assert
            var result = output.ToString();
            Assert.Contains("Total updates: 0", result);
        }
        finally
        {
            Console.SetOut(Console.Out);
        }
    }

    [Fact]
    public void CompareBeforeAfter_ShowsDifferences()
    {
        // Arrange
        var output = new StringWriter();
        Console.SetOut(output);

        try
        {
            // Act
            DryRunService.CompareBeforeAfter("Newtonsoft.Json", "12.0.3", "13.0.3");

            // Assert
            var result = output.ToString();
            Assert.Contains("-", result); // Indicates old version
            Assert.Contains("+", result); // Indicates new version
            Assert.Contains("12.0.3", result);
            Assert.Contains("13.0.3", result);
        }
        finally
        {
            Console.SetOut(Console.Out);
        }
    }
}
