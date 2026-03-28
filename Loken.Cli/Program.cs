using Loken.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddScoped<IShellExecutor>(sp => new ShellExecutor(workingDirectory: "."));
builder.Services.AddScoped<IChatClient, LiteLlmChatClient>();
builder.Services.AddTransient<Agent, Agent>();
using IHost host = builder.Build();

await RunConsoleLoop(host.Services);

async Task RunConsoleLoop(IServiceProvider services)
{
    var agent = services.GetRequiredService<Agent>();
    Console.WriteLine($"Loken Version: {agent.Version()}");

    while (true)
    {
        Console.Write("❯ ");
        var input = Console.ReadLine()?.Trim();

        if (String.IsNullOrEmpty(input)) continue;

        if (input.ToLower() is "exit" or "quit" or "q") break;

        try
        {
            Console.WriteLine($"❯❯ {await agent.Run(input)}");
        }
        catch (System.Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}
