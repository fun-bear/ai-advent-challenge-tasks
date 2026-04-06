using System.Text;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace AIAdventChallenge.Handlers;

/// <summary>
/// День 16.
/// Комментарий: демонстрация работы с локальным MCP Server.
/// </summary>
public static class Day16TaskHandler
{
    public static async Task<IResult> HandleAsync()
    {
        var logBuilder = new StringBuilder();

        logBuilder.AppendLine("=== MCP Client ===\n");

        // 1. Подключаемся к серверу через HTTP
        var clientTransport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri("http://localhost:5279")
        });

        // 2. Создаём клиента и устанавливаем соединение
        await using var client = await McpClient.CreateAsync(clientTransport);

        logBuilder.AppendLine("✅ Соединение установлено!\n");

        // 3. Получаем список инструментов
        var tools = await client.ListToolsAsync();

        logBuilder.AppendLine($"📦 Доступно инструментов: {tools.Count}\n");

        foreach (var tool in tools)
        {
            logBuilder.AppendLine($"  🔧 {tool.Name}");
            logBuilder.AppendLine($"     {tool.Description}");
            logBuilder.AppendLine();
        }

        // 4. Вызываем инструменты
        logBuilder.AppendLine("=== Вызовы инструментов ===\n");

        var greetResult = await client.CallToolAsync("greet",
            new Dictionary<string, object?> { ["name"] = "Студент" });
        logBuilder.AppendLine($"  Greet → {GetText(greetResult)}");

        var addResult = await client.CallToolAsync("add",
            new Dictionary<string, object?> { ["a"] = 42, ["b"] = 58 });
        logBuilder.AppendLine($"  Add   → {GetText(addResult)}");

        var timeResult = await client.CallToolAsync("get_current_time",
            new Dictionary<string, object?>());
        logBuilder.AppendLine($"  Time  → {GetText(timeResult)}");

        logBuilder.AppendLine("\n✅ Готово!");

        return Results.Content(logBuilder.ToString());
    }

    private static string GetText(CallToolResult result)
    {
        return result.Content.OfType<TextContentBlock>().First().Text;
    }
}