namespace NugetUpdateBot.Validation;

/// <summary>
/// Service responsible for validating user inputs and file paths.
/// Follows Single Responsibility Principle - only handles input validation.
/// </summary>
public class InputValidatorService
{
    /// <summary>
    /// Validates that a project file exists and has the correct extension.
    /// </summary>
    /// <param name="projectPath">Path to the project file</param>
    /// <exception cref="FileNotFoundException">Thrown when file doesn't exist</exception>
    /// <exception cref="InvalidOperationException">Thrown when file has wrong extension</exception>
    public void ValidateProjectFile(string projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            throw new ArgumentException("Project path cannot be null or empty.", nameof(projectPath));
        }

        if (!File.Exists(projectPath))
        {
            throw new FileNotFoundException($"Project file not found: {projectPath}");
        }

        var extension = Path.GetExtension(projectPath).ToLowerInvariant();
        if (extension != ".csproj" && extension != ".fsproj" && extension != ".vbproj")
        {
            throw new InvalidOperationException(
                $"Invalid project file type: {extension}. Expected .csproj, .fsproj, or .vbproj");
        }
    }

    /// <summary>
    /// Validates that a directory exists and is accessible.
    /// </summary>
    /// <param name="directoryPath">Path to the directory</param>
    /// <exception cref="DirectoryNotFoundException">Thrown when directory doesn't exist</exception>
    public void ValidateDirectory(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new ArgumentException("Directory path cannot be null or empty.", nameof(directoryPath));
        }

        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
        }
    }

    /// <summary>
    /// Validates package name format.
    /// </summary>
    /// <param name="packageName">Package name to validate</param>
    /// <exception cref="ArgumentException">Thrown when package name is invalid</exception>
    public void ValidatePackageName(string packageName)
    {
        if (string.IsNullOrWhiteSpace(packageName))
        {
            throw new ArgumentException("Package name cannot be null or empty.", nameof(packageName));
        }

        // NuGet package IDs must be at least 1 character and can contain letters, numbers, dots, and underscores
        if (packageName.Length == 0)
        {
            throw new ArgumentException("Package name cannot be empty.", nameof(packageName));
        }

        // Check for invalid characters (very basic check)
        if (packageName.Contains(' '))
        {
            throw new ArgumentException(
                $"Package name cannot contain spaces: '{packageName}'", nameof(packageName));
        }
    }

    /// <summary>
    /// Validates output path for reports.
    /// </summary>
    /// <param name="outputPath">Path where report will be saved</param>
    /// <exception cref="InvalidOperationException">Thrown when output path is invalid</exception>
    public void ValidateOutputPath(string outputPath)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Output path cannot be null or empty.", nameof(outputPath));
        }

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException(
                $"Output directory does not exist: {directory}");
        }

        // Check if file is read-only
        if (File.Exists(outputPath))
        {
            var fileInfo = new FileInfo(outputPath);
            if (fileInfo.IsReadOnly)
            {
                throw new InvalidOperationException(
                    $"Output file is read-only: {outputPath}");
            }
        }
    }

    /// <summary>
    /// Validates that a file path doesn't point to a directory.
    /// </summary>
    /// <param name="filePath">File path to validate</param>
    /// <exception cref="InvalidOperationException">Thrown when path points to a directory</exception>
    public void ValidateNotDirectory(string filePath)
    {
        if (Directory.Exists(filePath))
        {
            throw new InvalidOperationException(
                $"Expected a file path but got a directory: {filePath}");
        }
    }

    /// <summary>
    /// Searches for project files in a directory.
    /// </summary>
    /// <param name="directoryPath">Directory to search</param>
    /// <returns>List of project file paths found</returns>
    public List<string> FindProjectFiles(string directoryPath)
    {
        ValidateDirectory(directoryPath);

        var projectFiles = new List<string>();
        var patterns = new[] { "*.csproj", "*.fsproj", "*.vbproj" };

        foreach (var pattern in patterns)
        {
            projectFiles.AddRange(Directory.GetFiles(directoryPath, pattern, SearchOption.TopDirectoryOnly));
        }

        return projectFiles;
    }

    /// <summary>
    /// Resolves a project path (handles both files and directories).
    /// </summary>
    /// <param name="path">Path to resolve</param>
    /// <returns>Resolved project file path</returns>
    /// <exception cref="InvalidOperationException">Thrown when project cannot be resolved</exception>
    public string ResolveProjectPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            path = Directory.GetCurrentDirectory();
        }

        // If it's a file, validate and return
        if (File.Exists(path))
        {
            ValidateProjectFile(path);
            return path;
        }

        // If it's a directory, search for project files
        if (Directory.Exists(path))
        {
            var projectFiles = FindProjectFiles(path);

            if (projectFiles.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No project files found in directory: {path}");
            }

            if (projectFiles.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Multiple project files found in directory: {path}. Please specify which one to use.");
            }

            return projectFiles[0];
        }

        throw new FileNotFoundException($"Path not found: {path}");
    }

    /// <summary>
    /// Validates parallelism value.
    /// </summary>
    /// <param name="parallelism">Number of parallel operations</param>
    /// <exception cref="ArgumentException">Thrown when value is out of valid range</exception>
    public void ValidateParallelism(int parallelism)
    {
        if (parallelism < 1 || parallelism > 16)
        {
            throw new ArgumentException(
                $"Parallelism must be between 1 and 16. Got: {parallelism}",
                nameof(parallelism));
        }
    }
}
