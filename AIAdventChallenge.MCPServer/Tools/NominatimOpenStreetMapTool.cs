using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace AIAdventChallenge.McpServer.Tools;

[McpServerToolType]
public sealed class NominatimOpenStreetMapTool
{
    [McpServerTool, Description("Поиск почтового индекса по адресу. Возвращает массив возможных почтовых индексов.")]
    public static async Task<string[]> GetPostalCodesByAddress(
        [Description("Адрес одной строкой")] string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return [];
        }

        var url = $"https://nominatim.openstreetmap.org/search?format=json&addressdetails=1&q={Uri.EscapeDataString(address)}";

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("AIAdventChallenge.McpServer/1.0");

        var json = await httpClient.GetStringAsync(url);

        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var postcodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in document.RootElement.EnumerateArray())
        {
            if (!item.TryGetProperty("address", out var addressElement) ||
                addressElement.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (!addressElement.TryGetProperty("postcode", out var postcodeElement) ||
                postcodeElement.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var postcode = postcodeElement.GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(postcode))
            {
                continue;
            }

            postcodes.Add(postcode);
        }

        return [.. postcodes];
    }
}