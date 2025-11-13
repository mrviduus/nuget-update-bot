using System.Xml.Linq;

namespace NugetUpdateBot.Services;

/// <summary>
/// Service responsible for updating package versions in project files.
/// Supports both traditional PackageReference and Central Package Management (CPM).
/// Follows Single Responsibility Principle - only handles package update operations.
/// </summary>
public class PackageUpdaterService
{
    /// <summary>
    /// Updates a package version in the project file or Directory.Packages.props.
    /// Automatically detects if the project uses Central Package Management.
    /// </summary>
    public void UpdatePackageVersion(string projectPath, string packageName, string newVersion)
    {
        try
        {
            // Check if project uses Central Package Management
            if (UsesCentralPackageManagement(projectPath))
            {
                // Find and update Directory.Packages.props
                var packagesPropsPath = FindDirectoryPackagesProps(projectPath);
                if (packagesPropsPath != null)
                {
                    UpdatePackageVersionInCpm(packagesPropsPath, packageName, newVersion);
                    return;
                }
            }

            // Traditional approach: update Version attribute in .csproj
            UpdatePackageVersionInProject(projectPath, packageName, newVersion);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException($"Failed to update package: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Checks if a project uses Central Package Management.
    /// </summary>
    private bool UsesCentralPackageManagement(string projectPath)
    {
        try
        {
            var doc = XDocument.Load(projectPath);

            // Check for ManagePackageVersionsCentrally property
            var cpmProperty = doc.Descendants("ManagePackageVersionsCentrally")
                .FirstOrDefault()?.Value;

            if (cpmProperty?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
            {
                return true;
            }

            // Also check if PackageReferences lack Version attributes (strong indicator of CPM)
            var packageRefs = doc.Descendants("PackageReference").ToList();
            if (packageRefs.Any())
            {
                // If more than 80% of PackageReferences lack Version, likely using CPM
                var withoutVersion = packageRefs.Count(p => p.Attribute("Version") == null);
                return (double)withoutVersion / packageRefs.Count > 0.8;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Finds the Directory.Packages.props file by searching parent directories.
    /// </summary>
    private string? FindDirectoryPackagesProps(string projectPath)
    {
        var projectDir = Path.GetDirectoryName(projectPath);
        if (string.IsNullOrEmpty(projectDir))
        {
            return null;
        }

        var currentDir = new DirectoryInfo(projectDir);

        // Search up to 5 levels up
        for (int i = 0; i < 5 && currentDir != null; i++)
        {
            var packagesPropsPath = Path.Combine(currentDir.FullName, "Directory.Packages.props");
            if (File.Exists(packagesPropsPath))
            {
                return packagesPropsPath;
            }

            currentDir = currentDir.Parent;
        }

        return null;
    }

    /// <summary>
    /// Updates a package version in Directory.Packages.props (CPM).
    /// </summary>
    private void UpdatePackageVersionInCpm(string packagesPropsPath, string packageName, string newVersion)
    {
        var doc = XDocument.Load(packagesPropsPath);
        var packageVersion = doc.Descendants("PackageVersion")
            .FirstOrDefault(p => p.Attribute("Include")?.Value == packageName);

        if (packageVersion == null)
        {
            throw new InvalidOperationException(
                $"Package '{packageName}' not found in Directory.Packages.props at {packagesPropsPath}");
        }

        var versionAttr = packageVersion.Attribute("Version");
        if (versionAttr != null)
        {
            versionAttr.Value = newVersion;
        }

        doc.Save(packagesPropsPath);
    }

    /// <summary>
    /// Updates a package version in the traditional .csproj file.
    /// </summary>
    private void UpdatePackageVersionInProject(string projectPath, string packageName, string newVersion)
    {
        var doc = XDocument.Load(projectPath);
        var packageRef = doc.Descendants("PackageReference")
            .FirstOrDefault(p => p.Attribute("Include")?.Value == packageName);

        if (packageRef == null)
        {
            throw new InvalidOperationException($"Package '{packageName}' not found in project file");
        }

        var versionAttr = packageRef.Attribute("Version");
        if (versionAttr != null)
        {
            versionAttr.Value = newVersion;
        }

        doc.Save(projectPath);
    }

    /// <summary>
    /// Creates a backup of the project file before modifications.
    /// If using CPM, also backs up Directory.Packages.props.
    /// </summary>
    public string CreateBackup(string projectPath)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var directory = Path.GetDirectoryName(projectPath) ?? Path.GetTempPath();
        var fileName = Path.GetFileNameWithoutExtension(projectPath);
        var extension = Path.GetExtension(projectPath);
        var backupPath = Path.Combine(directory, $"{fileName}.backup.{timestamp}{extension}");

        File.Copy(projectPath, backupPath);

        // Also backup Directory.Packages.props if using CPM
        if (UsesCentralPackageManagement(projectPath))
        {
            var packagesPropsPath = FindDirectoryPackagesProps(projectPath);
            if (packagesPropsPath != null && File.Exists(packagesPropsPath))
            {
                var packagesPropsDir = Path.GetDirectoryName(packagesPropsPath) ?? Path.GetTempPath();
                var packagesPropsBackup = Path.Combine(
                    packagesPropsDir,
                    $"Directory.Packages.backup.{timestamp}.props");
                File.Copy(packagesPropsPath, packagesPropsBackup);
            }
        }

        return backupPath;
    }

    /// <summary>
    /// Validates that a project file is well-formed XML.
    /// </summary>
    public bool ValidateProjectFile(string projectPath)
    {
        try
        {
            if (!File.Exists(projectPath))
            {
                return false;
            }

            var doc = XDocument.Load(projectPath);
            return doc.Root != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Restores a project file from backup.
    /// </summary>
    public void RestoreFromBackup(string projectPath, string backupPath)
    {
        if (!File.Exists(backupPath))
        {
            throw new FileNotFoundException($"Backup file not found: {backupPath}");
        }

        File.Copy(backupPath, projectPath, overwrite: true);
    }
}
