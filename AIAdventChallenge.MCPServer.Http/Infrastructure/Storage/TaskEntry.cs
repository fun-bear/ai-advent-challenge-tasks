using AIAdventChallenge.McpServer.Models;

namespace AIAdventChallenge.McpServer.Infrastructure.Storage;

public class TaskEntry
{
    public required Guid Id { get; set; }
    public required TaskType Type { get; set; }
    public required TaskState State { get; set; }
    public required int Cycles { get; set; }
    public required int CycleTimeout { get; set; }
    public required string Log { get; set; }
    public required string? Payload { get; set; }
    public required DateTimeOffset NextOccurrence { get; set; }
}