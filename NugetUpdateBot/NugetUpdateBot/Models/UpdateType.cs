namespace NugetUpdateBot.Models;

/// <summary>
/// Represents the type of update available for a package.
/// </summary>
public enum UpdateType
{
    /// <summary>
    /// Patch update (bug fixes, no new features) - e.g., 1.0.0 → 1.0.1
    /// </summary>
    Patch,

    /// <summary>
    /// Minor update (new features, backward compatible) - e.g., 1.0.0 → 1.1.0
    /// </summary>
    Minor,

    /// <summary>
    /// Major update (breaking changes) - e.g., 1.0.0 → 2.0.0
    /// </summary>
    Major,

    /// <summary>
    /// Pre-release version available
    /// </summary>
    Prerelease
}
