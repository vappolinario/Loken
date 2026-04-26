namespace Loken.Core;

using System;
using System.Collections.Generic;
using OpenAI.Chat;

public partial class Agent
{
    private readonly List<ChatMessage> _messages;
    private readonly IEnumerable<IToolHandler> _handlers;
    private readonly IChatClient _chatClient;
    private readonly ChatCompletionOptions _options;
    private readonly IAgentReporter _reporter;
    private readonly ITodoService _todoService;
    private readonly ISkillService _skillService;
    private readonly IContextCompactorService _compactor;
    private readonly Dictionary<string, IToolHandler> _toolHandlers;

    public Agent(IEnumerable<IToolHandler> handlers,
                 IChatClient chatClient,
                 IAgentReporter reporter,
                 ITodoService todoService,
                 ISkillService skillService,
                 IContextCompactorService compactorService)
    {
        _messages = new List<ChatMessage>();
        _toolHandlers = handlers.ToDictionary(h => h.Name, h => h);
        _handlers = handlers;
        _options = new ChatCompletionOptions();
        foreach (var tool in _handlers.ToChatTools())
            _options.Tools.Add(tool);

        _chatClient = chatClient;
        _reporter = reporter;
        _todoService = todoService;
        _skillService = skillService;

        _compactor = compactorService;
    }

    public string Version() => VersionInfo.Version;

    public async Task<string> Run(string prompt)
    {
        _messages.Add(new UserChatMessage(prompt));
        while (true)
        {
            _compactor.MicroCompact(_messages);

            var result = await _chatClient.CompleteChatAsync(_messages, _options);
            var assistantMessage = new AssistantChatMessage(result);
            _messages.Add(assistantMessage);

            foreach (var msg in assistantMessage.Content)
                _reporter.ReportMessage(msg.Text, false);

            if (result.Value.FinishReason != ChatFinishReason.ToolCalls)
                return result.Value.Content[0].Text;

            foreach (var toolCall in result.Value.ToolCalls)
            {
                string output = string.Empty;
                try
                {
                    output = await ExecuteToolAsync(toolCall.FunctionName, toolCall.FunctionArguments);
                }
                catch (ToolException ex) when (ex is ExecutionFailedException)
                {
                    output = ex.Message;
                }

                var tm = new ToolChatMessage(toolCall.Id, output);
                _messages.Add(tm);
                _reporter.ReportMessage(output, true);
            }

            if (_todoService.ShouldRemindAboutTodos())
                _messages.Add(new AssistantChatMessage("If you have an APPROVED strategy update your todos"));
        }
    }

    public async Task<string> ExecuteToolAsync(string name, BinaryData input)
    {
        if (!_toolHandlers.TryGetValue(name, out var handler))
            throw new UnknownToolException(name);

        _reporter.ReportMessage($"Tool used: {name}", true);

        return await handler.ExecuteAsync(input);
    }

    /// <summary>
    /// Returns a read-only copy of the conversation messages for summary generation.
    /// </summary>
    public IReadOnlyList<ChatMessage> GetMessages() => _messages.AsReadOnly();

    public void SetSystemPrompt(string prompt)
    {
        var skillDescription = _skillService.GetSkills();

        if (!string.IsNullOrWhiteSpace(skillDescription))
            prompt += $"\nUse load_skill to access specialized knowledge.\n\nSkills available:\n{skillDescription}";

        _messages.Clear();
        _messages.Add(new SystemChatMessage(prompt));
    }
}
