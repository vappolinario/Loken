using Loken.Core;

namespace Loken.Cli;

public class ConsoleReporter : IAgentReporter
{
    public void ReportMessage(string message, bool isTool = false)
    {
        Console.ForegroundColor = isTool ? ConsoleColor.Green : ConsoleColor.Yellow;
        Console.WriteLine($"❯❯ {message}");
        Console.ResetColor();
    }
}
