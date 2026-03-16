using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AIAdventChallenge.Models;

namespace AIAdventChallenge.Infrastructure;

public class Agent : IDisposable
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly string _systemPrompt;
    private List<ChatMessage> _history = [];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public Agent(string baseAddress, string apiKey, string model, string systemPrompt)
    {
        _model = model;
        _systemPrompt = systemPrompt;

        _http = new HttpClient { BaseAddress = new Uri(baseAddress) };
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        Reset();
    }

    public async Task<string> ChatAsync(string userMessage)
    {
        _history.Add(new ChatMessage("user", userMessage));

        var json = JsonSerializer.Serialize(new ChatRequest(_model, _history), JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var httpResponse = await _http.PostAsync("chat/completions", content);
        httpResponse.EnsureSuccessStatusCode();

        var body = await httpResponse.Content.ReadAsStringAsync();
        var response = JsonSerializer.Deserialize<ChatResponse>(body, JsonOptions)!;

        var assistantText = response.Choices[0].Message.Content;
        _history.Add(new ChatMessage("assistant", assistantText));

        return assistantText;
    }

    public void Reset() =>
        _history = [new ChatMessage("system", _systemPrompt)];

    public void Dispose() => _http.Dispose();
}