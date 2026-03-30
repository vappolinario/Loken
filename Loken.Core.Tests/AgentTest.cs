#pragma warning disable OPENAI001
namespace Loken.Core.Tests;

using System.ClientModel;
using System.ClientModel.Primitives;
using NSubstitute;
using OpenAI.Chat;
using Shouldly;

public class AgentTest
{
    [Fact]
    public void Version_AgentMustReturnVersionNumber()
    {
        var chat = Substitute.For<IChatClient>();
        var reporter = Substitute.For<IAgentReporter>();
        var executor = Substitute.For<IToolHandler>();
        var handlers = new List<IToolHandler> { executor };
        var agent = new Agent(handlers, chat, reporter);
        var result = agent.Version();
        result.ShouldBe("0.1");
    }

    [Fact]
    public async Task ChatWithoutTool_AgentMustReturnMessageWithoutCallingTool()
    {
        var chat = Substitute.For<IChatClient>();

        var mockCompletion = OpenAIChatModelFactory.ChatCompletion(
            content: ["Test response"],
            finishReason: ChatFinishReason.Stop,
           role: ChatMessageRole.Assistant
        );

        var mockResult = ClientResult.FromValue(mockCompletion, Substitute.For<PipelineResponse>());

        chat.CompleteChatAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatCompletionOptions>())
            .Returns(Task.FromResult(mockResult));

        var executor = Substitute.For<IToolHandler>();
        executor.Name.Returns("bash");
        var handlers = new List<IToolHandler> { executor };
        var reporter = Substitute.For<IAgentReporter>();
        var agent = new Agent(handlers, chat, reporter);
        var result = await agent.Run("Test prompt");
        result.ShouldBe("Test response");
    }

    [Fact]
    public async Task ChatWithTool_AgentMustExecuteShellCommandAndReturnResult()
    {
        var chat = Substitute.For<IChatClient>();
        var executor = Substitute.For<IToolHandler>();
        executor.Name.Returns("bash");
        var handlers = new List<IToolHandler> { executor };
        var reporter = Substitute.For<IAgentReporter>();

        var command = BinaryData.FromString("{\"command\": \"ls\"}");
        var toolCall = ChatToolCall.CreateFunctionToolCall("id123", "bash", command);

        var mockToolResponse = OpenAIChatModelFactory.ChatCompletion(
            content: [],
            finishReason: ChatFinishReason.ToolCalls,
            toolCalls: [toolCall],
           role: ChatMessageRole.Assistant
        );

        var mockFinalResponse = OpenAIChatModelFactory.ChatCompletion(
            content: ["Aqui estão os arquivos: file1.txt\nfile2.txt"],
            finishReason: ChatFinishReason.Stop,
           role: ChatMessageRole.Assistant
        );

        var resultTool = ClientResult.FromValue(mockToolResponse, Substitute.For<PipelineResponse>());
        var resultFinal = ClientResult.FromValue(mockFinalResponse, Substitute.For<PipelineResponse>());

        chat.CompleteChatAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatCompletionOptions>())
            .Returns(Task.FromResult(resultTool), Task.FromResult(resultFinal));

        executor.ExecuteAsync("ls").Returns(Task.FromResult("file1.txt\nfile2.txt"));

        var agent = new Agent(handlers, chat, reporter);
        var result = await agent.Run("Listar arquivos");

        await executor.Received(1).ExecuteAsync("ls");
        await chat.Received().CompleteChatAsync(Arg.Is<IEnumerable<ChatMessage>>(msgs =>
            msgs.Any(m =>
                m is ToolChatMessage &&
                ((ToolChatMessage)m).Content.Any(c => c.Text != null && c.Text.Contains("file1.txt"))
            )
        ), Arg.Any<ChatCompletionOptions>());
        result.ShouldBe("Aqui estão os arquivos: file1.txt\nfile2.txt");
    }

    [Fact]
    public async Task Run_ShouldThrowException_WhenToolNameIsNotRegistered()
    {
        var chat = Substitute.For<IChatClient>();
        var executor = Substitute.For<IToolHandler>();
        executor.Name.Returns("bash");

        var toolCall = ChatToolCall.CreateFunctionToolCall("id123", "python_script", BinaryData.FromString("{}"));

        var mockToolResponse = OpenAIChatModelFactory.ChatCompletion(
            finishReason: ChatFinishReason.ToolCalls,
            toolCalls: [toolCall],
            role: ChatMessageRole.Assistant
        );

        chat.CompleteChatAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatCompletionOptions>())
            .Returns(Task.FromResult(ClientResult.FromValue(mockToolResponse, Substitute.For<PipelineResponse>())));

        var reporter = Substitute.For<IAgentReporter>();
        var handlers = new List<IToolHandler> { executor };
        var agent = new Agent(handlers, chat, reporter);

        await Should.ThrowAsync<Exception>(async () => await agent.Run("Rode um script python"));
        await executor.DidNotReceive().ExecuteAsync(Arg.Any<string>());
    }
    [Fact]
    public async Task Run_WithMultipleHandlers_ShouldInvokeCorrectHandlerByName()
    {
        var chatClient = Substitute.For<IChatClient>();

        var bashHandler = Substitute.For<IToolHandler>();
        bashHandler.Name.Returns("bash");

        var echoHandler = Substitute.For<IToolHandler>();
        echoHandler.Name.Returns("echo");

        var reporter = Substitute.For<IAgentReporter>();

        var toolCall = ChatToolCall.CreateFunctionToolCall("id123", "bash", BinaryData.FromString("{\"command\": \"ls\"}"));

        var mockToolResponse = OpenAIChatModelFactory.ChatCompletion(
            finishReason: ChatFinishReason.ToolCalls,
            toolCalls: [toolCall],
            role: ChatMessageRole.Assistant
        );
        var mockFinalResponse = OpenAIChatModelFactory.ChatCompletion(
            content: ["Sucesso"],
            finishReason: ChatFinishReason.Stop,
            role: ChatMessageRole.Assistant
        );

        chatClient.CompleteChatAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatCompletionOptions>())
            .Returns(
                Task.FromResult(ClientResult.FromValue(mockToolResponse, Substitute.For<PipelineResponse>())),
                Task.FromResult(ClientResult.FromValue(mockFinalResponse, Substitute.For<PipelineResponse>()))
            );

        var handlers = new List<IToolHandler> { bashHandler, echoHandler };
        var agent = new Agent(handlers, chatClient, reporter);

        await agent.Run("Listar arquivos");
        await bashHandler.Received(1).ExecuteAsync(Arg.Is<string>(s => s.Contains("ls")));
        await echoHandler.DidNotReceive().ExecuteAsync(Arg.Any<string>());
    }
}
#pragma warning restore OPENAI001
