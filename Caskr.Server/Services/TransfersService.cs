using Caskr.server.Models;
using iText.Forms;
using iText.Forms.Fields;
using iText.Kernel.Pdf;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Caskr.server.Services;

public interface ITransfersService
{
    Task<byte[]> GenerateTtbFormAsync(TransferRequest request);
}

public class TransfersService(
    CaskrDbContext dbContext,
    IWebHostEnvironment env,
    ILogger<TransfersService> logger) : ITransfersService
{
    public async Task<byte[]> GenerateTtbFormAsync(TransferRequest request)
    {
        // Validate request
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request), "Transfer request cannot be null");
        }

        logger.LogInformation("Generating TTB Transfer Form for FromCompanyId: {FromCompanyId}", request.FromCompanyId);

        if (string.IsNullOrWhiteSpace(request.ToCompanyName))
        {
            throw new ArgumentException("Destination company name is required", nameof(request.ToCompanyName));
        }

        if (request.BarrelCount <= 0)
        {
            throw new ArgumentException("Barrel count must be greater than zero", nameof(request.BarrelCount));
        }

        // Fetch company with related data
        var company = await dbContext.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.FromCompanyId);

        if (company == null)
        {
            logger.LogWarning("Company not found: {FromCompanyId}", request.FromCompanyId);
            throw new ArgumentException($"Company with ID {request.FromCompanyId} not found");
        }

        // Optionally fetch barrel details if OrderId is provided
        List<Barrel>? barrels = null;
        if (request.OrderId.HasValue)
        {
            barrels = await dbContext.Barrels
                .AsNoTracking()
                .Where(b => b.OrderId == request.OrderId.Value)
                .Include(b => b.Batch)
                .ThenInclude(b => b.MashBill)
                .ToListAsync();

            logger.LogInformation("Found {BarrelCount} barrels for OrderId: {OrderId}", barrels.Count, request.OrderId);
        }

        // Check template existence
        var templatePath = Path.Combine(env.ContentRootPath, "Forms", "ttb_form_5100_16.pdf");
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
                    return await CreateSimplePdfAsync(company, request, barrels);
                }

                // Get all form fields for debugging
                var fields = form.GetAllFormFields();
                logger.LogInformation("PDF template has {FieldCount} form fields", fields.Count);

                // Fill basic transfer information
                PdfFormHelper.SetFieldSafe(form, "from_company", company.CompanyName, logger);
                PdfFormHelper.SetFieldSafe(form, "shipper_name", company.CompanyName, logger);
                PdfFormHelper.SetFieldSafe(form, "to_company", request.ToCompanyName, logger);
                PdfFormHelper.SetFieldSafe(form, "consignee_name", request.ToCompanyName, logger);
                PdfFormHelper.SetFieldSafe(form, "barrel_count", request.BarrelCount.ToString(), logger);
                PdfFormHelper.SetFieldSafe(form, "quantity", request.BarrelCount.ToString(), logger);

                // Fill permit information
                if (!string.IsNullOrWhiteSpace(request.PermitNumber))
                {
                    PdfFormHelper.SetFieldSafe(form, "permit_number", request.PermitNumber, logger);
                    PdfFormHelper.SetFieldSafe(form, "consignee_permit", request.PermitNumber, logger);
                }

                if (!string.IsNullOrWhiteSpace(company.TtbPermitNumber))
                {
                    PdfFormHelper.SetFieldSafe(form, "shipper_permit", company.TtbPermitNumber, logger);
                    PdfFormHelper.SetFieldSafe(form, "ttb_permit", company.TtbPermitNumber, logger);
                }

                // Fill addresses
                if (!string.IsNullOrWhiteSpace(request.Address))
                {
                    PdfFormHelper.SetFieldSafe(form, "address", request.Address, logger);
                    PdfFormHelper.SetFieldSafe(form, "consignee_address", request.Address, logger);
                }

                if (!string.IsNullOrWhiteSpace(company.AddressLine1))
                {
                    var fullAddress = PdfFormHelper.BuildFullAddress(company);
                    PdfFormHelper.SetFieldSafe(form, "shipper_address", fullAddress, logger);
                    PdfFormHelper.SetFieldSafe(form, "from_address", fullAddress, logger);
                }

                // Fill contact information
                if (!string.IsNullOrWhiteSpace(company.PhoneNumber))
                {
                    PdfFormHelper.SetFieldSafe(form, "phone", company.PhoneNumber, logger);
                    PdfFormHelper.SetFieldSafe(form, "shipper_phone", company.PhoneNumber, logger);
                }

                // Fill barrel details if available
                if (barrels != null && barrels.Count > 0)
                {
                    var barrelSkus = string.Join(", ", barrels.Select(b => b.Sku));
                    PdfFormHelper.SetFieldSafe(form, "barrel_numbers", barrelSkus, logger);
                    PdfFormHelper.SetFieldSafe(form, "serial_numbers", barrelSkus, logger);

                    // If we have batch/mash bill information
                    var mashBill = barrels.FirstOrDefault()?.Batch?.MashBill;
                    if (mashBill != null)
                    {
                        PdfFormHelper.SetFieldSafe(form, "product_type", mashBill.Name, logger);
                        PdfFormHelper.SetFieldSafe(form, "mash_bill", mashBill.Name, logger);
                    }
                }

                // Add current date
                PdfFormHelper.SetFieldSafe(form, "date", DateTime.Now.ToString("MM/dd/yyyy"), logger);
                PdfFormHelper.SetFieldSafe(form, "transfer_date", DateTime.Now.ToString("MM/dd/yyyy"), logger);

                // Flatten the form to make it non-editable
                form.FlattenFields();
            }

            var pdfBytes = ms.ToArray();
            logger.LogInformation("Successfully generated TTB Transfer Form. Size: {Size} bytes", pdfBytes.Length);
            return pdfBytes;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating TTB Transfer Form for FromCompanyId: {FromCompanyId}", request.FromCompanyId);
            throw new InvalidOperationException("Failed to generate PDF document. See logs for details.", ex);
        }
    }

    private async Task<byte[]> CreateSimplePdfAsync(Company company, TransferRequest request, List<Barrel>? barrels)
    {
        logger.LogInformation("Creating simple PDF document without form fields");

        using var ms = new MemoryStream();
        using (var writer = new PdfWriter(ms))
        using (var pdf = new PdfDocument(writer))
        using (var document = new iText.Layout.Document(pdf))
        {
            // Add title
            document.Add(new iText.Layout.Element.Paragraph("TTB Transfer Form 5100.16")
                .SetFontSize(16)
                .SetBold());

            document.Add(new iText.Layout.Element.Paragraph("\n"));

            // Add from company information
            document.Add(new iText.Layout.Element.Paragraph("SHIPPER INFORMATION")
                .SetFontSize(14)
                .SetBold());
            document.Add(new iText.Layout.Element.Paragraph($"Company Name: {company.CompanyName}")
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

            if (!string.IsNullOrWhiteSpace(company.PhoneNumber))
            {
                document.Add(new iText.Layout.Element.Paragraph($"Phone: {company.PhoneNumber}")
                    .SetFontSize(12));
            }

            document.Add(new iText.Layout.Element.Paragraph("\n"));

            // Add to company information
            document.Add(new iText.Layout.Element.Paragraph("CONSIGNEE INFORMATION")
                .SetFontSize(14)
                .SetBold());
            document.Add(new iText.Layout.Element.Paragraph($"Company Name: {request.ToCompanyName}")
                .SetFontSize(12));

            if (!string.IsNullOrWhiteSpace(request.Address))
            {
                document.Add(new iText.Layout.Element.Paragraph($"Address: {request.Address}")
                    .SetFontSize(12));
            }

            if (!string.IsNullOrWhiteSpace(request.PermitNumber))
            {
                document.Add(new iText.Layout.Element.Paragraph($"Permit Number: {request.PermitNumber}")
                    .SetFontSize(12));
            }

            document.Add(new iText.Layout.Element.Paragraph("\n"));

            // Add transfer details
            document.Add(new iText.Layout.Element.Paragraph("TRANSFER DETAILS")
                .SetFontSize(14)
                .SetBold());
            document.Add(new iText.Layout.Element.Paragraph($"Transfer Date: {DateTime.Now:MM/dd/yyyy}")
                .SetFontSize(12));
            document.Add(new iText.Layout.Element.Paragraph($"Number of Barrels: {request.BarrelCount}")
                .SetFontSize(12));

            // Add barrel details if available
            if (barrels != null && barrels.Count > 0)
            {
                document.Add(new iText.Layout.Element.Paragraph("\n"));
                document.Add(new iText.Layout.Element.Paragraph("BARREL DETAILS")
                    .SetFontSize(14)
                    .SetBold());

                foreach (var barrel in barrels)
                {
                    document.Add(new iText.Layout.Element.Paragraph($"• SKU: {barrel.Sku}")
                        .SetFontSize(11));
                }
            }
        }

        return ms.ToArray();
    }
}
