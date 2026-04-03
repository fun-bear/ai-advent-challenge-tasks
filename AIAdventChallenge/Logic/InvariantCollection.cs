using AIAdventChallenge.Logic.Models;

namespace AIAdventChallenge.Logic;

/// <summary>
/// Коллекция инвариантов.
/// Живёт отдельно от диалога — правила не зависят от контекста разговора.
/// </summary>
public class InvariantCollection
{
    private readonly List<Invariant> _invariants = new();

    public void Add(Invariant invariant)
    {
        _invariants.Add(invariant);
    }

    public void Remove(string id)
    {
        _invariants.RemoveAll(i => i.Id == id);
    }

    public IReadOnlyList<Invariant> GetAll() => _invariants.AsReadOnly();

    /// <summary>
    /// Форматируем все инварианты для подстановки в промпт
    /// </summary>
    public string ToPromptString()
    {
        if (_invariants.Count == 0)
            return "Ограничений нет.";

        var lines = _invariants
            .OrderByDescending(i => i.Priority)
            .Select((inv, idx) => $"{idx + 1}. [{inv.Category}] {inv.Name}: {inv.Description}");

        return string.Join("\n", lines);
    }
}