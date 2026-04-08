using System.Text;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace AIAdventChallenge.Handlers.Day18TaskHandlers;

/// <summary>
/// День 18, подзадача 01.
/// Отправка команды ping на MCP Server.
/// </summary>
public static class Day18Subtask01Handler
{
    public static async Task<IResult> HandleAsync(
        string? host,
        int? cycles,
        int? cycleTimeout)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return Results.BadRequest("Query-параметр 'host' обязателен.");
        }

        if (cycles is null || cycles <= 0)
        {
            return Results.BadRequest("Query-параметр 'cycles' обязателен и должен быть > 0.");
        }

        if (cycleTimeout is null || cycleTimeout <= 0)
        {
            return Results.BadRequest("Query-параметр 'cycleTimeout' обязателен и должен быть > 0.");
        }

        var logBuilder = new StringBuilder();
        logBuilder.AppendLine("=== Day18 / Subtask01: Запуск ping-задачи ===\n");
        logBuilder.AppendLine($"Host: {host}");
        logBuilder.AppendLine($"Cycles: {cycles}");
        logBuilder.AppendLine($"CycleTimeout: {cycleTimeout}\n");

        var clientTransport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri("http://localhost:5279")
        });

        await using var client = await McpClient.CreateAsync(clientTransport);

        var result = await client.CallToolAsync(
            "ping",
            new Dictionary<string, object?>
            {
                ["host"] = host,
                ["cycles"] = cycles,
                ["cycleTimeout"] = cycleTimeout
            });

        var taskId = GetText(result);
        logBuilder.AppendLine("Ping-задача создана.");
        logBuilder.AppendLine($"TaskId: {taskId}");

        return Results.Content(logBuilder.ToString());
    }

    private static string GetText(CallToolResult result)
    {
        return result.Content.OfType<TextContentBlock>().First().Text;
    }
}