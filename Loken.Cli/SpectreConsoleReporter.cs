using Loken.Core;
using Spectre.Console;

namespace Loken.Cli;

/// <summary>
/// A reporter that uses Spectre.Console for enhanced console output.
/// Following C# skill principles: null safety, proper async patterns, and clear type usage.
/// </summary>
public class SpectreConsoleReporter : IAgentReporter
{
    private readonly IAnsiConsole _console;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpectreConsoleReporter"/> class.
    /// </summary>
    /// <param name="console">The console to write to. If null, uses the default console.</param>
    public SpectreConsoleReporter(IAnsiConsole? console = null)
    {
        // Following C# skill: Use null-coalescing operator for safe defaults
        _console = console ?? AnsiConsole.Console;
    }

    /// <summary>
    /// Reports a message to the console with appropriate styling.
    /// </summary>
    /// <param name="message">The message to report. Handles null gracefully.</param>
    /// <param name="isTool">Whether this message represents a tool call.</param>
    public void ReportMessage(string? message, bool isTool = false)
    {
        // Following C# skill: Handle null messages gracefully
        if (string.IsNullOrWhiteSpace(message))
        {
            _console.WriteLine();
            return;
        }

        var color = isTool ? Theme.SuccessColor : Theme.SecondaryColor;
        var prefix = isTool ? "🛠️ " : "❯❯ ";

        _console.MarkupLine($"[{color}]{prefix.EscapeMarkup()}{message.EscapeMarkup()}[/]");
    }

    /// <summary>
    /// Reports a success message.
    /// </summary>
    /// <param name="message">The success message.</param>
    public void ReportSuccess(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        Theme.DisplaySuccess(message);
    }

    /// <summary>
    /// Shows an inline spinner for tool execution.
    /// </summary>
    /// <param name="toolName">The name of the tool being executed.</param>
    /// <returns>A disposable spinner instance.</returns>
    public SimpleSpinner ShowToolSpinner(string toolName)
    {
        return SpinnerExtensions.ShowToolSpinner(toolName);
    }

    /// <summary>
    /// Shows an inline spinner for assistant thinking.
    /// </summary>
    /// <param name="message">The spinner message.</param>
    /// <returns>A disposable spinner instance.</returns>
    public SimpleSpinner ShowAssistantSpinner(string message = "Thinking...")
    {
        return SpinnerExtensions.ShowAssistantSpinner(message);
    }

    /// <summary>
    /// Reports an error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public void ReportError(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        Theme.DisplayError(message);
    }

    /// <summary>
    /// Reports an informational message.
    /// </summary>
    /// <param name="message">The informational message.</param>
    public void ReportInfo(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        Theme.DisplayInfo(message);
    }

    /// <summary>
    /// Reports a warning message.
    /// </summary>
    /// <param name="message">The warning message.</param>
    public void ReportWarning(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        Theme.DisplayWarning(message);
    }

    /// <summary>
    /// Creates a progress context for tracking long-running operations.
    /// Following C# skill: Proper async patterns.
    /// </summary>
    /// <param name="taskDescription">Description of the overall task.</param>
    /// <returns>A disposable progress context.</returns>
    public async Task<IProgressContext> StartProgressAsync(string taskDescription, CancellationToken cancellationToken = default)
    {
        // Following C# skill: Use proper async patterns, avoid .Result
        return await _console.Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn(),
                new SpinnerColumn { Style = new Style(Theme.SuccessColor) }
            )
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask($"[{Theme.SuccessColor}]{taskDescription.EscapeMarkup()}[/]");
                return new SpectreProgressContext(ctx, task);
            });
    }

    /// <summary>
    /// Displays a table of data.
    /// </summary>
    /// <typeparam name="T">The type of data to display.</typeparam>
    /// <param name="data">The data to display.</param>
    /// <param name="title">Optional table title.</param>
    /// <param name="configure">Optional configuration for the table.</param>
    public void DisplayTable<T>(IEnumerable<T> data, string? title = null, Action<Table>? configure = null)
    {
        // Following C# skill: Use ToList() to avoid multiple enumeration
        var dataList = data?.ToList() ?? new List<T>();

        if (!dataList.Any())
        {
            _console.MarkupLine($"[{Theme.MutedColor}]No data to display.[/]");
            return;
        }

        var table = Theme.CreateTable(title);

        // Auto-detect columns from properties
        var properties = typeof(T).GetProperties();
        foreach (var property in properties)
        {
            table.AddColumn(new TableColumn($"[bold]{property.Name}[/]"));
        }

        // Add rows
        foreach (var item in dataList)
        {
            var values = properties.Select(p =>
            {
                var value = p.GetValue(item);
                return value?.ToString() ?? $"[{Theme.MutedColor}]null[/]";
            }).ToArray();

            table.AddRow(values);
        }

        configure?.Invoke(table);
        _console.Write(table);
    }

    /// <summary>
    /// Displays key-value pairs in a table format.
    /// </summary>
    /// <param name="data">The key-value pairs to display.</param>
    /// <param name="title">Optional table title.</param>
    /// <param name="keyColumnName">Name for the key column.</param>
    /// <param name="valueColumnName">Name for the value column.</param>
    public void DisplayKeyValueTable(
        IEnumerable<KeyValuePair<string, string>> data,
        string? title = null,
        string keyColumnName = "Key",
        string valueColumnName = "Value")
    {
        var dataList = data?.ToList() ?? new List<KeyValuePair<string, string>>();

        if (!dataList.Any())
        {
            _console.MarkupLine($"[{Theme.MutedColor}]No data to display.[/]");
            return;
        }

        var table = Theme.CreateTable(title);
        table.AddColumn(new TableColumn($"[bold]{keyColumnName}[/]"));
        table.AddColumn(new TableColumn($"[bold]{valueColumnName}[/]"));

        foreach (var kvp in dataList)
        {
            table.AddRow(kvp.Key, kvp.Value);
        }

        _console.Write(table);
    }

    /// <summary>
    /// Displays a dictionary in a table format.
    /// </summary>
    /// <typeparam name="TKey">The type of dictionary keys.</typeparam>
    /// <typeparam name="TValue">The type of dictionary values.</typeparam>
    /// <param name="data">The dictionary to display.</param>
    /// <param name="title">Optional table title.</param>
    /// <param name="keyColumnName">Name for the key column.</param>
    /// <param name="valueColumnName">Name for the value column.</param>
    public void DisplayDictionaryTable<TKey, TValue>(
        IDictionary<TKey, TValue> data,
        string? title = null,
        string keyColumnName = "Key",
        string valueColumnName = "Value")
        where TKey : notnull
    {
        if (data == null || data.Count == 0)
        {
            _console.MarkupLine($"[{Theme.MutedColor}]No data to display.[/]");
            return;
        }

        var table = Theme.CreateTable(title);
        table.AddColumn(new TableColumn($"[bold]{keyColumnName}[/]"));
        table.AddColumn(new TableColumn($"[bold]{valueColumnName}[/]"));

        foreach (var kvp in data)
        {
            table.AddRow(kvp.Key?.ToString() ?? $"[{Theme.MutedColor}]null[/]",
                         kvp.Value?.ToString() ?? $"[{Theme.MutedColor}]null[/]");
        }

        _console.Write(table);
    }

    /// <summary>
    /// Prompts the user for input with validation.
    /// </summary>
    /// <param name="prompt">The prompt text.</param>
    /// <param name="validator">Optional validator function.</param>
    /// <returns>The user's input.</returns>
    public string Prompt(string prompt, Func<string, ValidationResult>? validator = null)
    {
        var textPrompt = new TextPrompt<string>($"[yellow]{prompt.EscapeMarkup()}:[/]");

        if (validator != null)
        {
            textPrompt.Validate(validator);
        }

        return _console.Prompt(textPrompt);
    }

    /// <summary>
    /// Prompts the user for a selection from a list.
    /// </summary>
    /// <typeparam name="T">The type of items to select from.</typeparam>
    /// <param name="prompt">The prompt text.</param>
    /// <param name="choices">The available choices.</param>
    /// <param name="converter">Function to convert items to display strings.</param>
    /// <returns>The selected item.</returns>
    public T PromptSelection<T>(string prompt, IEnumerable<T> choices, Func<T, string>? converter = null) where T : notnull
   {
        var selectionPrompt = new SelectionPrompt<T>()
            .Title($"[yellow]{prompt.EscapeMarkup()}[/]")
            .PageSize(10)
            .MoreChoicesText("[grey](Move up and down to reveal more choices)[/]")
            .AddChoices(choices);

        if (converter != null)
        {
            selectionPrompt.UseConverter(converter);
        }

        return _console.Prompt(selectionPrompt);
    }

    /// <summary>
    /// Asks the user for confirmation.
    /// </summary>
    /// <param name="question">The question to ask.</param>
    /// <param name="defaultValue">The default value if user presses enter.</param>
    /// <returns>True if user confirms, false otherwise.</returns>
    public bool Confirm(string question, bool defaultValue = false)
    {
        return _console.Confirm($"[yellow]{question.EscapeMarkup()}[/]", defaultValue);
    }

    /// <summary>
    /// Displays a status message that updates in place.
    /// </summary>
    /// <param name="status">The status message.</param>
    /// <param name="action">The action to perform while showing status.</param>
    public void Status(string status, Action action)
    {
        Theme.DisplayStatus(status, action);
    }

    /// <summary>
    /// Displays a status message that updates in place with async support.
    /// Following C# skill: Proper async patterns.
    /// </summary>
    /// <param name="status">The status message.</param>
    /// <param name="action">The async action to perform.</param>
    public async Task StatusAsync(string status, Func<Task> action)
    {
        // Following C# skill: Use proper async patterns
        await Theme.DisplayStatusAsync(status, action);
    }

    /// <summary>
    /// Displays a status message that can be updated dynamically.
    /// </summary>
    /// <param name="status">The initial status message.</param>
    /// <param name="action">The action to perform with status updates.</param>
    public async Task DynamicStatusAsync(string status, Func<StatusContext, Task> action)
    {
        await _console.Status()
            .AutoRefresh(true)
            .Spinner(Spinner.Known.Default)
            .SpinnerStyle(new Style(Theme.SuccessColor))
            .StartAsync($"[{Theme.SuccessColor}]{status.EscapeMarkup()}[/]", async ctx => await action(ctx));
    }


}

/// <summary>
/// Wrapper for Spectre.Console progress context.
/// Following C# skill: Proper disposal patterns.
/// </summary>
public class SpectreProgressContext : IProgressContext, IDisposable
{
    private readonly ProgressContext _context;
    private readonly ProgressTask _task;
    private bool _disposed;

    public SpectreProgressContext(ProgressContext context, ProgressTask task)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _task = task ?? throw new ArgumentNullException(nameof(task));
    }

    public void Update(double value)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SpectreProgressContext));

        _task.Value = value;
    }

    public void Update(double value, string description)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SpectreProgressContext));

        _task.Value = value;
        _task.Description = description;
    }

    public void Complete()
    {
        if (_disposed)
            return;

        _task.StopTask();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Complete();
        _disposed = true;
    }
}

/// <summary>
/// Interface for progress contexts.
/// Following C# skill: Clear interfaces for dependency injection.
/// </summary>
public interface IProgressContext : IDisposable
{
    void Update(double value);
    void Update(double value, string description);
    void Complete();
}

