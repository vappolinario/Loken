using OpenAI.Chat;

namespace Loken.Core
{
    public interface IContextCompactorService
    {
        void MicroCompact(IList<ChatMessage> messages);
    }
}
