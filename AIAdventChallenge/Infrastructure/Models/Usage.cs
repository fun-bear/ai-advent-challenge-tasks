using System.Text.Json.Serialization;

namespace AIAdventChallenge.Infrastructure.Models;

public record Usage(
    [property: JsonPropertyName("total_tokens")] int TotalTokens,
    [property: JsonPropertyName("completion_tokens")] int CompletionTokens
);