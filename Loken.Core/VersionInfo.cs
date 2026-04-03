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
        var assembly = typeof(VersionInfo).Assembly;
        var gitCommitHash = GetGitCommitHash(assembly);

        _version = string.IsNullOrEmpty(gitCommitHash)
            ? "0.1-unknown"
            : $"0.1-{gitCommitHash}";
    }

    public static string Version => _version;

    private static string? GetGitCommitHash(Assembly assembly)
    {
        var gitCommitHashAttribute = assembly.GetCustomAttribute<AssemblyMetadataAttribute>();
        if (gitCommitHashAttribute != null && gitCommitHashAttribute.Key == "GitCommitHash")
        {
            return gitCommitHashAttribute.Value;
        }

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
            return null;
        }
    }
}
