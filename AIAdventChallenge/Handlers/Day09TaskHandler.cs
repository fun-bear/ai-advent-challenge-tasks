using AIAdventChallenge.Infrastructure;
using AIAdventChallenge.Infrastructure.Models;
using AIAdventChallenge.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace AIAdventChallenge.Handlers;

/// <summary>
/// День 09.
/// Агент с компрессией контекста:
/// - последние N сообщений храним как есть;
/// - более старые сообщения сворачиваем в summary блоками.
/// </summary>
public static class Day09TaskHandler
{
    private const string SYSTEM_PROMPT =
    """
    Ты — агент, который поддерживает разговор с пользователем.
    Отвечай по существу, дружелюбно и понятно.
    """;

    private const string RECENT_HISTORY_AGENT_KEY = "day09-chat-agent-recent";
    private const int KEEP_LAST_MESSAGES = 10;
    private const string SUMMARY_PROMPT =
    "Ты — агент-суммаризатор. Максимально кратко и лаконично сожми переданный диалог, сохранив только ключевые факты, договоренности и контекст.";

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
        using var llmClient = new LLMClient(baseUrl, apiKey, modelSettings, SYSTEM_PROMPT);

        var summaryEntry = await dbContext.AgentSummaryEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AgentKey == RECENT_HISTORY_AGENT_KEY);

        var summary = summaryEntry?.Summary;
        await llmClient.ChatAsync($"Краткое содержание предыдущего разговора: {summary ?? "еще нет"}");

        var persistedHistory = await LoadHistoryAsync(dbContext, RECENT_HISTORY_AGENT_KEY);
        if (persistedHistory.Count > 0)
        {
            llmClient.AddHistory(persistedHistory);
        }

        var chatResult = await llmClient.ChatAsync(message);

        var lastMessagesHistory = llmClient.ExportHistory()
            .Where((_, index) => index > 2) // Skip system prompt (0) and summary chat messages (1, 2)
            .ToList();

        if (lastMessagesHistory.Count >= KEEP_LAST_MESSAGES)
        {
            var combinedSummaryParts = new List<string>();

            if (!string.IsNullOrWhiteSpace(summary))
            {
                combinedSummaryParts.Add(summary);
            }

            combinedSummaryParts.AddRange(lastMessagesHistory.Select(x => x.Content));
            var combinedContent = string.Join(Environment.NewLine, combinedSummaryParts);
            var compressedSummary = await CompressSummaryAsync(baseUrl, apiKey, modelName, combinedContent);

            await SaveSummaryAsync(dbContext, compressedSummary);
            await SaveHistoryAsync(dbContext, Array.Empty<ChatMessage>());

            summary = compressedSummary;
            lastMessagesHistory = [];
        }

        await SaveHistoryAsync(dbContext, lastMessagesHistory);

        return Results.Ok(new
        {
            UserMessage = message,
            AIMessage = chatResult.Content,
            Summary = summary,
            LastMessagesCount = lastMessagesHistory.Count,
            TotalTokens = chatResult.TotalTokens
        });
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

    private static async Task SaveHistoryAsync(AppDbContext dbContext, IReadOnlyList<ChatMessage> history)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        await dbContext.AgentHistoryEntries
            .Where(x => x.AgentKey == RECENT_HISTORY_AGENT_KEY)
            .ExecuteDeleteAsync();

        for (var i = 0; i < history.Count; i++)
        {
            var message = history[i];

            dbContext.AgentHistoryEntries.Add(new AgentHistoryEntry
            {
                AgentKey = RECENT_HISTORY_AGENT_KEY,
                SortOrder = i,
                Role = message.Role,
                Content = message.Content
            });
        }

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static async Task SaveSummaryAsync(AppDbContext dbContext, string summary)
    {
        var entry = await dbContext.AgentSummaryEntries
            .FirstOrDefaultAsync(x => x.AgentKey == RECENT_HISTORY_AGENT_KEY);

        if (entry is null)
        {
            dbContext.AgentSummaryEntries.Add(new AgentSummaryEntry
            {
                AgentKey = RECENT_HISTORY_AGENT_KEY,
                Summary = summary
            });
        }
        else
        {
            entry.Summary = summary;
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task<string> CompressSummaryAsync(string baseUrl, string apiKey, string modelName, string combinedContent)
    {
        using var summaryAgent = new LLMClient(baseUrl, apiKey, new AIModelSettings(modelName), SUMMARY_PROMPT);
        var summaryResult = await summaryAgent.ChatAsync(combinedContent);
        return summaryResult.Content;
    }
}