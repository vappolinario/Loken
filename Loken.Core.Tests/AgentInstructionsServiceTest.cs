using Shouldly;

namespace Loken.Core.Tests;

public class AgentInstructionsServiceTest
{
    private const string FileName = "AGENTS.md";

    [Fact]
    public void LoadInstructions_WhenAgentsMdExistsInWorkingDir_ReturnsContent()
    {
        using var fixture = new DirectoryFixture();
        var expectedContent = "# Project Instructions\n\nTest content.";
        File.WriteAllText(Path.Combine(fixture.WorkingDir, FileName), expectedContent);

        var resolver = new PathResolver(fixture.WorkingDir);
        var service = new AgentInstructionsService(resolver);

        var result = service.LoadInstructions();

        result.ShouldBe(expectedContent);
    }

    [Fact]
    public void LoadInstructions_WhenAgentsMdExistsInParentDir_ReturnsContent()
    {
        using var fixture = new DirectoryFixture();
        var expectedContent = "# Parent Project Instructions\n\nParent content.";
        File.WriteAllText(Path.Combine(fixture.ParentDir, FileName), expectedContent);

        var resolver = new PathResolver(fixture.WorkingDir);
        var service = new AgentInstructionsService(resolver);

        var result = service.LoadInstructions();

        result.ShouldBe(expectedContent);
    }

    [Fact]
    public void LoadInstructions_WhenNoAgentsMdExists_ReturnsNull()
    {
        using var fixture = new DirectoryFixture();

        var resolver = new PathResolver(fixture.WorkingDir);
        var service = new AgentInstructionsService(resolver);

        var result = service.LoadInstructions();

        result.ShouldBeNull();
    }

    [Fact]
    public void LoadInstructions_WhenMultipleAgentsMdExist_FindsNearest()
    {
        using var fixture = new DirectoryFixture();
        var nearestContent = "# Nearest Instructions\n\nNearest content.";
        var parentContent = "# Parent Instructions\n\nParent content.";
        File.WriteAllText(Path.Combine(fixture.WorkingDir, FileName), nearestContent);
        File.WriteAllText(Path.Combine(fixture.ParentDir, FileName), parentContent);

        var resolver = new PathResolver(fixture.WorkingDir);
        var service = new AgentInstructionsService(resolver);

        var result = service.LoadInstructions();

        result.ShouldBe(nearestContent);
    }

    /// <summary>
    /// Creates a temporary directory structure for testing upward traversal.
    ///
    /// Structure:
    ///   {baseDir}/
    ///     parent/
    ///       working/
    /// </summary>
    private sealed class DirectoryFixture : IDisposable
    {
        public string BaseDir { get; }
        public string ParentDir { get; }
        public string WorkingDir { get; }

        public DirectoryFixture()
        {
            BaseDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            ParentDir = Path.Combine(BaseDir, "parent");
            WorkingDir = Path.Combine(ParentDir, "working");

            Directory.CreateDirectory(WorkingDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(BaseDir))
                Directory.Delete(BaseDir, recursive: true);
        }
    }
}
