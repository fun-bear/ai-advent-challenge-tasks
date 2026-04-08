using AIAdventChallenge.McpServer.Infrastructure.Storage;
using ModelContextProtocol.Server;
using System.ComponentModel;
using DomainTask = AIAdventChallenge.McpServer.Models.Task;

namespace AIAdventChallenge.McpServer.Tools;

[McpServerToolType]
public sealed class PingTool
{
    private readonly TaskRepository _taskRepository;

    public PingTool(TaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    [McpServerTool, Description("Создаёт ping-задачу и сохраняет её в репозитории")]
    public async Task<Guid> Ping(
        [Description("Хост (домен или IP)")] string host,
        [Description("Количество циклов ping")] int cycles,
        [Description("Таймаут одного цикла в миллисекундах")] int cycleTimeout,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException("Host is required.", nameof(host));
        }

        if (cycles <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cycles), "Cycles must be greater than 0.");
        }

        if (cycleTimeout <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cycleTimeout), "Cycle timeout must be greater than 0.");
        }

        var task = DomainTask.CreateNew(cycles, cycleTimeout, host);
        await _taskRepository.AddAsync(task, cancellationToken);
        return task.Id;
    }

    [McpServerTool, Description("Возвращает информацию по ping-задаче")]
    public async Task<string> GetLog(
        [Description("Идентификатор задачи ping")] Guid taskId,
        CancellationToken cancellationToken)
    {
        var task = await _taskRepository.GetAsync(taskId, cancellationToken);
        return task.Log;
    }
}