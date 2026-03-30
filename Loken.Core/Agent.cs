namespace Loken.Core;

using System;
using System.Collections.Generic;
using System.Text.Json;
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
            var x = new AssistantChatMessage(result);
            _messages.Add(x);

            foreach (var msg in x.Content)
            {
                _reporter.ReportMessage(msg.Text, false);
            }

            if (result.Value.FinishReason != ChatFinishReason.ToolCalls)
                return result.Value.Content[0].Text;

            foreach (var toolCall in result.Value.ToolCalls)
            {
                try
                {
                    var toolResult = await ExecuteToolAsync(toolCall.FunctionName, toolCall.FunctionArguments);
                    _messages.Add(new ToolChatMessage(toolCall.Id, toolResult));
                    _reporter.ReportMessage(toolResult, true);
                }
                catch (ToolException ex) when (ex is ExecutionFailedException)
                {
                    _messages.Add(new ToolChatMessage(toolCall.Id, ex.Message));
                    _reporter.ReportMessage(ex.Message, true);
                }
                catch (System.Exception)
                {
                    throw;
                }
            }
        }
    }

    public async Task<string> ExecuteToolAsync(string name, BinaryData input)
    {

        if (!_toolHandlers.TryGetValue(name, out var handler))
            throw new UnknownToolException(name);

        var json = JsonDocument.Parse(input);

        if (!json.RootElement.TryGetProperty("command", out var commandProperty) ||
            commandProperty.GetString() is not string command)
            throw new MissingParameterException("command");

        return await handler.ExecuteAsync(command);
    }
}
