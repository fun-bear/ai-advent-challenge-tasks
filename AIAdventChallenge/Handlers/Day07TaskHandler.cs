using AIAdventChallenge.Infrastructure;
using AIAdventChallenge.Infrastructure.Models;
using AIAdventChallenge.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace AIAdventChallenge.Handlers;

/// <summary>
/// День 07.
/// Специальный handler для агента, который сохраняет историю в SQLite,
/// чтобы она переживала перезапуск сервера.
/// </summary>
public static class Day07TaskHandler
{
    private const string SYSTEM_PROMPT =
    """
    Ты — агент, который накапливает покупки в магазине.

    Правила:
    - Каждый раз, когда пользователь просит добавить покупку, добавляй её в общий список.
    - Если покупка уже есть в списке, то увеличивай количество на 1.
    - Количество единиц каждой покупки выводится рядом в круглых скобках (если количество > 1).
    - Всегда отвечай summary со списком всех покупок, накопленных до текущего момента.
    - Summary должен быть кратким и содержать только список покупок.
    """;

    private const string AGENT_KEY = "day07-shopping-agent";

    public static async Task<IResult> HandleAsync(
        IConfiguration configuration,
        AppDbContext dbContext,
        string? product)
    {
        if (string.IsNullOrWhiteSpace(product))
        {
            return Results.BadRequest("Query-параметр 'product' обязателен.");
        }

        var settings = configuration.GetSection("OpenAISettings");
        var apiKey = settings["ApiKey"] ?? throw new InvalidOperationException("OpenAISettings:ApiKey is missing.");
        var modelName = settings["ModelName"] ?? throw new InvalidOperationException("OpenAISettings:ModelName is missing.");
        var baseUrl = settings["BaseUrl"] ?? throw new InvalidOperationException("OpenAISettings:BaseUrl is missing.");

        var modelSettings = new AIModelSettings(modelName);
        using var agent = new Agent(baseUrl, apiKey, modelSettings, SYSTEM_PROMPT);

        var persistedHistory = await LoadHistoryAsync(dbContext);
        if (persistedHistory.Count > 0)
        {
            agent.ImportHistory(persistedHistory);
        }

        var result = await agent.ChatAsync($"Добавь покупку: {product}.");

        await SaveHistoryAsync(dbContext, agent.ExportHistory());

        return Results.Ok(result.Content);
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