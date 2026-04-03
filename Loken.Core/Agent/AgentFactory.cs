namespace Loken.Core;

using Microsoft.Extensions.DependencyInjection;

public class AgentFactory : IAgentFactory
{
    private readonly IServiceProvider _serviceProvider;

    public AgentFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Agent CreateSubagent()
    {
        var subAgent = _serviceProvider.GetRequiredService<Agent>();
        subAgent.SetSystemPrompt(Agent.SubagentPrompt);
        return subAgent;
    }
}
