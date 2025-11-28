using System.Globalization;
using Caskr.server.Models;
using iText.Forms;
using iText.Kernel.Pdf;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Caskr.server.Services;

public interface ITtbPdfGenerator
{
    Task<PdfGenerationResult> GenerateForm5110_28Async(TtbMonthlyReportData reportData, CancellationToken cancellationToken = default);
}

public sealed class TtbPdfGeneratorService(
    CaskrDbContext dbContext,
    IWebHostEnvironment env,
    ILogger<TtbPdfGeneratorService> logger) : ITtbPdfGenerator
{
    private static readonly IReadOnlyDictionary<TtbSpiritsType, string> SpiritsTypeKeys = new Dictionary<TtbSpiritsType, string>
    {
        [TtbSpiritsType.Under190Proof] = "under190",
        [TtbSpiritsType.Neutral190OrMore] = "neutral190ormore",
        [TtbSpiritsType.Alcohol] = "alcohol",
        [TtbSpiritsType.Wine] = "wine"
    };

    public async Task<PdfGenerationResult> GenerateForm5110_28Async(TtbMonthlyReportData reportData, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reportData);

        logger.LogInformation(
            "Generating TTB Form 5110.28 for CompanyId {CompanyId} Month {Month} Year {Year}",
            reportData.CompanyId,
            reportData.Month,
            reportData.Year);

        var company = await dbContext.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == reportData.CompanyId, cancellationToken);

        if (company is null)
        {
            logger.LogError("Company {CompanyId} not found when generating Form 5110.28", reportData.CompanyId);
            throw new InvalidOperationException($"Company {reportData.CompanyId} not found");
        }

        var templatePath = Path.Combine(env.ContentRootPath, "Forms", "ttb_form_5110_28.pdf");
        if (!File.Exists(templatePath))
        {
            logger.LogError("Form template not found at {TemplatePath}", templatePath);
            throw new FileNotFoundException("Form template not found", templatePath);
        }

        using var ms = new MemoryStream();

        try
        {
            using (var reader = new PdfReader(templatePath))
            using (var writer = new PdfWriter(ms))
            using (var pdf = new PdfDocument(reader, writer))
            {
                var form = PdfAcroForm.GetAcroForm(pdf, false);

                if (form is null)
                {
                    logger.LogError("Form 5110.28 template does not contain AcroForm fields");
                    throw new InvalidOperationException("Form template is missing expected form fields");
                }

                PopulateHeaderFields(form, company, reportData);

                MapSection(form, "opening_inventory", reportData.OpeningInventory.Rows);
                MapSection(form, "production", reportData.Production.Rows);
                MapSection(form, "transfers_in", reportData.Transfers.TransfersIn);
                MapSection(form, "transfers_out", reportData.Transfers.TransfersOut);
                MapSection(form, "losses", reportData.Losses.Rows);
                MapSection(form, "closing_inventory", reportData.ClosingInventory.Rows);

                form.FlattenFields();
            }

            var pdfBytes = ms.ToArray();
            var storagePath = Path.Combine(
                env.ContentRootPath,
                "Storage",
                "TTBReports",
                reportData.CompanyId.ToString(CultureInfo.InvariantCulture),
                reportData.Year.ToString("D4", CultureInfo.InvariantCulture),
                reportData.Month.ToString("D2", CultureInfo.InvariantCulture));

            Directory.CreateDirectory(storagePath);
            var outputPath = Path.Combine(storagePath, "Form_5110_28.pdf");
            await File.WriteAllBytesAsync(outputPath, pdfBytes, cancellationToken);

            logger.LogInformation("Generated Form 5110.28 for Company {CompanyId} at {OutputPath}", reportData.CompanyId, outputPath);

            return new PdfGenerationResult(outputPath, pdfBytes);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate Form 5110.28 for CompanyId {CompanyId}", reportData.CompanyId);
            throw;
        }
    }

    private void PopulateHeaderFields(PdfAcroForm form, Company company, TtbMonthlyReportData reportData)
    {
        SetField(form, "company_name", company.CompanyName);
        SetField(form, "permit_number", company.TtbPermitNumber);
        SetField(form, "report_month", reportData.Month.ToString("D2", CultureInfo.InvariantCulture));
        SetField(form, "report_year", reportData.Year.ToString(CultureInfo.InvariantCulture));

        var address = BuildFullAddress(company);
        if (!string.IsNullOrWhiteSpace(address))
        {
            SetField(form, "company_address", address);
        }
    }

    private void MapSection(PdfAcroForm form, string sectionKey, IReadOnlyCollection<TtbSectionTotal> rows)
    {
        var totalsByType = AggregateBySpiritsType(rows);
        var totalProof = totalsByType.Values.Sum(t => t.ProofGallons);
        var totalWine = totalsByType.Values.Sum(t => t.WineGallons);

        foreach (var kvp in SpiritsTypeKeys)
        {
            var total = totalsByType.TryGetValue(kvp.Key, out var value)
                ? value
                : new GallonTotals(0m, 0m);

            SetField(form, $"{sectionKey}_{kvp.Value}_proof_gallons", FormatGallons(total.ProofGallons));
            SetField(form, $"{sectionKey}_{kvp.Value}_wine_gallons", FormatGallons(total.WineGallons));
        }

        SetField(form, $"{sectionKey}_total_proof_gallons", FormatGallons(totalProof));
        SetField(form, $"{sectionKey}_total_wine_gallons", FormatGallons(totalWine));
    }

    private static Dictionary<TtbSpiritsType, GallonTotals> AggregateBySpiritsType(IEnumerable<TtbSectionTotal> rows)
    {
        return rows
            .GroupBy(row => row.SpiritsType)
            .ToDictionary(
                group => group.Key,
                group => new GallonTotals(
                    group.Sum(r => r.ProofGallons),
                    group.Sum(r => r.WineGallons)));
    }

    private void SetField(PdfAcroForm form, string fieldName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var field = form.GetField(fieldName);
        if (field is null)
        {
            logger.LogWarning("Field {FieldName} not found in Form 5110.28 template", fieldName);
            return;
        }

        field.SetValue(value);
        logger.LogDebug("Set form field {FieldName} to {Value}", fieldName, value);
    }

    private static string FormatGallons(decimal value) => value.ToString("F2", CultureInfo.InvariantCulture);

    private static string BuildFullAddress(Company company)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(company.AddressLine1))
        {
            parts.Add(company.AddressLine1);
        }

        if (!string.IsNullOrWhiteSpace(company.AddressLine2))
        {
            parts.Add(company.AddressLine2);
        }

        var cityStateZip = new List<string>();
        if (!string.IsNullOrWhiteSpace(company.City))
        {
            cityStateZip.Add(company.City);
        }

        if (!string.IsNullOrWhiteSpace(company.State))
        {
            cityStateZip.Add(company.State);
        }

        if (!string.IsNullOrWhiteSpace(company.PostalCode))
        {
            cityStateZip.Add(company.PostalCode);
        }

        if (cityStateZip.Count > 0)
        {
            parts.Add(string.Join(", ", cityStateZip));
        }

        if (!string.IsNullOrWhiteSpace(company.Country))
        {
            parts.Add(company.Country);
        }

        return string.Join("\n", parts);
    }

    private sealed record GallonTotals(decimal ProofGallons, decimal WineGallons);
}

public sealed record PdfGenerationResult(string FilePath, byte[] Content);
