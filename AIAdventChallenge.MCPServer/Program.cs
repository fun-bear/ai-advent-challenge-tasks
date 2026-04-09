using AIAdventChallenge.McpServer.Infrastructure.Storage;
using AIAdventChallenge.McpServer.BackgroundServices;
using AIAdventChallenge.McpServer.Tools;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);

var sqliteConnectionBuilder = new SqliteConnectionStringBuilder();
sqliteConnectionBuilder.DataSource = Path.Combine(AppContext.BaseDirectory, "ai-advent-mcp-server.db");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(sqliteConnectionBuilder.ToString()));
builder.Services.AddScoped<TaskRepository>();

builder.Services.AddHostedService<TaskSchedulerService>();

// Регистрируем MCP сервер
builder.Services.AddMcpServer()
    .WithHttpTransport()       // HTTP транспорт
    .WithTools<HelloTool>()
    .WithTools<PingTool>()
    .WithTools<MoonTelegramTool>()
    .WithTools<NominatimOpenStreetMapTool>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}

// Маппим MCP эндпоинт
app.MapMcp();

Console.WriteLine("🚀 MCP-сервер запущен");
Console.WriteLine("   Ожидаем подключения клиента...\n");

app.Run();