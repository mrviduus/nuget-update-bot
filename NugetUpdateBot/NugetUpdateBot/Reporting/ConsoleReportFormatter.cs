using NugetUpdateBot.Models;

namespace NugetUpdateBot.Reporting;

/// <summary>
/// Service responsible for formatting reports for console output.
/// Follows Single Responsibility Principle - only handles console formatting.
/// </summary>
public class ConsoleReportFormatter
{
    /// <summary>
    /// Displays a formatted update report in the console.
    /// </summary>
    public void DisplayReport(UpdateReport report)
    {
        Console.WriteLine($"\n{new string('=', 80)}");
        Console.WriteLine($"UPDATE REPORT - {report.Type}");
        Console.WriteLine($"Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC");
        Console.WriteLine($"Project: {Path.GetFileName(report.ProjectPath)}");
        Console.WriteLine($"{new string('=', 80)}\n");

        if (report.Updates.Count == 0)
        {
            Console.WriteLine("No updates available.");
            return;
        }

        DisplayUpdateTable(report.Updates);
        DisplaySummary(report.Summary);
    }

    /// <summary>
    /// Displays updates in a formatted table.
    /// </summary>
    private static void DisplayUpdateTable(List<UpdateCandidate> updates)
    {
        const int packageWidth = 40;
        const int versionWidth = 15;
        const int typeWidth = 12;

        // Header
        Console.WriteLine(
            $"{"Package",-packageWidth} {"Current",-versionWidth} {"Latest",-versionWidth} {"Type",-typeWidth}");
        Console.WriteLine(new string('-', packageWidth + versionWidth + versionWidth + typeWidth + 3));

        // Rows
        foreach (var update in updates)
        {
            var latest = update.LatestPrereleaseVersion ?? update.LatestStableVersion;
            var packageName = update.PackageId.Length > packageWidth - 3
                ? update.PackageId.Substring(0, packageWidth - 3) + "..."
                : update.PackageId;

            var typeDisplay = update.UpdateType.ToString();
            if (update.IsDeprecated)
            {
                typeDisplay += " [DEPRECATED]";
            }

            Console.WriteLine(
                $"{packageName,-packageWidth} {update.CurrentVersion.ToString(),-versionWidth} {latest.ToString(),-versionWidth} {typeDisplay,-typeWidth}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Displays summary statistics.
    /// </summary>
    public static void DisplaySummary(UpdateSummary summary)
    {
        Console.WriteLine($"\n{new string('-', 80)}");
        Console.WriteLine("SUMMARY");
        Console.WriteLine($"{new string('-', 80)}");
        Console.WriteLine($"Total packages:     {summary.TotalPackages}");
        Console.WriteLine($"Outdated packages:  {summary.OutdatedCount}");
        Console.WriteLine($"  - Major updates:  {summary.MajorUpdates}");
        Console.WriteLine($"  - Minor updates:  {summary.MinorUpdates}");
        Console.WriteLine($"  - Patch updates:  {summary.PatchUpdates}");
        Console.WriteLine($"Excluded packages:  {summary.ExcludedCount}");
        Console.WriteLine($"{new string('=', 80)}\n");
    }

    /// <summary>
    /// Displays deprecated packages warning.
    /// </summary>
    public static void DisplayDeprecatedWarning(List<UpdateCandidate> updates)
    {
        var deprecated = updates.Where(u => u.IsDeprecated).ToList();
        if (deprecated.Count == 0) return;

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n⚠️  WARNING: {deprecated.Count} deprecated package(s) found:");
        foreach (var pkg in deprecated)
        {
            Console.WriteLine($"  - {pkg.PackageId}");
        }
        Console.ResetColor();
    }
}
