using System.Reflection;
using Loken.Cli;
using Loken.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddTransient<IChatClient, OpenAiChatClient>();
builder.Services.AddSingleton<IAgentReporter, SpectreConsoleReporter>();
builder.Services.AddTransient<Agent>();
builder.Services.AddSingleton<IAgentFactory, AgentFactory>();
builder.Services.AddSingleton<IPathResolver, PathResolver>(pr => new PathResolver("."));
builder.Services.AddTransient<IToolHandler, ShellExecutorHandler>();
builder.Services.AddTransient<IToolHandler, FileReaderHandler>();
builder.Services.AddTransient<IToolHandler, FileWriterHandler>();
builder.Services.AddTransient<IToolHandler, FileEditorHandler>();
builder.Services.AddTransient<TodoManager>();
builder.Services.AddSingleton<ITodoService, TodoService>();
builder.Services.AddTransient<IToolHandler, TodoHandler>();
builder.Services.AddTransient<IToolHandler, SubagentHandler>();
builder.Services.AddTransient<SkillLoader>(sl => new SkillLoader(Path.Combine(".", "skills")));
builder.Services.AddTransient<ISkillService, SkillService>();
builder.Services.AddTransient<IToolHandler, SkillHandler>();
builder.Services.AddTransient<IContextCompactorService, ContextCompactorService>();

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
    var skills = services.GetRequiredService<ISkillService>();
    agent.SetSystemPrompt(Agent.LokenPrompt);
    var reporter = services.GetRequiredService<IAgentReporter>();

    // Display welcome banner
    AnsiConsole.Write(new FigletText("LOKEN").Color(Theme.PrimaryColor));
    AnsiConsole.MarkupLine($"[bold {Theme.PrimaryColor}]The Emperor's Truth in Code[/]");
    AnsiConsole.WriteLine();

    // Display version and skills in a panel
    var infoPanel = Theme.CreatePanel(
        $"[bold]Version:[/] {agent.Version()}\n" +
        $"[bold]Loaded Skills:[/] {skills.GetSkills()}",
        "System Status");

    AnsiConsole.Write(infoPanel);
    AnsiConsole.WriteLine();

    while (true)
    {
        // Use Spectre.Console prompt for better UX
        var input = AnsiConsole.Prompt(
            new TextPrompt<string>("[bold yellow]❯[/]")
                .PromptStyle("yellow")
                .AllowEmpty()
                .ValidationErrorMessage("[red]Invalid input[/]")
        );

        if (string.IsNullOrWhiteSpace(input))
            continue;

        if (input.ToLower() is "exit" or "quit" or "q")
        {
            AnsiConsole.MarkupLine("[grey]The Emperor protects. Until next time.[/]");
            break;
        }

        // Handle special commands
        if (input.ToLower() is "help" or "?" or "commands")
        {
            DisplayHelp();
            continue;
        }

        try
        {
            await agent.Run(input);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message.EscapeMarkup()}[/]");

            // Offer to show full exception details
            if (AnsiConsole.Confirm("[yellow]Show full error details?[/]", false))
            {
                var exceptionPanel = Theme.CreatePanel(
                    ex.ToString(),
                    "Exception Details",
                    Theme.ErrorColor
                );
                AnsiConsole.Write(exceptionPanel);
            }
        }

        AnsiConsole.WriteLine();
    }
}

/// <summary>
/// Displays help information for available commands.
/// </summary>
static void DisplayHelp()
{
    var helpTable = Theme.CreateTable("Available Commands");

    helpTable.AddColumn("Command");
    helpTable.AddColumn("Description");
    helpTable.AddColumn("Aliases");

    helpTable.AddRow(
        "[bold]help[/]",
        "Show this help message",
        "?, commands");

    helpTable.AddRow(
        "[bold]exit[/]",
        "Exit the application",
        "quit, q");

    helpTable.AddRow(
        "[bold]any other text[/]",
        "Process as a command for the Loken agent",
        "—");

    AnsiConsole.Write(helpTable);
    AnsiConsole.WriteLine();

    AnsiConsole.MarkupLine($"[{Theme.InfoColor}]The Loken agent can handle various tasks including file operations, shell commands, todo management, and more.[/]");
    AnsiConsole.MarkupLine($"[{Theme.InfoColor}]Type your command naturally and Loken will determine the appropriate action.[/]");
    AnsiConsole.WriteLine();
}
