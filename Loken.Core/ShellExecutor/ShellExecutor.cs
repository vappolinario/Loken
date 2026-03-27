namespace Loken.Core;

using System.Diagnostics;
using System.Text;

public class ShellExecutor : IShellExecutor
{
    private static readonly string[] DangerousPatterns =
    [
        "rm -rf /", "sudo", "shutdown", "reboot", "> /dev/"
    ];

    public string WorkingDirectory { get; init; }

    public ShellExecutor(string workingDirectory = ".")
    {
        WorkingDirectory = workingDirectory;
    }

    public async Task<ShellResult> ExecuteAsync(string command)
    {
        if (DangerousPatterns.FirstOrDefault(p => command.Contains(p)) is string blocked)
        {
            return new ShellResult(
                Stdout: "",
                Stderr: $"Error: Dangerous command blocked (matched '{blocked}')",
                ExitCode: 1
            );
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = $"-c \"{command.Replace("\"", "\\\"")}\"",
            WorkingDirectory = WorkingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            return new ShellResult(stdout, stderr, process.ExitCode);
        }
        catch (Exception ex)
        {
            return new ShellResult("", $"Exception: {ex.Message}", 1);
        }
    }
}
