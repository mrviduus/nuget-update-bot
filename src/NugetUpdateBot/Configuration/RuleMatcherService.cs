using System.Text.RegularExpressions;
using NugetUpdateBot.Models;

namespace NugetUpdateBot.Configuration;

/// <summary>
/// Service responsible for matching packages against update rules.
/// Follows Single Responsibility Principle - only handles rule matching logic.
/// </summary>
public class RuleMatcherService
{
    /// <summary>
    /// Finds the matching update rule for a package.
    /// Returns the first matching rule or null if no match found.
    /// </summary>
    /// <param name="packageId">Package identifier to match</param>
    /// <param name="rules">List of update rules to check against</param>
    /// <returns>Matching rule or null</returns>
    public UpdateRule? FindMatchingRule(string packageId, List<UpdateRule> rules)
    {
        if (string.IsNullOrWhiteSpace(packageId))
        {
            throw new ArgumentException("Package ID cannot be null or whitespace.", nameof(packageId));
        }

        if (rules == null || rules.Count == 0)
        {
            return null;
        }

        // Rules are evaluated in order - first match wins
        foreach (var rule in rules)
        {
            if (IsMatch(packageId, rule.Pattern))
            {
                return rule;
            }
        }

        return null;
    }

    /// <summary>
    /// Determines the effective update policy for a package.
    /// Checks rules first, falls back to default policy.
    /// </summary>
    /// <param name="packageId">Package identifier</param>
    /// <param name="rules">List of update rules</param>
    /// <param name="defaultPolicy">Default policy if no rule matches</param>
    /// <returns>Effective update policy</returns>
    public UpdatePolicy GetEffectivePolicy(
        string packageId,
        List<UpdateRule> rules,
        UpdatePolicy defaultPolicy)
    {
        var matchingRule = FindMatchingRule(packageId, rules);
        return matchingRule?.Policy ?? defaultPolicy;
    }

    /// <summary>
    /// Checks if a package ID matches a pattern.
    /// Supports wildcards: * (any characters) and ? (single character).
    /// </summary>
    /// <param name="packageId">Package identifier to test</param>
    /// <param name="pattern">Pattern with optional wildcards</param>
    /// <returns>True if package matches pattern</returns>
    public static bool IsMatch(string packageId, string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return false;
        }

        // Exact match (case-insensitive)
        if (string.Equals(packageId, pattern, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Convert wildcard pattern to regex
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";

        return Regex.IsMatch(packageId, regexPattern, RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Gets all packages that match a specific pattern.
    /// </summary>
    /// <param name="packageIds">List of package identifiers</param>
    /// <param name="pattern">Pattern to match against</param>
    /// <returns>List of matching package IDs</returns>
    public List<string> GetMatchingPackages(List<string> packageIds, string pattern)
    {
        if (packageIds == null || packageIds.Count == 0)
        {
            return new List<string>();
        }

        return packageIds.Where(id => IsMatch(id, pattern)).ToList();
    }

    /// <summary>
    /// Groups packages by their effective update policy.
    /// </summary>
    /// <param name="packageIds">List of package identifiers</param>
    /// <param name="rules">Update rules to apply</param>
    /// <param name="defaultPolicy">Default policy for packages without matching rules</param>
    /// <returns>Dictionary mapping policies to package lists</returns>
    public Dictionary<UpdatePolicy, List<string>> GroupByPolicy(
        List<string> packageIds,
        List<UpdateRule> rules,
        UpdatePolicy defaultPolicy)
    {
        var result = new Dictionary<UpdatePolicy, List<string>>
        {
            { UpdatePolicy.Patch, new List<string>() },
            { UpdatePolicy.Minor, new List<string>() },
            { UpdatePolicy.Major, new List<string>() }
        };

        foreach (var packageId in packageIds)
        {
            var policy = GetEffectivePolicy(packageId, rules, defaultPolicy);
            result[policy].Add(packageId);
        }

        return result;
    }
}
