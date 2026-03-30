namespace Loken.Core;

using Shouldly;
using System.Text.Json;
using System.Collections.Generic;

public class FileWriterHandlerTest : IDisposable
{
    private readonly string _testBasePath;
    private readonly PathResolver _pathResolver;
    private readonly FileWriterHandler _handler;

    public FileWriterHandlerTest()
    {
        _testBasePath = Path.Combine(Path.GetTempPath(), $"loken_test_{Guid.NewGuid()}");
        _pathResolver = new PathResolver(_testBasePath);
        _handler = new FileWriterHandler(_pathResolver);
    }

    [Fact]
    public async Task WriteFile_ValidPathAndContent_ShouldCreateFile()
    {
        var relativePath = "test.txt";
        var content = "Hello, World!";
        var args = BinaryData.FromString($"{{\"path\": \"{relativePath}\", \"content\": \"{content}\"}}");

        var result = await _handler.ExecuteAsync(args);
        var fullPath = Path.Combine(_testBasePath, relativePath);

        result.ShouldBe($"file {fullPath} writen");
        File.Exists(fullPath).ShouldBeTrue();
        var fileContent = await File.ReadAllTextAsync(fullPath);
        fileContent.ShouldBe(content);
    }

    [Fact]
    public async Task WriteFile_NestedDirectory_ShouldCreateDirectoryAndFile()
    {
        var relativePath = "nested/folder/test.txt";
        var content = "Nested content";
        var args = BinaryData.FromString($"{{\"path\": \"{relativePath}\", \"content\": \"{content}\"}}");

        var result = await _handler.ExecuteAsync(args);
        var fullPath = Path.Combine(_testBasePath, relativePath);

        result.ShouldBe($"file {fullPath} writen");
        File.Exists(fullPath).ShouldBeTrue();
        var fileContent = await File.ReadAllTextAsync(fullPath);
        fileContent.ShouldBe(content);
    }

    [Fact]
    public async Task WriteFile_WithDotInPath_ShouldNormalizeCorrectly()
    {
        var relativePath = "./current/test.txt";
        var content = "Current directory content";
        var args = BinaryData.FromString($"{{\"path\": \"{relativePath}\", \"content\": \"{content}\"}}");

        var result = await _handler.ExecuteAsync(args);
        var expectedPath = Path.Combine(_testBasePath, "current/test.txt");

        result.ShouldBe($"file {expectedPath} writen");
        File.Exists(expectedPath).ShouldBeTrue();
    }

    [Fact]
    public async Task WriteFile_WithDotDotInPath_ShouldNormalizeCorrectly()
    {
        var relativePath = "folder/../test.txt";
        var content = "Normalized content";
        var args = BinaryData.FromString($"{{\"path\": \"{relativePath}\", \"content\": \"{content}\"}}");

        var result = await _handler.ExecuteAsync(args);
        var expectedPath = Path.Combine(_testBasePath, "test.txt");

        result.ShouldBe($"file {expectedPath} writen");
        File.Exists(expectedPath).ShouldBeTrue();
    }

    [Theory]
    [InlineData("../malicious.txt")]
    [InlineData("../../etc/passwd")]
    [InlineData("folder/../../../home/user/.ssh/id_rsa")]
    public async Task WriteFile_MaliciousPathTraversal_ShouldThrowExecutionFailedException(string maliciousPath)
    {
        var args = BinaryData.FromString($"{{\"path\": \"{maliciousPath}\", \"content\": \"malicious\"}}");

        await Should.ThrowAsync<ExecutionFailedException>(async () =>
            await _handler.ExecuteAsync(args)
        );
    }

    [Fact]
    public async Task WriteFile_MissingPathParameter_ShouldThrowKeyNotFoundException()
    {
        var args = BinaryData.FromString("{\"content\": \"test\"}");

        await Should.ThrowAsync<KeyNotFoundException>(async () =>
            await _handler.ExecuteAsync(args)
        );
    }

    [Fact]
    public async Task WriteFile_MissingContentParameter_ShouldThrowKeyNotFoundException()
    {
        var args = BinaryData.FromString("{\"path\": \"test.txt\"}");

        await Should.ThrowAsync<KeyNotFoundException>(async () =>
            await _handler.ExecuteAsync(args)
        );
    }

    [Fact]
    public async Task WriteFile_EmptyContent_ShouldCreateFileWithEmptyContent()
    {
        var relativePath = "empty.txt";
        var args = BinaryData.FromString($"{{\"path\": \"{relativePath}\", \"content\": \"\"}}");

        var result = await _handler.ExecuteAsync(args);
        var fullPath = Path.Combine(_testBasePath, relativePath);

        result.ShouldBe($"file {fullPath} writen");
        File.Exists(fullPath).ShouldBeTrue();
        var fileContent = await File.ReadAllTextAsync(fullPath);
        fileContent.ShouldBeEmpty();
    }

    [Fact]
    public async Task WriteFile_SpecialCharactersInContent_ShouldHandleUTF8Correctly()
    {
        var relativePath = "special.txt";
        var content = "Hello 🌍! 你好! Привет! 😊";
        var args = BinaryData.FromString($"{{\"path\": \"{relativePath}\", \"content\": \"{JsonEncodedText.Encode(content)}\"}}");

        var result = await _handler.ExecuteAsync(args);
        var fullPath = Path.Combine(_testBasePath, relativePath);

        result.ShouldBe($"file {fullPath} writen");
        File.Exists(fullPath).ShouldBeTrue();
        var fileContent = await File.ReadAllTextAsync(fullPath);
        fileContent.ShouldBe(content);
    }

    [Fact]
    public async Task WriteFile_MultilineContent_ShouldPreserveNewlines()
    {
        var relativePath = "multiline.txt";
        var content = "Line 1\nLine 2\r\nLine 3";
        var args = BinaryData.FromString($"{{\"path\": \"{relativePath}\", \"content\": \"{JsonEncodedText.Encode(content)}\"}}");

        var result = await _handler.ExecuteAsync(args);
        var fullPath = Path.Combine(_testBasePath, relativePath);

        result.ShouldBe($"file {fullPath} writen");
        File.Exists(fullPath).ShouldBeTrue();
        var fileContent = await File.ReadAllTextAsync(fullPath);
        fileContent.ShouldBe(content);
    }

    [Fact]
    public async Task WriteFile_OverwriteExistingFile_ShouldReplaceContent()
    {
        var relativePath = "overwrite.txt";
        var initialContent = "Initial content";
        var newContent = "New content";

        var initialArgs = BinaryData.FromString($"{{\"path\": \"{relativePath}\", \"content\": \"{initialContent}\"}}");
        await _handler.ExecuteAsync(initialArgs);

        var fullPath = Path.Combine(_testBasePath, relativePath);
        var initialFileContent = await File.ReadAllTextAsync(fullPath);
        initialFileContent.ShouldBe(initialContent);

        var newArgs = BinaryData.FromString($"{{\"path\": \"{relativePath}\", \"content\": \"{newContent}\"}}");
        var result = await _handler.ExecuteAsync(newArgs);

        result.ShouldBe($"file {fullPath} writen");
        var finalFileContent = await File.ReadAllTextAsync(fullPath);
        finalFileContent.ShouldBe(newContent);
    }

    [Fact]
    public async Task WriteFile_InvalidJson_ShouldThrowExecutionFailedException()
    {
        var args = BinaryData.FromString("invalid json");

        await Should.ThrowAsync<ExecutionFailedException>(async () =>
            await _handler.ExecuteAsync(args)
        );
    }

    [Fact]
    public async Task WriteFile_JsonMissingRequiredProperties_ShouldThrowKeyNotFoundException()
    {
        var args = BinaryData.FromString("{\"wrong\": \"property\"}");

        await Should.ThrowAsync<KeyNotFoundException>(async () =>
            await _handler.ExecuteAsync(args)
        );
    }

    [Fact]
    public void Handler_Properties_ShouldReturnCorrectValues()
    {
        var name = _handler.Name;
        var description = _handler.Description;
        var parameters = _handler.Parameters;

        name.ShouldBe("write_file");
        description.ShouldBe( "Write content to a file");
        parameters.ShouldNotBeNull();
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testBasePath))
            {
                Directory.Delete(_testBasePath, true);
            }
        }
        catch
        {
          // ignore cleanup errors
        }
    }
}
