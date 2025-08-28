using Caskr.server.Models;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Hosting;

namespace Caskr.server.Services;

public interface ILabelsService
{
    Task<byte[]> GenerateTtbFormAsync(LabelRequest request);
}

public class LabelsService(CaskrDbContext dbContext, IWebHostEnvironment env) : ILabelsService
{
    public async Task<byte[]> GenerateTtbFormAsync(LabelRequest request)
    {
        var company = await dbContext.Companies.FindAsync(request.CompanyId);
        if (company == null)
        {
            throw new ArgumentException("Company not found");
        }
        var templatePath = Path.Combine(env.ContentRootPath, "Forms", "ttb_form_5100_31.pdf");
        var reader = new PdfReader(templatePath);
        var ms = new MemoryStream();
        var stamper = new PdfStamper(reader, ms);
        var form = stamper.AcroFields;
        form.SetField("applicant_name", company.CompanyName);
        form.SetField("brand_name", request.BrandName);
        form.SetField("product_name", request.ProductName);
        form.SetField("alcohol_content", request.AlcoholContent);
        stamper.FormFlattening = true;
        stamper.Close();
        reader.Close();
        var bytes = ms.ToArray();
        ms.Close();
        return bytes;
    }
}
