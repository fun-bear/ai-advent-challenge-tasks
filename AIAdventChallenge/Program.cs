using AIAdventChallenge.Handlers;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/day01/task", Day01TaskHandler.HandleAsync);
app.MapGet("/day02/task", Day02TaskHandler.HandleAsync);

app.Run();
