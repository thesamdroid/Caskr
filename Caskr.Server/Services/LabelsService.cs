using Caskr.server.Models;
using iText.Forms;
using iText.Forms.Fields;
using iText.Kernel.Pdf;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Caskr.server.Services;

public interface ILabelsService
{
    Task<byte[]> GenerateTtbFormAsync(LabelRequest request);
}

public class LabelsService(
    CaskrDbContext dbContext,
    IWebHostEnvironment env,
    ILogger<LabelsService> logger) : ILabelsService
{
    public async Task<byte[]> GenerateTtbFormAsync(LabelRequest request)
    {
        // Validate request
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request), "Label request cannot be null");
        }

        logger.LogInformation("Generating TTB Label Form for CompanyId: {CompanyId}", request.CompanyId);

        if (string.IsNullOrWhiteSpace(request.BrandName))
        {
            throw new ArgumentException("Brand name is required", nameof(request.BrandName));
        }

        if (string.IsNullOrWhiteSpace(request.ProductName))
        {
            throw new ArgumentException("Product name is required", nameof(request.ProductName));
        }

        // Fetch company with related data
        var company = await dbContext.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CompanyId);

        if (company == null)
        {
            logger.LogWarning("Company not found: {CompanyId}", request.CompanyId);
            throw new ArgumentException($"Company with ID {request.CompanyId} not found");
        }

        // Check template existence
        var templatePath = Path.Combine(env.ContentRootPath, "Forms", "ttb_form_5100_31.pdf");
        if (!File.Exists(templatePath))
        {
            logger.LogError("PDF template not found at: {TemplatePath}", templatePath);
            throw new FileNotFoundException($"PDF template not found at: {templatePath}");
        }

        try
        {
            using var ms = new MemoryStream();

            // Use iText7 with proper resource disposal
            using (var pdfReader = new PdfReader(templatePath))
            using (var pdfWriter = new PdfWriter(ms))
            using (var pdfDoc = new PdfDocument(pdfReader, pdfWriter))
            {
                var form = PdfAcroForm.GetAcroForm(pdfDoc, false);

                if (form == null)
                {
                    logger.LogWarning("PDF template has no form fields. Creating new document with text overlay.");
                    // If no form exists, create a simple PDF with text
                    return await CreateSimplePdfAsync(company, request);
                }

                // Get all form fields for debugging
                var fields = form.GetAllFormFields();
                logger.LogInformation("PDF template has {FieldCount} form fields", fields.Count);

                // Fill form fields with validation
                PdfFormHelper.SetFieldSafe(form, "applicant_name", company.CompanyName, logger);
                PdfFormHelper.SetFieldSafe(form, "company_name", company.CompanyName, logger);
                PdfFormHelper.SetFieldSafe(form, "brand_name", request.BrandName, logger);
                PdfFormHelper.SetFieldSafe(form, "product_name", request.ProductName, logger);
                PdfFormHelper.SetFieldSafe(form, "alcohol_content", request.AlcoholContent, logger);

                // Add company address if available
                if (!string.IsNullOrWhiteSpace(company.AddressLine1))
                {
                    var fullAddress = PdfFormHelper.BuildFullAddress(company);
                    PdfFormHelper.SetFieldSafe(form, "address", fullAddress, logger);
                    PdfFormHelper.SetFieldSafe(form, "applicant_address", fullAddress, logger);
                }

                // Add TTB permit number if available
                if (!string.IsNullOrWhiteSpace(company.TtbPermitNumber))
                {
                    PdfFormHelper.SetFieldSafe(form, "permit_number", company.TtbPermitNumber, logger);
                    PdfFormHelper.SetFieldSafe(form, "ttb_permit", company.TtbPermitNumber, logger);
                }

                // Add contact info if available
                if (!string.IsNullOrWhiteSpace(company.PhoneNumber))
                {
                    PdfFormHelper.SetFieldSafe(form, "phone", company.PhoneNumber, logger);
                    PdfFormHelper.SetFieldSafe(form, "phone_number", company.PhoneNumber, logger);
                }

                // Flatten the form to make it non-editable
                form.FlattenFields();
            }

            var pdfBytes = ms.ToArray();
            logger.LogInformation("Successfully generated TTB Label Form. Size: {Size} bytes", pdfBytes.Length);
            return pdfBytes;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating TTB Label Form for CompanyId: {CompanyId}", request.CompanyId);
            throw new InvalidOperationException("Failed to generate PDF document. See logs for details.", ex);
        }
    }

    private async Task<byte[]> CreateSimplePdfAsync(Company company, LabelRequest request)
    {
        logger.LogInformation("Creating simple PDF document without form fields");

        using var ms = new MemoryStream();
        using (var writer = new PdfWriter(ms))
        using (var pdf = new PdfDocument(writer))
        using (var document = new iText.Layout.Document(pdf))
        {
            // Add title
            document.Add(new iText.Layout.Element.Paragraph("TTB Label Application Form 5100.31")
                .SetFontSize(16)
                .SetBold());

            document.Add(new iText.Layout.Element.Paragraph("\n"));

            // Add company information
            document.Add(new iText.Layout.Element.Paragraph($"Applicant Name: {company.CompanyName}")
                .SetFontSize(12));

            if (!string.IsNullOrWhiteSpace(company.AddressLine1))
            {
                document.Add(new iText.Layout.Element.Paragraph($"Address: {PdfFormHelper.BuildFullAddress(company)}")
                    .SetFontSize(12));
            }

            if (!string.IsNullOrWhiteSpace(company.TtbPermitNumber))
            {
                document.Add(new iText.Layout.Element.Paragraph($"TTB Permit Number: {company.TtbPermitNumber}")
                    .SetFontSize(12));
            }

            document.Add(new iText.Layout.Element.Paragraph("\n"));

            // Add label information
            document.Add(new iText.Layout.Element.Paragraph($"Brand Name: {request.BrandName}")
                .SetFontSize(12));
            document.Add(new iText.Layout.Element.Paragraph($"Product Name: {request.ProductName}")
                .SetFontSize(12));
            document.Add(new iText.Layout.Element.Paragraph($"Alcohol Content: {request.AlcoholContent}")
                .SetFontSize(12));
        }

        return ms.ToArray();
    }
}
