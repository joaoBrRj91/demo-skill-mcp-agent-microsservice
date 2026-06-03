using System.Text.Json;
using JL.DddScaffold.Mcp.Models;

namespace JL.DddScaffold.Mcp.Tools;

internal static class ScaffoldHelpers
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    internal static List<PropertyDefinition> ParseProperties(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        return JsonSerializer.Deserialize<List<PropertyDefinition>>(json, JsonOpts) ?? [];
    }

    /// <summary>PascalCase → kebab-case: CatalogProduct → catalog-product</summary>
    internal static string ToKebabCase(string name) =>
        System.Text.RegularExpressions.Regex
            .Replace(name, "(?<=[a-z])([A-Z])", "-$1")
            .ToLowerInvariant();

    /// <summary>Simple English pluralization: Order → Orders, Category → Categories</summary>
    internal static string Pluralize(string name)
    {
        if (name.EndsWith("y", StringComparison.Ordinal)
            && name.Length > 1
            && !"aeiou".Contains(name[^2]))
            return name[..^1] + "ies";
        return name + "s";
    }

    /// <summary>C# type declaration with optional '?' suffix when isNullable.</summary>
    internal static string TypeDecl(PropertyDefinition p) =>
        p.IsNullable ? p.Type + "?" : p.Type;

    /// <summary>
    /// Default initializer for property declarations.
    /// Non-nullable reference types (string) get " = default!;", value types get "".
    /// </summary>
    internal static string DefaultInit(PropertyDefinition p)
    {
        if (p.IsNullable) return "";
        return p.Type switch
        {
            "string" => " = default!;",
            _ => ""
        };
    }

    /// <summary>PascalCase → camelCase: Name → name, ProductSku → productSku</summary>
    internal static string ToCamelCase(string name) =>
        string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];

    /// <summary>
    /// Returns whether the property is a value type (int, decimal, bool, DateTime, Guid, long, double).
    /// Value types are not nullable by default and do not need NotEmpty validators.
    /// </summary>
    internal static bool IsValueType(PropertyDefinition p) =>
        p.Type is "int" or "decimal" or "bool" or "DateTime" or "Guid" or "long" or "double" or "float";
}
