using AIAdventChallenge.Infrastructure;
using AIAdventChallenge.Infrastructure.Models;
using AIAdventChallenge.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace AIAdventChallenge.Handlers.Day10TaskHandlers;

/// <summary>
/// День 10, подзадача 03.
/// Специальный handler для решения составления и обработки ТЗ.
/// Режим: Branching.
/// </summary>
public static class Day10Subtask03Handler
{
    private const string AGENT_KEY_BRANCH_1 = "day10-03-tech-design-agent-branch-1";
    private const string AGENT_KEY_BRANCH_2 = "day10-03-tech-design-agent-branch-2";
    
    public static async Task<IResult> HandleAsync(
        IConfiguration configuration,
        AppDbContext dbContext,
        string? message,
        int? branch)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return Results.BadRequest("Query-параметр 'message' обязателен.");
        }

        if (branch is null)
        {
            return Results.BadRequest("Query-параметр 'branch' обязателен.");
        }

        if (branch is not (1 or 2))
        {
            return Results.BadRequest("Query-параметр 'branch' должен быть равен 1 или 2.");
        }

        var settings = configuration.GetSection("OpenAISettings");
        var apiKey = settings["ApiKey"] ?? throw new InvalidOperationException("OpenAISettings:ApiKey is missing.");
        var modelName = settings["ModelName"] ?? throw new InvalidOperationException("OpenAISettings:ModelName is missing.");
        var baseUrl = settings["BaseUrl"] ?? throw new InvalidOperationException("OpenAISettings:BaseUrl is missing.");

        var modelSettings = new AIModelSettings(modelName);
        using var llmClient = new LLMClient(baseUrl, apiKey, modelSettings, Day10TaskDescriptions.SYSTEM_PROMPT);

        var branchAgentKey = branch == 1 ? AGENT_KEY_BRANCH_1 : AGENT_KEY_BRANCH_2;

        var persistedHistory = await LoadHistoryAsync(dbContext, branchAgentKey);
        if (persistedHistory.Count == 0) // Init branch agent
        {
            persistedHistory = Day10TaskDescriptions.INITIAL_MESSAGES
                .Select(messageText => new ChatMessage("user", messageText))
                .ToList();
        }

        llmClient.AddHistory(persistedHistory);

        var chatResult = await llmClient.ChatAsync(message);

        var lastMessagesHistory = llmClient.ExportHistory()
            .Skip(1) // Skip system prompt (0)
            .ToList();

        await SaveHistoryAsync(dbContext, branchAgentKey, lastMessagesHistory);

        return Results.Ok(new
        {
            UserMessage = message,
            AIMessage = chatResult.Content,
            Branch = branch,
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