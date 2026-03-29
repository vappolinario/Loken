namespace Loken.Core;

public interface IAgentReporter
{
    void ReportMessage(string message, bool isTool);
}
