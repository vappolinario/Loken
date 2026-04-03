namespace Loken.Core;

using Shouldly;
using System.Text.Json;
using System.Collections.Generic;

public class FileEditorHandlerTest : IDisposable
{
    private readonly string _testBasePath;
    private readonly PathResolver _pathResolver;
    private readonly FileEditorHandler _handler;

    public FileEditorHandlerTest()
    {
        _testBasePath = Path.Combine(Path.GetTempPath(), $"loken_editor_test_{Guid.NewGuid()}");
        _pathResolver = new PathResolver(_testBasePath);
        _handler = new FileEditorHandler(_pathResolver);
    }

    private async Task CreateTestFileAsync(string relativePath, string content)
    {
        var safePath = _pathResolver.ResolveSafePath(relativePath);
        var directory = Path.GetDirectoryName(safePath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        await File.WriteAllTextAsync(safePath, content);
    }

    [Fact]
    public async Task EditFile_ValidReplacement_ShouldUpdateFile()
    {
        var relativePath = "test.txt";
        var initialContent = "Hello, World! This is a test.";
        var oldText = "World";
        var newText = "Universe";
        var expectedContent = "Hello, Universe! This is a test.";

        await CreateTestFileAsync(relativePath, initialContent);
        var safePath = _pathResolver.ResolveSafePath(relativePath);

        var args = BinaryData.FromString($"{{\"path\": \"{relativePath}\", \"old_text\": \"{oldText}\", \"new_text\": \"{newText}\"}}");

        var result = await _handler.ExecuteAsync(args);

        result.ShouldBe($"File {relativePath} updated.");
        File.Exists(safePath).ShouldBeTrue();
        var fileContent = await File.ReadAllTextAsync(safePath);
        fileContent.ShouldBe(expectedContent);
    }

    [Fact]
    public async Task EditFile_MultipleOccurrences_ShouldReplaceAll()
    {
        var relativePath = "multi.txt";
        var initialContent = "test test test";
        var oldText = "test";
        var newText = "passed";
        var expectedContent = "passed passed passed";

        await CreateTestFileAsync(relativePath, initialContent);
        var safePath = _pathResolver.ResolveSafePath(relativePath);

        var args = BinaryData.FromString($"{{\"path\": \"{relativePath}\", \"old_text\": \"{oldText}\", \"new_text\": \"{newText}\"}}");

        var result = await _handler.ExecuteAsync(args);

        result.ShouldBe($"File {relativePath} updated.");
        var fileContent = await File.ReadAllTextAsync(safePath);
        fileContent.ShouldBe(expectedContent);
    }

    [Fact]
    public async Task EditFile_EmptyNewText_ShouldReplaceWithEmpty()
    {
        var relativePath = "empty.txt";
        var initialContent = "Remove this text";
        var oldText = " this";
        var newText = "";
        var expectedContent = "Remove text";

        await CreateTestFileAsync(relativePath, initialContent);
        var safePath = _pathResolver.ResolveSafePath(relativePath);

        var args = BinaryData.FromString($"{{\"path\": \"{relativePath}\", \"old_text\": \"{oldText}\", \"new_text\": \"{newText}\"}}");

        var result = await _handler.ExecuteAsync(args);

        result.ShouldBe($"File {relativePath} updated.");
        var fileContent = await File.ReadAllTextAsync(safePath);
        fileContent.ShouldBe(expectedContent);
    }

    [Fact]
    public async Task EditFile_SpecialCharacters_ShouldHandleCorrectly()
    {
        var relativePath = "special.txt";
        var initialContent = "Hello 🌍! 你好! Привет! 😊";
        var oldText = "🌍";
        var newText = "🚀";
        var expectedContent = "Hello 🚀! 你好! Привет! 😊";

        await CreateTestFileAsync(relativePath, initialContent);
        var safePath = _pathResolver.ResolveSafePath(relativePath);

        var args = BinaryData.FromString($"{{\"path\": \"{relativePath}\", \"old_text\": \"{JsonEncodedText.Encode(oldText)}\", \"new_text\": \"{JsonEncodedText.Encode(newText)}\"}}");

        var result = await _handler.ExecuteAsync(args);

        result.ShouldBe($"File {relativePath} updated.");
        var fileContent = await File.ReadAllTextAsync(safePath);
        fileContent.ShouldBe(expectedContent);
    }

    [Fact]
    public async Task EditFile_MultilineText_ShouldReplaceCorrectly()
    {
        var relativePath = "multiline.txt";
        var initialContent = "Line 1\nLine 2\nLine 3";
        var oldText = "Line 2";
        var newText = "Middle Line";
        var expectedContent = "Line 1\nMiddle Line\nLine 3";

        await CreateTestFileAsync(relativePath, initialContent);
        var safePath = _pathResolver.ResolveSafePath(relativePath);

        var args = BinaryData.FromString($"{{\"path\": \"{relativePath}\", \"old_text\": \"{JsonEncodedText.Encode(oldText)}\", \"new_text\": \"{JsonEncodedText.Encode(newText)}\"}}");

        var result = await _handler.ExecuteAsync(args);

        result.ShouldBe($"File {relativePath} updated.");
        var fileContent = await File.ReadAllTextAsync(safePath);
        fileContent.ShouldBe(expectedContent);
    }

    [Fact]
    public async Task EditFile_FileNotFound_ShouldThrowExecutionFailedException()
    {
        var relativePath = "nonexistent.txt";
        var args = BinaryData.FromString($"{{\"path\": \"{relativePath}\", \"old_text\": \"test\", \"new_text\": \"updated\"}}");

        await Should.ThrowAsync<ExecutionFailedException>(async () =>
            await _handler.ExecuteAsync(args)
        );
    }

    [Fact]
    public async Task EditFile_OldTextNotFound_ShouldThrowExecutionFailedException()
    {
        var relativePath = "test.txt";
        var initialContent = "Hello, World!";
        await CreateTestFileAsync(relativePath, initialContent);

        var args = BinaryData.FromString($"{{\"path\": \"{relativePath}\", \"old_text\": \"NotFound\", \"new_text\": \"updated\"}}");

        await Should.ThrowAsync<ExecutionFailedException>(async () =>
            await _handler.ExecuteAsync(args)
        );
    }

    [Fact]
    public async Task EditFile_MissingPathParameter_ShouldThrowKeyNotFoundException()
    {
        var args = BinaryData.FromString("{\"old_text\": \"test\", \"new_text\": \"updated\"}");

        await Should.ThrowAsync<KeyNotFoundException>(async () =>
            await _handler.ExecuteAsync(args)
        );
    }

    [Fact]
    public async Task EditFile_MissingOldTextParameter_ShouldThrowKeyNotFoundException()
    {
        var args = BinaryData.FromString("{\"path\": \"test.txt\", \"new_text\": \"updated\"}");

        await Should.ThrowAsync<KeyNotFoundException>(async () =>
            await _handler.ExecuteAsync(args)
        );
    }

    [Fact]
    public async Task EditFile_MissingNewTextParameter_ShouldThrowKeyNotFoundException()
    {
        var args = BinaryData.FromString("{\"path\": \"test.txt\", \"old_text\": \"test\"}");

        await Should.ThrowAsync<KeyNotFoundException>(async () =>
            await _handler.ExecuteAsync(args)
        );
    }

    [Fact]
    public async Task EditFile_EmptyOldText_ShouldThrowMissingParameterException()
    {
        var relativePath = "test.txt";
        var initialContent = "Hello, World!";
        await CreateTestFileAsync(relativePath, initialContent);

        var args = BinaryData.FromString($"{{\"path\": \"{relativePath}\", \"old_text\": \"\", \"new_text\": \"updated\"}}");

        await Should.ThrowAsync<MissingParameterException>(async () =>
            await _handler.ExecuteAsync(args)
        );
    }

    [Fact]
    public async Task EditFile_WithDotInPath_ShouldNormalizeCorrectly()
    {
        var relativePath = "./current/test.txt";
        var initialContent = "Replace me";
        var oldText = "Replace me";
        var newText = "Replaced";

        var normalizedPath = "current/test.txt";
        await CreateTestFileAsync(normalizedPath, initialContent);
        var safePath = _pathResolver.ResolveSafePath(normalizedPath);

        var args = BinaryData.FromString($"{{\"path\": \"{relativePath}\", \"old_text\": \"{oldText}\", \"new_text\": \"{newText}\"}}");

        var result = await _handler.ExecuteAsync(args);

        result.ShouldBe($"File {relativePath} updated.");
        File.Exists(safePath).ShouldBeTrue();
        var fileContent = await File.ReadAllTextAsync(safePath);
        fileContent.ShouldBe(newText);
    }

    [Fact]
    public async Task EditFile_WithDotDotInPath_ShouldNormalizeCorrectly()
    {
        var relativePath = "folder/../test.txt";
        var initialContent = "Original content";
        var oldText = "Original";
        var newText = "Updated";

        var normalizedPath = "test.txt";
        await CreateTestFileAsync(normalizedPath, initialContent);
        var safePath = _pathResolver.ResolveSafePath(normalizedPath);

        var args = BinaryData.FromString($"{{\"path\": \"{relativePath}\", \"old_text\": \"{oldText}\", \"new_text\": \"{newText}\"}}");

        var result = await _handler.ExecuteAsync(args);

        result.ShouldBe($"File {relativePath} updated.");
        File.Exists(safePath).ShouldBeTrue();
        var fileContent = await File.ReadAllTextAsync(safePath);
        fileContent.ShouldBe("Updated content");
    }

    [Theory]
    [InlineData("../malicious.txt")]
    [InlineData("../../etc/passwd")]
    [InlineData("folder/../../../home/user/.ssh/id_rsa")]
    public async Task EditFile_MaliciousPathTraversal_ShouldThrowExecutionFailedException(string maliciousPath)
    {
        var args = BinaryData.FromString($"{{\"path\": \"{maliciousPath}\", \"old_text\": \"test\", \"new_text\": \"malicious\"}}");

        await Should.ThrowAsync<ExecutionFailedException>(async () =>
            await _handler.ExecuteAsync(args)
        );
    }

    [Fact]
    public async Task EditFile_InvalidJson_ShouldThrowExecutionFailedException()
    {
        var args = BinaryData.FromString("invalid json");

        await Should.ThrowAsync<ExecutionFailedException>(async () =>
            await _handler.ExecuteAsync(args)
        );
    }

    [Fact]
    public async Task EditFile_JsonMissingRequiredProperties_ShouldThrowKeyNotFoundException()
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

        name.ShouldBe("edit_file");
        description.ShouldBe("Replace exact text in a file");
        parameters.ShouldNotBeNull();
    }

    [Fact]
    public async Task EditFile_CaseSensitiveReplacement_ShouldRespectCase()
    {
        var relativePath = "case.txt";
        var initialContent = "Hello WORLD world WoRlD";
        var oldText = "WORLD";
        var newText = "Universe";
        var expectedContent = "Hello Universe world WoRlD";

        await CreateTestFileAsync(relativePath, initialContent);
        var safePath = _pathResolver.ResolveSafePath(relativePath);

        var args = BinaryData.FromString($"{{\"path\": \"{relativePath}\", \"old_text\": \"{oldText}\", \"new_text\": \"{newText}\"}}");

        var result = await _handler.ExecuteAsync(args);

        result.ShouldBe($"File {relativePath} updated.");
        var fileContent = await File.ReadAllTextAsync(safePath);
        fileContent.ShouldBe(expectedContent);
    }

    [Fact]
    public async Task EditFile_WhitespaceInOldText_ShouldMatchExactly()
    {
        var relativePath = "whitespace.txt";
        var initialContent = "Hello   World";
        var oldText = "   ";
        var newText = "-";
        var expectedContent = "Hello-World";

        await CreateTestFileAsync(relativePath, initialContent);
        var safePath = _pathResolver.ResolveSafePath(relativePath);

        var args = BinaryData.FromString($"{{\"path\": \"{relativePath}\", \"old_text\": \"{oldText}\", \"new_text\": \"{newText}\"}}");

        var result = await _handler.ExecuteAsync(args);

        result.ShouldBe($"File {relativePath} updated.");
        var fileContent = await File.ReadAllTextAsync(safePath);
        fileContent.ShouldBe(expectedContent);
    }

    [Fact]
    public async Task EditFile_NewTextContainsOldText_ShouldNotCauseInfiniteLoop()
    {
        var relativePath = "recursive.txt";
        var initialContent = "test";
        var oldText = "test";
        var newText = "testtest";

        await CreateTestFileAsync(relativePath, initialContent);
        var safePath = _pathResolver.ResolveSafePath(relativePath);

        var args = BinaryData.FromString($"{{\"path\": \"{relativePath}\", \"old_text\": \"{oldText}\", \"new_text\": \"{newText}\"}}");

        var result = await _handler.ExecuteAsync(args);

        result.ShouldBe($"File {relativePath} updated.");
        var fileContent = await File.ReadAllTextAsync(safePath);
        fileContent.ShouldBe("testtest");
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
