namespace AIAdventChallenge.McpServer.Models;
using System.Text;

public class Task
{
    public Guid Id { get; }
    public TaskType Type { get; }
    public TaskState State { get; private set; }
    public int Cycles { get; private set; }
    public int CycleTimeout { get; }
    public string Log { get; private set; }
    public string? Payload { get; }
    public DateTimeOffset NextOccurrence { get; private set; }

    private Task(Guid id, TaskType type, TaskState state, int cycles, int cycleTimeout, string log, string? payload, DateTimeOffset nextOccurrence)
    {
        Id = id;
        Type = type;
        State = state;
        Cycles = cycles;
        CycleTimeout = cycleTimeout;
        Log = log;
        Payload = payload;
        NextOccurrence = nextOccurrence;
    }

    public static Task CreateNew(int cycles, int cycleTimeout, string? payload)
    {
        var task = new Task(Guid.NewGuid(), TaskType.Ping, TaskState.Idle, cycles, cycleTimeout, string.Empty, payload, DateTimeOffset.UtcNow);
        task.LogState();

        return task;
    }

    public static Task CreateExisting(Guid id, TaskType type, TaskState state, int cycles, int cycleTimeout, string log, string? payload, DateTimeOffset nextOccurrence)
    {
        return new Task(id, type, state, cycles, cycleTimeout, log, payload, nextOccurrence);
    }

    private void LogState()
    {
        var log = new StringBuilder(Log);

        log.AppendLine($"- State: {State}, Cycles left: {Cycles}, Next occurrence: {NextOccurrence}");

        Log = log.ToString();
    }

    private void LogResult(string result)
    {
        var log = new StringBuilder(Log);

        log.AppendLine();
        log.AppendLine($"- New cycle, Timestamp: {DateTimeOffset.UtcNow}");
        log.AppendLine($"- Cycle result: {result}");

        Log = log.ToString();
    }

    public void DoCycle(string currentCycleResult)
    {
        if (string.IsNullOrEmpty(currentCycleResult))
        {
            throw new ArgumentException("Current cycle result cannot be null or empty.");
        }

        if (State is not TaskState.Idle && State is not TaskState.Started)
        {
            throw new InvalidOperationException($"Task in state '{State}' cannot execute next cycle.");
        }

        if (Cycles < 1)
        {
            throw new InvalidOperationException("Task cycles must be greater than 0 to execute next cycle.");
        }

        if (State == TaskState.Idle)
        {
            State = TaskState.Started;
        }

        Cycles--;

        if (Cycles == 0)
        {
            State = TaskState.Finished;
        }
        else
        {
            NextOccurrence = DateTimeOffset.UtcNow.AddMilliseconds(CycleTimeout);
        }

        LogResult(currentCycleResult);
        LogState();
    }
}