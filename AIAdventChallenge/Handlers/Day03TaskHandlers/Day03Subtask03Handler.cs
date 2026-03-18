using AIAdventChallenge.Infrastructure;

namespace AIAdventChallenge.Handlers.Day03TaskHandlers;

/// <summary>
/// День 03, подзадача 03.
/// Специальный handler для решения аналитической задачи по открытию бизнеса.
/// Режим: сначала просим AI составить промпт, потом используем его в новой сессии.
/// </summary>
public static class Day03Subtask03Handler
{
    private const string PROMPT_MODEL = "openai/gpt-5.1-chat";
    private const string TASK_MODEL = "openai/gpt-5.3-chat";

    public static async Task<string> HandleAsync(IConfiguration configuration)
    {
        var settings = configuration.GetSection("OpenAISettings");
        var apiKey = settings["ApiKey"] ?? throw new InvalidOperationException("OpenAISettings:ApiKey is missing.");
        var baseUrl = settings["BaseUrl"] ?? throw new InvalidOperationException("OpenAISettings:BaseUrl is missing.");

        var promptModelSettings = new AIModelSettings(PROMPT_MODEL);
        using var promptAgent = new Agent(baseUrl, apiKey, promptModelSettings);
        var promptUserMessage = 
        $"""
        Нам дана такая задача (находится в блоке между *** и ***):
        ***
        {Day03TaskDescriptions.TASK_DESCRIPTION}
        ***

        Составь мне промпт для AI для успешного решения данной задачи. Промпт предназначается для AI, укажи дополнительные детали, на что нужно обратить внимание.
        В промпте зафиксируй, какие метрики лучше изучить, за какие годы взять статистику, какие параметры важны больше, чем другие.
        Составленный тобой промпт должен содержать полностью условие задачи (он будет сразу целиком отправлен AI без изменений).
        Самая первая инструкция промпта: сделай аналитический отчёт по условиям этого промпта.
        В твоем выводе должен быть только промпт, никаких вводных предложений.
        """;

        var prompt = await promptAgent.ChatAsync(promptUserMessage);

        var taskModelSettings = new AIModelSettings(TASK_MODEL);
        using var taskAgent = new Agent(baseUrl, apiKey, taskModelSettings);
        var taskSolution = await taskAgent.ChatAsync(prompt);

        return 
        $"""
        СГЕНЕРИРОВАННЫЙ ПРОМПТ:
        {prompt}

        РЕШЕНИЕ ЗАДАЧИ:
        {taskSolution}
        """;
    }
}