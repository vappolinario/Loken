namespace Loken.Core;

public class SystemEditorService : IEditorService
{
  private const string Header = "# Write your prompt here\n\n";

  public async Task<string?> EditAsync(string? initialContent = null)
  {
    var editor = ResolveEditor();
    if (editor is null)
      return null;

    var tempFile = Path.GetTempFileName() + ".md";
    await File.WriteAllTextAsync(tempFile, initialContent ?? Header);

    try
    {
      var exitCode = await LaunchEditor(editor, tempFile);

      if (exitCode != 0)
        return null;

      var content = await File.ReadAllTextAsync(tempFile);

      if (initialContent is null && content.StartsWith(Header))
        content = content[Header.Length..];

      return content.Trim();
    }
    finally
    {
      if (File.Exists(tempFile))
        File.Delete(tempFile);
    }
  }

  private static string? ResolveEditor()
  {
    var editor = Environment.GetEnvironmentVariable("EDITOR");

    if (!string.IsNullOrWhiteSpace(editor))
      return editor;

    if (OperatingSystem.IsWindows())
      return "notepad";

    // Linux and macOS default to vim
    return "vim";
  }

  private static async Task<int> LaunchEditor(string editor, string filePath)
  {
    using var process = new System.Diagnostics.Process
    {
      StartInfo = new System.Diagnostics.ProcessStartInfo
      {
        FileName = editor,
        Arguments = $"\"{filePath}\"",
        UseShellExecute = false
      }
    };

    process.Start();
    await process.WaitForExitAsync();
    return process.ExitCode;
  }
}
