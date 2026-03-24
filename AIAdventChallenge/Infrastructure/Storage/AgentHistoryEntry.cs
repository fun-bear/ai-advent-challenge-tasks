namespace AIAdventChallenge.Infrastructure.Storage;

public class AgentHistoryEntry
{
    public long Id { get; set; }
    public string AgentKey { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}