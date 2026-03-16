using System.Text.Json.Serialization;

namespace AIAdventChallenge.Models;

public record Choice(
    [property: JsonPropertyName("message")] ChatMessage Message
);