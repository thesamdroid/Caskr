using Caskr.server.Models;
using Caskr.server.Services;
using iText.Forms;
using iText.Forms.Fields;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Caskr.Server.Tests;

public class PdfFormHelperTests
{
    [Fact]
    public void BuildFullAddress_WithAllFields_ReturnsMultiLineAddress()
    {
        var company = new Company
        {
            AddressLine1 = "123 Main St",
            AddressLine2 = "Suite 5",
            City = "Portland",
            State = "OR",
            PostalCode = "97201",
            Country = "USA"
        };

        var address = PdfFormHelper.BuildFullAddress(company);

        Assert.Equal("123 Main St\nSuite 5\nPortland, OR, 97201\nUSA", address);
    }

    [Fact]
    public void BuildFullAddress_IgnoresBlankSegments()
    {
        var company = new Company
        {
            AddressLine1 = "456 Warehouse Rd",
            City = "Louisville",
            PostalCode = "40202",
            Country = "USA"
        };

        var address = PdfFormHelper.BuildFullAddress(company);

        Assert.Equal("456 Warehouse Rd\nLouisville, 40202\nUSA", address);
    }

    [Fact]
    public void BuildFullAddress_AllFieldsBlank_ReturnsEmptyString()
    {
        var company = new Company();

        var address = PdfFormHelper.BuildFullAddress(company);

        Assert.Equal(string.Empty, address);
    }

    [Fact]
    public void SetFieldSafe_SetsExistingFieldValue()
    {
        using var ms = new MemoryStream();
        using var writer = new PdfWriter(ms);
        using var pdfDoc = new PdfDocument(writer);
        var form = PdfAcroForm.GetAcroForm(pdfDoc, true);
        var field = PdfFormField.CreateText(pdfDoc, new Rectangle(0, 0, 100, 20), "brand", "seed");
        form.AddField(field);

        PdfFormHelper.SetFieldSafe(form, "brand", "Barrel A", NullLogger.Instance);

        Assert.Equal("Barrel A", form.GetField("brand").GetValueAsString());
    }

    [Fact]
    public void SetFieldSafe_IgnoresWhitespaceValues()
    {
        using var ms = new MemoryStream();
        using var writer = new PdfWriter(ms);
        using var pdfDoc = new PdfDocument(writer);
        var form = PdfAcroForm.GetAcroForm(pdfDoc, true);
        var field = PdfFormField.CreateText(pdfDoc, new Rectangle(0, 0, 100, 20), "brand", "seed");
        form.AddField(field);

        PdfFormHelper.SetFieldSafe(form, "brand", "  \t", NullLogger.Instance);

        Assert.Equal("seed", form.GetField("brand").GetValueAsString());
    }

    [Fact]
    public void SetFieldSafe_WhenFieldMissing_LogsDebugAndSkipsUpdate()
    {
        using var ms = new MemoryStream();
        using var writer = new PdfWriter(ms);
        using var pdfDoc = new PdfDocument(writer);
        var form = PdfAcroForm.GetAcroForm(pdfDoc, true);
        var logger = new ListLogger<PdfFormHelper>();

        PdfFormHelper.SetFieldSafe(form, "nonexistent", "Value", logger);

        Assert.Empty(form.GetFormFields());
        Assert.Contains(logger.Entries, entry => entry.LogLevel == LogLevel.Debug && entry.Message.Contains("not found", StringComparison.OrdinalIgnoreCase));
    }
}

internal class ListLogger<T> : ILogger<T>
{
    public List<LoggedEntry> Entries { get; } = new();

    public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Entries.Add(new LoggedEntry(logLevel, formatter(state, exception)));
    }
}

internal record LoggedEntry(LogLevel LogLevel, string Message);

internal sealed class NullScope : IDisposable
{
    public static NullScope Instance { get; } = new();

    public void Dispose()
    {
    }
}
