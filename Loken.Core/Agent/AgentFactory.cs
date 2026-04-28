namespace Loken.Core;

using Microsoft.Extensions.DependencyInjection;

public class AgentFactory : IAgentFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IAgentInstructionsService _agentInstructionsService;

    public AgentFactory(IServiceProvider serviceProvider, IAgentInstructionsService agentInstructionsService)
    {
        _serviceProvider = serviceProvider;
        _agentInstructionsService = agentInstructionsService;
    }

    public Agent CreateSubagent()
    {
        var subAgent = _serviceProvider.GetRequiredService<Agent>();

        var instructions = _agentInstructionsService.LoadInstructions();
        var prompt = instructions is not null
            ? $"{Agent.SubagentPrompt}\n\n# Project Instructions\n\n{instructions}"
            : Agent.SubagentPrompt;

        subAgent.SetSystemPrompt(prompt);
        return subAgent;
    }
}
