using System.Text.Json;
using NugetUpdateBot.Models;

namespace NugetUpdateBot.Reporting;

/// <summary>
/// Service responsible for generating update reports in various formats.
/// Follows Single Responsibility Principle - only handles report generation.
/// </summary>
public class ReportGeneratorService
{
    private readonly SummaryCalculatorService _summaryCalculator;

    public ReportGeneratorService(SummaryCalculatorService summaryCalculator)
    {
        _summaryCalculator = summaryCalculator ?? throw new ArgumentNullException(nameof(summaryCalculator));
    }

    /// <summary>
    /// Generates a complete update report.
    /// </summary>
    public UpdateReport GenerateReport(
        string projectPath,
        List<UpdateCandidate> updates,
        UpdateReportType type)
    {
        var summary = _summaryCalculator.CalculateSummary(updates, 0);

        return new UpdateReport(
            GeneratedAt: DateTime.UtcNow,
            ProjectPath: projectPath,
            Updates: updates,
            Type: type,
            Summary: summary);
    }

    /// <summary>
    /// Serializes report to JSON format.
    /// </summary>
    public string ToJson(UpdateReport report)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(report, options);
    }

    /// <summary>
    /// Saves report to a file.
    /// </summary>
    public void SaveToFile(UpdateReport report, string outputPath, OutputFormat format)
    {
        string content = format switch
        {
            OutputFormat.Json => ToJson(report),
            OutputFormat.Console => throw new InvalidOperationException(
                "Console format cannot be saved to file. Use ConsoleReportFormatter instead."),
            _ => throw new ArgumentException($"Unsupported format: {format}", nameof(format))
        };

        File.WriteAllText(outputPath, content);
        Console.WriteLine($"\nReport saved to: {outputPath}");
    }
}
