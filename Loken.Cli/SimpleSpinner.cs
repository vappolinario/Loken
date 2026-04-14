using Spectre.Console;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Loken.Cli;

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

    public bool IsRunning { get; private set; }

    public string Message { get; set; }

    private SimpleSpinner(string initialMessage, Spinner spinner, Color spinnerColor)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _initialMessage = initialMessage ?? "Processing...";
        Message = _initialMessage;
        _spinner = spinner;
        _spinnerColor = spinnerColor;
        IsRunning = true;

        _cursorTop = Console.CursorTop;
        _consoleBufferHeightAtStart = Console.BufferHeight;

        _spinnerTask = Task.Run(async () => await RunSpinnerAsync(_cancellationTokenSource.Token));
    }

    public static SimpleSpinner Start(string message, Spinner? spinner = null, Color? spinnerColor = null)
    {
        spinner ??= Spinner.Known.Default;
        var color = spinnerColor ?? Theme.PrimaryColor;

        return new SimpleSpinner(message, spinner, color);
    }

    public void UpdateMessage(string newMessage)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SimpleSpinner));

        Message = newMessage;
    }

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
        }
        finally
        {
            ClearSpinnerLine();
            _cancellationTokenSource.Dispose();
            _disposed = true;
        }
    }

    private async Task RunSpinnerAsync(CancellationToken cancellationToken)
    {
        var frameIndex = 0;

        try
        {
            while (!cancellationToken.IsCancellationRequested && IsRunning)
            {
                var frame = _spinner.Frames[frameIndex];

                var spinnerLine = GetSpinnerLine();

                var currentLeft = Console.CursorLeft;
                var currentTop = Console.CursorTop;

                try
                {
                    Console.SetCursorPosition(0, spinnerLine);
                    AnsiConsole.Write(new string(' ', Console.WindowWidth));

                    Console.SetCursorPosition(0, spinnerLine);
                    AnsiConsole.Markup($"[{_spinnerColor}]{frame}[/] {Message.EscapeMarkup()}");
                }
                finally
                {
                    Console.SetCursorPosition(currentLeft, currentTop);
                }

                frameIndex = (frameIndex + 1) % _spinner.Frames.Count;
                await Task.Delay(_spinner.Interval, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private int GetSpinnerLine()
    {
        var bufferScroll = Console.BufferHeight - _consoleBufferHeightAtStart;
        var currentSpinnerTop = Math.Max(0, _cursorTop - bufferScroll);

        if (currentSpinnerTop >= Console.WindowHeight)
        {
            return Console.CursorTop;
        }

        return currentSpinnerTop;
    }

    private void ClearSpinnerLine()
    {
        try
        {
            var spinnerLine = GetSpinnerLine();

            var currentLeft = Console.CursorLeft;
            var currentTop = Console.CursorTop;

            Console.SetCursorPosition(0, spinnerLine);
            AnsiConsole.Write(new string(' ', Console.WindowWidth));

            Console.SetCursorPosition(currentLeft, currentTop);
        }
        catch (Exception)
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }
}
