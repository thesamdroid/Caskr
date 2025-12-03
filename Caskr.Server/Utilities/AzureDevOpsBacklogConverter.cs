using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualBasic.FileIO;

namespace Caskr.server.Utilities;

public record AzureDevOpsWorkItem(
    string WorkItemType,
    string Title,
    string State,
    string Priority,
    string Tags,
    string Description,
    string AcceptanceCriteria);

public static class AzureDevOpsBacklogConverter
{
    private static readonly string[] RequiredFields =
    {
        "Work Item Type",
        "Title",
        "State",
        "Priority",
        "Tags",
        "Description"
    };

    public static IReadOnlyList<AzureDevOpsWorkItem> Convert(TextReader reader)
    {
        using var parser = CreateParser(reader);
        var headers = parser.ReadFields();
        if (headers is null)
        {
            throw new InvalidDataException("Source CSV has no header row.");
        }

        var headerLookup = headers
            .Select((name, index) => (name, index))
            .ToDictionary(x => x.name, x => x.index, StringComparer.OrdinalIgnoreCase);

        var missingHeaders = RequiredFields.Where(field => !headerLookup.ContainsKey(field)).ToList();
        if (missingHeaders.Count > 0)
        {
            throw new InvalidDataException($"Source CSV is missing required columns: {string.Join(", ", missingHeaders)}");
        }

        var results = new List<AzureDevOpsWorkItem>();
        while (!parser.EndOfData)
        {
            var fields = parser.ReadFields();
            if (fields is null || fields.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            string GetField(string name) => headerLookup.TryGetValue(name, out var index) && index < fields.Length
                ? fields[index] ?? string.Empty
                : string.Empty;

            var (description, acceptanceCriteria) = SplitAcceptance(GetField("Description"));
            results.Add(new AzureDevOpsWorkItem(
                WorkItemType: NormalizeWorkItemType(GetField("Work Item Type")),
                Title: GetField("Title").Trim(),
                State: NormalizeState(GetField("State")),
                Priority: NormalizePriority(GetField("Priority")),
                Tags: NormalizeTags(GetField("Tags")),
                Description: description,
                AcceptanceCriteria: acceptanceCriteria));
        }

        return results;
    }

    public static void WriteAzureCsv(IEnumerable<AzureDevOpsWorkItem> items, TextWriter writer)
    {
        var columns = new[]
        {
            "Work Item Type",
            "Title",
            "State",
            "Priority",
            "Tags",
            "Description",
            "Acceptance Criteria"
        };

        writer.WriteLine(string.Join(',', columns.Select(Quote)));
        foreach (var item in items)
        {
            writer.WriteLine(string.Join(',', new[]
            {
                Quote(item.WorkItemType),
                Quote(item.Title),
                Quote(item.State),
                Quote(item.Priority),
                Quote(item.Tags),
                Quote(NormalizeNewLines(item.Description)),
                Quote(NormalizeNewLines(item.AcceptanceCriteria))
            }));
        }
    }

    private static TextFieldParser CreateParser(TextReader reader)
    {
        var parser = new TextFieldParser(reader)
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true
        };
        parser.SetDelimiters(",");
        parser.TrimWhiteSpace = false;
        return parser;
    }

    internal static (string Description, string AcceptanceCriteria) SplitAcceptance(string rawDescription)
    {
        if (string.IsNullOrWhiteSpace(rawDescription))
        {
            return (string.Empty, string.Empty);
        }

        const string marker = "Acceptance Criteria:";
        var index = rawDescription.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return (rawDescription.Trim(), string.Empty);
        }

        var description = rawDescription[..index].TrimEnd();
        var acceptance = rawDescription[(index + marker.Length)..].Trim();
        return (description, acceptance);
    }

    private static string NormalizeWorkItemType(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException("Work Item Type cannot be empty.");
        }

        var trimmed = value.Trim();
        return trimmed.Equals("Product Backlog Item", StringComparison.OrdinalIgnoreCase)
            ? "User Story"
            : trimmed;
    }

    private static string NormalizePriority(string value) => value.Trim();

    private static string NormalizeTags(string value)
    {
        var tags = value
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(tag => tag.Trim())
            .Where(tag => !string.IsNullOrWhiteSpace(tag));
        return string.Join(';', tags);
    }

    private static string NormalizeState(string value) => string.IsNullOrWhiteSpace(value) ? "New" : value.Trim();

    private static string Quote(string? value)
    {
        var normalized = (value ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
        normalized = normalized.Replace("\"", "\"\"");
        return $"\"{normalized}\"";
    }

    private static string NormalizeNewLines(string value)
    {
        var normalized = value.Replace("\r\n", "\n").Replace('\r', '\n');
        return normalized;
    }
}
