using System.Text;

namespace Loken.Core;

public class ToolService : IToolService
{
    private readonly IEnumerable<IToolHandler> _handlers;

    public ToolService(IEnumerable<IToolHandler> handlers)
    {
        _handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
    }

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
