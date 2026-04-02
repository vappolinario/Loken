namespace Loken.Core;

using System.Text.Json;

public class TodoHandler : IToolHandler
{
    private readonly ITodoService _todoService;

    public string Name => "todo";

    public string Description => "Update task list. Track progress on multi-step tasks.";

    public BinaryData Parameters => BinaryData.FromObjectAsJson(new
    {
        type = "object",
        properties = new
        {
            items = new
            {
                type = "array",
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        id = new { type = "string" },
                        text = new { type = "string" },
                        status = new
                        {
                            type = "string",
                            @enum = new[] { "todo", "doing", "done" }
                        }
                    },
                    required = new[] { "id", "text", "status" }
                }
            }
        },
        required = new[] { "items" }
    });

    public TodoHandler(ITodoService todoService)
    {
        _todoService = todoService;
    }

    public async Task<string> ExecuteAsync(BinaryData input)
    {
        try
        {
            using var doc = JsonDocument.Parse(input);

            JsonElement items;
            if (!doc.RootElement.TryGetProperty("items", out items))
                throw new MissingParameterException("items");

            var newList = new List<TodoItem>();
            foreach (var todoItem in items.EnumerateArray())
            {
                if (!todoItem.TryGetProperty("id", out var idProp) || idProp.GetString() is not string id || string.IsNullOrWhiteSpace(id))
                    throw new MissingParameterException("id");

                if (!todoItem.TryGetProperty("text", out var textProp) || textProp.GetString() is not string text || string.IsNullOrWhiteSpace(text))
                    throw new MissingParameterException("text");

                if (!todoItem.TryGetProperty("status", out var statusProp) || statusProp.GetString() is not string statusStr)
                    throw new MissingParameterException("status");

                if (!Enum.TryParse<TodoStatus>(statusStr, true, out var status))
                    throw new MissingParameterException("status");

                newList.Add(new() { Id = id, Text = text, Status = status });
            }

            _todoService.Update(newList);
            _todoService.MarkTodoCalled();

            return _todoService.ToString();

        }
        catch (JsonException)
        {
            throw new ExecutionFailedException("Invalid Json.");
        }
    }
}

