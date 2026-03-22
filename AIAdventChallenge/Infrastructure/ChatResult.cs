namespace AIAdventChallenge.Infrastructure;

public record ChatResult(
    string Content,
    int TotalTokens);