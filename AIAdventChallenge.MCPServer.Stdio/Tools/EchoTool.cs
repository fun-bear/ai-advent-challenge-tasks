using ModelContextProtocol.Server;
using System.ComponentModel;

namespace AIAdventChallenge.MCPServer.Stdio.Tools;

[McpServerToolType]
public sealed class EchoTool
{
    [McpServerTool, Description("Возвращает переданную строку без изменений")]
    public static string Echo([Description("Строка для возврата")] string text) => text;
}
