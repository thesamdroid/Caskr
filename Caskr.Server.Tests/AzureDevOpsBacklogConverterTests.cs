using System.IO;
using System.Linq;
using System.Text;
using Caskr.server.Utilities;
using Xunit;

namespace Caskr.Server.Tests;

public class AzureDevOpsBacklogConverterTests
{
    [Fact]
    public void Convert_ExtractsAcceptanceCriteria()
    {
        const string csv = "Work Item Type,Title,State,Priority,Tags,Description\n" +
                           "Product Backlog Item,Sample Item,Active,2,alpha; beta,\"Intro text.\\n\\nAcceptance Criteria:\\n- One\\n- Two\"";

        using var reader = new StringReader(csv);
        var results = AzureDevOpsBacklogConverter.Convert(reader);

        Assert.Single(results);
        var item = results.First();
        Assert.Equal("User Story", item.WorkItemType);
        Assert.Equal("Sample Item", item.Title);
        Assert.Equal("Active", item.State);
        Assert.Equal("2", item.Priority);
        Assert.Equal("alpha;beta", item.Tags);
        Assert.Equal("Intro text.", item.Description);
        Assert.Equal("- One\n- Two", item.AcceptanceCriteria);
    }

    [Fact]
    public void Convert_NormalizesProductBacklogItemToUserStory()
    {
        const string csv = "Work Item Type,Title,State,Priority,Tags,Description\n" +
                           "Product Backlog Item,Story Title,Active,2,tag,\"Description text\"";

        using var reader = new StringReader(csv);
        var results = AzureDevOpsBacklogConverter.Convert(reader);

        Assert.Single(results);
        Assert.Equal("User Story", results.First().WorkItemType);
    }

    [Fact]
    public void Convert_ThrowsWhenWorkItemTypeMissing()
    {
        const string csv = "Work Item Type,Title,State,Priority,Tags,Description\n" +
                           ",Missing type,Active,2,tag,\"Description text\"";

        using var reader = new StringReader(csv);

        var exception = Assert.Throws<InvalidDataException>(() => AzureDevOpsBacklogConverter.Convert(reader));
        Assert.Contains("Work Item Type cannot be empty", exception.Message);
    }

    [Fact]
    public void Convert_DefaultsMissingStateAndAcceptance()
    {
        const string csv = "Work Item Type,Title,State,Priority,Tags,Description\n" +
                           "Task,Missing State,,3,tag-one;,\"Only description text\"";

        using var reader = new StringReader(csv);
        var results = AzureDevOpsBacklogConverter.Convert(reader);

        Assert.Single(results);
        var item = results.First();
        Assert.Equal("New", item.State);
        Assert.Equal(string.Empty, item.AcceptanceCriteria);
        Assert.Equal("tag-one", item.Tags);
    }

    [Fact]
    public void WriteAzureCsv_QuotesFieldsAndPreservesNewLines()
    {
        var items = new[]
        {
            new AzureDevOpsWorkItem(
                "Task",
                "Write file",
                "New",
                "1",
                "tag1;tag2",
                "Description with \n new line",
                "- one\n- two"),
            new AzureDevOpsWorkItem(
                "Product Backlog Item",
                "Another",
                "Committed",
                "2",
                "",
                "Plain",
                string.Empty)
        };

        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            AzureDevOpsBacklogConverter.WriteAzureCsv(items, writer);
        }

        var output = builder.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("\"Work Item Type\",\"Title\",\"State\",\"Priority\",\"Tags\",\"Description\",\"Acceptance Criteria\"", output[0]);
        Assert.Contains("\"Description with \n new line\"", output[1]);
        Assert.Contains("\"- one\n- two\"", output[1]);
        Assert.EndsWith("\"\",\"Plain\",\"\"\",\"\"", output[2]);
    }
}
