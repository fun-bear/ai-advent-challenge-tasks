using AIAdventChallenge.Infrastructure;
using AIAdventChallenge.Infrastructure.Models;
using AIAdventChallenge.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace AIAdventChallenge.Handlers;

/// <summary>
/// День 30.
/// Чат с хранением истории в БД и ограничением общего размера истории в токенах.
/// </summary>
public static class Day30TaskHandler
{
    private const string SYSTEM_PROMPT =
    """
    Ты — умная голосовая колонка.
    Ты общаешься с пользователем на самые разные темы и отвечаешь на разные вопросы.
    Отвечай естественно, дружелюбно и на русском языке.
    """;

    private const string AGENT_KEY_PREFIX = "day30-chat-agent-";
    private const int INPUT_TOKENS_LIMIT = 50;
    private const int HISTORY_TOKENS_LIMIT = 500;

    private const string OLLAMA_API_KEY = "ollama";
    private const string OLLAMA_MODEL_NAME = "llama3.1:8b";
    private const string OLLAMA_BASE_URL = "http://localhost:11434/v1/";

    public static async Task<IResult> HandleAsync(
        AppDbContext dbContext,
        int? chatId,
        string? message)
    {
        if (!chatId.HasValue)
        {
            return Results.BadRequest("Query-параметр 'chatId' обязателен.");
        }

        if (chatId.Value < 1)
        {
            return Results.BadRequest("Query-параметр 'chatId' должен быть >= 1.");
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return Results.BadRequest("Query-параметр 'message' обязателен.");
        }

        var estimatedInputTokens = EstimateTokens(message);
        if (estimatedInputTokens > INPUT_TOKENS_LIMIT)
        {
            return Results.BadRequest(new
            {
                ChatId = chatId.Value,
                UserMessage = message,
                EstimatedInputTokens = estimatedInputTokens,
                Error = $"Входное сообщение превышает лимит в {INPUT_TOKENS_LIMIT} токенов."
            });
        }

        var agentKey = $"{AGENT_KEY_PREFIX}{chatId.Value}";
        var persistedHistory = await LoadHistoryAsync(dbContext, agentKey);

        var modelSettings = new AIModelSettings(
            OLLAMA_MODEL_NAME,
            Temperature: 0.5m);

        using var llmClient = new LLMClient(
            OLLAMA_BASE_URL,
            OLLAMA_API_KEY,
            modelSettings,
            SYSTEM_PROMPT,
            timeout: TimeSpan.FromMinutes(2));

        if (persistedHistory.Count > 0)
        {
            llmClient.AddHistory(persistedHistory);
        }

        var result = await llmClient.ChatAsync(message);

        var historyToSave = llmClient.ExportHistory()
            .Skip(1) // Skip system prompt (0)
            .ToList();

        if (result.TotalTokens > HISTORY_TOKENS_LIMIT)
        {
            return Results.BadRequest(new
            {
                ChatId = chatId.Value,
                UserMessage = message,
                TotalTokens = result.TotalTokens,
                Error = $"Общий размер истории превышает лимит в {HISTORY_TOKENS_LIMIT} токенов. История не сохранена."
            });
        }

        await SaveHistoryAsync(dbContext, agentKey, historyToSave);

        return Results.Ok(new
        {
            ChatId = chatId.Value,
            UserMessage = message,
            AIMessage = result.Content,
            HistoryCount = historyToSave.Count,
            TotalTokens = result.TotalTokens
        });
    }

    private static int EstimateTokens(string text)
    {
        // Грубая оценка:
        // ~4 символа на токен для английского
        // ~2-3 символа на токен для русского

        double charsPerToken = text.Any(c => c >= 'а' && c <= 'я') ? 2.5 : 4.0;
        return (int)Math.Ceiling(text.Length / charsPerToken);
    }

    private static Task<List<ChatMessage>> LoadHistoryAsync(AppDbContext dbContext, string agentKey)
    {
        return dbContext.AgentHistoryEntries
            .AsNoTracking()
            .Where(x => x.AgentKey == agentKey)
            .OrderBy(x => x.SortOrder)
            .Select(x => new ChatMessage(x.Role, x.Content))
            .ToListAsync();
    }

    private static async Task SaveHistoryAsync(AppDbContext dbContext, string agentKey, IReadOnlyList<ChatMessage> history)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        await dbContext.AgentHistoryEntries
            .Where(x => x.AgentKey == agentKey)
            .ExecuteDeleteAsync();

        for (var i = 0; i < history.Count; i++)
        {
            var message = history[i];

            dbContext.AgentHistoryEntries.Add(new AgentHistoryEntry
            {
                AgentKey = agentKey,
                SortOrder = i,
                Role = message.Role,
                Content = message.Content
            });
        }

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}
