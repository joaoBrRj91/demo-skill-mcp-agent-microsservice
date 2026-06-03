using System.ComponentModel;
using ModelContextProtocol.Server;

namespace JL.DddScaffold.Mcp.Tools;

[McpServerToolType]
public static class PreviewScaffoldTool
{
    [McpServerTool]
    [Description(
        "Dry-run of scaffold_aggregate. Returns all file paths and their full generated content " +
        "WITHOUT writing anything to disk. Use this to review the generated code before committing.")]
    public static string PreviewScaffold(
        [Description("Absolute path to the solution's src/ directory")]
        string srcPath,
        [Description("Root namespace of the target solution, e.g. JL.Commerce.Tecnology.Service")]
        string rootNamespace,
        [Description("PascalCase name of the aggregate to preview, e.g. Order")]
        string aggregateName,
        [Description(
            "JSON array of PropertyDefinition — same format as scaffold_aggregate. " +
            "Example: [{\"name\":\"Name\",\"type\":\"string\",\"maxLength\":200}]")]
        string propertiesJson)
    {
        var props = ScaffoldHelpers.ParseProperties(propertiesJson);
        var files = ScaffoldAggregateTool.CollectAllFiles(srcPath, rootNamespace, aggregateName, props);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== Preview: {files.Count} files would be generated for '{aggregateName}' ===");
        sb.AppendLine();

        foreach (var (absPath, content) in files)
        {
            sb.AppendLine($"{'─',60}");
            sb.AppendLine($"FILE: {absPath}");
            sb.AppendLine($"{'─',60}");
            sb.AppendLine(content);
        }

        return sb.ToString();
    }
}
