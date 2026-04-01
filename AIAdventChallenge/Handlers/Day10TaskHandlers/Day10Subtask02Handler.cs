using AIAdventChallenge.Infrastructure;
using AIAdventChallenge.Infrastructure.Models;
using AIAdventChallenge.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace AIAdventChallenge.Handlers.Day10TaskHandlers;

/// <summary>
/// День 10, подзадача 02.
/// Специальный handler для решения составления и обработки ТЗ.
/// Режим: Sticky Facts.
/// </summary>
public static class Day10Subtask02Handler
{
    private const string AGENT_KEY = "day10-02-tech-design-agent";

    private const string FACTS_COMPILATION_REQUEST = "Собери новые факты на основании прежних фактов и последних сообщений.";

    private const int KEEP_LAST_MESSAGES = 10;

    public static async Task<IResult> HandleAsync(
        IConfiguration configuration,
        AppDbContext dbContext,
        string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return Results.BadRequest("Query-параметр 'message' обязателен.");
        }

        var settings = configuration.GetSection("OpenAISettings");
        var apiKey = settings["ApiKey"] ?? throw new InvalidOperationException("OpenAISettings:ApiKey is missing.");
        var modelName = settings["ModelName"] ?? throw new InvalidOperationException("OpenAISettings:ModelName is missing.");
        var baseUrl = settings["BaseUrl"] ?? throw new InvalidOperationException("OpenAISettings:BaseUrl is missing.");

        var modelSettings = new AIModelSettings(modelName);
        var updatedSystemPrompt = 
        $"""
        {Day10TaskDescriptions.SYSTEM_PROMPT}

        Кроме того, у тебя добавляется третий тип входного сообщения:
        **{FACTS_COMPILATION_REQUEST}**

        Факты ТЗ собираем из истории чата в категории: цели, ограничения, предпочтения. Прочие категории отбрасываем.
        Новые факты нужно объединить с ранее предоставленными. Если факты не изменились, то нужно вывести имеющиеся факты.
        """;
        using var llmClient = new LLMClient(baseUrl, apiKey, modelSettings, updatedSystemPrompt);

        var factsEntry = await dbContext.AgentSummaryEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AgentKey == AGENT_KEY);

        var facts = factsEntry?.Summary;
        await llmClient.ChatAsync($"Текущие факты ТЗ: {facts ?? "еще нет"}");

        var persistedHistory = await LoadHistoryAsync(dbContext);
        if (persistedHistory.Count == 0) // Init history
        {
            persistedHistory = Day10TaskDescriptions.INITIAL_MESSAGES
                .Select(messageText => new ChatMessage("user", messageText))
                .ToList();
        }

        llmClient.AddHistory(persistedHistory);

        var chatResult = await llmClient.ChatAsync(message);

        var lastMessagesHistory = llmClient.ExportHistory()
            .Where((_, index) => index > 2) // Skip system prompt (0) and facts chat messages (1, 2)
            .TakeLast(KEEP_LAST_MESSAGES)
            .ToList();

        await SaveHistoryAsync(dbContext, lastMessagesHistory);

        var factsResult = await llmClient.ChatAsync(FACTS_COMPILATION_REQUEST);
        await SaveFactsAsync(dbContext, factsResult.Content);

        return Results.Ok(new
        {
            UserMessage = message,
            AIMessage = chatResult.Content,
            Facts = factsResult.Content,
            LastMessagesCount = lastMessagesHistory.Count,
            TotalTokens = chatResult.TotalTokens
        });
    }

    private static Task<List<ChatMessage>> LoadHistoryAsync(AppDbContext dbContext)
    {
        return dbContext.AgentHistoryEntries
            .AsNoTracking()
            .Where(x => x.AgentKey == AGENT_KEY)
            .OrderBy(x => x.SortOrder)
            .Select(x => new ChatMessage(x.Role, x.Content))
            .ToListAsync();
    }

    private static async Task SaveHistoryAsync(AppDbContext dbContext, IReadOnlyList<ChatMessage> history)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        await dbContext.AgentHistoryEntries
            .Where(x => x.AgentKey == AGENT_KEY)
            .ExecuteDeleteAsync();

        for (var i = 0; i < history.Count; i++)
        {
            var message = history[i];

            dbContext.AgentHistoryEntries.Add(new AgentHistoryEntry
            {
                AgentKey = AGENT_KEY,
                SortOrder = i,
                Role = message.Role,
                Content = message.Content
            });
        }

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static async Task SaveFactsAsync(AppDbContext dbContext, string facts)
    {
        var entry = await dbContext.AgentSummaryEntries
            .FirstOrDefaultAsync(x => x.AgentKey == AGENT_KEY);

        if (entry is null)
        {
            dbContext.AgentSummaryEntries.Add(new AgentSummaryEntry
            {
                AgentKey = AGENT_KEY,
                Summary = facts
            });
        }
        else
        {
            entry.Summary = facts;
        }

        await dbContext.SaveChangesAsync();
    }
}