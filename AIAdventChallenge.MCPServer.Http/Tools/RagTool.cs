using ModelContextProtocol.Server;
using System.ComponentModel;
using AIAdventChallenge.McpServer.Logic;

namespace AIAdventChallenge.McpServer.Tools;

[McpServerToolType]
public sealed class RagTool
{
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

        var results = await Retriever.SearchAsync(indexPath, query, topK: 10);

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