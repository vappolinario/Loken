namespace Loken.Core;

using System.Diagnostics;
using System.Reflection;

public static class VersionInfo
{

    static VersionInfo()
    {
        var assembly = typeof(VersionInfo).Assembly;
        var gitCommitHash = GetGitCommitHash(assembly);

        Version = string.IsNullOrEmpty(gitCommitHash)
            ? "0.1-unknown"
            : $"0.1-{gitCommitHash}";
    }

    public static string Version { get; }

    private static string? GetGitCommitHash(Assembly assembly)
    {
        var gitCommitHashAttribute = assembly.GetCustomAttribute<AssemblyMetadataAttribute>();
        if (gitCommitHashAttribute?.Key == "GitCommitHash")
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
