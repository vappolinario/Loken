namespace Loken.Core;

using System.Diagnostics;
using System.Text;
using System.Text.Json;

public class ShellExecutorHandler : IToolHandler
{
    private static readonly string[] DangerousPatterns =
    [
        "rm -rf /", "sudo", "shutdown", "reboot", "> /dev/"
    ];

    private readonly IPathResolver _resolver;

    public string Name => "bash";

    public string Description => "Run a shell command and return its output.";

    public BinaryData Parameters => BinaryData.FromObjectAsJson(
                new
                {
                    type = "object",
                    properties = new
                    {
                        command = new
                        {
                            type = "string",
                            description = "The shell command to execute"
                        }
                    },
                    required = new[] { "command" }
    });


    public ShellExecutorHandler(IPathResolver resolver)
    {
      _resolver = resolver;
    }

    public async Task<string> ExecuteAsync(BinaryData input)
    {
        var json = JsonDocument.Parse(input);

        if (!json.RootElement.TryGetProperty("command", out var commandProperty) ||
            commandProperty.GetString() is not string command)
            throw new MissingParameterException("command");

        if (DangerousPatterns.FirstOrDefault(p => command.Contains(p)) is string blocked)
            throw new System.Security.SecurityException(
                $"Error: Dangerous command blocked (matched '{blocked}')");

        var startInfo = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = $"-c \"{command.Replace("\"", "\\\"")}\"",
            WorkingDirectory = _resolver.WorkingDirectory,
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

            if (process.ExitCode != 0 || !string.IsNullOrWhiteSpace(stderr))
                throw new Loken.Core.ExecutionFailedException(stderr);

            return stdout;
        }
        catch (Exception ex)
        {
            throw new ExecutionFailedException(ex.Message);
        }
    }
}
