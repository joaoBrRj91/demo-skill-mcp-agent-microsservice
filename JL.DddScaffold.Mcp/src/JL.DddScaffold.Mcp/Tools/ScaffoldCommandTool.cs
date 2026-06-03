using System.ComponentModel;
using JL.DddScaffold.Mcp.Models;
using ModelContextProtocol.Server;

namespace JL.DddScaffold.Mcp.Tools;

[McpServerToolType]
public static class ScaffoldCommandTool
{
    [McpServerTool]
    [Description(
        "Adds a single custom command (command record + handler + validator) to an existing aggregate. " +
        "Use for domain-specific operations beyond standard CRUD, e.g. ActivateOrder, ApproveInvoice. " +
        "The command class is named {AggregateName}{OperationName}Command.")]
    public static string ScaffoldCommand(
        [Description("Absolute path to the solution's src/ directory")]
        string srcPath,
        [Description("Root namespace of the target solution, e.g. JL.Commerce.Tecnology.Service")]
        string rootNamespace,
        [Description("PascalCase name of the existing aggregate, e.g. Order")]
        string aggregateName,
        [Description(
            "PascalCase name of the operation WITHOUT the aggregate prefix, e.g. Approve or Activate. " +
            "The command class will be {AggregateName}{OperationName}Command.")]
        string operationName,
        [Description("true = IRequest<Guid> (returns a new id or value), false = IRequest (void mutation)")]
        bool returnsGuid,
        [Description(
            "JSON array of PropertyDefinition for the command's parameters. " +
            "Include Guid Id as first parameter for operations on existing entities. " +
            "Example: [{\"name\":\"Id\",\"type\":\"Guid\"},{\"name\":\"Reason\",\"type\":\"string\",\"maxLength\":500}]")]
        string propertiesJson)
    {
        var props = ScaffoldHelpers.ParseProperties(propertiesJson);
        var commandName = $"{aggregateName}{operationName}";
        var folder = $"Application/Commands/{commandName}";

        var files = new List<(string rel, string content)>
        {
            GenerateCommand(commandName, rootNamespace, props, returnsGuid),
            GenerateHandler(commandName, aggregateName, rootNamespace, props, returnsGuid),
            GenerateValidator(commandName, rootNamespace, props)
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
        sb.AppendLine($"scaffold_command completed for '{commandName}Command'.");
        sb.AppendLine($"Written ({written.Count}):");
        foreach (var f in written) sb.AppendLine($"  + {f}");
        if (skipped.Count > 0)
        {
            sb.AppendLine($"Skipped ({skipped.Count}):");
            foreach (var f in skipped) sb.AppendLine($"  ~ {f}");
        }
        sb.AppendLine($"\nNEXT STEP: Add the handler method to the {aggregateName} aggregate if it doesn't exist yet.");
        return sb.ToString();
    }

    private static (string, string) GenerateCommand(
        string commandName, string rootNs, List<PropertyDefinition> props, bool returnsGuid)
    {
        var ns = $"{rootNs}.Application.Commands.{commandName}";
        var returnType = returnsGuid ? "IRequest<Guid>" : "IRequest";
        var recordParams = string.Join(",\n    ", props.Select(p =>
            $"{ScaffoldHelpers.TypeDecl(p)} {p.Name}"));

        return ($"Application/Commands/{commandName}/{commandName}Command.cs", $$"""
using MediatR;

namespace {{ns}};

public sealed record {{commandName}}Command(
    {{recordParams}}) : {{returnType}};
""");
    }

    private static (string, string) GenerateHandler(
        string commandName, string aggregateName, string rootNs,
        List<PropertyDefinition> props, bool returnsGuid)
    {
        var ns = $"{rootNs}.Application.Commands.{commandName}";
        var portsNs = $"{rootNs}.Application.Ports";
        var aggregateNs = $"{rootNs}.Domain.Aggregates.{aggregateName}";
        var returnType = returnsGuid ? $"IRequestHandler<{commandName}Command, Guid>" : $"IRequestHandler<{commandName}Command>";
        var returnSig   = returnsGuid ? "Task<Guid>" : "Task";
        var returnStmt  = returnsGuid ? "\n        // TODO: return the resulting Guid\n        throw new NotImplementedException();" : "        // TODO: implement command logic";

        return ($"Application/Commands/{commandName}/{commandName}CommandHandler.cs", $$"""
using {{portsNs}};
using {{aggregateNs}};
using MediatR;

namespace {{ns}};

public sealed class {{commandName}}CommandHandler(I{{aggregateName}}Repository repository)
    : {{returnType}}
{
    public async {{returnSig}} Handle({{commandName}}Command cmd, CancellationToken ct)
    {
{{returnStmt}}
    }
}
""");
    }

    private static (string, string) GenerateValidator(
        string commandName, string rootNs, List<PropertyDefinition> props)
    {
        var ns = $"{rootNs}.Application.Commands.{commandName}";
        var rules = new List<string>();

        foreach (var p in props)
        {
            if (p.Type == "Guid" && p.Name == "Id")
                rules.Add("        RuleFor(x => x.Id)\n            .NotEmpty().WithMessage(\"Id is required.\");");
            else if (p.Type == "string" && p.Required && !p.IsNullable)
            {
                var r = $"        RuleFor(x => x.{p.Name})\n            .NotEmpty().WithMessage(\"{p.Name} is required.\")";
                if (p.MaxLength.HasValue)
                    r += $"\n            .MaximumLength({p.MaxLength}).WithMessage(\"{p.Name} must not exceed {p.MaxLength} characters.\")";
                rules.Add(r + ";");
            }
        }

        var rulesBlock = rules.Count > 0 ? "\n" + string.Join("\n\n", rules) + "\n    " : "";

        return ($"Application/Commands/{commandName}/{commandName}CommandValidator.cs", $$"""
using FluentValidation;

namespace {{ns}};

public sealed class {{commandName}}CommandValidator : AbstractValidator<{{commandName}}Command>
{
    public {{commandName}}CommandValidator()
    {{{rulesBlock}}}
}
""");
    }
}
