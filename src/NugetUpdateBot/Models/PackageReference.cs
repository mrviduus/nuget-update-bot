using NuGet.Versioning;

namespace NugetUpdateBot.Models;

/// <summary>
/// Represents a NuGet package reference in a project file.
/// </summary>
/// <param name="Name">Package name/ID</param>
/// <param name="Version">Current version installed</param>
/// <param name="TargetFramework">Optional target framework</param>
public record PackageReference(
    string Name,
    NuGetVersion Version,
    string? TargetFramework = null);
