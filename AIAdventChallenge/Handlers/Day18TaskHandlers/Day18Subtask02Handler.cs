using System.Text;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace AIAdventChallenge.Handlers.Day18TaskHandlers;

/// <summary>
/// День 18, подзадача 02.
/// Получение лога ping-задачи по GUID.
/// </summary>
public static class Day18Subtask02Handler
{
    public static async Task<IResult> HandleAsync(string? taskId)
    {
        if (string.IsNullOrWhiteSpace(taskId))
        {
            return Results.BadRequest("Query-параметр 'taskId' обязателен.");
        }

        if (!Guid.TryParse(taskId, out var parsedTaskId))
        {
            return Results.BadRequest("Query-параметр 'taskId' должен быть корректным GUID.");
        }

        var logBuilder = new StringBuilder();
        logBuilder.AppendLine("=== Day18 / Subtask02: Получение лога ping-задачи ===\n");
        logBuilder.AppendLine($"TaskId: {parsedTaskId}\n");

        var clientTransport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri("http://localhost:5279")
        });

        await using var client = await McpClient.CreateAsync(clientTransport);

        var result = await client.CallToolAsync(
            "get_log",
            new Dictionary<string, object?>
            {
                ["taskId"] = parsedTaskId
            });

        logBuilder.AppendLine("Лог задачи:");
        logBuilder.AppendLine(GetText(result));

        return Results.Content(logBuilder.ToString());
    }

    private static string GetText(CallToolResult result)
    {
        return result.Content.OfType<TextContentBlock>().First().Text;
    }
}