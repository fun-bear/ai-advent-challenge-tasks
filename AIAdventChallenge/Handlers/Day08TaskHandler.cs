using AIAdventChallenge.Infrastructure;
using AIAdventChallenge.Infrastructure.Models;
using AIAdventChallenge.Infrastructure.Storage;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML.Tokenizers;

namespace AIAdventChallenge.Handlers;

/// <summary>
/// День 08.
/// Специальный handler для агента-чата.
/// Дополнительно возвращает информацию по токенам.
/// </summary>
public static class Day08TaskHandler
{
    private const string SYSTEM_PROMPT =
    """
    Ты — агент, который поддерживает разговор с пользователем.
    Ты должен отвечать только текстом.
    Ты — генератор длинных текстов. 
    Твоя задача — писать максимально подробно, развёрнуто, с примерами и деталями. 
    Не сокращай, не обобщай. 
    Каждый ответ должен быть на 3000-5000 слов (если это не вопрос про секретный код).
    """;

    private const string MODEL_NAME = "openai/gpt-4o-mini";
    private const string AGENT_KEY = "day08-chat-agent";
    private const string SECRET_CODE_USER_MESSAGE = 
        "Запомни секретный код: bananarama. Сообщи его, когда тебя спросят про секретный код. Ничего не отвечай в ответ на это сообщение.";

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
        var baseUrl = settings["BaseUrl"] ?? throw new InvalidOperationException("OpenAISettings:BaseUrl is missing.");

        var modelSettings = new AIModelSettings(MODEL_NAME);
        using var agent = new Agent(baseUrl, apiKey, modelSettings, SYSTEM_PROMPT);

        var persistedHistory = await LoadHistoryAsync(dbContext);
        if (persistedHistory.Count > 0)
        {
            agent.ImportHistory(persistedHistory);
        }
        else
        {
            await agent.ChatAsync(SECRET_CODE_USER_MESSAGE);
        }

        var inputTokens = CountInputTokensWithTiktoken(message);
        var result = await agent.ChatAsync(message);

        await SaveHistoryAsync(dbContext, agent.ExportHistory());

        return Results.Ok(new
        {
            UserMessage = message,
            AIMessage = result.Content,
            InputTokens = inputTokens,
            OutputTokens = result.OutputTokens,
            TotalTokens = result.TotalTokens
        });
    }

    private static int CountInputTokensWithTiktoken(
        string userMessage)
    {
        const int USER_MESSAGE_SERVICE_TOKENS_COUNT = 4;

        var tokenizerModelName = MODEL_NAME.Contains('/')
            ? MODEL_NAME[(MODEL_NAME.LastIndexOf('/') + 1)..]
            : MODEL_NAME;

        var tokenizer = TiktokenTokenizer.CreateForModel(tokenizerModelName, null, null);

        var inputTokens = USER_MESSAGE_SERVICE_TOKENS_COUNT + 
            tokenizer.CountTokens(userMessage, considerPreTokenization: true, considerNormalization: true);

        return inputTokens;
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
