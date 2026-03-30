namespace Loken.Core;

public interface IToolHandler
{
    string Name { get; }
    string Description { get; }
    BinaryData Parameters { get; }
    public Task<string> ExecuteAsync(BinaryData input);
}
