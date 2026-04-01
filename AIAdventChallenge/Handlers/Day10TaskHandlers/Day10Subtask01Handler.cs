using AIAdventChallenge.Infrastructure;
using AIAdventChallenge.Infrastructure.Models;
using AIAdventChallenge.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace AIAdventChallenge.Handlers.Day10TaskHandlers;

/// <summary>
/// День 10, подзадача 01.
/// Специальный handler для решения составления и обработки ТЗ.
/// Режим: Sliding Window.
/// </summary>
public static class Day10Subtask01Handler
{
    private const string AGENT_KEY = "day10-01-tech-design-agent";
    
    private const int SLIDING_WINDOW_SIZE = 10;

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
        using var llmClient = new LLMClient(baseUrl, apiKey, modelSettings, Day10TaskDescriptions.SYSTEM_PROMPT);

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
            .Skip(1) // Skip system prompt (0)
            .TakeLast(SLIDING_WINDOW_SIZE)
            .ToList();

        await SaveHistoryAsync(dbContext, lastMessagesHistory);

        return Results.Ok(new
        {
            UserMessage = message,
            AIMessage = chatResult.Content,
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
}