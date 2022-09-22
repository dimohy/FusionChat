using FusionChat.Service;

using RestEase;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FusionChat;


[BasePath("chat")]
public interface IChatServiceDef
{
    [Get(nameof(GetChatMessages))]
    Task<ChatInfo> GetChatMessages(int index, CancellationToken cancellationToken = default);
    [Post(nameof(SendMessage))]
    Task SendMessage([Body] ChatMessage message, CancellationToken cancellationToken = default);
}
