namespace NugetUpdateBot.Models;

/// <summary>
/// Summary statistics for package updates.
/// </summary>
/// <param name="TotalPackages">Total number of packages in project</param>
/// <param name="OutdatedCount">Number of packages with updates available</param>
/// <param name="MajorUpdates">Number of major version updates</param>
/// <param name="MinorUpdates">Number of minor version updates</param>
/// <param name="PatchUpdates">Number of patch version updates</param>
/// <param name="ExcludedCount">Number of packages excluded from updates</param>
public record UpdateSummary(
    int TotalPackages,
    int OutdatedCount,
    int MajorUpdates,
    int MinorUpdates,
    int PatchUpdates,
    int ExcludedCount);
