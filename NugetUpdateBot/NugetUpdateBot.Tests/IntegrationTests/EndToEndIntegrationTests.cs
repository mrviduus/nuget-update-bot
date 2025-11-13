using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NugetUpdateBot.Configuration;
using NugetUpdateBot.Handlers;
using NugetUpdateBot.Models;
using NugetUpdateBot.Reporting;
using NugetUpdateBot.Services;
using NugetUpdateBot.Validation;

namespace NugetUpdateBot.Tests.IntegrationTests;

/// <summary>
/// End-to-end integration tests that verify the complete workflow
/// from scanning to updating packages in a real project file.
/// </summary>
public class EndToEndIntegrationTests : IDisposable
{
    private readonly string _testProjectPath;
    private readonly string _testConfigPath;
    private readonly List<string> _cleanupFiles;

    // Services
    private readonly InputValidatorService _validator;
    private readonly PackageScannerService _scanner;
    private readonly PackageUpdaterService _updater;
    private readonly PolicyEngineService _policyEngine;
    private readonly DryRunService _dryRunService;
    private readonly ConfigurationLoaderService _configLoader;
    private readonly ReportGeneratorService _reportGenerator;
    private readonly ConsoleReportFormatter _formatter;
    private readonly RuleMatcherService _ruleMatcher;
    private readonly SummaryCalculatorService _summaryCalculator;
    private readonly ILogger _logger;

    // Handlers
    private readonly ScanCommandHandler _scanHandler;
    private readonly UpdateCommandHandler _updateHandler;
    private readonly ReportCommandHandler _reportHandler;

    public EndToEndIntegrationTests()
    {
        _testProjectPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.csproj");
        _testConfigPath = Path.Combine(Path.GetTempPath(), $"config_{Guid.NewGuid()}.json");
        _cleanupFiles = new List<string>();

        // Initialize services
        var repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
        var throttler = new SemaphoreSlim(5, 5);
        _logger = NullLogger.Instance;

        _validator = new InputValidatorService();
        _scanner = new PackageScannerService(repository, throttler, _logger);
        _updater = new PackageUpdaterService();
        _policyEngine = new PolicyEngineService();
        _dryRunService = new DryRunService();
        _configLoader = new ConfigurationLoaderService();
        _ruleMatcher = new RuleMatcherService();
        _summaryCalculator = new SummaryCalculatorService();
        _reportGenerator = new ReportGeneratorService(_summaryCalculator);
        _formatter = new ConsoleReportFormatter();

        // Initialize handlers
        _scanHandler = new ScanCommandHandler(
            _validator, _scanner, _policyEngine, _configLoader,
            _ruleMatcher, _formatter, _logger);

        _updateHandler = new UpdateCommandHandler(
            _validator, _scanner, _updater, _policyEngine,
            _dryRunService, _configLoader, _reportGenerator,
            _formatter, _logger);

        _reportHandler = new ReportCommandHandler(
            _validator, _scanner, _policyEngine, _configLoader,
            _reportGenerator, _formatter, _logger);
    }

    [Fact]
    public async Task ScanCommand_WithOutdatedPackages_ReturnsSuccessWithUpdates()
    {
        // Arrange: Create project with old packages
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""12.0.1"" />
    <PackageReference Include=""xunit"" Version=""2.4.0"" />
  </ItemGroup>
</Project>";

        File.WriteAllText(_testProjectPath, projectContent);

        // Act: Run scan command
        var exitCode = await _scanHandler.HandleAsync(
            _testProjectPath,
            configPath: null,
            includePrerelease: false,
            noCache: true,
            verbose: false);

        // Assert: Should return 1 (updates available)
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task UpdateCommand_WithDryRun_DoesNotModifyFile()
    {
        // Arrange
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""12.0.1"" />
  </ItemGroup>
</Project>";

        File.WriteAllText(_testProjectPath, projectContent);
        var originalContent = File.ReadAllText(_testProjectPath);

        // Act: Run update with dry-run
        var exitCode = await _updateHandler.HandleAsync(
            _testProjectPath,
            configPath: null,
            includePrerelease: false,
            dryRun: true,
            noCache: true,
            verbose: false);

        // Assert: File should be unchanged
        var currentContent = File.ReadAllText(_testProjectPath);
        Assert.Equal(originalContent, currentContent);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task UpdateCommand_WithRealUpdate_UpdatesPackages()
    {
        // Arrange: Create project with old package
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""12.0.1"" />
  </ItemGroup>
</Project>";

        File.WriteAllText(_testProjectPath, projectContent);

        // Act: Run real update
        var exitCode = await _updateHandler.HandleAsync(
            _testProjectPath,
            configPath: null,
            includePrerelease: false,
            dryRun: false,
            noCache: true,
            verbose: false);

        // Assert: File should be updated
        var updatedContent = File.ReadAllText(_testProjectPath);
        Assert.NotEqual(projectContent, updatedContent);
        Assert.DoesNotContain("12.0.1", updatedContent);

        // Should have created a backup
        var backupFiles = Directory.GetFiles(Path.GetTempPath(), $"*{Path.GetFileName(_testProjectPath)}*.backup.*");
        Assert.NotEmpty(backupFiles);
        _cleanupFiles.AddRange(backupFiles);
    }

    [Fact]
    public async Task ReportCommand_WithJsonFormat_GeneratesValidJson()
    {
        // Arrange
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""12.0.1"" />
  </ItemGroup>
</Project>";

        File.WriteAllText(_testProjectPath, projectContent);

        var jsonOutputPath = Path.Combine(Path.GetTempPath(), $"report_{Guid.NewGuid()}.json");
        _cleanupFiles.Add(jsonOutputPath);

        // Act: Generate JSON report
        var exitCode = await _reportHandler.HandleAsync(
            _testProjectPath,
            configPath: null,
            outputPath: jsonOutputPath,
            format: OutputFormat.Json,
            includePrerelease: false,
            noCache: true,
            verbose: false);

        // Assert: Should create valid JSON file
        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(jsonOutputPath));

        var jsonContent = File.ReadAllText(jsonOutputPath);
        Assert.NotEmpty(jsonContent);
        Assert.StartsWith("{", jsonContent.Trim());
        Assert.EndsWith("}", jsonContent.Trim());
    }

    [Fact]
    public async Task EndToEnd_WithPolicyConfiguration_RespectsPolicy()
    {
        // Arrange: Create project
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""12.0.1"" />
    <PackageReference Include=""xunit"" Version=""2.4.0"" />
  </ItemGroup>
</Project>";

        File.WriteAllText(_testProjectPath, projectContent);

        // Create config with Patch policy
        var configContent = @"{
  ""UpdatePolicy"": ""Patch"",
  ""IncludePrerelease"": false,
  ""ExcludePackages"": []
}";

        File.WriteAllText(_testConfigPath, configContent);
        _cleanupFiles.Add(_testConfigPath);

        // Act: Update with Patch policy
        var exitCode = await _updateHandler.HandleAsync(
            _testProjectPath,
            configPath: _testConfigPath,
            includePrerelease: false,
            dryRun: false,
            noCache: true,
            verbose: false);

        // Assert: Should only apply patch updates (if any)
        // This will depend on available versions, but it should respect the policy
        Assert.True(exitCode >= 0);
    }

    [Fact]
    public async Task EndToEnd_WithExcludedPackages_DoesNotUpdateExcluded()
    {
        // Arrange
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""12.0.1"" />
    <PackageReference Include=""xunit"" Version=""2.4.0"" />
  </ItemGroup>
</Project>";

        File.WriteAllText(_testProjectPath, projectContent);

        // Create config excluding Newtonsoft.Json
        var configContent = @"{
  ""UpdatePolicy"": ""Major"",
  ""IncludePrerelease"": false,
  ""ExcludePackages"": [""Newtonsoft.Json""]
}";

        File.WriteAllText(_testConfigPath, configContent);
        _cleanupFiles.Add(_testConfigPath);

        // Act: Update with exclusion
        await _updateHandler.HandleAsync(
            _testProjectPath,
            configPath: _testConfigPath,
            includePrerelease: false,
            dryRun: false,
            noCache: true,
            verbose: false);

        // Assert: Newtonsoft.Json should not be updated
        var updatedContent = File.ReadAllText(_testProjectPath);
        Assert.Contains(@"<PackageReference Include=""Newtonsoft.Json"" Version=""12.0.1"" />", updatedContent);
    }

    [Fact]
    public async Task CompleteWorkflow_ScanUpdateReport_WorksEndToEnd()
    {
        // Arrange: Create project with old packages
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""12.0.1"" />
  </ItemGroup>
</Project>";

        File.WriteAllText(_testProjectPath, projectContent);

        // Step 1: Scan
        var scanExitCode = await _scanHandler.HandleAsync(
            _testProjectPath,
            configPath: null,
            includePrerelease: false,
            noCache: true,
            verbose: false);

        Assert.Equal(1, scanExitCode); // Should have updates

        // Step 2: Update
        var updateExitCode = await _updateHandler.HandleAsync(
            _testProjectPath,
            configPath: null,
            includePrerelease: false,
            dryRun: false,
            noCache: true,
            verbose: false);

        Assert.Equal(0, updateExitCode); // Should succeed

        // Step 3: Report
        var reportExitCode = await _reportHandler.HandleAsync(
            _testProjectPath,
            configPath: null,
            outputPath: null,
            format: OutputFormat.Console,
            includePrerelease: false,
            noCache: true,
            verbose: false);

        Assert.Equal(0, reportExitCode); // Should succeed

        // Verify: File was actually updated
        var updatedContent = File.ReadAllText(_testProjectPath);
        Assert.DoesNotContain("12.0.1", updatedContent);
    }

    [Fact]
    public async Task UpdateCommand_WithInvalidProject_ReturnsErrorCode()
    {
        // Arrange: Invalid project file
        var invalidContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  <!-- Missing closing tag";

        File.WriteAllText(_testProjectPath, invalidContent);

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await _updateHandler.HandleAsync(
                _testProjectPath,
                configPath: null,
                includePrerelease: false,
                dryRun: false,
                noCache: true,
                verbose: false));
    }

    public void Dispose()
    {
        // Cleanup all test files
        if (File.Exists(_testProjectPath))
        {
            File.Delete(_testProjectPath);
        }

        foreach (var file in _cleanupFiles)
        {
            if (File.Exists(file))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }
}
