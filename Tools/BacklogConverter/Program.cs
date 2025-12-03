using Caskr.Server.Services.BacklogImport;

var inputPath = args.Length > 0
    ? args[0]
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../..", "PRODUCT_BACKLOG_FULL.csv"));
var outputPath = args.Length > 1
    ? args[1]
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../..", "PRODUCT_BACKLOG_AZURE_IMPORT.csv"));
var areaPath = args.Length > 2 ? args[2] : "Caskr";
var iterationPath = args.Length > 3 ? args[3] : "Caskr/Backlog";

if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"Input CSV not found at {inputPath}");
    return 1;
}

var csvContent = await File.ReadAllTextAsync(inputPath);
var converter = new BacklogToAzureDevOpsConverter();
var workItems = converter.ConvertFromCsv(csvContent, areaPath, iterationPath);
var outputCsv = converter.ToAzureCsv(workItems);
await File.WriteAllTextAsync(outputPath, outputCsv);

Console.WriteLine($"Wrote {workItems.Count} work items to {outputPath}");
Console.WriteLine($"Area Path: {areaPath}");
Console.WriteLine($"Iteration Path: {iterationPath}");
return 0;
