namespace Loken.Core.Tests;

using Shouldly;

public class PathResolverTests
{
    private readonly string _basePath;
    private readonly PathResolver _resolver;

    public PathResolverTests()
    {
        _basePath  = Path.Combine(Path.GetTempPath(), $"loken_test_{Guid.NewGuid()}");
        _resolver = new PathResolver(_basePath);
    }

    [Theory]
    [InlineData("file.txt")]
    [InlineData("folder/script.sh")]
    [InlineData("./config.json")]
    [InlineData("deep/nested/folder/file.cpp")]
    public void Resolve_ValidRelativePaths_ShouldReturnFullPath(string relativePath)
    {
        var result = _resolver.ResolveSafePath(relativePath);
        result.ShouldStartWith(_basePath);
        result.ShouldEndWith(relativePath.Replace("./", ""));
    }

    [Fact]
    public void Resolve_PathWithDotDot_ShouldNormalizeCorrectly()
    {
        var relativePath = "folder/../test.txt";
        var result = _resolver.ResolveSafePath(relativePath);
        result.ShouldBe(Path.Combine(_basePath, "test.txt"));
    }

    [Theory]
    [InlineData("../passwd")]
    [InlineData("../../etc/shadow")]
    [InlineData("/etc/passwd")]
    [InlineData("folder/../../../home/vitor/.ssh/id_rsa")]
    public void Resolve_MaliciousPaths_ShouldThrowExecutionFailedException(string maliciousPath)
    {
        var exception = Should.Throw<ExecutionFailedException>(() =>
            _resolver.ResolveSafePath(maliciousPath)
        );

        exception.Message.ShouldContain("outside");
    }

    [Fact]
    public void Resolve_EmptyPath_ShouldReturnRootPath()
    {
        var result = _resolver.ResolveSafePath("");

        result.TrimEnd(Path.DirectorySeparatorChar)
              .ShouldBe(_basePath.TrimEnd(Path.DirectorySeparatorChar));
    }
}
