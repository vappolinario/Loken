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
    private readonly TodoManager _todoManager;
    private readonly Agent _agent;

    public AgentTest()
    {
        _chat = Substitute.For<IChatClient>();
        _reporter = Substitute.For<IAgentReporter>();

        _bashHandler = Substitute.For<IToolHandler>();
        _bashHandler.Name.Returns("bash");

        _echoHandler = Substitute.For<IToolHandler>();
        _echoHandler.Name.Returns("echo");

        _todoManager = Substitute.For<TodoManager>();

        var handlers = new List<IToolHandler> { _bashHandler, _echoHandler };

        _agent = new Agent(handlers, _chat, _reporter, _todoManager);
    }

    [Fact]
    public void Version_AgentMustReturnVersionNumber()
    {
        _agent.Version().ShouldBe("0.1");
    }

    [Fact]
    public async Task ChatWithoutTool_AgentMustReturnMessageWithoutCallingTool()
    {
        var mockCompletion = CreateMockCompletion(["Test response"], ChatFinishReason.Stop);
        _chat.CompleteChatAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatCompletionOptions>())
            .Returns(Task.FromResult(mockCompletion));

        var result = await _agent.Run("Test prompt");

        result.ShouldBe("Test response");
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
