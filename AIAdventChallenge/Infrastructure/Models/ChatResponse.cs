using System.Text.Json.Serialization;

namespace AIAdventChallenge.Infrastructure.Models;

public record ChatResponse(
    [property: JsonPropertyName("choices")] List<Choice> Choices,
    [property: JsonPropertyName("usage")] Usage? Usage = null
);