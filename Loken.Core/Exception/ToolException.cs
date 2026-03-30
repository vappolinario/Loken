namespace Loken.Core;

public abstract class ToolException(string message) : Exception(message);

public class UnknownToolException(string toolName)
    : ToolException($"Unknown Tool: {toolName}");

public class MissingParameterException(string paramName)
    : ToolException($"Missing parameter: {paramName}");

public class ExecutionFailedException(string details)
    : ToolException($"Execution error: {details}");
