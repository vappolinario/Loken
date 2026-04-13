using Spectre.Console;

namespace Loken.Cli;

/// <summary>
/// Provides consistent theming and styling for the Loken CLI.
/// Following C# skill: Static classes for utility functions.
/// </summary>
public static class Theme
{
    /// <summary>
    /// Gets the primary color for the application.
    /// </summary>
    public static Color PrimaryColor => Color.Blue;

    /// <summary>
    /// Gets the secondary color for the application.
    /// </summary>
    public static Color SecondaryColor => Color.Yellow;

    /// <summary>
    /// Gets the success color.
    /// </summary>
    public static Color SuccessColor => Color.Green;

    /// <summary>
    /// Gets the error color.
    /// </summary>
    public static Color ErrorColor => Color.Red;

    /// <summary>
    /// Gets the warning color.
    /// </summary>
    public static Color WarningColor => Color.Orange3;

    /// <summary>
    /// Gets the info color.
    /// </summary>
    public static Color InfoColor => Color.Cyan1;

    /// <summary>
    /// Gets the muted color for less important text.
    /// </summary>
    public static Color MutedColor => Color.Grey;

    /// <summary>
    /// Creates a styled panel with consistent theming.
    /// </summary>
    /// <param name="content">The panel content.</param>
    /// <param name="title">The panel title.</param>
    /// <param name="borderColor">Optional border color override.</param>
    /// <returns>A styled panel.</returns>
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

    /// <summary>
    /// Creates a styled table with consistent theming.
    /// </summary>
    /// <param name="title">Optional table title.</param>
    /// <returns>A styled table.</returns>
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

    /// <summary>
    /// Creates a styled prompt with consistent theming.
    /// </summary>
    /// <typeparam name="T">The type of the prompt result.</typeparam>
    /// <param name="promptText">The prompt text.</param>
    /// <returns>A styled text prompt.</returns>
    public static TextPrompt<T> CreateTextPrompt<T>(string promptText)
    {
        return new TextPrompt<T>($"[{SecondaryColor}]{promptText.EscapeMarkup()}:[/]")
            .PromptStyle(SecondaryColor.ToString());
    }

    /// <summary>
    /// Creates a styled selection prompt with consistent theming.
    /// </summary>
    /// <typeparam name="T">The type of items to select from.</typeparam>
    /// <param name="promptText">The prompt text.</param>
    /// <returns>A styled selection prompt.</returns>
    public static SelectionPrompt<T> CreateSelectionPrompt<T>(string promptText) where T : notnull
    {
        return new SelectionPrompt<T>()
            .Title($"[{SecondaryColor}]{promptText.EscapeMarkup()}[/]")
            .PageSize(10)
            .MoreChoicesText($"[{MutedColor}](Move up and down to reveal more choices)[/]")
            .HighlightStyle(new Style(SecondaryColor, decoration: Decoration.Bold));
    }

    /// <summary>
    /// Creates a styled confirmation prompt with consistent theming.
    /// </summary>
    /// <param name="question">The question to ask.</param>
    /// <param name="defaultValue">The default value.</param>
    /// <returns>A styled confirmation prompt.</returns>
    public static ConfirmationPrompt CreateConfirmationPrompt(string question, bool defaultValue = false)
    {
        return new ConfirmationPrompt($"[{SecondaryColor}]{question.EscapeMarkup()}[/]")
        {
            DefaultValue = defaultValue,
            ShowDefaultValue = true
        };
    }

    /// <summary>
    /// Displays a success message with consistent styling.
    /// </summary>
    /// <param name="message">The success message.</param>
    public static void DisplaySuccess(string message)
    {
        AnsiConsole.MarkupLine($"[{SuccessColor}]✓ {message.EscapeMarkup()}[/]");
    }

    /// <summary>
    /// Displays an error message with consistent styling.
    /// </summary>
    /// <param name="message">The error message.</param>
    public static void DisplayError(string message)
    {
        AnsiConsole.MarkupLine($"[{ErrorColor}]✗ {message.EscapeMarkup()}[/]");
    }

    /// <summary>
    /// Displays an info message with consistent styling.
    /// </summary>
    /// <param name="message">The info message.</param>
    public static void DisplayInfo(string message)
    {
        AnsiConsole.MarkupLine($"[{InfoColor}]ℹ {message.EscapeMarkup()}[/]");
    }

    /// <summary>
    /// Displays a warning message with consistent styling.
    /// </summary>
    /// <param name="message">The warning message.</param>
    public static void DisplayWarning(string message)
    {
        AnsiConsole.MarkupLine($"[{WarningColor}]⚠ {message.EscapeMarkup()}[/]");
    }

    /// <summary>
    /// Displays a header with consistent styling.
    /// </summary>
    /// <param name="text">The header text.</param>
    public static void DisplayHeader(string text)
    {
        AnsiConsole.Write(new Rule($"[bold {PrimaryColor}]{text.EscapeMarkup()}[/]")
        {
            Justification = Justify.Left,
            Style = new Style(PrimaryColor)
        });
    }

    /// <summary>
    /// Displays a separator with consistent styling.
    /// </summary>
    public static void DisplaySeparator()
    {
        AnsiConsole.Write(new Rule()
        {
            Style = new Style(MutedColor)
        });
    }

    /// <summary>
    /// Displays a status with consistent styling.
    /// </summary>
    /// <param name="status">The status message.</param>
    /// <param name="action">The action to perform.</param>
    public static void DisplayStatus(string status, Action action)
    {
        AnsiConsole.Status()
            .AutoRefresh(true)
            .Spinner(Spinner.Known.Default)
            .SpinnerStyle(new Style(SuccessColor))
            .Start($"[{SuccessColor}]{status.EscapeMarkup()}[/]", ctx => action());
    }

    /// <summary>
    /// Displays a status with consistent styling (async version).
    /// Following C# skill: Proper async patterns.
    /// </summary>
    /// <param name="status">The status message.</param>
    /// <param name="action">The async action to perform.</param>
    public static async Task DisplayStatusAsync(string status, Func<Task> action)
    {
        await AnsiConsole.Status()
            .AutoRefresh(true)
            .Spinner(Spinner.Known.Default)
            .SpinnerStyle(new Style(SuccessColor))
            .StartAsync($"[{SuccessColor}]{status.EscapeMarkup()}[/]", async ctx => await action());
    }
}
