namespace Loken.Core;

public class TodoService : ITodoService
{
    private readonly TodoManager _todoManager;
    private int _turnsWithoutTodo;
    private bool _todoCalledThisTurn;

    public IList<TodoItem> Todos => _todoManager.Todos;

    public TodoService(TodoManager todoManager)
    {
        _todoManager = todoManager;
        _turnsWithoutTodo = 0;
        _todoCalledThisTurn = false;
    }

    public void Update(IList<TodoItem> todos)
    {
        _todoManager.Update(todos);
    }

    public void MarkTodoCalled()
    {
        _todoCalledThisTurn = true;
    }

    public bool ShouldRemindAboutTodos()
    {
        if (_todoCalledThisTurn)
        {
            _turnsWithoutTodo = 0;
            _todoCalledThisTurn = false;
        }
        else
        {
            _turnsWithoutTodo++;
        }

        return _turnsWithoutTodo >= 3 && Todos.Count(t => t.Status == TodoStatus.Todo) > 0;
    }

    public override string ToString()
    {
        return _todoManager.ToString();
    }
}
