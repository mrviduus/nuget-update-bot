using NugetUpdateBot.Services;

namespace NugetUpdateBot.Tests.IntegrationTests;

/// <summary>
/// Integration tests for PackageUpdaterService that verify file operations.
/// These tests work with real temporary files to ensure update operations work correctly.
/// </summary>
public class PackageUpdaterIntegrationTests : IDisposable
{
    private readonly PackageUpdaterService _updater;
    private readonly string _testProjectPath;
    private readonly List<string> _cleanupFiles;

    public PackageUpdaterIntegrationTests()
    {
        _updater = new PackageUpdaterService();
        _testProjectPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.csproj");
        _cleanupFiles = new List<string>();
    }

    [Fact]
    public void UpdatePackageVersion_WithValidPackage_UpdatesVersion()
    {
        // Arrange
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""13.0.1"" />
    <PackageReference Include=""xunit"" Version=""2.5.0"" />
  </ItemGroup>
</Project>";

        File.WriteAllText(_testProjectPath, projectContent);

        // Act
        _updater.UpdatePackageVersion(_testProjectPath, "Newtonsoft.Json", "13.0.3");

        // Assert
        var updatedContent = File.ReadAllText(_testProjectPath);
        Assert.Contains(@"<PackageReference Include=""Newtonsoft.Json"" Version=""13.0.3"" />", updatedContent);
        Assert.DoesNotContain(@"Version=""13.0.1""", updatedContent);
        // Verify other package wasn't affected
        Assert.Contains(@"<PackageReference Include=""xunit"" Version=""2.5.0"" />", updatedContent);
    }

    [Fact]
    public void UpdatePackageVersion_WithMultipleUpdates_UpdatesAllCorrectly()
    {
        // Arrange
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""12.0.1"" />
    <PackageReference Include=""xunit"" Version=""2.4.0"" />
    <PackageReference Include=""Moq"" Version=""4.18.0"" />
  </ItemGroup>
</Project>";

        File.WriteAllText(_testProjectPath, projectContent);

        // Act: Update multiple packages
        _updater.UpdatePackageVersion(_testProjectPath, "Newtonsoft.Json", "13.0.3");
        _updater.UpdatePackageVersion(_testProjectPath, "xunit", "2.6.0");
        _updater.UpdatePackageVersion(_testProjectPath, "Moq", "4.20.0");

        // Assert: All should be updated
        var updatedContent = File.ReadAllText(_testProjectPath);
        Assert.Contains(@"Version=""13.0.3""", updatedContent);
        Assert.Contains(@"Version=""2.6.0""", updatedContent);
        Assert.Contains(@"Version=""4.20.0""", updatedContent);

        // Old versions should not exist
        Assert.DoesNotContain(@"Version=""12.0.1""", updatedContent);
        Assert.DoesNotContain(@"Version=""2.4.0""", updatedContent);
        Assert.DoesNotContain(@"Version=""4.18.0""", updatedContent);
    }

    [Fact]
    public void CreateBackup_CreatesBackupFile()
    {
        // Arrange
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""13.0.1"" />
  </ItemGroup>
</Project>";

        File.WriteAllText(_testProjectPath, projectContent);

        // Act
        var backupPath = _updater.CreateBackup(_testProjectPath);
        _cleanupFiles.Add(backupPath);

        // Assert
        Assert.True(File.Exists(backupPath));
        Assert.Contains(".backup.", backupPath);

        var backupContent = File.ReadAllText(backupPath);
        Assert.Equal(projectContent, backupContent);
    }

    [Fact]
    public void RestoreFromBackup_RestoresOriginalContent()
    {
        // Arrange: Create original file and backup
        var originalContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""13.0.1"" />
  </ItemGroup>
</Project>";

        File.WriteAllText(_testProjectPath, originalContent);
        var backupPath = _updater.CreateBackup(_testProjectPath);
        _cleanupFiles.Add(backupPath);

        // Modify the file
        var modifiedContent = originalContent.Replace("13.0.1", "13.0.3");
        File.WriteAllText(_testProjectPath, modifiedContent);

        // Act: Restore from backup
        _updater.RestoreFromBackup(_testProjectPath, backupPath);

        // Assert: Content should be restored to original
        var restoredContent = File.ReadAllText(_testProjectPath);
        Assert.Equal(originalContent, restoredContent);
        Assert.Contains("13.0.1", restoredContent);
        Assert.DoesNotContain("13.0.3", restoredContent);
    }

    [Fact]
    public void ValidateProjectFile_WithValidXml_ReturnsTrue()
    {
        // Arrange: Valid project file
        var validContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""13.0.1"" />
  </ItemGroup>
</Project>";

        File.WriteAllText(_testProjectPath, validContent);

        // Act
        var isValid = _updater.ValidateProjectFile(_testProjectPath);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateProjectFile_WithInvalidXml_ReturnsFalse()
    {
        // Arrange: Invalid XML (unclosed tag)
        var invalidContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""13.0.1"" />
  </ItemGroup>
</Project>";

        File.WriteAllText(_testProjectPath, invalidContent);

        // Act
        var isValid = _updater.ValidateProjectFile(_testProjectPath);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void UpdatePackageVersion_WithNonExistentPackage_ThrowsException()
    {
        // Arrange
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""13.0.1"" />
  </ItemGroup>
</Project>";

        File.WriteAllText(_testProjectPath, projectContent);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            _updater.UpdatePackageVersion(_testProjectPath, "NonExistentPackage", "1.0.0"));
    }

    [Fact]
    public void BackupAndRestore_Workflow_WorksEndToEnd()
    {
        // Arrange: Complete backup/update/restore workflow
        var originalContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""13.0.1"" />
    <PackageReference Include=""xunit"" Version=""2.5.0"" />
  </ItemGroup>
</Project>";

        File.WriteAllText(_testProjectPath, originalContent);

        // Act 1: Create backup
        var backupPath = _updater.CreateBackup(_testProjectPath);
        _cleanupFiles.Add(backupPath);

        // Act 2: Make multiple updates
        _updater.UpdatePackageVersion(_testProjectPath, "Newtonsoft.Json", "13.0.3");
        _updater.UpdatePackageVersion(_testProjectPath, "xunit", "2.6.0");

        // Verify updates were applied
        var updatedContent = File.ReadAllText(_testProjectPath);
        Assert.Contains("13.0.3", updatedContent);
        Assert.Contains("2.6.0", updatedContent);

        // Act 3: Validate (should pass)
        var isValid = _updater.ValidateProjectFile(_testProjectPath);
        Assert.True(isValid);

        // Act 4: Restore from backup
        _updater.RestoreFromBackup(_testProjectPath, backupPath);

        // Assert: Should be back to original
        var restoredContent = File.ReadAllText(_testProjectPath);
        Assert.Equal(originalContent, restoredContent);
    }

    [Fact]
    public void UpdatePackageVersion_WithVersionRange_UpdatesCorrectly()
    {
        // Arrange: Project with version range
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""[13.0.1]"" />
  </ItemGroup>
</Project>";

        File.WriteAllText(_testProjectPath, projectContent);

        // Act
        _updater.UpdatePackageVersion(_testProjectPath, "Newtonsoft.Json", "13.0.3");

        // Assert
        var updatedContent = File.ReadAllText(_testProjectPath);
        Assert.Contains(@"Version=""13.0.3""", updatedContent);
        Assert.DoesNotContain(@"Version=""[13.0.1]""", updatedContent);
    }

    public void Dispose()
    {
        // Cleanup: Remove test files
        if (File.Exists(_testProjectPath))
        {
            File.Delete(_testProjectPath);
        }

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
    }
}
