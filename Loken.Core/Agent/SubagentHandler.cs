using System.Text.Json;

namespace Loken.Core;

public class SubagentHandler : IToolHandler
{
    private readonly IAgentFactory _agentFactory;

    public string Name => "subagent";

    public string Description => "Spawn a subagent to handle a complex subtask independently.";

    public BinaryData Parameters => BinaryData.FromObjectAsJson(
                new
                {
                    type = "object",
                    properties = new
                    {
                        prompt = new
                        {
                            type = "string",
                            description = "The task for the subagent to complete"
                        },
                    },
                    required = new[] { "prompt" }
                });


    public SubagentHandler(IAgentFactory agentFactory)
    {
        _agentFactory = agentFactory;
    }

    public async Task<string> ExecuteAsync(BinaryData input)
    {
        try
        {
            using var doc = JsonDocument.Parse(input);
            var prompt = doc.RootElement.GetProperty("prompt").GetString()
                       ?? throw new MissingParameterException("prompt");

            var agent = _agentFactory.CreateSubagent();
            return await agent.Run(prompt);
        }
        catch (JsonException)
        {
            throw new ExecutionFailedException("Invalid Json.");
        }
        catch (Exception ex)
        {
            throw new ExecutionFailedException($"Failed to spawn subagent: {ex.Message}");
        }
    }
}
