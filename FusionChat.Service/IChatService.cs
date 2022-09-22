using Stl.Fusion;

namespace FusionChat.Service;

public interface IChatService
{
    [ComputeMethod]
    Task<ChatInfo> GetChatMessages(int index, CancellationToken cancellationToken = default);
    Task SendMessage(ChatMessage message, CancellationToken cancellationToken = default);
}

public record ChatInfo(int TotalMessages, IEnumerable<ChatMessage> Messages);
public record ChatMessage(string Nickname, string Message);