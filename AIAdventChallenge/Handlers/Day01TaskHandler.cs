using AIAdventChallenge.Infrastructure;

namespace AIAdventChallenge.Handlers;

/// <summary>
/// День 01.
/// Специальный handler для музыкального ассистента сервиса по подбору музыки.
/// Составляет плейлист из 10 музыкальных композиций, основываясь на текущих дате и времени.
/// </summary>
public static class Day01TaskHandler
{
    private const string SYSTEM_PROMPT =
    """
    Ты — музыкальный ассистент сервиса по подбору музыки.

    Твоя задача: составлять плейлист из ровно 10 музыкальных композиций, основываясь на дате и времени, которые передаёт пользователь.

    При подборе учитывай:
    - Время суток (утро / день / вечер / ночь) — темп, энергетика и настроение треков
    - Месяц и время года — атмосфера и тематика музыки
    - Сочетаемость треков между собой внутри плейлиста

    Правила составления плейлиста:
    - Ровно 10 треков, не больше и не меньше
    - Указывай: номер, исполнитель — название трека
    - После плейлиста — одно короткое предложение (1–2 строки), объясняющее настроение подборки
    - Не используй маркдаун, звёздочки, заголовки — только чистый текст
    - Не задавай уточняющих вопросов, всегда возвращай готовый плейлист

    Формат ответа:
    1. Исполнитель — Название трека
    2. Исполнитель — Название трека
    ...
    10. Исполнитель — Название трека

    [Одна фраза про настроение плейлиста]
    """;

    public static async Task<string> HandleAsync(IConfiguration configuration)
    {
        var settings = configuration.GetSection("OpenAISettings");
        var apiKey = settings["ApiKey"] ?? throw new InvalidOperationException("OpenAISettings:ApiKey is missing.");
        var model = settings["Model"] ?? throw new InvalidOperationException("OpenAISettings:Model is missing.");
        var baseUrl = settings["BaseUrl"] ?? throw new InvalidOperationException("OpenAISettings:BaseUrl is missing.");
        
        using var agent = new Agent(baseUrl, apiKey, model, SYSTEM_PROMPT);
        var now = DateTimeOffset.Now;
        var userMessage = $"Текущая дата и время: {now:yyyy-MM-dd HH:mm}. Составь плейлист на основе этих данных.";

        return await agent.ChatAsync(userMessage);
    }
}