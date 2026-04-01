namespace Loken.Core;

public class TodoItem
{
    public required string Id { get; set; }
    public required string Text { get; set; }
    public TodoStatus Status { get; set; } = TodoStatus.Todo;

    public override string ToString() => Status switch
    {
        TodoStatus.Todo => $"[ ] {Text}",
        TodoStatus.Doing => $"[>] {Text}",
        TodoStatus.Done => $"[X] {Text}",
        _ => $"[?] {Text}"
    };
}
