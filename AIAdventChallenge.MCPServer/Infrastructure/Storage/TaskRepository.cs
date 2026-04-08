using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using DomainTask = AIAdventChallenge.McpServer.Models.Task;
using TaskState = AIAdventChallenge.McpServer.Models.TaskState;

namespace AIAdventChallenge.McpServer.Infrastructure.Storage;

public class TaskRepository
{
    private readonly AppDbContext _dbContext;

    public TaskRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(DomainTask task, CancellationToken token)
    {
        var entry = new TaskEntry
        {
            Id = task.Id,
            Type = task.Type,
            State = task.State,
            Cycles = task.Cycles,
            CycleTimeout = task.CycleTimeout,
            Log = task.Log,
            Payload = task.Payload,
            NextOccurrence = task.NextOccurrence
        };

        await _dbContext.TaskEntries.AddAsync(entry, token);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(DomainTask task, CancellationToken token)
    {
        var entry = await _dbContext.TaskEntries.FirstOrDefaultAsync(e => e.Id == task.Id, token);

        if (entry == null)
        {
            throw new InvalidOperationException($"Task with id {task.Id} not found.");
        }

        entry.State = task.State;
        entry.Cycles = task.Cycles;
        entry.Log = task.Log;
        entry.NextOccurrence = task.NextOccurrence;

        await _dbContext.SaveChangesAsync(token);
    }

    public async Task<DomainTask> GetAsync(Guid id, CancellationToken token)
    {
        var entry = await _dbContext.TaskEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, token);

        if (entry is null)
        {
            throw new InvalidOperationException($"Task with id '{id}' was not found.");
        }

        return DomainTask.CreateExisting(entry.Id, entry.Type, entry.State, entry.Cycles, entry.CycleTimeout, entry.Log, entry.Payload, entry.NextOccurrence);
    }

    public async Task<IReadOnlyList<DomainTask>> GetDueTasksAsync(CancellationToken token)
    {
        var now = DateTimeOffset.UtcNow;

        var entries = await _dbContext.TaskEntries
            .AsNoTracking()
            .Where(x =>
                (x.State == TaskState.Idle || x.State == TaskState.Started)
                && x.NextOccurrence <= now)
            .ToListAsync(token);

        return entries.Select(e => DomainTask.CreateExisting(e.Id, e.Type, e.State, e.Cycles, e.CycleTimeout, e.Log, e.Payload, e.NextOccurrence))
            .ToList();
    }
}