using System.ComponentModel;
using System.Text.RegularExpressions;
using AIAdventChallenge.MCPServer.Stdio.Contracts;
using MessagePack;
using ModelContextProtocol.Server;

namespace AIAdventChallenge.MCPServer.Stdio.Tools;

[McpServerToolType]
public sealed class ContractValidatorTool
{
    private static readonly Dictionary<string, string> AliasToClrName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["int"]      = "Int32",
        ["long"]     = "Int64",
        ["short"]    = "Int16",
        ["byte"]     = "Byte",
        ["uint"]     = "UInt32",
        ["ulong"]    = "UInt64",
        ["ushort"]   = "UInt16",
        ["float"]    = "Single",
        ["double"]   = "Double",
        ["decimal"]  = "Decimal",
        ["bool"]     = "Boolean",
        ["string"]   = "String",
        ["char"]     = "Char",
        ["object"]   = "Object",
        ["DateTime"] = "DateTime",
    };

    [McpServerTool, Description(
        "Validates that a MessagePack contract from a C# source file is binary-compatible with the reference contract " +
        "stored in this MCP server. Checks that Key indices and field types match; field names are ignored. " +
        "Returns IsValid=true when compatible, or IsValid=false with a description of every mismatch.")]
    public static ContractValidationResult ValidateContract(
        [Description("Full content of the .cs source file that contains the contract class.")] string fileContent,
        [Description("Name of the contract class to validate, e.g. 'OrderContract'.")] string contractName)
    {
        var referenceTypes = typeof(ContractValidatorTool).Assembly
            .GetTypes()
            .Where(t => t.GetCustomAttributes(typeof(MessagePackObjectAttribute), false).Length > 0)
            .ToList();

        var referenceType = referenceTypes.FirstOrDefault(t =>
            t.Name.Equals(contractName, StringComparison.OrdinalIgnoreCase));

        if (referenceType is null)
        {
            var known = string.Join(", ", referenceTypes.Select(t => t.Name));
            return new ContractValidationResult(false,
                $"Unknown contract '{contractName}'. Known contracts: {known}.");
        }

        var referenceSchema = ExtractSchemaFromReflection(referenceType);
        var sourceSchema = ExtractSchemaFromSource(fileContent, contractName);

        if (sourceSchema.Count == 0)
            return new ContractValidationResult(false,
                $"Could not parse any [Key] fields from the provided file for class '{contractName}'. " +
                "Make sure the class uses [Key(N)] attributes on its properties.");

        var errors = new List<string>();

        foreach (var (key, refTypeName) in referenceSchema.OrderBy(kv => kv.Key))
        {
            if (!sourceSchema.TryGetValue(key, out var srcTypeName))
            {
                errors.Add($"Key({key}): missing in source (reference expects '{refTypeName}')");
                continue;
            }

            if (!NormalizeType(srcTypeName).Equals(NormalizeType(refTypeName), StringComparison.OrdinalIgnoreCase))
                errors.Add($"Key({key}): type mismatch — source '{srcTypeName}', reference '{refTypeName}'");
        }

        foreach (var key in sourceSchema.Keys.Where(k => !referenceSchema.ContainsKey(k)).OrderBy(k => k))
            errors.Add($"Key({key}): present in source but not in reference contract");

        return errors.Count == 0
            ? new ContractValidationResult(true, $"Contract '{contractName}' is compatible with the reference.")
            : new ContractValidationResult(false, string.Join("; ", errors));
    }

    private static Dictionary<int, string> ExtractSchemaFromReflection(Type type)
    {
        var schema = new Dictionary<int, string>();
        foreach (var prop in type.GetProperties())
        {
            var keyAttr = prop.GetCustomAttributes(typeof(KeyAttribute), false)
                              .OfType<KeyAttribute>()
                              .FirstOrDefault();
            if (keyAttr?.IntKey is int key)
                schema[key] = prop.PropertyType.Name;
        }
        return schema;
    }

    private static Dictionary<int, string> ExtractSchemaFromSource(string source, string contractName)
    {
        var schema = new Dictionary<int, string>();

        // Locate the target class declaration
        var classMatch = Regex.Match(source, $@"class\s+{Regex.Escape(contractName)}\b[^{{]*\{{", RegexOptions.Singleline);
        if (!classMatch.Success)
            return schema;

        // Extract the class body by counting braces
        int start = classMatch.Index + classMatch.Length - 1;
        int depth = 0;
        int end = start;
        for (int i = start; i < source.Length; i++)
        {
            if (source[i] == '{') depth++;
            else if (source[i] == '}') { depth--; if (depth == 0) { end = i; break; } }
        }

        var classBody = source.Substring(start, end - start + 1);

        // Match [Key(N)] only within this class body
        var pattern = @"\[Key\((\d+)\)\]\s+(?:public\s+)?(\w+\??)\s+\w+";
        foreach (Match match in Regex.Matches(classBody, pattern, RegexOptions.Singleline))
        {
            if (int.TryParse(match.Groups[1].Value, out int key))
                schema[key] = match.Groups[2].Value.TrimEnd('?');
        }

        return schema;
    }

    private static string NormalizeType(string typeName)
    {
        var clean = typeName.TrimEnd('?');
        return AliasToClrName.TryGetValue(clean, out var clrName) ? clrName : clean;
    }
}

public record ContractValidationResult(bool IsValid, string Description);
