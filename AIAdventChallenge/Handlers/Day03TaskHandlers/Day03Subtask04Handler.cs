using AIAdventChallenge.Infrastructure;

namespace AIAdventChallenge.Handlers.Day03TaskHandlers;

/// <summary>
/// День 03, подзадача 04.
/// Специальный handler для решения аналитической задачи по открытию бизнеса.
/// Режим: группа экспертов в промпте.
/// </summary>
public static class Day03Subtask04Handler
{
    private const string SYSTEM_PROMPT =
    """
    Ты — ассистент сервиса по бизнес-советам в сфере питания.

    На вопрос пользователя тебе нужно провести анализ от каждого эксперта:
    - шеф-повар;
    - банкир;
    - маркетолог;
    - агент по недвижимости.

    Твой вывод должен содержать анализ от каждого из экспертов, а также одну итоговую рекомендацию на основе мнения экспертов.
    """;

    public static async Task<string> HandleAsync(IConfiguration configuration)
    {
        var settings = configuration.GetSection("OpenAISettings");
        var apiKey = settings["ApiKey"] ?? throw new InvalidOperationException("OpenAISettings:ApiKey is missing.");
        var modelName = settings["ModelName"] ?? throw new InvalidOperationException("OpenAISettings:ModelName is missing.");
        var baseUrl = settings["BaseUrl"] ?? throw new InvalidOperationException("OpenAISettings:BaseUrl is missing.");

        var modelSettings = new AIModelSettings(modelName);
        using var agent = new Agent(baseUrl, apiKey, modelSettings, SYSTEM_PROMPT);
        var userMessage = Day03TaskDescriptions.TASK_DESCRIPTION;

        var result = await agent.ChatAsync(userMessage);
        return result.Content;
    }
}