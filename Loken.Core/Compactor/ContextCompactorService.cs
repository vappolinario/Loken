using OpenAI.Chat;

namespace Loken.Core;

public class ContextCompactorService : IContextCompactorService
{
    private const int KEEP_RECENT = 3;
    private const int MIN_CONTENT_LENGTH = 100;

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
                var truncatedText = TruncateToolResponse(toolMsg.Text, MIN_CONTENT_LENGTH);
                message.Content[contentIdx] = truncatedText;
            }
        }
    }

    private string TruncateToolResponse(string originalText, int maxLength)
    {
        if (originalText.StartsWith("{") && originalText.EndsWith("}"))
        {
            try
            {
                var truncated = originalText.Substring(0, Math.Min(maxLength, originalText.Length));

                var openBraces = truncated.Count(c => c == '{');
                var closeBraces = truncated.Count(c => c == '}');

                while (openBraces > closeBraces && truncated.Length < originalText.Length)
                {
                    var nextClose = originalText.IndexOf('}', truncated.Length);
                    if (nextClose > 0 && nextClose - truncated.Length < MIN_CONTENT_LENGTH)
                    {
                        truncated = originalText.Substring(0, nextClose + 1);
                        break;
                    }
                    else
                    {
                        truncated += "}";
                        closeBraces++;
                    }
                }

                if (truncated.Length < originalText.Length)
                    truncated = truncated.TrimEnd('}') + $", \"truncated\": true, \"original_length\": {originalText.Length}}}";

                return truncated;
            }
            catch
            {
                return $"[Tool response truncated. Original length: {originalText.Length} chars] {originalText[..Math.Min(maxLength, originalText.Length)]}";
            }
        }

        if (originalText.Length <= maxLength)
            return originalText;

        return $"[Tool response truncated. Original length: {originalText.Length} chars] {originalText[..maxLength]}";
    }

    private IEnumerable<(int MsgIdx, int ContentIdx)> FindToolResultLocations(IList<ChatMessage> messages)
    {
        for (int m = 0; m < messages.Count; m++)
            for (int c = 0; c < messages[m].Content.Count; c++)
                if (messages[m] is ToolChatMessage)
                    yield return (m, c);
    }
}
