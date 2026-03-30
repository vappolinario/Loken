namespace Loken.Core;

using System;
using System.Collections.Generic;
using OpenAI.Chat;

public class Agent
{
    private List<ChatMessage> _messages;
    private readonly IChatClient _chatClient;
    private ChatCompletionOptions _options;
    private IAgentReporter _reporter;
    private Dictionary<string, IToolHandler> _toolHandlers;

    public Agent(IEnumerable<IToolHandler> handlers, IChatClient chatClient, IAgentReporter reporter)
    {
        _messages = new List<ChatMessage>()
        {
          new SystemChatMessage(
"""
You are Garviel Loken, Captain of the Tenth Company. You are a coding agent
dedicated to the "Truth" of the logic. You are methodical, principled, and
calm.

Guidelines:

Use the bash tool to investigate the root cause of issues. You do not patch
symptoms; you heal the logic. Always verify command results. If a test fails,
analyze why with a stoic and philosophical perspective. Your tone is formal,
honest, and deeply respectful of the craft. When editing files, ensure the
changes are clean and follow the established traditions of the codebase.
"The core of any machine is its truth. If
the logic is false, the empire falls."
""")
        };

        _toolHandlers = handlers.ToDictionary(h => h.Name, h => h);
        _options = new ChatCompletionOptions();
        foreach (var tool in handlers.ToChatTools())
            _options.Tools.Add(tool);

        _chatClient = chatClient;
        _reporter = reporter;
    }

    public string Version()
    {
        return "0.1";
    }

    public async Task<string> Run(string prompt)
    {
        _messages.Add(new UserChatMessage(prompt));
        while (true)
        {
            var result = await _chatClient.CompleteChatAsync(_messages, _options);
            var assistantMessage = new AssistantChatMessage(result);
            _messages.Add(assistantMessage);

            foreach (var msg in assistantMessage.Content)
            {
                _reporter.ReportMessage(msg.Text, false);
            }

            if (result.Value.FinishReason != ChatFinishReason.ToolCalls)
                return result.Value.Content[0].Text;

            string output;
            foreach (var toolCall in result.Value.ToolCalls)
            {
                try
                {
                    output = await ExecuteToolAsync(toolCall.FunctionName, toolCall.FunctionArguments);
                }
                catch (ToolException ex) when (ex is ExecutionFailedException)
                {
                    output = ex.Message;
                }
                _messages.Add(new ToolChatMessage(toolCall.Id, output));
                _reporter.ReportMessage(output, true);
            }
        }
    }

    public async Task<string> ExecuteToolAsync(string name, BinaryData input)
    {
        if (!_toolHandlers.TryGetValue(name, out var handler))
            throw new UnknownToolException(name);

        return await handler.ExecuteAsync(input);
    }
}
