using AIAdventChallenge.McpServer.Models;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace AIAdventChallenge.McpServer.Tools;

[McpServerToolType]
public sealed class AccountTool
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [McpServerTool, Description("Возвращает информацию по аккаунту пользователя по ID")]
    public static async Task<Account?> GetAccountById(
        [Description("ID пользователя")] long id,
        CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "Accounts", "accounts.json");
        if (!File.Exists(filePath))
        {
            return null;
        }

        await using var stream = File.OpenRead(filePath);
        var accounts = await JsonSerializer.DeserializeAsync<Account[]>(stream, JsonOptions, cancellationToken);
        if (accounts is null || accounts.Length == 0)
        {
            return null;
        }

        return accounts.FirstOrDefault(a => a.Id == id);
    }
}