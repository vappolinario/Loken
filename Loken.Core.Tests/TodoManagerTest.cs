namespace Loken.Core;

using Shouldly;
using System.Collections.Generic;

public class TodoManagerTest
{
    private readonly TodoManager _manager;

    public TodoManagerTest()
    {
        _manager = new TodoManager();
    }

    [Fact]
    public void Constructor_InitializesWithEmptyList()
    {

        _manager.Todos.ShouldNotBeNull();
        _manager.Todos.ShouldBeEmpty();
    }

    [Fact]
    public void Update_WithEmptyList_UpdatesSuccessfully()
    {
        var emptyList = new List<TodoItem>();

        _manager.Update(emptyList);

        _manager.Todos.ShouldBeEmpty();
    }

    [Fact]
    public void Update_WithSingleTodoItem_UpdatesSuccessfully()
    {
        var todo = new TodoItem { Id = "task1", Text = "Test task", Status = TodoStatus.Todo };
        var list = new List<TodoItem> { todo };

        _manager.Update(list);

        _manager.Todos.ShouldHaveSingleItem();
        var actual = _manager.Todos[0];
        actual.Id.ShouldBe("task1");
        actual.Text.ShouldBe("Test task");
        actual.Status.ShouldBe(TodoStatus.Todo);
    }

    [Fact]
    public void Update_WithMultipleTodoItems_UpdatesSuccessfully()
    {
        var todos = new List<TodoItem>
        {
            new() { Id = "task1", Text = "First task", Status = TodoStatus.Todo },
            new() { Id = "task2", Text = "Second task", Status = TodoStatus.Doing },
            new() { Id = "task3", Text = "Third task", Status = TodoStatus.Done }
        };

        _manager.Update(todos);

        _manager.Todos.ShouldNotBeNull();
        _manager.Todos.Count.ShouldBe(3);
        _manager.Todos[0].Id.ShouldBe("task1");
        _manager.Todos[1].Id.ShouldBe("task2");
        _manager.Todos[2].Id.ShouldBe("task3");
    }

    [Fact]
    public void Update_WithMultipleTodoStatusItems_Allowed()
    {
        var todos = new List<TodoItem>
        {
            new() { Id = "task1", Text = "Todo 1", Status = TodoStatus.Todo },
            new() { Id = "task2", Text = "Todo 2", Status = TodoStatus.Todo },
            new() { Id = "task3", Text = "Todo 3", Status = TodoStatus.Todo }
        };

        _manager.Update(todos);

        _manager.Todos.Count(t => t.Status == TodoStatus.Todo).ShouldBe(3);
    }

    [Fact]
    public void Update_WithMultipleDoneStatusItems_Allowed()
    {
        var todos = new List<TodoItem>
        {
            new() { Id = "task1", Text = "Done 1", Status = TodoStatus.Done },
            new() { Id = "task2", Text = "Done 2", Status = TodoStatus.Done },
            new() { Id = "task3", Text = "Done 3", Status = TodoStatus.Done }
        };

        _manager.Update(todos);

        _manager.Todos.Count(t => t.Status == TodoStatus.Done).ShouldBe(3);
    }

    [Fact]
    public void Update_WithZeroDoingItems_Allowed()
    {
        var todos = new List<TodoItem>
        {
            new() { Id = "task1", Text = "Todo only", Status = TodoStatus.Todo },
            new() { Id = "task2", Text = "Done only", Status = TodoStatus.Done }
        };

        _manager.Update(todos);

        _manager.Todos.Count(t => t.Status == TodoStatus.Doing).ShouldBe(0);
    }

    [Fact]
    public void Update_WithOneDoingItem_Allowed()
    {
        var todos = new List<TodoItem>
        {
            new() { Id = "task1", Text = "In progress", Status = TodoStatus.Doing },
            new() { Id = "task2", Text = "Todo task", Status = TodoStatus.Todo }
        };

        _manager.Update(todos);

        _manager.Todos.Count(t => t.Status == TodoStatus.Doing).ShouldBe(1);
    }

    [Fact]
    public void Update_WithMultipleDoingItems_ThrowsExecutionFailedException()
    {
        var todos = new List<TodoItem>
        {
            new() { Id = "task1", Text = "First in progress", Status = TodoStatus.Doing },
            new() { Id = "task2", Text = "Second in progress", Status = TodoStatus.Doing },
            new() { Id = "task3", Text = "Todo task", Status = TodoStatus.Todo }
        };

        var exception = Should.Throw<ExecutionFailedException>(() => _manager.Update(todos));
        exception.Message.ShouldBe("Execution error: More than one TodoItem in progress");
    }

    [Fact]
    public void Update_WithExactlyMaxItems_Allowed()
    {
        var todos = new List<TodoItem>();
        for (int i = 1; i <= 20; i++)
        {
            todos.Add(new TodoItem { Id = $"task{i}", Text = $"Task {i}", Status = TodoStatus.Todo });
        }

        _manager.Update(todos);

        _manager.Todos.Count.ShouldBe(20);
    }

    [Fact]
    public void Update_WithMoreThanMaxItems_ThrowsExecutionFailedException()
    {
        var todos = new List<TodoItem>();
        for (int i = 1; i <= 21; i++)
        {
            todos.Add(new TodoItem { Id = $"task{i}", Text = $"Task {i}", Status = TodoStatus.Todo });
        }

        var exception = Should.Throw<ExecutionFailedException>(() => _manager.Update(todos));
        exception.Message.ShouldBe("Execution error: Too many todo items.");
    }

    [Fact]
    public void Update_WithNullId_ThrowsArgumentException()
    {
        var todos = new List<TodoItem>
        {
            new() { Id = null!, Text = "Task with null id", Status = TodoStatus.Todo }
        };

        Should.Throw<ArgumentException>(() => _manager.Update(todos));
    }

    [Fact]
    public void Update_WithEmptyId_ThrowsArgumentException()
    {
        var todos = new List<TodoItem>
        {
            new() { Id = "", Text = "Task with empty id", Status = TodoStatus.Todo }
        };

        Should.Throw<ArgumentException>(() => _manager.Update(todos));
    }

    [Fact]
    public void Update_WithWhitespaceId_ThrowsArgumentException()
    {
        var todos = new List<TodoItem>
        {
            new() { Id = "   ", Text = "Task with whitespace id", Status = TodoStatus.Todo }
        };

        Should.Throw<ArgumentException>(() => _manager.Update(todos));
    }

    [Fact]
    public void ToString_WithEmptyList_ReturnsCorrectFormat()
    {
        _manager.Update(new List<TodoItem>());

        var result = _manager.ToString();

        result.ShouldBe("\n0/0 tasks completed\n");
    }

    [Fact]
    public void ToString_WithSingleTodoItem_ReturnsCorrectFormat()
    {
        var todos = new List<TodoItem>
        {
            new() { Id = "task1", Text = "Single task", Status = TodoStatus.Todo }
        };
        _manager.Update(todos);

        var result = _manager.ToString();

        result.ShouldBe("[ ] Single task\n\n0/1 tasks completed\n");
    }

    [Fact]
    public void ToString_WithSingleDoingItem_ReturnsCorrectFormat()
    {
        var todos = new List<TodoItem>
        {
            new() { Id = "task1", Text = "In progress task", Status = TodoStatus.Doing }
        };
        _manager.Update(todos);

        var result = _manager.ToString();

        result.ShouldBe("[>] In progress task\n\n0/1 tasks completed\n");
    }

    [Fact]
    public void ToString_WithSingleDoneItem_ReturnsCorrectFormat()
    {
        var todos = new List<TodoItem>
        {
            new() { Id = "task1", Text = "Completed task", Status = TodoStatus.Done }
        };
        _manager.Update(todos);

        var result = _manager.ToString();

        result.ShouldBe("[X] Completed task\n\n1/1 tasks completed\n");
    }

    [Fact]
    public void ToString_WithMultipleItems_ReturnsCorrectFormat()
    {
        var todos = new List<TodoItem>
        {
            new() { Id = "task1", Text = "Todo task", Status = TodoStatus.Todo },
            new() { Id = "task2", Text = "Doing task", Status = TodoStatus.Doing },
            new() { Id = "task3", Text = "Done task", Status = TodoStatus.Done },
            new() { Id = "task4", Text = "Another done", Status = TodoStatus.Done }
        };
        _manager.Update(todos);

        var result = _manager.ToString();

        var expected = @"[ ] Todo task
[>] Doing task
[X] Done task
[X] Another done

2/4 tasks completed
";
        result.ShouldBe(expected);
    }

    [Fact]
    public void ToString_WithSpecialCharactersInText_ReturnsCorrectFormat()
    {
        var todos = new List<TodoItem>
        {
            new() { Id = "task1", Text = "Task with special chars: !@#$%^&*()", Status = TodoStatus.Todo },
            new() { Id = "task2", Text = "Task with emoji 🚀", Status = TodoStatus.Doing }
        };
        _manager.Update(todos);

        var result = _manager.ToString();

        var expected = @"[ ] Task with special chars: !@#$%^&*()
[>] Task with emoji 🚀

0/2 tasks completed
";
        result.ShouldBe(expected);
    }

    [Fact]
    public void ToString_WithLongText_ReturnsCorrectFormat()
    {
        var longText = new string('A', 100);
        var todos = new List<TodoItem>
        {
            new() { Id = "task1", Text = longText, Status = TodoStatus.Todo }
        };
        _manager.Update(todos);

        var result = _manager.ToString();

        result.ShouldBe($"[ ] {longText}\n\n0/1 tasks completed\n");
    }

    [Fact]
    public void Update_ReplacesPreviousTodos_NotAppends()
    {
        var firstList = new List<TodoItem>
        {
            new() { Id = "old1", Text = "Old task 1", Status = TodoStatus.Todo },
            new() { Id = "old2", Text = "Old task 2", Status = TodoStatus.Doing }
        };
        _manager.Update(firstList);

        var secondList = new List<TodoItem>
        {
            new() { Id = "new1", Text = "New task 1", Status = TodoStatus.Done }
        };

        _manager.Update(secondList);

        _manager.Todos.ShouldHaveSingleItem();
        _manager.Todos[0].Id.ShouldBe("new1");
        _manager.Todos[0].Text.ShouldBe("New task 1");
        _manager.Todos[0].Status.ShouldBe(TodoStatus.Done);
    }

    [Fact]
    public void TodosProperty_IsReadOnlyAfterUpdate()
    {
        var todos = new List<TodoItem>
        {
            new() { Id = "task1", Text = "Test task", Status = TodoStatus.Todo }
        };
        _manager.Update(todos);

        _manager.Todos.ShouldNotBeNull();
        _manager.Todos.Count.ShouldBe(1);

        var newList = new List<TodoItem>
        {
            new() { Id = "task2", Text = "Another task", Status = TodoStatus.Done }
        };
        _manager.Update(newList);

        todos.Count.ShouldBe(1);
        todos[0].Id.ShouldBe("task1");
    }

    [Fact]
    public void Update_WithMixedStatusesAndValidIds_UpdatesSuccessfully()
    {
        var todos = new List<TodoItem>
        {
            new() { Id = "a", Text = "Task A", Status = TodoStatus.Todo },
            new() { Id = "b", Text = "Task B", Status = TodoStatus.Doing },
            new() { Id = "c", Text = "Task C", Status = TodoStatus.Done },
            new() { Id = "d", Text = "Task D", Status = TodoStatus.Todo },
            new() { Id = "e", Text = "Task E", Status = TodoStatus.Done }
        };

        _manager.Update(todos);

        _manager.Todos.Count.ShouldBe(5);
        _manager.Todos.Count(t => t.Status == TodoStatus.Todo).ShouldBe(2);
        _manager.Todos.Count(t => t.Status == TodoStatus.Doing).ShouldBe(1);
        _manager.Todos.Count(t => t.Status == TodoStatus.Done).ShouldBe(2);
    }
}
