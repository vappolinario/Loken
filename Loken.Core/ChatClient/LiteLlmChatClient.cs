using System.ClientModel;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

namespace Loken.Core;

public class LiteLlmChatClient : IChatClient
{
    private ChatClient _chatClient;

    public LiteLlmChatClient(IOptions<AiOptions> options)
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? string.Empty;

        _chatClient = new(
            model: "chat",
            credential: new ApiKeyCredential(apiKey),
            options: new OpenAIClientOptions()
            {
                Endpoint = new Uri(options.Value.OpenAiUri)
            });
    }

    public async Task<ClientResult<ChatCompletion>> CompleteChatAsync(IEnumerable<ChatMessage> messages, ChatCompletionOptions options)
    {
        return await _chatClient.CompleteChatAsync(messages, options);
    }
}
