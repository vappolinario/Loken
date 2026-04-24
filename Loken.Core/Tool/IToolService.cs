namespace Loken.Core;

/// <summary>
/// Provides a formatted string of all available tools registered in the system.
/// </summary>
public interface IToolService
{
    /// <summary>
    /// Returns a human-readable string listing all registered tool handlers
    /// with their names and descriptions.
    /// </summary>
    string GetTools();
}
