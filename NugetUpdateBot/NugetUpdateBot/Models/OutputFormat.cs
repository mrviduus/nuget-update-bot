namespace NugetUpdateBot.Models;

/// <summary>
/// Output format for reports.
/// </summary>
public enum OutputFormat
{
    /// <summary>
    /// Human-readable console output
    /// </summary>
    Console,

    /// <summary>
    /// JSON format for automation and CI/CD integration
    /// </summary>
    Json
}
