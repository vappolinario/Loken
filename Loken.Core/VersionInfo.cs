namespace Loken.Core;

using System.Diagnostics;
using System.Reflection;

/// <summary>
/// Provides version information for the application, including git commit hash.
/// </summary>
public static class VersionInfo
{
    private static readonly string _version;

    static VersionInfo()
    {
        // Try to get the git commit hash from assembly metadata
        var assembly = typeof(VersionInfo).Assembly;
        var gitCommitHash = GetGitCommitHash(assembly);
        
        // Base version is 0.1, append commit hash if available
        _version = string.IsNullOrEmpty(gitCommitHash) 
            ? "0.1-unknown" 
            : $"0.1-{gitCommitHash}";
    }

    /// <summary>
    /// Gets the current version of the application.
    /// </summary>
    public static string Version => _version;

    /// <summary>
    /// Gets the git commit hash from assembly metadata or by executing git command.
    /// </summary>
    private static string? GetGitCommitHash(Assembly assembly)
    {
        // First, try to get from assembly metadata (set during build)
        var gitCommitHashAttribute = assembly.GetCustomAttribute<AssemblyMetadataAttribute>();
        if (gitCommitHashAttribute != null && gitCommitHashAttribute.Key == "GitCommitHash")
        {
            return gitCommitHashAttribute.Value;
        }

        // Fallback: try to execute git command
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "rev-parse --short HEAD",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            
            return string.IsNullOrEmpty(output) ? null : output;
        }
        catch
        {
            // Git command failed or git is not installed
            return null;
        }
    }
}