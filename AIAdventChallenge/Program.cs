using AIAdventChallenge.Handlers;
using AIAdventChallenge.Handlers.Day03TaskHandlers;
using AIAdventChallenge.Handlers.Day10TaskHandlers;
using AIAdventChallenge.Handlers.Day18TaskHandlers;
using AIAdventChallenge.Infrastructure.Storage;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var sqliteConnectionBuilder = new SqliteConnectionStringBuilder();
sqliteConnectionBuilder.DataSource = Path.Combine(AppContext.BaseDirectory, "ai-advent.db");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(sqliteConnectionBuilder.ToString()));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("day30-task-rate-limit", httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-client";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: clientIp,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

var app = builder.Build();

// Temporary comment for dev5 branch change
// ABP Penetration Test Report
// The ABP Commercial MVC v10.0.1 application template has never been tested against security vulnerabilities by the OWASP ZAP v2.14.0 tool. So, we cannot use it here.

app.UseRateLimiter();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}

app.MapGet("/day01/task", Day01TaskHandler.HandleAsync);
app.MapGet("/day02/task", Day02TaskHandler.HandleAsync);
app.MapGet("/day03/subtask01", Day03Subtask01Handler.HandleAsync);
app.MapGet("/day03/subtask02", Day03Subtask02Handler.HandleAsync);
app.MapGet("/day03/subtask03", Day03Subtask03Handler.HandleAsync);
app.MapGet("/day03/subtask04", Day03Subtask04Handler.HandleAsync);
app.MapGet("/day04/task", Day04TaskHandler.HandleAsync);
app.MapGet("/day05/task", (IConfiguration configuration, AIAdventChallenge.ViewModels.AgentPowerParam power) =>
    Day05TaskHandler.HandleAsync(configuration, power));
app.MapGet("/day06/task", Day06TaskHandler.HandleAsync);
app.MapGet("/day07/task", Day07TaskHandler.HandleAsync);
app.MapGet("/day08/task", Day08TaskHandler.HandleAsync);
app.MapGet("/day09/task", Day09TaskHandler.HandleAsync);
app.MapGet("/day10/subtask01", Day10Subtask01Handler.HandleAsync);
app.MapGet("/day10/subtask02", Day10Subtask02Handler.HandleAsync);
app.MapGet("/day10/subtask03", Day10Subtask03Handler.HandleAsync);
app.MapGet("/day11/task", Day11TaskHandler.HandleAsync);
app.MapGet("/day12/task", Day12TaskHandler.HandleAsync);
app.MapGet("/day13/task", Day13TaskHandler.HandleAsync);
app.MapGet("/day14/task", Day14TaskHandler.HandleAsync);
app.MapGet("/day15/task", Day15TaskHandler.HandleAsync);
app.MapGet("/day16/task", Day16TaskHandler.HandleAsync);
app.MapGet("/day17/task", Day17TaskHandler.HandleAsync);
app.MapGet("/day18/subtask01", Day18Subtask01Handler.HandleAsync);
app.MapGet("/day18/subtask02", Day18Subtask02Handler.HandleAsync);
app.MapGet("/day21/task", Day21TaskHandler.HandleAsync);
app.MapGet("/day22/task", Day22TaskHandler.HandleAsync);
app.MapGet("/day23/task", Day23TaskHandler.HandleAsync);
app.MapGet("/day24/task", Day24TaskHandler.HandleAsync);
app.MapGet("/day25/task", Day25TaskHandler.HandleAsync);
app.MapGet("/day27/task", Day27TaskHandler.HandleAsync);
app.MapGet("/day28/task", Day28TaskHandler.HandleAsync);
app.MapGet("/day29/task", Day29TaskHandler.HandleAsync);
app.MapGet("/day30/task", Day30TaskHandler.HandleAsync)
    .RequireRateLimiting("day30-task-rate-limit");

app.Run();

// dev7: trivial change for PR

// dev9: trivial change for PR
