# Quick Start - PDF Document Generation

## Installation

```bash
cd Caskr.Server
dotnet restore
dotnet build
```

## Usage

### Generate Label (Form 5100.31)

```http
POST /api/labels/ttb-form
Content-Type: application/json
Authorization: Bearer {your-token}

{
  "companyId": 1,
  "brandName": "Kentucky Reserve",
  "productName": "Straight Bourbon Whiskey",
  "alcoholContent": "45% ABV"
}
```

### Generate Transfer (Form 5100.16)

```http
POST /api/transfers/ttb-form
Content-Type: application/json
Authorization: Bearer {your-token}

{
  "fromCompanyId": 1,
  "toCompanyName": "Destination Distillery LLC",
  "permitNumber": "DSP-KY-12345",
  "address": "123 Bourbon St, Louisville, KY 40201",
  "barrelCount": 50,
  "orderId": 42  // OPTIONAL: Includes barrel details
}
```

## What Gets Auto-Filled

### Label Form
- ✅ Company name
- ✅ Company address (full formatted)
- ✅ TTB permit number
- ✅ Phone number
- ✅ Brand name (from request)
- ✅ Product name (from request)
- ✅ Alcohol content (from request)

### Transfer Form
- ✅ Shipper company info (name, address, permit, phone)
- ✅ Consignee info (from request)
- ✅ Barrel count
- ✅ **Barrel SKUs** (if OrderId provided)
- ✅ **Mash bill details** (if OrderId provided)
- ✅ Current date

## Common Errors

| Error | Cause | Fix |
|-------|-------|-----|
| 400 Bad Request | Missing required fields | Check all required fields are provided |
| 404 Not Found | Invalid CompanyId | Verify company exists |
| 500 Template Not Found | Missing PDF template | Add Forms/ttb_form_*.pdf files |

## Logs

Check console for detailed logs:
```
[Information] Generating TTB Label Form for CompanyId: 1
[Information] PDF template has 15 form fields
[Information] Successfully generated TTB Label Form. Size: 45678 bytes
```

## Need Official Forms?

Download from TTB.gov:
- Form 5100.31 (Label): https://www.ttb.gov/forms/f510031.pdf
- Form 5100.16 (Transfer): https://www.ttb.gov/forms/f510016.pdf

Place in: `Caskr.Server/Forms/`

## Full Documentation

See [PDF_GENERATION_FIX.md](PDF_GENERATION_FIX.md) for complete details.
