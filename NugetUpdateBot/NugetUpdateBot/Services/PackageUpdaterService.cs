using System.Xml.Linq;

namespace NugetUpdateBot.Services;

/// <summary>
/// Service responsible for updating package versions in project files.
/// Follows Single Responsibility Principle - only handles package update operations.
/// </summary>
public class PackageUpdaterService
{
    /// <summary>
    /// Updates a package version in the project file while preserving XML formatting.
    /// </summary>
    public void UpdatePackageVersion(string projectPath, string packageName, string newVersion)
    {
        try
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
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException($"Failed to update package: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Creates a backup of the project file before modifications.
    /// </summary>
    public string CreateBackup(string projectPath)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var directory = Path.GetDirectoryName(projectPath) ?? Path.GetTempPath();
        var fileName = Path.GetFileNameWithoutExtension(projectPath);
        var extension = Path.GetExtension(projectPath);
        var backupPath = Path.Combine(directory, $"{fileName}.backup.{timestamp}{extension}");

        File.Copy(projectPath, backupPath);
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
