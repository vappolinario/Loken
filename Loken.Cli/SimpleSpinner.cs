using Spectre.Console;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Loken.Cli;

/// <summary>
/// Provides a simple inline spinner that can be shown while maintaining the rest of the UI.
/// This uses a simpler approach than LiveDisplay for better compatibility.
/// Following C# skill: Proper async patterns and cancellation support.
/// </summary>
public sealed class SimpleSpinner : IAsyncDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task _spinnerTask;
    private readonly string _initialMessage;
    private readonly Spinner _spinner;
    private readonly Color _spinnerColor;
    private bool _disposed;
    private int _cursorTop;
    private int _consoleBufferHeightAtStart;

    /// <summary>
    /// Gets a value indicating whether the spinner is currently running.
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Gets or sets the current spinner message.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleSpinner"/> class.
    /// </summary>
    /// <param name="initialMessage">The initial spinner message.</param>
    /// <param name="spinner">The spinner style.</param>
    /// <param name="spinnerColor">The spinner color.</param>
    private SimpleSpinner(string initialMessage, Spinner spinner, Color spinnerColor)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _initialMessage = initialMessage ?? "Processing...";
        Message = _initialMessage;
        _spinner = spinner;
        _spinnerColor = spinnerColor;
        IsRunning = true;
        
        // Save cursor position and console state
        _cursorTop = Console.CursorTop;
        _consoleBufferHeightAtStart = Console.BufferHeight;

        // Start the spinner animation
        _spinnerTask = Task.Run(async () => await RunSpinnerAsync(_cancellationTokenSource.Token));
    }

    /// <summary>
    /// Creates and starts a new inline spinner.
    /// </summary>
    /// <param name="message">The spinner message.</param>
    /// <param name="spinner">Optional spinner style. Defaults to Default.</param>
    /// <param name="spinnerColor">Optional spinner color. Defaults to Theme.PrimaryColor.</param>
    /// <returns>A running inline spinner.</returns>
    public static SimpleSpinner Start(string message, Spinner? spinner = null, Color? spinnerColor = null)
    {
        spinner ??= Spinner.Known.Default;
        var color = spinnerColor ?? Theme.PrimaryColor;

        return new SimpleSpinner(message, spinner, color);
    }

    /// <summary>
    /// Updates the spinner message.
    /// </summary>
    /// <param name="newMessage">The new message to display.</param>
    public void UpdateMessage(string newMessage)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SimpleSpinner));

        Message = newMessage;
    }

    /// <summary>
    /// Stops and disposes the spinner.
    /// </summary>
    public async Task StopAsync()
    {
        if (_disposed)
            return;

        IsRunning = false;
        _cancellationTokenSource.Cancel();

        try
        {
            await _spinnerTask;
        }
        catch (OperationCanceledException)
        {
            // Expected when spinner is stopped
        }
        finally
        {
            // Clear the spinner line
            ClearSpinnerLine();
            _cancellationTokenSource.Dispose();
            _disposed = true;
        }
    }

    /// <summary>
    /// Runs the spinner animation.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop the spinner.</param>
    private async Task RunSpinnerAsync(CancellationToken cancellationToken)
    {
        var frameIndex = 0;

        try
        {
            while (!cancellationToken.IsCancellationRequested && IsRunning)
            {
                var frame = _spinner.Frames[frameIndex];
                
                // Get the spinner line and write spinner at correct position
                var spinnerLine = GetSpinnerLine();
                
                // Save current cursor position
                var currentLeft = Console.CursorLeft;
                var currentTop = Console.CursorTop;
                
                try
                {
                    // Move to spinner line and clear it
                    Console.SetCursorPosition(0, spinnerLine);
                    AnsiConsole.Write(new string(' ', Console.WindowWidth));
                    
                    // Write spinner at the spinner line
                    Console.SetCursorPosition(0, spinnerLine);
                    AnsiConsole.Markup($"[{_spinnerColor}]{frame}[/] {Message.EscapeMarkup()}");
                }
                finally
                {
                    // Restore original cursor position
                    Console.SetCursorPosition(currentLeft, currentTop);
                }
                
                frameIndex = (frameIndex + 1) % _spinner.Frames.Count;
                await Task.Delay(_spinner.Interval, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when spinner is stopped
        }
    }

    /// <summary>
    /// Clears the spinner line.
    /// </summary>
    private int GetSpinnerLine()
    {
        // Calculate current spinner position
        // When console buffer scrolls, the spinner's relative position changes
        var bufferScroll = Console.BufferHeight - _consoleBufferHeightAtStart;
        var currentSpinnerTop = Math.Max(0, _cursorTop - bufferScroll);
        
        // Ensure we don't try to set cursor outside console bounds
        if (currentSpinnerTop >= Console.WindowHeight)
        {
            // Spinner has scrolled out of view, return current cursor position
            return Console.CursorTop;
        }
        
        return currentSpinnerTop;
    }
    
    private void ClearSpinnerLine()
    {
        try
        {
            var spinnerLine = GetSpinnerLine();
            
            // Save current cursor position
            var currentLeft = Console.CursorLeft;
            var currentTop = Console.CursorTop;
            
            // Move to spinner line and clear it
            Console.SetCursorPosition(0, spinnerLine);
            AnsiConsole.Write(new string(' ', Console.WindowWidth));
            
            // Restore cursor position
            Console.SetCursorPosition(currentLeft, currentTop);
        }
        catch (Exception)
        {
            // If console operations fail, just continue
        }
    }

    /// <summary>
    /// Disposes the spinner asynchronously.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }
}