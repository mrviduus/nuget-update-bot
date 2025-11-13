using NuGet.Common;
using NugetUpdateBot.Configuration;
using NugetUpdateBot.Models;
using NugetUpdateBot.Reporting;
using NugetUpdateBot.Services;
using NugetUpdateBot.Validation;

namespace NugetUpdateBot.Handlers;

/// <summary>
/// Handler for the report command.
/// Orchestrates report generation and output.
/// </summary>
public class ReportCommandHandler
{
    private readonly InputValidatorService _validator;
    private readonly PackageScannerService _scanner;
    private readonly PolicyEngineService _policyEngine;
    private readonly ConfigurationLoaderService _configLoader;
    private readonly ReportGeneratorService _reportGenerator;
    private readonly ConsoleReportFormatter _consoleFormatter;
    private readonly ILogger _logger;

    public ReportCommandHandler(
        InputValidatorService validator,
        PackageScannerService scanner,
        PolicyEngineService policyEngine,
        ConfigurationLoaderService configLoader,
        ReportGeneratorService reportGenerator,
        ConsoleReportFormatter consoleFormatter,
        ILogger logger)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
        _policyEngine = policyEngine ?? throw new ArgumentNullException(nameof(policyEngine));
        _configLoader = configLoader ?? throw new ArgumentNullException(nameof(configLoader));
        _reportGenerator = reportGenerator ?? throw new ArgumentNullException(nameof(reportGenerator));
        _consoleFormatter = consoleFormatter ?? throw new ArgumentNullException(nameof(consoleFormatter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes the report command.
    /// </summary>
    public async Task<int> HandleAsync(
        string projectPath,
        string? configPath,
        string? outputPath,
        OutputFormat format,
        bool includePrerelease,
        bool noCache,
        bool verbose,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Resolve and validate project path
            var resolvedPath = _validator.ResolveProjectPath(projectPath);
            Console.WriteLine($"Generating report for: {Path.GetFileName(resolvedPath)}");

            // Validate output path if specified
            if (outputPath != null)
            {
                _validator.ValidateOutputPath(outputPath);
            }

            // Load configuration
            var config = _configLoader.LoadConfiguration(configPath);

            // Override config with command-line options
            if (includePrerelease && !config.IncludePrerelease)
            {
                config = config with { IncludePrerelease = true };
            }

            // Parse project file
            var packages = _scanner.ParseProjectFile(resolvedPath);

            // Check for updates
            var updates = await _scanner.CheckPackagesAsync(
                packages,
                config.IncludePrerelease,
                noCache,
                verbose,
                cancellationToken);

            // Apply policies and filters
            var filteredUpdates = _policyEngine.ApplyPolicies(
                updates,
                config.UpdatePolicy,
                config.ExcludePackages.ToArray());

            // Generate report
            var report = _reportGenerator.GenerateReport(
                resolvedPath,
                filteredUpdates,
                UpdateReportType.Preview);

            // Output report based on format
            switch (format)
            {
                case OutputFormat.Console:
                    _consoleFormatter.DisplayReport(report);
                    ConsoleReportFormatter.DisplayDeprecatedWarning(filteredUpdates);
                    break;

                case OutputFormat.Json:
                    if (string.IsNullOrEmpty(outputPath))
                    {
                        // Output JSON to console
                        var json = _reportGenerator.ToJson(report);
                        Console.WriteLine(json);
                    }
                    else
                    {
                        // Save to file
                        _reportGenerator.SaveToFile(report, outputPath, format);
                    }
                    break;

                default:
                    throw new ArgumentException($"Unsupported output format: {format}");
            }

            return 0;
        }
        catch (FileNotFoundException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 2;
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 3;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unexpected error: {ex.Message}");
            if (verbose)
            {
                Console.Error.WriteLine(ex.StackTrace);
            }
            return 4;
        }
    }
}
