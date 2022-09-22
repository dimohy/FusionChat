using FusionChat.Service;

using FusionChatServer.Services;

using Stl.Fusion;
using Stl.Fusion.Bridge;
using Stl.Fusion.Extensions;
using Stl.Fusion.Server;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 퓨전 서비스 등록
var fusion = builder.Services.AddFusion();
fusion.AddWebServer();

fusion.AddComputeService<IChatService, ChatService>();

builder.Services.AddSingleton(new PublisherOptions() { Id = "p" });
//builder.Services.AddTransient<IUpdateDelayer>(_ => UpdateDelayer.Instant);
builder.Services.AddTransient<IUpdateDelayer>(_ => FixedDelayer.Instant);


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseWebSockets(new WebSocketOptions()
{
    KeepAliveInterval = TimeSpan.FromSeconds(30), // You can change this
});
app.UseHttpsRedirection();
//app.UseAuthorization();
app.MapControllers();
app.MapFusionWebSocketServer();

app.Run();
