using System.Text.Json;
using NugetUpdateBot.Models;

namespace NugetUpdateBot.Configuration;

/// <summary>
/// Service responsible for loading and parsing bot configuration.
/// Follows Single Responsibility Principle - only handles configuration loading.
/// </summary>
public class ConfigurationLoaderService
{
    private const string DefaultConfigFileName = ".nuget-update-bot.json";

    /// <summary>
    /// Loads configuration from a file.
    /// </summary>
    /// <param name="configPath">Path to configuration file. If null, searches for default config.</param>
    /// <returns>Loaded configuration or default configuration if file not found.</returns>
    public BotConfiguration LoadConfiguration(string? configPath = null)
    {
        var path = configPath ?? FindDefaultConfigFile();

        if (path == null || !File.Exists(path))
        {
            return GetDefaultConfiguration();
        }

        try
        {
            var json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<BotConfiguration>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            });

            if (config == null)
            {
                throw new InvalidOperationException("Configuration file is empty or invalid.");
            }

            return ValidateConfiguration(config);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse configuration file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Searches for default configuration file in current directory and parent directories.
    /// </summary>
    private static string? FindDefaultConfigFile()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var dir = new DirectoryInfo(currentDir);

        while (dir != null)
        {
            var configPath = Path.Combine(dir.FullName, DefaultConfigFileName);
            if (File.Exists(configPath))
            {
                return configPath;
            }

            dir = dir.Parent;
        }

        return null;
    }

    /// <summary>
    /// Returns default configuration settings.
    /// </summary>
    public static BotConfiguration GetDefaultConfiguration()
    {
        return new BotConfiguration(
            UpdatePolicy: UpdatePolicy.Minor,
            ExcludePackages: new List<string>(),
            IncludePrerelease: false,
            MaxParallelism: 4,
            UpdateRules: new List<UpdateRule>());
    }

    /// <summary>
    /// Validates configuration settings.
    /// </summary>
    private static BotConfiguration ValidateConfiguration(BotConfiguration config)
    {
        if (config.MaxParallelism < 1 || config.MaxParallelism > 16)
        {
            throw new InvalidOperationException(
                $"MaxParallelism must be between 1 and 16. Got: {config.MaxParallelism}");
        }

        // Ensure lists are not null
        var excludePackages = config.ExcludePackages ?? new List<string>();
        var updateRules = config.UpdateRules ?? new List<UpdateRule>();

        return config with
        {
            ExcludePackages = excludePackages,
            UpdateRules = updateRules
        };
    }

    /// <summary>
    /// Saves configuration to a file.
    /// </summary>
    public void SaveConfiguration(BotConfiguration config, string outputPath)
    {
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        File.WriteAllText(outputPath, json);
        Console.WriteLine($"Configuration saved to: {outputPath}");
    }

    /// <summary>
    /// Creates a sample configuration file with comments.
    /// </summary>
    public void CreateSampleConfig(string outputPath)
    {
        var sample = @"{
  // Default update policy: Patch, Minor, or Major
  ""updatePolicy"": ""Minor"",

  // Package patterns to exclude from updates (supports wildcards)
  ""excludePackages"": [
    ""System.*"",
    ""Microsoft.NETCore.App""
  ],

  // Include pre-release versions
  ""includePrerelease"": false,

  // Maximum number of parallel NuGet API calls (1-16)
  ""maxParallelism"": 4,

  // Package-specific update rules
  ""updateRules"": [
    {
      ""pattern"": ""Microsoft.*"",
      ""policy"": ""Minor""
    },
    {
      ""pattern"": ""Newtonsoft.Json"",
      ""policy"": ""Patch""
    }
  ]
}";

        File.WriteAllText(outputPath, sample);
        Console.WriteLine($"Sample configuration created: {outputPath}");
    }
}
