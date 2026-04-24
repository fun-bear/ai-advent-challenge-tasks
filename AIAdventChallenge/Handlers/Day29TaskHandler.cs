using System.Text;
using AIAdventChallenge.Infrastructure;

namespace AIAdventChallenge.Handlers;

/// <summary>
/// День 29.
/// Запрос к локальной LLM (Ollama) с параметрами maxTokens и temperature.
/// </summary>
public static class Day29TaskHandler
{
    private const string DefaultSystemPrompt =
        "You are a helpful AI assistant. Answer concisely, clearly, and to the point.";

    private const string OLLAMA_API_KEY = "ollama";
    private const string OLLAMA_MODEL_NAME = "llama3.1:8b";
    private const string OLLAMA_BASE_URL = "http://localhost:11434/v1/";

    public static async Task<IResult> HandleAsync(
        int maxTokens,
        decimal temperature,
        string prompt,
        string? systemPrompt = null)
    {
        if (maxTokens <= 0)
        {
            return Results.BadRequest("Параметр maxTokens должен быть > 0.");
        }

        if (temperature < 0)
        {
            return Results.BadRequest("Параметр temperature должен быть >= 0.");
        }

        if (string.IsNullOrWhiteSpace(prompt))
        {
            return Results.BadRequest("Параметр prompt обязателен.");
        }

        var answer = await AskLlmAsync(
            prompt,
            maxTokens,
            temperature,
            string.IsNullOrWhiteSpace(systemPrompt) ? DefaultSystemPrompt : systemPrompt);

        var response = new StringBuilder();
        response.AppendLine("=== Day29: Локальная LLM с параметрами maxTokens и temperature ===");
        response.AppendLine($"Параметры: maxTokens={maxTokens}, temperature={temperature}");
        response.AppendLine();
        response.AppendLine("Ответ:");
        response.AppendLine(answer);

        return Results.Content(response.ToString());
    }

    private static async Task<string> AskLlmAsync(
        string prompt,
        int maxTokens,
        decimal temperature,
        string? systemPrompt)
    {
        var modelSettings = new AIModelSettings(
            OLLAMA_MODEL_NAME,
            MaxTokens: maxTokens,
            Temperature: temperature);

        using var llmClient = new LLMClient(
            OLLAMA_BASE_URL,
            OLLAMA_API_KEY,
            modelSettings,
            systemPrompt,
            timeout: TimeSpan.FromMinutes(10));

        var result = await llmClient.ChatAsync(prompt);

        return string.IsNullOrWhiteSpace(result.Content)
            ? "[Пустой ответ от AI LLM]"
            : result.Content;
    }
}