using System.Text.Json.Serialization;

namespace AIAdventChallenge.Models;

public record ChatMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content
);