namespace AIAdventChallenge.Logic.Models;

/// <summary>
/// Описание нарушения инварианта
/// </summary>
public class InvariantViolation
{
    public Invariant Invariant { get; init; } = null!;
    public string Explanation { get; init; } = string.Empty;
}