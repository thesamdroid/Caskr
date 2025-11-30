using System.Collections.Generic;
using Caskr.server.Models;
using iText.Forms;
using Microsoft.Extensions.Logging;

namespace Caskr.server.Services;

internal static class PdfFormHelper
{
    public static void SetFieldSafe(PdfAcroForm form, string fieldName, string? value, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var field = form.GetField(fieldName);
        if (field != null)
        {
            field.SetValue(value);
            logger.LogDebug("Set field '{FieldName}' to '{Value}'", fieldName, value);
        }
        else
        {
            logger.LogDebug("Field '{FieldName}' not found in PDF template", fieldName);
        }
    }

    public static string BuildFullAddress(Company company)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(company.AddressLine1))
            parts.Add(company.AddressLine1);

        if (!string.IsNullOrWhiteSpace(company.AddressLine2))
            parts.Add(company.AddressLine2);

        var cityStateZip = new List<string>();
        if (!string.IsNullOrWhiteSpace(company.City))
            cityStateZip.Add(company.City);
        if (!string.IsNullOrWhiteSpace(company.State))
            cityStateZip.Add(company.State);
        if (!string.IsNullOrWhiteSpace(company.PostalCode))
            cityStateZip.Add(company.PostalCode);

        if (cityStateZip.Count > 0)
            parts.Add(string.Join(", ", cityStateZip));

        if (!string.IsNullOrWhiteSpace(company.Country))
            parts.Add(company.Country);

        return string.Join("\n", parts);
    }
}
