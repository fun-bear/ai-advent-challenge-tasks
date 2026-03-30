using AIAdventChallenge.Infrastructure;

namespace AIAdventChallenge.Handlers;

/// <summary>
/// День 11.
/// Базовый handler для диалога с агентом.
/// </summary>
public static class Day11TaskHandler
{
    private const string SYSTEM_PROMPT =
    """
    Ты — AI-ассистент для обучения языку программирования C#.
    Ты работаешь с **явной моделью памяти**, состоящей из трех слоев:

    ### 1. Краткосрочная память (Short-Term Memory)
    - Хранит только **последние N сообщений диалога** (пользователя и твои ответы).
    - Старые сообщения удаляются при переполнении (FIFO).
    - Влияние: Определяет контекст текущего диалога.

    ### 2. Рабочая память (Working Memory)
    - Хранит **данные текущей задачи** (ключ-значение).
    - Примеры ключей: `цель`, `ограничение`.
    - Очищается при завершении задачи (по команде "Сбросить задачу").
    - Влияние: Позволяет следить за прогрессом обучения.

    ### 3. Долговременная память (Long-Term Memory)
    - Хранит **профиль пользователя и знания**.
    - **Профиль**: Язык, уровень (начинающий/продвинутый), предпочтения.
    - **Знания**: Полезные ресурсы, объяснения, ссылки на документацию.
    - Сохраняется между сессиями (если поддерживается системой).
    - Влияние: Персонализирует обучение.

    ### Правила работы:
    1. **Сохранение данных**:
    - Каждое сообщение пользователя автоматически сохраняется в краткосрочную память.
    - Если сообщение содержит ключевые слова (например, "цель:", "ограничение:"), данные сохраняются в рабочую память.
    - Если сообщение содержит "запомни:", данные сохраняются в долговременную память.

    2. **Использование данных**:
    - Ответы должны учитывать данные из всех трех слоев памяти.
    - Если в рабочей памяти есть цель, отвечай с учетом этой цели.
    - Если в долговременной памяти есть информация о пользователе, используй её для персонализации.

    3. **Формат ответа**:
    - Отвечай кратко, по делу и на русском языке.
    - Если нужно, упомяни, какие данные из памяти использовались (для демонстрации).

    ### Пример диалога:
    **Пользователь**: "Я хочу научиться создавать веб-приложения на C#."
    **Ты**: "Отлично! Цель: создание веб-приложений. Я помогу тебе с основами ASP.NET. (Данные из рабочей памяти: цель = создание веб-приложений)"

    **Пользователь**: "Запомни, что я люблю визуальные примеры."
    **Ты**: "Я запомнил, что ты любишь визуальные примеры. (Данные из долговременной памяти: предпочтения = визуальные примеры)"
    """;

    private const int SHORT_TERM_MEMORY_LIMIT = 4;

    // Слои памяти
    private static Queue<string> shortTermMemory = new Queue<string>();
    private static Dictionary<string, string> workingMemory = new Dictionary<string, string>();
    private static Dictionary<string, string> longTermMemory = new Dictionary<string, string>();

    public static async Task<IResult> HandleAsync(
        IConfiguration configuration,
        string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return Results.BadRequest("Query-параметр 'message' обязателен.");
        }

        var settings = configuration.GetSection("OpenAISettings");
        var apiKey = settings["ApiKey"] ?? throw new InvalidOperationException("OpenAISettings:ApiKey is missing.");
        var modelName = settings["ModelName"] ?? throw new InvalidOperationException("OpenAISettings:ModelName is missing.");
        var baseUrl = settings["BaseUrl"] ?? throw new InvalidOperationException("OpenAISettings:BaseUrl is missing.");

        var modelSettings = new AIModelSettings(modelName);
        using var agent = new Agent(baseUrl, apiKey, modelSettings, SYSTEM_PROMPT);

        var userMessage = message.Trim().ToLower();

        // 1. Сохраняем новое сообщение в краткосрочную память
        AddToShortTermMemory(userMessage);

        // 2. Извлекаем данные в рабочую память
        if (userMessage.Contains("цель:"))
        {
            workingMemory["цель"] = userMessage.Replace("цель:", "").Trim();
        }

        if (userMessage.Contains("ограничение:"))
        {
            workingMemory["ограничение"] = userMessage.Replace("ограничение:", "").Trim();
        }

        if (userMessage.Contains("сбросить задачу"))
        {
            workingMemory.Clear();
        }

        // 3. Сохраняем в долговременную память
        if (userMessage.Contains("запомни:"))
        {
            longTermMemory[Guid.NewGuid().ToString()] = userMessage.Replace("запомни:", "").Trim();
        }

        // 4. Формируем ответ с учетом памяти
        string aiRequest = $"Сформируй ответ на основе памяти:\n";
        aiRequest += $"- Краткосрочная: {string.Join(", ", shortTermMemory)}\n";
        aiRequest += $"- Рабочая цель: {workingMemory.GetValueOrDefault("цель", "не задана")}\n";
        aiRequest += $"- Рабочее ограничение: {workingMemory.GetValueOrDefault("ограничение", "не задано")}\n";
        aiRequest += $"- Долговременная: {string.Join(", ", longTermMemory.Values)}";

        var result = await agent.ChatAsync(aiRequest);

        // 5. Сохраняем ответ в краткосрочную память
        AddToShortTermMemory(result.Content);

        return Results.Ok(new
        {
            UserMessage = message,
            AIMessage = result.Content,
            TotalTokens = result.TotalTokens
        });
    }

    private static void AddToShortTermMemory(string message)
    {
        shortTermMemory.Enqueue(message);

        while (shortTermMemory.Count > SHORT_TERM_MEMORY_LIMIT)
        {
            shortTermMemory.Dequeue();
        }
    }
}