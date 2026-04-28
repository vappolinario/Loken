using Loken.Core.Options;
using Loken.Core.Services;
using Spectre.Console;

namespace Loken.Cli;

public static class Theme
{
    public static Color PrimaryColor { get; private set; } = Color.Blue;
    public static Color SecondaryColor { get; private set; } = Color.Yellow;
    public static Color SuccessColor { get; private set; } = Color.Green;
    public static Color ErrorColor { get; private set; } = Color.Red;
    public static Color WarningColor { get; private set; } = Color.Orange3;
    public static Color InfoColor { get; private set; } = Color.Cyan1;
    public static Color MutedColor { get; private set; } = Color.Grey;

    public static void LoadFromFile(string path)
    {
        var loader = new ThemeLoader();
        var options = loader.Load(path);

        PrimaryColor = ParseColor(options.Primary, Color.Blue);
        SecondaryColor = ParseColor(options.Secondary, Color.Yellow);
        SuccessColor = ParseColor(options.Success, Color.Green);
        ErrorColor = ParseColor(options.Error, Color.Red);
        WarningColor = ParseColor(options.Warning, Color.Orange3);
        InfoColor = ParseColor(options.Info, Color.Cyan1);
        MutedColor = ParseColor(options.Muted, Color.Grey);
    }

    public static void ApplyOptions(ThemeOptions options)
    {
        PrimaryColor = ParseColor(options.Primary, Color.Blue);
        SecondaryColor = ParseColor(options.Secondary, Color.Yellow);
        SuccessColor = ParseColor(options.Success, Color.Green);
        ErrorColor = ParseColor(options.Error, Color.Red);
        WarningColor = ParseColor(options.Warning, Color.Orange3);
        InfoColor = ParseColor(options.Info, Color.Cyan1);
        MutedColor = ParseColor(options.Muted, Color.Grey);
    }

    private static Color ParseColor(string hex, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return fallback;

        try
        {
            return FromHex(hex);
        }
        catch
        {
            return fallback;
        }
    }

    private static Color FromHex(string hex)
    {
        hex = hex.TrimStart('#');

        if (hex.Length != 6)
            throw new ArgumentException($"Invalid hex color: #{hex}");

        var r = byte.Parse(hex[..2], System.Globalization.NumberStyles.HexNumber);
        var g = byte.Parse(hex[2..4], System.Globalization.NumberStyles.HexNumber);
        var b = byte.Parse(hex[4..6], System.Globalization.NumberStyles.HexNumber);

        return new Color(r, g, b);
    }

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
