namespace AIAdventChallenge.McpServer.Models;

public sealed class Account
{
    public long Id { get; init; }

    public required string FullName { get; init; }

    public bool IsBlocked { get; init; }

    public required string Role { get; init; }

    public long Balance { get; init; }
}