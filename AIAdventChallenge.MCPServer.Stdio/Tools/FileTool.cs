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

    [McpServerTool, Description("Создает новый файл в указанной директории с переданным содержимым")]
    public static async Task<FileCreateResult> CreateFile(
        [Description("Путь к директории")] string path,
        [Description("Название файла")] string fileName,
        [Description("Содержимое файла")] string content)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Путь к директории не должен быть пустым.", nameof(path));
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("Название файла не должно быть пустым.", nameof(fileName));
        }

        var fullDirectoryPath = Path.GetFullPath(path);

        if (!Directory.Exists(fullDirectoryPath))
        {
            throw new DirectoryNotFoundException($"Директория не найдена: {fullDirectoryPath}");
        }

        var fullFilePath = Path.Combine(fullDirectoryPath, fileName);
        await File.WriteAllTextAsync(fullFilePath, content ?? string.Empty);

        return new FileCreateResult(
            Name: Path.GetFileName(fullFilePath),
            FullPath: fullFilePath,
            Created: true);
    }

    public sealed record FileEntry(string Name, string Content, DateTime LastModified);
    public sealed record FileCreateResult(string Name, string FullPath, bool Created);
}