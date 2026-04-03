namespace AIAdventChallenge.Logic.Models;

/// <summary>
/// Описание нарушения инварианта
/// </summary>
public class InvariantViolation
{
    public string Name { get; init; } = string.Empty;
    public string Explanation { get; init; } = string.Empty;
}