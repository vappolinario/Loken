namespace Loken.Core;

public interface IToolHandler
{
    string Name { get; }
    string Description { get; }
    BinaryData Parameters { get; }
    public string WorkingDirectory { get; init; }
    public Task<string> ExecuteAsync(string command);
}
