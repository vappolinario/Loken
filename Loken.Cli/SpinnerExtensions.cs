using Spectre.Console;
using System;
using System.Threading.Tasks;

namespace Loken.Cli;

/// <summary>
/// Extension methods for working with spinners.
/// Following C# skill: Proper async patterns and extension method design.
/// </summary>
public static class SpinnerExtensions
{
    /// <summary>
    /// Shows an inline spinner while executing an asynchronous operation.
    /// </summary>
    /// <param name="message">The spinner message.</param>
    /// <param name="asyncAction">The asynchronous action to execute.</param>
    /// <param name="spinner">Optional spinner style. Defaults to Default.</param>
    /// <param name="spinnerColor">Optional spinner color. Defaults to Theme.PrimaryColor.</param>
    /// <returns>The result of the asynchronous operation.</returns>
    public static async Task<T> WithInlineSpinnerAsync<T>(
        string message,
        Func<Task<T>> asyncAction,
        Spinner? spinner = null,
        Color? spinnerColor = null)
    {
        if (asyncAction is null)
            throw new ArgumentNullException(nameof(asyncAction));

        await using var spinnerInstance = SimpleSpinner.Start(message, spinner, spinnerColor);
        
        try
        {
            return await asyncAction();
        }
        catch (Exception ex)
        {
            // Update spinner message to show error
            spinnerInstance.UpdateMessage($"[red]Error: {ex.Message.EscapeMarkup()}[/]");
            await Task.Delay(1000); // Show error briefly
            throw;
        }
    }

    /// <summary>
    /// Shows an inline spinner while executing an asynchronous action.
    /// </summary>
    /// <param name="message">The spinner message.</param>
    /// <param name="asyncAction">The asynchronous action to execute.</param>
    /// <param name="spinner">Optional spinner style. Defaults to Default.</param>
    /// <param name="spinnerColor">Optional spinner color. Defaults to Theme.PrimaryColor.</param>
    public static async Task WithInlineSpinnerAsync(
        string message,
        Func<Task> asyncAction,
        Spinner? spinner = null,
        Color? spinnerColor = null)
    {
        if (asyncAction is null)
            throw new ArgumentNullException(nameof(asyncAction));

        await using var spinnerInstance = SimpleSpinner.Start(message, spinner, spinnerColor);
        
        try
        {
            await asyncAction();
        }
        catch (Exception ex)
        {
            // Update spinner message to show error
            spinnerInstance.UpdateMessage($"[red]Error: {ex.Message.EscapeMarkup()}[/]");
            await Task.Delay(1000); // Show error briefly
            throw;
        }
    }

    /// <summary>
    /// Shows an inline spinner while waiting for assistant or tool responses.
    /// </summary>
    /// <param name="message">The spinner message.</param>
    /// <param name="spinner">Optional spinner style. Defaults to Default.</param>
    /// <param name="spinnerColor">Optional spinner color. Defaults to Theme.PrimaryColor.</param>
    /// <returns>A disposable spinner instance.</returns>
    public static SimpleSpinner ShowAssistantSpinner(
        string message = "Thinking...",
        Spinner? spinner = null,
        Color? spinnerColor = null)
    {
        return SimpleSpinner.Start(message, spinner, spinnerColor);
    }

    /// <summary>
    /// Shows an inline spinner while waiting for tool execution.
    /// </summary>
    /// <param name="toolName">The name of the tool being executed.</param>
    /// <param name="spinner">Optional spinner style. Defaults to Default.</param>
    /// <param name="spinnerColor">Optional spinner color. Defaults to Theme.PrimaryColor.</param>
    /// <returns>A disposable spinner instance.</returns>
    public static SimpleSpinner ShowToolSpinner(
        string toolName,
        Spinner? spinner = null,
        Color? spinnerColor = null)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            throw new ArgumentException("Tool name cannot be null or empty", nameof(toolName));

        var message = $"Executing {toolName}...";
        return SimpleSpinner.Start(message, spinner, spinnerColor);
    }
}