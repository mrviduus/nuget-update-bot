using Xunit;
using NuGet.Versioning;
using System.Xml.Linq;

namespace NugetUpdateBot.Tests.UnitTests;

public class UpdateServiceTests
{
    [Fact]
    public void UpdatePackageVersion_ValidPackage_UpdatesVersion()
    {
        // Arrange
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""12.0.3"" />
    <PackageReference Include=""Serilog"" Version=""3.1.1"" />
  </ItemGroup>
</Project>";
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, projectContent);

            // Act
            PackageUpdater.UpdatePackageVersion(tempFile, "Newtonsoft.Json", "13.0.3");

            // Assert
            var doc = XDocument.Load(tempFile);
            var packageRef = doc.Descendants("PackageReference")
                .FirstOrDefault(p => p.Attribute("Include")?.Value == "Newtonsoft.Json");

            Assert.NotNull(packageRef);
            Assert.Equal("13.0.3", packageRef.Attribute("Version")?.Value);

            // Verify other package wasn't changed
            var otherPackage = doc.Descendants("PackageReference")
                .FirstOrDefault(p => p.Attribute("Include")?.Value == "Serilog");
            Assert.Equal("3.1.1", otherPackage?.Attribute("Version")?.Value);
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
    public void UpdatePackageVersion_PreservesFormatting_MaintainsIndentation()
    {
        // Arrange
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""12.0.3"" />
  </ItemGroup>
</Project>";
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, projectContent);

            // Act
            PackageUpdater.UpdatePackageVersion(tempFile, "Newtonsoft.Json", "13.0.3");

            // Assert
            var content = File.ReadAllText(tempFile);
            Assert.Contains("  <ItemGroup>", content); // Should preserve 2-space indentation
            Assert.Contains("    <PackageReference", content); // Should preserve 4-space indentation
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
    public void UpdatePackageVersion_NonExistentPackage_ThrowsException()
    {
        // Arrange
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""Serilog"" Version=""3.1.1"" />
  </ItemGroup>
</Project>";
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, projectContent);

            // Act & Assert
            var exception = Assert.Throws<Exception>(() =>
                PackageUpdater.UpdatePackageVersion(tempFile, "NonExistent.Package", "1.0.0"));
            Assert.Contains("Package 'NonExistent.Package' not found", exception.Message);
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
    public void UpdatePackageVersion_FileNotFound_ThrowsException()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".csproj");

        // Act & Assert
        var exception = Assert.Throws<Exception>(() =>
            PackageUpdater.UpdatePackageVersion(nonExistentFile, "Newtonsoft.Json", "13.0.3"));
        Assert.Contains("Failed to update package", exception.Message);
    }

    [Fact]
    public void CreateBackup_CreatesBackupFile_WithTimestamp()
    {
        // Arrange
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"" />";
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, projectContent);

            // Act
            var backupPath = PackageUpdater.CreateBackup(tempFile);

            // Assert
            Assert.True(File.Exists(backupPath));
            Assert.Contains(".backup.", backupPath);
            var backupContent = File.ReadAllText(backupPath);
            Assert.Equal(projectContent, backupContent);

            // Cleanup
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }
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
    public void UpdateMultiplePackages_UpdatesAll_Correctly()
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
        try
        {
            File.WriteAllText(tempFile, projectContent);
            var updates = new Dictionary<string, string>
            {
                { "Newtonsoft.Json", "13.0.3" },
                { "Serilog", "3.1.1" },
                { "xunit", "2.6.0" }
            };

            // Act
            foreach (var update in updates)
            {
                PackageUpdater.UpdatePackageVersion(tempFile, update.Key, update.Value);
            }

            // Assert
            var doc = XDocument.Load(tempFile);
            var packages = doc.Descendants("PackageReference").ToList();

            Assert.Equal(3, packages.Count);
            Assert.Equal("13.0.3", packages.First(p => p.Attribute("Include")?.Value == "Newtonsoft.Json").Attribute("Version")?.Value);
            Assert.Equal("3.1.1", packages.First(p => p.Attribute("Include")?.Value == "Serilog").Attribute("Version")?.Value);
            Assert.Equal("2.6.0", packages.First(p => p.Attribute("Include")?.Value == "xunit").Attribute("Version")?.Value);
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
    public void ValidateProjectFile_ValidXml_ReturnsTrue()
    {
        // Arrange
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""13.0.3"" />
  </ItemGroup>
</Project>";
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, projectContent);

            // Act
            var isValid = PackageUpdater.ValidateProjectFile(tempFile);

            // Assert
            Assert.True(isValid);
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
    public void ValidateProjectFile_InvalidXml_ReturnsFalse()
    {
        // Arrange
        var projectContent = "<Project><InvalidXml";
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, projectContent);

            // Act
            var isValid = PackageUpdater.ValidateProjectFile(tempFile);

            // Assert
            Assert.False(isValid);
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
    public void ValidateProjectFile_FileNotFound_ReturnsFalse()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".csproj");

        // Act
        var isValid = PackageUpdater.ValidateProjectFile(nonExistentFile);

        // Assert
        Assert.False(isValid);
    }
}
