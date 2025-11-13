namespace NugetUpdateBot.Configuration;

/// <summary>
/// Represents an update rule for specific package patterns.
/// </summary>
/// <param name="Pattern">Package name pattern (supports wildcards like "Microsoft.*")</param>
/// <param name="Policy">Update policy to apply to matching packages</param>
public record UpdateRule(
    string Pattern,
    Models.UpdatePolicy Policy);
