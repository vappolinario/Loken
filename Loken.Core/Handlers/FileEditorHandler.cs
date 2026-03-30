namespace Loken.Core;

using System.Text.Json;

public class FileEditorHandler : IToolHandler
{
    private readonly IPathResolver _pathResolver;

    public string Name => "edit_file";

    public string Description => "Replace exact text in a file";

    public BinaryData Parameters => BinaryData.FromObjectAsJson(
                new
                {
                    type = "object",
                    properties = new
                    {
                        path = new
                        {
                            type = "string",
                            description = "The file path to write"
                        },
                        new_text = new
                        {
                            type = "string",
                            description = "the replacement text"
                        },
                        old_text = new
                        {
                            type = "string",
                            description = "The exact text to find and replace"
                        },
                    },
                    required = new[] { "path", "old_text", "new_text" }
                });


    public FileEditorHandler(IPathResolver pathResolver)
    {
        _pathResolver = pathResolver;
    }

    public async Task<string> ExecuteAsync(BinaryData input)
    {
        try
        {
            using var doc = JsonDocument.Parse(input);
            var path = doc.RootElement.GetProperty("path").GetString()
                       ?? throw new MissingParameterException("path");

            var old_text = doc.RootElement.GetProperty("old_text").GetString()
                       ?? throw new MissingParameterException("old_text");

            var new_text = doc.RootElement.GetProperty("new_text").GetString()
                       ?? throw new MissingParameterException("new_text");

            if (string.IsNullOrEmpty(old_text))
                throw new MissingParameterException("old_text");

            string safePath = _pathResolver.ResolveSafePath(path);

            if (!File.Exists(safePath))
                throw new ExecutionFailedException($"File not found: {path}");

            var content = await File.ReadAllTextAsync(safePath);

            if (!content.Contains(old_text!))
                throw new ExecutionFailedException($"Original text not found on file.");

            var updatedContent = content.Replace(old_text!, new_text);

            await File.WriteAllTextAsync(safePath, updatedContent);

            return $"File {path} updated.";
        }
        catch (JsonException)
        {
            throw new ExecutionFailedException("Invalid Json.");
        }
    }
}
