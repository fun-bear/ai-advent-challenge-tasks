namespace AIAdventChallenge.Infrastructure.Storage;

public class AgentSummaryEntry
{
    public long Id { get; set; }
    public string AgentKey { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
}