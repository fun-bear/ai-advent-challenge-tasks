namespace AIAdventChallenge.Logic.Models;

/// <summary>
/// Инвариант — правило, которое агент не имеет права нарушать
/// </summary>
public class Invariant
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];

    /// <summary>Категория: архитектура, стек, бизнес-правило и т.д.</summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>Краткое название для логов</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Полное описание правила</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Приоритет: чем выше, тем важнее</summary>
    public int Priority { get; init; } = 1;

    public override string ToString() => $"[{Category}] {Name}: {Description}";
}