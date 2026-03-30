namespace Loken.Core;

using System.Text.Json;

public class FileWriterHandler : IToolHandler
{
    private readonly IPathResolver _pathResolver;

    public string Name => "write_file";

    public string Description => "Read file contents";

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
                        content = new
                        {
                            type = "string",
                            description = "The content to be writen"
                        },
                    },
                    required = new[] { "path", "content" }
                });


    public FileWriterHandler(IPathResolver pathResolver)
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

            string safePath = _pathResolver.ResolveSafePath(path);

            var dir = Path.GetDirectoryName(safePath);

            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var content = doc.RootElement.GetProperty("content").GetString()
                       ?? throw new MissingParameterException("content");

            await File.WriteAllTextAsync(safePath, content, System.Text.Encoding.UTF8);

            return $"file {safePath} writen";
        }
        catch (JsonException)
        {
            throw new ExecutionFailedException("Invalid Json property 'path' was expected.");
        }
    }
}

