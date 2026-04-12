using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using TaskType = AIAdventChallenge.McpServer.Models.TaskType;
using AIAdventChallenge.McpServer.Infrastructure.Storage;
using AIAdventChallenge.McpServer.Jobs;

namespace AIAdventChallenge.McpServer.BackgroundServices;

public class TaskSchedulerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public TaskSchedulerService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            stoppingToken.ThrowIfCancellationRequested();

            using var scope = _scopeFactory.CreateScope();
            var taskRepository = scope.ServiceProvider.GetRequiredService<TaskRepository>();

            var dueTasks = await taskRepository.GetDueTasksAsync(stoppingToken);
            foreach (var task in dueTasks)
            {
                if (task.Type == TaskType.Ping)
                {
                    await PingJob.ExecuteAsync(task, stoppingToken);
                    await taskRepository.UpdateAsync(task, stoppingToken);
                    continue;
                }

                throw new NotSupportedException($"Task with type {task.Type} is not supported");
            }
            
            await Task.Delay(50, stoppingToken);
        }
    }
}