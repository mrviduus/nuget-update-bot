using NuGet.Versioning;
using NugetUpdateBot.Models;

namespace NugetUpdateBot.Services;

/// <summary>
/// Service responsible for applying update policies to packages.
/// Follows Single Responsibility Principle - only handles policy logic.
/// </summary>
public class PolicyEngineService
{
    /// <summary>
    /// Determines if a package should be updated based on the policy.
    /// </summary>
    public bool ShouldUpdate(NuGetVersion current, NuGetVersion latest, UpdatePolicy policy)
    {
        var updateType = DetermineUpdateType(current, latest);

        return policy switch
        {
            UpdatePolicy.Patch => updateType == UpdateType.Patch,
            UpdatePolicy.Minor => updateType is UpdateType.Patch or UpdateType.Minor,
            UpdatePolicy.Major => true,
            _ => false
        };
    }

    /// <summary>
    /// Filters updates based on the update policy.
    /// </summary>
    public List<UpdateCandidate> FilterUpdatesByPolicy(List<UpdateCandidate> updates, UpdatePolicy policy)
    {
        return updates.Where(u =>
        {
            return policy switch
            {
                UpdatePolicy.Patch => u.UpdateType == UpdateType.Patch,
                UpdatePolicy.Minor => u.UpdateType is UpdateType.Patch or UpdateType.Minor,
                UpdatePolicy.Major => true,
                _ => false
            };
        }).ToList();
    }

    /// <summary>
    /// Applies update policies and exclusions to filter the update list.
    /// </summary>
    public List<UpdateCandidate> ApplyPolicies(
        List<UpdateCandidate> updates,
        UpdatePolicy policy,
        string[] excludePatterns)
    {
        var filtered = FilterUpdatesByPolicy(updates, policy);

        if (excludePatterns.Length > 0)
        {
            filtered = filtered.Where(u => !IsExcluded(u.PackageId, excludePatterns)).ToList();
        }

        return filtered;
    }

    /// <summary>
    /// Checks if a package matches any exclusion pattern.
    /// </summary>
    private static bool IsExcluded(string packageName, string[] patterns)
    {
        return patterns.Any(pattern =>
            packageName.Equals(pattern, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Determines the update type between two versions.
    /// </summary>
    private static UpdateType DetermineUpdateType(NuGetVersion current, NuGetVersion latest)
    {
        if (latest.IsPrerelease && !current.IsPrerelease)
        {
            return UpdateType.Prerelease;
        }

        if (latest.Major > current.Major)
        {
            return UpdateType.Major;
        }

        if (latest.Minor > current.Minor)
        {
            return UpdateType.Minor;
        }

        return UpdateType.Patch;
    }
}
