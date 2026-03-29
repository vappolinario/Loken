using System.Reflection;
using Loken.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddScoped<IShellExecutor>(sp => new ShellExecutor(workingDirectory: "."));
builder.Services.AddScoped<IChatClient, LiteLlmChatClient>();
builder.Services.AddTransient<Agent, Agent>();
builder.Services.AddSingleton<IAgentReporter, Loken.Cli.ConsoleReporter>();
builder.Services.AddScoped<Agent>();

builder.Configuration.Sources.Clear();
builder.Configuration
  .SetBasePath(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? Directory.GetCurrentDirectory())
  .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
builder.Services.Configure<AiOptions>(builder.Configuration.GetSection("AI"));

using IHost host = builder.Build();

await RunConsoleLoop(host.Services);

async Task RunConsoleLoop(IServiceProvider services)
{
    var agent = services.GetRequiredService<Agent>();
    var reporter = services.GetRequiredService<IAgentReporter>();
    Console.WriteLine($"Loken Version: {agent.Version()}");

    while (true)
    {
        Console.Write("❯ ");
        var input = Console.ReadLine()?.Trim();

        if (String.IsNullOrEmpty(input)) continue;

        if (input.ToLower() is "exit" or "quit" or "q") break;

        try
        {
            await agent.Run(input);
        }
        catch (System.Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}
