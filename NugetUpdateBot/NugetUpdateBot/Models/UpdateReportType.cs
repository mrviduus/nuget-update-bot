namespace NugetUpdateBot.Models;

/// <summary>
/// Type of update report being generated.
/// </summary>
public enum UpdateReportType
{
    /// <summary>
    /// Preview of potential updates (dry-run)
    /// </summary>
    Preview,

    /// <summary>
    /// Report of updates that were applied
    /// </summary>
    Applied
}
