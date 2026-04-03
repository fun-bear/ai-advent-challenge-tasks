using AIAdventChallenge.Infrastructure;
using AIAdventChallenge.Logic;
using AIAdventChallenge.Logic.Models;

namespace AIAdventChallenge.Handlers;

/// <summary>
/// День 14.
/// Агент с контролем инвариантов: запускает StateAgent с query,
/// выполняет паузу/возобновление и возвращает накопленный строковый лог.
/// </summary>
public static class Day14TaskHandler
{
    public static async Task<IResult> HandleAsync(
        IConfiguration configuration, string? query, bool checkInvariantBeforeStart = false)
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
        
        var invariants = CreateInvariants();
        using var stateAgent = new StateAgent(llmClient, invariants);

        await stateAgent.RunAsync(query, checkInvariantBeforeStart);

        return Results.Content(stateAgent.GetLog());
    }

    private static InvariantCollection CreateInvariants()
    {
        var invariants = new InvariantCollection();

        invariants.Add(new Invariant
        {
            Category = "Архитектура",
            Name = "Микросервисы",
            Description = "Система строится на микросервисной архитектуре. Монолитные решения запрещены.",
            Priority = 10
        });

        invariants.Add(new Invariant
        {
            Category = "Стек",
            Name = "Дотнет-стек",
            Description = "Бэкенд — только .NET (C#). Не предлагать Python, Java, Go для основного бэкенда.",
            Priority = 10
        });

        invariants.Add(new Invariant
        {
            Category = "База данных",
            Name = "PostgreSQL",
            Description = "Единая СУБД — PostgreSQL. Не предлагать MySQL, MongoDB, MSSQL.",
            Priority = 8
        });

        invariants.Add(new Invariant
        {
            Category = "Бизнес-правило",
            Name = "Лимит запросов",
            Description = "Клиенту нельзя давать более 100 запросов в минуту. Не предлагать обход лимитов.",
            Priority = 9
        });

        return invariants;
    }
}