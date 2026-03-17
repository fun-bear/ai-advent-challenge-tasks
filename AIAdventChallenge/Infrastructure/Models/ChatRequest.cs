using System.Text.Json.Serialization;

namespace AIAdventChallenge.Infrastructure.Models;

public record ChatRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("messages")] List<ChatMessage> Messages,
    [property: JsonPropertyName("max_tokens")] int? MaxTokens = null,
    [property: JsonPropertyName("temperature")] decimal? Temperature = null
);