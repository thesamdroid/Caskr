# TTB Compliance Guide for Developers and AI Agents

**Purpose:** Ensure all TTB-related development maintains regulatory compliance
**Last Updated:** 2025-11-28
**Applies To:** All TTB monthly reporting features (Forms 5110.28, 5110.11, 5110.40)

---

## 🚨 Critical Compliance Rules

### 1. ALWAYS Reference the Mapping Document

**Before implementing ANY TTB feature, you MUST:**

✓ Read `docs/TTB_FORM_5110_28_MAPPING.md`
✓ Understand the form structure and calculations
✓ Follow the data model mappings exactly
✓ Implement validation rules as specified

**Why:** TTB regulations are federal law. Incorrect reporting can result in:
- Federal penalties
- License suspension
- Audit failures
- Criminal liability for intentional misreporting

### 2. Never Modify These Constants Without TTB Authority

```csharp
// ❌ DO NOT CHANGE without verifying against TTB regulations
public const decimal STANDARD_BARREL_WINE_GALLONS = 53m;
public const decimal SnapshotTolerance = 0.01m;  // Proof gallon rounding tolerance
```

**If you need to change these:** Cite the specific TTB regulation (27 CFR Part 19) that authorizes the change.

### 3. Proof Gallon Formula is NON-NEGOTIABLE

```csharp
// ✅ CORRECT - This is the official TTB formula
ProofGallons = WineGallons × (ABV × 2) / 100

// ❌ INCORRECT - Do not use approximations or shortcuts
ProofGallons = WineGallons × ABV  // WRONG
```

### 4. Inventory Balance Equation Must Always Hold

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

**Every monthly report MUST balance.** If it doesn't, find the bug—don't fudge the numbers.

### 5. Transaction Multipliers Are Fixed

```csharp
// These are set by regulation, not business logic
Production      → +1 (adds to inventory)
TransferIn      → +1 (adds to inventory)
Gain            → +1 (adds to inventory)
TransferOut     → -1 (removes from inventory)
Loss            → -1 (removes from inventory)
TaxDetermination→ -1 (removes from bonded inventory)
Bottling        → -1 (removes from bulk inventory)
Destruction     → -1 (removes from inventory)
```

---

## 📋 Development Workflow for TTB Features

### Step 1: Review Mapping Document FIRST

```bash
# Open the compliance document before writing any code
cat docs/TTB_FORM_5110_28_MAPPING.md
```

**Find:**
- Which form lines your feature affects
- Which database tables to query
- Which calculation formulas to use
- Edge cases that apply to your feature

### Step 2: Include Compliance Reference in Code Comments

Every TTB service file should have a header comment:

```csharp
/// <summary>
/// Calculates TTB Form 5110.28 monthly reports.
///
/// COMPLIANCE REFERENCE: docs/TTB_FORM_5110_28_MAPPING.md
/// REGULATORY AUTHORITY: 27 CFR Part 19 Subpart V
///
/// This service implements the official TTB inventory balance equation:
/// Closing = Opening + Production + TransfersIn - TransfersOut - Losses
/// </summary>
public class TtbReportCalculatorService
{
    // Implementation
}
```

### Step 3: Implement with Validation

Always validate that your calculations match TTB requirements:

```csharp
// ✅ GOOD - Validates against closing snapshot
var calculatedClosing = /* your calculation */;
var actualClosing = await GetClosingSnapshotAsync();
var tolerance = 0.01m;

if (Math.Abs(calculatedClosing - actualClosing) > tolerance)
{
    logger.LogError(
        "TTB COMPLIANCE ERROR: Closing inventory mismatch. " +
        "Calculated={Calculated}, Actual={Actual}, Diff={Diff}. " +
        "See docs/TTB_FORM_5110_28_MAPPING.md Section 'Calculation Formulas'",
        calculatedClosing, actualClosing, Math.Abs(calculatedClosing - actualClosing)
    );
    throw new InvalidOperationException("Inventory balance validation failed");
}
```

### Step 4: Reference Specific Form Lines in Code

Make it easy to audit code against the official form:

```csharp
// ✅ GOOD - Clear reference to form structure
public class Form511028Data
{
    // Part I: Bulk Ingredients
    [JsonPropertyName("line_1")]
    public decimal Line1_OpeningInventory { get; set; }  // TTB Form 5110.28, Part I, Line 1

    [JsonPropertyName("line_2")]
    public decimal Line2_Production { get; set; }  // TTB Form 5110.28, Part I, Line 2

    [JsonPropertyName("line_21")]
    public decimal Line21_Losses { get; set; }  // TTB Form 5110.28, Part I, Line 21

    // etc...
}
```

### Step 5: Write Compliance Tests

Test against known TTB scenarios:

```csharp
[Fact]
public async Task CalculateMonthlyReport_MustBalance_WhenNoActivity()
{
    // Scenario: Zero activity month (TTB_FORM_5110_28_MAPPING.md, Edge Case #8)
    var opening = 1000m; // proof gallons
    var production = 0m;
    var transfers = 0m;
    var losses = 0m;

    var closing = await calculator.CalculateClosingAsync(opening, production, transfers, losses);

    // TTB requires: Opening = Closing when no activity
    Assert.Equal(opening, closing);
}

[Fact]
public void ProofGallonCalculation_MustMatchTtbFormula()
{
    // Scenario: Bourbon at 62.5% ABV (standard barrel entry proof)
    var wineGallons = 53m;
    var abv = 62.5m;

    var proofGallons = TtbVolumeCalculator.CalculateProofGallons(wineGallons, abv);

    // TTB Formula: PG = WG × (ABV × 2) / 100
    // Expected: 53 × 125 / 100 = 66.25
    Assert.Equal(66.25m, proofGallons);
}
```

---

## 🤖 For AI Agents: Required Context

When implementing TTB features, AI agents should be provided this context:

### Minimal Required Prompt Template

```
COMPLIANCE REQUIREMENT:
Before implementing this TTB feature, review docs/TTB_FORM_5110_28_MAPPING.md

Task: [Your task here]

Requirements:
1. Follow the exact calculation formulas in the mapping document
2. Use the correct database tables as specified in "Data Model Mapping" section
3. Handle the edge cases documented in the "Edge Cases" section
4. Add code comments referencing specific form line numbers
5. Include validation that calculations balance
6. Reference the regulatory authority (27 CFR Part 19)

Provide:
- Complete implementation following existing patterns in TtbReportCalculatorService.cs
- Unit tests covering edge cases from the mapping document
- Code comments with form line references (e.g., "// TTB Form 5110.28, Part I, Line 21")
```

### Example: Good AI Prompt

```
I need to implement angel's share loss tracking for TTB compliance.

COMPLIANCE CONTEXT:
- Review: docs/TTB_FORM_5110_28_MAPPING.md, Edge Case #2 (Angel's Share)
- Form: TTB Form 5110.28, Part I, Line 21 (Losses)
- Regulation: 27 CFR Part 19 - allowable losses during aging

Requirements:
1. Log losses as TtbTransactionType.Loss (multiplier: -1)
2. Calculate based on aging duration and industry standard rates (2-4% per year)
3. Include detailed reason in transaction notes
4. Apply at month-end during snapshot reconciliation

Implementation should follow patterns in:
- TtbTransactionLoggerService.cs (for logging transactions)
- TtbInventorySnapshotCalculator.cs (for month-end processing)

Please implement:
1. Service method to calculate angel's share for a barrel
2. Integration with month-end snapshot process
3. Unit tests for various aging periods
```

---

## ✅ Compliance Checklist for Pull Requests

Before merging ANY TTB-related code, verify:

### Code Review Checklist

- [ ] Reviewed `docs/TTB_FORM_5110_28_MAPPING.md` for affected form sections
- [ ] Code comments reference specific TTB form lines (e.g., "Part I, Line 21")
- [ ] Calculations use exact formulas from mapping document
- [ ] Transaction types use correct multipliers (+1 or -1)
- [ ] Proof gallon calculations use `TtbVolumeCalculator.CalculateProofGallons()`
- [ ] Inventory balance equation is maintained
- [ ] Edge cases from mapping document are handled
- [ ] Validation includes tolerance checks (0.01 proof gallons)
- [ ] Error messages reference compliance documentation
- [ ] Unit tests cover TTB-specific scenarios
- [ ] No magic numbers (use named constants)
- [ ] No "TODO" comments in compliance-critical code

### Testing Checklist

- [ ] Zero activity month: Opening = Closing
- [ ] Proof gallon formula: Matches TTB calculation
- [ ] Inventory balance: Closing = Opening + Additions - Removals
- [ ] Negative values: Rejected for inventory and production
- [ ] Tolerance check: Warns if mismatch > 0.01 proof gallons
- [ ] Edge cases: Tested against scenarios in mapping doc

### Documentation Checklist

- [ ] Service class has `/// COMPLIANCE REFERENCE:` comment
- [ ] Complex calculations include formula comments
- [ ] Any deviation from mapping doc is documented with TTB citation
- [ ] README or changelog mentions TTB compliance impact

---

## 🔒 What NOT to Do

### ❌ Bad Practice: Ignoring Validation Errors

```csharp
// ❌ NEVER DO THIS
try {
    ValidateBalance(opening, closing);
} catch {
    // Suppress error - we'll fix it later
}
```

**Why:** TTB audits can go back 3 years. "We'll fix it later" becomes "We have 3 years of invalid reports."

### ❌ Bad Practice: Approximating Proof Gallons

```csharp
// ❌ NEVER DO THIS
var proofGallons = Math.Round(wineGallons * abv);  // Wrong formula
```

**Why:** Even small rounding errors compound across thousands of barrels and can trigger audit flags.

### ❌ Bad Practice: Hard-Coding Form Values

```csharp
// ❌ NEVER DO THIS
var losses = 42.50m;  // "Looks about right"
```

**Why:** Every proof gallon must be traced to a transaction. Made-up numbers are fraud.

### ❌ Bad Practice: Skipping Edge Case Handling

```csharp
// ❌ NEVER DO THIS
// TODO: Handle transfers between bonded premises later
```

**Why:** Edge cases are regulatory requirements, not optional features.

---

## 📞 When in Doubt

If you're uncertain about TTB compliance:

1. **Check the mapping document:** `docs/TTB_FORM_5110_28_MAPPING.md`
2. **Review TTB regulations:** [27 CFR Part 19](https://www.ecfr.gov/current/title-27/chapter-I/subchapter-A/part-19)
3. **Consult TTB directly:** [TTB Contact Page](https://www.ttb.gov/contact)
4. **Ask a compliance expert:** Do not guess on federal regulations

### TTB Resources

- **Official Forms:** https://www.ttb.gov/forms
- **Form 5110.28 Page:** https://www.ttb.gov/ttb-form-511028
- **Regulations (27 CFR 19):** https://www.ecfr.gov/current/title-27/chapter-I/subchapter-A/part-19
- **TTB Contact:** https://www.ttb.gov/contact

---

## 📝 Document Revision Policy

This compliance guide and the mapping document are **living documents**:

- **Update when:** TTB changes forms or regulations
- **Review frequency:** Quarterly or when TTB publishes updates
- **Approval required:** Compliance officer or legal review for substantive changes
- **Version control:** All changes must be committed with detailed commit messages explaining regulatory basis

---

## 🎯 Quick Reference

| Task | Required Reading | Key Formula | Validation Rule |
|------|-----------------|-------------|-----------------|
| Calculate proof gallons | Mapping Doc: "Proof Gallons Calculation" | PG = WG × (ABV × 2) / 100 | Must use `TtbVolumeCalculator` |
| Monthly report | Mapping Doc: "Calculation Formulas" | Closing = Opening + Additions - Removals | Balance within 0.01 PG |
| Log production | Mapping Doc: "ttb_transactions" | Multiplier: +1 | Must link to batch ID |
| Log losses | Mapping Doc: Edge Case #2, #3 | Multiplier: -1 | Must include reason in notes |
| Transfers | Mapping Doc: Edge Case #4 | Both facilities log transaction | Must reference TTB Form 5100.11 |

---

**REMEMBER:** TTB compliance is not optional. When in doubt, over-document and over-validate.
Better to be audited and found meticulous than to be audited and found negligent.

---

**Document Version:** 1.0
**Regulatory Basis:** 27 CFR Part 19 Subpart V - Records and Reports
**Last TTB Form Update:** Check [TTB Forms Page](https://www.ttb.gov/forms) for latest versions
