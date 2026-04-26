namespace Loken.Core.Options;

public class ConversationSummaryOptions
{
    public const string SectionName = "ConversationSummary";

    public bool? Enabled { get; set; }

    public string? Directory { get; set; }
}
