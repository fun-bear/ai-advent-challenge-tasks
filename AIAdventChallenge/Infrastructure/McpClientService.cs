using System.Text.Json;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace AIAdventChallenge.Infrastructure;

/// <summary>
/// Сервис подключения к MCP-серверу по stdio и адаптации MCP tools в KernelFunction.
/// </summary>
public sealed class McpClientService : IAsyncDisposable
{
    private readonly McpClient _client;

    private McpClientService(McpClient client)
    {
        _client = client;
    }

    public static async Task<McpClientService> CreateAsync(string mcpServerProjectPath)
    {
        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "day34-mcp-stdio-client",
            Command = "dotnet",
            Arguments = ["run", "--project", mcpServerProjectPath, "--no-build"]
        });

        var client = await McpClient.CreateAsync(transport);
        return new McpClientService(client);
    }

    public async Task<IReadOnlyCollection<KernelFunction>> GetKernelFunctionsAsync()
    {
        var mcpTools = await _client.ListToolsAsync();
        Console.WriteLine($"[MCP] Загружено инструментов: {mcpTools.Count}");

        var functions = new List<KernelFunction>(mcpTools.Count);

        foreach (var tool in mcpTools)
        {
            var parameters = ExtractParameters(tool);
            var description = string.IsNullOrWhiteSpace(tool.Description)
                ? $"MCP tool: {tool.Name}"
                : tool.Description;

            // Логируем для отладки
            Console.WriteLine($"[MCP] Tool: {tool.Name}");
            Console.WriteLine($"[MCP]   Description: {description}");
            Console.WriteLine($"[MCP]   Parameters: [{string.Join(", ", parameters.Select(p => $"{p.Name}:{p.ParameterType?.Name}"))}]");

            // Захватываем tool в замыкание, чтобы не пересоздавать список каждый вызов
            var capturedTool = tool;

            var function = KernelFunctionFactory.CreateFromMethod(
                method: (KernelArguments args) => CallToolAsync(capturedTool, args),
                functionName: tool.Name,
                description: description,
                parameters: parameters);

            functions.Add(function);
        }

        return functions;
    }

    private static IReadOnlyList<KernelParameterMetadata> ExtractParameters(McpClientTool tool)
    {
        var parameters = new List<KernelParameterMetadata>();

        if (tool.JsonSchema.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            return parameters;

        // Извлекаем required-поля
        var requiredFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (tool.JsonSchema.TryGetProperty("required", out var req) && req.ValueKind == JsonValueKind.Array)
        {
            foreach (var r in req.EnumerateArray())
            {
                if (r.GetString() is { } name)
                    requiredFields.Add(name);
            }
        }

        // Извлекаем properties
        if (!tool.JsonSchema.TryGetProperty("properties", out var props) || props.ValueKind != JsonValueKind.Object)
            return parameters;

        foreach (var prop in props.EnumerateObject())
        {
            var description = prop.Value.TryGetProperty("description", out var desc)
                ? desc.GetString() ?? ""
                : "";

            // Определяем C# тип из JSON Schema type
            var schemaType = GetSchemaType(typeEl: prop.Value.TryGetProperty("type", out var typeEl) ? typeEl : default);

            var clrType = schemaType switch
            {
                "integer" => typeof(long),
                "number" => typeof(double),
                "boolean" => typeof(bool),
                "array" => typeof(string),  // массивы передаём как JSON-строку
                _ => typeof(string)
            };

            parameters.Add(new KernelParameterMetadata(prop.Name)
            {
                Description = description,
                ParameterType = clrType,
                IsRequired = requiredFields.Contains(prop.Name)
            });
        }

        return parameters;
    }

    private static string? GetSchemaType(JsonElement typeEl)
    {
        if (typeEl.ValueKind == JsonValueKind.String)
        {
            return typeEl.GetString();
        }

        if (typeEl.ValueKind == JsonValueKind.Array)
        {
            // JSON Schema иногда задаёт type как массив, например: ["string", "null"]
            // Берём первый непустой и не-null тип.
            foreach (var item in typeEl.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String)
                    continue;

                var t = item.GetString();
                if (!string.IsNullOrWhiteSpace(t) && !string.Equals(t, "null", StringComparison.OrdinalIgnoreCase))
                {
                    return t;
                }
            }
        }

        return null;
    }

    private async Task<string> CallToolAsync(McpClientTool tool, KernelArguments arguments)
    {
        Console.WriteLine($"[MCP] Вызов: {tool.Name}");

        // Собираем аргументы: KernelArguments → Dictionary<string, object?>
        var toolArguments = new Dictionary<string, object?>();

        foreach (var (key, value) in arguments)
        {
            // Пропускаем служебные параметры SK
            if (key is "settings" or "arguments" or "kernel")
                continue;

            if (value is null)
                continue;

            toolArguments[key] = value is JsonElement je ? ConvertJsonElement(je) : value;
            Console.WriteLine($"[MCP]   {key} = {value}");
        }

        var result = await _client.CallToolAsync(tool.Name, toolArguments);

        // Извлекаем текстовые блоки из результата
        var textBlocks = result.Content
            .OfType<TextContentBlock>()
            .Select(b => b.Text)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToArray();

        if (textBlocks.Length > 0)
        {
            var text = string.Join("\n", textBlocks);
            Console.WriteLine($"[MCP] Результат: {text}");
            return text;
        }

        // Fallback — сериализация всего Content
        var fallback = JsonSerializer.Serialize(result.Content);
        Console.WriteLine($"[MCP] Результат (raw): {fallback}");
        return fallback;
    }

    private static object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var l) => l,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToArray(),
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
            _ => element.GetRawText()
        };
    }

    public ValueTask DisposeAsync() => _client.DisposeAsync();
}