#pragma warning disable OPENAI001
namespace Loken.Core.Tests;

using System;
using System.Collections.Generic;
using Shouldly;
using Xunit;

public class ToolServiceTest
{
    [Fact]
    public void GetTools_NoHandlers_ReturnsEmptyString()
    {
        var handlers = new List<IToolHandler>();
        var service = new ToolService(handlers);

        var result = service.GetTools();

        result.ShouldBeEmpty();
    }

    [Fact]
    public void GetTools_SingleHandler_ReturnsFormattedEntry()
    {
        var handlers = new List<IToolHandler>
        {
            new StubHandler("bash", "Execute shell commands")
        };
        var service = new ToolService(handlers);

        var result = service.GetTools();

        result.ShouldBe("  - bash: Execute shell commands");
    }

    [Fact]
    public void GetTools_MultipleHandlers_ReturnsSortedFormattedList()
    {
        var handlers = new List<IToolHandler>
        {
            new StubHandler("write_file", "Write content to a file"),
            new StubHandler("bash", "Execute shell commands"),
            new StubHandler("read_file", "Read file contents")
        };
        var service = new ToolService(handlers);

        var result = service.GetTools();

        var lines = result.Split(Environment.NewLine);
        lines.Length.ShouldBe(3);
        lines[0].ShouldBe("  - bash: Execute shell commands");
        lines[1].ShouldBe("  - read_file: Read file contents");
        lines[2].ShouldBe("  - write_file: Write content to a file");
    }

    [Fact]
    public void GetTools_HandlersWithMixedNames_OrdersAlphabetically()
    {
        var handlers = new List<IToolHandler>
        {
            new StubHandler("X-ray", "Scan project structure"),
            new StubHandler("Alpha", "First tool"),
            new StubHandler("Beta", "Second tool")
        };
        var service = new ToolService(handlers);

        var result = service.GetTools();

        var lines = result.Split(Environment.NewLine);
        lines[0].ShouldBe("  - Alpha: First tool");
        lines[1].ShouldBe("  - Beta: Second tool");
        lines[2].ShouldBe("  - X-ray: Scan project structure");
    }

    [Fact]
    public void Constructor_NullHandlers_ThrowsArgumentNullException()
    {
        var exception = Should.Throw<ArgumentNullException>(() => new ToolService(null!));
        exception.ParamName.ShouldBe("handlers");
    }

    /// <summary>
    /// A minimal stub implementing <see cref="IToolHandler"/> for test purposes.
    /// Only <see cref="Name"/> and <see cref="Description"/> are populated,
    /// as <see cref="ToolService"/> does not invoke <see cref="ExecuteAsync"/>
    /// or read <see cref="Parameters"/>.
    /// </summary>
    private sealed class StubHandler : IToolHandler
    {
        public string Name { get; }
        public string Description { get; }
        public BinaryData Parameters { get; } = BinaryData.Empty;

        public StubHandler(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public Task<string> ExecuteAsync(BinaryData input)
        {
            throw new NotSupportedException("Not intended for use in ToolService tests.");
        }
    }
}
