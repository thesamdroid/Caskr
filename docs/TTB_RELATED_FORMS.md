# TTB Related Forms - Quick Reference Guide

**Purpose:** Cross-reference guide for TTB forms related to Form 5110.28
**Last Updated:** 2025-11-28
**For:** Distilled Spirits Plants (DSP)

---

## Overview

Form 5110.28 (Monthly Report of Processing Operations) is one of **three required monthly operational reports** that distilled spirits plants must file with the TTB. Understanding how these forms relate to each other is critical for compliance.

---

## The Three Monthly Operational Reports

| Form | Name | Purpose | Primary Unit | Due Date |
|------|------|---------|--------------|----------|
| **5110.40** | Production Report | Track spirits produced/distilled | Proof Gallons | 15th of following month |
| **5110.11** | Storage Report | Track spirits in bonded storage | Proof Gallons | 15th of following month |
| **5110.28** | Processing Report | Track bottling/packaging/denaturing | Proof Gallons | 15th of following month |

**Critical:** These three reports must **cross-validate** with each other:
- Spirits produced (5110.40) → Transfer to storage (5110.11) or processing (5110.28)
- Spirits in storage (5110.11) → Transfer to processing (5110.28) for bottling
- Closing inventory must reconcile across all three forms

---

## Form 5110.40 - Monthly Report of Production Operations

### Purpose
Report spirits **produced** (distilled) during the month.

### Key Sections
- **Part I:** Production of spirits (by type)
- **Part II:** Production of alcohol (if applicable)
- **Part III:** Spirits produced for fuel use

### Data Flow to Form 5110.28

```
Form 5110.40 (Production)
    ↓
Production complete → Transfer to...
    ↓
Form 5110.28, Part I, Line 2 (Received from Production)
```

### Caskr Implementation

**When batch is completed:**

```csharp
// 1. Log production on Form 5110.40
await _ttbTransactionLogger.LogProductionAsync(batchId, completionDate);
// Creates: TtbTransaction with TransactionType.Production

// 2. This automatically appears on Form 5110.28, Part I, Line 2
// "Received from Production" in next monthly report
```

### Official TTB Resource
- **Form:** [TTB F 5110.40](https://www.ttb.gov/ttb-form-511040)
- **Instructions:** Available on TTB form page
- **Regulation:** 27 CFR 19.620

---

## Form 5110.11 - Monthly Report of Storage Operations

### Purpose
Report bulk spirits **stored** in bonded warehouses.

### Key Sections
- **Line 1:** On hand first of month (opening inventory)
- **Lines 2-6:** Receipts (production, transfers, imports)
- **Lines 7-20:** Dispositions (withdrawals, transfers, tax paid)
- **Line 23:** On hand end of month (closing inventory)

### Data Flow with Form 5110.28

```
Form 5110.11 (Storage)
    ↓
Aged spirits ready for bottling → Transfer to processing
    ↓
Form 5110.28, Part I, Line 1 or Line 4 (Bulk spirits received)
    ↓
After bottling →
    ↓
Form 5110.28, Part II (Finished products)
```

### Cross-Validation Rules

**Opening Inventory Continuity:**
```
Form 5110.11, Line 1 (This Month)
    MUST EQUAL
Form 5110.11, Line 23 (Previous Month)
```

**Transfer Matching:**
```
Form 5110.11, Line 18 (Transfer to Processing Account)
    MUST EQUAL
Form 5110.28, Part I, Line 4 (Received from storage/other premises)
```

### Caskr Implementation

**Storage to processing transfer:**

```csharp
// When barrels are dumped for bottling:
await _ttbTransactionLogger.LogTransferToProcessingAsync(barrelIds);

// This creates TWO transactions:
// 1. Form 5110.11: TransferOut (reduces storage inventory)
// 2. Form 5110.28: TransferIn (increases processing inventory)
```

### Official TTB Resource
- **Form:** [TTB F 5110.11](https://www.ttb.gov/ttb-form-511011)
- **Help:** [Form 5110.11 Help Guide](https://www.pay.gov/public/static-assets/paygov/ATF/help/ATF_5110_11_help_v3.html)
- **Regulation:** 27 CFR 19.619

---

## Form 5100.11 - Transfer of Spirits in Bond

### Purpose
Document **inter-facility transfers** of spirits while still under bond (tax not yet paid).

**CRITICAL:** This is NOT a monthly report—it's a **per-transfer document**.

### When Required
- Transferring spirits from one DSP to another
- Both facilities are bonded
- Tax has not been paid on the spirits

### Data on Form
- **From:** Transferring DSP (name, permit number, address)
- **To:** Receiving DSP (name, permit number, address)
- **Spirits:** Quantity (proof gallons, wine gallons), type, container count
- **Date:** Transfer date
- **Serial Number:** Unique transfer ID

### Connection to Form 5110.28

```
Prepare Transfer
    ↓
Complete Form 5100.11 → Give to carrier
    ↓
Sending DSP:
    Form 5110.28, Part I, Line 12 (Transfers to other bonded premises)
    ↓
Receiving DSP:
    Form 5110.28, Part I, Line 4 (Received from other bonded premises)
```

### Caskr Implementation

**Recommended: Add transfer table**

```sql
CREATE TABLE ttb_transfers (
    id BIGSERIAL PRIMARY KEY,
    form_5100_11_number VARCHAR(50) NOT NULL,  -- Reference to transfer document
    from_company_id INTEGER NOT NULL,
    to_company_id INTEGER NOT NULL,
    transfer_date DATE NOT NULL,
    proof_gallons DECIMAL(12,2) NOT NULL,
    wine_gallons DECIMAL(12,2) NOT NULL,
    spirit_type TEXT NOT NULL,
    status VARCHAR(20) DEFAULT 'Pending',  -- Pending, InTransit, Received
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- When transfer is initiated:
INSERT INTO ttb_transfers (...) VALUES (...);

-- Create outbound transaction for sender:
INSERT INTO ttb_transactions (company_id, transaction_type, ...)
VALUES (from_company_id, 'TransferOut', ...);

-- When transfer is received:
UPDATE ttb_transfers SET status = 'Received' WHERE id = ...;

-- Create inbound transaction for receiver:
INSERT INTO ttb_transactions (company_id, transaction_type, ...)
VALUES (to_company_id, 'TransferIn', ...);
```

### Official TTB Resource
- **Form:** [TTB F 5100.11](https://www.ttb.gov/images/pdfs/forms/f510011.pdf)
- **Regulation:** 27 CFR 19.453

---

## Form 5110.43 - Monthly Report of Denaturing Operations

### Purpose
Report **denaturing** operations (making spirits unfit for beverage use).

### When Required
- If your DSP denatures spirits
- If you manufacture products from denatured alcohol

### Key Sections
- Alcohol received for denaturing
- Formulas used (SDA formulas)
- Quantities denatured
- Products manufactured

### Connection to Form 5110.28

```
Form 5110.28, Part I, Line 11 (Used for Denaturation)
    ↓
Transferred to denaturing account
    ↓
Form 5110.43 (Report denaturing operations)
```

**Note:** Most craft distilleries **do not** denature spirits, so Form 5110.43 may not apply.

### Caskr Implementation

**If denaturing is supported:**

```csharp
public enum TtbTransactionType
{
    // ... existing types
    Denaturation = 8  // Add new type
}

// Track denatured spirits separately
public class TtbDenaturingRecord
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string SdaFormulaNumber { get; set; }  // e.g., "SDA 3-A"
    public decimal ProofGallons { get; set; }
    public DateTime DenaturingDate { get; set; }
}
```

### Official TTB Resource
- **Form:** [TTB F 5110.43](https://www.ttb.gov/ttb-form-511043)
- **Regulation:** 27 CFR 19.622

---

## Form 5000.24 - Excise Tax Return

### Purpose
**Pay taxes** on spirits removed from bond.

**CRITICAL:** This is a **tax payment form**, not an operations report.

### Filing Frequency
- **Semi-monthly** if average monthly tax liability > $50,000
- **Monthly** if average monthly tax liability ≤ $50,000

### Due Dates
- **Semi-monthly filers:**
  - 1st-15th of month: Due on 29th of same month
  - 16th-end of month: Due on 14th of following month
- **Monthly filers:** 14th day of following month

### Connection to Form 5110.28

```
Form 5110.28: Spirits removed on determination of tax
    ↓
Line 29 (or similar): Tax determinations
    ↓
Form 5000.24: Calculate and pay excise tax
    (Current rate: $13.50 per proof gallon for < 100,000 PG/year)
```

### Tax Calculation

```csharp
public decimal CalculateExciseTax(decimal proofGallons, int annualProductionVolume)
{
    decimal rate;

    if (annualProductionVolume <= 100000)
        rate = 2.70m;  // Reduced rate for small producers
    else if (annualProductionVolume <= 22_130_000)
        rate = 13.34m; // Standard rate (first 22.13M proof gallons)
    else
        rate = 13.50m; // Standard rate (over 22.13M proof gallons)

    return Math.Round(proofGallons * rate, 2);
}
```

**Note:** Rates change periodically—verify current rates at [TTB Excise Tax Rates](https://www.ttb.gov/tax-audit/tax-and-fee-rates).

### Official TTB Resource
- **Form:** [TTB F 5000.24](https://www.ttb.gov/ttb-form-500024)
- **Regulation:** 27 CFR 19.235

---

## Cross-Form Validation Matrix

| If this happens on... | Then this must happen on... |
|-----------------------|-----------------------------|
| **5110.40:** Spirits produced | **5110.11 or 5110.28:** Received from production |
| **5110.11:** Transferred to processing | **5110.28:** Received from storage |
| **5110.28:** Used for denaturation | **5110.43:** Alcohol received for denaturing |
| **5110.28:** Tax determination | **5000.24:** Excise tax payment |
| **5110.11:** Transferred out in bond | **5100.11:** Transfer document issued |
| **5100.11:** Spirits received | **5110.11 or 5110.28:** Received from other premises |

---

## Form Filing Calendar

### Monthly (All due 15th of following month)

```
January 1-31 operations → Reports due February 15
  ✓ Form 5110.40 (Production)
  ✓ Form 5110.11 (Storage)
  ✓ Form 5110.28 (Processing)
  ✓ Form 5110.43 (Denaturing, if applicable)
```

### Semi-Monthly or Monthly Tax Returns

```
Month 1, 1st-15th → Form 5000.24 due Month 1, 29th (if semi-monthly)
Month 1, 16th-31st → Form 5000.24 due Month 2, 14th (if semi-monthly)

OR

Month 1, entire month → Form 5000.24 due Month 2, 14th (if monthly)
```

### Per-Event Forms

- **Form 5100.11:** Completed for each transfer (no fixed due date)

---

## Implementation Priority for Caskr

### Phase 1: Core Operations (Current Sprint)
1. ✅ Form 5110.28 - Processing Report (Task TTB-001 through TTB-014)
2. ☐ Form 5110.11 - Storage Report (Future Sprint)
3. ☐ Form 5110.40 - Production Report (Future Sprint)

### Phase 2: Tax Compliance
4. ☐ Form 5000.24 - Excise Tax Return
5. ☐ Tax calculation service
6. ☐ Payment tracking

### Phase 3: Advanced Features
7. ☐ Form 5100.11 - Transfer documents
8. ☐ Form 5110.43 - Denaturing (if needed)
9. ☐ Cross-form validation service

---

## Quick Reference: Form-to-Caskr Mapping

| TTB Form | Caskr Service | Database Tables | Documentation |
|----------|---------------|-----------------|---------------|
| **5110.28** | `TtbReportCalculatorService` | `ttb_monthly_reports`, `ttb_transactions` | `TTB_FORM_5110_28_MAPPING.md` |
| **5110.11** | (Future) `TtbStorageReportService` | `ttb_inventory_snapshots`, `ttb_transactions` | (Future) |
| **5110.40** | (Future) `TtbProductionReportService` | `batches`, `ttb_transactions` | (Future) |
| **5100.11** | (Future) `TtbTransferService` | `ttb_transfers`, `ttb_transactions` | (Future) |
| **5000.24** | (Future) `TtbExciseTaxService` | `ttb_tax_payments` | (Future) |

---

## Resources

- **All TTB Forms:** [https://www.ttb.gov/forms](https://www.ttb.gov/forms)
- **Regulations:** [27 CFR Part 19](https://www.ecfr.gov/current/title-27/chapter-I/subchapter-A/part-19)
- **Industry Circulars:** [https://www.ttb.gov/regulations-and-rulings/industry-circulars](https://www.ttb.gov/regulations-and-rulings/industry-circulars)

---

**Document Version:** 1.0
**Next Review:** When implementing Forms 5110.11 or 5110.40
