using AIAdventChallenge.Infrastructure;
using AIAdventChallenge.ViewModels;

namespace AIAdventChallenge.Handlers;

/// <summary>
/// День 04.
/// Специальный handler для ассистента сервиса литературного редактора.
/// Режим: три агента с разной температурой.
/// </summary>
public static class Day04TaskHandler
{
    private const string SYSTEM_PROMPT =
    """
    Ты — литературный редактор. Напиши короткую выдуманную аннотацию к книге только на основе ее названия.

    Требования:
    - Полностью придумай содержание книги по названию
    - Определи жанр и настроение самостоятельно
    - Сделай аннотацию правдоподобной, как для реальной книги
    - Сохрани интригу
    - Не раскрывай финал
    - Длина: 80–120 слов
    - Ответ: только аннотация
    """;

    private const decimal TEMPERATURE_ZERO = 0m;
    private const decimal TEMPERATURE_MEDIUM = 0.7m;
    private const decimal TEMPERATURE_HIGH = 1.2m;
    private const decimal TEMPERATURE_EXTRA = 2m;

    public static async Task<Day04TaskResult> HandleAsync(IConfiguration configuration)
    {
        var settings = configuration.GetSection("OpenAISettings");
        var apiKey = settings["ApiKey"] ?? throw new InvalidOperationException("OpenAISettings:ApiKey is missing.");
        var modelName = settings["ModelName"] ?? throw new InvalidOperationException("OpenAISettings:ModelName is missing.");
        var baseUrl = settings["BaseUrl"] ?? throw new InvalidOperationException("OpenAISettings:BaseUrl is missing.");

        using var agentZero = CreateAgent(baseUrl, apiKey, modelName, TEMPERATURE_ZERO);
        using var agentMedium = CreateAgent(baseUrl, apiKey, modelName, TEMPERATURE_MEDIUM);
        using var agentHigh = CreateAgent(baseUrl, apiKey, modelName, TEMPERATURE_HIGH);
        using var agentExtra = CreateAgent(baseUrl, apiKey, modelName, TEMPERATURE_EXTRA);

        var userMessage = "Название книги: Имя, забытое в поезде";

        var zeroTask = agentZero.ChatAsync(userMessage);
        var mediumTask = agentMedium.ChatAsync(userMessage);
        var highTask = agentHigh.ChatAsync(userMessage);
        var extraTask = agentExtra.ChatAsync(userMessage);

        await Task.WhenAll(zeroTask, mediumTask, highTask, extraTask);

        return new Day04TaskResult(
            Temperature0: await zeroTask,
            Temperature07: await mediumTask,
            Temperature12: await highTask,
            Temperature2: await extraTask);
    }

    private static Agent CreateAgent(string baseUrl, string apiKey, string modelName, decimal temperature)
    {
        var modelSettings = new AIModelSettings(modelName, Temperature: temperature);
        return new Agent(baseUrl, apiKey, modelSettings, SYSTEM_PROMPT);
    }
}