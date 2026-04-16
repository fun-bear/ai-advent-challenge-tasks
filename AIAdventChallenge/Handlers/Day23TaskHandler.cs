using System.Text;
using AIAdventChallenge.Infrastructure;
using AIAdventChallenge.Logic.RAG;
using SemanticKernel.Rankers.Abstractions;
using SemanticKernel.Rankers.BM25;
using SemanticKernel.Rankers.Pipelines;

namespace AIAdventChallenge.Handlers;

/// <summary>
/// День 23.
/// Генерация ответа LLM с RAG-контекстом из индекса Day21 (c фильтрацией и реранкингом).
/// </summary>
public static class Day23TaskHandler
{
    private const string DefaultSystemPrompt =
        "You are a helpful AI assistant. Answer concisely, clearly, and to the point.";

    private static readonly string[] LLMQuestions =
    [
        "What about Joseph Stalin?",
        "The best friend of Chandler",
        "Suggest name of perk with coffee and cakes",
        "Is divorce a bad thing for a man? How he usually reacts to it?",
        "Who is a brother of Monica?",
        "The worst Thanksgiving day ever",
        "Who is the most funny person in this show?",
        "The best name for monkey",
        "Is Paolo a good person?",
        "What profession do main characters do have?"
    ];

    public static async Task<IResult> HandleAsync(
        IConfiguration configuration,
        int index,
        float minCosSim = 0.0f,
        int topK = 10,
        int topKAfterFiltration = 5,
        double scoreThreshold = 0.1)
    {
        if (index < 0 || index >= LLMQuestions.Length)
        {
            return Results.BadRequest($"Некорректный index: {index}. Допустимый диапазон: 0..{LLMQuestions.Length - 1}");
        }

        if (topK <= 0)
        {
            return Results.BadRequest("Параметр topK должен быть > 0.");
        }

        if (topKAfterFiltration <= 0)
        {
            return Results.BadRequest("Параметр topKAfterFiltration должен быть > 0.");
        }

        if (minCosSim is < -1.0f or > 1.0f)
        {
            return Results.BadRequest("Параметр minCosSim должен быть в диапазоне [-1; 1].");
        }

        if (scoreThreshold < 0)
        {
            return Results.BadRequest("Параметр scoreThreshold должен быть >= 0.");
        }

        var userQuery = LLMQuestions[index];

        var response = new StringBuilder();
        response.AppendLine("=== Day23: Ответ с RAG ===");
        response.AppendLine($"Вопрос: {userQuery}");
        response.AppendLine($"Параметры: topK={topK}, topKAfterFiltration={topKAfterFiltration}, minCosSim={minCosSim:F3}, scoreThreshold={scoreThreshold:F3}");
        response.AppendLine();

        var indexPath = Path.Combine(AppContext.BaseDirectory, "Day21Indexes", "day21_index_structured.json");
        if (!File.Exists(indexPath))
        {
            return Results.NotFound($"Индекс не найден: {indexPath}. Сначала вызовите /day21/task");
        }

        var searchResults = await Retriever.SearchAsync(indexPath, userQuery, topK: topK);
        var results = searchResults
            .Where(r => r.Score >= minCosSim)
            .ToList();

        // Реранкинг после фильтрации по minCosSim и topK
        var rerankedResults = await RerankWithCascadeAsync(userQuery, results, topK, topKAfterFiltration, scoreThreshold);

        response.AppendLine($"Релевантные чанки после фильтра (Score >= {minCosSim:F3}): {results.Count}");
        response.AppendLine($"После CascadeRerankPipeline: {rerankedResults.Count}");
        response.AppendLine();
        response.AppendLine("Релевантные чанки после реранкинга:");
        foreach (var r in rerankedResults)
        {
            response.AppendLine($"ChunkId: {r.Chunk.ChunkId} | Score: {r.Score:F3} | Файл: {r.Chunk.File} | Секция: {r.Chunk.Section}");
            response.AppendLine(r.Chunk.Text[..Math.Min(1000, r.Chunk.Text.Length)]);
            response.AppendLine("---");
        }

        var answerWithRag = await AskLlmAsync(configuration, BuildRagPrompt(userQuery, rerankedResults));

        response.AppendLine();
        response.AppendLine("Ответ LLM С RAG:");
        response.AppendLine(answerWithRag);

        return Results.Content(response.ToString());
    }

    private static string BuildRagPrompt(string userQuery, List<Retriever.SearchResult> chunks)
    {
        var contextBuilder = new StringBuilder();
        for (var i = 0; i < chunks.Count; i++)
        {
            var c = chunks[i].Chunk;
            contextBuilder.AppendLine($"[Фрагмент {i + 1}] ChunkId: {c.ChunkId}, Файл: {c.File}, Секция: {c.Section}, Score: {chunks[i].Score:F3}");
            contextBuilder.AppendLine(c.Text);
            contextBuilder.AppendLine();
        }

        return $$"""
You are an assistant answering based on the context below.
Prioritize the provided fragments and make sure to use them in your answer.
If the context is not enough, you may carefully add general knowledge,
but explicitly label it as information beyond the RAG context.

Context:
{{contextBuilder}}

User question: {{userQuery}}
""";
    }

    private static async Task<List<Retriever.SearchResult>> RerankWithCascadeAsync(
        string userQuery,
        List<Retriever.SearchResult> filteredResults,
        int topK,
        int topKAfterFiltration,
        double scoreThreshold)
    {
        if (filteredResults.Count == 0)
        {
            return filteredResults;
        }

        var bm25Ranker = new BM25Reranker();

        var config = new CascadeRerankPipelineConfig
        {
            TopK = topK,
            TopM = topKAfterFiltration,
            ScoreThreshold = scoreThreshold
        };

        var pipeline = new CascadeRerankPipeline(new List<IRanker> { bm25Ranker }, config);

        var indexed = filteredResults
            .Select((item, idx) => new { idx, text = item.Chunk.Text })
            .ToList();

        var textToIndices = new Dictionary<string, Queue<int>>();
        foreach (var x in indexed)
        {
            if (!textToIndices.TryGetValue(x.text, out var queue))
            {
                queue = new Queue<int>();
                textToIndices[x.text] = queue;
            }

            queue.Enqueue(x.idx);
        }

        var reranked = new List<Retriever.SearchResult>();
        await foreach (var (docText, score) in pipeline.RankAsync(userQuery, ToAsyncEnumerable(indexed.Select(x => x.text)), topN: config.TopM))
        {
            if (textToIndices.TryGetValue(docText, out var queue) && queue.Count > 0)
            {
                var original = filteredResults[queue.Dequeue()];
                // Возвращаем score реранкера для отображения и сортировки
                reranked.Add(new Retriever.SearchResult
                {
                    Chunk = original.Chunk,
                    Score = (float)score
                });
            }
        }

        return reranked;
    }

    private static async IAsyncEnumerable<string> ToAsyncEnumerable(IEnumerable<string> source)
    {
        foreach (var item in source)
        {
            yield return item;
            await Task.Yield();
        }
    }

    private static async Task<string> AskLlmAsync(IConfiguration configuration, string prompt)
    {
        var settings = configuration.GetSection("OpenAISettings");
        var apiKey = settings["ApiKey"] ?? throw new InvalidOperationException("OpenAISettings:ApiKey is missing.");
        var modelName = settings["ModelName"] ?? throw new InvalidOperationException("OpenAISettings:ModelName is missing.");
        var baseUrl = settings["BaseUrl"] ?? throw new InvalidOperationException("OpenAISettings:BaseUrl is missing.");

        var modelSettings = new AIModelSettings(modelName);
        using var llmClient = new LLMClient(baseUrl, apiKey, modelSettings, DefaultSystemPrompt);
        var result = await llmClient.ChatAsync(prompt);

        return string.IsNullOrWhiteSpace(result.Content)
            ? "[Пустой ответ от AI LLM]"
            : result.Content;
    }
}