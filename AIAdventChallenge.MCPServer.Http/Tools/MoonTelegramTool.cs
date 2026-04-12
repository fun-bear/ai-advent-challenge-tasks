using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;

namespace AIAdventChallenge.McpServer.Tools;

[McpServerToolType]
public sealed class MoonTelegramTool
{
    private static readonly Regex BroccoliWordRegex = new(@"\bbroccoli\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex MoonWordRegex = new(@"\bmoon\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    // Проводит цензуру сообщения: broccoli -> pancake, и при отсутствии moon добавляет фразу I love Moon!
    [McpServerTool, Description("Проводит цензуру сообщения")]
    public static string CensorMessage(
        [Description("Исходное сообщение")] string message)
    {
        var sourceMessage = message ?? string.Empty;
        var hasMoonInSource = MoonWordRegex.IsMatch(sourceMessage);

        var censored = BroccoliWordRegex.Replace(sourceMessage, "pancake");

        if (!hasMoonInSource)
        {
            censored = string.IsNullOrWhiteSpace(censored)
                ? "I love Moon!"
                : $"{censored} I love Moon!";
        }

        return censored;
    }

    // Кодирует сообщение в base64
    [McpServerTool, Description("Шифрует сообщение")]
    public static string Encrypt(
        [Description("Исходное сообщение")] string message)
    {
        var sourceMessage = message ?? string.Empty;
        var bytes = Encoding.UTF8.GetBytes(sourceMessage);
        return Convert.ToBase64String(bytes);
    }

    // Отправляет сообщение на Луну: декодирует base64, проверяет правила и возвращает emoji-ответ
    [McpServerTool, Description("Отправляет сообщение на Луну и возвращает ответ")]
    public static string SendToMoon(
        [Description("Зашифрованное сообщение")] string encryptedMessage)
    {
        string message;

        try
        {
            var bytes = Convert.FromBase64String(encryptedMessage ?? string.Empty);
            message = Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return "Ошибка дешифрации";
        }

        if (Regex.IsMatch(message, @"\bbroccoli\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            return "😠";
        }

        var moonCount = Regex.Matches(message, @"\bmoon\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Count;

        if (moonCount == 0)
        {
            return "😢";
        }

        if (moonCount == 1)
        {
            return "😄";
        }

        return string.Concat(Enumerable.Repeat("🌚", moonCount));
    }
}