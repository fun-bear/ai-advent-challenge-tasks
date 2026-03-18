using AIAdventChallenge.Handlers;
using AIAdventChallenge.Handlers.Day03TaskHandlers;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/day01/task", Day01TaskHandler.HandleAsync);
app.MapGet("/day02/task", Day02TaskHandler.HandleAsync);
app.MapGet("/day03/subtask01", Day03Subtask01Handler.HandleAsync);
app.MapGet("/day03/subtask02", Day03Subtask02Handler.HandleAsync);
app.MapGet("/day03/subtask03", Day03Subtask03Handler.HandleAsync);
app.MapGet("/day03/subtask04", Day03Subtask04Handler.HandleAsync);

app.Run();
