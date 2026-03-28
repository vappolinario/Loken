namespace Loken.Core;

public readonly record struct ShellResult(string Stdout, string Stderr, int ExitCode)
{
    public string Formatted
    {
        get
        {
            var output = Stdout;

            if (!string.IsNullOrEmpty(Stderr))
                output += $"\nSTDERR:\n{Stderr}";

            if (ExitCode != 0)
                output += $"\n[exit code: {ExitCode}]";

            if (output.Length > 50_000)
                output = output[..50_000];

            return string.IsNullOrWhiteSpace(output) ? "(no output)" : output;
        }
    }
}
