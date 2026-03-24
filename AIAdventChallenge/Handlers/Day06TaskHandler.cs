using AIAdventChallenge.Infrastructure;

namespace AIAdventChallenge.Handlers;

/// <summary>
/// День 06.
/// Специальный handler для агента, накапливающего покупки в магазине.
/// Каждый вызов добавляет покупку и возвращает summary со списком покупок.
/// </summary>
public static class Day06TaskHandler
{
    private const string SYSTEM_PROMPT =
    """
    Ты — агент, который накапливает покупки в магазине.

    Правила:
    - Каждый раз, когда пользователь просит добавить покупку, добавляй её в общий список.
    - Всегда отвечай summary со списком всех покупок, накопленных до текущего момента.
    - Summary должен быть кратким и содержать только список покупок.
    """;

    public static async Task<string> HandleAsync(IConfiguration configuration)
    {
        var settings = configuration.GetSection("OpenAISettings");
        var apiKey = settings["ApiKey"] ?? throw new InvalidOperationException("OpenAISettings:ApiKey is missing.");
        var modelName = settings["ModelName"] ?? throw new InvalidOperationException("OpenAISettings:ModelName is missing.");
        var baseUrl = settings["BaseUrl"] ?? throw new InvalidOperationException("OpenAISettings:BaseUrl is missing.");

        var modelSettings = new AIModelSettings(modelName);
        using var agent = new Agent(baseUrl, apiKey, modelSettings, SYSTEM_PROMPT);

        await agent.ChatAsync("Добавь покупку: молоко.");
        await agent.ChatAsync("Добавь покупку: хлеб.");
        var result = await agent.ChatAsync("Добавь покупку: яблоки.");

        return result.Content;
    }
}