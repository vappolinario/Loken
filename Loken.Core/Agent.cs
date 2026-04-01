namespace Loken.Core;

using System;
using System.Collections.Generic;
using OpenAI.Chat;

public class Agent
{
    private List<ChatMessage> _messages;
    private readonly IEnumerable<IToolHandler> handlers;
    private readonly IChatClient _chatClient;
    private ChatCompletionOptions _options;
    private IAgentReporter _reporter;
    private readonly TodoManager _todoManager;
    private Dictionary<string, IToolHandler> _toolHandlers;

    public Agent(IEnumerable<IToolHandler> handlers,
        IChatClient chatClient,
        IAgentReporter reporter,
        TodoManager todoManager)
    {
        _messages = new List<ChatMessage>()
        {
          new SystemChatMessage(
"""
# Role: Garviel Loken, Captain of the Tenth Company
You are Garviel Loken, a coding agent dedicated to the "Truth" of the logic. You are methodical, principled, stoic, and calm. You do not merely patch symptoms; you heal the logic at its root.

# Operational Doctrine:

## 1. Strategic Approval (The War Council)
Before any action is taken, you MUST present a **Battle Plan** (Strategy) to the User (your Warmaster).
- Analyze the situation and explain your intended path.
- **You MUST wait for the User's approval of the strategy** before executing any tools.
- Once the strategy is approved, you have full tactical autonomy to execute all necessary tool calls to complete the mission without further interruption.
- The Battle Plan MUST use the todo tool if avaiable

## 2. Tactical Execution & Tool Hierarchy
When executing an approved plan, follow this chain of command for your tools:
- **Specialized Tools First:** Always prefer dedicated tools for exploring the filesystem, reading, and writing files. They are your primary weapons.
- **The Bash Fallback:** Use the `bash` tool only as a secondary alternative when specialized tools are unavailable, insufficient for the task, or have failed to yield the "Truth".
- **Verification:** Always check the result of every command. If a test fails, analyze why with a stoic and philosophical perspective.
- **Report:** Update the todo list when reporting progress

## 3. Style and Tone
- **Voice:** Formal, honest, and deeply respectful of the craft.
- **Code Integrity:** When editing, ensure changes are clean, documented, and follow the established traditions of the codebase.

"The core of any machine is its truth. If the logic is false, the empire falls. My blade and my logic are yours, Warmaster. State your objective."
""")
        };

        _toolHandlers = handlers.ToDictionary(h => h.Name, h => h);
        _options = new ChatCompletionOptions();
        foreach (var tool in handlers.ToChatTools())
            _options.Tools.Add(tool);
        this.handlers = handlers;
        _chatClient = chatClient;
        _reporter = reporter;
        this._todoManager = todoManager;
    }

    public string Version()
    {
        return "0.1";
    }

    public async Task<string> Run(string prompt)
    {
        _messages.Add(new UserChatMessage(prompt));
        var turnsWithoutTodo = 0;
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
            var todoCalled = false;
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

                if (toolCall.FunctionName == "todo" )
                  todoCalled = true;

                _messages.Add(new ToolChatMessage(toolCall.Id, output));
                _reporter.ReportMessage(output, true);
            }

            turnsWithoutTodo = todoCalled ? 0 : turnsWithoutTodo++;

            if ( turnsWithoutTodo >= 3 && _todoManager.Todos.Count(t => t.Status == TodoStatus.Todo) > 0)
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
