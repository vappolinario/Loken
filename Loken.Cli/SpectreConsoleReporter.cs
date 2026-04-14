using Loken.Core;
using Spectre.Console;

namespace Loken.Cli;

public class SpectreConsoleReporter : IAgentReporter
{
    private readonly IAnsiConsole _console;

    public SpectreConsoleReporter(IAnsiConsole? console = null)
    {
        _console = console ?? AnsiConsole.Console;
    }

    public void ReportMessage(string? message, bool isTool = false)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            _console.WriteLine();
            return;
        }

        var color = isTool ? Theme.SuccessColor : Theme.SecondaryColor;
        var prefix = isTool ? "🛠️ " : "❯❯ ";

        _console.MarkupLine($"[{color}]{prefix.EscapeMarkup()}{message.EscapeMarkup()}[/]");
    }

    public void ReportSuccess(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        Theme.DisplaySuccess(message);
    }

    public SimpleSpinner ShowToolSpinner(string toolName)
    {
        return SpinnerExtensions.ShowToolSpinner(toolName);
    }

    public SimpleSpinner ShowAssistantSpinner(string message = "Thinking...")
    {
        return SpinnerExtensions.ShowAssistantSpinner(message);
    }

    public void ReportError(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        Theme.DisplayError(message);
    }

    public void ReportInfo(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        Theme.DisplayInfo(message);
    }

    public void ReportWarning(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        Theme.DisplayWarning(message);
    }

    public async Task<IProgressContext> StartProgressAsync(string taskDescription, CancellationToken cancellationToken = default)
    {
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

    public void DisplayTable<T>(IEnumerable<T> data, string? title = null, Action<Table>? configure = null)
    {
        var dataList = data?.ToList() ?? new List<T>();

        if (!dataList.Any())
        {
            _console.MarkupLine($"[{Theme.MutedColor}]No data to display.[/]");
            return;
        }

        var table = Theme.CreateTable(title);

        var properties = typeof(T).GetProperties();
        foreach (var property in properties)
        {
            table.AddColumn(new TableColumn($"[bold]{property.Name}[/]"));
        }

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

    public string Prompt(string prompt, Func<string, ValidationResult>? validator = null)
    {
        var textPrompt = new TextPrompt<string>($"[yellow]{prompt.EscapeMarkup()}:[/]");

        if (validator != null)
        {
            textPrompt.Validate(validator);
        }

        return _console.Prompt(textPrompt);
    }

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

    public bool Confirm(string question, bool defaultValue = false)
    {
        return _console.Confirm($"[yellow]{question.EscapeMarkup()}[/]", defaultValue);
    }

    public void Status(string status, Action action)
    {
        Theme.DisplayStatus(status, action);
    }

    public async Task StatusAsync(string status, Func<Task> action)
    {
        await Theme.DisplayStatusAsync(status, action);
    }

    public async Task DynamicStatusAsync(string status, Func<StatusContext, Task> action)
    {
        await _console.Status()
            .AutoRefresh(true)
            .Spinner(Spinner.Known.Default)
            .SpinnerStyle(new Style(Theme.SuccessColor))
            .StartAsync($"[{Theme.SuccessColor}]{status.EscapeMarkup()}[/]", async ctx => await action(ctx));
    }


}

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

public interface IProgressContext : IDisposable
{
    void Update(double value);
    void Update(double value, string description);
    void Complete();
}

