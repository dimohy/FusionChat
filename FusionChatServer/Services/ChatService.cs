using FusionChat.Service;

using Stl.Async;
using Stl.Fusion;

using System.Reactive;

namespace FusionChatServer.Services
{
    public class ChatService : IChatService
    {
        private readonly List<ChatMessage> _chatInfos = new();
        private readonly object _chatInfosLock = new object();
 
        public ChatService()
        {
        }

        [ComputeMethod]
        public virtual async Task<ChatInfo> GetChatMessages(int index, CancellationToken cancellationToken = default)
        {
            await EveryChatTail();

            ChatInfo chatInfo;
            lock (_chatInfosLock)
            {
                chatInfo = new ChatInfo(_chatInfos.Count, _chatInfos.Skip(index).ToArray());
            }
                
            return chatInfo;
        }

        [ComputeMethod]
        protected virtual Task<Unit> EveryChatTail() => TaskExt.UnitTask;

        public Task SendMessage(ChatMessage message, CancellationToken cancellationToken = default)
        {
            lock (_chatInfosLock)
            {
                _chatInfos.Add(message);
            }

            using (Computed.Invalidate())
            {
                _ = EveryChatTail();
            }

            return Task.CompletedTask;
        }
    }
}
