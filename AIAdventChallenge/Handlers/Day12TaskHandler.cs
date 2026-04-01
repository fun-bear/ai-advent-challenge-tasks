using AIAdventChallenge.Infrastructure;
using AIAdventChallenge.Infrastructure.Models;
using AIAdventChallenge.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace AIAdventChallenge.Handlers;

/// <summary>
/// День 12.
/// Чат-агент, который подстраивается под профиль пользователя.
/// </summary>
public static class Day12TaskHandler
{
    private const string SYSTEM_PROMPT_TEMPLATE =
    """
    Ты — умная голосовая колонка.
    Ты общаешься с пользователем на самые разные темы и отвечаешь на разные вопросы.
    Подстраивайся под пользователя на основе его профиля.
    Отвечай естественно, дружелюбно и на русском языке.
    Учитывай стиль общения, желаемую краткость и настроение пользователя.
    Профиль пользователя: {0}
    """;

    private const string AGENT_KEY_PREFIX = "day12-chat-agent-";
    private const int PROFILE_REBUILD_MESSAGES_THRESHOLD = 4;

    private const string DEFAULT_PROFILE =
    """
    стиль общения: нейтральный, вежливый
    краткость: средняя
    настроение: спокойное
    """;

    private const string PROFILE_AGENT_SYSTEM_PROMPT =
    """
    Ты — AI-агент, который обновляет профиль пользователя для умной колонки.
    Тебе передаются:
    1) текущий профиль пользователя;
    2) последние сообщения пользователя.

    Задача:
    - Вернуть НОВЫЙ профиль, если в последних сообщениях появились новые важные данные
      по трем аспектам: стиль общения, краткость, настроение.
    - Вернуть ПРЕЖНИЙ профиль без изменений, если новых значимых данных нет.

    Формат профиля (строгий, 3 строки):
    стиль общения: ...
    краткость: ...
    настроение: ...

    Правила:
    - Отвечай только текстом профиля, без markdown, JSON и пояснений.
    - Профиль должен быть кратким, конкретным и пригодным для персонализации ответов колонки.
    - Язык профиля: русский.
    """;

    private const int SLIDING_WINDOW_SIZE = 6;
    private static readonly Dictionary<int, UserChatProfileState> userChatProfiles = [];

    public static async Task<IResult> HandleAsync(
        IConfiguration configuration,
        AppDbContext dbContext,
        int? profileId,
        string? message)
    {
        if (!profileId.HasValue)
        {
            return Results.BadRequest("Query-параметр 'profileId' обязателен.");
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return Results.BadRequest("Query-параметр 'message' обязателен.");
        }

        var settings = configuration.GetSection("OpenAISettings");
        var apiKey = settings["ApiKey"] ?? throw new InvalidOperationException("OpenAISettings:ApiKey is missing.");
        var modelName = settings["ModelName"] ?? throw new InvalidOperationException("OpenAISettings:ModelName is missing.");
        var baseUrl = settings["BaseUrl"] ?? throw new InvalidOperationException("OpenAISettings:BaseUrl is missing.");
        var modelSettings = new AIModelSettings(modelName);

        var agentKey = $"{AGENT_KEY_PREFIX}{profileId.Value}";
        var persistedHistory = await LoadHistoryAsync(dbContext, agentKey);

        var profileState = GetOrCreateProfileState(profileId.Value);
        var profileDescription = profileState.ProfileDescription;

        var systemPrompt = string.Format(SYSTEM_PROMPT_TEMPLATE, profileDescription);

        using var agent = new Agent(baseUrl, apiKey, modelSettings, systemPrompt);
        if (persistedHistory.Count > 0)
        {
            agent.AddHistory(persistedHistory);
        }

        var result = await agent.ChatAsync(message);

        var historyToSave = agent.ExportHistory()
            .Skip(1) // Skip system prompt (0)
            .TakeLast(SLIDING_WINDOW_SIZE)
            .ToList();

        await SaveHistoryAsync(dbContext, agentKey, historyToSave);

        profileState.MessagesSentWithCurrentProfile++;

        if (profileState.MessagesSentWithCurrentProfile >= PROFILE_REBUILD_MESSAGES_THRESHOLD)
        {
            var userRecentMessages = GetRecentUserMessages(historyToSave);

            var updatedProfile = await BuildOrKeepProfileAsync(
                baseUrl,
                apiKey,
                modelSettings,
                profileState.ProfileDescription,
                userRecentMessages);

            profileState.ProfileDescription = updatedProfile;
            profileState.MessagesSentWithCurrentProfile = 0;
        }

        userChatProfiles[profileId.Value] = profileState;

        return Results.Ok(new
        {
            ProfileId = profileId.Value,
            ProfileDescription = profileState.ProfileDescription,
            MessagesSentWithCurrentProfile = profileState.MessagesSentWithCurrentProfile,
            UserMessage = message,
            AIMessage = result.Content,
            HistoryCount = historyToSave.Count,
            TotalTokens = result.TotalTokens
        });
    }

    private static async Task<string> BuildOrKeepProfileAsync(
        string baseUrl,
        string apiKey,
        AIModelSettings modelSettings,
        string currentProfile,
        IReadOnlyList<string> recentUserMessages)
    {
        using var profileAgent = new Agent(baseUrl, apiKey, modelSettings, PROFILE_AGENT_SYSTEM_PROMPT);

        var request =
            $"Текущий профиль пользователя:\n{currentProfile}\n\n" +
            $"Последние сообщения пользователя:\n{string.Join("\n", recentUserMessages.Select((m, i) => $"{i + 1}. {m}"))}\n\n" +
            "Сформируй итоговый профиль по правилам.";

        var response = await profileAgent.ChatAsync(request);
        var profileText = response.Content.Trim();

        return string.IsNullOrWhiteSpace(profileText)
            ? currentProfile
            : profileText;
    }

    private static UserChatProfileState GetOrCreateProfileState(int profileId)
    {
        if (userChatProfiles.TryGetValue(profileId, out var state))
        {
            return state;
        }

        state = new UserChatProfileState
        {
            ProfileDescription = DEFAULT_PROFILE,
            MessagesSentWithCurrentProfile = 0
        };

        userChatProfiles[profileId] = state;
        return state;
    }

    private static IReadOnlyList<string> GetRecentUserMessages(
        IReadOnlyList<ChatMessage> history)
    {
        var messages = history
            .Where(x => x.Role == "user")
            .Select(x => x.Content)
            .ToList();

        return messages;
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

    private sealed class UserChatProfileState
    {
        public required string ProfileDescription { get; set; }

        public int MessagesSentWithCurrentProfile { get; set; }
    }
}