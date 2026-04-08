using System.Globalization;
using System.Net.NetworkInformation;

namespace AIAdventChallenge.McpServer.Jobs;

public static class PingJob
{
    public static async Task ExecuteAsync(
        Models.Task task,
        CancellationToken stoppingToken)
    {
        if (task.Payload is null)
        {
            throw new InvalidOperationException("Ping task payload is null");
        }

        var pingResult = await PingAsync(task.Payload);
        task.DoCycle(pingResult.ToString(CultureInfo.InvariantCulture));
    }

    private static async Task<bool> PingAsync(string hostname)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(hostname);

            return reply.Status == IPStatus.Success;
        }
        catch (PingException)
        {
            return false;
        }
    }
}