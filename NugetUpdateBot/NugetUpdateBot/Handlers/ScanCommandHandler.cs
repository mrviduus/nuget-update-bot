using NuGet.Common;
using NugetUpdateBot.Configuration;
using NugetUpdateBot.Models;
using NugetUpdateBot.Reporting;
using NugetUpdateBot.Services;
using NugetUpdateBot.Validation;

namespace NugetUpdateBot.Handlers;

/// <summary>
/// Handler for the scan command.
/// Orchestrates package scanning and reporting.
/// </summary>
public class ScanCommandHandler
{
    private readonly InputValidatorService _validator;
    private readonly PackageScannerService _scanner;
    private readonly PolicyEngineService _policyEngine;
    private readonly ConfigurationLoaderService _configLoader;
    private readonly RuleMatcherService _ruleMatcher;
    private readonly ConsoleReportFormatter _formatter;
    private readonly ILogger _logger;

    public ScanCommandHandler(
        InputValidatorService validator,
        PackageScannerService scanner,
        PolicyEngineService policyEngine,
        ConfigurationLoaderService configLoader,
        RuleMatcherService ruleMatcher,
        ConsoleReportFormatter formatter,
        ILogger logger)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
        _policyEngine = policyEngine ?? throw new ArgumentNullException(nameof(policyEngine));
        _configLoader = configLoader ?? throw new ArgumentNullException(nameof(configLoader));
        _ruleMatcher = ruleMatcher ?? throw new ArgumentNullException(nameof(ruleMatcher));
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes the scan command.
    /// </summary>
    public async Task<int> HandleAsync(
        string projectPath,
        string? configPath,
        bool includePrerelease,
        bool noCache,
        bool verbose,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Resolve and validate project path
            var resolvedPath = _validator.ResolveProjectPath(projectPath);
            Console.WriteLine($"Scanning project: {Path.GetFileName(resolvedPath)}");

            // Load configuration
            var config = _configLoader.LoadConfiguration(configPath);

            // Override config with command-line options if specified
            if (includePrerelease && !config.IncludePrerelease)
            {
                config = config with { IncludePrerelease = true };
            }

            // Parse project file
            var packages = _scanner.ParseProjectFile(resolvedPath);
            Console.WriteLine($"Found {packages.Count} package references");

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

            // Display results
            _scanner.DisplayScanResults(filteredUpdates);

            // Show deprecated warnings
            ConsoleReportFormatter.DisplayDeprecatedWarning(filteredUpdates);

            return filteredUpdates.Count > 0 ? 1 : 0;
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
