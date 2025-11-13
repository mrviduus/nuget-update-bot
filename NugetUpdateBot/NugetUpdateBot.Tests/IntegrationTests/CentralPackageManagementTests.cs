using NugetUpdateBot.Services;

namespace NugetUpdateBot.Tests.IntegrationTests;

/// <summary>
/// Integration tests for Central Package Management (CPM) support.
/// Tests updating packages when using Directory.Packages.props.
/// </summary>
public class CentralPackageManagementTests : IDisposable
{
    private readonly PackageUpdaterService _updater;
    private readonly string _testProjectPath;
    private readonly string _testPackagesPropsPath;
    private readonly string _testProjectDir;
    private readonly List<string> _cleanupFiles;

    public CentralPackageManagementTests()
    {
        _updater = new PackageUpdaterService();
        _testProjectDir = Path.Combine(Path.GetTempPath(), $"cpm_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testProjectDir);

        _testProjectPath = Path.Combine(_testProjectDir, "TestProject.csproj");
        _testPackagesPropsPath = Path.Combine(_testProjectDir, "Directory.Packages.props");
        _cleanupFiles = new List<string>();
    }

    [Fact]
    public void UpdatePackageVersion_WithCpm_UpdatesDirectoryPackagesProps()
    {
        // Arrange: Create CPM project structure
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" />
    <PackageReference Include=""xunit"" />
  </ItemGroup>
</Project>";

        var packagesPropsContent = @"<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include=""Newtonsoft.Json"" Version=""13.0.1"" />
    <PackageVersion Include=""xunit"" Version=""2.5.0"" />
  </ItemGroup>
</Project>";

        File.WriteAllText(_testProjectPath, projectContent);
        File.WriteAllText(_testPackagesPropsPath, packagesPropsContent);

        // Act: Update package version
        _updater.UpdatePackageVersion(_testProjectPath, "Newtonsoft.Json", "13.0.3");

        // Assert: Directory.Packages.props should be updated
        var updatedPackagesProps = File.ReadAllText(_testPackagesPropsPath);
        Assert.Contains(@"<PackageVersion Include=""Newtonsoft.Json"" Version=""13.0.3"" />", updatedPackagesProps);
        Assert.DoesNotContain(@"Version=""13.0.1""", updatedPackagesProps);

        // Project file should remain unchanged
        var projectFile = File.ReadAllText(_testProjectPath);
        Assert.Equal(projectContent, projectFile);
    }

    [Fact]
    public void UpdatePackageVersion_WithCpm_MultiplePackages_UpdatesCorrectPackage()
    {
        // Arrange
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" />
    <PackageReference Include=""xunit"" />
    <PackageReference Include=""Moq"" />
  </ItemGroup>
</Project>";

        var packagesPropsContent = @"<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include=""Newtonsoft.Json"" Version=""13.0.1"" />
    <PackageVersion Include=""xunit"" Version=""2.5.0"" />
    <PackageVersion Include=""Moq"" Version=""4.18.0"" />
  </ItemGroup>
</Project>";

        File.WriteAllText(_testProjectPath, projectContent);
        File.WriteAllText(_testPackagesPropsPath, packagesPropsContent);

        // Act: Update multiple packages
        _updater.UpdatePackageVersion(_testProjectPath, "Newtonsoft.Json", "13.0.3");
        _updater.UpdatePackageVersion(_testProjectPath, "xunit", "2.6.0");

        // Assert: Both should be updated, Moq unchanged
        var updatedPackagesProps = File.ReadAllText(_testPackagesPropsPath);
        Assert.Contains(@"Version=""13.0.3""", updatedPackagesProps);
        Assert.Contains(@"Version=""2.6.0""", updatedPackagesProps);
        Assert.Contains(@"<PackageVersion Include=""Moq"" Version=""4.18.0"" />", updatedPackagesProps);
    }

    [Fact]
    public void UpdatePackageVersion_WithCpmInParentDirectory_FindsAndUpdates()
    {
        // Arrange: Create nested directory structure
        var srcDir = Path.Combine(_testProjectDir, "src");
        Directory.CreateDirectory(srcDir);

        var nestedProjectPath = Path.Combine(srcDir, "NestedProject.csproj");

        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" />
  </ItemGroup>
</Project>";

        var packagesPropsContent = @"<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include=""Newtonsoft.Json"" Version=""13.0.1"" />
  </ItemGroup>
</Project>";

        File.WriteAllText(nestedProjectPath, projectContent);
        File.WriteAllText(_testPackagesPropsPath, packagesPropsContent);
        _cleanupFiles.Add(nestedProjectPath);

        // Act: Update from nested project
        _updater.UpdatePackageVersion(nestedProjectPath, "Newtonsoft.Json", "13.0.3");

        // Assert: Parent Directory.Packages.props should be updated
        var updatedPackagesProps = File.ReadAllText(_testPackagesPropsPath);
        Assert.Contains(@"Version=""13.0.3""", updatedPackagesProps);
    }

    [Fact]
    public void CreateBackup_WithCpm_BacksUpBothFiles()
    {
        // Arrange
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" />
  </ItemGroup>
</Project>";

        var packagesPropsContent = @"<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include=""Newtonsoft.Json"" Version=""13.0.1"" />
  </ItemGroup>
</Project>";

        File.WriteAllText(_testProjectPath, projectContent);
        File.WriteAllText(_testPackagesPropsPath, packagesPropsContent);

        // Act: Create backup
        var backupPath = _updater.CreateBackup(_testProjectPath);
        _cleanupFiles.Add(backupPath);

        // Assert: Both backups should exist
        Assert.True(File.Exists(backupPath));

        // Find Directory.Packages.props backup
        var packagesPropsBackups = Directory.GetFiles(_testProjectDir, "Directory.Packages.backup.*.props");
        Assert.NotEmpty(packagesPropsBackups);
        _cleanupFiles.AddRange(packagesPropsBackups);

        // Verify backup contents
        var projectBackupContent = File.ReadAllText(backupPath);
        Assert.Equal(projectContent, projectBackupContent);

        var packagesPropsBackupContent = File.ReadAllText(packagesPropsBackups[0]);
        Assert.Equal(packagesPropsContent, packagesPropsBackupContent);
    }

    [Fact]
    public void UpdatePackageVersion_WithNonCpmProject_UpdatesProjectFile()
    {
        // Arrange: Traditional project (no CPM)
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""13.0.1"" />
  </ItemGroup>
</Project>";

        File.WriteAllText(_testProjectPath, projectContent);

        // Act: Update package
        _updater.UpdatePackageVersion(_testProjectPath, "Newtonsoft.Json", "13.0.3");

        // Assert: Project file should be updated
        var updatedProject = File.ReadAllText(_testProjectPath);
        Assert.Contains(@"Version=""13.0.3""", updatedProject);
        Assert.DoesNotContain(@"Version=""13.0.1""", updatedProject);
    }

    [Fact]
    public void UpdatePackageVersion_CpmPackageNotFound_ThrowsException()
    {
        // Arrange
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" />
  </ItemGroup>
</Project>";

        var packagesPropsContent = @"<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include=""Newtonsoft.Json"" Version=""13.0.1"" />
  </ItemGroup>
</Project>";

        File.WriteAllText(_testProjectPath, projectContent);
        File.WriteAllText(_testPackagesPropsPath, packagesPropsContent);

        // Act & Assert: Should throw when package not found
        Assert.Throws<InvalidOperationException>(() =>
            _updater.UpdatePackageVersion(_testProjectPath, "NonExistentPackage", "1.0.0"));
    }

    [Fact]
    public void UpdatePackageVersion_WithCpmDetectedByLackOfVersions_UpdatesCorrectly()
    {
        // Arrange: CPM detected by lack of Version attributes (no explicit property)
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" />
    <PackageReference Include=""xunit"" />
    <PackageReference Include=""Moq"" />
    <PackageReference Include=""System.Text.Json"" />
  </ItemGroup>
</Project>";

        var packagesPropsContent = @"<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include=""Newtonsoft.Json"" Version=""13.0.1"" />
    <PackageVersion Include=""xunit"" Version=""2.5.0"" />
    <PackageVersion Include=""Moq"" Version=""4.18.0"" />
    <PackageVersion Include=""System.Text.Json"" Version=""8.0.0"" />
  </ItemGroup>
</Project>";

        File.WriteAllText(_testProjectPath, projectContent);
        File.WriteAllText(_testPackagesPropsPath, packagesPropsContent);

        // Act: Update package
        _updater.UpdatePackageVersion(_testProjectPath, "Newtonsoft.Json", "13.0.3");

        // Assert: Directory.Packages.props should be updated
        var updatedPackagesProps = File.ReadAllText(_testPackagesPropsPath);
        Assert.Contains(@"Version=""13.0.3""", updatedPackagesProps);
    }

    [Fact]
    public void UpdatePackageVersion_MixedVersionAttributes_UsesCorrectStrategy()
    {
        // Arrange: Some packages with versions (20%), most without (80%) - should use CPM
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" />
    <PackageReference Include=""xunit"" />
    <PackageReference Include=""Moq"" />
    <PackageReference Include=""System.Text.Json"" />
    <PackageReference Include=""SpecialPackage"" Version=""1.0.0"" />
  </ItemGroup>
</Project>";

        var packagesPropsContent = @"<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include=""Newtonsoft.Json"" Version=""13.0.1"" />
    <PackageVersion Include=""xunit"" Version=""2.5.0"" />
    <PackageVersion Include=""Moq"" Version=""4.18.0"" />
    <PackageVersion Include=""System.Text.Json"" Version=""8.0.0"" />
  </ItemGroup>
</Project>";

        File.WriteAllText(_testProjectPath, projectContent);
        File.WriteAllText(_testPackagesPropsPath, packagesPropsContent);

        // Act: Update package
        _updater.UpdatePackageVersion(_testProjectPath, "Newtonsoft.Json", "13.0.3");

        // Assert: Should update Directory.Packages.props (CPM detected)
        var updatedPackagesProps = File.ReadAllText(_testPackagesPropsPath);
        Assert.Contains(@"Version=""13.0.3""", updatedPackagesProps);
    }

    public void Dispose()
    {
        // Cleanup: Remove all test files and directories
        foreach (var file in _cleanupFiles)
        {
            if (File.Exists(file))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        if (Directory.Exists(_testProjectDir))
        {
            try
            {
                Directory.Delete(_testProjectDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
