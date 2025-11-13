namespace NugetUpdateBot.Configuration;

/// <summary>
/// Configuration settings for the NuGet Update Bot.
/// </summary>
/// <param name="UpdatePolicy">Default update policy for packages</param>
/// <param name="ExcludePackages">List of package patterns to exclude from updates</param>
/// <param name="IncludePrerelease">Whether to include pre-release versions</param>
/// <param name="MaxParallelism">Maximum number of parallel NuGet API calls</param>
/// <param name="UpdateRules">Package-specific update rules</param>
public record BotConfiguration(
    Models.UpdatePolicy UpdatePolicy,
    List<string> ExcludePackages,
    bool IncludePrerelease,
    int MaxParallelism,
    List<UpdateRule> UpdateRules);
