using AIAdventChallenge.Infrastructure;
using AIAdventChallenge.Logic;

namespace AIAdventChallenge.Handlers;

/// <summary>
/// День 13.
/// Запуск StateAgent с query и возврат накопленного строкового лога.
/// </summary>
public static class Day13TaskHandler
{
    private const int TIME_BEFORE_PAUSE = 3000; 
    private const int PAUSE_TIME = 1000; 

    public static async Task<IResult> HandleAsync(IConfiguration configuration, string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Results.BadRequest("Query-параметр 'query' обязателен.");
        }

        var settings = configuration.GetSection("OpenAISettings");
        var apiKey = settings["ApiKey"] ?? throw new InvalidOperationException("OpenAISettings:ApiKey is missing.");
        var modelName = settings["ModelName"] ?? throw new InvalidOperationException("OpenAISettings:ModelName is missing.");
        var baseUrl = settings["BaseUrl"] ?? throw new InvalidOperationException("OpenAISettings:BaseUrl is missing.");

        var modelSettings = new AIModelSettings(modelName);
        using var llmClient = new LLMClient(baseUrl, apiKey, modelSettings);
        var invariants = new InvariantCollection();
        using var stateAgent = new StateAgent(llmClient, invariants);

        var task = stateAgent.RunAsync(query);

        await Task.Delay(TIME_BEFORE_PAUSE);

        stateAgent.Pause();

        await Task.Delay(PAUSE_TIME);

        stateAgent.LogState();
        stateAgent.Resume();
        
        await task;

        return Results.Content(stateAgent.GetLog());
    }
}