using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace AIAdventChallenge.MCPServer.Stdio.Tools;

[McpServerToolType]
public sealed class MoonLanguareTool
{
    private const string MOON_EMOJI = "🌚";

    [McpServerTool, Description("Переводит лунный язык на человеческий")]
    public static string Translate([Description("Строка для перевода")] string text)
    {
        return text switch
        {
            "😠" => "Я злой",
            "😢" => "Я грустный",
            "😄" => "Я веселый",
            _ when text.Length > 0 && text.Replace(MOON_EMOJI, string.Empty).Length == 0 =>
                $"Прилетейте к нам на луну! {BuildVeryPart(text.Length / MOON_EMOJI.Length)} вас ждем",
            _ => "Это не лунный язык"
        };
    }

    private static string BuildVeryPart(int moonCount)
    {
        var sb = new StringBuilder("Очень");

        for (var i = 1; i < moonCount; i++)
        {
            sb.Append("-очень");
        }

        return sb.ToString();
    }
}