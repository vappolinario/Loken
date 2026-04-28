namespace Loken.Core.Options;

public class ThemeOptions
{
    public const string SectionName = "Theme";

    public string Primary { get; init; } = "#0000FF";
    public string Secondary { get; init; } = "#FFFF00";
    public string Success { get; init; } = "#00FF00";
    public string Error { get; init; } = "#FF0000";
    public string Warning { get; init; } = "#FFA500";
    public string Info { get; init; } = "#00FFFF";
    public string Muted { get; init; } = "#808080";
}
