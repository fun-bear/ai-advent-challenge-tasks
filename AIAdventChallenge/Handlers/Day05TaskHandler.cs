using System.Diagnostics;
using AIAdventChallenge.Infrastructure;
using AIAdventChallenge.ViewModels;

namespace AIAdventChallenge.Handlers;

/// <summary>
/// День 05.
/// Специальный handler для решения детективных задач.
/// Параметр модели ("мощность") берет из параметров.
/// </summary>
public static class Day05TaskHandler
{
    private const string SYSTEM_PROMPT =
    """
    Представь, что ты детектив. Ты должен провести расследование преступления.
    Предоставь ответ с рассуждениями, суммарно не более 3-5 предложений.
    Запрещено искать решение этой же задачи в интернете и в своей базе знаний - необходимо именно рассуждать.
    """;

    private const string MODEL_NAME_LOW = "yandex/gpt-lite-5";
    private const string MODEL_NAME_MEDIUM = "deepseek/deepseek-v3.2";
    private const string MODEL_NAME_HIGH = "openai/gpt-5.3-codex";

    public static async Task<Day05TaskResult> HandleAsync(
        IConfiguration configuration,
        AgentPower power)
    {
        var settings = configuration.GetSection("OpenAISettings");
        var apiKey = settings["ApiKey"] ?? throw new InvalidOperationException("OpenAISettings:ApiKey is missing.");
        var baseUrl = settings["BaseUrl"] ?? throw new InvalidOperationException("OpenAISettings:BaseUrl is missing.");

        var modelSettings = CreateModelSettings(power);
        using var agent = new Agent(baseUrl, apiKey, modelSettings, SYSTEM_PROMPT);
        var userMessage = 
        """
        Доктор Квик приехал в магазин рано утром. Управляющий уже ждал его у входа.
        – Только что обнаружили, что в торговом зале недостает нескольких телевизоров. Я думаю, их украли! — волновался управляющий.
        – Вы кого-нибудь подозреваете? — спросил Квик.
        – Да! Я полагаю, они пропали во время вечерней смены, когда в магазине было всего два продавца. Каждый раз, когда пропадают телевизоры, дежурят эти двое — Том и Ленни. Мне кажется, ворует один из них, но кто именно, я не знаю. Похоже, что Том, хотя иногда я грешу на Ленни, — ответил управляющий.
        – Почему сейчас вы подозреваете Тома?
        – Я разговаривал с ними обоими. Вы ж понимаете: когда в таком большом магазине, как наш работают всего два продавца, один из них может находиться в одном зале магазина, а второй — в другом. Они могут долго не попадаться друг другу на глаза. Когда я их расспрашивал о пропаже, Том сказал, что не заметил ничего подозрительного, а вот Ленни признался, что видел, как Том вечером укладывал какую-то большую коробку в багажник своей машины.
        – Я хотел бы поговорить с Томом,— сказал доктор Квик.
        – А вот как раз и он! — И управляющий кивнул на маленький красный грузовичок, который подъезжал к магазину.
        – Теперь я не уверен, что мне нужен именно Том. Давайте-ка лучше спросим Ленни, куда он дел телевизоры, — сказал доктор.

        Почему доктор Квик заподозрил Ленни?
        """;

        var stopwatch = Stopwatch.StartNew();
        var result = await agent.ChatAsync(userMessage);
        stopwatch.Stop();

        return new Day05TaskResult(
            Solution: result.Content,
            ElapsedInMs: stopwatch.ElapsedMilliseconds,
            Power: power.ToString(),
            TotalTokens: result.TotalTokens);
    }

    private static AIModelSettings CreateModelSettings(AgentPower power) => power switch
    {
        AgentPower.Low => new AIModelSettings(MODEL_NAME_LOW),
        AgentPower.Medium => new AIModelSettings(MODEL_NAME_MEDIUM),
        AgentPower.High => new AIModelSettings(MODEL_NAME_HIGH),
        _ => throw new NotSupportedException($"Unsupported agent power: {power}.")
    };
}