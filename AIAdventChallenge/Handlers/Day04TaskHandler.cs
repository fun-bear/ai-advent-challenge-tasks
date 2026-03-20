using AIAdventChallenge.Infrastructure;
using AIAdventChallenge.ViewModels;

namespace AIAdventChallenge.Handlers;

/// <summary>
/// День 04.
/// Специальный handler для ассистента сервиса литературного редактора.
/// Режим: четыре агента с разной температурой.
/// </summary>
public static class Day04TaskHandler
{
    private const string OLD_MODEL_NAME = "openai/gpt-4o";
    private const string SYSTEM_PROMPT_OLD_MODEL =
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

    private const string SYSTEM_PROMPT_NEW_MODEL =
    """
    Ты — литературный редактор. Для одного и того же названия книги придумай 3 разные короткие аннотации, каждая только на основе названия.

    Требования:
    - Полностью придумай содержание книги по названию
    - Вариант 1: максимально правдоподобный и жанрово очевидный
    - Вариант 2: оригинальный, но реалистичный
    - Вариант 3: самый необычный, но всё ещё убедительный
    - Каждая аннотация должна звучать как текст для реальной книги
    - Сохрани интригу
    - Не раскрывай финал
    - Длина каждой аннотации: 80–120 слов
    - Ответ: только 3 аннотации без пояснений
    """;

    private const decimal TEMPERATURE_ZERO = 0m;
    private const decimal TEMPERATURE_MEDIUM = 0.7m;
    private const decimal TEMPERATURE_HIGH = 1.2m;

    public static async Task<Day04TaskResult> HandleAsync(IConfiguration configuration)
    {
        var settings = configuration.GetSection("OpenAISettings");
        var apiKey = settings["ApiKey"] ?? throw new InvalidOperationException("OpenAISettings:ApiKey is missing.");
        var modelName = settings["ModelName"] ?? throw new InvalidOperationException("OpenAISettings:ModelName is missing.");
        var baseUrl = settings["BaseUrl"] ?? throw new InvalidOperationException("OpenAISettings:BaseUrl is missing.");

        using var agentZero = CreateOldAgent(baseUrl, apiKey, TEMPERATURE_ZERO);
        using var agentMedium = CreateOldAgent(baseUrl, apiKey, TEMPERATURE_MEDIUM);
        using var agentHigh = CreateOldAgent(baseUrl, apiKey, TEMPERATURE_HIGH);
        
        var modelSettings = new AIModelSettings(modelName);
        using var newAgent = new Agent(baseUrl, apiKey, modelSettings, SYSTEM_PROMPT_NEW_MODEL);

        var userMessage = "Название книги: Четверг обитания";

        var zeroTask = agentZero.ChatAsync(userMessage);
        var mediumTask = agentMedium.ChatAsync(userMessage);
        var highTask = agentHigh.ChatAsync(userMessage);
        var newModelTask = newAgent.ChatAsync(userMessage);

        await Task.WhenAll(zeroTask, mediumTask, highTask, newModelTask);

        var oldModel = new Day04TaskOldModel(
            Temperature0: await zeroTask,
            Temperature07: await mediumTask,
            Temperature12: await highTask);

        var newModel = new Day04TaskNewModel(
            Result: await newModelTask);

        return new Day04TaskResult(
            OldModel: oldModel,
            NewModel: newModel);
    }

    private static Agent CreateOldAgent(string baseUrl, string apiKey, decimal temperature)
    {
        var modelSettings = new AIModelSettings(OLD_MODEL_NAME, Temperature: temperature);
        return new Agent(baseUrl, apiKey, modelSettings, SYSTEM_PROMPT_OLD_MODEL);
    }
}