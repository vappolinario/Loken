using Spectre.Console;
using System;
using System.Threading.Tasks;

namespace Loken.Cli;

public static class SpinnerExtensions
{
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
            spinnerInstance.UpdateMessage($"[red]Error: {ex.Message.EscapeMarkup()}[/]");
            await Task.Delay(1000); // Show error briefly
            throw;
        }
    }

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
            spinnerInstance.UpdateMessage($"[red]Error: {ex.Message.EscapeMarkup()}[/]");
            await Task.Delay(1000); // Show error briefly
            throw;
        }
    }

    public static SimpleSpinner ShowAssistantSpinner(
        string message = "Thinking...",
        Spinner? spinner = null,
        Color? spinnerColor = null)
    {
        return SimpleSpinner.Start(message, spinner, spinnerColor);
    }

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
