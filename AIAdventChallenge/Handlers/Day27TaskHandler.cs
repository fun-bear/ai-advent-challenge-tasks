using AIAdventChallenge.Infrastructure;

namespace AIAdventChallenge.Handlers;

/// <summary>
/// День 27.
/// Специальный handler для музыкального ассистента сервиса по подбору музыки.
/// Составляет плейлист из 3 музыкальных композиций, основываясь на текущих дате и времени.
/// В отличие от Day01 использует локальный Ollama через OpenAI-совместимый API.
/// </summary>
public static class Day27TaskHandler
{
    private const string OLLAMA_API_KEY = "ollama";
    private const string OLLAMA_MODEL_NAME = "llama3.1:8b";
    private const string OLLAMA_BASE_URL = "http://localhost:11434/v1/";

    private const string SYSTEM_PROMPT =
    """
    Ты — музыкальный ассистент сервиса по подбору музыки.

    Твоя задача: составлять плейлист из ровно 3 музыкальные композиции, основываясь на дате и времени, которые передаёт пользователь.

    При подборе учитывай:
    - Время суток (утро / день / вечер / ночь) — темп, энергетика и настроение треков
    - Месяц и время года — атмосфера и тематика музыки
    - Сочетаемость треков между собой внутри плейлиста

    Правила составления плейлиста:
    - Ровно 3 трека, не больше и не меньше
    - Указывай: номер, исполнитель — название трека
    - После плейлиста — одно короткое предложение (1–2 строки), объясняющее настроение подборки
    - После фразы про настроение добавляй отдельную строку с представлением модели в формате: Модель: [название модели]
    - Не используй маркдаун, звёздочки, заголовки — только чистый текст
    - Не задавай уточняющих вопросов, всегда возвращай готовый плейлист

    Формат ответа:
    1. Исполнитель — Название трека
    2. Исполнитель — Название трека
    3. Исполнитель — Название трека

    [Одна фраза про настроение плейлиста]
    Модель: [название LLM модели]
    """;

    public static async Task<string> HandleAsync()
    {
        var modelSettings = new AIModelSettings(OLLAMA_MODEL_NAME);
        using var llmClient = new LLMClient(OLLAMA_BASE_URL, OLLAMA_API_KEY, modelSettings, SYSTEM_PROMPT);
        var now = DateTimeOffset.Now;
        var userMessage = $"Текущая дата и время: {now:yyyy-MM-dd HH:mm}. Составь плейлист на основе этих данных.";

        var result = await llmClient.ChatAsync(userMessage);
        return result.Content;
    }
}