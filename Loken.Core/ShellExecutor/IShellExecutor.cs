namespace Loken.Core;

public interface IShellExecutor
{
    string WorkingDirectory { get; init; }
    Task<ShellResult> ExecuteAsync(string command);
}
