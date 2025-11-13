using Xunit;

namespace NugetUpdateBot.Tests.UnitTests;

public class RuleMatchingTests
{
    [Theory]
    [InlineData("Microsoft.Extensions.Logging", "Microsoft.*", true)]
    [InlineData("Microsoft.AspNetCore.Mvc", "Microsoft.*", true)]
    [InlineData("Newtonsoft.Json", "Microsoft.*", false)]
    [InlineData("System.Text.Json", "System.*", true)]
    public void MatchesPattern_BasicWildcard_MatchesCorrectly(string packageName, string pattern, bool expected)
    {
        // Act
        var result = RuleMatcher.MatchesPattern(packageName, pattern);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Microsoft.Extensions.Logging", "Microsoft.Extensions.*", true)]
    [InlineData("Microsoft.Extensions.DependencyInjection", "Microsoft.Extensions.*", true)]
    [InlineData("Microsoft.AspNetCore.Mvc", "Microsoft.Extensions.*", false)]
    [InlineData("Microsoft.Logging", "Microsoft.Extensions.*", false)]
    public void MatchesPattern_NestedWildcard_MatchesCorrectly(string packageName, string pattern, bool expected)
    {
        // Act
        var result = RuleMatcher.MatchesPattern(packageName, pattern);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Newtonsoft.Json", "Newtonsoft.Json", true)]
    [InlineData("Newtonsoft.Json", "Newtonsoft.Json.Bson", false)]
    [InlineData("Serilog", "Serilog", true)]
    [InlineData("Serilog.Sinks.Console", "Serilog", false)]
    public void MatchesPattern_ExactMatch_MatchesCorrectly(string packageName, string pattern, bool expected)
    {
        // Act
        var result = RuleMatcher.MatchesPattern(packageName, pattern);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Microsoft.Extensions.Logging.Abstractions", "*.Abstractions", true)]
    [InlineData("System.Text.Json.Abstractions", "*.Abstractions", true)]
    [InlineData("Microsoft.Extensions.Logging", "*.Abstractions", false)]
    public void MatchesPattern_SuffixWildcard_MatchesCorrectly(string packageName, string pattern, bool expected)
    {
        // Act
        var result = RuleMatcher.MatchesPattern(packageName, pattern);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FindMatchingRule_NoRules_ReturnsNull()
    {
        // Arrange
        var packageName = "Microsoft.Extensions.Logging";
        var rules = new List<UpdateRule>();

        // Act
        var result = RuleMatcher.FindMatchingRule(packageName, rules);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FindMatchingRule_MultipleRules_ReturnsFirstMatch()
    {
        // Arrange
        var packageName = "Microsoft.Extensions.Logging";
        var rules = new List<UpdateRule>
        {
            new UpdateRule("Microsoft.*", UpdatePolicy.Minor),
            new UpdateRule("Microsoft.Extensions.*", UpdatePolicy.Patch), // More specific, but comes second
            new UpdateRule("System.*", UpdatePolicy.Major)
        };

        // Act
        var result = RuleMatcher.FindMatchingRule(packageName, rules);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Microsoft.*", result.Pattern);
        Assert.Equal(UpdatePolicy.Minor, result.Policy);
    }

    [Fact]
    public void FindMatchingRule_NoMatch_ReturnsNull()
    {
        // Arrange
        var packageName = "Newtonsoft.Json";
        var rules = new List<UpdateRule>
        {
            new UpdateRule("Microsoft.*", UpdatePolicy.Minor),
            new UpdateRule("System.*", UpdatePolicy.Patch)
        };

        // Act
        var result = RuleMatcher.FindMatchingRule(packageName, rules);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetEffectivePolicy_WithMatchingRule_ReturnsRulePolicy()
    {
        // Arrange
        var packageName = "Microsoft.Extensions.Logging";
        var rules = new List<UpdateRule>
        {
            new UpdateRule("Microsoft.*", UpdatePolicy.Minor)
        };
        var defaultPolicy = UpdatePolicy.Major;

        // Act
        var policy = RuleMatcher.GetEffectivePolicy(packageName, rules, defaultPolicy);

        // Assert
        Assert.Equal(UpdatePolicy.Minor, policy);
    }

    [Fact]
    public void GetEffectivePolicy_NoMatchingRule_ReturnsDefaultPolicy()
    {
        // Arrange
        var packageName = "Newtonsoft.Json";
        var rules = new List<UpdateRule>
        {
            new UpdateRule("Microsoft.*", UpdatePolicy.Minor)
        };
        var defaultPolicy = UpdatePolicy.Major;

        // Act
        var policy = RuleMatcher.GetEffectivePolicy(packageName, rules, defaultPolicy);

        // Assert
        Assert.Equal(UpdatePolicy.Major, policy);
    }

    [Theory]
    [InlineData("Microsoft.Extensions.Logging", "microsoft.*", true)] // Case insensitive
    [InlineData("MICROSOFT.EXTENSIONS.LOGGING", "Microsoft.*", true)] // Case insensitive
    [InlineData("microsoft.extensions.logging", "MICROSOFT.*", true)] // Case insensitive
    public void MatchesPattern_CaseInsensitive_MatchesCorrectly(string packageName, string pattern, bool expected)
    {
        // Act
        var result = RuleMatcher.MatchesPattern(packageName, pattern);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsPackageExcluded_InExclusionList_ReturnsTrue()
    {
        // Arrange
        var packageName = "Newtonsoft.Json";
        var excludeList = new List<string> { "Newtonsoft.Json", "Microsoft.EntityFrameworkCore" };

        // Act
        var result = RuleMatcher.IsPackageExcluded(packageName, excludeList);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsPackageExcluded_NotInExclusionList_ReturnsFalse()
    {
        // Arrange
        var packageName = "Serilog";
        var excludeList = new List<string> { "Newtonsoft.Json", "Microsoft.EntityFrameworkCore" };

        // Act
        var result = RuleMatcher.IsPackageExcluded(packageName, excludeList);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsPackageExcluded_EmptyList_ReturnsFalse()
    {
        // Arrange
        var packageName = "Serilog";
        var excludeList = new List<string>();

        // Act
        var result = RuleMatcher.IsPackageExcluded(packageName, excludeList);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsPackageExcluded_PatternInList_MatchesWildcard()
    {
        // Arrange
        var packageName = "Microsoft.Extensions.Logging";
        var excludeList = new List<string> { "Microsoft.*", "Newtonsoft.Json" };

        // Act
        var result = RuleMatcher.IsPackageExcluded(packageName, excludeList);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("Microsoft.Extensions.Logging", "Microsoft.Extensions.*", true)]
    [InlineData("Microsoft.Extensions.Logging.Abstractions", "Microsoft.Extensions.Logging.*", true)]
    [InlineData("Microsoft.Extensions.Logging.Console", "Microsoft.Extensions.Logging.*", true)]
    [InlineData("Microsoft.AspNetCore.Mvc", "Microsoft.Extensions.Logging.*", false)]
    public void MatchesPattern_DeepWildcard_MatchesCorrectly(string packageName, string pattern, bool expected)
    {
        // Act
        var result = RuleMatcher.MatchesPattern(packageName, pattern);

        // Assert
        Assert.Equal(expected, result);
    }
}
