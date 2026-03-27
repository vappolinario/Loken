namespace Loken.Core;

using System;
using System.Collections.Generic;
using System.Text.Json;
using OpenAI.Chat;

public class Agent
{
    private List<ChatMessage> _messages;
    private IShellExecutor _executor;
    private IChatClient _chatClient;
    private ChatCompletionOptions _options;

    public Agent(IShellExecutor shellExecutor, IChatClient chatClient)
    {
        _messages = new List<ChatMessage>()
        {
          new SystemChatMessage(    """
    You are a coding agent. You help the user by executing
    shell commands to explore the filesystem, read and write files, run programs,
    and accomplish tasks.

    Guidelines:
    - Use the bash tool to execute commands
    - Always check the result of commands before proceeding
    - If a command fails, try to understand why and fix it
    - Be concise in your explanations
    - When editing files, show the relevant changes
    """)
        };

        var schema = new
        {
            type = "object",
            properties = new
            {
                command = new
                {
                    type = "string",
                    description = "The shell command to execute"
                }
            },
            required = new[] { "command" }
        };

        _options = new()
        {
            Tools = { ChatTool.CreateFunctionTool(
                      functionName: "bash",
                      functionDescription: "Run a shell command and return its output.",
                      functionParameters: BinaryData.FromObjectAsJson(schema)
                    )
            }
        };

        _chatClient = chatClient;
        _executor = shellExecutor;
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
            _messages.Add(new AssistantChatMessage(result));

            if (result.Value.FinishReason != ChatFinishReason.ToolCalls)
                return result.Value.Content[0].Text;

            foreach (var toolCall in result.Value.ToolCalls)
            {
                var toolResult = await ExecuteToolAsync(toolCall.FunctionName, toolCall.FunctionArguments);
                _messages.Add(new ToolChatMessage(toolCall.Id, toolResult));
            }
        }
    }

    public async Task<string> ExecuteToolAsync(string name, BinaryData input)
    {
        if (name != "bash")
            throw new Exception($"Unknown tool: {name}");

        var json = JsonDocument.Parse(input);

        if (!json.RootElement.TryGetProperty("command", out var commandProperty) ||
            commandProperty.GetString() is not string command)
              return "Error: Missing parameter 'command'";

        try
        {
            var result = await _executor.ExecuteAsync(command);
            return result.Formatted;
        }
        catch (Exception ex)
        {
            return $"Execution failed: {ex.Message}";
        }
    }
}
