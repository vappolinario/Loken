namespace Loken.Core;

public interface IEditorService
{
  Task<string?> EditAsync(string? initialContent = null);
}
