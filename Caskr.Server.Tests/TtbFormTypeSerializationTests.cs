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
        const string payload = "{\"companyId\":3,\"month\":7,\"year\":2024,\"formType\":1}";

        var request = JsonSerializer.Deserialize<TtbReportGenerationRequest>(payload);

        Assert.NotNull(request);
        Assert.Equal(TtbFormType.Form5110_40, request!.FormType);
    }
}
