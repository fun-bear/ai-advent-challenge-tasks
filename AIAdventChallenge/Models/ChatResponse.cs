using System.Text.Json.Serialization;

namespace AIAdventChallenge.Models;

public record ChatResponse(
    [property: JsonPropertyName("choices")] List<Choice> Choices
);