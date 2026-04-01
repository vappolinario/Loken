using System.Text;

namespace Loken.Core;

public class TodoManager
{
    private const int MAX_TODOS = 20;

    public IList<TodoItem> Todos { get; private set; }

    public TodoManager()
    {
        Todos = new List<TodoItem>();
    }

    public void Update(IList<TodoItem> todos)
    {
        if (todos.Count > MAX_TODOS)
            throw new ExecutionFailedException("Too many todo items.");

        foreach (var item in todos)
            ArgumentException.ThrowIfNullOrWhiteSpace(item.Id);

        if (todos.Count(t => t.Status == TodoStatus.Doing) > 1)
            throw new ExecutionFailedException("More than one TodoItem in progress");

        Todos = todos;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var todo in Todos)
        {
            sb.AppendLine(todo.ToString());
        }
        sb.AppendLine();
        sb.AppendLine($"{Todos.Count(t => t.Status == TodoStatus.Done)}/{Todos.Count} tasks completed");
        return sb.ToString();
    }
}
