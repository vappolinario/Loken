namespace Loken.Core;

public interface ITodoService
{
    IList<TodoItem> Todos { get; }

    void Update(IList<TodoItem> todos);

    void MarkTodoCalled();

    bool ShouldRemindAboutTodos();

    string ToString();
}
