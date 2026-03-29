using AIAdventChallenge.Handlers;
using AIAdventChallenge.Handlers.Day03TaskHandlers;
using AIAdventChallenge.Handlers.Day10TaskHandlers;
using AIAdventChallenge.Infrastructure.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var sqliteConnectionBuilder = new SqliteConnectionStringBuilder();
sqliteConnectionBuilder.DataSource = Path.Combine(AppContext.BaseDirectory, "ai-advent.db");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(sqliteConnectionBuilder.ToString()));

var app = builder.Build();

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

app.Run();
