namespace Loken.Core;

using Shouldly;
using System.Collections.Generic;

public class FileReaderHandlerTest : IDisposable
{
    private readonly string _testBasePath;
    private readonly PathResolver _pathResolver;
    private readonly FileReaderHandler _handler;

    public FileReaderHandlerTest()
    {
        _testBasePath = Path.Combine(Path.GetTempPath(), $"loken_test_{Guid.NewGuid()}");
        _pathResolver = new PathResolver(_testBasePath);
        _handler = new FileReaderHandler(_pathResolver);

        Directory.CreateDirectory(_testBasePath);
    }

    [Fact]
    public async Task ReadFile_MaliciousPathTraversal_ShouldThrowExecutionFailedException()
    {
        var handler = new FileReaderHandler(new PathResolver("/tmp/safe_zone"));
        var maliciousArgs = BinaryData.FromString("{\"path\": \"../../../etc/passwd\"}");

        await Should.ThrowAsync<ExecutionFailedException>(async () =>
            await handler.ExecuteAsync(maliciousArgs)
        );
    }

    [Fact]
    public async Task ReadFile_ValidFile_ShouldReturnContent()
    {
        var relativePath = "test.txt";
        var content = "Hello, World!";
        var fullPath = Path.Combine(_testBasePath, relativePath);

        await File.WriteAllTextAsync(fullPath, content);

        var args = BinaryData.FromString($"{{\"path\": \"{relativePath}\"}}");

        var result = await _handler.ExecuteAsync(args);

        result.ShouldBe(content);
    }

    [Fact]
    public async Task ReadFile_WithCustomLimit_ShouldRespectLimit()
    {
        var relativePath = "large.txt";
        var content = new string('A', 100) + new string('B', 100);
        var fullPath = Path.Combine(_testBasePath, relativePath);

        await File.WriteAllTextAsync(fullPath, content);

        var limit = 150;
        var args = BinaryData.FromString($"{{\"path\": \"{relativePath}\", \"limit\": {limit}}}");

        var result = await _handler.ExecuteAsync(args);

        result.ShouldStartWith(new string('A', 100) + new string('B', 50));
        result.ShouldContain($"[WARNING: Read only the first {limit} chars to save context.]");
    }

    [Fact]
    public async Task ReadFile_WithoutLimit_ShouldUseDefaultLimit()
    {
        var relativePath = "default_limit.txt";
        var content = new string('X', 60000);
        var fullPath = Path.Combine(_testBasePath, relativePath);

        await File.WriteAllTextAsync(fullPath, content);

        var args = BinaryData.FromString($"{{\"path\": \"{relativePath}\"}}");

        var result = await _handler.ExecuteAsync(args);

        result.Length.ShouldBeGreaterThan(50000);
        result.ShouldContain("[WARNING: Read only the first 50000 chars to save context.]");
    }

    [Fact]
    public async Task ReadFile_FileExceedsLimit_ShouldAddWarning()
    {
        var relativePath = "exceeds_limit.txt";
        var content = new string('Z', 1000);
        var fullPath = Path.Combine(_testBasePath, relativePath);

        await File.WriteAllTextAsync(fullPath, content);

        var limit = 500;
        var args = BinaryData.FromString($"{{\"path\": \"{relativePath}\", \"limit\": {limit}}}");

        var result = await _handler.ExecuteAsync(args);

        result.ShouldContain($"[WARNING: Read only the first {limit} chars to save context.]");
        result.Length.ShouldBeGreaterThan(limit);
    }

    [Fact]
    public async Task ReadFile_FileWithinLimit_ShouldNotAddWarning()
    {
        var relativePath = "within_limit.txt";
        var content = "Short content";
        var fullPath = Path.Combine(_testBasePath, relativePath);

        await File.WriteAllTextAsync(fullPath, content);

        var limit = 1000;
        var args = BinaryData.FromString($"{{\"path\": \"{relativePath}\", \"limit\": {limit}}}");

        var result = await _handler.ExecuteAsync(args);

        result.ShouldBe(content);
        result.ShouldNotContain("WARNING:");
    }

    [Fact]
    public async Task ReadFile_FileNotFound_ShouldThrowExecutionFailedException()
    {
        var args = BinaryData.FromString("{\"path\": \"nonexistent.txt\"}");

        await Should.ThrowAsync<ExecutionFailedException>(async () =>
            await _handler.ExecuteAsync(args)
        );
    }

    [Fact]
    public async Task ReadFile_MissingPathParameter_ShouldThrowKeyNotFoundException()
    {
        var args = BinaryData.FromString("{\"limit\": 100}");

        await Should.ThrowAsync<KeyNotFoundException>(async () =>
            await _handler.ExecuteAsync(args)
        );
    }

    [Fact]
    public async Task ReadFile_InvalidJson_ShouldThrowExecutionFailedException()
    {
        var args = BinaryData.FromString("invalid json");

        await Should.ThrowAsync<ExecutionFailedException>(async () =>
            await _handler.ExecuteAsync(args)
        );
    }

    [Fact]
    public async Task ReadFile_NullPathValue_ShouldThrowMissingParameterException()
    {
        var args = BinaryData.FromString("{\"path\": null}");

        await Should.ThrowAsync<MissingParameterException>(async () =>
            await _handler.ExecuteAsync(args)
        );
    }

    [Fact]
    public async Task ReadFile_EmptyPath_ShouldThrowExecutionFailedExceptionForNotFound()
    {
        var args = BinaryData.FromString("{\"path\": \"\"}");

        await Should.ThrowAsync<ExecutionFailedException>(async () =>
            await _handler.ExecuteAsync(args)
        );
    }

    [Fact]
    public async Task ReadFile_SpecialCharacters_ShouldHandleUTF8Correctly()
    {
        var relativePath = "unicode.txt";
        var content = "Hello 🌍! 你好! Привет! 😊";
        var fullPath = Path.Combine(_testBasePath, relativePath);

        await File.WriteAllTextAsync(fullPath, content, System.Text.Encoding.UTF8);

        var args = BinaryData.FromString($"{{\"path\": \"{relativePath}\"}}");

        var result = await _handler.ExecuteAsync(args);

        result.ShouldBe(content);
    }

    [Fact]
    public async Task ReadFile_MultilineContent_ShouldPreserveNewlines()
    {
        var relativePath = "multiline.txt";
        var content = "Line 1\nLine 2\r\nLine 3";
        var fullPath = Path.Combine(_testBasePath, relativePath);

        await File.WriteAllTextAsync(fullPath, content);

        var args = BinaryData.FromString($"{{\"path\": \"{relativePath}\"}}");

        var result = await _handler.ExecuteAsync(args);

        result.ShouldBe(content);
    }

    [Fact]
    public async Task ReadFile_NestedDirectory_ShouldReadFromCorrectPath()
    {
        var relativePath = "nested/folder/test.txt";
        var content = "Nested content";
        var fullPath = Path.Combine(_testBasePath, relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(fullPath, content);

        var args = BinaryData.FromString($"{{\"path\": \"{relativePath}\"}}");

        var result = await _handler.ExecuteAsync(args);

        result.ShouldBe(content);
    }

    [Fact]
    public async Task ReadFile_WithDotInPath_ShouldNormalizeCorrectly()
    {
        var actualPath = "folder/test.txt";
        var content = "Actual content";
        var fullPath = Path.Combine(_testBasePath, actualPath);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(fullPath, content);

        var args = BinaryData.FromString("{\"path\": \"./folder/test.txt\"}");

        var result = await _handler.ExecuteAsync(args);

        result.ShouldBe(content);
    }

    [Fact]
    public async Task ReadFile_WithDotDotInPath_ShouldNormalizeCorrectly()
    {
        var actualPath = "test.txt";
        var content = "Normalized content";
        var fullPath = Path.Combine(_testBasePath, actualPath);

        await File.WriteAllTextAsync(fullPath, content);

        var subDir = Path.Combine(_testBasePath, "subdir");
        Directory.CreateDirectory(subDir);

        var args = BinaryData.FromString("{\"path\": \"subdir/../test.txt\"}");

        var result = await _handler.ExecuteAsync(args);

        result.ShouldBe(content);
    }

    [Theory]
    [InlineData("../malicious.txt")]
    [InlineData("../../etc/passwd")]
    [InlineData("folder/../../../home/user/.ssh/id_rsa")]
    public async Task ReadFile_VariousMaliciousPaths_ShouldThrowExecutionFailedException(string maliciousPath)
    {
        var args = BinaryData.FromString($"{{\"path\": \"{maliciousPath}\"}}");

        await Should.ThrowAsync<ExecutionFailedException>(async () =>
            await _handler.ExecuteAsync(args)
        );
    }

    [Fact]
    public async Task ReadFile_InvalidLimit_ShouldThrowException()
    {
        var relativePath = "test.txt";
        var content = "Test content";
        var fullPath = Path.Combine(_testBasePath, relativePath);

        await File.WriteAllTextAsync(fullPath, content);

        var args = BinaryData.FromString($"{{\"path\": \"{relativePath}\", \"limit\": -1}}");

        await Should.ThrowAsync<OverflowException>(async () =>
            await _handler.ExecuteAsync(args)
        );
    }

    [Fact]
    public async Task ReadFile_ZeroLimit_ShouldReturnOnlyWarning()
    {
        var relativePath = "test.txt";
        var content = "Test content";
        var fullPath = Path.Combine(_testBasePath, relativePath);

        await File.WriteAllTextAsync(fullPath, content);

        var args = BinaryData.FromString($"{{\"path\": \"{relativePath}\", \"limit\": 0}}");

        var result = await _handler.ExecuteAsync(args);

        result.ShouldContain("[WARNING: Read only the first 0 chars to save context.]");
        result.Trim().ShouldBe("[WARNING: Read only the first 0 chars to save context.]");
    }

    [Fact]
    public async Task ReadFile_VeryLargeLimit_ShouldHandleGracefully()
    {
        var relativePath = "test.txt";
        var content = "Small content";
        var fullPath = Path.Combine(_testBasePath, relativePath);

        await File.WriteAllTextAsync(fullPath, content);

        var largeLimit = 1000000;
        var args = BinaryData.FromString($"{{\"path\": \"{relativePath}\", \"limit\": {largeLimit}}}");

        var result = await _handler.ExecuteAsync(args);

        result.ShouldBe(content);
    }

    [Fact]
    public void Handler_Properties_ShouldReturnCorrectValues()
    {
        var name = _handler.Name;
        var description = _handler.Description;
        var parameters = _handler.Parameters;

        name.ShouldBe("read_file");
        description.ShouldBe("Read file contents");
        parameters.ShouldNotBeNull();

        var paramsJson = parameters.ToString();
        paramsJson.ShouldContain("path");
        paramsJson.ShouldContain("limit");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testBasePath))
                Directory.Delete(_testBasePath, true);
        }
        catch
        {
        }
    }
}
