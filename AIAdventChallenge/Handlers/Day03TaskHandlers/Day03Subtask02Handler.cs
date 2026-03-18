using AIAdventChallenge.Infrastructure;

namespace AIAdventChallenge.Handlers.Day03TaskHandlers;

/// <summary>
/// День 03, подзадача 02.
/// Специальный handler для решения аналитической задачи по открытию бизнеса.
/// Режим: пошаговое решение.
/// </summary>
public static class Day03Subtask02Handler
{
    public static async Task<string> HandleAsync(IConfiguration configuration)
    {
        var settings = configuration.GetSection("OpenAISettings");
        var apiKey = settings["ApiKey"] ?? throw new InvalidOperationException("OpenAISettings:ApiKey is missing.");
        var modelName = settings["ModelName"] ?? throw new InvalidOperationException("OpenAISettings:ModelName is missing.");
        var baseUrl = settings["BaseUrl"] ?? throw new InvalidOperationException("OpenAISettings:BaseUrl is missing.");

        var modelSettings = new AIModelSettings(modelName);
        using var agent = new Agent(baseUrl, apiKey, modelSettings);
        var userMessage = 
        $"""
        {Day03TaskDescriptions.TASK_DESCRIPTION}

        Решай задачу пошагово.
        """;

        return await agent.ChatAsync(userMessage);
    }
}