using OpenAI.Chat;

namespace Loken.Core;

public static class ToolMappingExtensions
{
    public static ChatTool ToChatTool(this IToolHandler handler)
    {
        return ChatTool.CreateFunctionTool(
            functionName: handler.Name,
            functionDescription: handler.Description,
            functionParameters: handler.Parameters
        );
    }

    public static IEnumerable<ChatTool> ToChatTools(this IEnumerable<IToolHandler> handlers)
        => handlers.Select(h => h.ToChatTool());
}
