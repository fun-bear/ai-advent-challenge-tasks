using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AIAdventChallenge.Infrastructure;
using AIAdventChallenge.Infrastructure.Models;
using AIAdventChallenge.Logic.RAG;

namespace AIAdventChallenge.Handlers;

/// <summary>
/// День 25.
/// Мини-чат с in-memory историей + RAG-контекстом + памятью задачи (task state).
/// </summary>
public static class Day25TaskHandler
{
    private const string DefaultSystemPrompt =
        "Ты полезный AI-ассистент. Отвечай на русском языке, кратко и по делу.";

    private const int RagTopK = 10;
    private const int KeepLastHistoryMessages = 20;

    private static readonly List<ChatMessage> ChatHistory = [];
    private static TaskStateState TaskState = new();

    public static async Task<IResult> HandleAsync(IConfiguration configuration, string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Results.BadRequest("Query-параметр 'query' обязателен.");
        }

        var indexPath = Path.Combine(AppContext.BaseDirectory, "Day21Indexes", "day21_index_structured.json");
        if (!File.Exists(indexPath))
        {
            return Results.NotFound($"Индекс не найден: {indexPath}. Сначала вызовите /day21/task");
        }

        var ragResults = await Retriever.SearchAsync(indexPath, query, topK: RagTopK);

        var historySnapshot = SnapshotHistory();
        var taskStateSnapshot = SnapshotTaskState();

        var prompt = BuildRagChatPrompt(query, ragResults, historySnapshot, taskStateSnapshot);
        var answer = await AskLlmAsync(configuration, prompt);

        var updatedState = await UpdateTaskStateAsync(configuration, query, answer, taskStateSnapshot);

        PersistTurn(query, answer, updatedState);

        var response = new StringBuilder();
        response.AppendLine("=== Day25: Мини-чат с RAG + task state ===");
        response.AppendLine($"Запрос: {query}");
        response.AppendLine();
        response.AppendLine("Ответ:");
        response.AppendLine(answer);
        response.AppendLine();
        response.AppendLine("Память задачи:");
        response.AppendLine($"Цель диалога: {updatedState.Goal}");
        response.AppendLine($"Уточнения: {FormatList(updatedState.Clarifications)}");
        response.AppendLine($"Ограничения/термины: {FormatList(updatedState.ConstraintsOrTerms)}");
        response.AppendLine();
        response.AppendLine("Источники:");

        if (ragResults.Count == 0)
        {
            response.AppendLine("- Не найдено релевантных фрагментов в индексе.");
        }
        else
        {
            foreach (var r in ragResults)
            {
                response.AppendLine($"- ChunkId: {r.Chunk.ChunkId} | Score: {r.Score:F3} | Файл: {r.Chunk.File} | Секция: {r.Chunk.Section}");
            }
        }

        return Results.Content(response.ToString());
    }

    private static List<ChatMessage> SnapshotHistory()
    {
        return ChatHistory
            .Select(m => new ChatMessage(m.Role, m.Content))
            .ToList();
    }

    private static TaskStateState SnapshotTaskState()
    {
        return TaskState.Clone();
    }

    private static void PersistTurn(string query, string answer, TaskStateState updatedState)
    {
        ChatHistory.Add(new ChatMessage("user", query));
        ChatHistory.Add(new ChatMessage("assistant", answer));

        if (ChatHistory.Count > KeepLastHistoryMessages)
        {
            var removeCount = ChatHistory.Count - KeepLastHistoryMessages;
            ChatHistory.RemoveRange(0, removeCount);
        }

        TaskState = updatedState.Clone();
    }

    private static string BuildRagChatPrompt(
        string userQuery,
        IReadOnlyList<Retriever.SearchResult> chunks,
        IReadOnlyList<ChatMessage> history,
        TaskStateState taskState)
    {
        var contextBuilder = new StringBuilder();
        for (var i = 0; i < chunks.Count; i++)
        {
            var c = chunks[i].Chunk;
            contextBuilder.AppendLine($"[Источник {i + 1}] ChunkId: {c.ChunkId}, File: {c.File}, Section: {c.Section}, Score: {chunks[i].Score:F3}");
            contextBuilder.AppendLine(c.Text);
            contextBuilder.AppendLine();
        }

        var historyBuilder = new StringBuilder();
        if (history.Count == 0)
        {
            historyBuilder.AppendLine("(история пока пуста)");
        }
        else
        {
            foreach (var message in history)
            {
                historyBuilder.AppendLine($"{message.Role}: {message.Content}");
            }
        }

        return $$"""
Ты — ассистент мини-чата с RAG.

Требования:
1) Учитывай историю диалога.
2) В первую очередь опирайся на найденный контекст.
3) Если в контексте не хватает данных — честно скажи об этом.
4) Обязательно добавь раздел "Источники" в конце ответа и перечисли только реально использованные источники в формате:
   - ChunkId: ..., File: ..., Section: ...

Текущее состояние задачи:
- Цель: {{taskState.Goal}}
- Что уже уточнено: {{FormatList(taskState.Clarifications)}}
- Ограничения/термины: {{FormatList(taskState.ConstraintsOrTerms)}}

История диалога:
{{historyBuilder}}

RAG-контекст:
{{contextBuilder}}

Текущий вопрос пользователя:
{{userQuery}}
""";
    }

    private static async Task<TaskStateState> UpdateTaskStateAsync(
        IConfiguration configuration,
        string userQuery,
        string assistantAnswer,
        TaskStateState previous)
    {
        var prompt = $$"""
Ты обновляешь "память задачи" диалога.

Нужно вернуть строго JSON-объект (без markdown и пояснений) формата:
{
  "goal": "...",
  "clarifications": ["..."],
  "constraintsOrTerms": ["..."]
}

Правила:
- goal: краткая цель диалога (одна строка).
- clarifications: что пользователь уже уточнил по задаче.
- constraintsOrTerms: какие ограничения, условия или термины явно зафиксированы.
- Обнови состояние с учетом прошлого состояния и нового шага.
- Если данных нет, оставь прежние значения.

Предыдущее состояние:
goal: {{previous.Goal}}
clarifications: {{FormatList(previous.Clarifications)}}
constraintsOrTerms: {{FormatList(previous.ConstraintsOrTerms)}}

Новый шаг:
user: {{userQuery}}
assistant: {{assistantAnswer}}
""";

        var raw = await AskLlmAsync(configuration, prompt);

        try
        {
            var parsed = JsonSerializer.Deserialize<TaskStateDto>(raw);
            if (parsed is null)
            {
                return previous;
            }

            return new TaskStateState
            {
                Goal = NormalizeGoal(parsed.Goal, previous.Goal),
                Clarifications = MergeDistinct(previous.Clarifications, parsed.Clarifications),
                ConstraintsOrTerms = MergeDistinct(previous.ConstraintsOrTerms, parsed.ConstraintsOrTerms)
            };
        }
        catch
        {
            return previous;
        }
    }

    private static List<string> MergeDistinct(IReadOnlyList<string> oldValues, IReadOnlyList<string>? newValues)
    {
        var merged = oldValues
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToList();

        if (newValues is null)
        {
            return merged;
        }

        foreach (var value in newValues)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var trimmed = value.Trim();
            if (merged.Any(x => x.Equals(trimmed, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            merged.Add(trimmed);
        }

        return merged;
    }

    private static string NormalizeGoal(string? candidate, string fallback)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return fallback;
        }

        return candidate.Trim();
    }

    private static string FormatList(IReadOnlyList<string> values)
    {
        if (values.Count == 0)
        {
            return "нет";
        }

        return string.Join("; ", values);
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

    private sealed class TaskStateState
    {
        public string Goal { get; init; } = "Пока не зафиксирована";

        public List<string> Clarifications { get; init; } = [];

        public List<string> ConstraintsOrTerms { get; init; } = [];

        public TaskStateState Clone() =>
            new()
            {
                Goal = Goal,
                Clarifications = [.. Clarifications],
                ConstraintsOrTerms = [.. ConstraintsOrTerms]
            };
    }

    private sealed class TaskStateDto
    {
        [JsonPropertyName("goal")]
        public string? Goal { get; init; }

        [JsonPropertyName("clarifications")]
        public List<string>? Clarifications { get; init; }

        [JsonPropertyName("constraintsOrTerms")]
        public List<string>? ConstraintsOrTerms { get; init; }
    }
}