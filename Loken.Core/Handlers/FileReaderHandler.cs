namespace Loken.Core;

using System.Text.Json;

public class FileReaderHandler : IToolHandler
{
    public string WorkingDirectory { get; init; }

    public string Name => "read_file";

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
                            description = "The file path to read"
                        },
                        limit = new
                        {
                            type = "integer",
                            description = "Maximum numer of chars to read"
                        }

                    },
                    required = new[] { "path" }
                });


    public FileReaderHandler(string workingDirectory = ".")
    {
        WorkingDirectory = workingDirectory;
    }

    public async Task<string> ExecuteAsync(BinaryData input)
    {
        try
        {
            using var doc = JsonDocument.Parse(input);
            var path = doc.RootElement.GetProperty("path").GetString()
                       ?? throw new MissingParameterException("path");

            int limit = doc.RootElement.TryGetProperty("limit", out var limitProp)
                        ? limitProp.GetInt32()
                        : 50000;

            string safePath = ResolveSafePath(path);

            if (!File.Exists(safePath))
                throw new ExecutionFailedException($"File not found: {path}");

            char[] buffer = new char[limit];
            int charsRead;

            using (var reader = new StreamReader(safePath))
            {
                charsRead = await reader.ReadAsync(buffer, 0, limit);
            }

            var content = new string(buffer, 0, charsRead);

            var fileInfo = new FileInfo(safePath);
            if (fileInfo.Length > limit)
            {
                content += $"\n\n[WARNING: Read only the first {limit} chars to save context.]";
            }

            return content;
        }
        catch (JsonException)
        {
            throw new ExecutionFailedException("Invalid Json property 'path' was expected.");
        }
    }

    private string ResolveSafePath(string relativePath)
    {
        string workingDir = Path.GetFullPath(WorkingDirectory).TrimEnd(Path.DirectorySeparatorChar);
        string fullPath = Path.GetFullPath(Path.Combine(workingDir, relativePath));

        if (!fullPath.StartsWith(workingDir, StringComparison.OrdinalIgnoreCase))
            throw new ExecutionFailedException($"Directory outside working directory: {relativePath}");

        return fullPath;
    }
}
