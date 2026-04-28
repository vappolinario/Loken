namespace Loken.Core;

using Loken.Core.Tests;
using Shouldly;

public class SystemEditorServiceTest
{
  [Fact]
  public async Task EditAsync_ReturnsContent_FromTestEditorService()
  {
    var expected = "This is my prompt for the AI.";
    var service = TestEditorService.Returning(expected);

    var result = await service.EditAsync();

    result.ShouldBe(expected);
  }

  [Fact]
  public async Task EditAsync_ReturnsNull_WhenCancelled()
  {
    var service = TestEditorService.Cancelled();

    var result = await service.EditAsync();

    result.ShouldBeNull();
  }

  [Fact]
  public async Task EditAsync_ReturnsRawContentIncludingHeader_WhenUsingWithDefaultHeader()
  {
    var userContent = "Tell me about the Imperium.";
    var service = TestEditorService.WithDefaultHeader(userContent);

    var result = await service.EditAsync();

    // TestEditorService.WithDefaultHeader prepends the header to simulate raw file
    // content as saved by the editor. It does NOT strip the header — that's the
    // responsibility of SystemEditorService.
    result.ShouldBe("# Write your prompt here\n\nTell me about the Imperium.");
  }

  [Fact]
  public async Task EditAsync_PreservesInitialContent_WhenSet()
  {
    var initial = "# Custom header\n\nUser wrote this directly.";
    var service = TestEditorService.Returning(initial);

    var result = await service.EditAsync(initial);

    result.ShouldBe(initial);
  }

  [Fact]
  public async Task EditAsync_TracksCallCount()
  {
    var service = TestEditorService.Returning("hello");

    service.CallCount.ShouldBe(0);

    await service.EditAsync();
    service.CallCount.ShouldBe(1);

    await service.EditAsync();
    service.CallCount.ShouldBe(2);
  }

  [Fact]
  public async Task EditAsync_CapturesInitialContent()
  {
    var initial = "some initial text";
    var service = TestEditorService.Returning("result");

    await service.EditAsync(initial);

    service.LastInitialContent.ShouldBe(initial);
  }

  [Fact]
  public async Task EditAsync_NullResult_WhenCustomHandlerReturnsNull()
  {
    var service = new TestEditorService((_) => Task.FromResult<string?>(null));

    var result = await service.EditAsync("anything");

    result.ShouldBeNull();
  }

  [Fact]
  public async Task EditAsync_ReturnsInitialContent_WhenHandlerIsNull()
  {
    var service = new TestEditorService(default(Func<string?, Task<string?>>?));

    var result = await service.EditAsync("preserve me");

    result.ShouldBe("preserve me");
  }

  [Fact]
  public async Task EditAsync_CanSimulate_AppendedContent()
  {
    var service = new TestEditorService((initial) =>
        Task.FromResult<string?>(initial + "\n\nMore text added by user."));

    var result = await service.EditAsync("Original text.");

    result.ShouldBe("Original text.\n\nMore text added by user.");
  }
}
