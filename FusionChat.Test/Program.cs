using FusionChat;
using FusionChat.Service;

using Stl.Fusion;
using Stl.Fusion.UI;

using System.Threading.Tasks.Dataflow;

var client = new FusionChatClient(new Uri("https://localhost:7233/"));
using var messageState = client.StateFactory.NewComputed<ChatInfo>(new ComputedState<ChatInfo>.Options()
    {
    }, async (state, CancellationToken) =>
    {
        var result = await client.ChatService.GetChatMessages(0);
        PrintMessages(result);

        return result;
    });


//PrintMessages(messages);

await client.ChatService.SendMessage(new ChatMessage("dimohy", "test1"));
await Task.Delay(500);
await client.ChatService.SendMessage(new ChatMessage("dimohy", "test1"));
await Task.Delay(500);
await client.ChatService.SendMessage(new ChatMessage("dimohy", "test1"));


//PrintMessages(messages);

Console.ReadLine();


static void PrintMessages(ChatInfo info)
{
    Console.WriteLine($"Total Messages: {info.TotalMessages}");

    foreach (var message in info.Messages)
    {
        Console.WriteLine(message);
    }
}