using System.ComponentModel;
using ModelContextProtocol.Server;

namespace JL.DddScaffold.Mcp.Tools;

[McpServerToolType]
public static class ListAggregatesTool
{
    [McpServerTool]
    [Description(
        "Scans the Domain/Aggregates/ directory of a solution and lists all existing aggregates " +
        "with their strongly-typed ID status, command count, and query count.")]
    public static string ListAggregates(
        [Description("Absolute path to the solution's src/ directory, e.g. C:/Projects/MyService/src")]
        string srcPath)
    {
        var aggregatesPath = Path.Combine(srcPath, "Domain", "Aggregates");
        if (!Directory.Exists(aggregatesPath))
            return $"No Domain/Aggregates/ directory found at: {aggregatesPath}";

        var aggregateDirs = Directory.GetDirectories(aggregatesPath);
        if (aggregateDirs.Length == 0)
            return $"No aggregates found in {aggregatesPath}";

        var commandsPath = Path.Combine(srcPath, "Application", "Commands");
        var queriesPath  = Path.Combine(srcPath, "Application", "Queries");

        var lines = new List<string>();
        foreach (var dir in aggregateDirs.OrderBy(d => d))
        {
            var aggName = Path.GetFileName(dir);
            var hasId   = File.Exists(Path.Combine(dir, $"{aggName}Id.cs"));
            var hasAgg  = File.Exists(Path.Combine(dir, $"{aggName}.cs"));

            var commandCount = Directory.Exists(commandsPath)
                ? Directory.GetDirectories(commandsPath)
                      .Count(d => Path.GetFileName(d).Contains(aggName, StringComparison.OrdinalIgnoreCase))
                : 0;

            var queryCount = Directory.Exists(queriesPath)
                ? Directory.GetDirectories(queriesPath)
                      .Count(d => Path.GetFileName(d).Contains(aggName, StringComparison.OrdinalIgnoreCase))
                : 0;

            lines.Add(
                $"  {aggName,-30} " +
                $"Aggregate: {(hasAgg  ? "yes" : "NO "),-4}  " +
                $"Id: {(hasId  ? "yes" : "NO "),-4}  " +
                $"Commands: {commandCount,-3}  " +
                $"Queries: {queryCount}");
        }

        return $"Found {aggregateDirs.Length} aggregate(s) in {aggregatesPath}:\n\n" +
               string.Join("\n", lines);
    }
}
