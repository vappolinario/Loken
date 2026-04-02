namespace Loken.Core;

using System;
using System.Collections.Generic;
using OpenAI.Chat;

public partial class Agent
{
    private List<ChatMessage> _messages;
    private readonly IEnumerable<IToolHandler> handlers;
    private readonly IChatClient _chatClient;
    private ChatCompletionOptions _options;
    private IAgentReporter _reporter;
    private readonly ITodoService _todoService;
    private Dictionary<string, IToolHandler> _toolHandlers;

    public Agent(IEnumerable<IToolHandler> handlers,
        IChatClient chatClient,
        IAgentReporter reporter,
        ITodoService todoService)
    {
        _messages = new List<ChatMessage>()
        {
          new SystemChatMessage(_systemPrompt)
        };

        _toolHandlers = handlers.ToDictionary(h => h.Name, h => h);
        _options = new ChatCompletionOptions();
        foreach (var tool in handlers.ToChatTools())
            _options.Tools.Add(tool);
        this.handlers = handlers;
        _chatClient = chatClient;
        _reporter = reporter;
        this._todoService = todoService;
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

                if (toolCall.FunctionName == "todo")
                    _todoService.MarkTodoCalled();

                _messages.Add(new ToolChatMessage(toolCall.Id, output));
                _reporter.ReportMessage(output, true);
            }

            if (_todoService.ShouldRemindAboutTodos())
                _messages.Add(new AssistantChatMessage("Update your todos"));
        }
    }

    public async Task<string> ExecuteToolAsync(string name, BinaryData input)
    {
        if (!_toolHandlers.TryGetValue(name, out var handler))
            throw new UnknownToolException(name);

        _reporter.ReportMessage($"Tool used: {name}", true);

        return await handler.ExecuteAsync(input);
    }
}
