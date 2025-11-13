namespace NugetUpdateBot.Models;

/// <summary>
/// Policy for determining which package updates to apply.
/// </summary>
public enum UpdatePolicy
{
    /// <summary>
    /// Only apply patch updates (bug fixes)
    /// </summary>
    Patch,

    /// <summary>
    /// Apply minor and patch updates (new features, backward compatible)
    /// </summary>
    Minor,

    /// <summary>
    /// Apply all updates including breaking changes
    /// </summary>
    Major
}
