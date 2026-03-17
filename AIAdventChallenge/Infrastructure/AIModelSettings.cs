namespace AIAdventChallenge.Infrastructure;

public record AIModelSettings(
    string ModelName,
    int? MaxTokens = null,
    decimal? Temperature = null);