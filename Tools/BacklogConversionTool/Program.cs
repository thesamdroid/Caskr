using Caskr.server.Utilities;

var inputPath = args.Length > 0 ? args[0] : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "PRODUCT_BACKLOG_FULL.csv");
var outputPath = args.Length > 1 ? args[1] : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "PRODUCT_BACKLOG_AZDO.csv");

if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"Input file not found: {inputPath}");
    return 1;
}

Console.WriteLine($"Reading backlog from {Path.GetFullPath(inputPath)}");
using var reader = new StreamReader(inputPath);
var workItems = AzureDevOpsBacklogConverter.Convert(reader);

Console.WriteLine($"Writing Azure DevOps CSV to {Path.GetFullPath(outputPath)}");
using var writer = new StreamWriter(outputPath);
AzureDevOpsBacklogConverter.WriteAzureCsv(workItems, writer);

Console.WriteLine($"Converted {workItems.Count} work items.");
return 0;
