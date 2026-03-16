using System.Text.Json.Serialization;

namespace AIAdventChallenge.Models;

public record ChatRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("messages")] List<ChatMessage> Messages
);