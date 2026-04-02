namespace Loken.Core;

using NSubstitute;
using Shouldly;
using System.Text.Json;

public class TodoHandlerTest
{
    private readonly TodoHandler _handler;
    private readonly ITodoService _todoService;

    public TodoHandlerTest()
    {
        _todoService = Substitute.For<ITodoService>();
        _handler = new TodoHandler(_todoService);
    }

    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        _handler.Name.ShouldBe("todo");
        _handler.Description.ShouldBe("Update task list. Track progress on multi-step tasks.");
        _handler.Parameters.ShouldNotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WithValidTodoItems_ReturnsSuccessMessage()
    {
        var input = new
        {
            items = new[]
            {
                new { id = "task1", text = "First task", status = "todo" },
                new { id = "task2", text = "Second task", status = "doing" },
                new { id = "task3", text = "Third task", status = "done" }
            }
        };

        var json = JsonSerializer.Serialize(input);
        var binaryData = BinaryData.FromString(json);

        _todoService.ToString().Returns(@"[ ] First task
[>] Second task
[X] Third task

1/3 tasks completed
");

        var result = await _handler.ExecuteAsync(binaryData);

        var expected = @"[ ] First task
[>] Second task
[X] Third task

1/3 tasks completed
";
        result.ShouldBe(expected);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyItemsArray_ReturnsZeroCountMessage()
    {
        var input = new
        {
            items = Array.Empty<object>()
        };

        var json = JsonSerializer.Serialize(input);
        var binaryData = BinaryData.FromString(json);

        _todoService.ToString().Returns("\n0/0 tasks completed\n");

        var result = await _handler.ExecuteAsync(binaryData);

        result.ShouldBe("\n0/0 tasks completed\n");
    }

    [Fact]
    public async Task ExecuteAsync_WithSingleTodoItem_ReturnsCorrectCount()
    {
        var input = new
        {
            items = new[]
            {
                new { id = "single-task", text = "Only one task", status = "todo" }
            }
        };

        var json = JsonSerializer.Serialize(input);
        var binaryData = BinaryData.FromString(json);

        _todoService.ToString().Returns("[ ] Only one task\n\n0/1 tasks completed\n");

        var result = await _handler.ExecuteAsync(binaryData);

        result.ShouldBe("[ ] Only one task\n\n0/1 tasks completed\n");
    }

    [Fact]
    public async Task ExecuteAsync_MissingItemsProperty_ThrowsMissingParameterException()
    {
        var input = new { };
        var json = JsonSerializer.Serialize(input);
        var binaryData = BinaryData.FromString(json);

        var exception = await Should.ThrowAsync<MissingParameterException>(
            async () => await _handler.ExecuteAsync(binaryData)
        );

        exception.Message.ShouldBe("Missing parameter: items");
    }

    [Fact]
    public async Task ExecuteAsync_MissingIdProperty_ThrowsMissingParameterException()
    {
        var input = new
        {
            items = new[]
            {
                new { text = "Task without id", status = "todo" }
            }
        };

        var json = JsonSerializer.Serialize(input);
        var binaryData = BinaryData.FromString(json);

        var exception = await Should.ThrowAsync<MissingParameterException>(
            async () => await _handler.ExecuteAsync(binaryData)
        );

        exception.Message.ShouldBe("Missing parameter: id");
    }

    [Fact]
    public async Task ExecuteAsync_MissingTextProperty_ThrowsMissingParameterException()
    {
        var input = new
        {
            items = new[]
            {
                new { id = "task1", status = "todo" }
            }
        };

        var json = JsonSerializer.Serialize(input);
        var binaryData = BinaryData.FromString(json);

        var exception = await Should.ThrowAsync<MissingParameterException>(
            async () => await _handler.ExecuteAsync(binaryData)
        );

        exception.Message.ShouldBe("Missing parameter: text");
    }

    [Fact]
    public async Task ExecuteAsync_MissingStatusProperty_ThrowsMissingParameterException()
    {
        var input = new
        {
            items = new[]
            {
                new { id = "task1", text = "Task without status" }
            }
        };

        var json = JsonSerializer.Serialize(input);
        var binaryData = BinaryData.FromString(json);

        var exception = await Should.ThrowAsync<MissingParameterException>(
            async () => await _handler.ExecuteAsync(binaryData)
        );

        exception.Message.ShouldBe("Missing parameter: status");
    }

    [Fact]
    public async Task ExecuteAsync_InvalidStatusValue_ThrowsMissingParameterException()
    {
        var input = new
        {
            items = new[]
            {
                new { id = "task1", text = "Task with invalid status", status = "invalid_status" }
            }
        };

        var json = JsonSerializer.Serialize(input);
        var binaryData = BinaryData.FromString(json);

        var exception = await Should.ThrowAsync<MissingParameterException>(
            async () => await _handler.ExecuteAsync(binaryData)
        );

        exception.Message.ShouldBe("Missing parameter: status");
    }

    [Fact]
    public async Task ExecuteAsync_NullIdValue_ThrowsMissingParameterException()
    {
        var input = new
        {
            items = new[]
            {
                new { id = (string?)null, text = "Task with null id", status = "todo" }
            }
        };

        var json = JsonSerializer.Serialize(input);
        var binaryData = BinaryData.FromString(json);

        var exception = await Should.ThrowAsync<MissingParameterException>(
            async () => await _handler.ExecuteAsync(binaryData)
        );

        exception.Message.ShouldBe("Missing parameter: id");
    }

    [Fact]
    public async Task ExecuteAsync_NullTextValue_ThrowsMissingParameterException()
    {
        var input = new
        {
            items = new[]
            {
                new { id = "task1", text = (string?)null, status = "todo" }
            }
        };

        var json = JsonSerializer.Serialize(input);
        var binaryData = BinaryData.FromString(json);

        var exception = await Should.ThrowAsync<MissingParameterException>(
            async () => await _handler.ExecuteAsync(binaryData)
        );

        exception.Message.ShouldBe("Missing parameter: text");
    }

    [Fact]
    public async Task ExecuteAsync_InvalidJson_ThrowsExecutionFailedException()
    {
        var invalidJson = "{ invalid json }";
        var binaryData = BinaryData.FromString(invalidJson);

        var exception = await Should.ThrowAsync<ExecutionFailedException>(
            async () => await _handler.ExecuteAsync(binaryData)
        );

        exception.Message.ShouldBe("Execution error: Invalid Json.");
    }

    [Fact]
    public async Task ExecuteAsync_WithAllStatusValues_ProcessesCorrectly()
    {
        var input = new
        {
            items = new[]
            {
                new { id = "todo1", text = "Todo task", status = "todo" },
                new { id = "doing1", text = "Doing task", status = "doing" },
                new { id = "done1", text = "Done task", status = "done" }
            }
        };

        var json = JsonSerializer.Serialize(input);
        var binaryData = BinaryData.FromString(json);

        _todoService.ToString().Returns(@"[ ] Todo task
[>] Doing task
[X] Done task

1/3 tasks completed
");

        var result = await _handler.ExecuteAsync(binaryData);

        var expected = @"[ ] Todo task
[>] Doing task
[X] Done task

1/3 tasks completed
";
        result.ShouldBe(expected);
    }

    [Fact]
    public async Task ExecuteAsync_WithDuplicateIds_ProcessesCorrectly()
    {
        var input = new
        {
            items = new[]
            {
                new { id = "same-id", text = "First task", status = "todo" },
                new { id = "same-id", text = "Second task", status = "doing" }
            }
        };

        var json = JsonSerializer.Serialize(input);
        var binaryData = BinaryData.FromString(json);

        _todoService.ToString().Returns(@"[ ] First task
[>] Second task

0/2 tasks completed
");

        var result = await _handler.ExecuteAsync(binaryData);

        var expected = @"[ ] First task
[>] Second task

0/2 tasks completed
";
        result.ShouldBe(expected);
    }

    [Fact]
    public async Task ExecuteAsync_WithMixedValidAndInvalidItems_ThrowsOnFirstInvalid()
    {
        var json = @"{
            ""items"": [
                { ""id"": ""valid1"", ""text"": ""Valid task"", ""status"": ""todo"" },
                { ""id"": ""invalid"", ""text"": ""Invalid task"" }
            ]
        }";

        var binaryData = BinaryData.FromString(json);

        var exception = await Should.ThrowAsync<MissingParameterException>(
            async () => await _handler.ExecuteAsync(binaryData)
        );

        exception.Message.ShouldBe("Missing parameter: status");
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyStringId_ThrowsMissingParameterException()
    {
        var input = new
        {
            items = new[]
            {
                new { id = "", text = "Task with empty id", status = "todo" }
            }
        };

        var json = JsonSerializer.Serialize(input);
        var binaryData = BinaryData.FromString(json);

        var exception = await Should.ThrowAsync<MissingParameterException>(
            async () => await _handler.ExecuteAsync(binaryData)
        );

        exception.Message.ShouldBe("Missing parameter: id");
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyStringText_ThrowsMissingParameterException()
    {
        var input = new
        {
            items = new[]
            {
                new { id = "task1", text = "", status = "todo" }
            }
        };

        var json = JsonSerializer.Serialize(input);
        var binaryData = BinaryData.FromString(json);

        var exception = await Should.ThrowAsync<MissingParameterException>(
            async () => await _handler.ExecuteAsync(binaryData)
        );

        exception.Message.ShouldBe("Missing parameter: text");
    }

    [Fact]
    public async Task ExecuteAsync_WithWhitespaceOnlyId_ThrowsMissingParameterException()
    {
        var input = new
        {
            items = new[]
            {
                new { id = "   ", text = "Task with whitespace id", status = "todo" }
            }
        };

        var json = JsonSerializer.Serialize(input);
        var binaryData = BinaryData.FromString(json);

        var exception = await Should.ThrowAsync<MissingParameterException>(
            async () => await _handler.ExecuteAsync(binaryData)
        );

        exception.Message.ShouldBe("Missing parameter: id");
    }

    [Fact]
    public async Task ExecuteAsync_WithCaseInsensitiveStatus_ProcessesCorrectly()
    {
        var input = new
        {
            items = new[]
            {
                new { id = "task1", text = "Task with uppercase status", status = "TODO" },
                new { id = "task2", text = "Task with mixed case status", status = "DoInG" },
                new { id = "task3", text = "Task with lowercase status", status = "done" }
            }
        };

        var json = JsonSerializer.Serialize(input);
        var binaryData = BinaryData.FromString(json);

        _todoService.ToString().Returns(@"[ ] Task with uppercase status
[>] Task with mixed case status
[X] Task with lowercase status

1/3 tasks completed
");

        var result = await _handler.ExecuteAsync(binaryData);

        var expected = @"[ ] Task with uppercase status
[>] Task with mixed case status
[X] Task with lowercase status

1/3 tasks completed
";
        result.ShouldBe(expected);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullItemsProperty_ThrowsInvalidOperationException()
    {
        var json = @"{ ""items"": null }";
        var binaryData = BinaryData.FromString(json);

        await Should.ThrowAsync<InvalidOperationException>(
            async () => await _handler.ExecuteAsync(binaryData)
        );
    }
}
