using AIAdventChallenge.Infrastructure;

namespace AIAdventChallenge.Handlers;

/// <summary>
/// День 34.
/// Заглушка обработчика с проверкой входного query-параметра.
/// </summary>
public static class Day34TaskHandler
{
    public static async Task<IResult> HandleAsync(IConfiguration configuration, string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Results.BadRequest("Query-параметр 'query' обязателен.");
        }

        try
        {
            var assistant = new AiAssistantService();
            var response = await assistant.ProcessMessageAsync(configuration, query);
            return Results.Content(response);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Ошибка Day34 AI-ассистента: {ex.Message}");
        }
    }
}