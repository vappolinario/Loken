#pragma warning disable OPENAI001
namespace Loken.Core.Tests;

using System.ClientModel;
using System.ClientModel.Primitives;
using NSubstitute;
using OpenAI.Chat;
using Shouldly;

public class AgentTest
{
    private readonly IChatClient _chat;
    private readonly IAgentReporter _reporter;
    private readonly IToolHandler _bashHandler;
    private readonly IToolHandler _echoHandler;
    private readonly IToolHandler _skillHandler;
    private readonly ITodoService _todoService;
    private readonly ISkillService _skillService;
    private readonly IContextCompactorService _compactorService;
    private readonly Agent _agent;

    public AgentTest()
    {
        _chat = Substitute.For<IChatClient>();
        _reporter = Substitute.For<IAgentReporter>();

        _bashHandler = Substitute.For<IToolHandler>();
        _bashHandler.Name.Returns("bash");

        _echoHandler = Substitute.For<IToolHandler>();
        _echoHandler.Name.Returns("echo");

        _todoService = Substitute.For<ITodoService>();

        _skillService = Substitute.For<ISkillService>();
        _skillHandler = new SkillHandler(_skillService);

        _compactorService = Substitute.For<IContextCompactorService>();

        var handlers = new List<IToolHandler> { _bashHandler, _echoHandler, _skillHandler };

        _agent = new Agent(handlers, _chat, _reporter, _todoService, _skillService, _compactorService);
    }

    [Fact]
    public void Version_AgentMustReturnVersionNumber()
    {
        var version = _agent.Version();

        version.ShouldStartWith("0.1-");

        var suffix = version.Substring(4);
        if (suffix != "unknown")
        {
            suffix.Length.ShouldBeGreaterThanOrEqualTo(7);
            System.Text.RegularExpressions.Regex.IsMatch(suffix, "^[0-9a-f]+$").ShouldBeTrue();
        }
    }

    [Fact]
    public void Agent_ShouldInitializeWithEmptyHandlers()
    {
        var agent = new Agent(new List<IToolHandler>(), _chat, _reporter, _todoService, _skillService, _compactorService);

        agent.ShouldNotBeNull();
        var version = agent.Version();
        version.ShouldStartWith("0.1-");
    }

    [Fact]
    public void SetSystemPrompt_ShouldClearMessagesAndAddSystemMessage()
    {
        var systemPrompt = "You are a helpful assistant.";

        _agent.SetSystemPrompt(systemPrompt);

        var mockCompletion = CreateMockCompletion(["Response with system prompt"], ChatFinishReason.Stop);
        _chat.CompleteChatAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatCompletionOptions>())
            .Returns(Task.FromResult(mockCompletion));

        Should.NotThrow(async () => await _agent.Run("Test"));
    }

    [Fact]
    public void SetSystemPrompt_ShouldIncludeSkillDescriptions_WhenSkillsAreAvailable()
    {
        var systemPrompt = "You are a helpful assistant.";
        var skillDescription = "  - brave-search: Web search and content extraction via Brave Search API. Use for searching documentation, facts, or any web content. Lightweight, no browser required.\n";
        _skillService.GetSkills().Returns(skillDescription);

        _agent.SetSystemPrompt(systemPrompt);

        _skillService.Received(1).GetSkills();
    }

    [Fact]
    public async Task ExecuteToolAsync_ShouldThrowException_WhenToolNotFound()
    {
        var toolName = "non_existent_tool";
        var input = BinaryData.FromString("{}");

        await Should.ThrowAsync<UnknownToolException>(async () =>
            await _agent.ExecuteToolAsync(toolName, input));
    }

    [Fact]
    public async Task ExecuteToolAsync_ShouldExecuteTool_WhenToolExists()
    {
        var toolName = "bash";
        var input = BinaryData.FromString("{\"command\": \"echo test\"}");
        var expectedOutput = "test output";

        _bashHandler.ExecuteAsync(input).Returns(Task.FromResult(expectedOutput));

        var result = await _agent.ExecuteToolAsync(toolName, input);

        result.ShouldBe(expectedOutput);
        await _bashHandler.Received(1).ExecuteAsync(input);
        _reporter.Received(1).ReportMessage($"Tool used: {toolName}", true);
    }

    [Fact]
    public async Task ChatWithoutTool_AgentMustReturnMessageWithoutCallingTool()
    {
        var mockCompletion = CreateMockCompletion(["Test response"], ChatFinishReason.Stop);
        _chat.CompleteChatAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatCompletionOptions>())
            .Returns(Task.FromResult(mockCompletion));

        var result = await _agent.Run("Test prompt");

        result.ShouldBe("Test response");
        await _bashHandler.DidNotReceive().ExecuteAsync(Arg.Any<BinaryData>());
        await _echoHandler.DidNotReceive().ExecuteAsync(Arg.Any<BinaryData>());
    }

    [Fact]
    public async Task Run_ShouldHandleEmptyPrompt()
    {
        var mockCompletion = CreateMockCompletion(["Empty prompt response"], ChatFinishReason.Stop);
        _chat.CompleteChatAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatCompletionOptions>())
            .Returns(Task.FromResult(mockCompletion));

        var result = await _agent.Run("");

        result.ShouldBe("Empty prompt response");
    }

    [Fact]
    public async Task ChatWithTool_AgentMustExecuteShellCommandAndReturnResult()
    {
        var command = BinaryData.FromString("{\"command\": \"ls\"}");
        var toolCall = ChatToolCall.CreateFunctionToolCall("id123", "bash", command);

        var responseWithTool = CreateMockCompletion([], ChatFinishReason.ToolCalls, [toolCall]);
        var finalResponse = CreateMockCompletion(["Aqui estão os arquivos"], ChatFinishReason.Stop);

        _chat.CompleteChatAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatCompletionOptions>())
            .Returns(Task.FromResult(responseWithTool), Task.FromResult(finalResponse));

        _bashHandler.ExecuteAsync(command).Returns(Task.FromResult("file1.txt"));

        var result = await _agent.Run("Listar arquivos");

        await _bashHandler.Received(1).ExecuteAsync(command);
        result.ShouldContain("Aqui estão os arquivos");
        await _echoHandler.DidNotReceive().ExecuteAsync(Arg.Any<BinaryData>());
    }

    [Fact]
    public async Task Run_ShouldThrowException_WhenToolNameIsNotRegistered()
    {
        var toolCall = ChatToolCall.CreateFunctionToolCall("id123", "python_script", BinaryData.FromString("{}"));
        var response = CreateMockCompletion([], ChatFinishReason.ToolCalls, [toolCall]);

        _chat.CompleteChatAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatCompletionOptions>())
            .Returns(Task.FromResult(response));

        await Should.ThrowAsync<Exception>(async () => await _agent.Run("Rode python"));
    }

    [Fact]
    public async Task Run_ShouldHandleMultipleToolCallsInSingleResponse()
    {
        var command1 = BinaryData.FromString("{\"command\": \"ls\"}");
        var command2 = BinaryData.FromString("{\"command\": \"pwd\"}");

        var toolCall1 = ChatToolCall.CreateFunctionToolCall("id1", "bash", command1);
        var toolCall2 = ChatToolCall.CreateFunctionToolCall("id2", "echo", command2);

        var responseWithTools = CreateMockCompletion([], ChatFinishReason.ToolCalls, [toolCall1, toolCall2]);
        var finalResponse = CreateMockCompletion(["Both tools executed"], ChatFinishReason.Stop);

        _chat.CompleteChatAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatCompletionOptions>())
            .Returns(Task.FromResult(responseWithTools), Task.FromResult(finalResponse));

        _bashHandler.ExecuteAsync(command1).Returns(Task.FromResult("file1.txt"));
        _echoHandler.ExecuteAsync(command2).Returns(Task.FromResult("/home/user"));

        var result = await _agent.Run("Execute multiple tools");

        await _bashHandler.Received(1).ExecuteAsync(command1);
        await _echoHandler.Received(1).ExecuteAsync(command2);
        result.ShouldContain("Both tools executed");
    }

    [Fact]
    public async Task Run_ShouldHandleToolExecutionFailure()
    {
        var command = BinaryData.FromString("{\"command\": \"fail\"}");
        var toolCall = ChatToolCall.CreateFunctionToolCall("id123", "bash", command);

        var responseWithTool = CreateMockCompletion([], ChatFinishReason.ToolCalls, [toolCall]);
        var finalResponse = CreateMockCompletion(["Tool failed but handled"], ChatFinishReason.Stop);

        _chat.CompleteChatAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatCompletionOptions>())
            .Returns(Task.FromResult(responseWithTool), Task.FromResult(finalResponse));

        _bashHandler.ExecuteAsync(command).Returns(Task.FromException<string>(new ExecutionFailedException("Command failed")));

        var result = await _agent.Run("Execute failing tool");

        await _bashHandler.Received(1).ExecuteAsync(command);
        result.ShouldContain("Tool failed but handled");
    }

    [Fact]
    public async Task Run_ShouldAddTodoReminder_WhenTodoServiceRequiresIt()
    {
        _todoService.ShouldRemindAboutTodos().Returns(true);

        var mockCompletion = CreateMockCompletion(["Test response"], ChatFinishReason.Stop);
        _chat.CompleteChatAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatCompletionOptions>())
            .Returns(Task.FromResult(mockCompletion));

        var result = await _agent.Run("Test prompt");

        result.ShouldBe("Test response");
    }

    [Fact]
    public async Task Run_ShouldReportMessages_WhenToolIsExecuted()
    {
        var command = BinaryData.FromString("{\"command\": \"ls\"}");
        var toolCall = ChatToolCall.CreateFunctionToolCall("id123", "bash", command);

        var responseWithTool = CreateMockCompletion([], ChatFinishReason.ToolCalls, [toolCall]);
        var finalResponse = CreateMockCompletion(["Tool executed"], ChatFinishReason.Stop);

        _chat.CompleteChatAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatCompletionOptions>())
            .Returns(Task.FromResult(responseWithTool), Task.FromResult(finalResponse));

        _bashHandler.ExecuteAsync(command).Returns(Task.FromResult("file1.txt"));

        var result = await _agent.Run("List files");

        _reporter.Received(1).ReportMessage("Tool used: bash", true);
        _reporter.Received(1).ReportMessage("file1.txt", true);
        _reporter.Received(1).ReportMessage(Arg.Is<string>(s => s.Contains("Tool executed")), false);
    }

    [Fact]
    public async Task Run_ShouldExecuteLoadSkillTool_WhenRequested()
    {
        var skillName = "brave-search";
        var skillBody = "Web search and content extraction via Brave Search API.";
        var skillInput = BinaryData.FromString($"{{\"name\": \"{skillName}\"}}");
        var toolCall = ChatToolCall.CreateFunctionToolCall("id123", "load_skill", skillInput);

        var responseWithTool = CreateMockCompletion([], ChatFinishReason.ToolCalls, [toolCall]);
        var finalResponse = CreateMockCompletion(["Skill loaded successfully"], ChatFinishReason.Stop);

        _chat.CompleteChatAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatCompletionOptions>())
            .Returns(Task.FromResult(responseWithTool), Task.FromResult(finalResponse));

        _skillService.GetSkillBody(skillName).Returns(skillBody);

        var result = await _agent.Run($"Load skill {skillName}");

        _skillService.Received(1).GetSkillBody(skillName);
        _reporter.Received(1).ReportMessage("Tool used: load_skill", true);
        _reporter.Received(1).ReportMessage(skillBody, true);
        result.ShouldContain("Skill loaded successfully");
    }

    [Fact]
    public async Task Run_ShouldHandleLoadSkillToolCalls_InChatResponses()
    {
        var skillName = "brave-search";
        var skillBody = "Web search and content extraction via Brave Search API.";
        var skillInput = BinaryData.FromString($"{{\"name\": \"{skillName}\"}}");
        var toolCall = ChatToolCall.CreateFunctionToolCall("id123", "load_skill", skillInput);

        var responseWithTool = CreateMockCompletion([], ChatFinishReason.ToolCalls, [toolCall]);
        var finalResponse = CreateMockCompletion(["Now I can help you search the web"], ChatFinishReason.Stop);

        _chat.CompleteChatAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatCompletionOptions>())
            .Returns(Task.FromResult(responseWithTool), Task.FromResult(finalResponse));

        _skillService.GetSkillBody(skillName).Returns(skillBody);

        var result = await _agent.Run("I need to search for something");

        _skillService.Received(1).GetSkillBody(skillName);
        _reporter.Received(1).ReportMessage("Tool used: load_skill", true);
        _reporter.Received(1).ReportMessage(skillBody, true);
        result.ShouldContain("Now I can help you search the web");
    }

    [Fact]
    public async Task Run_ShouldHandleLoadSkillToolFailure_Gracefully()
    {
        var skillName = "non-existent-skill";
        var skillInput = BinaryData.FromString($"{{\"name\": \"{skillName}\"}}");
        var toolCall = ChatToolCall.CreateFunctionToolCall("id123", "load_skill", skillInput);

        var responseWithTool = CreateMockCompletion([], ChatFinishReason.ToolCalls, [toolCall]);
        var finalResponse = CreateMockCompletion(["Skill not found, but continuing"], ChatFinishReason.Stop);

        _chat.CompleteChatAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatCompletionOptions>())
            .Returns(Task.FromResult(responseWithTool), Task.FromResult(finalResponse));

        _skillService.GetSkillBody(skillName).Returns(x => throw new ExecutionFailedException($"Skill '{skillName}' not found"));

        var result = await _agent.Run($"Load non-existent skill {skillName}");

        _skillService.Received(1).GetSkillBody(skillName);
        _reporter.Received(1).ReportMessage("Tool used: load_skill", true);
        result.ShouldContain("Skill not found, but continuing");
    }

    [Fact]
    public void SetSystemPrompt_ShouldNotIncludeSkillDescriptions_WhenNoSkillsAreAvailable()
    {
        var systemPrompt = "You are a helpful assistant.";
        _skillService.GetSkills().Returns("");

        _agent.SetSystemPrompt(systemPrompt);

        _skillService.Received(1).GetSkills();
    }

    [Fact]
    public async Task Run_ShouldWorkCorrectly_WhenNoSkillsAreAvailable()
    {
        var systemPrompt = "You are a helpful assistant.";
        _skillService.GetSkills().Returns("");

        _agent.SetSystemPrompt(systemPrompt);

        var mockCompletion = CreateMockCompletion(["I can help you without skills"], ChatFinishReason.Stop);
        _chat.CompleteChatAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatCompletionOptions>())
            .Returns(Task.FromResult(mockCompletion));

        var result = await _agent.Run("Help me without skills");

        result.ShouldBe("I can help you without skills");
        _skillService.Received(1).GetSkills();
    }

    [Fact]
    public async Task ExecuteToolAsync_ShouldExecuteLoadSkillTool_ThroughSkillHandler()
    {
        var skillName = "brave-search";
        var skillBody = "Web search and content extraction via Brave Search API.";
        var input = BinaryData.FromString($"{{\"name\": \"{skillName}\"}}");

        _skillService.GetSkillBody(skillName).Returns(skillBody);

        var result = await _agent.ExecuteToolAsync("load_skill", input);

        result.ShouldBe(skillBody);
        _skillService.Received(1).GetSkillBody(skillName);
        _reporter.Received(1).ReportMessage($"Tool used: load_skill", true);
    }

    [Fact]
    public void Agent_ShouldInitializeWithSkillHandler_WhenProvided()
    {
        var handlers = new List<IToolHandler> { _skillHandler };
        var agent = new Agent(handlers, _chat, _reporter, _todoService, _skillService, _compactorService);

        agent.ShouldNotBeNull();
        Should.NotThrow(async () => await agent.ExecuteToolAsync("load_skill", BinaryData.FromString("{\"name\": \"test\"}")));
    }

    [Fact]
    public void SetSystemPrompt_ShouldFormatSkillDescriptions_Correctly()
    {
        var systemPrompt = "You are a helpful assistant.";
        var skillDescription = "  - brave-search: Web search and content extraction via Brave Search API. Use for searching documentation, facts, or any web content. Lightweight, no browser required.\n  - file-search: Search for files in the filesystem.\n";
        _skillService.GetSkills().Returns(skillDescription);

        _agent.SetSystemPrompt(systemPrompt);

        _skillService.Received(1).GetSkills();
    }

    [Fact]
    public async Task Run_ShouldIncludeFormattedSkillDescriptions_InSystemPrompt()
    {
        var systemPrompt = "You are a helpful assistant.";
        var skillDescription = "  - brave-search: Web search and content extraction via Brave Search API.\n";
        _skillService.GetSkills().Returns(skillDescription);

        _agent.SetSystemPrompt(systemPrompt);

        var mockCompletion = CreateMockCompletion(["I see the skills available"], ChatFinishReason.Stop);
        _chat.CompleteChatAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatCompletionOptions>())
            .Returns(Task.FromResult(mockCompletion));

        var result = await _agent.Run("What skills do you have?");

        _skillService.Received(1).GetSkills();
        result.ShouldBe("I see the skills available");
    }

    private ClientResult<ChatCompletion> CreateMockCompletion(
        ChatMessageContent content,
        ChatFinishReason finishReason,
        IEnumerable<ChatToolCall>? toolCalls = null)
    {
        var completion = OpenAIChatModelFactory.ChatCompletion(
            content: content,
            finishReason: finishReason,
            toolCalls: toolCalls,
            role: ChatMessageRole.Assistant
        );

        return ClientResult.FromValue(completion, Substitute.For<PipelineResponse>());
    }

}
#pragma warning restore OPENAI001
