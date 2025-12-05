using System;
using System.Text.Json;
using Caskr.server.Controllers;
using Caskr.server.Models;
using Xunit;

namespace Caskr.Server.Tests;

public sealed class TtbFormTypeSerializationTests
{
    [Fact]
    public void FormType_UsesNumericSerialization_ByDefault()
    {
        var response = new TtbReportSummaryResponse
        {
            Id = 1,
            CompanyId = 5,
            ReportMonth = 8,
            ReportYear = 2024,
            FormType = TtbFormType.Form5110_40,
            Status = TtbReportStatus.Approved,
            GeneratedAt = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(response);

        Assert.Contains("\"FormType\":1", json);
    }

    [Fact]
    public void FormType_DeserializesNumericValues_ForGenerationRequests()
    {
        // Use PascalCase property names to match C# class (System.Text.Json is case-sensitive by default)
        const string payload = "{\"CompanyId\":3,\"Month\":7,\"Year\":2024,\"FormType\":1}";

        var request = JsonSerializer.Deserialize<TtbReportGenerationRequest>(payload);

        Assert.NotNull(request);
        Assert.Equal(TtbFormType.Form5110_40, request!.FormType);
    }
}
