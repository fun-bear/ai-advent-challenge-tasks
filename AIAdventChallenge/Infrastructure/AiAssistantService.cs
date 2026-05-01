using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace AIAdventChallenge.Infrastructure;

/// <summary>
/// AI-ассистент на Semantic Kernel с OpenRouter и MCP tools.
/// </summary>
public sealed class AiAssistantService
{
    private const string SystemPrompt =
        "Ты полезный AI-ассистент. Если для ответа нужны данные/действия, используй доступные инструменты. " +
        "Путь директории для операций с файлами: C:\\github\\ai-advent-challenge-tasks\\AIAdventChallenge\\Handlers. " +
        "Работать с файлами нужно в этой директории." +
        "Путь директории для новых созданных файлов: C:\\github\\ai-advent-challenge-tasks\\AIAdventChallenge\\Reports." +
        "При сохранении нового файла генерируй для него оригинальное название (с GUID), расширение: md.";

    public async Task<string> ProcessMessageAsync(
        IConfiguration configuration,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            throw new ArgumentException("Сообщение пользователя не должно быть пустым.", nameof(userMessage));
        }

        var settings = configuration.GetSection("OpenAISettings");
        var apiKey = settings["ApiKey"] ?? throw new InvalidOperationException("OpenAISettings:ApiKey is missing.");
        var modelName = settings["ModelName"] ?? throw new InvalidOperationException("OpenAISettings:ModelName is missing.");
        var baseUrl = settings["BaseUrl"] ?? throw new InvalidOperationException("OpenAISettings:BaseUrl is missing.");

        var mcpServerProjectPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "AIAdventChallenge.MCPServer.Stdio", "AIAdventChallenge.MCPServer.Stdio.csproj"));

        if (!File.Exists(mcpServerProjectPath))
        {
            throw new FileNotFoundException("Не найден .csproj MCP stdio сервера.", mcpServerProjectPath);
        }

        var builder = Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion(
            modelId: modelName,
            apiKey: apiKey,
            endpoint: new Uri(baseUrl));

        var kernel = builder.Build();

        await using var mcpClientService = await McpClientService.CreateAsync(mcpServerProjectPath);
        var mcpFunctions = await mcpClientService.GetKernelFunctionsAsync();

        kernel.Plugins.AddFromFunctions("mcp", mcpFunctions);

        var prompt = $"""
                     System:
                     {SystemPrompt}

                     User:
                     {userMessage}
                     """;

        var executionSettings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var response = await kernel.InvokePromptAsync(
            prompt,
            new KernelArguments(executionSettings),
            cancellationToken: cancellationToken);

        var text = response.GetValue<string>();
        return string.IsNullOrWhiteSpace(text)
            ? "[Пустой ответ от AI-ассистента]"
            : text;
    }
}
