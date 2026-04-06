using System.Text;
using AIAdventChallenge.Logic;
using AIAdventChallenge.Logic.Models;

namespace AIAdventChallenge.Handlers;

/// <summary>
/// День 15.
/// Демонстрация переходов AgentState с намеренной ошибкой перехода Execution -> Done.
/// </summary>
public static class Day15TaskHandler
{
    public static IResult HandleAsync()
    {
        var logBuilder = new StringBuilder();

        void Log(string message)
        {
            Console.WriteLine(message);
            logBuilder.AppendLine(message);
        }

        var state = new AgentState(Log);

        Log("🚀 Старт симуляции переходов AgentState");

        TryTransition(state, Stage.Planning, Log);
        TryTransition(state, Stage.Execution, Log);

        Log("⚠️ Пробуем выполнить переход напрямую: Execution -> Done");
        var movedToDone = state.TransitionTo(Stage.Done);
        Log(movedToDone
            ? "✅ Неожиданно получилось перейти в Done"
            : "❌ Переход отклонён: Execution -> Done запрещён");

        Log("➡️ Переходим по корректному пути: Execution -> Validation -> Done");
        TryTransition(state, Stage.Validation, Log);
        TryTransition(state, Stage.Done, Log);

        Log("🏁 Симуляция завершена");

        return Results.Content(logBuilder.ToString());
    }

    private static void TryTransition(AgentState state, Stage next, Action<string> log)
    {
        var previous = state.Stage;
        var ok = state.TransitionTo(next);
        log(ok
            ? $"Handler: ✅ Переход выполнен: {previous} -> {next}"
            : $"Handler: ❌ Переход отклонён: {previous} -> {next}");
    }
}