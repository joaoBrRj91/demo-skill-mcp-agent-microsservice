using System.ComponentModel;
using JL.DddScaffold.Mcp.Models;
using ModelContextProtocol.Server;

namespace JL.DddScaffold.Mcp.Tools;

[McpServerToolType]
public static class ScaffoldQueryTool
{
    [McpServerTool]
    [Description(
        "Adds a single custom query (query record + handler) to an existing aggregate. " +
        "The query class is named {AggregateName}{OperationName}Query.")]
    public static string ScaffoldQuery(
        [Description("Absolute path to the solution's src/ directory")]
        string srcPath,
        [Description("Root namespace of the target solution, e.g. JL.Commerce.Tecnology.Service")]
        string rootNamespace,
        [Description("PascalCase name of the existing aggregate, e.g. Order")]
        string aggregateName,
        [Description(
            "PascalCase name of the query operation, e.g. GetByStatus or GetByCustomer. " +
            "The query class will be {AggregateName}{OperationName}Query.")]
        string operationName,
        [Description(
            "true = returns a single item ({AggregateName}Dto?), " +
            "false = returns a list (IReadOnlyList<{AggregateName}Dto>)")]
        bool returnsSingleItem,
        [Description(
            "JSON array of PropertyDefinition for the query's filter parameters. " +
            "Example: [{\"name\":\"Status\",\"type\":\"string\"}]")]
        string parametersJson)
    {
        var props = ScaffoldHelpers.ParseProperties(parametersJson);
        var queryName = $"{aggregateName}{operationName}";

        var files = new List<(string rel, string content)>
        {
            GenerateQuery(queryName, aggregateName, rootNamespace, props, returnsSingleItem),
            GenerateHandler(queryName, aggregateName, rootNamespace, props, returnsSingleItem)
        };

        var written = new List<string>();
        var skipped = new List<string>();

        foreach (var (rel, fileContent) in files)
        {
            var absPath = Path.Combine(srcPath, rel.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(absPath)) { skipped.Add(absPath); continue; }
            Directory.CreateDirectory(Path.GetDirectoryName(absPath)!);
            File.WriteAllText(absPath, fileContent, System.Text.Encoding.UTF8);
            written.Add(absPath);
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"scaffold_query completed for '{queryName}Query'.");
        sb.AppendLine($"Written ({written.Count}):");
        foreach (var f in written) sb.AppendLine($"  + {f}");
        if (skipped.Count > 0)
        {
            sb.AppendLine($"Skipped ({skipped.Count}):");
            foreach (var f in skipped) sb.AppendLine($"  ~ {f}");
        }
        sb.AppendLine($"\nNEXT STEP: Add the corresponding repository method to I{aggregateName}Repository if needed.");
        return sb.ToString();
    }

    private static (string, string) GenerateQuery(
        string queryName, string aggregateName, string rootNs,
        List<PropertyDefinition> props, bool returnsSingleItem)
    {
        var ns = $"{rootNs}.Application.Queries.{queryName}";
        var dtosNs = $"{rootNs}.Application.DTOs";
        var returnType = returnsSingleItem
            ? $"IRequest<{aggregateName}Dto?>"
            : $"IRequest<IReadOnlyList<{aggregateName}Dto>>";

        var recordParams = props.Count > 0
            ? string.Join(", ", props.Select(p => $"{ScaffoldHelpers.TypeDecl(p)} {p.Name}"))
            : "";
        var recordDecl = recordParams.Length > 0
            ? $"public sealed record {queryName}Query({recordParams}) : {returnType};"
            : $"public sealed record {queryName}Query : {returnType};";

        return ($"Application/Queries/{queryName}/{queryName}Query.cs", $$"""
using {{dtosNs}};
using MediatR;

namespace {{ns}};

{{recordDecl}}
""");
    }

    private static (string, string) GenerateHandler(
        string queryName, string aggregateName, string rootNs,
        List<PropertyDefinition> props, bool returnsSingleItem)
    {
        var ns = $"{rootNs}.Application.Queries.{queryName}";
        var dtosNs = $"{rootNs}.Application.DTOs";
        var portsNs = $"{rootNs}.Application.Ports";
        var returnType = returnsSingleItem
            ? $"{aggregateName}Dto?"
            : $"IReadOnlyList<{aggregateName}Dto>";
        var handlerBase = $"IRequestHandler<{queryName}Query, {returnType}>";

        return ($"Application/Queries/{queryName}/{queryName}QueryHandler.cs", $$"""
using AutoMapper;
using {{dtosNs}};
using {{portsNs}};
using MediatR;

namespace {{ns}};

public sealed class {{queryName}}QueryHandler(I{{aggregateName}}Repository repository, IMapper mapper)
    : {{handlerBase}}
{
    public async Task<{{returnType}}> Handle({{queryName}}Query query, CancellationToken ct)
    {
        // TODO: implement query logic using repository
        throw new NotImplementedException();
    }
}
""");
    }
}
