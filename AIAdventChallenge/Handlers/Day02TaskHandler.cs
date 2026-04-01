using AIAdventChallenge.Infrastructure;
using AIAdventChallenge.ViewModels;

namespace AIAdventChallenge.Handlers;

/// <summary>
/// День 02.
/// Специальный handler для ассистента сервиса по подбору одежды.
/// Составляет рекомендации по тому, что надеть, основываясь на текущих дате и времени.
/// Использует двух агентов: с ограничениями и без.
/// </summary>
public static class Day02TaskHandler
{
    private const string SYSTEM_PROMPT_WITH_RESTRICTIONS =
    """
    Ты — ассистент сервиса по подбору одежды.

    Твоя задача: на основе даты и времени, которые передаёт пользователь, рекомендовать, что надеть на улицу прямо сейчас.

    При подборе учитывай:
    - Время суток (утро / день / вечер / ночь) — ощущение прохлады, тип активности и уместность образа
    - Месяц и время года — сезонность одежды
    - Практичность и сочетаемость всех вещей между собой
    - Базовый городской сценарий использования

    Что нужно выбрать:
    - Верхнюю одежду
    - Головной убор
    - Обувь

    Правила подбора:
    - Всегда возвращай готовую рекомендацию, не задавай уточняющих вопросов
    - Если какой-то предмет не нужен, пиши "не нужен"
    - Не добавляй никаких пояснений вне заданного формата
    - Ответ должен быть кратким, конкретным и прикладным

    Ответ должен содержать 1 абзац, без переводов строк.

    Строгий формат ответа:
    Верхняя одежда: [вариант]. Головной убор: [вариант или "не нужен"]. Обувь: [вариант]. [Одно короткое предложение про общий характер рекомендации]
    Больше ничего не выводи.
    """;

    private const string SYSTEM_PROMPT_WITHOUT_RESTRICTIONS =
    """
    Ты — ассистент сервиса по подбору одежды.

    Твоя задача: на основе даты и времени, которые передаёт пользователь, рекомендовать, что надеть на улицу прямо сейчас.

    При подборе учитывай:
    - Время суток (утро / день / вечер / ночь) — ощущение прохлады, тип активности и уместность образа
    - Месяц и время года — сезонность одежды
    - Общую практичность сочетания вещей между собой
    - Базовый городской сценарий использования — одежда должна быть уместной, удобной и логичной для выхода на улицу

    Что нужно выбрать:
    - Верхнюю одежду
    - Головной убор
    - Обувь

    Правила подбора:
    - Всегда возвращай готовую рекомендацию, не задавай уточняющих вопросов
    - Если по сезону какой-то предмет не нужен, так и укажи
    - Рекомендация должна быть конкретной, понятной и краткой
    - Все выбранные вещи должны сочетаться между собой по сезону и назначению

    Ответ должен содержать 1 абзац, без переводов строк.

    Нужны итоговые рекомендации и одно короткое предложение на 1–2 строки, объясняющее общий характер рекомендации.
    """;

    private const int MAX_TOKENS = 150;
    private const decimal TEMPERATURE = 0.1m;

    public static async Task<Day02TaskResult> HandleAsync(IConfiguration configuration)
    {
        var settings = configuration.GetSection("OpenAISettings");
        var apiKey = settings["ApiKey"] ?? throw new InvalidOperationException("OpenAISettings:ApiKey is missing.");
        var modelName = settings["ModelName"] ?? throw new InvalidOperationException("OpenAISettings:ModelName is missing.");
        var baseUrl = settings["BaseUrl"] ?? throw new InvalidOperationException("OpenAISettings:BaseUrl is missing.");

        using var agentWithoutRestrictions = CreateAgentWithoutRestrictions(baseUrl, apiKey, modelName);
        using var agentWithRestrictions = CreateAgentWithRestrictions(baseUrl, apiKey, modelName);

        var now = DateTimeOffset.Now;
        var userMessage = $"Текущая дата и время: {now:yyyy-MM-dd HH:mm}. Составь рекомендации по выбору одежды на основе этих данных.";
        var withoutRestrictionsTask = agentWithoutRestrictions.ChatAsync(userMessage);

        var withRestrictionsTask = agentWithRestrictions.ChatAsync(userMessage);

        await Task.WhenAll(withoutRestrictionsTask, withRestrictionsTask);

        var withoutRestrictionsResult = await withoutRestrictionsTask;
        var withRestrictionsResult = await withRestrictionsTask;

        return new Day02TaskResult(
            WithoutRestrictions: withoutRestrictionsResult.Content,
            WithRestrictions: withRestrictionsResult.Content);
    }
    
    private static LLMClient CreateAgentWithoutRestrictions(string baseUrl, string apiKey, string modelName)
    {
        var modelSettings = new AIModelSettings(modelName);
        return new LLMClient(baseUrl, apiKey, modelSettings, SYSTEM_PROMPT_WITHOUT_RESTRICTIONS);
    }

    private static LLMClient CreateAgentWithRestrictions(string baseUrl, string apiKey, string modelName)
    {
        var modelSettings = new AIModelSettings(modelName, MAX_TOKENS, TEMPERATURE);
        return new LLMClient(baseUrl, apiKey, modelSettings, SYSTEM_PROMPT_WITH_RESTRICTIONS);
    }
}