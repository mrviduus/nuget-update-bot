using NugetUpdateBot.Models;

namespace NugetUpdateBot.Reporting;

/// <summary>
/// Service responsible for calculating update statistics.
/// Follows Single Responsibility Principle - only handles summary calculations.
/// </summary>
public class SummaryCalculatorService
{
    /// <summary>
    /// Calculates summary statistics for a list of updates.
    /// </summary>
    /// <param name="updates">List of update candidates</param>
    /// <param name="excludedCount">Number of excluded packages</param>
    /// <returns>Summary statistics</returns>
    public UpdateSummary CalculateSummary(List<UpdateCandidate> updates, int excludedCount)
    {
        var majorCount = updates.Count(u => u.UpdateType == UpdateType.Major);
        var minorCount = updates.Count(u => u.UpdateType == UpdateType.Minor);
        var patchCount = updates.Count(u => u.UpdateType == UpdateType.Patch);

        return new UpdateSummary(
            TotalPackages: updates.Count + excludedCount,
            OutdatedCount: updates.Count,
            MajorUpdates: majorCount,
            MinorUpdates: minorCount,
            PatchUpdates: patchCount,
            ExcludedCount: excludedCount);
    }

    /// <summary>
    /// Groups updates by type.
    /// </summary>
    public Dictionary<UpdateType, List<UpdateCandidate>> GroupByType(List<UpdateCandidate> updates)
    {
        return updates.GroupBy(u => u.UpdateType)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    /// <summary>
    /// Calculates the total number of packages that need updates.
    /// </summary>
    public int CountOutdated(List<UpdateCandidate> updates)
    {
        return updates.Count;
    }

    /// <summary>
    /// Checks if any major breaking changes are present.
    /// </summary>
    public bool HasMajorUpdates(List<UpdateCandidate> updates)
    {
        return updates.Any(u => u.UpdateType == UpdateType.Major);
    }

    /// <summary>
    /// Checks if any deprecated packages are present.
    /// </summary>
    public bool HasDeprecatedPackages(List<UpdateCandidate> updates)
    {
        return updates.Any(u => u.IsDeprecated);
    }

    /// <summary>
    /// Gets the list of deprecated packages.
    /// </summary>
    public List<UpdateCandidate> GetDeprecatedPackages(List<UpdateCandidate> updates)
    {
        return updates.Where(u => u.IsDeprecated).ToList();
    }
}
