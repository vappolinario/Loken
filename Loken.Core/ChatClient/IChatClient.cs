using System.ClientModel;
using OpenAI.Chat;

namespace Loken.Core;

public interface IChatClient
{
    Task<ClientResult<ChatCompletion>> CompleteChatAsync(IEnumerable<ChatMessage> messages, ChatCompletionOptions options);
}
