using System.Text;
using Microsoft.VisualBasic.FileIO;

namespace Caskr.Server.Services.BacklogImport;

public record BacklogWorkItem(
    string WorkItemType,
    string Title,
    string State,
    string Priority,
    string Tags,
    string Description
);

public record AzureDevOpsWorkItem
{
    public required string WorkItemType { get; init; }
    public required string Title { get; init; }
    public required string AreaPath { get; init; }
    public required string IterationPath { get; init; }
    public string State { get; init; } = "New";
    public string? Priority { get; init; }
    public string? Tags { get; init; }
    public string? Description { get; init; }
    public string? AcceptanceCriteria { get; init; }
}

public class BacklogToAzureDevOpsConverter
{
    private static readonly string[] RequiredHeaders =
    {
        "Work Item Type", "Title", "State", "Priority", "Tags", "Description"
    };

    public IReadOnlyList<AzureDevOpsWorkItem> ConvertFromCsv(string csvContent, string areaPath, string iterationPath)
    {
        if (string.IsNullOrWhiteSpace(csvContent))
        {
            throw new ArgumentException("CSV content cannot be empty", nameof(csvContent));
        }

        using var reader = new StringReader(csvContent);
        return ConvertFromReader(reader, areaPath, iterationPath);
    }

    public string ToAzureCsv(IEnumerable<AzureDevOpsWorkItem> workItems)
    {
        var builder = new StringBuilder();
        var headers = new[]
        {
            "Work Item Type", "Title", "Area Path", "Iteration Path", "State", "Priority", "Tags",
            "Description", "Acceptance Criteria"
        };
        builder.AppendLine(string.Join(',', headers));

        foreach (var item in workItems)
        {
            builder.AppendLine(string.Join(',', headers.Select(h => FormatValue(h switch
            {
                "Work Item Type" => item.WorkItemType,
                "Title" => item.Title,
                "Area Path" => item.AreaPath,
                "Iteration Path" => item.IterationPath,
                "State" => item.State,
                "Priority" => item.Priority,
                "Tags" => item.Tags,
                "Description" => item.Description,
                "Acceptance Criteria" => item.AcceptanceCriteria,
                _ => null
            }))));
        }

        return builder.ToString();
    }

    private IReadOnlyList<AzureDevOpsWorkItem> ConvertFromReader(TextReader reader, string areaPath, string iterationPath)
    {
        var workItems = new List<AzureDevOpsWorkItem>();

        using var parser = new TextFieldParser(reader)
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true
        };
        parser.SetDelimiters(",");

        var headerRow = parser.ReadFields() ?? throw new InvalidDataException("CSV file is missing a header row.");
        var headerLookup = BuildHeaderLookup(headerRow);

        EnsureRequiredHeaders(headerLookup);
        var cleanedAreaPath = Require(areaPath, nameof(areaPath));
        var cleanedIterationPath = Require(iterationPath, nameof(iterationPath));

        while (!parser.EndOfData)
        {
            var row = parser.ReadFields();
            if (row is null || row.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            var backlogItem = new BacklogWorkItem(
                GetFieldValue(row, headerLookup, "Work Item Type"),
                GetFieldValue(row, headerLookup, "Title"),
                GetFieldValue(row, headerLookup, "State"),
                GetFieldValue(row, headerLookup, "Priority"),
                GetFieldValue(row, headerLookup, "Tags"),
                GetFieldValue(row, headerLookup, "Description"));

            workItems.Add(MapToAzure(backlogItem, cleanedAreaPath, cleanedIterationPath));
        }

        return workItems;
    }

    private static AzureDevOpsWorkItem MapToAzure(BacklogWorkItem backlogItem, string areaPath, string iterationPath)
    {
        var normalizedDescription = NormalizeMultiline(backlogItem.Description);
        var (description, acceptanceCriteria) = ExtractAcceptanceCriteria(normalizedDescription);

        return new AzureDevOpsWorkItem
        {
            WorkItemType = Require(backlogItem.WorkItemType, "Work Item Type"),
            Title = Require(backlogItem.Title, "Title"),
            State = string.IsNullOrWhiteSpace(backlogItem.State) ? "New" : backlogItem.State.Trim(),
            Priority = NormalizeOptional(backlogItem.Priority),
            Tags = NormalizeTags(backlogItem.Tags),
            Description = description,
            AcceptanceCriteria = acceptanceCriteria,
            AreaPath = areaPath,
            IterationPath = iterationPath
        };
    }

    private static (string Description, string AcceptanceCriteria) ExtractAcceptanceCriteria(string description)
    {
        const string marker = "acceptance criteria:";
        var index = description.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return (description.Trim(), string.Empty);
        }

        var summary = description[..index].TrimEnd();
        var acceptance = description[(index + marker.Length)..].Trim();
        return (summary, acceptance);
    }

    private static string NormalizeOptional(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    private static string NormalizeMultiline(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
    }

    private static string NormalizeTags(string tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
        {
            return string.Empty;
        }

        var normalized = tags
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t));

        return string.Join(';', normalized);
    }

    private static string GetFieldValue(IReadOnlyList<string> row, IReadOnlyDictionary<string, int> headerLookup, string header)
    {
        return headerLookup.TryGetValue(header, out var index) && index < row.Count
            ? row[index]
            : string.Empty;
    }

    private static IReadOnlyDictionary<string, int> BuildHeaderLookup(IEnumerable<string> headers)
    {
        var lookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var index = 0;
        foreach (var header in headers)
        {
            lookup[header.Trim()] = index;
            index++;
        }

        return lookup;
    }

    private static void EnsureRequiredHeaders(IReadOnlyDictionary<string, int> headerLookup)
    {
        var missing = RequiredHeaders.Where(h => !headerLookup.ContainsKey(h)).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidDataException($"CSV file is missing required column(s): {string.Join(", ", missing)}");
        }
    }

    private static string Require(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{fieldName} cannot be empty");
        }

        return value.Trim();
    }

    private static string FormatValue(string? value)
    {
        value ??= string.Empty;
        value = NormalizeMultiline(value);

        var needsQuotes = value.Contains('"') || value.Contains(',') || value.Contains('\n');
        if (value.Contains('"'))
        {
            value = value.Replace("\"", "\"\"");
        }

        return needsQuotes ? $"\"{value}\"" : value;
    }
}
