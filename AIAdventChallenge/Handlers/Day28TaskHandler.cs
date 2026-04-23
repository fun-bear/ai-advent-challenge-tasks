using System.Text;
using AIAdventChallenge.Infrastructure;
using AIAdventChallenge.Logic.RAG;

namespace AIAdventChallenge.Handlers;

/// <summary>
/// День 28.
/// Сравнение ответа LLM без RAG и с RAG-контекстом из индекса Day21.
/// В отличие от Day22 использует локальную LLM (Ollama) через OpenAI-совместимый API.
/// </summary>
public static class Day28TaskHandler
{
    private const string DefaultSystemPrompt =
        "You are a helpful AI assistant. Answer concisely, clearly, and to the point.";

    private const string OLLAMA_API_KEY = "ollama";
    private const string OLLAMA_MODEL_NAME = "llama3.1:8b";
    private const string OLLAMA_BASE_URL = "http://localhost:11434/v1/";

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

    public static async Task<IResult> HandleAsync(int index)
    {
        if (index < 0 || index >= LLMQuestions.Length)
        {
            return Results.BadRequest($"Некорректный index: {index}. Допустимый диапазон: 0..{LLMQuestions.Length - 1}");
        }

        var userQuery = LLMQuestions[index];

        var response = new StringBuilder();
        response.AppendLine("=== Day28: RAG vs без RAG (локальная LLM) ===");
        response.AppendLine($"Вопрос: {userQuery}");
        response.AppendLine();

        var indexPath = Path.Combine(AppContext.BaseDirectory, "Day21Indexes", "day21_index_structured.json");
        if (!File.Exists(indexPath))
        {
            return Results.NotFound($"Индекс не найден: {indexPath}. Сначала вызовите /day21/task");
        }

        var results = await Retriever.SearchAsync(indexPath, userQuery, topK: 10);

        response.AppendLine("Top-10 релевантных чанков:");
        foreach (var r in results)
        {
            response.AppendLine($"ChunkId: {r.Chunk.ChunkId} | Score: {r.Score:F3} | Файл: {r.Chunk.File} | Секция: {r.Chunk.Section}");
            response.AppendLine(r.Chunk.Text[..Math.Min(1000, r.Chunk.Text.Length)]);
            response.AppendLine("---");
        }

        var answerWithoutRag = await AskLlmAsync(BuildPlainPrompt(userQuery));
        var answerWithRag = await AskLlmAsync(BuildRagPrompt(userQuery, results));

        response.AppendLine();
        response.AppendLine("Ответ LLM БЕЗ RAG:");
        response.AppendLine(answerWithoutRag);
        response.AppendLine();
        response.AppendLine("Ответ LLM С RAG:");
        response.AppendLine(answerWithRag);

        return Results.Content(response.ToString());
    }

    private static string BuildPlainPrompt(string userQuery)
    {
        return $$"""
Answer the user's question concisely and directly.
If you are not sure, state it explicitly.

Question: {{userQuery}}
""";
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

    private static async Task<string> AskLlmAsync(string prompt)
    {
        var modelSettings = new AIModelSettings(OLLAMA_MODEL_NAME);
        using var llmClient = new LLMClient(
            OLLAMA_BASE_URL,
            OLLAMA_API_KEY,
            modelSettings,
            DefaultSystemPrompt,
            timeout: TimeSpan.FromMinutes(10));
        var result = await llmClient.ChatAsync(prompt);

        return string.IsNullOrWhiteSpace(result.Content)
            ? "[Пустой ответ от AI LLM]"
            : result.Content;
    }
}