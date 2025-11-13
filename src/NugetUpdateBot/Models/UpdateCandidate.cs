using NuGet.Versioning;

namespace NugetUpdateBot.Models;

/// <summary>
/// Represents a package that has an update available.
/// </summary>
/// <param name="PackageId">Package identifier</param>
/// <param name="CurrentVersion">Currently installed version</param>
/// <param name="LatestStableVersion">Latest stable version available</param>
/// <param name="LatestPrereleaseVersion">Latest pre-release version (if any)</param>
/// <param name="UpdateType">Type of update (Major/Minor/Patch/Prerelease)</param>
/// <param name="IsDeprecated">Whether the package is deprecated</param>
public record UpdateCandidate(
    string PackageId,
    NuGetVersion CurrentVersion,
    NuGetVersion LatestStableVersion,
    NuGetVersion? LatestPrereleaseVersion,
    UpdateType UpdateType,
    bool IsDeprecated = false);
