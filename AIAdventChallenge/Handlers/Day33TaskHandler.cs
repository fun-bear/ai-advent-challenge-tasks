using System.Text;
using AIAdventChallenge.Infrastructure;
using AIAdventChallenge.Logic.RAG;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace AIAdventChallenge.Handlers;

/// <summary>
/// День 33.
/// Ассистент поддержки пользователей.
/// </summary>
public static class Day33TaskHandler
{
    private const int RagTopK = 3;

    private const string SYSTEM_PROMPT =
    """
    Ты — ассистент поддержки пользователей.
    Отвечай вежливо, понятно и по делу.
    """;

    public static async Task<IResult> HandleAsync(IConfiguration configuration, long? accountId, string? message)
    {
        if (!accountId.HasValue)
        {
            return Results.BadRequest("Query-параметр 'accountId' обязателен.");
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return Results.BadRequest("Query-параметр 'message' обязателен.");
        }

        var clientTransport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri("http://localhost:5279")
        });

        await using var client = await McpClient.CreateAsync(clientTransport);

        var result = await client.CallToolAsync(
            "get_account_by_id",
            new Dictionary<string, object?>
            {
                ["id"] = accountId.Value
            });

        var accountData = GetText(result);

        var indexPath = Path.Combine(AppContext.BaseDirectory, "Day33Indexes", "day33_index_structured.json");
        if (!File.Exists(indexPath))
        {
            return Results.NotFound($"Индекс не найден: {indexPath}");
        }

        var ragResults = await Retriever.SearchAsync(indexPath, message, topK: RagTopK);

        var prompt = BuildPrompt(accountData, message, ragResults);
        var llmAnswer = await AskLlmAsync(configuration, prompt);

        return Results.Content(llmAnswer);
    }

    private static string BuildPrompt(
        string accountData,
        string userQuestion,
        IReadOnlyList<Retriever.SearchResult> ragChunks)
    {
        var ragContextBuilder = new StringBuilder();

        if (ragChunks.Count == 0)
        {
            ragContextBuilder.AppendLine("Релевантные фрагменты не найдены.");
        }
        else
        {
            for (var i = 0; i < ragChunks.Count; i++)
            {
                var chunk = ragChunks[i].Chunk;
                ragContextBuilder.AppendLine($"[Фрагмент {i + 1}] ChunkId: {chunk.ChunkId}, File: {chunk.File}, Section: {chunk.Section}, Score: {ragChunks[i].Score:F3}");
                ragContextBuilder.AppendLine(chunk.Text);
                ragContextBuilder.AppendLine();
            }
        }

        return $$"""
Используй данные аккаунта пользователя и RAG-контекст, чтобы ответить на вопрос как ассистент поддержки.
В первую очередь опирайся на найденные фрагменты.
Если данных недостаточно — так и скажи.

Данные аккаунта:
{{accountData}}

RAG-контекст:
{{ragContextBuilder}}

Вопрос пользователя:
{{userQuestion}}
""";
    }

    private static async Task<string> AskLlmAsync(IConfiguration configuration, string prompt)
    {
        var settings = configuration.GetSection("OpenAISettings");
        var apiKey = settings["ApiKey"] ?? throw new InvalidOperationException("OpenAISettings:ApiKey is missing.");
        var modelName = settings["ModelName"] ?? throw new InvalidOperationException("OpenAISettings:ModelName is missing.");
        var baseUrl = settings["BaseUrl"] ?? throw new InvalidOperationException("OpenAISettings:BaseUrl is missing.");

        var modelSettings = new AIModelSettings(modelName);
        using var llmClient = new LLMClient(baseUrl, apiKey, modelSettings, SYSTEM_PROMPT);
        var llmResult = await llmClient.ChatAsync(prompt);

        return string.IsNullOrWhiteSpace(llmResult.Content)
            ? "[Пустой ответ от AI LLM]"
            : llmResult.Content;
    }

    private static string GetText(CallToolResult result)
    {
        return result.Content.OfType<TextContentBlock>().First().Text;
    }
}