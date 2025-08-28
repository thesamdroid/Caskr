using Caskr.server.Models;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Hosting;

namespace Caskr.server.Services;

public interface ITransfersService
{
    Task<byte[]> GenerateTtbFormAsync(TransferRequest request);
}

public class TransfersService(CaskrDbContext dbContext, IWebHostEnvironment env) : ITransfersService
{
    public async Task<byte[]> GenerateTtbFormAsync(TransferRequest request)
    {
        var company = await dbContext.Companies.FindAsync(request.FromCompanyId);
        if (company == null)
        {
            throw new ArgumentException("Company not found");
        }
        var templatePath = Path.Combine(env.ContentRootPath, "Forms", "ttb_form_5100_16.pdf");
        var reader = new PdfReader(templatePath);
        var ms = new MemoryStream();
        var stamper = new PdfStamper(reader, ms);
        var form = stamper.AcroFields;
        form.SetField("from_company", company.CompanyName);
        form.SetField("to_company", request.ToCompanyName);
        form.SetField("permit_number", request.PermitNumber);
        form.SetField("address", request.Address);
        form.SetField("barrel_count", request.BarrelCount.ToString());
        stamper.FormFlattening = true;
        stamper.Close();
        reader.Close();
        var bytes = ms.ToArray();
        ms.Close();
        return bytes;
    }
}
