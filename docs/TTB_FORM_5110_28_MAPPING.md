# TTB Form 5110.28 - Monthly Report of Processing Operations
## Comprehensive Field Mapping to Caskr Data Model

**Document Version:** 1.0
**Last Updated:** 2025-11-28
**Purpose:** Map TTB Form 5110.28 fields to Caskr database tables and calculation logic
**Compliance:** 27 CFR Part 19 Subpart V - Records and Reports

---

## Table of Contents
1. [Form Overview](#form-overview)
2. [Form Structure](#form-structure)
3. [Data Model Mapping](#data-model-mapping)
4. [Calculation Formulas](#calculation-formulas)
5. [Edge Cases](#edge-cases)
6. [Implementation Guide](#implementation-guide)

---

## Form Overview

### What is Form 5110.28?

**TTB Form 5110.28** (Monthly Report of Processing Operations) is a mandatory monthly compliance report for Distilled Spirits Plants (DSP) that conduct:
- Processing (rectifying) operations
- Bottling operations
- Packaging operations
- Denaturing operations

### Filing Requirements

| Attribute | Details |
|-----------|---------|
| **Frequency** | Monthly |
| **Due Date** | 15th day of the month following the reporting period |
| **Zero Activity** | Must file even if no activity (report zeros) |
| **Unit of Measure** | Proof gallons (primary), wine gallons (denaturing) |
| **Submission Address** | TTB National Revenue Center, 550 Main Street, Room 8002, Cincinnati, OH 45202 |

### Form Parts Overview

Form 5110.28 consists of multiple parts:

| Part | Description | Primary Unit |
|------|-------------|--------------|
| **Part I** | Bulk Ingredients - Spirits received and used for processing | Proof Gallons |
| **Part II** | Finished Products - Bottled/packaged spirits inventory | Proof Gallons |
| **Part III** | Puerto Rican, Virgin Islands, and Other Imported Rum | Proof Gallons |
| **Part IV** | Processing of Beverage (Non-industrial) Spirits | Proof Gallons |
| **Part V** | Wine Used in Wine Products (if applicable) | Wine Gallons |
| **Part VI** | Denaturation Operations | Wine Gallons |

---

## Form Structure

### Part I: Bulk Ingredients

**Purpose:** Account for bulk spirits received into processing and their disposition.

#### Key Line Items (Typical Structure)

| Line | Field Description | Data Type | Formula/Source |
|------|-------------------|-----------|----------------|
| 1 | On Hand First of Month (Opening Inventory) | Decimal (12,2) | Previous month's Line 23 OR latest `ttb_inventory_snapshots` before report month |
| 2 | Received from Production | Decimal (12,2) | Sum of `ttb_transactions` where `transaction_type = 'Production'` |
| 3 | Received from Customs | Decimal (12,2) | Sum of import transactions |
| 4 | Received from Other Bonded Premises | Decimal (12,2) | Sum of `ttb_transactions` where `transaction_type = 'TransferIn'` |
| 5 | Other Receipts | Decimal (12,2) | Sum of `ttb_transactions` where `transaction_type = 'Gain'` |
| 6 | **Total Available for Processing** | Decimal (12,2) | **Line 1 + 2 + 3 + 4 + 5** |
| 7 | Used in Production of Finished Products | Decimal (12,2) | Sum of bottling transactions (`transaction_type = 'Bottling'`) |
| 8-10 | Tax-Free Withdrawals (Government, Export, etc.) | Decimal (12,2) | Filter by `tax_status = 'TaxFree'` or `'Export'` |
| 11 | Used for Denaturation | Decimal (12,2) | Denaturing transactions (if tracked) |
| 12-20 | Transfers Out, Destroyed, Other Dispositions | Decimal (12,2) | Sum of `ttb_transactions` where `transaction_type IN ('TransferOut', 'Destruction')` |
| 21 | Losses | Decimal (12,2) | Sum of `ttb_transactions` where `transaction_type = 'Loss'` |
| 22 | Adjustments | Decimal (12,2) | Manual adjustments or rounding corrections |
| 23 | On Hand End of Month (Closing Inventory) | Decimal (12,2) | **Line 6 - (7 + 8 + ... + 22)** |
| 24 | **Total Dispositions and Inventory** | Decimal (12,2) | **Line 7 + 8 + ... + 23** (must equal Line 6) |

**Validation Rule:**
```
Line 6 (Total Available) MUST EQUAL Line 24 (Total Dispositions)
```

### Part II: Finished Products

**Purpose:** Track bottled and packaged finished products.

| Line | Field Description | Data Type | Formula/Source |
|------|-------------------|-----------|----------------|
| 25 | On Hand First of Month | Decimal (12,2) | Previous month's Line 45 |
| 26 | Produced This Month | Decimal (12,2) | Bottling transactions from Part I Line 7 |
| 27 | Received from Other Plants | Decimal (12,2) | Transfer In of finished goods |
| 28 | **Total Available** | Decimal (12,2) | **Line 25 + 26 + 27** |
| 29-43 | Tax Determinations, Exports, Transfers, Losses | Decimal (12,2) | Various transaction types |
| 45 | On Hand End of Month | Decimal (12,2) | **Line 28 - (29 + ... + 44)** |

### Part III: Puerto Rican and Virgin Islands Rum

**Purpose:** Report imported rum removed from processing account.

**Caskr Implementation:** Filter `product_type` containing "Rum" AND origin metadata (requires product_origin field).

### Part IV: Processing of Beverage Spirits

**Purpose:** Track beverage spirits by type (Whiskey, Brandy, Rum, Gin, Vodka, etc.).

| Spirit Type | Line Range | Caskr `product_type` Mapping |
|-------------|-----------|------------------------------|
| Whiskey | Lines 48-52 | "Bourbon", "Whiskey", "Rye" |
| Brandy | Lines 53-57 | "Brandy" |
| Rum | Lines 58-62 | "Rum" |
| Gin | Lines 63-67 | "Gin" |
| Vodka | Lines 68-72 | "Vodka" |
| Tequila/Mezcal | Lines 73-77 | "Tequila" |
| Other Spirits | Lines 78-82 | Other `spirit_type` values |

**Typical Line Structure per Spirit:**
- Opening Inventory
- Received/Dumped This Month
- Bottled This Month
- Losses/Adjustments
- Closing Inventory

---

## Data Model Mapping

### Caskr Tables Used for Form 5110.28

#### 1. `ttb_inventory_snapshots`

**Purpose:** Provides opening inventory (Line 1) for each reporting period.

| TTB Form Field | Caskr Column | Mapping Logic |
|----------------|--------------|---------------|
| Line 1 - Opening Inventory | `proof_gallons` | Latest snapshot WHERE `snapshot_date < first_day_of_report_month` |
| Spirit Type Breakdown | `spirits_type` | Enum: 'Under190Proof', 'Neutral190OrMore', 'Alcohol', 'Wine' |
| Product Classification | `product_type` | "Bourbon", "Whiskey", "Gin", "Vodka", etc. |
| Tax Status | `tax_status` | 'Bonded', 'TaxPaid', 'Export', 'TaxFree' |

**Query Example:**
```sql
SELECT product_type, spirits_type, SUM(proof_gallons) AS opening_inventory
FROM ttb_inventory_snapshots
WHERE company_id = :company_id
  AND snapshot_date = (
    SELECT MAX(snapshot_date)
    FROM ttb_inventory_snapshots
    WHERE company_id = :company_id
      AND snapshot_date < :report_month_start
  )
GROUP BY product_type, spirits_type;
```

#### 2. `ttb_transactions`

**Purpose:** All activity during the month (receipts, dispositions, losses).

| TTB Form Field | Transaction Type | Mapping Logic |
|----------------|------------------|---------------|
| Line 2 - Production | `Production` | SUM `proof_gallons` WHERE `transaction_type = 0` |
| Line 4 - Transfers In | `TransferIn` | SUM `proof_gallons` WHERE `transaction_type = 1` |
| Line 12+ - Transfers Out | `TransferOut` | SUM `proof_gallons` WHERE `transaction_type = 2` (multiplier: -1) |
| Line 21 - Losses | `Loss` | SUM `proof_gallons` WHERE `transaction_type = 3` (multiplier: -1) |
| Line 5 - Gains | `Gain` | SUM `proof_gallons` WHERE `transaction_type = 4` |
| Tax Determination | `TaxDetermination` | SUM `proof_gallons` WHERE `transaction_type = 5` |
| Destruction | `Destruction` | SUM `proof_gallons` WHERE `transaction_type = 6` (multiplier: -1) |
| Line 7 - Bottling | `Bottling` | SUM `proof_gallons` WHERE `transaction_type = 7` (multiplier: -1) |

**Transaction Type Enum Mapping:**
```csharp
enum TtbTransactionType {
    Production = 0,       // +1 multiplier (increases inventory)
    TransferIn = 1,       // +1 multiplier
    TransferOut = 2,      // -1 multiplier (decreases inventory)
    Loss = 3,             // -1 multiplier
    Gain = 4,             // +1 multiplier
    TaxDetermination = 5, // -1 multiplier (removes from bonded inventory)
    Destruction = 6,      // -1 multiplier
    Bottling = 7          // -1 multiplier (moves to finished goods)
}
```

**Query Example:**
```sql
SELECT
    transaction_type,
    product_type,
    spirits_type,
    SUM(proof_gallons *
        CASE
            WHEN transaction_type IN (0, 1, 4) THEN 1  -- Production, TransferIn, Gain
            ELSE -1                                     -- All others reduce inventory
        END
    ) AS net_proof_gallons
FROM ttb_transactions
WHERE company_id = :company_id
  AND transaction_date >= :report_month_start
  AND transaction_date < :report_month_end
GROUP BY transaction_type, product_type, spirits_type;
```

#### 3. `barrel`

**Purpose:** Physical inventory tracking (53 wine gallons per barrel).

| TTB Calculation | Caskr Source | Formula |
|-----------------|--------------|---------|
| Wine Gallons | `COUNT(barrels) * 53` | Standard barrel size assumption |
| Proof Gallons | Wine Gallons × (ABV × 2) / 100 | See `TtbVolumeCalculator.cs` |

**Barrel Status Filtering:**
```csharp
// Active barrels (included in inventory):
// - NOT IN ('sold', 'emptied', 'dumped', 'transferred out')

// Query logic from TtbInventorySnapshotCalculator.cs:
var activeBarrels = await context.Barrels
    .Include(b => b.Order)
        .ThenInclude(o => o.Status)
    .Where(b => b.CompanyId == companyId)
    .Where(b => b.CreatedDate < snapshotDate)
    .Where(b =>
        !new[] { "sold", "emptied", "dumped", "transferred out" }
            .Contains(b.Order.Status.Name.ToLower())
        || b.UpdatedDate >= snapshotDate  // Recently transitioned barrels
    )
    .ToListAsync();
```

#### 4. `batch`

**Purpose:** Production batch metadata (links to mash bill and production date).

| TTB Form Field | Caskr Source | Mapping |
|----------------|--------------|---------|
| Production Date | `batch.created_date` | When production transaction is logged |
| Product Type | Derived from `spirit_type` via `order.spirit_type_id` | See `TtbProductMetadataCatalog.cs` |
| Mash Bill | `batch.mash_bill_id` → `mash_bill` → `component[]` | Grain composition (informational) |

#### 5. `order`

**Purpose:** Track barrel orders, status changes, and tax determinations.

| TTB Form Field | Caskr Source | Mapping |
|----------------|--------------|---------|
| Tax Determination | `order.status_id` → status.name LIKE '%tax paid%' | Triggers `TtbTransactionType.TaxDetermination` |
| Transfer Out | `order.status_id` → status.name = 'transferred out' | Triggers `TtbTransactionType.TransferOut` |
| Spirit Type | `order.spirit_type_id` → `spirit_type.name` | "Bourbon", "Whiskey", "Rye", etc. |
| Quantity (Barrels) | `order.quantity` | Multiplied by 53 for wine gallons |

---

## Calculation Formulas

### Core Inventory Formula

The fundamental TTB inventory equation:

```
Closing Inventory = Opening Inventory
                   + Production
                   + Transfers In
                   + Gains
                   - Transfers Out
                   - Losses
                   - Tax Determinations
                   - Bottling
                   - Destruction
```

### Caskr Implementation

**Service:** `TtbReportCalculatorService.cs`
**Method:** `CalculateMonthlyReportAsync(companyId, reportYear, reportMonth)`

```csharp
// 1. Opening Inventory
var openingInventory = await GetOpeningInventoryAsync(companyId, reportStartDate);

// 2. Production
var production = await GetTransactionsByTypeAsync(
    companyId, reportStartDate, reportEndDate, TtbTransactionType.Production
);

// 3. Transfers
var transfersIn = await GetTransactionsByTypeAsync(
    companyId, reportStartDate, reportEndDate, TtbTransactionType.TransferIn
);
var transfersOut = await GetTransactionsByTypeAsync(
    companyId, reportStartDate, reportEndDate, TtbTransactionType.TransferOut
);

// 4. Losses
var losses = await GetTransactionsByTypeAsync(
    companyId, reportStartDate, reportEndDate, TtbTransactionType.Loss
);

// 5. Calculate Closing Inventory
var calculatedClosing = openingInventory.ProofGallons
    + production.ProofGallons
    + transfersIn.ProofGallons
    - transfersOut.ProofGallons
    - losses.ProofGallons;

// 6. Validate against actual snapshot
var closingSnapshot = await GetClosingInventoryAsync(companyId, reportEndDate);
var tolerance = 0.01m;
if (Math.Abs(calculatedClosing - closingSnapshot.ProofGallons) > tolerance) {
    _logger.LogWarning($"Closing inventory mismatch: calculated={calculatedClosing}, snapshot={closingSnapshot.ProofGallons}");
}
```

### Proof Gallons Calculation

**Service:** `TtbVolumeCalculator.cs`

```csharp
public static decimal CalculateProofGallons(decimal volumeGallons, decimal abv)
{
    if (volumeGallons <= 0 || abv <= 0) return 0m;

    // Proof = ABV × 2
    var proof = abv * 2m;

    // Proof Gallons = Wine Gallons × (Proof / 100)
    return Math.Round(volumeGallons * (proof / 100m), 2, MidpointRounding.AwayFromZero);
}
```

**Example:**
- 100 wine gallons of bourbon at 62.5% ABV
- Proof = 62.5 × 2 = 125
- Proof Gallons = 100 × (125 / 100) = **125.00 proof gallons**

### Wine Gallons to Proof Gallons Conversion

| Spirit Type | Default ABV | Proof | Wine Gallons → Proof Gallons |
|-------------|-------------|-------|------------------------------|
| Bourbon | 62.5% | 125 | 1 WG = 1.25 PG |
| Whiskey | 62.5% | 125 | 1 WG = 1.25 PG |
| Rye | 62.5% | 125 | 1 WG = 1.25 PG |
| Gin | 70% | 140 | 1 WG = 1.40 PG |
| Vodka | 95% | 190 | 1 WG = 1.90 PG |
| Neutral Spirits | 95% | 190+ | 1 WG = 1.90+ PG |
| Rum | 80% | 160 | 1 WG = 1.60 PG |
| Brandy | 40% | 80 | 1 WG = 0.80 PG |
| Wine | 20% | 40 | 1 WG = 0.40 PG |

**Source:** `TtbProductMetadataCatalog.cs`

### Standard Barrel Volume

```csharp
public const decimal STANDARD_BARREL_WINE_GALLONS = 53m;
```

**Used in:**
- `TtbInventorySnapshotCalculator.cs` - Inventory calculations
- `TtbTransactionLoggerService.cs` - Transaction logging

---

## Edge Cases

### 1. In-Progress Batches

**Scenario:** Batch started but not completed during reporting month.

**TTB Requirement:** Only completed production counts toward "Produced This Month."

**Caskr Implementation:**
- Production transactions only logged when batch is completed
- In-progress batches do NOT generate `TtbTransaction.Production` records
- Monitor via `batch.status` or completion date

**Recommendation:**
- Add `batch.completion_date` field (nullable)
- Log production transaction only when `completion_date` is set

### 2. Angel's Share (Evaporation Losses)

**Scenario:** Natural evaporation from aging barrels.

**TTB Requirement:** Report as "Losses" (Line 21 in Part I).

**Caskr Implementation:**
```csharp
await _ttbTransactionLogger.LogLossAsync(
    barrelId: barrel.Id,
    proofGallons: angelShareProofGallons,
    reason: "Angel's share - natural evaporation"
);
```

**Best Practice:**
- Calculate monthly based on aging duration and warehouse conditions
- Apply industry standard loss rates (2-4% per year for bourbon)
- Log at month-end during snapshot reconciliation

### 3. Spillage and Breakage

**Scenario:** Accidental loss due to broken barrel or handling error.

**TTB Requirement:** Report as "Losses" with documentation.

**Caskr Implementation:**
```csharp
await _ttbTransactionLogger.LogLossAsync(
    barrelId: barrel.Id,
    proofGallons: spillageProofGallons,
    reason: "Spillage - barrel breach during transport"
);
```

**Documentation:** Store incident report in `ttb_transactions.notes` field.

### 4. Transfers Between Bonded Premises

**Scenario:** Spirits moved from one DSP to another under bond (tax-deferred).

**TTB Requirement:**
- Sending facility: Report as "Transfer Out" (Line 12+)
- Receiving facility: Report as "Transfer In" (Line 4)

**Caskr Implementation:**
```csharp
// Sending facility
await _ttbTransactionLogger.LogTransferOutAsync(transferId);

// Receiving facility
await _ttbTransactionLogger.LogTransferInAsync(transferId);
```

**Data Model:** Requires `transfer` table linking two companies:
```csharp
public class Transfer {
    public int Id { get; set; }
    public int FromCompanyId { get; set; }
    public int ToCompanyId { get; set; }
    public int BarrelId { get; set; }
    public DateTime TransferDate { get; set; }
    public decimal ProofGallons { get; set; }
    public string TransferDocumentNumber { get; set; } // TTB Form 5100.11
}
```

### 5. Tax Determination Events

**Scenario:** Spirits removed from bonded storage for tax-paid sale.

**TTB Requirement:** Report tax determination in month it occurs.

**Caskr Implementation:**
```csharp
// Triggered when order status changes to "tax paid"
await _ttbTransactionLogger.LogTaxDeterminationAsync(orderId);
```

**Note:** Reduces bonded inventory but may increase tax-paid inventory (tracked separately).

### 6. Gains (Rare)

**Scenario:** Inventory increases due to temperature changes or measurement corrections.

**TTB Requirement:** Report as "Other Receipts" (Line 5).

**Caskr Implementation:**
```csharp
var transaction = new TtbTransaction {
    CompanyId = companyId,
    TransactionType = TtbTransactionType.Gain,
    ProofGallons = gainAmount,
    Notes = "Inventory adjustment - temperature correction"
};
await _context.TtbTransactions.AddAsync(transaction);
```

### 7. Spirits ≥190 Proof (Neutral Spirits)

**Scenario:** Vodka or neutral grain spirits at very high proof.

**TTB Requirement:** Report separately in Part I under "Neutral Spirits" category.

**Caskr Implementation:**
```csharp
// Filter by spirits_type
var neutralSpirits = await _context.TtbTransactions
    .Where(t => t.SpiritsType == TtbSpiritsType.Neutral190OrMore)
    .SumAsync(t => t.ProofGallons);
```

**Classification Logic:**
- Vodka, Neutral Spirits, Alcohol ≥95% ABV → `Neutral190OrMore`
- Bourbon, Whiskey, Gin, Rum → `Under190Proof`

### 8. Zero Activity Months

**Scenario:** No production, transfers, or dispositions during month.

**TTB Requirement:** Must still file report showing:
- Opening Inventory (from previous month)
- Closing Inventory (same as opening)
- All other lines = 0

**Caskr Implementation:**
```csharp
var report = new TtbMonthlyReportData {
    OpeningInventory = previousClosing,
    Production = 0,
    Transfers = new TransfersSection { TransfersIn = 0, TransfersOut = 0 },
    Losses = 0,
    ClosingInventory = previousClosing  // No change
};
```

### 9. Partial Month Operations

**Scenario:** New DSP opens mid-month.

**TTB Requirement:** Report from date of operation start.

**Caskr Implementation:**
- Opening inventory = 0 (if first month)
- Use `company.created_date` to determine partial month reporting
- Prorate calculations if necessary (consult TTB guidance)

### 10. Inventory Shortages

**Scenario:** Physical inventory count is less than book inventory.

**TTB Requirement:** Report shortage as "Loss" and include on excise tax return.

**Caskr Implementation:**
```csharp
var shortage = bookInventory - physicalInventory;
if (shortage > 0) {
    await _ttbTransactionLogger.LogLossAsync(
        barrelId: null,  // Not tied to specific barrel
        proofGallons: shortage,
        reason: "Inventory shortage - annual physical count reconciliation"
    );
}
```

---

## Implementation Guide

### Step 1: Ensure Daily Snapshots are Running

**Background Service:** `TtbInventorySnapshotService`

**Verification:**
```bash
# Check if service is running
grep "TtbInventorySnapshotService" logs/application.log

# Verify recent snapshots exist
SELECT snapshot_date, COUNT(*)
FROM ttb_inventory_snapshots
GROUP BY snapshot_date
ORDER BY snapshot_date DESC
LIMIT 7;
```

**Manual Backfill (if needed):**
```csharp
var snapshotService = serviceProvider.GetRequiredService<TtbInventorySnapshotService>();
await snapshotService.BackfillSnapshotsAsync(
    companyId: 1,
    startDate: new DateTime(2024, 1, 1),
    endDate: DateTime.Today
);
```

### Step 2: Configure Spirit Type Metadata

**File:** `TtbProductMetadataCatalog.cs`

**Action:** Verify all company spirit types are mapped with correct ABV defaults.

```csharp
public static TtbProductMetadata GetMetadata(string productType) {
    return productType.ToLower() switch {
        "bourbon" => new TtbProductMetadata {
            DefaultAbv = 62.5m,
            SpiritsType = TtbSpiritsType.Under190Proof
        },
        "vodka" => new TtbProductMetadata {
            DefaultAbv = 95m,
            SpiritsType = TtbSpiritsType.Neutral190OrMore
        },
        // Add custom spirit types as needed
        _ => throw new ArgumentException($"Unknown product type: {productType}")
    };
}
```

### Step 3: Generate Monthly Report

**Service:** `TtbReportCalculatorService`

**API Endpoint:**
```csharp
[HttpPost("api/ttb/reports/monthly")]
public async Task<IActionResult> GenerateMonthlyReport(
    [FromBody] GenerateReportRequest request
) {
    var reportData = await _ttbReportCalculator.CalculateMonthlyReportAsync(
        companyId: request.CompanyId,
        reportYear: request.Year,
        reportMonth: request.Month
    );

    // Create report record
    var report = new TtbMonthlyReport {
        CompanyId = request.CompanyId,
        ReportMonth = request.Month,
        ReportYear = request.Year,
        Status = TtbReportStatus.Draft,
        ReportData = JsonSerializer.Serialize(reportData),
        CreatedByUserId = User.GetUserId()
    };

    await _context.TtbMonthlyReports.AddAsync(report);
    await _context.SaveChangesAsync();

    return Ok(report);
}
```

### Step 4: Map to Form 5110.28 Structure

**Transformation Logic:**

```csharp
public class Form511028Mapper {
    public Form511028Data MapToForm(TtbMonthlyReportData reportData) {
        var form = new Form511028Data();

        // Part I: Bulk Ingredients
        form.PartI.Line1_OpeningInventory = reportData.OpeningInventory
            .Where(i => i.SpiritsType == TtbSpiritsType.Under190Proof)
            .Sum(i => i.ProofGallons);

        form.PartI.Line2_Production = reportData.Production
            .Where(p => p.SpiritsType == TtbSpiritsType.Under190Proof)
            .Sum(p => p.ProofGallons);

        form.PartI.Line4_TransfersIn = reportData.Transfers.TransfersIn
            .Sum(t => t.ProofGallons);

        form.PartI.Line21_Losses = reportData.Losses
            .Sum(l => l.ProofGallons);

        // Calculate Line 23: Closing Inventory
        form.PartI.Line23_ClosingInventory =
            form.PartI.Line1_OpeningInventory +
            form.PartI.Line2_Production +
            form.PartI.Line4_TransfersIn -
            form.PartI.Line21_Losses;

        // Part IV: Beverage Spirits by Type
        form.PartIV.Whiskey = MapSpiritTypeSection(reportData, "Bourbon", "Whiskey", "Rye");
        form.PartIV.Gin = MapSpiritTypeSection(reportData, "Gin");
        form.PartIV.Vodka = MapSpiritTypeSection(reportData, "Vodka");
        form.PartIV.Rum = MapSpiritTypeSection(reportData, "Rum");
        form.PartIV.Brandy = MapSpiritTypeSection(reportData, "Brandy");

        return form;
    }

    private SpiritTypeSection MapSpiritTypeSection(
        TtbMonthlyReportData data,
        params string[] productTypes
    ) {
        var filtered = data.Transactions
            .Where(t => productTypes.Contains(t.ProductType));

        return new SpiritTypeSection {
            OpeningInventory = filtered.FirstOrDefault()?.OpeningInventory ?? 0,
            Dumped = filtered.Where(t => t.Type == "Production").Sum(t => t.ProofGallons),
            Bottled = filtered.Where(t => t.Type == "Bottling").Sum(t => t.ProofGallons),
            Losses = filtered.Where(t => t.Type == "Loss").Sum(t => t.ProofGallons),
            ClosingInventory = /* calculated */
        };
    }
}
```

### Step 5: Validation and Submission

**Pre-Submission Checklist:**

- [ ] All lines balance (Total Available = Total Dispositions)
- [ ] Closing inventory matches calculated value (within tolerance)
- [ ] All required product types are represented
- [ ] Losses are documented with notes
- [ ] Transfers have corresponding TTB Form 5100.11 references
- [ ] Zero activity months show zeros (not blanks)

**Validation Service:**
```csharp
public class Form511028Validator {
    public ValidationResult Validate(Form511028Data form) {
        var errors = new List<string>();

        // Check balance
        var totalAvailable = form.PartI.Line6_TotalAvailable;
        var totalDispositions = form.PartI.Line24_TotalDispositions;
        if (Math.Abs(totalAvailable - totalDispositions) > 0.01m) {
            errors.Add($"Part I does not balance: Available={totalAvailable}, Dispositions={totalDispositions}");
        }

        // Check negative values
        if (form.PartI.Line1_OpeningInventory < 0) {
            errors.Add("Opening inventory cannot be negative");
        }

        return new ValidationResult {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}
```

### Step 6: Generate PDF for Submission

**PDF Generation:** Use library like QuestPDF or iText to fill official form template.

**Alternative:** Export to Excel format matching TTB's electronic submission template.

```csharp
public async Task<byte[]> GeneratePdfAsync(int reportId) {
    var report = await _context.TtbMonthlyReports
        .Include(r => r.Company)
        .FirstOrDefaultAsync(r => r.Id == reportId);

    var reportData = JsonSerializer.Deserialize<TtbMonthlyReportData>(report.ReportData);
    var formData = _mapper.MapToForm(reportData);

    // Generate PDF using template
    var pdf = _pdfGenerator.GenerateForm511028(formData, report.Company);

    // Store PDF path
    report.PdfPath = $"ttb_reports/{report.Id}_5110_28_{report.ReportYear}_{report.ReportMonth:D2}.pdf";
    await _context.SaveChangesAsync();

    return pdf;
}
```

---

## Required Attachments and Supporting Documentation

### Documents to Maintain

1. **Daily Inventory Records**
   - Source: `ttb_inventory_snapshots` table
   - Retention: 3 years per TTB regulations

2. **Transaction Logs**
   - Source: `ttb_transactions` table
   - Includes: Production, transfers, losses, gains
   - Retention: 3 years

3. **Transfer Documents**
   - TTB Form 5100.11 (Transfer in Bond)
   - Must reference in Line 4 (Transfers In) and Line 12+ (Transfers Out)

4. **Loss Documentation**
   - Incident reports for spillage/breakage
   - Angel's share calculation worksheets
   - Stored in: `ttb_transactions.notes` field

5. **Production Records**
   - Batch sheets with mash bill
   - Source: `batch`, `mash_bill`, `component` tables
   - Fermentation and distillation logs

6. **Bottling Records**
   - Bottle counts and proof
   - Label approval (COLA) references

7. **Prior Month Reports**
   - Previous Form 5110.28 (for opening inventory verification)
   - Source: `ttb_monthly_reports` table

8. **Related Forms**
   - Form 5110.11 (Storage Operations) - cross-reference inventory
   - Form 5110.40 (Production Operations)
   - Form 5110.43 (Denaturing Operations, if applicable)

---

## Database Schema Updates Needed

### Recommended Additions

#### 1. Add Completion Date to Batch
```sql
ALTER TABLE batch
ADD COLUMN completion_date TIMESTAMPTZ NULL;

COMMENT ON COLUMN batch.completion_date IS
'Date when batch production was completed and logged for TTB reporting';
```

#### 2. Add Transfer Table
```sql
CREATE TABLE ttb_transfer (
    id BIGSERIAL PRIMARY KEY,
    from_company_id INTEGER NOT NULL REFERENCES company(id),
    to_company_id INTEGER NOT NULL REFERENCES company(id),
    transfer_date DATE NOT NULL,
    ttb_form_number VARCHAR(50),  -- Reference to Form 5100.11
    barrel_ids INTEGER[] NOT NULL,
    proof_gallons DECIMAL(12,2) NOT NULL,
    wine_gallons DECIMAL(12,2) NOT NULL,
    product_type TEXT NOT NULL,
    spirits_type VARCHAR(20) NOT NULL,
    status VARCHAR(20) DEFAULT 'Pending',  -- Pending, InTransit, Received
    created_at TIMESTAMPTZ DEFAULT NOW(),
    received_at TIMESTAMPTZ NULL
);

CREATE INDEX idx_ttb_transfer_from_company ON ttb_transfer(from_company_id, transfer_date);
CREATE INDEX idx_ttb_transfer_to_company ON ttb_transfer(to_company_id, transfer_date);
```

#### 3. Add Product Origin Field
```sql
ALTER TABLE barrel
ADD COLUMN product_origin VARCHAR(50) NULL;

COMMENT ON COLUMN barrel.product_origin IS
'Origin for imported spirits: PuertoRico, VirginIslands, Imported, Domestic';
```

#### 4. Extend Company for TTB Metadata
```sql
ALTER TABLE company
ADD COLUMN ttb_dsp_number VARCHAR(50) NULL,
ADD COLUMN ttb_plant_type VARCHAR(100) NULL;  -- 'Distillery', 'Processor', 'Bottler', etc.

COMMENT ON COLUMN company.ttb_dsp_number IS
'TTB Distilled Spirits Plant registration number';
```

---

## Testing Checklist

### Unit Tests

- [ ] Proof gallons calculation (`TtbVolumeCalculator`)
- [ ] Transaction multiplier application (Production vs Loss)
- [ ] Inventory snapshot aggregation by product type
- [ ] Spirit type classification logic
- [ ] Opening inventory retrieval (latest before month start)

### Integration Tests

- [ ] Full monthly report generation
- [ ] Form 5110.28 mapping from `TtbMonthlyReportData`
- [ ] Balance validation (Total Available = Total Dispositions)
- [ ] Cross-form consistency (5110.11, 5110.28, 5110.40)

### End-to-End Tests

- [ ] Create batch → Log production → Verify appears in report
- [ ] Transfer barrel → Verify TransferOut in sending company, TransferIn in receiving
- [ ] Log loss → Verify reduces inventory and appears on Line 21
- [ ] Change order to "tax paid" → Verify TaxDetermination transaction
- [ ] Zero activity month → Report shows opening = closing

### Data Validation Tests

- [ ] Import historical data → Backfill snapshots → Generate reports for past 12 months
- [ ] Compare calculated closing vs actual snapshot (tolerance check)
- [ ] Verify no negative inventory values
- [ ] Confirm all product types have ABV metadata

---

## References

### TTB Official Resources

- [TTB Form 5110.28 Official Page](https://www.ttb.gov/ttb-form-511028)
- [Form 5110.28 PDF](https://www.ttb.gov/images/pdfs/forms/f511028.pdf)
- [Helpful Hints for Form 5110.28](https://www.ttb.gov/images/pdfs/forms/5110.28pr-hh.pdf)
- [27 CFR Part 19 - Distilled Spirits Plants](https://www.ecfr.gov/current/title-27/chapter-I/subchapter-A/part-19)
- [TTB Forms Index](https://www.ttb.gov/forms)

### Related Forms

- **Form 5110.11** - Monthly Report of Storage Operations
- **Form 5110.40** - Monthly Report of Production Operations
- **Form 5110.43** - Monthly Report of Denaturing Operations
- **Form 5100.11** - Transfer of Spirits and/or Denatured Spirits in Bond

### Caskr Implementation Files

- `/home/user/Caskr/Caskr.Server/Models/TtbMonthlyReport.cs`
- `/home/user/Caskr/Caskr.Server/Models/TtbInventorySnapshot.cs`
- `/home/user/Caskr/Caskr.Server/Models/TtbTransaction.cs`
- `/home/user/Caskr/Caskr.Server/Services/TtbReportCalculatorService.cs`
- `/home/user/Caskr/Caskr.Server/Services/TtbInventorySnapshotCalculator.cs`
- `/home/user/Caskr/Caskr.Server/Services/TtbVolumeCalculator.cs`

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-11-28 | Claude AI | Initial comprehensive documentation |

---

**END OF DOCUMENT**
