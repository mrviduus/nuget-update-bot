using System.CommandLine;
using System.Runtime.CompilerServices;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NugetUpdateBot.Configuration;
using NugetUpdateBot.Handlers;
using NugetUpdateBot.Models;
using NugetUpdateBot.Reporting;
using NugetUpdateBot.Services;
using NugetUpdateBot.Validation;

[assembly: InternalsVisibleTo("NugetUpdateBot.Tests")]

// Initialize dependencies
var repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
var logger = NullLogger.Instance;
var throttler = new SemaphoreSlim(5, 5);

// Initialize services
var validator = new InputValidatorService();
var scanner = new PackageScannerService(repository, throttler, logger);
var updater = new PackageUpdaterService();
var policyEngine = new PolicyEngineService();
var dryRunService = new DryRunService();
var configLoader = new ConfigurationLoaderService();
var ruleMatcher = new RuleMatcherService();
var summaryCalculator = new SummaryCalculatorService();
var reportGenerator = new ReportGeneratorService(summaryCalculator);
var consoleFormatter = new ConsoleReportFormatter();

// Initialize command handlers
var scanHandler = new ScanCommandHandler(
    validator,
    scanner,
    policyEngine,
    configLoader,
    ruleMatcher,
    consoleFormatter,
    logger);

var updateHandler = new UpdateCommandHandler(
    validator,
    scanner,
    updater,
    policyEngine,
    dryRunService,
    configLoader,
    reportGenerator,
    consoleFormatter,
    logger);

var reportHandler = new ReportCommandHandler(
    validator,
    scanner,
    policyEngine,
    configLoader,
    reportGenerator,
    consoleFormatter,
    logger);

// Main command setup
var rootCommand = new RootCommand("NuGet Update Bot - Automatically manage NuGet package updates in your .NET projects");

// Scan command
var scanCommand = new Command("scan", @"Scan a .NET project for outdated NuGet packages.

This command analyzes your project file (.csproj) and checks each NuGet package
against the latest available versions on NuGet.org. It reports which packages
have newer versions available and categorizes updates as Major, Minor, or Patch.

Examples:
  nuget-update-bot scan --project MyApp.csproj
  nuget-update-bot scan -p MyApp.csproj --include-prerelease
  nuget-update-bot scan -p MyApp.csproj -v --no-cache");

var scanProjectOption = new Option<string>("--project", "-p") { Description = "Path to .csproj file or directory", IsRequired = true };
var scanConfigOption = new Option<string?>("--config", "-c") { Description = "Path to configuration file" };
var scanPrereleaseOption = new Option<bool>("--include-prerelease") { Description = "Include pre-release versions" };
var scanVerboseOption = new Option<bool>("--verbose", "-v") { Description = "Enable verbose output" };
var scanNoCacheOption = new Option<bool>("--no-cache") { Description = "Bypass cache and fetch fresh data" };

scanCommand.AddOption(scanProjectOption);
scanCommand.AddOption(scanConfigOption);
scanCommand.AddOption(scanPrereleaseOption);
scanCommand.AddOption(scanVerboseOption);
scanCommand.AddOption(scanNoCacheOption);

scanCommand.SetHandler(async (string project, string? config, bool includePrerelease, bool verbose, bool noCache) =>
{
    Environment.ExitCode = await scanHandler.HandleAsync(project, config, includePrerelease, noCache, verbose);
}, scanProjectOption, scanConfigOption, scanPrereleaseOption, scanVerboseOption, scanNoCacheOption);

rootCommand.AddCommand(scanCommand);

// Update command
var updateCommand = new Command("update", @"Update outdated NuGet packages in a .NET project.

This command modifies your project file (.csproj) to update package versions.
You can control which updates to apply using configuration or the --dry-run option
to preview changes without modifying files.

Examples:
  nuget-update-bot update --project MyApp.csproj --dry-run
  nuget-update-bot update -p MyApp.csproj --config .nuget-update-bot.json
  nuget-update-bot update -p MyApp.csproj --include-prerelease -v");

var updateProjectOption = new Option<string>("--project", "-p") { Description = "Path to .csproj file or directory", IsRequired = true };
var updateConfigOption = new Option<string?>("--config", "-c") { Description = "Path to configuration file" };
var updateDryRunOption = new Option<bool>("--dry-run") { Description = "Preview updates without applying them" };
var updatePrereleaseOption = new Option<bool>("--include-prerelease") { Description = "Include pre-release versions" };
var updateNoCacheOption = new Option<bool>("--no-cache") { Description = "Bypass cache and fetch fresh data" };
var updateVerboseOption = new Option<bool>("--verbose", "-v") { Description = "Enable verbose output" };

updateCommand.AddOption(updateProjectOption);
updateCommand.AddOption(updateConfigOption);
updateCommand.AddOption(updateDryRunOption);
updateCommand.AddOption(updatePrereleaseOption);
updateCommand.AddOption(updateNoCacheOption);
updateCommand.AddOption(updateVerboseOption);

updateCommand.SetHandler(async (string project, string? config, bool dryRun, bool includePrerelease, bool noCache, bool verbose) =>
{
    Environment.ExitCode = await updateHandler.HandleAsync(project, config, includePrerelease, dryRun, noCache, verbose);
}, updateProjectOption, updateConfigOption, updateDryRunOption, updatePrereleaseOption, updateNoCacheOption, updateVerboseOption);

rootCommand.AddCommand(updateCommand);

// Report command
var reportCommand = new Command("report", @"Generate a report of outdated NuGet packages.

This command analyzes your project and generates a detailed report of available
package updates. Reports can be formatted as console tables or JSON for integration
with CI/CD pipelines and other automation tools.

Output formats:
  - Console: Human-readable table with package details (default)
  - Json: Structured data for automation and reporting tools

Examples:
  nuget-update-bot report --project MyApp.csproj
  nuget-update-bot report -p MyApp.csproj --format Json --output report.json
  nuget-update-bot report -p MyApp.csproj --format Json -v");

var reportProjectOption = new Option<string>("--project", "-p") { Description = "Path to .csproj file or directory", IsRequired = true };
var reportConfigOption = new Option<string?>("--config", "-c") { Description = "Path to configuration file" };
var reportFormatOption = new Option<OutputFormat>("--format", () => OutputFormat.Console) { Description = "Output format: Console or Json" };
var reportOutputOption = new Option<string?>("--output", "-o") { Description = "Output file path (for JSON format)" };
var reportPrereleaseOption = new Option<bool>("--include-prerelease") { Description = "Include pre-release versions" };
var reportNoCacheOption = new Option<bool>("--no-cache") { Description = "Bypass cache and fetch fresh data" };
var reportVerboseOption = new Option<bool>("--verbose", "-v") { Description = "Enable verbose output" };

reportCommand.AddOption(reportProjectOption);
reportCommand.AddOption(reportConfigOption);
reportCommand.AddOption(reportFormatOption);
reportCommand.AddOption(reportOutputOption);
reportCommand.AddOption(reportPrereleaseOption);
reportCommand.AddOption(reportNoCacheOption);
reportCommand.AddOption(reportVerboseOption);

reportCommand.SetHandler(async (string project, string? config, OutputFormat format, string? output, bool includePrerelease, bool noCache, bool verbose) =>
{
    Environment.ExitCode = await reportHandler.HandleAsync(project, config, output, format, includePrerelease, noCache, verbose);
}, reportProjectOption, reportConfigOption, reportFormatOption, reportOutputOption, reportPrereleaseOption, reportNoCacheOption, reportVerboseOption);

rootCommand.AddCommand(reportCommand);

// Execute
return await rootCommand.InvokeAsync(args);
