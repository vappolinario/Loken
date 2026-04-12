using OpenAI.Chat;

namespace Loken.Core;

public class ContextCompactorService : IContextCompactorService
{
    private readonly int KEEP_RECENT = 3;
    private readonly int MIN_CONTENT_LENGTH = 100;

    public void MicroCompact(IList<ChatMessage> messages)
    {
        var toolResultLocations = FindToolResultLocations(messages).ToList();

        if (toolResultLocations.Count <= KEEP_RECENT)
            return;

        var oldResults = toolResultLocations.SkipLast(KEEP_RECENT);

        foreach (var (msgIdx, contentIdx) in oldResults)
        {
            var message = messages[msgIdx];
            var content = message.Content[contentIdx];

            if (content is ChatMessageContentPart toolMsg && toolMsg.Text.Length > MIN_CONTENT_LENGTH)
            {
                message.Content[contentIdx] = $"[Previous: Tool with id {msgIdx} was used {toolMsg.Text[..MIN_CONTENT_LENGTH]}";
            }
        }
    }

    private IEnumerable<(int MsgIdx, int ContentIdx)> FindToolResultLocations(IList<ChatMessage> messages)
    {
        for (int m = 0; m < messages.Count; m++)
            for (int c = 0; c < messages[m].Content.Count; c++)
                if (messages[m] is ToolChatMessage)
                    yield return (m, c);
    }
}
