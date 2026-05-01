using ModelContextProtocol.Server;
using System.ComponentModel;

namespace AIAdventChallenge.MCPServer.Stdio.Tools;

[McpServerToolType]
public sealed class FileTool
{
    [McpServerTool, Description("Возвращает список файлов в директории с названием, содержимым и датой изменения")]
    public static async Task<IReadOnlyCollection<FileEntry>> GetFiles(
        [Description("Путь к директории")] string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Путь к директории не должен быть пустым.", nameof(path));
        }

        var fullPath = Path.GetFullPath(path);

        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"Директория не найдена: {fullPath}");
        }
        
        var fileInfos = Directory
            .EnumerateFiles(fullPath)
            .Select(filePath => new FileInfo(filePath))
            .ToArray();

        var tasks = fileInfos.Select(async fileInfo =>
        {
            var content = await File.ReadAllTextAsync(fileInfo.FullName);

            return new FileEntry(
                Name: fileInfo.Name,
                Content: content,
                LastModified: fileInfo.LastWriteTimeUtc);
        });

        var files = await Task.WhenAll(tasks);
        return files;
    }

    public sealed record FileEntry(string Name, string Content, DateTime LastModified);
}