using Xunit;
using NuGet.Versioning;
using System.Xml.Linq;

namespace NugetUpdateBot.Tests.UnitTests;

public class PackageScannerTests
{
    [Fact]
    public void ParseProjectFile_ValidProject_ReturnsPackages()
    {
        // Arrange
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""13.0.3"" />
    <PackageReference Include=""Serilog"" Version=""3.1.1"" />
  </ItemGroup>
</Project>";
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, projectContent);

            // Act
            var packages = PackageScanner.ParseProjectFile(tempFile);

            // Assert
            Assert.NotNull(packages);
            Assert.Equal(2, packages.Count);
            Assert.Contains(packages, p => p.Name == "Newtonsoft.Json" && p.Version.ToString() == "13.0.3");
            Assert.Contains(packages, p => p.Name == "Serilog" && p.Version.ToString() == "3.1.1");
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
    public void ParseProjectFile_EmptyProject_ReturnsEmptyList()
    {
        // Arrange
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
</Project>";
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, projectContent);

            // Act
            var packages = PackageScanner.ParseProjectFile(tempFile);

            // Assert
            Assert.NotNull(packages);
            Assert.Empty(packages);
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
    public void ParseProjectFile_InvalidVersion_SkipsPackage()
    {
        // Arrange
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""ValidPackage"" Version=""1.0.0"" />
    <PackageReference Include=""InvalidPackage"" Version=""not-a-version"" />
    <PackageReference Include=""AnotherValid"" Version=""2.0.0"" />
  </ItemGroup>
</Project>";
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, projectContent);

            // Act
            var packages = PackageScanner.ParseProjectFile(tempFile);

            // Assert
            Assert.Equal(2, packages.Count);
            Assert.DoesNotContain(packages, p => p.Name == "InvalidPackage");
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
    public void ParseProjectFile_MissingNameOrVersion_SkipsPackage()
    {
        // Arrange
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""ValidPackage"" Version=""1.0.0"" />
    <PackageReference Include="""" Version=""1.0.0"" />
    <PackageReference Include=""NoVersion"" />
  </ItemGroup>
</Project>";
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, projectContent);

            // Act
            var packages = PackageScanner.ParseProjectFile(tempFile);

            // Assert
            Assert.Single(packages);
            Assert.Equal("ValidPackage", packages[0].Name);
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
    public void ParseProjectFile_FileNotFound_ThrowsException()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".csproj");

        // Act & Assert
        var exception = Assert.Throws<Exception>(() => PackageScanner.ParseProjectFile(nonExistentFile));
        Assert.Contains("Failed to parse project file", exception.Message);
    }

    [Fact]
    public void ParseProjectFile_InvalidXml_ThrowsException()
    {
        // Arrange
        var projectContent = "<Project><InvalidXml";
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, projectContent);

            // Act & Assert
            var exception = Assert.Throws<Exception>(() => PackageScanner.ParseProjectFile(tempFile));
            Assert.Contains("Failed to parse project file", exception.Message);
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
