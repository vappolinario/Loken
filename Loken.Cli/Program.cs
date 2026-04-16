using System.Reflection;
using Loken.Cli;
using Loken.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Spectre.Console;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddTransient<IChatClient, OpenAiChatClient>();
builder.Services.AddSingleton<IAgentReporter, SpectreConsoleReporter>();
builder.Services.AddTransient<Agent>();
builder.Services.AddSingleton<IAgentFactory, AgentFactory>();
builder.Services.AddSingleton<IPathResolver, PathResolver>(_ => new PathResolver("."));
builder.Services.AddTransient<IToolHandler, ShellExecutorHandler>();
builder.Services.AddTransient<IToolHandler, FileReaderHandler>();
builder.Services.AddTransient<IToolHandler, FileWriterHandler>();
builder.Services.AddTransient<IToolHandler, FileEditorHandler>();
builder.Services.AddTransient<TodoManager>();
builder.Services.AddSingleton<ITodoService, TodoService>();
builder.Services.AddTransient<IToolHandler, TodoHandler>();
builder.Services.AddTransient<IToolHandler, SubagentHandler>();
builder.Services.AddTransient(sp =>
{
  var options = sp.GetRequiredService<IOptions<SkillOptions>>();
  var skillsPath = options.Value.SkillsPath;

  if (string.IsNullOrWhiteSpace(skillsPath))
    skillsPath = Path.Combine(".", "Assets", "skills");

  return new SkillLoader(skillsPath);
});
builder.Services.AddTransient<ISkillService, SkillService>();
builder.Services.AddTransient<IToolHandler, SkillHandler>();
builder.Services.AddTransient<IContextCompactorService, ContextCompactorService>();

builder.Configuration.Sources.Clear();
builder.Configuration
  .SetBasePath(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? Directory.GetCurrentDirectory())
  .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
builder.Services.Configure<AiOptions>(builder.Configuration.GetSection("AI"));
builder.Services.Configure<SkillOptions>(builder.Configuration.GetSection("Skills"));

using IHost host = builder.Build();

await RunConsoleLoop(host.Services);

static async Task RunConsoleLoop(IServiceProvider services)
{
  var agent = services.GetRequiredService<Agent>();
  var skills = services.GetRequiredService<ISkillService>();
  agent.SetSystemPrompt(Agent.LokenPrompt);
  var reporter = services.GetRequiredService<IAgentReporter>();

  DisplayBanner();

  var infoPanel = Theme.CreatePanel(
      $"[bold]Version:[/] {agent.Version()}\n" +
      $"[bold]Loaded Skills:[/] {skills.GetSkills()}",
      "System Status");

  AnsiConsole.Write(infoPanel);
  AnsiConsole.WriteLine();

  while (true)
  {
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

    if (input.ToLower() is "help" or "?" or "commands")
    {
      DisplayHelp();
      continue;
    }

    try
    {
      await using var spinner = SpinnerExtensions.ShowAssistantSpinner("Thinking...");
      await agent.Run(input);
    }
    catch (Exception ex)
    {
      AnsiConsole.MarkupLine($"[red]Error: {ex.Message.EscapeMarkup()}[/]");

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

static void DisplayBanner()
{
  var fontPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", "Elite.flf");

  if (!File.Exists(fontPath))
  {
    AnsiConsole.Write(new FigletText("LOKEN").Color(Theme.PrimaryColor));
  }
  else
  {
    var font = FigletFont.Load(fontPath);
    AnsiConsole.Write(new FigletText(font, "LOKEN").Color(Theme.PrimaryColor));
  }

  AnsiConsole.MarkupLine($"[bold {Theme.PrimaryColor}]The Emperor's Truth in Code[/]");
  AnsiConsole.WriteLine();
}
