using OpenAI.Chat;
using System;

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
                // Preserve the JSON structure by truncating intelligently
                var truncatedText = TruncateToolResponse(toolMsg.Text, MIN_CONTENT_LENGTH);
                message.Content[contentIdx] = truncatedText;
            }
        }
    }

    private string TruncateToolResponse(string originalText, int maxLength)
    {
        // If it's JSON, try to truncate at a valid JSON boundary
        if (originalText.StartsWith("{") && originalText.EndsWith("}"))
        {
            try
            {
                // Find a good place to truncate within the JSON
                var truncated = originalText.Substring(0, Math.Min(maxLength, originalText.Length));
                
                // Try to close any open JSON structures
                var openBraces = truncated.Count(c => c == '{');
                var closeBraces = truncated.Count(c => c == '}');
                
                while (openBraces > closeBraces && truncated.Length < originalText.Length)
                {
                    // Look for the next closing brace
                    var nextClose = originalText.IndexOf('}', truncated.Length);
                    if (nextClose > 0 && nextClose - truncated.Length < 100) // Don't add too much
                    {
                        truncated = originalText.Substring(0, nextClose + 1);
                        break;
                    }
                    else
                    {
                        // Just add a closing brace
                        truncated += "}";
                        closeBraces++;
                    }
                }
                
                // Add truncation indicator
                if (truncated.Length < originalText.Length)
                {
                    truncated = truncated.TrimEnd('}') + $", \"truncated\": true, \"original_length\": {originalText.Length}}}";
                }
                
                return truncated;
            }
            catch
            {
                // Fallback to simple truncation
                return $"[Tool response truncated. Original length: {originalText.Length} chars] {originalText[..Math.Min(maxLength, originalText.Length)]}";
            }
        }
        
        // For non-JSON content, use simple truncation
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
