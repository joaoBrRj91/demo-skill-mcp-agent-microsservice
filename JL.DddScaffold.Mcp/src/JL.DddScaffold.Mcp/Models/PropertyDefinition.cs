using System.Text.Json.Serialization;

namespace JL.DddScaffold.Mcp.Models;

/// <summary>
/// Describes a single property on a DDD aggregate.
/// Passed as a JSON-encoded array in MCP tool parameters.
/// Id, CreatedAt, and UpdatedAt are always auto-generated — do NOT include them.
/// </summary>
public sealed class PropertyDefinition
{
    /// <summary>Property name in PascalCase, e.g. "ProductName"</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    /// <summary>
    /// CLR type keyword or name: string | int | decimal | bool | DateTime | Guid | long | double
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "string";

    /// <summary>EF MaxLength; null = no constraint. Only relevant for string.</summary>
    [JsonPropertyName("maxLength")]
    public int? MaxLength { get; set; }

    /// <summary>Whether the property is required (non-nullable, IsRequired in EF, NotEmpty in validator).</summary>
    [JsonPropertyName("required")]
    public bool Required { get; set; } = true;

    /// <summary>Whether the property type should be nullable (appends '?' and skips NotEmpty validator).</summary>
    [JsonPropertyName("isNullable")]
    public bool IsNullable { get; set; }

    /// <summary>Whether EF should add a unique index on this column.</summary>
    [JsonPropertyName("isUnique")]
    public bool IsUnique { get; set; }

    /// <summary>EF HasPrecision(precision, scale). Only relevant for decimal.</summary>
    [JsonPropertyName("precision")]
    public int? Precision { get; set; }

    /// <summary>EF HasPrecision scale portion. Defaults to 2 when Precision is set.</summary>
    [JsonPropertyName("scale")]
    public int Scale { get; set; } = 2;
}
