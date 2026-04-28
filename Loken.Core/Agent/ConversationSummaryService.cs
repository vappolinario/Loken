namespace Loken.Core;

using System.Text;
using Microsoft.Extensions.Options;
using Loken.Core.Options;
using OpenAI.Chat;

public class ConversationSummaryService(
    IOptions<ConversationSummaryOptions> options,
    IAgentReporter reporter,
    IChatClient chatClient)
{
    private readonly IOptions<ConversationSummaryOptions> _options = options;
    private readonly IAgentReporter _reporter = reporter;
    private readonly IChatClient _chatClient = chatClient;

    public async Task<string?> GenerateSummaryAsync(IReadOnlyList<ChatMessage> messages, string? modelName = null)
    {
        if (messages.Count < 2)
        {
            _reporter.ReportMessage($"Conversation too short to summarize (only {messages.Count} messages). Skipping.", false);
            return null;
        }

        var directory = ResolveDirectory();

        string markdown;
        string title;

        try
        {
            _reporter.ReportMessage("Generating conversation summary via LLM...", false);
            markdown = await GenerateLlmSummaryAsync(messages, modelName);
            _reporter.ReportMessage("LLM summary generated successfully.", false);
        }
        catch (Exception ex)
        {
            _reporter.ReportMessage($"Warning: {ex.Message} - LLM summary generation failed. Falling back to mechanical summary.", false);
            title = GenerateTitle(messages);
            markdown = BuildSummaryMarkdown(messages, title, modelName);
            return await WriteSummaryFileAsync(directory, markdown, title);
        }

        try
        {
            _reporter.ReportMessage("Generating title from summary...", false);
            title = await GenerateLlmTitleAsync(markdown, modelName);
            _reporter.ReportMessage($"LLM title generated: {title}", false);
        }
        catch (Exception ex)
        {
            _reporter.ReportMessage($"Warning: {ex.Message} - LLM title generation failed. Falling back to mechanical title.", false);
            title = GenerateTitle(messages);
        }

        return await WriteSummaryFileAsync(directory, markdown, title);
    }

    private async Task<string> WriteSummaryFileAsync(string directory, string markdown, string title)
    {
        var cwd = new DirectoryInfo(Directory.GetCurrentDirectory()).Name;
        var fileName = $"{DateTime.Now:yyyyMMdd}-{SanitizeFileName(cwd)}-{SanitizeFileName(title)}.md";
        var filePath = Path.Combine(directory, fileName);

        await File.WriteAllTextAsync(filePath, markdown, Encoding.UTF8);

        _reporter.ReportMessage($"Conversation summary saved to {filePath}", false);
        return filePath;
    }

    private async Task<string> GenerateLlmSummaryAsync(IReadOnlyList<ChatMessage> messages, string? modelName)
    {
        var conversationText = SerializeMessagesForPrompt(messages);

        const string systemPrompt = """
You are a precise conversation summarizer. Given the conversation below, produce a concise markdown summary with these sections:

## Summary
A brief 2-3 sentence high-level overview of what was accomplished.

## Key Decisions
- Bullet points of important decisions made or conclusions reached.

## Technical Details
- Bullet points of any code changes, files modified, tools used, or technical outcomes.

## Next Steps
- Bullet points of any follow-up tasks or unresolved items mentioned.

Keep the tone factual and neutral. Do not add commentary beyond what is present in the conversation. Return only the summary content, no title or header.
""";

        var userMessage = $"""
{conversationText}
""";

        var summaryMessages = new ChatMessage[]
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userMessage),
        };

        var response = await _chatClient.CompleteChatAsync(summaryMessages, new ChatCompletionOptions());
        var summary = string.Concat(response.Value.Content.Select(c => c.Text));

        var sb = new StringBuilder();
        sb.AppendLine($"**Date:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        if (!string.IsNullOrWhiteSpace(modelName))
            sb.AppendLine($"**Model:** {modelName}");
        sb.AppendLine($"**Messages:** {messages.Count}");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine(summary);
        sb.AppendLine();

        return sb.ToString();
    }

    private async Task<string> GenerateLlmTitleAsync(string summaryMarkdown, string? modelName)
    {
        const string systemPrompt = """
You are a precise conversation titler. Given the conversation summary below, produce a single concise title (max 10 words) that captures the essence of the conversation.

Rules:
- Return ONLY the title text, no quotes, no formatting, no extra commentary.
- Max 10 words.
- Be descriptive but concise.
- Use title case.
""";

        var userMessage = $"""
Generate a title for the following conversation summary:

{summaryMarkdown}
""";

        var titleMessages = new ChatMessage[]
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userMessage),
        };

        var response = await _chatClient.CompleteChatAsync(titleMessages, new ChatCompletionOptions());
        var title = string.Concat(response.Value.Content.Select(c => c.Text)).Trim();

        if (string.IsNullOrWhiteSpace(title))
            throw new InvalidOperationException("LLM returned an empty title.");

        return title;
    }

    private static string SerializeMessagesForPrompt(IReadOnlyList<ChatMessage> messages)
    {
        var sb = new StringBuilder();
        foreach (var msg in messages)
        {
            var role = msg switch
            {
                SystemChatMessage => "SYSTEM",
                UserChatMessage => "USER",
                AssistantChatMessage => "ASSISTANT",
                ToolChatMessage => "TOOL",
                _ => "UNKNOWN"
            };

            var text = ExtractTextContent(msg);
            if (string.IsNullOrWhiteSpace(text))
                continue;

            sb.AppendLine($"=== {role} ===");
            sb.AppendLine(text);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    public bool ShouldGenerate(bool? explicitOverride = null)
    {
        var setting = explicitOverride ?? _options.Value.Enabled;
        return setting ?? false;
    }

    public bool ShouldPrompt => _options.Value.Enabled is null;

    private string ResolveDirectory()
    {
        var configured = _options.Value.Directory;
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;

        return Directory.GetCurrentDirectory();
    }

    private static string GenerateTitle(IReadOnlyList<ChatMessage> messages)
    {
        foreach (var msg in messages)
        {
            if (msg is UserChatMessage userMsg)
            {
                var text = ExtractTextContent(userMsg);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var firstLine = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .FirstOrDefault() ?? text;

                    if (firstLine.Length > 60)
                        firstLine = firstLine[..57] + "...";

                    return firstLine;
                }
            }
        }

        return "Conversation Summary";
    }

    private static string SanitizeFileName(string title)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string([.. title.Where(c => !invalid.Contains(c) && !char.IsControl(c))]);

        sanitized = sanitized.Trim();
        if (sanitized.Length > 100)
            sanitized = sanitized[..100];

        return string.IsNullOrWhiteSpace(sanitized) ? "Conversation-Summary" : sanitized;
    }

    private static string BuildSummaryMarkdown(IReadOnlyList<ChatMessage> messages, string title, string? modelName)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"# {title}");
        sb.AppendLine();
        sb.AppendLine($"**Date:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        if (!string.IsNullOrWhiteSpace(modelName))
            sb.AppendLine($"**Model:** {modelName}");
        sb.AppendLine($"**Messages:** {messages.Count}");
        sb.AppendLine();

        var systemPrompt = messages.OfType<SystemChatMessage>().FirstOrDefault();
        if (systemPrompt != null)
        {
            var systemText = ExtractTextContent(systemPrompt);
            if (!string.IsNullOrWhiteSpace(systemText))
            {
                sb.AppendLine("## System Prompt");
                sb.AppendLine();
                sb.AppendLine("```");
                sb.AppendLine(systemText);
                sb.AppendLine("```");
                sb.AppendLine();
            }
        }

        sb.AppendLine("## Conversation");
        sb.AppendLine();
        sb.AppendLine("| Role | Content |");
        sb.AppendLine("|------|---------|");

        foreach (var msg in messages)
        {
            var role = msg switch
            {
                SystemChatMessage => "System",
                UserChatMessage => "User",
                AssistantChatMessage => "Assistant",
                ToolChatMessage => "Tool",
                _ => "Unknown"
            };

            var summary = ExtractTextContent(msg);
            if (!string.IsNullOrWhiteSpace(summary))
            {
                var firstLine = summary.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .FirstOrDefault() ?? string.Empty;
                if (firstLine.Length > 120)
                    firstLine = firstLine[..117] + "...";
                summary = firstLine.EscapeMarkdown();
            }

            sb.AppendLine($"| {role} | {summary ?? "*non-text content*"} |");
        }

        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        sb.AppendLine("## Full Message Details");
        sb.AppendLine();

        for (int i = 0; i < messages.Count; i++)
        {
            var msg = messages[i];
            var role = msg switch
            {
                SystemChatMessage => "System",
                UserChatMessage => "User",
                AssistantChatMessage => "Assistant",
                ToolChatMessage => "Tool",
                _ => "Unknown"
            };

            var text = ExtractTextContent(msg);
            if (string.IsNullOrWhiteSpace(text))
                continue;

            sb.AppendLine($"### Message {i + 1} — {role}");
            sb.AppendLine();
            sb.AppendLine("```");
            sb.AppendLine(text);
            sb.AppendLine("```");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string? ExtractTextContent(ChatMessage msg)
    {
        if (msg.Content == null || msg.Content.Count == 0)
            return null;

        var sb = new StringBuilder();
        foreach (var content in msg.Content)
        {
            if (content.Kind == ChatMessageContentPartKind.Text && !string.IsNullOrWhiteSpace(content.Text))
            {
                if (sb.Length > 0)
                    sb.Append(' ');
                sb.Append(content.Text);
            }
        }

        return sb.Length > 0 ? sb.ToString() : null;
    }
}

internal static class MarkdownExtensions
{
    internal static string EscapeMarkdown(this string text)
    {
        return text
            .Replace("|", "\\|")
            .Replace("*", "\\*")
            .Replace("[", "\\[")
            .Replace("]", "\\]")
            .Replace("`", "\\`");
    }
}
