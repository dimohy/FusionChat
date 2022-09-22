using FusionChat.Service;

using Microsoft.AspNetCore.Mvc;

using Stl.Fusion.Server;

namespace FusionChatServer.Controllers;

[Route("[controller]/[action]")]
[ApiController, JsonifyErrors, UseDefaultSession]
public class ChatController : ControllerBase, IChatService
{
    private IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpGet, Publish]
    public Task<ChatInfo> GetChatMessages(int index, CancellationToken cancellationToken = default) => _chatService.GetChatMessages(index, cancellationToken);
    [HttpPost]
    public Task SendMessage([FromBody] ChatMessage message, CancellationToken cancellationToken = default) => _chatService.SendMessage(message, cancellationToken);
}
