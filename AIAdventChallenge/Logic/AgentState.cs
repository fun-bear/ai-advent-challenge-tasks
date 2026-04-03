using AIAdventChallenge.Logic.Models;

namespace AIAdventChallenge.Logic;

public class AgentState
{
    private readonly Action<string> logAction;

    public AgentState(Action<string>? log = null)
    {
        logAction = log ?? (_ => { });
    }

    public Stage Stage { get; set; } = Stage.Idle;
    public string CurrentStep { get; set; } = "";
    public string ExpectedAction { get; set; } = "Ожидаем задачу от пользователя";
    public List<string> Plan { get; set; } = new();
    public int CurrentStepIndex { get; set; } = 0;
    public List<string> Results { get; set; } = new();
    public string UserQuery { get; set; } = "";
    public bool IsPaused { get; set; }

    // допустимые переходы
    private static readonly Dictionary<Stage, Stage[]> AllowedTransitions = new()
    {
        [Stage.Idle]       = new[] { Stage.Planning },
        [Stage.Planning]   = new[] { Stage.Execution, Stage.Error },
        [Stage.Execution]  = new[] { Stage.Validation, Stage.Planning, Stage.Error },
        [Stage.Validation] = new[] { Stage.Done, Stage.Execution, Stage.Planning, Stage.Error },
        [Stage.Done]       = new[] { Stage.Idle },  // можно начать заново
        [Stage.Error]      = new[] { Stage.Idle },  // сброс
    };

    public bool TransitionTo(Stage next)
    {
        if (IsPaused)
        {
            logAction("⏸ Агент на паузе. Вызовите Resume().");
            return false;
        }

        if (!AllowedTransitions.ContainsKey(Stage) ||
            !AllowedTransitions[Stage].Contains(next))
        {
            logAction($"❌ Переход {Stage} → {next} запрещён!");
            return false;
        }

        Stage = next;
        return true;
    }

    public void Pause() => IsPaused = true;

    public void Resume()
    {
        IsPaused = false;
        logAction($"▶️ Продолжаем с этапа [{Stage}]");
    }

    public void Print()
    {
        logAction(string.Empty);
        logAction($"📍 Этап:    {Stage}");
        logAction($"📌 Шаг:     {CurrentStep}");
        logAction($"👉 Ожидаем: {ExpectedAction}");
        logAction($"⏸ Пауза:    {IsPaused}");

        if (Plan.Count > 0)
            logAction($"📋 План:    {Plan.Count} шагов{new string(' ', Math.Max(0, 14 - Plan.Count.ToString().Length))}");
    }
}