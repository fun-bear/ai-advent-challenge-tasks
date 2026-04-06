using ModelContextProtocol.Server;
using System.ComponentModel;

namespace AIAdventChallenge.McpServer.Tools;

[McpServerToolType]
public sealed class HelloTool
{
    [McpServerTool, Description("Приветствует пользователя по имени")]
    public static string Greet(
        [Description("Имя пользователя")] string name)
    {
        return $"Привет, {name}! 👋";
    }

    [McpServerTool, Description("Складывает два числа")]
    public static int Add(
        [Description("Первое число")] int a,
        [Description("Второе число")] int b)
    {
        return a + b;
    }

    [McpServerTool, Description("Возвращает текущую дату и время")]
    public static string GetCurrentTime()
    {
        return DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
    }
}