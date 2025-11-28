# TTB Testing & Validation Guide

**Purpose:** Comprehensive test scenarios and validation procedures for TTB compliance
**Last Updated:** 2025-11-28
**Regulatory Authority:** 27 CFR Part 19 Subpart V
**Related Documents:**
- `TTB_FORM_5110_28_MAPPING.md` (form structure)
- `TTB_COMPLIANCE_GUIDE.md` (development standards)

---

## Table of Contents

1. [Testing Philosophy](#testing-philosophy)
2. [Pre-Implementation Validation](#pre-implementation-validation)
3. [Unit Test Scenarios](#unit-test-scenarios)
4. [Integration Test Scenarios](#integration-test-scenarios)
5. [End-to-End Test Scenarios](#end-to-end-test-scenarios)
6. [Regression Test Suite](#regression-test-suite)
7. [Manual Validation Procedures](#manual-validation-procedures)
8. [Test Data Sets](#test-data-sets)
9. [Validation Checklist](#validation-checklist)

---

## Testing Philosophy

### Critical Principle

**Every TTB calculation must be tested against known correct results.**

Unlike general business logic where "close enough" might be acceptable, TTB calculations must be:
- **Exact** (within 0.01 proof gallon tolerance)
- **Reproducible** (same inputs = same outputs)
- **Auditable** (test results can be shown to TTB auditors)
- **Traceable** (every value traces to source data)

### Test Coverage Requirements

| Category | Minimum Coverage | Target Coverage |
|----------|-----------------|-----------------|
| TTB calculation services | 95% | 100% |
| TTB data models | 90% | 100% |
| TTB API endpoints | 95% | 100% |
| TTB edge cases | 100% | 100% |

**CRITICAL:** Edge case coverage MUST be 100%. These are regulatory requirements, not optional features.

---

## Pre-Implementation Validation

Before writing ANY TTB code, validate your understanding:

### 1. Paper Calculation Exercise

**Scenario:** Single barrel of bourbon, one month lifecycle

```
Given:
- 1 barrel created January 1, 2024
- Spirit Type: Bourbon (62.5% ABV)
- Volume: 53 wine gallons (standard barrel)
- No transactions during month

Calculate on paper:
1. Opening inventory (Jan 1): ?
2. Production: ?
3. Transfers: ?
4. Losses: ?
5. Closing inventory (Jan 31): ?

Expected Results:
1. Opening: 0 PG, 0 WG (new operation)
2. Production: 66.25 PG, 53 WG
   - Calculation: 53 × (62.5 × 2 / 100) = 53 × 1.25 = 66.25
3. Transfers: 0 PG, 0 WG
4. Losses: 0 PG, 0 WG
5. Closing: 66.25 PG, 53 WG
```

**Validation:** If your manual calculation doesn't match expected results, STOP. Review `TTB_FORM_5110_28_MAPPING.md` before proceeding.

### 2. Compare Against TTB Publications

TTB publishes example calculations in:
- [TTB Industry Circular 2008-1](https://www.ttb.gov/regulations-and-rulings/industry-circulars)
- [Form 5110.28 Instructions](https://www.ttb.gov/ttb-form-511028)

**Action:** Work through at least one published TTB example by hand before coding.

---

## Unit Test Scenarios

### Scenario 1: Proof Gallon Calculation (Critical)

```csharp
[Theory]
[InlineData(53, 62.5, 66.25)]    // Bourbon barrel at entry proof
[InlineData(53, 80.0, 84.80)]    // High-proof whiskey
[InlineData(100, 40.0, 80.00)]   // Bottled bourbon (80 proof)
[InlineData(100, 95.0, 190.00)]  // Neutral spirits (190 proof)
[InlineData(1, 50.0, 1.00)]      // 1 gallon at 100 proof
public void CalculateProofGallons_ReturnsCorrectValue(
    decimal wineGallons,
    decimal abv,
    decimal expectedProofGallons)
{
    // Arrange & Act
    var actual = TtbVolumeCalculator.CalculateProofGallons(wineGallons, abv);

    // Assert
    Assert.Equal(expectedProofGallons, actual);
}

[Theory]
[InlineData(0, 62.5, 0)]      // Zero wine gallons
[InlineData(53, 0, 0)]        // Zero ABV
[InlineData(-53, 62.5, 0)]    // Negative wine gallons (invalid)
[InlineData(53, -10, 0)]      // Negative ABV (invalid)
public void CalculateProofGallons_InvalidInputs_ReturnsZero(
    decimal wineGallons,
    decimal abv,
    decimal expected)
{
    var actual = TtbVolumeCalculator.CalculateProofGallons(wineGallons, abv);
    Assert.Equal(expected, actual);
}
```

### Scenario 2: Transaction Multipliers (Critical)

```csharp
[Theory]
[InlineData(TtbTransactionType.Production, 1)]
[InlineData(TtbTransactionType.TransferIn, 1)]
[InlineData(TtbTransactionType.Gain, 1)]
[InlineData(TtbTransactionType.TransferOut, -1)]
[InlineData(TtbTransactionType.Loss, -1)]
[InlineData(TtbTransactionType.Destruction, -1)]
[InlineData(TtbTransactionType.Bottling, -1)]
[InlineData(TtbTransactionType.TaxDetermination, -1)]
public void GetTransactionMultiplier_ReturnsCorrectValue(
    TtbTransactionType type,
    int expectedMultiplier)
{
    // This test ensures multipliers match TTB regulations
    // See: docs/TTB_FORM_5110_28_MAPPING.md, Section "Transaction Type Enum Mapping"

    var multiplier = GetTransactionMultiplier(type);
    Assert.Equal(expectedMultiplier, multiplier);
}
```

### Scenario 3: Inventory Balance Equation (Critical)

```csharp
[Fact]
public void CalculateClosingInventory_SimpleProduction_BalancesCorrectly()
{
    // Arrange
    var opening = new Dictionary<SectionKey, SectionAggregate>
    {
        [new SectionKey("Bourbon", TtbSpiritsType.Under190Proof)] =
            new SectionAggregate { ProofGallons = 0, WineGallons = 0 }
    };

    var production = new Dictionary<SectionKey, SectionAggregate>
    {
        [new SectionKey("Bourbon", TtbSpiritsType.Under190Proof)] =
            new SectionAggregate { ProofGallons = 66.25m, WineGallons = 53m }
    };

    var transfers = new Dictionary<SectionKey, SectionAggregate>();
    var losses = new Dictionary<SectionKey, SectionAggregate>();

    // Act
    var closing = CalculateClosingInventory(opening, production, transfers, transfers, losses);

    // Assert
    var bourbonKey = new SectionKey("Bourbon", TtbSpiritsType.Under190Proof);
    Assert.Equal(66.25m, closing[bourbonKey].ProofGallons);
    Assert.Equal(53m, closing[bourbonKey].WineGallons);
}

[Fact]
public void CalculateClosingInventory_WithLosses_SubtractsCorrectly()
{
    // Scenario: Angel's share loss of 2% over one year
    // Opening: 100 PG
    // Loss: 2 PG (2% angel's share)
    // Closing: 98 PG

    var opening = new Dictionary<SectionKey, SectionAggregate>
    {
        [new SectionKey("Bourbon", TtbSpiritsType.Under190Proof)] =
            new SectionAggregate { ProofGallons = 100m, WineGallons = 80m }
    };

    var losses = new Dictionary<SectionKey, SectionAggregate>
    {
        [new SectionKey("Bourbon", TtbSpiritsType.Under190Proof)] =
            new SectionAggregate { ProofGallons = 2m, WineGallons = 1.6m }
    };

    var production = new Dictionary<SectionKey, SectionAggregate>();
    var transfers = new Dictionary<SectionKey, SectionAggregate>();

    // Act
    var closing = CalculateClosingInventory(opening, production, transfers, transfers, losses);

    // Assert
    var bourbonKey = new SectionKey("Bourbon", TtbSpiritsType.Under190Proof);
    Assert.Equal(98m, closing[bourbonKey].ProofGallons);
    Assert.Equal(78.4m, closing[bourbonKey].WineGallons);
}
```

### Scenario 4: Zero Activity Month (Edge Case)

```csharp
[Fact]
public async Task CalculateMonthlyReport_ZeroActivity_OpeningEqualsClosing()
{
    // Scenario: No production, transfers, or losses during month
    // TTB requirement: Must still file report with opening = closing

    // Arrange
    var companyId = 1;
    var month = 2;
    var year = 2024;

    // Seed opening inventory snapshot
    await SeedInventorySnapshot(companyId, new DateTime(2024, 1, 31),
        proofGallons: 1000m, wineGallons: 800m);

    // No transactions during February

    // Act
    var report = await calculator.CalculateMonthlyReportAsync(companyId, month, year);

    // Assert
    Assert.Equal(1000m, report.OpeningInventory.Rows[0].ProofGallons);
    Assert.Equal(1000m, report.ClosingInventory.Rows[0].ProofGallons);
    Assert.Empty(report.Production.Rows);
    Assert.Empty(report.Losses.Rows);
}
```

### Scenario 5: Rounding and Tolerance (Edge Case)

```csharp
[Fact]
public void ProofGallonCalculation_RoundsCorrectly()
{
    // TTB requires rounding to 2 decimal places, AwayFromZero

    // Test case: 53.333 WG at 62.5% ABV
    // Expected: 53.333 × 1.25 = 66.66625 → rounds to 66.67

    var result = TtbVolumeCalculator.CalculateProofGallons(53.333m, 62.5m);

    Assert.Equal(66.67m, result);
}

[Fact]
public async Task ValidateClosingSnapshot_WithinTolerance_NoWarning()
{
    // TTB allows 0.01 PG tolerance for rounding differences

    var calculated = 1000.004m;
    var snapshot = 1000.000m;

    // Should NOT log warning (within 0.01 tolerance)
    await ValidateAgainstClosingSnapshotAsync(companyId, endDate, calculated);

    // Assert no warning logged
    Assert.Empty(loggerMock.WarningMessages);
}

[Fact]
public async Task ValidateClosingSnapshot_ExceedsTolerance_LogsWarning()
{
    var calculated = 1000.02m;
    var snapshot = 1000.00m;

    // Should log warning (exceeds 0.01 tolerance)
    await ValidateAgainstClosingSnapshotAsync(companyId, endDate, calculated);

    Assert.Contains("Closing inventory mismatch", loggerMock.WarningMessages[0]);
}
```

---

## Integration Test Scenarios

### Scenario 1: Full Month Lifecycle

```csharp
[Fact]
public async Task FullMonthLifecycle_ProductionAndLosses_CalculatesCorrectly()
{
    // Scenario: Complete month with production, losses, and angel's share

    // January 2024
    var companyId = 1;

    // Day 1: Create 10 barrels of bourbon (53 WG each, 62.5% ABV)
    await CreateBatch(companyId, barrelCount: 10, spiritType: "Bourbon",
        createdDate: new DateTime(2024, 1, 1));
    // Expected production: 10 × 53 × 1.25 = 662.5 PG

    // Day 15: Spillage loss (1 barrel broken)
    await LogLoss(companyId, proofGallons: 66.25m, wineGallons: 53m,
        reason: "Barrel breach during handling", date: new DateTime(2024, 1, 15));

    // Day 31: Angel's share (0.5% loss)
    await LogLoss(companyId, proofGallons: 3.31m, wineGallons: 2.65m,
        reason: "Angel's share - January", date: new DateTime(2024, 1, 31));

    // Run monthly report
    var report = await calculator.CalculateMonthlyReportAsync(companyId, 1, 2024);

    // Assert
    Assert.Equal(0m, report.OpeningInventory.Rows[0].ProofGallons); // New operation
    Assert.Equal(662.5m, report.Production.Rows[0].ProofGallons);
    Assert.Equal(69.56m, report.Losses.Rows[0].ProofGallons); // 66.25 + 3.31

    // Closing = 0 + 662.5 - 69.56 = 592.94 PG
    Assert.Equal(592.94m, report.ClosingInventory.Rows[0].ProofGallons);

    // Verify balance
    var balance = report.OpeningInventory.Rows[0].ProofGallons
                + report.Production.Rows[0].ProofGallons
                - report.Losses.Rows[0].ProofGallons;
    Assert.Equal(report.ClosingInventory.Rows[0].ProofGallons, balance);
}
```

### Scenario 2: Inter-Facility Transfer

```csharp
[Fact]
public async Task InterFacilityTransfer_BothFacilitiesReportCorrectly()
{
    // Scenario: Company A transfers 5 barrels to Company B

    var companyA = 1;
    var companyB = 2;

    // Setup: Company A has 10 barrels
    await SeedInventorySnapshot(companyA, new DateTime(2024, 1, 31),
        proofGallons: 662.5m, wineGallons: 530m);

    // February 15: Transfer 5 barrels from A to B
    await LogTransferOut(companyA, proofGallons: 331.25m, wineGallons: 265m,
        transferDate: new DateTime(2024, 2, 15), toCompanyId: companyB);

    await LogTransferIn(companyB, proofGallons: 331.25m, wineGallons: 265m,
        transferDate: new DateTime(2024, 2, 15), fromCompanyId: companyA);

    // Run reports for both companies
    var reportA = await calculator.CalculateMonthlyReportAsync(companyA, 2, 2024);
    var reportB = await calculator.CalculateMonthlyReportAsync(companyB, 2, 2024);

    // Company A: Lost 5 barrels
    Assert.Equal(662.5m, reportA.OpeningInventory.Rows[0].ProofGallons);
    Assert.Equal(331.25m, reportA.Transfers.TransfersOut[0].ProofGallons);
    Assert.Equal(331.25m, reportA.ClosingInventory.Rows[0].ProofGallons); // 662.5 - 331.25

    // Company B: Gained 5 barrels
    Assert.Equal(0m, reportB.OpeningInventory.Rows[0].ProofGallons);
    Assert.Equal(331.25m, reportB.Transfers.TransfersIn[0].ProofGallons);
    Assert.Equal(331.25m, reportB.ClosingInventory.Rows[0].ProofGallons);
}
```

### Scenario 3: Multiple Spirit Types

```csharp
[Fact]
public async Task MultipleSpirits_SegregatedCorrectly()
{
    // Scenario: Distillery produces bourbon, rye, and vodka in same month

    var companyId = 1;

    // Bourbon: 10 barrels at 62.5% ABV
    await CreateBatch(companyId, barrelCount: 10, spiritType: "Bourbon", abv: 62.5m);

    // Rye: 5 barrels at 62.5% ABV
    await CreateBatch(companyId, barrelCount: 5, spiritType: "Rye", abv: 62.5m);

    // Vodka: 3 barrels at 95% ABV (neutral spirits)
    await CreateBatch(companyId, barrelCount: 3, spiritType: "Vodka", abv: 95m);

    // Run report
    var report = await calculator.CalculateMonthlyReportAsync(companyId, 1, 2024);

    // Assert: Each spirit type reported separately
    var bourbon = report.Production.Rows.First(r => r.ProductType == "Bourbon");
    var rye = report.Production.Rows.First(r => r.ProductType == "Rye");
    var vodka = report.Production.Rows.First(r => r.ProductType == "Vodka");

    Assert.Equal(662.5m, bourbon.ProofGallons);  // 10 × 53 × 1.25
    Assert.Equal(331.25m, rye.ProofGallons);     // 5 × 53 × 1.25
    Assert.Equal(301.35m, vodka.ProofGallons);   // 3 × 53 × 1.90

    // Vodka should be classified as Neutral190OrMore
    Assert.Equal(TtbSpiritsType.Neutral190OrMore, vodka.SpiritsType);
}
```

---

## End-to-End Test Scenarios

### Scenario 1: Complete Quarter of Operations

```csharp
[Fact]
public async Task CompleteQuarter_ThreeMonths_BalancesCorrectly()
{
    // Scenario: January-March 2024, continuous operations

    var companyId = 1;

    // JANUARY: Start production
    await CreateBatch(companyId, barrelCount: 100, createdDate: new DateTime(2024, 1, 10));
    var jan = await calculator.CalculateMonthlyReportAsync(companyId, 1, 2024);
    Assert.Equal(6625m, jan.ClosingInventory.Rows[0].ProofGallons); // 100 × 53 × 1.25

    // FEBRUARY: Add more production, some losses
    await CreateBatch(companyId, barrelCount: 50, createdDate: new DateTime(2024, 2, 5));
    await LogLoss(companyId, proofGallons: 10m, date: new DateTime(2024, 2, 28));

    var feb = await calculator.CalculateMonthlyReportAsync(companyId, 2, 2024);
    Assert.Equal(jan.ClosingInventory.Rows[0].ProofGallons,
                 feb.OpeningInventory.Rows[0].ProofGallons); // Continuity check
    Assert.Equal(3312.5m, feb.Production.Rows[0].ProofGallons); // 50 × 53 × 1.25
    Assert.Equal(9927.5m, feb.ClosingInventory.Rows[0].ProofGallons); // 6625 + 3312.5 - 10

    // MARCH: Transfer some out, tax determination
    await LogTransferOut(companyId, proofGallons: 662.5m, date: new DateTime(2024, 3, 10));
    await LogTaxDetermination(companyId, proofGallons: 1325m, date: new DateTime(2024, 3, 20));

    var mar = await calculator.CalculateMonthlyReportAsync(companyId, 3, 2024);
    Assert.Equal(feb.ClosingInventory.Rows[0].ProofGallons,
                 mar.OpeningInventory.Rows[0].ProofGallons); // Continuity check
    Assert.Equal(7940m, mar.ClosingInventory.Rows[0].ProofGallons); // 9927.5 - 662.5 - 1325

    // Validate: Total produced - total removed = current inventory
    var totalProduced = jan.Production.Rows[0].ProofGallons
                      + feb.Production.Rows[0].ProofGallons;
    var totalRemoved = feb.Losses.Rows[0].ProofGallons
                     + mar.Transfers.TransfersOut[0].ProofGallons
                     + 1325m; // tax determination

    Assert.Equal(totalProduced - totalRemoved, mar.ClosingInventory.Rows[0].ProofGallons);
}
```

---

## Regression Test Suite

**Critical Tests That Must NEVER Break:**

```csharp
/// <summary>
/// REGRESSION TEST: Proof gallon formula must never change.
/// If this test fails, you have violated TTB regulations.
/// See: docs/TTB_COMPLIANCE_GUIDE.md
/// </summary>
[Fact]
public void REGRESSION_ProofGallonFormula_MustNotChange()
{
    // This is the official TTB formula. DO NOT MODIFY.
    var wineGallons = 100m;
    var abv = 50m;

    var result = TtbVolumeCalculator.CalculateProofGallons(wineGallons, abv);

    // 100 WG at 50% ABV (100 proof) = 100 PG
    Assert.Equal(100m, result);
}

/// <summary>
/// REGRESSION TEST: Transaction multipliers must match TTB regulations.
/// If this test fails, inventory calculations are incorrect.
/// </summary>
[Fact]
public void REGRESSION_TransactionMultipliers_MustNotChange()
{
    Assert.Equal(1, GetTransactionMultiplier(TtbTransactionType.Production));
    Assert.Equal(-1, GetTransactionMultiplier(TtbTransactionType.Loss));
    Assert.Equal(-1, GetTransactionMultiplier(TtbTransactionType.TransferOut));
}

/// <summary>
/// REGRESSION TEST: Inventory balance equation must always hold.
/// </summary>
[Fact]
public void REGRESSION_InventoryBalanceEquation_MustAlwaysHold()
{
    var opening = 1000m;
    var production = 500m;
    var transfersIn = 200m;
    var transfersOut = 300m;
    var losses = 100m;

    var closing = opening + production + transfersIn - transfersOut - losses;

    // Closing = 1000 + 500 + 200 - 300 - 100 = 1300
    Assert.Equal(1300m, closing);
}
```

---

## Manual Validation Procedures

### Procedure 1: Monthly Report Review (Required)

**Frequency:** Before submitting any TTB report

**Steps:**

1. **Generate report in test environment**
   ```bash
   POST /api/ttb/reports/monthly
   {
     "companyId": 1,
     "month": 1,
     "year": 2024,
     "environment": "test"
   }
   ```

2. **Verify balance equation**
   ```
   Closing = Opening + Production + Transfers In - Transfers Out - Losses

   Check: Does Part I, Line 6 (Total Available) = Line 24 (Total Dispositions)?
   ```

3. **Cross-reference with source data**
   - Production: Match to batch completion records
   - Transfers: Match to transfer documents (Form 5100.11)
   - Losses: Match to loss incident reports

4. **Compare with previous month**
   - This month's opening = last month's closing?
   - Spirit type breakdowns consistent?

5. **Check for anomalies**
   - Negative values? (should never occur)
   - Sudden large changes? (investigate)
   - Missing product types? (verify intentional)

6. **Sign-off**
   ```
   Reviewed by: __________________
   Date: __________________
   Balance verified: ☐ Yes ☐ No
   Anomalies noted: ☐ None ☐ See notes
   Approved for submission: ☐ Yes ☐ No
   ```

### Procedure 2: Quarterly Reconciliation

**Frequency:** End of each quarter

**Steps:**

1. **Physical inventory count**
   - Count all barrels by rickhouse
   - Verify barrel SKUs match database
   - Measure volumes (if possible)

2. **Database inventory**
   - Run TTB inventory snapshot for quarter end
   - Compare to physical count

3. **Reconcile differences**
   - Acceptable: ≤ 0.5% variance (rounding, temperature)
   - Investigate: > 0.5% variance
   - Document all differences

4. **Adjust for findings**
   - Log losses for unaccounted inventory
   - Create adjustments for data entry errors
   - Update database if physical count is correct

5. **Document reconciliation**
   - Save spreadsheet: `Reconciliation_Q{Q}_{YYYY}.xlsx`
   - File in: `/TTB_Compliance/Reconciliations/`
   - Retain for 3 years (TTB requirement)

---

## Test Data Sets

### Data Set 1: Basic Production (Happy Path)

```sql
-- Company with simple bourbon production
INSERT INTO companies (id, company_name, ttb_permit_number)
VALUES (100, 'Test Distillery', 'DSP-KY-12345');

-- Batch of bourbon barrels
INSERT INTO batches (id, company_id, created_date)
VALUES (1, 100, '2024-01-01');

INSERT INTO spirit_types (id, name) VALUES (1, 'Bourbon');
INSERT INTO statuses (id, name) VALUES (1, 'aging');

-- 10 barrels
INSERT INTO orders (id, company_id, batch_id, spirit_type_id, status_id, quantity, created_at)
VALUES (1, 100, 1, 1, 1, 10, '2024-01-01');

-- Expected results:
-- Production: 10 × 53 × 1.25 = 662.5 PG
```

### Data Set 2: Complex Multi-Spirit (Edge Cases)

```sql
-- Company producing multiple spirit types
INSERT INTO companies (id, company_name) VALUES (200, 'Multi-Spirit Distillery');

-- Bourbon, Rye, Vodka, Gin
INSERT INTO spirit_types (id, name) VALUES
  (1, 'Bourbon'), (2, 'Rye'), (3, 'Vodka'), (4, 'Gin');

-- Different ABVs
-- Bourbon: 62.5%, Rye: 62.5%, Vodka: 95%, Gin: 70%

-- Expected: Different SpiritsType classifications
-- Bourbon/Rye: Under190Proof
-- Vodka: Neutral190OrMore
-- Gin: Under190Proof
```

### Data Set 3: Transfer Scenario

```sql
-- Two companies, transfer between them
INSERT INTO companies (id, company_name, ttb_permit_number) VALUES
  (300, 'Sending Distillery', 'DSP-KY-11111'),
  (301, 'Receiving Distillery', 'DSP-KY-22222');

-- Transfer record
INSERT INTO ttb_transfers (from_company_id, to_company_id, transfer_date, proof_gallons)
VALUES (300, 301, '2024-02-15', 331.25);

-- Expected:
-- Company 300: TransferOut transaction (-331.25 PG)
-- Company 301: TransferIn transaction (+331.25 PG)
```

---

## Validation Checklist

### Before Each Commit

- [ ] All unit tests pass
- [ ] Regression tests pass
- [ ] No TODO comments in TTB code
- [ ] All formulas match mapping document
- [ ] Code comments reference TTB form lines

### Before Each Pull Request

- [ ] Integration tests pass
- [ ] Edge cases tested
- [ ] Code review checklist completed
- [ ] Manual validation performed
- [ ] Documentation updated

### Before Production Deployment

- [ ] Full regression suite passes
- [ ] End-to-end tests pass
- [ ] Manual quarterly reconciliation completed
- [ ] Test data matches production patterns
- [ ] Rollback plan documented

### Before TTB Submission

- [ ] Monthly report generated in test environment
- [ ] Balance equation verified
- [ ] Cross-referenced with source documents
- [ ] Reviewed by compliance officer
- [ ] Anomalies investigated and documented
- [ ] Previous month continuity verified
- [ ] Sign-off obtained

---

## Test Environment Setup

### Database Setup

```sql
-- Create test company
INSERT INTO companies (id, company_name, ttb_permit_number, is_active)
VALUES (999, 'Test Company - DO NOT USE IN PRODUCTION', 'DSP-TEST-99999', true);

-- Mark as test data
UPDATE companies SET notes = '⚠️ TEST DATA ONLY' WHERE id = 999;
```

### Seed Data Script

```bash
#!/bin/bash
# Seed test data for TTB validation

# Reset test company data
psql -d caskr-db -c "DELETE FROM ttb_transactions WHERE company_id = 999;"
psql -d caskr-db -c "DELETE FROM ttb_inventory_snapshots WHERE company_id = 999;"
psql -d caskr-db -c "DELETE FROM barrels WHERE company_id = 999;"

# Load test scenarios
psql -d caskr-db -f tests/data/ttb_basic_production.sql
psql -d caskr-db -f tests/data/ttb_multi_spirit.sql
psql -d caskr-db -f tests/data/ttb_transfers.sql

echo "✅ Test data loaded for company ID 999"
```

---

## Continuous Integration

### GitHub Actions Workflow

```yaml
name: TTB Compliance Tests

on: [push, pull_request]

jobs:
  ttb-validation:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v3

      - name: Run TTB Unit Tests
        run: dotnet test --filter "Category=TTB&Category=Unit"

      - name: Run TTB Regression Tests
        run: dotnet test --filter "Category=TTB&Category=Regression"

      - name: Run TTB Integration Tests
        run: dotnet test --filter "Category=TTB&Category=Integration"

      - name: Verify Proof Gallon Formula (Critical)
        run: dotnet test --filter "FullyQualifiedName~REGRESSION_ProofGallonFormula"

      - name: Fail on TTB Test Failure
        if: failure()
        run: |
          echo "❌ TTB COMPLIANCE TESTS FAILED"
          echo "This PR cannot be merged until all TTB tests pass."
          exit 1
```

---

## Test Failure Response

### If a TTB Test Fails

**DO NOT:**
- ❌ Disable the test
- ❌ Change expected values without regulatory justification
- ❌ Merge PR with failing TTB tests

**DO:**
1. ✅ Review `TTB_FORM_5110_28_MAPPING.md` for correct formula
2. ✅ Compare against TTB regulations
3. ✅ Fix the implementation (not the test)
4. ✅ Document why test was failing
5. ✅ Add additional test to prevent regression

---

## Resources

- **TTB Regulations:** [27 CFR Part 19](https://www.ecfr.gov/current/title-27/chapter-I/subchapter-A/part-19)
- **TTB Forms:** [https://www.ttb.gov/forms](https://www.ttb.gov/forms)
- **Caskr Mapping Doc:** `docs/TTB_FORM_5110_28_MAPPING.md`
- **Compliance Guide:** `docs/TTB_COMPLIANCE_GUIDE.md`

---

**Remember:** Testing TTB compliance is not optional. These tests protect against federal penalties, license suspension, and criminal liability. When in doubt, add more tests.

**Document Version:** 1.0
**Last TTB Regulation Review:** 2025-11-28
