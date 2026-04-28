using System.Reflection;
using Loken.Cli;
using Loken.Core;
using Loken.Core.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectre.Console;

HostApplicationBuilder builder = AddServices(args);

ReadConfiguration(builder);

using IHost host = builder.Build();

await RunConsoleLoop(host.Services);

static async Task RunConsoleLoop(IServiceProvider services)
{
  var agent = services.GetRequiredService<Agent>();
  var skills = services.GetRequiredService<ISkillService>();
  var tools = services.GetRequiredService<IToolService>();
  var instructionsService = services.GetRequiredService<IAgentInstructionsService>();
  var instructions = instructionsService.LoadInstructions();
  var systemPrompt = instructions is not null
      ? $"{Agent.LokenPrompt}\n\n# Project Instructions\n\n{instructions}"
      : Agent.LokenPrompt;
  agent.SetSystemPrompt(systemPrompt);
  var reporter = services.GetRequiredService<IAgentReporter>();
  var editorService = services.GetRequiredService<IEditorService>();

  DisplayBanner();

  AnsiConsole.MarkupLine($"[bold {Theme.PrimaryColor}]Type /help if needed[/]");

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

    if (input.ToLower() is "/exit" or "/quit" or "/q" or "exit" or "quit" or "q")
    {
      await HandleExitAsync(services, agent);
      AnsiConsole.MarkupLine("[grey]The Emperor protects. Until next time.[/]");
      break;
    }

    if (input.ToLower() is "/help" or "/?" or "/commands" or "help" or "?" or "commands")
    {
      DisplayHelp();
      continue;
    }

    if (input.ToLower() is "/agents")
    {
      var freshInstructions = instructionsService.LoadInstructions();
      var freshPrompt = freshInstructions is not null
          ? $"{Agent.LokenPrompt}\n\n# Project Instructions\n\n{freshInstructions}"
          : Agent.LokenPrompt;
      agent.SetSystemPrompt(freshPrompt);
      AnsiConsole.MarkupLine(freshInstructions is not null
          ? $"[green]AGENTS.md reloaded. Project instructions refreshed.[/]"
          : $"[yellow]No AGENTS.md found. Project instructions cleared.[/]");
      continue;
    }

    if (input.ToLower() is "/info")
    {
      DisplayInfoPanel(agent, skills, tools);
      continue;
    }

    if (input.ToLower() is "/editor" or "/e")
    {
      AnsiConsole.MarkupLine($"[bold {Theme.PrimaryColor}]Opening editor...[/]");

      var editorContent = await editorService.EditAsync();

      if (string.IsNullOrWhiteSpace(editorContent))
      {
        AnsiConsole.MarkupLine("[yellow]No content captured from editor. Skipping.[/]");
        continue;
      }

      input = editorContent;
      // Fall through to send to agent below
    }

    try
    {
      await using var spinner = SpinnerExtensions.ShowAssistantSpinner("Thinking...");
      await agent.Run(input);
    }
    catch (Exception ex)
    {
      HandleError(ex);
    }

    AnsiConsole.WriteLine();
  }

}

/// <summary>
/// Handles the exit procedure: optionally generates a conversation summary.
/// </summary>
static async Task HandleExitAsync(IServiceProvider services, Agent agent)
{
  try
  {
    var summaryService = services.GetRequiredService<ConversationSummaryService>();

    bool shouldGenerate;

    if (summaryService.ShouldPrompt)
    {
      // Ask the user
      shouldGenerate = AnsiConsole.Confirm("[yellow]Generate a conversation summary for future reference?[/]", true);
    }
    else
    {
      shouldGenerate = summaryService.ShouldGenerate();
    }

    if (shouldGenerate)
    {
      var messages = agent.GetMessages();
      var modelName = services.GetRequiredService<IOptions<AiOptions>>().Value.Model;
      var filePath = await summaryService.GenerateSummaryAsync(messages, modelName);
      if (filePath != null)
      {
        AnsiConsole.MarkupLine($"[green]Conversation summary saved:[/] [cyan]{filePath}[/]");
      }
    }
  }
  catch (Exception ex)
  {
    AnsiConsole.MarkupLine($"[yellow]Failed to generate conversation summary: {ex.Message}[/]");
  }
}

static void DisplayInfoPanel(Agent agent, ISkillService skills, IToolService tools)
{
  var infoPanel = Theme.CreatePanel(
      $"[bold]Version:[/] {agent.Version()}\n" +
      $"[bold]Loaded Skills:[/] {skills.GetSkills()}\n" +
      $"[bold]Available Tools:[/]\n{tools.GetTools()}",
      "System Status");

  AnsiConsole.Write(infoPanel);
  AnsiConsole.WriteLine();
}

static void DisplayHelp()
{
  var helpTable = Theme.CreateTable("Available Commands");

  helpTable.AddColumn("Command");
  helpTable.AddColumn("Description");
  helpTable.AddColumn("Aliases");

  helpTable.AddRow(
      "[bold]/help[/]",
      "Show this help message",
      "/?, /commands, help, ?, commands");

  helpTable.AddRow(
      "[bold]/agents[/]",
      "Reload AGENTS.md project instructions",
      "—");

  helpTable.AddRow(
      "[bold]/info[/]",
      "Show system status (version, skills, tools)",
      "—");

  helpTable.AddRow(
      "[bold]/editor[/]",
      "Open $EDITOR to compose a long prompt",
      "/e");

  helpTable.AddRow(
      "[bold]/exit[/]",
      "Exit the application",
      "/quit, /q, exit, quit, q");

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
  var assembly = Assembly.GetExecutingAssembly();
  using var stream = assembly.GetManifestResourceStream("Loken.Cli.Assets.Fonts.Elite.flf")
                     ?? throw new Exception("Fonte não encontrada nos recursos internos.");
  var font = FigletFont.Load(stream);
  var fontPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", "Elite.flf");

  AnsiConsole.Write(new FigletText(font, "LOKEN").Color(Theme.PrimaryColor));
  AnsiConsole.MarkupLine($"[bold {Theme.PrimaryColor}]The Emperor's Truth in Code[/]");
  AnsiConsole.WriteLine();
}

static HostApplicationBuilder AddServices(string[] args)
{
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
  builder.Services.AddTransient<IToolHandler, HtmlFetcherHandler>();
  builder.Services.AddHttpClient();
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
  builder.Services.AddSingleton<IAgentInstructionsService, AgentInstructionsService>();
  builder.Services.AddTransient<IToolHandler, AgentInstructionsHandler>();

  builder.Services.AddSingleton<IToolService, ToolService>();

  builder.Services.AddTransient<IEditorService, SystemEditorService>();

  builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.None);
  builder.Logging.AddFilter("System.Net.Http.HttpClient.Default.LogicalHandler", LogLevel.None);
  builder.Logging.AddFilter("System.Net.Http.HttpClient.Default.ClientHandler", LogLevel.None);

  return builder;
}

static void ReadConfiguration(HostApplicationBuilder builder)
{
  builder.Configuration.Sources.Clear();

  string configDirectory;
#if DEBUG
  configDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? Directory.GetCurrentDirectory();
#else
    configDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "loken"
    );

    if (!Directory.Exists(configDirectory))
    {
        Directory.CreateDirectory(configDirectory);
    }
#endif

  builder.Configuration
    .SetBasePath(configDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
  builder.Services.Configure<AiOptions>(builder.Configuration.GetSection("AI"));
  builder.Services.Configure<SkillOptions>(builder.Configuration.GetSection("Skills"));
  builder.Services.Configure<ConversationSummaryOptions>(builder.Configuration.GetSection("ConversationSummary"));
  builder.Services.AddTransient<ConversationSummaryService>();
}

static void HandleError(Exception ex)
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
