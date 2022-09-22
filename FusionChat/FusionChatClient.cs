using FusionChat.Service;

using Microsoft.Extensions.DependencyInjection;
using Stl.CommandR;
using Stl.Fusion;
using Stl.Fusion.Client;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FusionChat;

public class FusionChatClient
{
    private readonly IServiceProvider _service;

    public IChatService ChatService => _service.GetRequiredService<IChatService>();

    public IStateFactory StateFactory => _service.StateFactory();


    public FusionChatClient(Uri baseUri)
    {
        var services = new ServiceCollection();
        var apiBaseUri = baseUri;

        var fusion = services.AddFusion();
        var fusionClient = fusion
            //.AddBackendStatus()
            .AddRestEaseClient();

        fusion
            //.AddAuthentication()
            .AddRestEaseClient();

        fusionClient.ConfigureHttpClient((c, name, options) =>
        {
            options.HttpClientActions.Add(client => client.BaseAddress = apiBaseUri);
        });
        fusionClient.ConfigureWebSocketChannel(c => new()
        {
            BaseUri = baseUri
        });

        fusionClient
            .AddReplicaService<IChatService, IChatServiceDef>();

        _service = services.BuildServiceProvider();
    }
}
