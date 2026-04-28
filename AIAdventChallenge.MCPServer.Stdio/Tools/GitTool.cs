using ModelContextProtocol.Server;
using LibGit2Sharp;
using System.ComponentModel;
using System.Text;

namespace AIAdventChallenge.MCPServer.Stdio.Tools;

[McpServerToolType]
public sealed class GitTool
{
    [McpServerTool, Description("Возвращает текущую git-ветку в формате Markdown")]
    public static Task<string> GetCurrentBranch(
        [Description("Рабочая директория репозитория (опционально)")] string? workingDirectory = null)
    {
        try
        {
            var repositoryPath = ResolveRepositoryPath(workingDirectory);
            using var repository = new Repository(repositoryPath);
            var branch = repository.Head.FriendlyName;

            if (string.IsNullOrWhiteSpace(branch))
            {
                return Task.FromResult("## Current branch\n\nНе удалось определить текущую ветку.");
            }

            return Task.FromResult($"## Current branch\n\n`{branch}`");
        }
        catch (RepositoryNotFoundException)
        {
            return Task.FromResult("## Current branch\n\nНе удалось найти git-репозиторий в указанной директории.");
        }
        catch (Exception ex)
        {
            return Task.FromResult($"## Current branch\n\n[EXCEPTION] {ex.GetType().Name}: {ex.Message}");
        }
    }

    [McpServerTool, Description("Возвращает список всех доступных git-веток в формате Markdown")]
    public static Task<string> GetAllBranches(
        [Description("Рабочая директория репозитория (опционально)")] string? workingDirectory = null)
    {
        try
        {
            var repositoryPath = ResolveRepositoryPath(workingDirectory);
            using var repository = new Repository(repositoryPath);

            var lines = repository.Branches
                .Select(branch => branch.FriendlyName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (lines.Count == 0)
            {
                return Task.FromResult("## All branches\n\nВетки не найдены.");
            }

            var sb = new StringBuilder("## All branches\n\n");

            foreach (var line in lines)
            {
                sb.AppendLine($"- `{line}`");
            }

            return Task.FromResult(sb.ToString().TrimEnd());
        }
        catch (RepositoryNotFoundException)
        {
            return Task.FromResult("## All branches\n\nНе удалось найти git-репозиторий в указанной директории.");
        }
        catch (Exception ex)
        {
            return Task.FromResult($"## All branches\n\n[EXCEPTION] {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static string ResolveRepositoryPath(string? workingDirectory)
    {
        var resolvedWorkingDirectory = ResolveWorkingDirectory(workingDirectory);
        var repositoryPath = Repository.Discover(resolvedWorkingDirectory);

        if (string.IsNullOrWhiteSpace(repositoryPath))
        {
            throw new RepositoryNotFoundException($"Git-репозиторий не найден для директории: {resolvedWorkingDirectory}");
        }

        return repositoryPath;
    }

    private static string ResolveWorkingDirectory(string? workingDirectory)
    {
        if (!string.IsNullOrWhiteSpace(workingDirectory) && Directory.Exists(workingDirectory))
        {
            return Path.GetFullPath(workingDirectory);
        }

        var envCandidates = new[]
        {
            "CLINE_WORKING_DIRECTORY",
            "WORKSPACE_FOLDER",
            "INIT_CWD",
            "PWD"
        };

        foreach (var variable in envCandidates)
        {
            var value = Environment.GetEnvironmentVariable(variable);
            if (!string.IsNullOrWhiteSpace(value) && Directory.Exists(value))
            {
                return Path.GetFullPath(value);
            }
        }

        return Directory.GetCurrentDirectory();
    }
}