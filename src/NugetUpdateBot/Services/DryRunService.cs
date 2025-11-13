using NugetUpdateBot.Models;

namespace NugetUpdateBot.Services;

/// <summary>
/// Service responsible for dry-run (preview) functionality.
/// Follows Single Responsibility Principle - only handles preview operations.
/// </summary>
public class DryRunService
{
    /// <summary>
    /// Previews updates without applying them.
    /// </summary>
    public void PreviewUpdates(List<UpdateCandidate> updates, string projectPath)
    {
        if (updates.Count == 0)
        {
            Console.WriteLine("No updates to apply.");
            return;
        }

        Console.WriteLine($"\n[DRY RUN] Preview of updates for: {Path.GetFileName(projectPath)}");
        Console.WriteLine("The following changes would be made:");
        Console.WriteLine(new string('=', 80));

        foreach (var update in updates)
        {
            var latest = update.LatestPrereleaseVersion ?? update.LatestStableVersion;
            CompareBeforeAfter(update.PackageId, update.CurrentVersion.ToString(), latest.ToString());
        }

        Console.WriteLine(new string('=', 80));
        Console.WriteLine($"\nTotal updates: {updates.Count}");
        Console.WriteLine("\nNo changes were made. Run without --dry-run to apply these updates.");
    }

    /// <summary>
    /// Displays before/after comparison for a package update.
    /// </summary>
    public static void CompareBeforeAfter(string packageName, string oldVersion, string newVersion)
    {
        Console.WriteLine($"\n{packageName}:");
        Console.WriteLine($"  Current: {oldVersion}");
        Console.WriteLine($"  New:     {newVersion}");
    }
}
