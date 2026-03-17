using System.Text.Json.Serialization;

namespace AIAdventChallenge.Infrastructure.Models;

public record Choice(
    [property: JsonPropertyName("message")] ChatMessage Message
);