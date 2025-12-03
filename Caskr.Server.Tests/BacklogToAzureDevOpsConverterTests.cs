using Caskr.Server.Services.BacklogImport;

namespace Caskr.Server.Tests;

public class BacklogToAzureDevOpsConverterTests
{
    private readonly BacklogToAzureDevOpsConverter _converter = new();

    [Fact]
    public void ConvertFromCsv_SeparatesAcceptanceCriteria()
    {
        const string csv = "Work Item Type,Title,State,Priority,Tags,Description\n" +
                           "Product Backlog Item,Mobile App,New,1,mobile;app,\"As a user I want quick access.\\n\\nAcceptance Criteria:\\n- first\\n- second\"";

        var items = _converter.ConvertFromCsv(csv, "Caskr", "Caskr/Backlog");

        var workItem = Assert.Single(items);
        Assert.Equal("Product Backlog Item", workItem.WorkItemType);
        Assert.Equal("Mobile App", workItem.Title);
        Assert.Equal("As a user I want quick access.", workItem.Description);
        Assert.Equal("- first\n- second", workItem.AcceptanceCriteria);
        Assert.Equal("mobile;app", workItem.Tags);
        Assert.Equal("Caskr", workItem.AreaPath);
        Assert.Equal("Caskr/Backlog", workItem.IterationPath);
    }

    [Fact]
    public void ConvertFromCsv_AllowsMissingAcceptanceCriteria()
    {
        const string csv = "Work Item Type,Title,State,Priority,Tags,Description\n" +
                           "Task,Create dashboard metric,New,2,analytics,\"Implement top-line KPI card\"";

        var items = _converter.ConvertFromCsv(csv, "Caskr", "Caskr/Backlog");

        var workItem = Assert.Single(items);
        Assert.Equal("Implement top-line KPI card", workItem.Description);
        Assert.Equal(string.Empty, workItem.AcceptanceCriteria);
    }

    [Fact]
    public void ConvertFromCsv_ThrowsWhenRequiredFieldMissing()
    {
        const string csv = "Work Item Type,Title,State,Priority,Tags,Description\n" +
                           ",Missing title should fail,New,3,tagged,\"No type provided\"";

        var exception = Assert.Throws<ArgumentException>(() =>
            _converter.ConvertFromCsv(csv, "Caskr", "Caskr/Backlog"));

        Assert.Contains("Work Item Type", exception.Message);
    }

    [Fact]
    public void ToAzureCsv_EscapesCommasAndNewLines()
    {
        var items = new List<AzureDevOpsWorkItem>
        {
            new()
            {
                WorkItemType = "Product Backlog Item",
                Title = "Scan barrels from mobile",
                State = "New",
                Priority = "1",
                Tags = "mobile;scanning",
                Description = "Summary with, comma",
                AcceptanceCriteria = "- works offline\n- handles errors",
                AreaPath = "Caskr",
                IterationPath = "Caskr/Backlog"
            }
        };

        var csv = _converter.ToAzureCsv(items);
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal("Work Item Type,Title,Area Path,Iteration Path,State,Priority,Tags,Description,Acceptance Criteria", lines[0]);
        Assert.Contains("\"Summary with, comma\"", lines[1]);
        Assert.Contains("\"- works offline\n- handles errors\"", lines[1]);
    }
}
