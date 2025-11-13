using Xunit;
using NuGet.Versioning;
using System.Xml.Linq;

namespace NugetUpdateBot.Tests.IntegrationTests;

public class FileOperationTests
{
    [Fact]
    public void EndToEnd_ScanAndUpdate_WorksCorrectly()
    {
        // Arrange - Create a test project file
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""12.0.3"" />
    <PackageReference Include=""Serilog"" Version=""2.10.0"" />
  </ItemGroup>
</Project>";
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, projectContent);

            // Act - Parse, then update
            var packages = PackageScanner.ParseProjectFile(tempFile);
            Assert.Equal(2, packages.Count);

            PackageUpdater.UpdatePackageVersion(tempFile, "Newtonsoft.Json", "13.0.3");

            // Assert - Verify update happened
            var updatedPackages = PackageScanner.ParseProjectFile(tempFile);
            var newtonsoftPackage = updatedPackages.First(p => p.Name == "Newtonsoft.Json");
            Assert.Equal("13.0.3", newtonsoftPackage.Version.ToString());

            // Verify other package wasn't touched
            var serilogPackage = updatedPackages.First(p => p.Name == "Serilog");
            Assert.Equal("2.10.0", serilogPackage.Version.ToString());
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void BackupAndRestore_WorksCorrectly()
    {
        // Arrange
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""12.0.3"" />
  </ItemGroup>
</Project>";
        var tempFile = Path.GetTempFileName();
        string? backupFile = null;

        try
        {
            File.WriteAllText(tempFile, projectContent);

            // Act - Create backup
            backupFile = PackageUpdater.CreateBackup(tempFile);

            // Modify original
            PackageUpdater.UpdatePackageVersion(tempFile, "Newtonsoft.Json", "13.0.3");

            // Restore from backup
            File.Copy(backupFile, tempFile, overwrite: true);

            // Assert - Should be back to original
            var packages = PackageScanner.ParseProjectFile(tempFile);
            var package = packages.First(p => p.Name == "Newtonsoft.Json");
            Assert.Equal("12.0.3", package.Version.ToString());
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
            if (backupFile != null && File.Exists(backupFile))
            {
                File.Delete(backupFile);
            }
        }
    }

    [Fact]
    public void MultipleUpdates_WithBackup_MaintainsIntegrity()
    {
        // Arrange
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""12.0.3"" />
    <PackageReference Include=""Serilog"" Version=""2.10.0"" />
    <PackageReference Include=""xunit"" Version=""2.4.2"" />
  </ItemGroup>
</Project>";
        var tempFile = Path.GetTempFileName();
        string? backupFile = null;

        try
        {
            File.WriteAllText(tempFile, projectContent);

            // Act - Create backup before updates
            backupFile = PackageUpdater.CreateBackup(tempFile);

            // Apply multiple updates
            PackageUpdater.UpdatePackageVersion(tempFile, "Newtonsoft.Json", "13.0.3");
            PackageUpdater.UpdatePackageVersion(tempFile, "Serilog", "3.1.1");
            PackageUpdater.UpdatePackageVersion(tempFile, "xunit", "2.6.0");

            // Assert - All updates applied
            var packages = PackageScanner.ParseProjectFile(tempFile);
            Assert.Equal(3, packages.Count);
            Assert.Equal("13.0.3", packages.First(p => p.Name == "Newtonsoft.Json").Version.ToString());
            Assert.Equal("3.1.1", packages.First(p => p.Name == "Serilog").Version.ToString());
            Assert.Equal("2.6.0", packages.First(p => p.Name == "xunit").Version.ToString());

            // Verify backup still has original versions
            var backupPackages = PackageScanner.ParseProjectFile(backupFile);
            Assert.Equal("12.0.3", backupPackages.First(p => p.Name == "Newtonsoft.Json").Version.ToString());
            Assert.Equal("2.10.0", backupPackages.First(p => p.Name == "Serilog").Version.ToString());
            Assert.Equal("2.4.2", backupPackages.First(p => p.Name == "xunit").Version.ToString());
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
            if (backupFile != null && File.Exists(backupFile))
            {
                File.Delete(backupFile);
            }
        }
    }

    [Fact]
    public void UpdateWithValidation_InvalidResultsInRestore()
    {
        // Arrange
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""12.0.3"" />
  </ItemGroup>
</Project>";
        var tempFile = Path.GetTempFileName();
        string? backupFile = null;

        try
        {
            File.WriteAllText(tempFile, projectContent);
            backupFile = PackageUpdater.CreateBackup(tempFile);

            // Act - Update to valid version
            PackageUpdater.UpdatePackageVersion(tempFile, "Newtonsoft.Json", "13.0.3");
            var isValid = PackageUpdater.ValidateProjectFile(tempFile);

            // Assert
            Assert.True(isValid);

            // Now corrupt the file manually
            File.WriteAllText(tempFile, "<Invalid><XML");
            var isInvalid = PackageUpdater.ValidateProjectFile(tempFile);
            Assert.False(isInvalid);

            // Restore from backup
            File.Copy(backupFile, tempFile, overwrite: true);
            var isRestoredValid = PackageUpdater.ValidateProjectFile(tempFile);
            Assert.True(isRestoredValid);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
            if (backupFile != null && File.Exists(backupFile))
            {
                File.Delete(backupFile);
            }
        }
    }

    [Fact]
    public void ConcurrentUpdates_HandleCorrectly()
    {
        // Arrange
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""Package1"" Version=""1.0.0"" />
    <PackageReference Include=""Package2"" Version=""1.0.0"" />
    <PackageReference Include=""Package3"" Version=""1.0.0"" />
  </ItemGroup>
</Project>";
        var tempFile = Path.GetTempFileName();

        try
        {
            File.WriteAllText(tempFile, projectContent);

            // Act - Simulate sequential updates (as if from concurrent scans)
            var updates = new[]
            {
                ("Package1", "2.0.0"),
                ("Package2", "2.0.0"),
                ("Package3", "2.0.0")
            };

            foreach (var (package, version) in updates)
            {
                PackageUpdater.UpdatePackageVersion(tempFile, package, version);
            }

            // Assert - All updates should be applied
            var packages = PackageScanner.ParseProjectFile(tempFile);
            Assert.All(packages, p => Assert.Equal("2.0.0", p.Version.ToString()));
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void UpdatePreservesComments_InProjectFile()
    {
        // Arrange
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <!-- This is a comment -->
  <ItemGroup>
    <!-- Package references -->
    <PackageReference Include=""Newtonsoft.Json"" Version=""12.0.3"" />
  </ItemGroup>
  <!-- End of file -->
</Project>";
        var tempFile = Path.GetTempFileName();

        try
        {
            File.WriteAllText(tempFile, projectContent);

            // Act
            PackageUpdater.UpdatePackageVersion(tempFile, "Newtonsoft.Json", "13.0.3");

            // Assert
            var content = File.ReadAllText(tempFile);
            Assert.Contains("<!-- This is a comment -->", content);
            Assert.Contains("<!-- Package references -->", content);
            Assert.Contains("<!-- End of file -->", content);
            Assert.Contains("Version=\"13.0.3\"", content);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void UpdateHandlesMultipleItemGroups()
    {
        // Arrange
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""12.0.3"" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include=""Serilog"" Version=""2.10.0"" />
  </ItemGroup>
</Project>";
        var tempFile = Path.GetTempFileName();

        try
        {
            File.WriteAllText(tempFile, projectContent);

            // Act
            PackageUpdater.UpdatePackageVersion(tempFile, "Newtonsoft.Json", "13.0.3");
            PackageUpdater.UpdatePackageVersion(tempFile, "Serilog", "3.1.1");

            // Assert
            var packages = PackageScanner.ParseProjectFile(tempFile);
            Assert.Equal(2, packages.Count);
            Assert.Equal("13.0.3", packages.First(p => p.Name == "Newtonsoft.Json").Version.ToString());
            Assert.Equal("3.1.1", packages.First(p => p.Name == "Serilog").Version.ToString());
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
