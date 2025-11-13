using Xunit;

namespace NugetUpdateBot.Tests.UnitTests;

public class EnvironmentConfigTests
{
    [Fact]
    public void LoadFromEnvironment_WithUpdatePolicyVar_ParsesCorrectly()
    {
        // Arrange
        Environment.SetEnvironmentVariable("NUGET_BOT_UPDATE_POLICY", "Minor");

        try
        {
            // Act
            var config = ConfigurationLoader.LoadFromEnvironment();

            // Assert
            Assert.NotNull(config);
            Assert.Equal(UpdatePolicy.Minor, config.UpdatePolicy);
        }
        finally
        {
            Environment.SetEnvironmentVariable("NUGET_BOT_UPDATE_POLICY", null);
        }
    }

    [Fact]
    public void LoadFromEnvironment_WithIncludePrereleaseVar_ParsesCorrectly()
    {
        // Arrange
        Environment.SetEnvironmentVariable("NUGET_BOT_INCLUDE_PRERELEASE", "true");

        try
        {
            // Act
            var config = ConfigurationLoader.LoadFromEnvironment();

            // Assert
            Assert.NotNull(config);
            Assert.True(config.IncludePrerelease);
        }
        finally
        {
            Environment.SetEnvironmentVariable("NUGET_BOT_INCLUDE_PRERELEASE", null);
        }
    }

    [Fact]
    public void LoadFromEnvironment_WithMaxParallelismVar_ParsesCorrectly()
    {
        // Arrange
        Environment.SetEnvironmentVariable("NUGET_BOT_MAX_PARALLELISM", "10");

        try
        {
            // Act
            var config = ConfigurationLoader.LoadFromEnvironment();

            // Assert
            Assert.NotNull(config);
            Assert.Equal(10, config.MaxParallelism);
        }
        finally
        {
            Environment.SetEnvironmentVariable("NUGET_BOT_MAX_PARALLELISM", null);
        }
    }

    [Fact]
    public void LoadFromEnvironment_WithExcludePackagesVar_ParsesCommaSeparated()
    {
        // Arrange
        Environment.SetEnvironmentVariable("NUGET_BOT_EXCLUDE_PACKAGES", "Microsoft.EntityFrameworkCore,Newtonsoft.Json");

        try
        {
            // Act
            var config = ConfigurationLoader.LoadFromEnvironment();

            // Assert
            Assert.NotNull(config);
            Assert.Equal(2, config.ExcludePackages.Count);
            Assert.Contains("Microsoft.EntityFrameworkCore", config.ExcludePackages);
            Assert.Contains("Newtonsoft.Json", config.ExcludePackages);
        }
        finally
        {
            Environment.SetEnvironmentVariable("NUGET_BOT_EXCLUDE_PACKAGES", null);
        }
    }

    [Fact]
    public void LoadFromEnvironment_NoEnvVars_ReturnsDefaults()
    {
        // Arrange - ensure no env vars set
        Environment.SetEnvironmentVariable("NUGET_BOT_UPDATE_POLICY", null);
        Environment.SetEnvironmentVariable("NUGET_BOT_INCLUDE_PRERELEASE", null);
        Environment.SetEnvironmentVariable("NUGET_BOT_MAX_PARALLELISM", null);
        Environment.SetEnvironmentVariable("NUGET_BOT_EXCLUDE_PACKAGES", null);

        // Act
        var config = ConfigurationLoader.LoadFromEnvironment();

        // Assert
        Assert.NotNull(config);
        Assert.Equal(UpdatePolicy.Major, config.UpdatePolicy);
        Assert.False(config.IncludePrerelease);
        Assert.Equal(5, config.MaxParallelism);
        Assert.Empty(config.ExcludePackages);
    }

    [Fact]
    public void LoadFromEnvironment_InvalidUpdatePolicy_UsesDefault()
    {
        // Arrange
        Environment.SetEnvironmentVariable("NUGET_BOT_UPDATE_POLICY", "InvalidValue");

        try
        {
            // Act
            var config = ConfigurationLoader.LoadFromEnvironment();

            // Assert
            Assert.NotNull(config);
            Assert.Equal(UpdatePolicy.Major, config.UpdatePolicy); // Falls back to default
        }
        finally
        {
            Environment.SetEnvironmentVariable("NUGET_BOT_UPDATE_POLICY", null);
        }
    }

    [Fact]
    public void LoadFromEnvironment_InvalidMaxParallelism_UsesDefault()
    {
        // Arrange
        Environment.SetEnvironmentVariable("NUGET_BOT_MAX_PARALLELISM", "invalid");

        try
        {
            // Act
            var config = ConfigurationLoader.LoadFromEnvironment();

            // Assert
            Assert.NotNull(config);
            Assert.Equal(5, config.MaxParallelism); // Falls back to default
        }
        finally
        {
            Environment.SetEnvironmentVariable("NUGET_BOT_MAX_PARALLELISM", null);
        }
    }

    [Fact]
    public void LoadFromEnvironment_NegativeMaxParallelism_UsesDefault()
    {
        // Arrange
        Environment.SetEnvironmentVariable("NUGET_BOT_MAX_PARALLELISM", "-5");

        try
        {
            // Act
            var config = ConfigurationLoader.LoadFromEnvironment();

            // Assert
            Assert.NotNull(config);
            Assert.Equal(5, config.MaxParallelism); // Falls back to default
        }
        finally
        {
            Environment.SetEnvironmentVariable("NUGET_BOT_MAX_PARALLELISM", null);
        }
    }

    [Fact]
    public void MergeConfigurations_EnvOverridesFile()
    {
        // Arrange
        var fileConfig = new BotConfiguration(
            UpdatePolicy: UpdatePolicy.Minor,
            ExcludePackages: new List<string> { "Package1" },
            IncludePrerelease: false,
            MaxParallelism: 5,
            UpdateRules: new List<UpdateRule>()
        );

        var envConfig = new BotConfiguration(
            UpdatePolicy: UpdatePolicy.Major, // Override
            ExcludePackages: new List<string>(),
            IncludePrerelease: true, // Override
            MaxParallelism: 10, // Override
            UpdateRules: new List<UpdateRule>()
        );

        // Act
        var merged = ConfigurationLoader.MergeConfigurations(fileConfig, envConfig);

        // Assert
        Assert.Equal(UpdatePolicy.Major, merged.UpdatePolicy); // Env wins
        Assert.True(merged.IncludePrerelease); // Env wins
        Assert.Equal(10, merged.MaxParallelism); // Env wins
    }

    [Fact]
    public void LoadFromEnvironment_EmptyExcludePackages_ReturnsEmptyList()
    {
        // Arrange
        Environment.SetEnvironmentVariable("NUGET_BOT_EXCLUDE_PACKAGES", "");

        try
        {
            // Act
            var config = ConfigurationLoader.LoadFromEnvironment();

            // Assert
            Assert.NotNull(config);
            Assert.Empty(config.ExcludePackages);
        }
        finally
        {
            Environment.SetEnvironmentVariable("NUGET_BOT_EXCLUDE_PACKAGES", null);
        }
    }

    [Fact]
    public void LoadFromEnvironment_SinglePackageExclude_ParsesCorrectly()
    {
        // Arrange
        Environment.SetEnvironmentVariable("NUGET_BOT_EXCLUDE_PACKAGES", "Newtonsoft.Json");

        try
        {
            // Act
            var config = ConfigurationLoader.LoadFromEnvironment();

            // Assert
            Assert.NotNull(config);
            Assert.Single(config.ExcludePackages);
            Assert.Contains("Newtonsoft.Json", config.ExcludePackages);
        }
        finally
        {
            Environment.SetEnvironmentVariable("NUGET_BOT_EXCLUDE_PACKAGES", null);
        }
    }

    [Fact]
    public void LoadFromEnvironment_WhitespaceInExcludeList_TrimsCorrectly()
    {
        // Arrange
        Environment.SetEnvironmentVariable("NUGET_BOT_EXCLUDE_PACKAGES", " Microsoft.EntityFrameworkCore , Newtonsoft.Json ");

        try
        {
            // Act
            var config = ConfigurationLoader.LoadFromEnvironment();

            // Assert
            Assert.NotNull(config);
            Assert.Equal(2, config.ExcludePackages.Count);
            Assert.Contains("Microsoft.EntityFrameworkCore", config.ExcludePackages);
            Assert.Contains("Newtonsoft.Json", config.ExcludePackages);
        }
        finally
        {
            Environment.SetEnvironmentVariable("NUGET_BOT_EXCLUDE_PACKAGES", null);
        }
    }

    [Theory]
    [InlineData("Major", UpdatePolicy.Major)]
    [InlineData("Minor", UpdatePolicy.Minor)]
    [InlineData("Patch", UpdatePolicy.Patch)]
    [InlineData("major", UpdatePolicy.Major)] // Case insensitive
    [InlineData("MINOR", UpdatePolicy.Minor)] // Case insensitive
    public void LoadFromEnvironment_VariousPolicyValues_ParsesCorrectly(string envValue, UpdatePolicy expected)
    {
        // Arrange
        Environment.SetEnvironmentVariable("NUGET_BOT_UPDATE_POLICY", envValue);

        try
        {
            // Act
            var config = ConfigurationLoader.LoadFromEnvironment();

            // Assert
            Assert.Equal(expected, config.UpdatePolicy);
        }
        finally
        {
            Environment.SetEnvironmentVariable("NUGET_BOT_UPDATE_POLICY", null);
        }
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("TRUE", true)]
    [InlineData("false", false)]
    [InlineData("False", false)]
    [InlineData("FALSE", false)]
    public void LoadFromEnvironment_VariousBoolValues_ParsesCorrectly(string envValue, bool expected)
    {
        // Arrange
        Environment.SetEnvironmentVariable("NUGET_BOT_INCLUDE_PRERELEASE", envValue);

        try
        {
            // Act
            var config = ConfigurationLoader.LoadFromEnvironment();

            // Assert
            Assert.Equal(expected, config.IncludePrerelease);
        }
        finally
        {
            Environment.SetEnvironmentVariable("NUGET_BOT_INCLUDE_PRERELEASE", null);
        }
    }
}
