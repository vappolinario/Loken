namespace Loken.Core.Tests;

/// <summary>
/// Test harness for IEditorService. Simulates editing by returning
/// pre-configured content as if the user wrote and saved in an editor.
/// Can also simulate cancellation (returns null) and failure cases.
/// </summary>
public class TestEditorService : IEditorService
{
  private readonly Func<string?, Task<string?>>? _handler;

  public TestEditorService(string? result)
  {
    _handler = (initialContent) => Task.FromResult(result ?? initialContent);
  }

  public TestEditorService(Func<string?, Task<string?>>? handler)
  {
    _handler = handler;
  }

  public int CallCount { get; private set; }

  public string? LastInitialContent { get; private set; }

  public async Task<string?> EditAsync(string? initialContent = null)
  {
    CallCount++;
    LastInitialContent = initialContent;

    if (_handler is null)
      return initialContent;

    return await _handler(initialContent);
  }

  /// <summary>
  /// Creates a service that simulates the user writing the given content in the editor.
  /// </summary>
  public static TestEditorService Returning(string content)
  {
    return new TestEditorService(content);
  }

  /// <summary>
  /// Creates a service that simulates the user cancelling / closing without saving.
  /// </summary>
  public static TestEditorService Cancelled()
  {
    return new TestEditorService((_) => Task.FromResult<string?>(null));
  }

  /// <summary>
  /// Creates a service that simulates the user writing content that includes the
  /// default header (as if they didn't clear it).
  /// </summary>
  public static TestEditorService WithDefaultHeader(string userContent)
  {
    var header = "# Write your prompt here\n\n";
    return new TestEditorService(header + userContent);
  }
}
