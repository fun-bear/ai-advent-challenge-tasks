using System.Text.Json.Serialization;

namespace AIAdventChallenge.Infrastructure.Models;

public record ChatResponse(
    [property: JsonPropertyName("choices")] List<Choice> Choices
);