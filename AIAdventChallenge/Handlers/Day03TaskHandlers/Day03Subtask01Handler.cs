using AIAdventChallenge.Infrastructure;

namespace AIAdventChallenge.Handlers.Day03TaskHandlers;

/// <summary>
/// День 03, подзадача 01.
/// Специальный handler для решения аналитической задачи по открытию бизнеса.
/// Режим: прямой ответ без дополнительных инструкций.
/// </summary>
public static class Day03Subtask01Handler
{
    public static async Task<string> HandleAsync(IConfiguration configuration)
    {
        var settings = configuration.GetSection("OpenAISettings");
        var apiKey = settings["ApiKey"] ?? throw new InvalidOperationException("OpenAISettings:ApiKey is missing.");
        var modelName = settings["ModelName"] ?? throw new InvalidOperationException("OpenAISettings:ModelName is missing.");
        var baseUrl = settings["BaseUrl"] ?? throw new InvalidOperationException("OpenAISettings:BaseUrl is missing.");

        var modelSettings = new AIModelSettings(modelName);
        using var llmClient = new LLMClient(baseUrl, apiKey, modelSettings);
        var userMessage = Day03TaskDescriptions.TASK_DESCRIPTION;

        var result = await llmClient.ChatAsync(userMessage);
        return result.Content;
    }
}