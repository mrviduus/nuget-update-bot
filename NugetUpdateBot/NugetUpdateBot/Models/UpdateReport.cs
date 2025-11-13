namespace NugetUpdateBot.Models;

/// <summary>
/// Represents a complete update report.
/// </summary>
/// <param name="GeneratedAt">Timestamp when report was generated</param>
/// <param name="ProjectPath">Path to the project file</param>
/// <param name="Updates">List of available updates</param>
/// <param name="Type">Type of report (Preview or Applied)</param>
/// <param name="Summary">Summary statistics</param>
public record UpdateReport(
    DateTime GeneratedAt,
    string ProjectPath,
    List<UpdateCandidate> Updates,
    UpdateReportType Type,
    UpdateSummary Summary);
