namespace AIAdventChallenge.ViewModels;

public record Day04TaskResult(
    Day04TaskOldModel OldModel,
    Day04TaskNewModel NewModel);

public record Day04TaskOldModel(
    string Temperature0,
    string Temperature07,
    string Temperature12);

public record Day04TaskNewModel(
    string Result);