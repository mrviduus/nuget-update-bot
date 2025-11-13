using NuGet.Common;
using NugetUpdateBot.Configuration;
using NugetUpdateBot.Models;
using NugetUpdateBot.Reporting;
using NugetUpdateBot.Services;
using NugetUpdateBot.Validation;

namespace NugetUpdateBot.Handlers;

/// <summary>
/// Handler for the update command.
/// Orchestrates package updates with backup and rollback support.
/// </summary>
public class UpdateCommandHandler
{
    private readonly InputValidatorService _validator;
    private readonly PackageScannerService _scanner;
    private readonly PackageUpdaterService _updater;
    private readonly PolicyEngineService _policyEngine;
    private readonly DryRunService _dryRunService;
    private readonly ConfigurationLoaderService _configLoader;
    private readonly ReportGeneratorService _reportGenerator;
    private readonly ConsoleReportFormatter _formatter;
    private readonly ILogger _logger;

    public UpdateCommandHandler(
        InputValidatorService validator,
        PackageScannerService scanner,
        PackageUpdaterService updater,
        PolicyEngineService policyEngine,
        DryRunService dryRunService,
        ConfigurationLoaderService configLoader,
        ReportGeneratorService reportGenerator,
        ConsoleReportFormatter formatter,
        ILogger logger)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
        _updater = updater ?? throw new ArgumentNullException(nameof(updater));
        _policyEngine = policyEngine ?? throw new ArgumentNullException(nameof(policyEngine));
        _dryRunService = dryRunService ?? throw new ArgumentNullException(nameof(dryRunService));
        _configLoader = configLoader ?? throw new ArgumentNullException(nameof(configLoader));
        _reportGenerator = reportGenerator ?? throw new ArgumentNullException(nameof(reportGenerator));
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes the update command.
    /// </summary>
    public async Task<int> HandleAsync(
        string projectPath,
        string? configPath,
        bool includePrerelease,
        bool dryRun,
        bool noCache,
        bool verbose,
        CancellationToken cancellationToken = default)
    {
        string? backupPath = null;

        try
        {
            // Resolve and validate project path
            var resolvedPath = _validator.ResolveProjectPath(projectPath);
            Console.WriteLine($"Processing project: {Path.GetFileName(resolvedPath)}");

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

            if (filteredUpdates.Count == 0)
            {
                Console.WriteLine("No updates to apply.");
                return 0;
            }

            // Dry run mode - preview only
            if (dryRun)
            {
                _dryRunService.PreviewUpdates(filteredUpdates, resolvedPath);
                return 0;
            }

            // Create backup before making changes
            Console.WriteLine("\nCreating backup...");
            backupPath = _updater.CreateBackup(resolvedPath);

            // Apply updates
            Console.WriteLine($"\nApplying {filteredUpdates.Count} update(s)...");
            var successCount = 0;

            foreach (var update in filteredUpdates)
            {
                try
                {
                    var newVersion = (update.LatestPrereleaseVersion ?? update.LatestStableVersion).ToString();
                    _updater.UpdatePackageVersion(resolvedPath, update.PackageId, newVersion);

                    Console.WriteLine($"✓ Updated {update.PackageId} to {newVersion}");
                    successCount++;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"✗ Failed to update {update.PackageId}: {ex.Message}");
                }
            }

            // Validate the updated project file
            if (!_updater.ValidateProjectFile(resolvedPath))
            {
                Console.Error.WriteLine("\nProject file validation failed. Restoring from backup...");
                if (backupPath != null)
                {
                    _updater.RestoreFromBackup(resolvedPath, backupPath);
                }
                return 5;
            }

            Console.WriteLine($"\n✓ Successfully updated {successCount} of {filteredUpdates.Count} package(s)");
            Console.WriteLine($"Backup saved at: {backupPath}");

            // Display summary
            var report = _reportGenerator.GenerateReport(resolvedPath, filteredUpdates, UpdateReportType.Applied);
            ConsoleReportFormatter.DisplaySummary(report.Summary);

            return successCount == filteredUpdates.Count ? 0 : 6;
        }
        catch (FileNotFoundException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 2;
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");

            // Attempt rollback if backup exists
            if (backupPath != null && File.Exists(backupPath))
            {
                try
                {
                    var resolvedPath = _validator.ResolveProjectPath(projectPath);
                    _updater.RestoreFromBackup(resolvedPath, backupPath);
                    Console.WriteLine("Changes rolled back successfully.");
                }
                catch
                {
                    Console.Error.WriteLine("Failed to rollback changes. Backup location: " + backupPath);
                }
            }

            return 3;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unexpected error: {ex.Message}");
            if (verbose)
            {
                Console.Error.WriteLine(ex.StackTrace);
            }

            // Attempt rollback
            if (backupPath != null && File.Exists(backupPath))
            {
                try
                {
                    var resolvedPath = _validator.ResolveProjectPath(projectPath);
                    _updater.RestoreFromBackup(resolvedPath, backupPath);
                    Console.WriteLine("Changes rolled back successfully.");
                }
                catch
                {
                    Console.Error.WriteLine("Failed to rollback changes. Backup location: " + backupPath);
                }
            }

            return 4;
        }
    }
}
