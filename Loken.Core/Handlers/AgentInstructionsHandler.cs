using System.Text.Json;

namespace Loken.Core;

public class AgentInstructionsHandler : IToolHandler
{
    private readonly IAgentInstructionsService _agentInstructionsService;

    public string Name => "load_project_instructions";

    public string Description => "Load project-specific instructions from the nearest AGENTS.md file.";

    public BinaryData Parameters => BinaryData.FromObjectAsJson(
        new
        {
            type = "object",
            properties = new object(),
            required = Array.Empty<string>()
        });

    public AgentInstructionsHandler(IAgentInstructionsService agentInstructionsService)
    {
        _agentInstructionsService = agentInstructionsService;
    }

    public Task<string> ExecuteAsync(BinaryData input)
    {
        var instructions = _agentInstructionsService.LoadInstructions();

        if (instructions is null)
            return Task.FromResult("No AGENTS.md found in the project directory tree.");

        return Task.FromResult(instructions);
    }
}
