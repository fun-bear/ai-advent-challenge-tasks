using System.Text;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace AIAdventChallenge.Handlers;

/// <summary>
/// День 17.
/// Поиск почтового индекса по адресу через MCP-инструмент Nominatim OpenStreetMap.
/// </summary>
public static class Day17TaskHandler
{
    public static async Task<IResult> HandleAsync(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return Results.BadRequest("Query-параметр 'address' обязателен.");
        }

        var logBuilder = new StringBuilder();
        logBuilder.AppendLine("=== Day17: Поиск почтового индекса ===\n");
        logBuilder.AppendLine($"Адрес: {address}\n");

        var clientTransport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri("http://localhost:5279")
        });

        await using var client = await McpClient.CreateAsync(clientTransport);

        var result = await client.CallToolAsync(
            "get_postal_codes_by_address",
            new Dictionary<string, object?> { ["address"] = address });

        logBuilder.AppendLine("Результат:");
        logBuilder.AppendLine(GetText(result));

        return Results.Content(logBuilder.ToString());
    }

    private static string GetText(CallToolResult result)
    {
        return result.Content.OfType<TextContentBlock>().First().Text;
    }
}