using Spectre.Console;

namespace Loken.Cli;

public static class Theme
{
    public static Color PrimaryColor => Color.Blue;

    public static Color SecondaryColor => Color.Yellow;

    public static Color SuccessColor => Color.Green;

    public static Color ErrorColor => Color.Red;

    public static Color WarningColor => Color.Orange3;

    public static Color InfoColor => Color.Cyan1;

    public static Color MutedColor => Color.Grey;

    public static Panel CreatePanel(string content, string title, Color? borderColor = null)
    {
        return new Panel(content)
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(borderColor ?? PrimaryColor),
            Header = new PanelHeader($"[bold {PrimaryColor}]{title.EscapeMarkup()}[/]"),
            Padding = new Padding(1, 1, 1, 1)
        };
    }

    public static Table CreateTable(string? title = null)
    {
        var table = new Table();

        if (!string.IsNullOrWhiteSpace(title))
        {
            table.Title = new TableTitle($"[bold {PrimaryColor}]{title.EscapeMarkup()}[/]");
        }

        table.Border = TableBorder.Rounded;
        table.BorderStyle = new Style(PrimaryColor);
        table.Expand = true;

        return table;
    }

    public static TextPrompt<T> CreateTextPrompt<T>(string promptText)
    {
        return new TextPrompt<T>($"[{SecondaryColor}]{promptText.EscapeMarkup()}:[/]")
            .PromptStyle(SecondaryColor.ToString());
    }

    public static SelectionPrompt<T> CreateSelectionPrompt<T>(string promptText) where T : notnull
    {
        return new SelectionPrompt<T>()
            .Title($"[{SecondaryColor}]{promptText.EscapeMarkup()}[/]")
            .PageSize(10)
            .MoreChoicesText($"[{MutedColor}](Move up and down to reveal more choices)[/]")
            .HighlightStyle(new Style(SecondaryColor, decoration: Decoration.Bold));
    }

    public static ConfirmationPrompt CreateConfirmationPrompt(string question, bool defaultValue = false)
    {
        return new ConfirmationPrompt($"[{SecondaryColor}]{question.EscapeMarkup()}[/]")
        {
            DefaultValue = defaultValue,
            ShowDefaultValue = true
        };
    }

    public static void DisplaySuccess(string message)
    {
        AnsiConsole.MarkupLine($"[{SuccessColor}]✓ {message.EscapeMarkup()}[/]");
    }

    public static void DisplayError(string message)
    {
        AnsiConsole.MarkupLine($"[{ErrorColor}]✗ {message.EscapeMarkup()}[/]");
    }

    public static void DisplayInfo(string message)
    {
        AnsiConsole.MarkupLine($"[{InfoColor}]ℹ {message.EscapeMarkup()}[/]");
    }

    public static void DisplayWarning(string message)
    {
        AnsiConsole.MarkupLine($"[{WarningColor}]⚠ {message.EscapeMarkup()}[/]");
    }

    public static void DisplayHeader(string text)
    {
        AnsiConsole.Write(new Rule($"[bold {PrimaryColor}]{text.EscapeMarkup()}[/]")
        {
            Justification = Justify.Left,
            Style = new Style(PrimaryColor)
        });
    }

    public static void DisplaySeparator()
    {
        AnsiConsole.Write(new Rule()
        {
            Style = new Style(MutedColor)
        });
    }

    public static void DisplayStatus(string status, Action action)
    {
        AnsiConsole.Status()
            .AutoRefresh(true)
            .Spinner(Spinner.Known.Default)
            .SpinnerStyle(new Style(SuccessColor))
            .Start($"[{SuccessColor}]{status.EscapeMarkup()}[/]", ctx => action());
    }

    public static async Task DisplayStatusAsync(string status, Func<Task> action)
    {
        await AnsiConsole.Status()
            .AutoRefresh(true)
            .Spinner(Spinner.Known.Default)
            .SpinnerStyle(new Style(SuccessColor))
            .StartAsync($"[{SuccessColor}]{status.EscapeMarkup()}[/]", async ctx => await action());
    }
}
