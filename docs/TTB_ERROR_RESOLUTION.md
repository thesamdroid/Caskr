# TTB Error Resolution Procedures

**Purpose:** Troubleshooting guide for TTB calculation and reporting errors
**Last Updated:** 2025-11-28
**For:** Developers, operations, compliance officers

---

## Table of Contents

1. [Common Errors](#common-errors)
2. [Diagnostic Procedures](#diagnostic-procedures)
3. [Resolution Steps](#resolution-steps)
4. [When to Contact TTB](#when-to-contact-ttb)
5. [Emergency Procedures](#emergency-procedures)

---

## Common Errors

### Error 1: "Inventory Balance Does Not Match"

**Symptoms:**
```
Closing inventory mismatch for Bourbon/Under190Proof on 2024-01-31:
calculated 662.50 PG vs snapshot 660.00 PG
```

**Root Causes:**
1. Missing transaction (loss not logged, transfer not recorded)
2. Incorrect ABV in product metadata
3. Barrel count mismatch (barrels created but not logged as production)
4. Angel's share not calculated for month

**Diagnostic Steps:**

```sql
-- 1. Check opening inventory
SELECT product_type, spirits_type, SUM(proof_gallons) AS opening
FROM ttb_inventory_snapshots
WHERE company_id = 1
  AND snapshot_date = '2024-01-01'  -- First day of month
GROUP BY product_type, spirits_type;

-- 2. Check all transactions for the month
SELECT
    transaction_type,
    SUM(proof_gallons) AS total_pg,
    COUNT(*) AS transaction_count
FROM ttb_transactions
WHERE company_id = 1
  AND transaction_date >= '2024-01-01'
  AND transaction_date <= '2024-01-31'
GROUP BY transaction_type;

-- 3. Check closing inventory snapshot
SELECT product_type, spirits_type, SUM(proof_gallons) AS closing
FROM ttb_inventory_snapshots
WHERE company_id = 1
  AND snapshot_date = '2024-01-31'  -- Last day of month
GROUP BY product_type, spirits_type;

-- 4. Manual balance check
-- Closing should = Opening + Production + TransfersIn - TransfersOut - Losses
```

**Resolution:**

If discrepancy is ≤ 0.01 PG:
- ✅ **ACCEPTABLE** - Rounding difference within TTB tolerance
- Action: None required (warning is informational)

If discrepancy is > 0.01 PG:
- ❌ **UNACCEPTABLE** - Must investigate
- Actions:
  1. Find missing transaction
  2. Log adjustment transaction if needed
  3. Re-generate snapshot
  4. Re-run monthly report

```csharp
// Log adjustment transaction
await _ttbTransactionLogger.LogGainAsync(
    companyId: 1,
    proofGallons: 2.50m,  // Difference amount
    wineGallons: 2.00m,
    reason: "Inventory adjustment - reconciliation discrepancy",
    date: new DateTime(2024, 1, 31)
);

// Re-generate snapshot
await _snapshotService.GenerateSnapshotAsync(1, new DateTime(2024, 1, 31));

// Re-run report
var report = await _calculator.CalculateMonthlyReportAsync(1, 1, 2024);
```

---

### Error 2: "Proof Gallons Calculation Incorrect"

**Symptoms:**
```
Expected: 66.25 PG
Actual: 66.00 PG
Difference: 0.25 PG
```

**Root Causes:**
1. Wrong ABV % in spirit metadata
2. Rounding error in calculation
3. Using wrong formula

**Diagnostic Steps:**

```csharp
// Test calculation manually
var wineGallons = 53m;
var abv = 62.5m;

var calculated = TtbVolumeCalculator.CalculateProofGallons(wineGallons, abv);
// Expected: 53 × (62.5 × 2 / 100) = 53 × 1.25 = 66.25

Assert.Equal(66.25m, calculated);
```

**Check ABV metadata:**

```csharp
var metadata = TtbProductMetadataCatalog.GetMetadata("Bourbon");
Console.WriteLine($"Default ABV: {metadata.DefaultAbv}");
// Should be: 62.5
```

**Resolution:**

If ABV is wrong:
```csharp
// Update product metadata catalog
public static TtbProductMetadata GetMetadata(string productType)
{
    return productType.ToLower() switch
    {
        "bourbon" => new() { DefaultAbv = 62.5m, ... },  // CORRECT
        // NOT 60.0 or 65.0!
        ...
    };
}
```

If formula is wrong:
- ❌ **CRITICAL ERROR** - Formula is defined by federal law
- See: `docs/TTB_COMPLIANCE_GUIDE.md`, Section "Never Modify These Constants"
- Revert to correct formula immediately

---

### Error 3: "Opening Inventory Does Not Match Previous Closing"

**Symptoms:**
```
February opening inventory: 1000.00 PG
January closing inventory: 950.00 PG
Difference: 50.00 PG
```

**Root Causes:**
1. Snapshot not generated for end of previous month
2. Transactions logged between months (backdated)
3. Data corruption or manual database edit

**Diagnostic Steps:**

```sql
-- Check if January closing snapshot exists
SELECT *
FROM ttb_inventory_snapshots
WHERE company_id = 1
  AND snapshot_date = '2024-01-31'
ORDER BY product_type, spirits_type;

-- Check for transactions dated between Jan 31 and Feb 1
SELECT *
FROM ttb_transactions
WHERE company_id = 1
  AND transaction_date > '2024-01-31 00:00:00'
  AND transaction_date < '2024-02-01 00:00:00'
  AND created_at > '2024-01-31 23:59:59';  -- Created after month ended
```

**Resolution:**

If snapshot missing:
```csharp
// Backfill snapshot for January 31
await _snapshotService.GenerateSnapshotAsync(1, new DateTime(2024, 1, 31));

// Re-run February report
var report = await _calculator.CalculateMonthlyReportAsync(1, 2, 2024);
```

If transactions backdated:
- Investigate why transaction was backdated
- If legitimate: Re-calculate January closing + February opening
- If error: Move transaction to correct month

```sql
-- Move transaction to correct month (with audit trail)
UPDATE ttb_transactions
SET transaction_date = '2024-02-01',
    notes = CONCAT(notes, ' | Originally dated 2024-01-31, moved to 2024-02-01 per resolution TTB-ERR-20240215')
WHERE id = 12345;
```

---

### Error 4: "Negative Inventory"

**Symptoms:**
```
Closing inventory: -50.00 PG
```

**This should NEVER happen. Negative inventory is impossible.**

**Root Causes:**
1. Transactions logged without corresponding barrels
2. Double-counting of losses
3. Transfer logged twice (both sending and receiving counted as outbound)

**Diagnostic Steps:**

```sql
-- Find all transactions for this product type
SELECT
    transaction_date,
    transaction_type,
    proof_gallons,
    notes
FROM ttb_transactions
WHERE company_id = 1
  AND product_type = 'Bourbon'
  AND transaction_date >= '2024-01-01'
  AND transaction_date <= '2024-01-31'
ORDER BY transaction_date, id;

-- Check for duplicate transactions
SELECT
    transaction_date,
    transaction_type,
    proof_gallons,
    COUNT(*) AS duplicate_count
FROM ttb_transactions
WHERE company_id = 1
GROUP BY transaction_date, transaction_type, proof_gallons
HAVING COUNT(*) > 1;
```

**Resolution:**

1. **STOP ALL TTB SUBMISSIONS** - Report with negative inventory is invalid
2. Identify duplicate or erroneous transactions
3. Log correcting adjustment transactions
4. Re-generate all snapshots for the month
5. Verify closing inventory is positive
6. Document incident in `TTB_AUDIT_CHANGE_CONTROL.md`

```csharp
// Example: Remove duplicate loss
// If loss was logged twice: 50 PG + 50 PG = 100 PG total
// But should only be 50 PG

// Log gain to offset the duplicate loss
await _ttbTransactionLogger.LogGainAsync(
    companyId: 1,
    proofGallons: 50m,
    reason: "Correction - duplicate loss transaction removed. See incident TTB-INC-20240215-001"
);
```

---

### Error 5: "Wrong Spirit Type Classification"

**Symptoms:**
```
Vodka (95% ABV) classified as Under190Proof
Should be: Neutral190OrMore
```

**Root Causes:**
1. ABV threshold check incorrect
2. Spirit type enum mapping wrong
3. Product metadata incomplete

**Diagnostic Steps:**

```csharp
// Check classification logic
public static TtbSpiritsType ClassifySpiritsType(decimal abv)
{
    var proof = abv * 2m;

    if (proof >= 190m)
        return TtbSpiritsType.Neutral190OrMore;
    else
        return TtbSpiritsType.Under190Proof;
}

// Test with vodka
var vodkaAbv = 95m;
var classification = ClassifySpiritsType(vodkaAbv);
// Should be: Neutral190OrMore (95 × 2 = 190)
```

**Resolution:**

Update classification logic:
```csharp
public static TtbProductMetadata GetMetadata(string productType)
{
    return productType.ToLower() switch
    {
        "vodka" => new()
        {
            DefaultAbv = 95m,
            SpiritsType = TtbSpiritsType.Neutral190OrMore  // CORRECT
        },
        "bourbon" => new()
        {
            DefaultAbv = 62.5m,
            SpiritsType = TtbSpiritsType.Under190Proof  // CORRECT
        },
        ...
    };
}
```

---

## Diagnostic Procedures

### Procedure 1: Full Month Audit

**Use when:** Monthly report doesn't balance or has unexpected values

**Steps:**

```bash
# 1. Export all TTB data for the month
psql -d caskr-db -c "
    COPY (
        SELECT * FROM ttb_transactions
        WHERE company_id = 1
          AND transaction_date >= '2024-01-01'
          AND transaction_date <= '2024-01-31'
        ORDER BY transaction_date, id
    ) TO '/tmp/ttb_transactions_2024_01.csv' CSV HEADER;
"

# 2. Export snapshots
psql -d caskr-db -c "
    COPY (
        SELECT * FROM ttb_inventory_snapshots
        WHERE company_id = 1
          AND snapshot_date IN ('2024-01-01', '2024-01-31')
        ORDER BY snapshot_date, product_type
    ) TO '/tmp/ttb_snapshots_2024_01.csv' CSV HEADER;
"

# 3. Open in Excel and manually verify balance equation
```

In Excel:
```
Column A: Opening (from Jan 1 snapshot)
Column B: Production (SUM of Production transactions)
Column C: Transfers In (SUM of TransferIn transactions)
Column D: Transfers Out (SUM of TransferOut transactions)
Column E: Losses (SUM of Loss transactions)
Column F: Calculated Closing = A + B + C - D - E
Column G: Actual Closing (from Jan 31 snapshot)
Column H: Difference = F - G

If Column H > 0.01, investigate.
```

### Procedure 2: Transaction Trace

**Use when:** Specific transaction seems incorrect

**Steps:**

```sql
-- 1. Find the transaction
SELECT * FROM ttb_transactions WHERE id = 12345;

-- 2. Check what triggered it (source entity)
-- If source_entity_type = 'Batch':
SELECT * FROM batches WHERE id = (SELECT source_entity_id FROM ttb_transactions WHERE id = 12345);

-- If source_entity_type = 'Barrel':
SELECT * FROM barrels WHERE id = (SELECT source_entity_id FROM ttb_transactions WHERE id = 12345);

-- 3. Check audit log for when it was created
SELECT * FROM ttb_audit_log
WHERE table_name = 'ttb_transactions'
  AND record_id = 12345
ORDER BY changed_at DESC;

-- 4. Verify calculation
-- If it's a production transaction, check proof gallons calculation
SELECT
    o.quantity AS barrel_count,
    53 AS wine_gallons_per_barrel,
    st.name AS spirit_type,
    -- Calculate: quantity × 53 × (ABV × 2 / 100)
    -- Bourbon at 62.5% ABV: × 1.25
FROM orders o
JOIN spirit_types st ON st.id = o.spirit_type_id
WHERE o.batch_id = (SELECT source_entity_id FROM ttb_transactions WHERE id = 12345);
```

---

## Resolution Steps

### Step 1: Identify Root Cause

Use diagnostic procedures above to pinpoint issue.

### Step 2: Determine Impact

```
Is the error in a submitted report?
├─ YES → Follow "Critical Incident" procedure (TTB_AUDIT_CHANGE_CONTROL.md)
└─ NO → Continue to Step 3
```

### Step 3: Fix Data

Depending on error type:

**For missing transactions:**
```csharp
await _ttbTransactionLogger.LogLossAsync(...);  // Add missing transaction
```

**For incorrect ABV:**
```csharp
// Update TtbProductMetadataCatalog.cs
// Re-calculate all affected transactions
```

**For duplicate transactions:**
```csharp
// Log offsetting transaction (Gain to offset Loss, or vice versa)
await _ttbTransactionLogger.LogGainAsync(...);
```

### Step 4: Re-Generate Affected Snapshots

```csharp
// Re-generate snapshots for all affected dates
foreach (var date in affectedDates)
{
    await _snapshotService.GenerateSnapshotAsync(companyId, date);
}
```

### Step 5: Re-Calculate Reports

```csharp
// Re-run monthly report calculation
var report = await _calculator.CalculateMonthlyReportAsync(companyId, month, year);

// Verify balance
Assert.True(report.IsBalanced);
```

### Step 6: Document Resolution

Update incident log in `TTB_AUDIT_CHANGE_CONTROL.md`.

---

## When to Contact TTB

### Contact TTB If:

1. ✅ Error discovered **after** report submission
2. ✅ Error affects tax liability (over/underpayment)
3. ✅ Systematic calculation error affecting multiple months
4. ✅ Data loss or corruption
5. ✅ Uncertainty about regulation interpretation

### Do NOT Contact TTB If:

1. ❌ Error caught before submission (fix and re-submit correct report)
2. ❌ Rounding difference within 0.01 PG tolerance
3. ❌ Procedural question answered in regulations (read 27 CFR 19 first)

### How to Contact TTB

**For Technical Questions:**
- Email: [alfd.nrc@ttb.gov](mailto:alfd.nrc@ttb.gov)
- Phone: 877-882-3277 (TTB National Revenue Center)
- Hours: Monday-Friday, 8:00 AM - 5:00 PM ET

**For Emergencies (Submitted Report Error):**
1. Call NRC immediately: 877-882-3277
2. Explain situation clearly
3. Provide:
   - Company name and permit number
   - Report month and form number
   - Nature of error
   - Magnitude of error (proof gallons)
4. Follow TTB instructions for corrected submission

---

## Emergency Procedures

### Emergency 1: Production System Failure Before Submission Deadline

**Scenario:** It's January 14, and your system crashes. Reports are due January 15.

**Actions:**

1. **Immediate (Hour 1):**
   - [ ] Notify compliance officer
   - [ ] Check backup status
   - [ ] Assess if deadline can be met

2. **If deadline cannot be met:**
   - [ ] Draft extension request letter to TTB
   - [ ] Explain circumstances (system failure)
   - [ ] Propose new submission date
   - [ ] Submit request BEFORE deadline

3. **Recovery:**
   - [ ] Restore from backup
   - [ ] Verify data integrity
   - [ ] Generate reports
   - [ ] Submit by extended deadline

### Emergency 2: Discovery of Systematic Calculation Error

**Scenario:** You discover the proof gallon formula was wrong for 6 months.

**Actions:**

1. **Immediate (Day 1):**
   - [ ] STOP all further TTB submissions
   - [ ] Notify legal counsel and compliance officer
   - [ ] Quantify error magnitude
   - [ ] Identify affected months

2. **Assessment (Days 2-3):**
   - [ ] Calculate corrected values for all affected months
   - [ ] Determine tax impact
   - [ ] Draft corrective action plan

3. **TTB Notification (Day 4):**
   - [ ] Contact TTB National Revenue Center
   - [ ] Submit written notice of error
   - [ ] Propose remediation plan

4. **Remediation (Weeks 2-4):**
   - [ ] Fix calculation logic
   - [ ] Re-submit corrected reports
   - [ ] Pay any additional taxes owed
   - [ ] Document lessons learned

---

## Escalation Matrix

| Error Severity | Response Time | Escalation Path |
|----------------|---------------|-----------------|
| **Critical** (Submitted report error) | 1 hour | Developer → Compliance Officer → Legal → CEO → TTB |
| **High** (Pre-submission validation failure) | 4 hours | Developer → Compliance Officer |
| **Medium** (Data discrepancy) | 1 business day | Developer → Team Lead → Compliance Officer |
| **Low** (Documentation issue) | 1 week | Developer → Team Lead |

---

## Prevention

**The best error resolution is error prevention.**

1. **Use the compliance framework:**
   - Read `TTB_FORM_5110_28_MAPPING.md` before coding
   - Follow `TTB_COMPLIANCE_GUIDE.md` workflow
   - Apply `TTB_TESTING_VALIDATION_GUIDE.md` tests

2. **Never skip validation:**
   - Run regression tests before every commit
   - Manual spot-check random months
   - Cross-validate with physical inventory quarterly

3. **Maintain audit trail:**
   - Document all changes in `CHANGELOG_TTB.md`
   - Log every transaction with detailed notes
   - Retain all supporting documentation

4. **When in doubt, ask:**
   - Consult compliance officer
   - Review TTB regulations
   - Contact TTB if truly uncertain

---

## Resources

- **TTB Contact:** 877-882-3277 or [alfd.nrc@ttb.gov](mailto:alfd.nrc@ttb.gov)
- **Regulations:** [27 CFR Part 19](https://www.ecfr.gov/current/title-27/chapter-I/subchapter-A/part-19)
- **Caskr Compliance Docs:** `docs/TTB_*.md`

---

**Document Version:** 1.0
**Last Updated:** 2025-11-28
