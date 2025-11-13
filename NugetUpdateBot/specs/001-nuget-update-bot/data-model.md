# Data Model: NuGet Update Bot

**Feature**: NuGet Update Bot
**Date**: 2025-11-13
**Source**: Extracted from spec.md functional requirements

## Core Entities

### 1. PackageReference

**Purpose**: Represents a NuGet package dependency in a project file

```csharp
record PackageReference(
    string Name,
    NuGetVersion Version,
    string? TargetFramework = null
)
```

**Fields**:
- `Name`: Package identifier (e.g., "Newtonsoft.Json")
- `Version`: Current version using NuGetVersion type
- `TargetFramework`: Optional target framework (e.g., "net8.0")

**Validation Rules**:
- Name must not be null or empty
- Name must be valid NuGet package identifier (alphanumeric, dots, hyphens)
- Version must be valid semantic version
- TargetFramework if present must be valid TFM

**Relationships**:
- Belongs to ProjectContext (many-to-one)
- Maps to UpdateCandidate when updates available (one-to-one)

---

### 2. UpdateCandidate

**Purpose**: Represents a package that has newer versions available

```csharp
record UpdateCandidate(
    string PackageId,
    NuGetVersion CurrentVersion,
    NuGetVersion LatestStableVersion,
    NuGetVersion? LatestPrereleaseVersion,
    UpdateType UpdateType,
    bool IsDeprecated = false
)
```

**Fields**:
- `PackageId`: Package identifier matching PackageReference.Name
- `CurrentVersion`: Version currently in use
- `LatestStableVersion`: Latest stable version available
- `LatestPrereleaseVersion`: Latest prerelease if newer than stable
- `UpdateType`: Categorization of update (Major/Minor/Patch)
- `IsDeprecated`: Whether current version is deprecated

**Validation Rules**:
- LatestStableVersion must be greater than CurrentVersion
- LatestPrereleaseVersion if present must be greater than LatestStableVersion
- UpdateType must match version difference calculation

**State Transitions**:
- Created when scan finds newer version
- Consumed when update is applied
- Discarded when excluded by rules

---

### 3. UpdateRule

**Purpose**: Defines constraints for package updates

```csharp
record UpdateRule(
    string? PackagePattern,
    UpdatePolicy Policy,
    bool AllowPrerelease = false,
    string? MaxVersion = null
)
```

**Fields**:
- `PackagePattern`: Glob pattern for package names (null = all packages)
- `Policy`: Maximum update level allowed (Major/Minor/Patch)
- `AllowPrerelease`: Whether to consider prerelease versions
- `MaxVersion`: Maximum version constraint (e.g., "<5.0.0")

**Validation Rules**:
- PackagePattern if present must be valid glob pattern
- MaxVersion if present must be valid version range specification
- Policy must be valid enum value

**Examples**:
```csharp
new UpdateRule("Microsoft.*", UpdatePolicy.Minor, false, null)  // Microsoft packages, minor updates only
new UpdateRule("*", UpdatePolicy.Patch, false, null)           // All packages, patches only
new UpdateRule("Experimental.*", UpdatePolicy.Major, true, null) // Experimental packages, all updates including prerelease
```

---

### 4. ProjectContext

**Purpose**: Represents a .NET project file with its package references

```csharp
record ProjectContext(
    string FilePath,
    string ProjectName,
    List<PackageReference> Packages,
    string? TargetFramework,
    ProjectType Type
)
```

**Fields**:
- `FilePath`: Absolute path to .csproj file
- `ProjectName`: Extracted project name
- `Packages`: List of package references
- `TargetFramework`: Primary target framework
- `Type`: Project type classification

**Validation Rules**:
- FilePath must exist and be readable
- FilePath must have .csproj/.fsproj/.vbproj extension
- Packages list can be empty but not null
- XML must be well-formed

**Relationships**:
- Contains multiple PackageReference (one-to-many)
- Part of SolutionContext if solution-level scan (many-to-one)

---

### 5. UpdateReport

**Purpose**: Contains summary of all proposed or applied package updates

```csharp
record UpdateReport(
    DateTime GeneratedAt,
    string ProjectPath,
    List<UpdateCandidate> Updates,
    UpdateReportType Type,
    UpdateSummary Summary
)

record UpdateSummary(
    int TotalPackages,
    int OutdatedCount,
    int MajorUpdates,
    int MinorUpdates,
    int PatchUpdates,
    int ExcludedCount
)
```

**Fields**:
- `GeneratedAt`: UTC timestamp of report generation
- `ProjectPath`: Path to project or solution
- `Updates`: List of update candidates
- `Type`: Report type (Preview/Applied)
- `Summary`: Statistical summary

**Validation Rules**:
- GeneratedAt must be UTC
- Updates list can be empty (all up-to-date)
- Summary counts must match Updates list

**State Transitions**:
- Preview → Applied when updates are executed
- Can be serialized to JSON or formatted for console

---

## Enumerations

### UpdateType
```csharp
enum UpdateType
{
    Patch,      // 1.0.0 → 1.0.1
    Minor,      // 1.0.0 → 1.1.0
    Major,      // 1.0.0 → 2.0.0
    Prerelease  // 1.0.0 → 1.1.0-beta
}
```

### UpdatePolicy
```csharp
enum UpdatePolicy
{
    Patch,  // Only patch updates
    Minor,  // Patch and minor updates
    Major   // All updates including major
}
```

### ProjectType
```csharp
enum ProjectType
{
    Console,
    ClassLibrary,
    Web,
    Test,
    Unknown
}
```

### UpdateReportType
```csharp
enum UpdateReportType
{
    Preview,  // Dry-run results
    Applied   // Actual updates performed
}
```

## Configuration Entities

### BotConfiguration

**Purpose**: Application-wide configuration settings

```csharp
record BotConfiguration(
    string[] ExcludedPackages,
    UpdatePolicy DefaultPolicy,
    bool IncludePrerelease,
    int TimeoutSeconds,
    OutputFormat OutputFormat,
    List<UpdateRule> Rules
)
```

**Default Values**:
```csharp
new BotConfiguration(
    ExcludedPackages: Array.Empty<string>(),
    DefaultPolicy: UpdatePolicy.Minor,
    IncludePrerelease: false,
    TimeoutSeconds: 30,
    OutputFormat: OutputFormat.Console,
    Rules: new List<UpdateRule>()
)
```

---

## Entity Relationships Diagram

```
ProjectContext (1) ──contains──> (*) PackageReference
                                          │
                                          ├──maps-to──> (0..1) UpdateCandidate
                                          │
UpdateRule (*) ──applies-to──> (*) UpdateCandidate
                                          │
UpdateReport (1) ──contains──> (*) UpdateCandidate
                 ──references──> (1) ProjectContext
```

## JSON Serialization Schemas

### UpdateCandidate JSON
```json
{
  "packageId": "Newtonsoft.Json",
  "currentVersion": "12.0.3",
  "latestStableVersion": "13.0.3",
  "latestPrereleaseVersion": null,
  "updateType": "major",
  "isDeprecated": false
}
```

### UpdateReport JSON
```json
{
  "generatedAt": "2025-11-13T10:00:00Z",
  "projectPath": "/path/to/project.csproj",
  "updates": [...],
  "type": "preview",
  "summary": {
    "totalPackages": 15,
    "outdatedCount": 3,
    "majorUpdates": 1,
    "minorUpdates": 1,
    "patchUpdates": 1,
    "excludedCount": 0
  }
}
```

### BotConfiguration JSON
```json
{
  "excludedPackages": ["Legacy.Package"],
  "defaultPolicy": "minor",
  "includePrerelease": false,
  "timeoutSeconds": 30,
  "outputFormat": "console",
  "rules": [
    {
      "packagePattern": "Microsoft.*",
      "policy": "patch",
      "allowPrerelease": false,
      "maxVersion": null
    }
  ]
}
```

## Validation Summary

All entities include validation to ensure:
1. **Data Integrity**: Required fields are present and valid
2. **Business Rules**: Version comparisons and policies are enforced
3. **File System**: Paths exist and are accessible
4. **Network Data**: Package names and versions from NuGet are validated
5. **Configuration**: User settings are within acceptable ranges

This data model provides a complete foundation for the NuGet Update Bot implementation while maintaining the single-file architecture principle through the use of C# records.