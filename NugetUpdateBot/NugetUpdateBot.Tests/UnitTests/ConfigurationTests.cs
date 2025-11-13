using Xunit;
using System.Text.Json;

namespace NugetUpdateBot.Tests.UnitTests;

public class ConfigurationTests
{
    [Fact]
    public void LoadConfiguration_ValidJsonFile_ParsesCorrectly()
    {
        // Arrange
        var json = """
        {
          "UpdatePolicy": "Minor",
          "ExcludePackages": ["Microsoft.EntityFrameworkCore", "Newtonsoft.Json"],
          "IncludePrerelease": false,
          "MaxParallelism": 5
        }
        """;
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, json);

        try
        {
            // Act
            var config = ConfigurationLoader.LoadFromFile(tempFile);

            // Assert
            Assert.NotNull(config);
            Assert.Equal(UpdatePolicy.Minor, config.UpdatePolicy);
            Assert.Equal(2, config.ExcludePackages.Count);
            Assert.Contains("Microsoft.EntityFrameworkCore", config.ExcludePackages);
            Assert.Contains("Newtonsoft.Json", config.ExcludePackages);
            Assert.False(config.IncludePrerelease);
            Assert.Equal(5, config.MaxParallelism);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void LoadConfiguration_MissingFile_ReturnsDefaults()
    {
        // Arrange
        var nonExistentFile = "/path/to/nonexistent/config.json";

        // Act
        var config = ConfigurationLoader.LoadFromFile(nonExistentFile);

        // Assert
        Assert.NotNull(config);
        Assert.Equal(UpdatePolicy.Major, config.UpdatePolicy); // Default
        Assert.Empty(config.ExcludePackages);
        Assert.False(config.IncludePrerelease); // Default
        Assert.Equal(5, config.MaxParallelism); // Default
    }

    [Fact]
    public void LoadConfiguration_EmptyFile_ReturnsDefaults()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "{}");

        try
        {
            // Act
            var config = ConfigurationLoader.LoadFromFile(tempFile);

            // Assert
            Assert.NotNull(config);
            Assert.Equal(UpdatePolicy.Major, config.UpdatePolicy);
            Assert.Empty(config.ExcludePackages);
            Assert.False(config.IncludePrerelease);
            Assert.Equal(5, config.MaxParallelism);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void LoadConfiguration_PartialJson_UsesMissingDefaults()
    {
        // Arrange
        var json = """
        {
          "UpdatePolicy": "Patch"
        }
        """;
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, json);

        try
        {
            // Act
            var config = ConfigurationLoader.LoadFromFile(tempFile);

            // Assert
            Assert.Equal(UpdatePolicy.Patch, config.UpdatePolicy);
            Assert.Empty(config.ExcludePackages); // Default
            Assert.False(config.IncludePrerelease); // Default
            Assert.Equal(5, config.MaxParallelism); // Default
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void LoadConfiguration_InvalidJson_ReturnsDefaults()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "{ invalid json");

        try
        {
            // Act
            var config = ConfigurationLoader.LoadFromFile(tempFile);

            // Assert - should return defaults when JSON is invalid
            Assert.NotNull(config);
            Assert.Equal(UpdatePolicy.Major, config.UpdatePolicy);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void LoadConfiguration_WithUpdateRules_ParsesCorrectly()
    {
        // Arrange
        var json = """
        {
          "UpdatePolicy": "Major",
          "UpdateRules": [
            {
              "Pattern": "Microsoft.*",
              "Policy": "Minor"
            },
            {
              "Pattern": "System.Text.*",
              "Policy": "Patch"
            }
          ]
        }
        """;
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, json);

        try
        {
            // Act
            var config = ConfigurationLoader.LoadFromFile(tempFile);

            // Assert
            Assert.NotNull(config);
            Assert.Equal(2, config.UpdateRules.Count);
            Assert.Equal("Microsoft.*", config.UpdateRules[0].Pattern);
            Assert.Equal(UpdatePolicy.Minor, config.UpdateRules[0].Policy);
            Assert.Equal("System.Text.*", config.UpdateRules[1].Pattern);
            Assert.Equal(UpdatePolicy.Patch, config.UpdateRules[1].Policy);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void MergeConfiguration_CliOverridesFile()
    {
        // Arrange
        var fileConfig = new BotConfiguration(
            UpdatePolicy: UpdatePolicy.Minor,
            ExcludePackages: new List<string> { "Package1" },
            IncludePrerelease: false,
            MaxParallelism: 5,
            UpdateRules: new List<UpdateRule>()
        );

        var cliConfig = new BotConfiguration(
            UpdatePolicy: UpdatePolicy.Major, // Override
            ExcludePackages: new List<string>(),
            IncludePrerelease: true, // Override
            MaxParallelism: 10, // Override
            UpdateRules: new List<UpdateRule>()
        );

        // Act
        var merged = ConfigurationLoader.MergeConfigurations(fileConfig, cliConfig);

        // Assert
        Assert.Equal(UpdatePolicy.Major, merged.UpdatePolicy); // CLI wins
        Assert.True(merged.IncludePrerelease); // CLI wins
        Assert.Equal(10, merged.MaxParallelism); // CLI wins
    }

    [Fact]
    public void ValidateConfiguration_ValidConfig_ReturnsTrue()
    {
        // Arrange
        var config = new BotConfiguration(
            UpdatePolicy: UpdatePolicy.Major,
            ExcludePackages: new List<string> { "Valid.Package" },
            IncludePrerelease: false,
            MaxParallelism: 5,
            UpdateRules: new List<UpdateRule>()
        );

        // Act
        var isValid = ConfigurationLoader.ValidateConfiguration(config, out var errors);

        // Assert
        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateConfiguration_InvalidMaxParallelism_ReturnsFalse()
    {
        // Arrange
        var config = new BotConfiguration(
            UpdatePolicy: UpdatePolicy.Major,
            ExcludePackages: new List<string>(),
            IncludePrerelease: false,
            MaxParallelism: 0, // Invalid
            UpdateRules: new List<UpdateRule>()
        );

        // Act
        var isValid = ConfigurationLoader.ValidateConfiguration(config, out var errors);

        // Assert
        Assert.False(isValid);
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("MaxParallelism"));
    }

    [Fact]
    public void ValidateConfiguration_InvalidUpdateRule_ReturnsFalse()
    {
        // Arrange
        var config = new BotConfiguration(
            UpdatePolicy: UpdatePolicy.Major,
            ExcludePackages: new List<string>(),
            IncludePrerelease: false,
            MaxParallelism: 5,
            UpdateRules: new List<UpdateRule>
            {
                new UpdateRule("", UpdatePolicy.Major) // Empty pattern is invalid
            }
        );

        // Act
        var isValid = ConfigurationLoader.ValidateConfiguration(config, out var errors);

        // Assert
        Assert.False(isValid);
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("Pattern"));
    }
}
