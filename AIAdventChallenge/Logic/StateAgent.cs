using System.Text;
using System.Text.Json;
using AIAdventChallenge.Infrastructure;

namespace AIAdventChallenge.Logic;

public class StateAgent : IDisposable
{
    private const int RESUME_CHECK_DELAY = 100;

    private readonly StringBuilder _logBuilder = new();
    private readonly AgentState _state;
    private readonly LLMClient _llm;
    private bool _disposed;

    public StateAgent(LLMClient llm)
    {
        _llm = llm;
        _state = new AgentState(Log);
    }

    private void Log(string message)
    {
        Console.WriteLine(message);

        _logBuilder.AppendLine(message);
    }

    public string GetLog() => _logBuilder.ToString();

    public async Task RunAsync(string userQuery)
    {
        _state.UserQuery = userQuery;

        Log($"\n👤 Пользователь: {userQuery}");

        while (_state.Stage != Stage.Done && _state.Stage != Stage.Error)
        {
            if (_state.IsPaused)
            {
                Log("⏸ Агент на паузе. Ждём Resume()...");
                await Task.Delay(RESUME_CHECK_DELAY);
                continue;
            }

            switch (_state.Stage)
            {
                case Stage.Idle:
                    _state.TransitionTo(Stage.Planning);
                    break;
                    
                case Stage.Planning:
                    await PlanningPhase();
                    break;

                case Stage.Execution:
                    await ExecutionPhase();
                    break;

                case Stage.Validation:
                    await ValidationPhase();
                    break;
            }
        }
    }

    private async Task PlanningPhase()
    {
        _state.CurrentStep = "Анализирую задачу и составляю план...";
        _state.ExpectedAction = "Генерация плана через LLM";
        _state.Print();

        var prompt = $"""
            Ты — AI-ассистент. Разбей задачу пользователя на конкретные шаги.

            Задача: {_state.UserQuery}

            Верни ТОЛЬКО JSON-массив шагов, например:
            ["Шаг 1: ...", "Шаг 2: ...", "Шаг 3: ..."]

            Не добавляй ничего лишнего.
            """;

        try
        {
            var response = await _llm.ChatAsync(prompt);
            var responseText = response.Content;
            Log($"\n🤖 LLM ответил:\n{responseText}");

            // парсим план
            var steps = JsonSerializer.Deserialize<List<string>>(responseText);
            if (steps is { Count: > 0 })
            {
                _state.Plan = steps;
                _state.CurrentStepIndex = 0;

                Log("\n📋 План:");
                for (int i = 0; i < steps.Count; i++)
                    Log($"   {i + 1}. {steps[i]}");

                _state.TransitionTo(Stage.Execution);
            }
            else
            {
                Log("❌ Не удалось извлечь план из ответа LLM");
                _state.TransitionTo(Stage.Error);
            }
        }
        catch (Exception ex)
        {
            Log($"❌ Ошибка LLM: {ex.Message}");
            _state.TransitionTo(Stage.Error);
        }
    }

    private async Task ExecutionPhase()
    {
        while (_state.CurrentStepIndex < _state.Plan.Count)
        {
            var step = _state.Plan[_state.CurrentStepIndex];

            _state.CurrentStep = $"Шаг {_state.CurrentStepIndex + 1}/{_state.Plan.Count}: {step}";
            _state.ExpectedAction = "Выполнение через LLM";
            _state.Print();

            var prompt = $"""
                Выполни этот шаг и верни результат.

                Задача: {_state.UserQuery}
                Текущий шаг: {step}

                Верни краткий, конкретный результат.
                """;

            try
            {
                var result = await _llm.ChatAsync(prompt);
                _state.Results.Add(result.Content);

                Log($"\n✅ Результат шага:\n{result.Content}");
                _state.CurrentStepIndex++;
            }
            catch (Exception ex)
            {
                Log($"❌ Ошибка на шаге: {ex.Message}");
                _state.TransitionTo(Stage.Error);
                return;
            }
        }

        // Все шаги выполнены → валидация
        _state.TransitionTo(Stage.Validation);
    }

    private async Task ValidationPhase()
    {
        _state.CurrentStep = "Проверяю результаты...";
        _state.ExpectedAction = "Валидация через LLM";
        _state.Print();

        var allResults = string.Join("\n---\n", _state.Results);

        var prompt = $"""
            Проверь результаты выполнения задачи.

            Задача: {_state.UserQuery}

            Результаты:
            {allResults}

            Ответь ТОЛЬКО одним словом: OK или ERROR
            Если ERROR, добавь через запятую причины.

            Пример: OK
            Пример: ERROR, шаг 2 неполный, шаг 3 содержит ошибку
            """;

        try
        {
            var validation = await _llm.ChatAsync(prompt);
            var validationText = validation.Content;
            Log($"\n🔍 Валидация: {validationText}");

            if (validationText.TrimStart().StartsWith("OK", StringComparison.OrdinalIgnoreCase))
            {
                _state.TransitionTo(Stage.Done);
                _state.CurrentStep = "Задача завершена ✅";
                _state.ExpectedAction = "—";
                _state.Print();

                Log("\n📝 Итоговый ответ:");
                Log(string.Join("\n\n", _state.Results));
            }
            else
            {
                // ошибка → возврат к planning для исправления
                Log("⚠️ Валидация не пройдена. Возвращаюсь к планированию...");
                _state.TransitionTo(Stage.Planning);

                // сбрасываем прогресс, но сохраняем контекст ошибок
                _state.Results.Clear();
                _state.CurrentStepIndex = 0;
            }
        }
        catch (Exception ex)
        {
            Log($"❌ Ошибка валидации: {ex.Message}");
            _state.TransitionTo(Stage.Error);
        }
    }

    public void Pause()
    {
        _state.Pause();
        Log("⏸ Агент поставлен на паузу.");
    }

    public void Resume()
    {
        _state.Resume();
        Log("▶️ Агент продолжает работу.");
    }

    public void LogState()
    {
        Log($"📦 Состояние: {_state.Stage}, шаг: {_state.CurrentStep}");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _llm.Dispose();
        _disposed = true;
    }
}