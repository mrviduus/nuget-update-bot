# Tasks: NuGet Update Bot

**Input**: Design documents from `/specs/001-nuget-update-bot/`
**Prerequisites**: plan.md, spec.md, data-model.md, contracts/, research.md

**Note**: Tests are included following TDD principles as required by the constitution.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [X] T001 Create NugetUpdateBot project structure per implementation plan
- [X] T002 [P] Create NugetUpdateBot.csproj with .NET 8 target and required packages
- [X] T003 [P] Create NugetUpdateBot.Tests.csproj with xUnit and Moq references
- [X] T004 [P] Add .editorconfig for code style consistency
- [X] T005 [P] Create .gitignore for .NET projects
- [X] T006 Setup Program.cs with top-level statements and minimal structure

## Phase 2: Foundational (Core Infrastructure)

**Purpose**: Shared components needed by all user stories

- [X] T007 Define core records in Program.cs (PackageReference, UpdateCandidate, UpdateRule)
- [X] T008 Define enums in Program.cs (UpdateType, UpdatePolicy, OutputFormat)
- [X] T009 Implement NuGet repository connection setup using NuGet.Protocol
- [X] T010 Create System.CommandLine root command structure with help text
- [X] T011 Implement error handling constants and exit codes
- [X] T012 Create configuration record (BotConfiguration) with default values
- [X] T013 Implement configuration loading from JSON file and environment variables
- [X] T014 Add SemaphoreSlim(5) throttling for parallel API calls
- [X] T015 Setup SourceCacheContext for API response caching

## Phase 3: User Story 1 - Check for Outdated NuGet Packages (P1)

**Goal**: Enable scanning .NET projects for outdated packages
**Independent Test**: Run scan command on test project, verify correct package identification

### Tests for User Story 1

- [X] T016 [P] [US1] Write unit test for project file parsing in NugetUpdateBot.Tests/UnitTests/PackageScannerTests.cs
- [X] T017 [P] [US1] Write unit test for version comparison logic in NugetUpdateBot.Tests/UnitTests/VersionComparerTests.cs
- [X] T018 [P] [US1] Write integration test for NuGet API queries in NugetUpdateBot.Tests/IntegrationTests/NuGetApiTests.cs
- [X] T019 [P] [US1] Write test for scan command output formatting in NugetUpdateBot.Tests/UnitTests/OutputFormatterTests.cs

### Implementation for User Story 1

- [X] T020 [US1] Implement project file XML parsing using System.Xml.Linq in Program.cs
- [X] T021 [US1] Extract PackageReference entries from project file in Program.cs
- [X] T022 [US1] Implement GetAllVersionsAsync method using FindPackageByIdResource in Program.cs
- [X] T023 [US1] Create version comparison logic using NuGetVersion in Program.cs
- [X] T024 [US1] Implement CheckPackageAsync method with throttling in Program.cs
- [X] T025 [US1] Create scan command with options (--project, --include-prerelease, --verbose, --no-cache) in Program.cs
- [X] T026 [US1] Implement console output formatter for scan results in Program.cs
- [X] T027 [US1] Add error handling for file not found and network errors in Program.cs
- [X] T028 [US1] Connect scan command handler to package checking logic in Program.cs
- [X] T029 [US1] Verify all US1 tests pass and scan command works end-to-end

## Phase 4: User Story 2 - Update Package Versions Automatically (P2)

**Goal**: Enable automatic updating of outdated packages
**Independent Test**: Run update command with dry-run, then actual update, verify file modifications

### Tests for User Story 2

- [X] T030 [P] [US2] Write unit test for project file modification in NugetUpdateBot.Tests/UnitTests/UpdateServiceTests.cs
- [X] T031 [P] [US2] Write test for update policy application in NugetUpdateBot.Tests/UnitTests/UpdatePolicyTests.cs
- [X] T032 [P] [US2] Write test for dry-run mode in NugetUpdateBot.Tests/UnitTests/DryRunTests.cs
- [X] T033 [P] [US2] Write integration test for file operations in NugetUpdateBot.Tests/IntegrationTests/FileOperationTests.cs

### Implementation for User Story 2

- [X] T034 [US2] Implement project file backup mechanism before modifications in Program.cs
- [X] T035 [US2] Create UpdatePackageVersion method preserving XML formatting in Program.cs
- [X] T036 [US2] Implement update policy logic (Major/Minor/Patch restrictions) in Program.cs
- [X] T037 [US2] Add package exclusion filtering logic in Program.cs
- [X] T038 [US2] Create update command with options (--dry-run, --exclude, --policy) in Program.cs
- [X] T039 [US2] Implement dry-run preview functionality in Program.cs
- [X] T040 [US2] Add project file validation after modifications in Program.cs
- [X] T041 [US2] Connect update command handler to modification logic in Program.cs
- [X] T042 [US2] Verify all US2 tests pass and update command works with dry-run

## Phase 5: User Story 3 - Generate Update Reports (P3)

**Goal**: Enable report generation for package updates
**Independent Test**: Generate report in both console and JSON formats, verify accuracy

### Tests for User Story 3

- [ ] T043 [P] [US3] Write test for JSON report generation in NugetUpdateBot.Tests/UnitTests/ReportGeneratorTests.cs
- [ ] T044 [P] [US3] Write test for console report formatting in NugetUpdateBot.Tests/UnitTests/ConsoleReportTests.cs
- [ ] T045 [P] [US3] Write test for UpdateSummary calculations in NugetUpdateBot.Tests/UnitTests/SummaryTests.cs

### Implementation for User Story 3

- [ ] T046 [US3] Create UpdateReport and UpdateSummary records in Program.cs
- [ ] T047 [US3] Implement report data collection from scan results in Program.cs
- [ ] T048 [US3] Add JSON serialization using System.Text.Json in Program.cs
- [ ] T049 [US3] Create console report formatter with table layout in Program.cs
- [ ] T050 [US3] Add report command with options (--format, --output, --include-up-to-date) in Program.cs
- [ ] T051 [US3] Implement file output option for reports in Program.cs
- [ ] T052 [US3] Connect report command handler to formatting logic in Program.cs
- [ ] T053 [US3] Verify all US3 tests pass and reports generate correctly

## Phase 6: User Story 4 - Configure Update Rules (P3)

**Goal**: Enable configuration of update rules and exclusions
**Independent Test**: Apply various rules via config file, verify bot respects them

### Tests for User Story 4

- [ ] T054 [P] [US4] Write test for configuration file loading in NugetUpdateBot.Tests/UnitTests/ConfigurationTests.cs
- [ ] T055 [P] [US4] Write test for rule pattern matching in NugetUpdateBot.Tests/UnitTests/RuleMatchingTests.cs
- [ ] T056 [P] [US4] Write test for environment variable overrides in NugetUpdateBot.Tests/UnitTests/EnvironmentConfigTests.cs

### Implementation for User Story 4

- [ ] T057 [US4] Implement JSON configuration file parsing using Microsoft.Extensions.Configuration in Program.cs
- [ ] T058 [US4] Add environment variable configuration support (NUGET_BOT_* prefix) in Program.cs
- [ ] T059 [US4] Create rule pattern matching logic using glob patterns in Program.cs
- [ ] T060 [US4] Implement configuration priority (CLI > env > file > defaults) in Program.cs
- [ ] T061 [US4] Apply update rules during scan and update operations in Program.cs
- [ ] T062 [US4] Add configuration validation and error messages in Program.cs
- [ ] T063 [US4] Verify all US4 tests pass and configuration is respected

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final improvements and documentation

- [ ] T064 Add comprehensive help text for all commands and options in Program.cs
- [ ] T065 [P] Implement verbose logging throughout the application in Program.cs
- [ ] T066 [P] Add performance timing for operations over 1 second in Program.cs
- [ ] T067 [P] Create example appsettings.json with all configuration options
- [ ] T068 [P] Update quickstart.md with actual command examples from implementation
- [ ] T069 Add input validation for all user inputs in Program.cs
- [ ] T070 Verify all edge cases are handled with appropriate error messages
- [ ] T071 Run full end-to-end test suite and fix any remaining issues
- [ ] T072 Ensure code follows .editorconfig rules and passes all analyzers

## Dependencies

### User Story Dependencies
```
US1 (Scan) → Independent (can be implemented first)
US2 (Update) → Depends on US1 (needs scan results)
US3 (Report) → Depends on US1 (needs scan data)
US4 (Config) → Can run parallel with US3 (enhances US1 & US2)
```

### Parallel Execution Opportunities

**Phase 1 (Setup)**: T002-T005 can run in parallel
**Phase 3 (US1 Tests)**: T016-T019 can run in parallel
**Phase 4 (US2 Tests)**: T030-T033 can run in parallel
**Phase 5 (US3 Tests)**: T043-T045 can run in parallel
**Phase 6 (US4 Tests)**: T054-T056 can run in parallel
**Phase 7 (Polish)**: T065-T068 can run in parallel

## Implementation Strategy

### MVP Scope (Minimum Viable Product)
Complete Phase 1-3 (Setup + Foundational + User Story 1) for basic scanning functionality.
This provides immediate value - users can identify outdated packages.

### Incremental Delivery
1. **First Release**: User Story 1 (Scan) - Core value proposition
2. **Second Release**: User Story 2 (Update) - Automation capability
3. **Third Release**: User Stories 3 & 4 (Report + Config) - Enterprise features

### Testing Approach
Following TDD principles from the constitution:
1. Write tests first (red phase)
2. Implement to pass tests (green phase)
3. Refactor while keeping tests passing (refactor phase)

## Total Task Count: 72

### Breakdown by Phase
- Phase 1 (Setup): 6 tasks
- Phase 2 (Foundational): 9 tasks
- Phase 3 (US1): 14 tasks (4 tests, 10 implementation)
- Phase 4 (US2): 13 tasks (4 tests, 9 implementation)
- Phase 5 (US3): 11 tasks (3 tests, 8 implementation)
- Phase 6 (US4): 10 tasks (3 tests, 7 implementation)
- Phase 7 (Polish): 9 tasks

### Parallel Opportunities: 19 tasks
- Setup: 4 parallel tasks
- US1 Tests: 4 parallel tasks
- US2 Tests: 4 parallel tasks
- US3 Tests: 3 parallel tasks
- US4 Tests: 3 parallel tasks
- Polish: 4 parallel tasks

### Independent Testing per User Story
- US1: Fully testable after T029 - scan command complete
- US2: Fully testable after T042 - update command complete
- US3: Fully testable after T053 - report command complete
- US4: Fully testable after T063 - configuration complete