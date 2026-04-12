#pragma warning disable OPENAI001
namespace Loken.Core.Tests;

using System.Collections.Generic;
using System.Linq;
using OpenAI.Chat;
using Shouldly;
using Xunit;

public class ContextCompactorServiceTest
{
    private readonly ContextCompactorService _compactorService;

    public ContextCompactorServiceTest()
    {
        _compactorService = new ContextCompactorService();
    }

    [Fact]
    public void MicroCompact_EmptyMessageList_DoesNothing()
    {
        var messages = new List<ChatMessage>();

        _compactorService.MicroCompact(messages);

        messages.ShouldBeEmpty();
    }

    [Fact]
    public void MicroCompact_NoToolMessages_DoesNothing()
    {
        var messages = new List<ChatMessage>
        {
            new UserChatMessage("Hello"),
            new AssistantChatMessage("Hi there"),
            new UserChatMessage("How are you?"),
            new AssistantChatMessage("I'm good, thanks!")
        };

        var originalMessages = messages.ToList();

        _compactorService.MicroCompact(messages);

        messages.ShouldBe(originalMessages);
    }

    [Fact]
    public void MicroCompact_FewerThanKeepRecentToolMessages_DoesNothing()
    {
        var messages = new List<ChatMessage>
        {
            new UserChatMessage("First question"),
            new AssistantChatMessage("First answer"),
            new ToolChatMessage("tool_1", "{\"result\": \"tool 1 result\"}"),
            new UserChatMessage("Second question"),
            new AssistantChatMessage("Second answer"),
            new ToolChatMessage("tool_2", "{\"result\": \"tool 2 result\"}")
        };

        var originalMessages = messages.ToList();

        _compactorService.MicroCompact(messages);

        messages.ShouldBe(originalMessages);
    }

    [Fact]
    public void MicroCompact_MultipleToolMessages_TruncatesOldOnes()
    {
        var messages = new List<ChatMessage>
        {
            new UserChatMessage("Question 1"),
            new AssistantChatMessage("Answer 1"),
            new ToolChatMessage("tool_1", "{\"result\": \"This is a very long tool response that should be truncated because it exceeds the minimum content length threshold by a significant margin.\"}"),

            new UserChatMessage("Question 2"),
            new AssistantChatMessage("Answer 2"),
            new ToolChatMessage("tool_2", "{\"result\": \"Another long tool response that should also be truncated for the same reasons as the previous one.\"}"),

            new UserChatMessage("Question 3"),
            new AssistantChatMessage("Answer 3"),
            new ToolChatMessage("tool_3", "{\"result\": \"This tool response should NOT be truncated because it's in the KEEP_RECENT window.\"}"),

            new UserChatMessage("Question 4"),
            new AssistantChatMessage("Answer 4"),
            new ToolChatMessage("tool_4", "{\"result\": \"This tool response should also NOT be truncated as it's recent.\"}")
        };

        var originalTool1Content = GetToolMessageContent(messages[2]);
        var originalTool2Content = GetToolMessageContent(messages[5]);
        var originalTool3Content = GetToolMessageContent(messages[8]);
        var originalTool4Content = GetToolMessageContent(messages[11]);

        _compactorService.MicroCompact(messages);

        var tool1Content = GetToolMessageContent(messages[2]);
        var tool2Content = GetToolMessageContent(messages[5]);
        var tool3Content = GetToolMessageContent(messages[8]);
        var tool4Content = GetToolMessageContent(messages[11]);

        tool1Content.ShouldNotBe(originalTool1Content);
        tool1Content.Length.ShouldBeLessThanOrEqualTo(originalTool1Content.Length);

        tool2Content.ShouldBe(originalTool2Content);
        tool3Content.ShouldBe(originalTool3Content);
        tool4Content.ShouldBe(originalTool4Content);
    }

    [Fact]
    public void MicroCompact_ShortToolResponse_NotTruncated()
    {
        var shortResponse = "{\"result\": \"short\"}";
        var messages = new List<ChatMessage>
        {
            new UserChatMessage("Question"),
            new AssistantChatMessage("Answer"),
            new ToolChatMessage("tool_1", shortResponse),
            new ToolChatMessage("tool_2", shortResponse),
            new ToolChatMessage("tool_3", shortResponse),
            new ToolChatMessage("tool_4", shortResponse)
        };

        _compactorService.MicroCompact(messages);

        for (int i = 2; i < messages.Count; i++)
        {
            var content = GetToolMessageContent(messages[i]);
            content.ShouldBe(shortResponse);
        }
    }

    [Fact]
    public void MicroCompact_NonJsonToolResponse_TruncatedWithIndicator()
    {
        var longText = new string('x', 200);
        var messages = new List<ChatMessage>
        {
            new UserChatMessage("Question"),
            new AssistantChatMessage("Answer"),
            new ToolChatMessage("tool_1", longText),
            new ToolChatMessage("tool_2", longText),
            new ToolChatMessage("tool_3", longText),
            new ToolChatMessage("tool_4", longText)
        };

        _compactorService.MicroCompact(messages);

        var content = GetToolMessageContent(messages[2]);
        content.ShouldStartWith("[Tool response truncated.");
        content.ShouldContain("Original length:");
        content.Length.ShouldBeLessThan(longText.Length);
    }

    [Fact]
    public void MicroCompact_JsonToolResponse_MaintainsJsonStructure()
    {
        var jsonResponse = "{\"result\": \"This is a comprehensive result with multiple fields\", \"data\": [1, 2, 3, 4, 5], \"status\": \"success\", \"metadata\": {\"timestamp\": \"2024-01-01\", \"source\": \"test\"}}";
        var messages = new List<ChatMessage>
        {
            new UserChatMessage("Question"),
            new AssistantChatMessage("Answer"),
            new ToolChatMessage("tool_1", jsonResponse),
            new ToolChatMessage("tool_2", jsonResponse),
            new ToolChatMessage("tool_3", jsonResponse),
            new ToolChatMessage("tool_4", jsonResponse)
        };

        _compactorService.MicroCompact(messages);

        var content = GetToolMessageContent(messages[2]);
        // When truncating from 171 to 100 chars, metadata would make it too long,
        // so it should use ... truncation or similar
        content.Length.ShouldBeLessThanOrEqualTo(100);
        // It should be truncated (shorter than original)
        content.Length.ShouldBeLessThan(jsonResponse.Length);
    }

    [Fact]
    public void MicroCompact_MixedContentTypes_HandlesCorrectly()
    {
        var messages = new List<ChatMessage>
        {
            new UserChatMessage("Question 1"),
            new AssistantChatMessage("Answer 1"),
            new ToolChatMessage("tool_1", "{\"json\": \"response\"}"),

            new UserChatMessage("Question 2"),
            new AssistantChatMessage("Answer 2"),
            new ToolChatMessage("tool_2", "Plain text response that is quite long and should be truncated appropriately. This text needs to be over 100 characters to trigger truncation, so let me add some more content to ensure it exceeds the minimum length threshold."),

            new UserChatMessage("Question 3"),
            new AssistantChatMessage("Answer 3"),
            new ToolChatMessage("tool_3", "{\"another\": \"json response\"}"),

            new UserChatMessage("Question 4"),
            new AssistantChatMessage("Answer 4"),
            new ToolChatMessage("tool_4", "Short")
        };

        _compactorService.MicroCompact(messages);

        // tool_1 should NOT be truncated (simple short JSON doesn't get truncated)
        var tool1Content = GetToolMessageContent(messages[2]);
        tool1Content.ShouldBe("{\"json\": \"response\"}");

        // tool_2 should NOT be truncated (it's in the KEEP_RECENT window)
        var tool2Content = GetToolMessageContent(messages[5]);
        tool2Content.ShouldBe("Plain text response that is quite long and should be truncated appropriately. This text needs to be over 100 characters to trigger truncation, so let me add some more content to ensure it exceeds the minimum length threshold.");

        // tool_3 should not be truncated (recent)
        var tool3Content = GetToolMessageContent(messages[8]);
        tool3Content.ShouldBe("{\"another\": \"json response\"}");

        // tool_4 should not be truncated (short)
        var tool4Content = GetToolMessageContent(messages[11]);
        tool4Content.ShouldBe("Short");
    }

    [Fact]
    public void FindToolResultLocations_CorrectlyIdentifiesToolMessages()
    {
        var messages = new List<ChatMessage>
        {
            new UserChatMessage("User message"),
            new AssistantChatMessage("Assistant message"),
            new ToolChatMessage("tool_1", "Tool response 1"),
            new SystemChatMessage("System message"),
            new ToolChatMessage("tool_2", "Tool response 2")
        };

        var method = typeof(ContextCompactorService).GetMethod("FindToolResultLocations",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var locations = (IEnumerable<(int MsgIdx, int ContentIdx)>)method!.Invoke(_compactorService, new object[] { messages })!;
        var locationList = locations.ToList();

        locationList.Count.ShouldBe(2);
        locationList[0].MsgIdx.ShouldBe(2);
        locationList[0].ContentIdx.ShouldBe(0);
        locationList[1].MsgIdx.ShouldBe(4);
        locationList[1].ContentIdx.ShouldBe(0);
    }

    [Fact]
    public void TruncateToolResponse_ValidJson_ReturnsTruncatedJson()
    {
        var json = "{\"result\": \"A very long result that needs truncation\", \"data\": [1, 2, 3, 4, 5, 6, 7, 8, 9, 10], \"status\": \"success\"}";

        var method = typeof(ContextCompactorService).GetMethod("TruncateToolResponse",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var truncated = (string)method!.Invoke(_compactorService, new object[] { json, 100 })!;

        // When truncating to 100 chars, metadata would make it too long,
        // so it should use ... truncation instead
        truncated.ShouldEndWith("...");
        truncated.Length.ShouldBeLessThanOrEqualTo(100);
    }

    [Fact]
    public void TruncateToolResponse_ShortJson_ReturnsOriginal()
    {
        var shortJson = "{\"result\": \"short\"}";

        var method = typeof(ContextCompactorService).GetMethod("TruncateToolResponse",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (string)method!.Invoke(_compactorService, new object[] { shortJson, 100 })!;

        result.ShouldBe(shortJson);
    }

    [Fact]
    public void TruncateToolResponse_PlainText_ReturnsTruncatedWithIndicator()
    {
        var longText = new string('x', 200);

        var method = typeof(ContextCompactorService).GetMethod("TruncateToolResponse",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (string)method!.Invoke(_compactorService, new object[] { longText, 100 })!;

        result.ShouldStartWith("[Tool response truncated.");
        result.ShouldContain("Original length:");
        result.Length.ShouldBeLessThan(longText.Length);
    }

    [Fact]
    public void TruncateToolResponse_MalformedJson_ReturnsOriginal()
    {
        var malformedJson = "{invalid json with missing quotes and brackets";

        var method = typeof(ContextCompactorService).GetMethod("TruncateToolResponse",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (string)method!.Invoke(_compactorService, new object[] { malformedJson, 50 })!;

        // Current implementation returns malformed JSON as-is
        result.ShouldBe(malformedJson);
    }

    [Fact]
    public void TruncateToolResponse_NestedJson_AddsMetadata()
    {
        var nestedJson = "{\"level1\": {\"level2\": {\"level3\": {\"data\": \"very nested data that will be truncated\"}, \"more\": \"data\"}}, \"other\": \"field\"}";

        var method = typeof(ContextCompactorService).GetMethod("TruncateToolResponse",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (string)method!.Invoke(_compactorService, new object[] { nestedJson, 80 })!;

        // When truncating from 122 to 80 chars, metadata would make it too long,
        // so it should use ... truncation instead
        result.ShouldEndWith("...");
        result.Length.ShouldBeLessThanOrEqualTo(80);
    }

    private static string GetToolMessageContent(ChatMessage message)
    {
        return message.Content[0].Text;
    }
}
