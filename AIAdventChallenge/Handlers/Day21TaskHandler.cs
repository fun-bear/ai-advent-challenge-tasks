using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace AIAdventChallenge.Handlers;

/// <summary>
/// День 21.
/// Индексация документов из папки Documents с двумя стратегиями чанкинга и эмбеддингами через локальный Ollama.
/// </summary>
public static partial class Day21TaskHandler
{
    private const string SourceName = "C:\\github\\ai-advent-challenge-tasks\\ABP.Docs";
    private const string OllamaEmbeddingModel = "nomic-embed-text";
    private const int FixedChunkSize = 1200;
    private const int FixedChunkOverlap = 200;
    private const int StructuredChunkSize = 4000;
    private const int StructuredChunkOverlap = 500;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static async Task<IResult> HandleAsync()
    {
        var log = new StringBuilder();

        static void ReportProgress(string message)
        {
            Console.WriteLine($"[Day21] {message}");
        }

        log.AppendLine("=== Day21: Индексация документов ===");
        log.AppendLine($"Ollama embedding model: {OllamaEmbeddingModel}");
        ReportProgress("Старт индексации документов.");

        var documentsPath = Path.Combine(AppContext.BaseDirectory, SourceName);
        if (!Directory.Exists(documentsPath))
        {
            return Results.NotFound($"Папка с документами не найдена: {documentsPath}");
        }

        var files = Directory
            .GetFiles(documentsPath, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)
                           || path.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
                           || path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .OrderBy(Path.GetFileName)
            .ToList();

        if (files.Count == 0)
        {
            return Results.BadRequest($"В папке {documentsPath} нет поддерживаемых документов (.txt/.md/.cs).");
        }

        log.AppendLine($"Документов к индексации: {files.Count}");
        ReportProgress($"Найдено документов: {files.Count}.");

        var docTexts = new List<DocText>();
        for (var fileIndex = 0; fileIndex < files.Count; fileIndex++)
        {
            var file = files[fileIndex];
            ReportProgress($"Чтение и нормализация файла {fileIndex + 1}/{files.Count}: {Path.GetFileName(file)}");
            var text = await File.ReadAllTextAsync(file);
            if (string.IsNullOrWhiteSpace(text))
            {
                ReportProgress($"Пропуск пустого файла: {Path.GetFileName(file)}");
                continue;
            }

            docTexts.Add(new DocText(
                FilePath: file,
                FileName: Path.GetFileName(file),
                Title: Path.GetFileNameWithoutExtension(file),
                Content: NormalizeText(text)));
        }

        if (docTexts.Count == 0)
        {
            return Results.BadRequest("Не удалось прочитать непустой текст из документов.");
        }

        var totalCharacters = docTexts.Sum(d => d.Content.Length);
        var estimatedPages = totalCharacters / 1800.0;
        log.AppendLine($"Суммарный объём текста: {totalCharacters:N0} символов (~{estimatedPages:F1} страниц)");
        ReportProgress($"Подготовлено документов с текстом: {docTexts.Count}. Общий объём: {totalCharacters:N0} символов.");

        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:11434/")
        };
        httpClient.Timeout = TimeSpan.FromMinutes(30);

        //var fixedChunks = BuildFixedChunks(docTexts);
        //log.AppendLine($"Fixed-size чанков: {fixedChunks.Count}");

        ReportProgress("Запуск structured чанкинга...");
        var structuredChunks = BuildStructuredChunks(docTexts, ReportProgress);
        log.AppendLine($"Structured чанков: {structuredChunks.Count}");
        ReportProgress($"Structured чанкинг завершён. Получено чанков: {structuredChunks.Count}.");

        //await EnrichWithEmbeddingsAsync(httpClient, fixedChunks);
        ReportProgress("Начинаю построение эмбеддингов...");
        await EnrichWithEmbeddingsAsync(httpClient, structuredChunks, ReportProgress);
        ReportProgress("Построение эмбеддингов завершено.");

        var outputDir = Path.Combine(AppContext.BaseDirectory, "Day31Indexes");
        Directory.CreateDirectory(outputDir);

        //var fixedIndex = BuildIndexResult("fixed_size", fixedChunks, totalCharacters, estimatedPages);
        var structuredIndex = BuildIndexResult("structured", structuredChunks, totalCharacters, estimatedPages);
        //var comparison = BuildComparison(fixedIndex, structuredIndex);

        //fixedIndex.Comparison = comparison;
        //structuredIndex.Comparison = comparison;

        //var fixedPath = Path.Combine(outputDir, "day31_index_fixed.json");
        var structuredPath = Path.Combine(outputDir, "day31_index_structured.json");
        //var comparisonPath = Path.Combine(outputDir, "day31_chunking_comparison.json");

        //await File.WriteAllTextAsync(fixedPath, JsonSerializer.Serialize(fixedIndex, JsonOptions));
        await File.WriteAllTextAsync(structuredPath, JsonSerializer.Serialize(structuredIndex, JsonOptions));
        //await File.WriteAllTextAsync(comparisonPath, JsonSerializer.Serialize(comparison, JsonOptions));

        log.AppendLine();
        log.AppendLine("✅ Индексация завершена.");
        //log.AppendLine($"Fixed index: {fixedPath}");
        log.AppendLine($"Structured index: {structuredPath}");
        ReportProgress($"Индексация завершена. Результат: {structuredPath}");
        //log.AppendLine($"Comparison: {comparisonPath}");

        return Results.Content(log.ToString());
    }

    private static List<ChunkRecord> BuildFixedChunks(IEnumerable<DocText> docs)
    {
        var chunks = new List<ChunkRecord>();

        foreach (var doc in docs)
        {
            var offset = 0;
            var index = 0;
            while (offset < doc.Content.Length)
            {
                var size = Math.Min(FixedChunkSize, doc.Content.Length - offset);
                var text = doc.Content.Substring(offset, size).Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    chunks.Add(new ChunkRecord
                    {
                        ChunkId = $"{Path.GetFileNameWithoutExtension(doc.FileName)}-fixed-{index:D4}",
                        Source = SourceName,
                        File = doc.FileName,
                        Title = doc.Title,
                        Section = "fixed_window",
                        Strategy = "fixed_size",
                        Text = text
                    });
                    index++;
                }

                if (offset + size >= doc.Content.Length)
                {
                    break;
                }

                offset += Math.Max(1, size - FixedChunkOverlap);
            }
        }

        return chunks;
    }

    private static List<ChunkRecord> BuildStructuredChunks(IEnumerable<DocText> docs, Action<string>? reportProgress = null)
    {
        var chunks = new List<ChunkRecord>();

        foreach (var doc in docs)
        {
            reportProgress?.Invoke($"Чанкинг файла: {doc.FileName}");
            var sections = SplitSections(doc.Content);
            var index = 0;

            foreach (var section in sections)
            {
                var windows = SplitByWindow(section.Content, StructuredChunkSize, StructuredChunkOverlap);
                foreach (var window in windows)
                {
                    if (string.IsNullOrWhiteSpace(window))
                    {
                        continue;
                    }

                    chunks.Add(new ChunkRecord
                    {
                        ChunkId = $"{Path.GetFileNameWithoutExtension(doc.FileName)}-struct-{index:D4}",
                        Source = SourceName,
                        File = doc.FileName,
                        Title = doc.Title,
                        Section = section.Title,
                        Strategy = "structured",
                        Text = window.Trim()
                    });
                    index++;
                }
            }
        }

        return chunks;
    }

    private static async Task EnrichWithEmbeddingsAsync(HttpClient httpClient, List<ChunkRecord> chunks, Action<string>? reportProgress = null)
    {
        string? lastFile = null;

        for (var i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            if (!string.Equals(lastFile, chunk.File, StringComparison.OrdinalIgnoreCase))
            {
                lastFile = chunk.File;
                reportProgress?.Invoke($"Эмбеддинги для файла: {chunk.File}");
            }

            if ((i + 1) % 25 == 0 || i == 0 || i + 1 == chunks.Count)
            {
                reportProgress?.Invoke($"Эмбеддинги: {i + 1}/{chunks.Count}");
            }

            chunks[i].Embedding = await GetEmbeddingAsync(httpClient, chunks[i].Text);
        }
    }

    private static async Task<List<float>> GetEmbeddingAsync(HttpClient httpClient, string text)
    {
        var embedRequest = JsonSerializer.Serialize(new
        {
            model = OllamaEmbeddingModel,
            input = text
        });

        using var embedContent = new StringContent(embedRequest, Encoding.UTF8, "application/json");
        using var embedResponse = await httpClient.PostAsync("api/embed", embedContent);

        if (embedResponse.IsSuccessStatusCode)
        {
            var embedBody = await embedResponse.Content.ReadAsStringAsync();
            var apiEmbedResponse = JsonSerializer.Deserialize<OllamaEmbedResponse>(embedBody, JsonOptions);
            var first = apiEmbedResponse?.Embeddings?.FirstOrDefault();
            if (first is not null && first.Count > 0)
            {
                return first;
            }
        }

        var legacyRequest = JsonSerializer.Serialize(new
        {
            model = OllamaEmbeddingModel,
            prompt = text
        });

        using var legacyContent = new StringContent(legacyRequest, Encoding.UTF8, "application/json");
        using var legacyResponse = await httpClient.PostAsync("api/embeddings", legacyContent);
        legacyResponse.EnsureSuccessStatusCode();

        var legacyBody = await legacyResponse.Content.ReadAsStringAsync();
        var apiLegacyResponse = JsonSerializer.Deserialize<OllamaLegacyEmbeddingResponse>(legacyBody, JsonOptions);

        return apiLegacyResponse?.Embedding ?? throw new InvalidOperationException("Ollama вернул пустой эмбеддинг.");
    }

    private static List<SectionPart> SplitSections(string content)
    {
        var lines = content.Split('\n');
        var sections = new List<SectionPart>();

        var currentTitle = "preamble";
        var buffer = new StringBuilder();

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r').Trim();
            if (IsSectionHeading(line))
            {
                AddSectionIfNotEmpty(sections, currentTitle, buffer.ToString());
                buffer.Clear();
                currentTitle = line;
                continue;
            }

            buffer.AppendLine(rawLine.TrimEnd('\r'));
        }

        AddSectionIfNotEmpty(sections, currentTitle, buffer.ToString());

        if (sections.Count == 0)
        {
            sections.Add(new SectionPart("full_document", content));
        }

        return sections;
    }

    private static List<string> SplitByWindow(string text, int chunkSize, int overlap)
    {
        var normalized = text.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return [];
        }

        var result = new List<string>();
        var offset = 0;

        while (offset < normalized.Length)
        {
            var size = Math.Min(chunkSize, normalized.Length - offset);
            var piece = normalized.Substring(offset, size).Trim();
            if (!string.IsNullOrWhiteSpace(piece))
            {
                result.Add(piece);
            }

            if (offset + size >= normalized.Length)
            {
                break;
            }

            offset += Math.Max(1, size - overlap);
        }

        return result;
    }

    private static void AddSectionIfNotEmpty(List<SectionPart> sections, string title, string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            sections.Add(new SectionPart(title, text));
        }
    }

    private static bool IsSectionHeading(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        return line.StartsWith("# ", StringComparison.Ordinal);
    }

    private static string NormalizeText(string text)
    {
        var normalized = text
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Replace("\u000c", "\n")
            .Replace("\t", " ");

        var lines = normalized
            .Split('\n')
            .Select(line => Regex.Replace(line, " {2,}", " ").Trim())
            .ToList();

        // Сжимаем серии пустых строк до одной, чтобы не терять структуру,
        // но и не раздувать текст лишними переносами.
        var resultLines = new List<string>(lines.Count);
        var prevEmpty = false;

        foreach (var line in lines)
        {
            var isEmpty = string.IsNullOrWhiteSpace(line);
            if (isEmpty && prevEmpty)
            {
                continue;
            }

            resultLines.Add(line);
            prevEmpty = isEmpty;
        }

        return string.Join('\n', resultLines).Trim();
    }

    private static IndexResult BuildIndexResult(
        string strategy,
        List<ChunkRecord> chunks,
        int totalCharacters,
        double estimatedPages)
    {
        return new IndexResult
        {
            Strategy = strategy,
            EmbeddingModel = OllamaEmbeddingModel,
            CreatedAtUtc = DateTime.UtcNow,
            TotalDocuments = chunks.Select(c => c.File).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            TotalChunks = chunks.Count,
            TotalCharacters = totalCharacters,
            EstimatedPages = estimatedPages,
            AverageChunkLength = chunks.Count == 0 ? 0 : chunks.Average(c => c.Text.Length),
            Chunks = chunks
        };
    }

    private static ChunkingComparison BuildComparison(IndexResult fixedIndex, IndexResult structuredIndex)
    {
        return new ChunkingComparison
        {
            GeneratedAtUtc = DateTime.UtcNow,
            FixedSize = new ComparisonItem(
                fixedIndex.TotalChunks,
                fixedIndex.AverageChunkLength,
                fixedIndex.Chunks.Select(c => c.Section).Distinct(StringComparer.OrdinalIgnoreCase).Count()),
            Structured = new ComparisonItem(
                structuredIndex.TotalChunks,
                structuredIndex.AverageChunkLength,
                structuredIndex.Chunks.Select(c => c.Section).Distinct(StringComparer.OrdinalIgnoreCase).Count()),
            Notes =
            [
                "fixed_size: стабилен и предсказуем по длине чанков, но может разрывать смысловые блоки.",
                "structured: лучше сохраняет семантические границы разделов, но размер чанков менее равномерен."
            ]
        };
    }

    private sealed record DocText(string FilePath, string FileName, string Title, string Content);

    private sealed record SectionPart(string Title, string Content);

    private sealed class ChunkRecord
    {
        [JsonPropertyName("chunk_id")]
        public required string ChunkId { get; init; }

        [JsonPropertyName("source")]
        public required string Source { get; init; }

        [JsonPropertyName("file")]
        public required string File { get; init; }

        [JsonPropertyName("title")]
        public required string Title { get; init; }

        [JsonPropertyName("section")]
        public required string Section { get; init; }

        [JsonPropertyName("strategy")]
        public required string Strategy { get; init; }

        [JsonPropertyName("text")]
        public required string Text { get; init; }

        [JsonPropertyName("embedding")]
        public List<float>? Embedding { get; set; }
    }

    private sealed class IndexResult
    {
        [JsonPropertyName("strategy")]
        public required string Strategy { get; init; }

        [JsonPropertyName("embedding_model")]
        public required string EmbeddingModel { get; init; }

        [JsonPropertyName("created_at_utc")]
        public DateTime CreatedAtUtc { get; init; }

        [JsonPropertyName("total_documents")]
        public int TotalDocuments { get; init; }

        [JsonPropertyName("total_chunks")]
        public int TotalChunks { get; init; }

        [JsonPropertyName("total_characters")]
        public int TotalCharacters { get; init; }

        [JsonPropertyName("estimated_pages")]
        public double EstimatedPages { get; init; }

        [JsonPropertyName("avg_chunk_length")]
        public double AverageChunkLength { get; init; }

        [JsonPropertyName("chunks")]
        public required List<ChunkRecord> Chunks { get; init; }

        [JsonPropertyName("comparison")]
        public ChunkingComparison? Comparison { get; set; }
    }

    private sealed class ChunkingComparison
    {
        [JsonPropertyName("generated_at_utc")]
        public DateTime GeneratedAtUtc { get; init; }

        [JsonPropertyName("fixed_size")]
        public required ComparisonItem FixedSize { get; init; }

        [JsonPropertyName("structured")]
        public required ComparisonItem Structured { get; init; }

        [JsonPropertyName("notes")]
        public required List<string> Notes { get; init; }
    }

    private sealed record ComparisonItem(
        [property: JsonPropertyName("chunks")] int Chunks,
        [property: JsonPropertyName("avg_chunk_length")] double AvgChunkLength,
        [property: JsonPropertyName("distinct_sections")] int DistinctSections);

    private sealed class OllamaEmbedResponse
    {
        [JsonPropertyName("embeddings")]
        public List<List<float>>? Embeddings { get; init; }
    }

    private sealed class OllamaLegacyEmbeddingResponse
    {
        [JsonPropertyName("embedding")]
        public List<float>? Embedding { get; init; }
    }
}