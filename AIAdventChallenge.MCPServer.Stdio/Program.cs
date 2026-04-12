using AIAdventChallenge.MCPServer.Stdio.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Для STDIO сервера полностью отключаем вывод логов в консоль,
// чтобы не ломать JSON-RPC транспорт.
builder.Logging.ClearProviders();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<MoonLanguareTool>();

await builder.Build().RunAsync();
