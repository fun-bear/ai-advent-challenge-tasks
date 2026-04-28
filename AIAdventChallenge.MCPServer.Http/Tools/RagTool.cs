using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIAdventChallenge.McpServer.Tools;

[McpServerToolType]
public sealed class RagTool
{
    private const string OllamaEmbeddingModel = "nomic-embed-text";
    private static readonly HttpClient Http = new() { BaseAddress = new Uri("http://localhost:11434/") };

    [McpServerTool, Description("Ищет top-10 релевантных чанков в Day31 индексе по query")]
    public static async Task<RagSearchItem[]> Search(
        [Description("Поисковый запрос")] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var indexPath = Path.Combine(AppContext.BaseDirectory, "Day31Indexes", "day31_index_structured.json");
        if (!File.Exists(indexPath))
        {
            throw new FileNotFoundException($"Индекс не найден: {indexPath}", indexPath);
        }

        var results = await SearchAsync(indexPath, query, topK: 10);

        return results
            .Select(r => new RagSearchItem
            {
                ChunkId = r.Chunk.ChunkId,
                Source = r.Chunk.Source,
                File = r.Chunk.File,
                Title = r.Chunk.Title,
                Section = r.Chunk.Section,
                Strategy = r.Chunk.Strategy,
                Text = r.Chunk.Text,
                Score = r.Score
            })
            .ToArray();
    }

    private static async Task<List<SearchResult>> SearchAsync(string indexJsonPath, string userQuery, int topK)
    {
        var json = await File.ReadAllTextAsync(indexJsonPath);
        var index = JsonSerializer.Deserialize<IndexResult>(json)
            ?? throw new InvalidOperationException("Не удалось десериализовать индекс.");

        var queryEmbedding = await GetEmbeddingAsync(userQuery);

        return index.Chunks
            .Where(c => c.Embedding is { Count: > 0 })
            .Select(chunk => new SearchResult
            {
                Chunk = chunk,
                Score = CosineSimilarity(queryEmbedding, chunk.Embedding!)
            })
            .OrderByDescending(r => r.Score)
            .Take(topK)
            .ToList();
    }

    private static async Task<List<float>> GetEmbeddingAsync(string text)
    {
        var request = JsonSerializer.Serialize(new { model = OllamaEmbeddingModel, input = text });
        using var content = new StringContent(request, Encoding.UTF8, "application/json");
        using var response = await Http.PostAsync("api/embed", content);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<OllamaEmbedResponse>(body)
            ?? throw new InvalidOperationException("Не удалось получить embedding-ответ от Ollama.");

        var vector = result.Embeddings?.FirstOrDefault();
        if (vector is null || vector.Count == 0)
        {
            throw new InvalidOperationException("Пустой embedding в ответе Ollama.");
        }

        return vector;
    }

    private static float CosineSimilarity(List<float> a, List<float> b)
    {
        if (a.Count != b.Count)
            throw new ArgumentException("Векторы разной длины");

        float dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Count; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        return dot / (MathF.Sqrt(normA) * MathF.Sqrt(normB));
    }

    private sealed class SearchResult
    {
        public required ChunkRecord Chunk { get; init; }
        public float Score { get; init; }
    }

    private sealed class OllamaEmbedResponse
    {
        [JsonPropertyName("embeddings")]
        public List<List<float>>? Embeddings { get; init; }
    }

    private sealed class IndexResult
    {
        [JsonPropertyName("chunks")]
        public required List<ChunkRecord> Chunks { get; init; }
    }

    private sealed class ChunkRecord
    {
        [JsonPropertyName("chunk_id")]
        public required string ChunkId { get; init; }

        [JsonPropertyName("source")]
        public required string Source { get; init; }

        [JsonPropertyName("file")]
        public required string File { get; init; }

        [JsonPropertyName("title")]
        public required string Title { get; init; }

        [JsonPropertyName("section")]
        public required string Section { get; init; }

        [JsonPropertyName("strategy")]
        public required string Strategy { get; init; }

        [JsonPropertyName("text")]
        public required string Text { get; init; }

        [JsonPropertyName("embedding")]
        public List<float>? Embedding { get; init; }
    }
}

public sealed class RagSearchItem
{
    [Description("ID чанка")]
    public required string ChunkId { get; init; }

    [Description("Источник")]
    public required string Source { get; init; }

    [Description("Файл")]
    public required string File { get; init; }

    [Description("Заголовок")]
    public required string Title { get; init; }

    [Description("Секция")]
    public required string Section { get; init; }

    [Description("Стратегия разбиения")]
    public required string Strategy { get; init; }

    [Description("Текст чанка")]
    public required string Text { get; init; }

    [Description("Косинусная релевантность")]
    public float Score { get; init; }
}