using AIAdventChallenge.McpServer.Tools;
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);

// Регистрируем MCP сервер
builder.Services.AddMcpServer()
    .WithHttpTransport()       // HTTP транспорт
    .WithTools<HelloTool>();

var app = builder.Build();

// Маппим MCP эндпоинт
app.MapMcp();

Console.WriteLine("🚀 MCP-сервер запущен");
Console.WriteLine("   Ожидаем подключения клиента...\n");

app.Run();