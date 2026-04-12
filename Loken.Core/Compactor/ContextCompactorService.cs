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
                    if (nextClose > 0 && nextClose - truncated.Length < MIN_CONTENT_LENGTH && nextClose + 1 <= maxLength)
                    {
                        // Only extend if it won't exceed maxLength
                        truncated = originalText.Substring(0, nextClose + 1);
                        break;
                    }
                    else
                    {
                        // Add a closing brace, but only if it won't exceed maxLength
                        if (truncated.Length + 1 <= maxLength)
                        {
                            truncated += "}";
                            closeBraces++;
                        }
                        else
                        {
                            // Can't add closing brace without exceeding maxLength
                            break;
                        }
                    }
                }

                // Only add metadata if it won't make the string longer than maxLength
                // and if we actually truncated something
                if (truncated.Length < originalText.Length)
                {
                    var metadata = $", \"truncated\": true, \"original_length\": {originalText.Length}}}";
                    var newLength = truncated.TrimEnd('}').Length + metadata.Length;
                    
                    if (newLength <= maxLength)
                    {
                        truncated = truncated.TrimEnd('}') + metadata;
                    }
                    else
                    {
                        // If metadata would make it too long, use simple truncation with "..."
                        // We need to truncate enough to fit "..."
                        var availableLength = maxLength - 3;
                        if (availableLength > 0)
                        {
                            // Take substring that fits
                            truncated = truncated.Substring(0, Math.Min(availableLength, truncated.Length));
                            
                            // Try to end at a valid JSON point
                            var lastValidEnd = FindLastValidJsonEnd(truncated);
                            if (lastValidEnd > 0)
                            {
                                truncated = truncated.Substring(0, lastValidEnd);
                            }
                            
                            truncated += "...";
                        }
                        else
                        {
                            // If we can't even fit "...", just return empty or minimal
                            truncated = "...";
                        }
                    }
                }

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

    private int FindLastValidJsonEnd(string jsonFragment)
    {
        // Simple heuristic: find the last position where braces are balanced
        int balance = 0;
        int lastBalancedPos = -1;
        
        for (int i = 0; i < jsonFragment.Length; i++)
        {
            char c = jsonFragment[i];
            if (c == '{' || c == '[')
                balance++;
            else if (c == '}' || c == ']')
                balance--;
                
            if (balance == 0)
                lastBalancedPos = i;
        }
        
        // If we found a balanced position, return it + 1 (since substring is exclusive)
        return lastBalancedPos >= 0 ? lastBalancedPos + 1 : -1;
    }

    private IEnumerable<(int MsgIdx, int ContentIdx)> FindToolResultLocations(IList<ChatMessage> messages)
    {
        for (int m = 0; m < messages.Count; m++)
        {
            if (messages[m] is ToolChatMessage)
            {
                for (int c = 0; c < messages[m].Content.Count; c++)
                {
                    yield return (m, c);
                }
            }
        }
    }
}
