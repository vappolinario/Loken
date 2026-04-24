using System.Text;

namespace Loken.Core;

/// <summary>
/// Collects all registered <see cref="IToolHandler"/> instances and formats
/// them into a human-readable listing of available tools.
/// </summary>
public class ToolService : IToolService
{
    private readonly IEnumerable<IToolHandler> _handlers;

    public ToolService(IEnumerable<IToolHandler> handlers)
    {
        _handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
    }

    /// <summary>
    /// Returns a formatted string listing all registered tool handlers
    /// with their names and descriptions.
    /// </summary>
    public string GetTools()
    {
        var sb = new StringBuilder();

        foreach (var handler in _handlers.OrderBy(h => h.Name))
        {
            sb.AppendLine($"  - {handler.Name}: {handler.Description}");
        }

        return sb.ToString().TrimEnd();
    }
}
