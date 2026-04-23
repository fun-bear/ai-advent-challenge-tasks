using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AIAdventChallenge.Infrastructure.Models;

namespace AIAdventChallenge.Infrastructure;

public class LLMClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly AIModelSettings _modelSettings;
    private string? _systemPrompt;
    private List<ChatMessage> _history = [];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public LLMClient(
        string baseAddress,
        string apiKey,
        AIModelSettings modelSettings,
        string? systemPrompt = null,
        TimeSpan? timeout = null)
    {
        _modelSettings = modelSettings;
        _systemPrompt = systemPrompt;

        _http = new HttpClient { BaseAddress = new Uri(baseAddress) };
        if (timeout.HasValue)
        {
            _http.Timeout = timeout.Value;
        }

        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        Reset();
    }

    public async Task<ChatResult> ChatAsync(string userMessage)
    {
        _history.Add(new ChatMessage("user", userMessage));

        var request = new ChatRequest(
            _modelSettings.ModelName,
            _history,
            _modelSettings.MaxTokens,
            _modelSettings.Temperature);

        var json = JsonSerializer.Serialize(request, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var httpResponse = await _http.PostAsync("chat/completions", content);
        httpResponse.EnsureSuccessStatusCode();

        var body = await httpResponse.Content.ReadAsStringAsync();
        var response = JsonSerializer.Deserialize<ChatResponse>(body, JsonOptions)!;

        var assistantText = response.Choices[0].Message.Content;
        var totalTokens = response.Usage?.TotalTokens ?? 0;
        var outputTokens = response.Usage?.CompletionTokens ?? 0;
        _history.Add(new ChatMessage("assistant", assistantText));

        return new ChatResult(assistantText, totalTokens, outputTokens);
    }

    public void Reset() =>
        _history = _systemPrompt is null
            ? []
            : [new ChatMessage("system", _systemPrompt)];

    public void SetSystemPrompt(string? systemPrompt)
    {
        _systemPrompt = systemPrompt;

        Reset();
    }

    public IReadOnlyList<ChatMessage> ExportHistory() =>
        _history
            .Select(m => new ChatMessage(m.Role, m.Content))
            .ToList();

    public void ImportHistory(IEnumerable<ChatMessage> history)
    {
        _history = history
            .Select(m => new ChatMessage(m.Role, m.Content))
            .ToList();
    }

    public void AddHistory(IEnumerable<ChatMessage> history)
    {
        _history.AddRange(
            history.Select(m => new ChatMessage(m.Role, m.Content)));
    }

    public void Dispose() => _http.Dispose();
}