using System.ClientModel;
using OpenAI;
using OpenAI.Chat;

namespace Loken.Core;

public class LiteLlmChatClient : IChatClient
{
    private ChatClient _chatClient;

    public LiteLlmChatClient()
    {
        _chatClient = new(
            model: "chat",
            credential: new ApiKeyCredential(Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? ""),
            options: new OpenAIClientOptions()
            {
                Endpoint = new Uri("http://192.168.68.117:4000")
            });
    }

    public async Task<ClientResult<ChatCompletion>> CompleteChatAsync(IEnumerable<ChatMessage> messages, ChatCompletionOptions options)
    {
        return await _chatClient.CompleteChatAsync(messages, options);
    }
}
