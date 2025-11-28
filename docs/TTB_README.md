# TTB Compliance Documentation

**Comprehensive compliance framework for TTB (Alcohol and Tobacco Tax and Trade Bureau) monthly reporting**

**Regulatory Authority:** 27 CFR Part 19 - Distilled Spirits Plants
**Last Updated:** 2025-11-28

---

## 🚨 CRITICAL NOTICE

**TTB reporting is federal law.** Incorrect reports can result in:
- Federal penalties and fines
- License suspension or revocation
- Criminal liability for intentional misreporting
- Failed audits (TTB can audit 3 years back)

**ALWAYS consult these documents before working on TTB features.**

---

## 📚 Document Index

### Start Here

| Document | Purpose | When to Read |
|----------|---------|-------------|
| **[TTB_FORM_5110_28_MAPPING.md](TTB_FORM_5110_28_MAPPING.md)** | Form structure, calculations, data model mappings | **BEFORE** implementing any TTB feature |
| **[TTB_COMPLIANCE_GUIDE.md](TTB_COMPLIANCE_GUIDE.md)** | Development workflow, validation rules, code review | **DURING** development |
| **[TTB_TESTING_VALIDATION_GUIDE.md](TTB_TESTING_VALIDATION_GUIDE.md)** | Test scenarios, validation procedures | **BEFORE** submitting PR |

### Implementation & Operations

| Document | Purpose | When to Read |
|----------|---------|-------------|
| **[TTB_RELATED_FORMS.md](TTB_RELATED_FORMS.md)** | Forms 5110.11, 5110.40, 5100.11 overview | When implementing related forms |
| **[TTB_ERROR_RESOLUTION.md](TTB_ERROR_RESOLUTION.md)** | Troubleshooting guide, error procedures | When calculations don't balance |
| **[TTB_AUDIT_CHANGE_CONTROL.md](TTB_AUDIT_CHANGE_CONTROL.md)** | Audit preparation, change control, retention | Before TTB audits, before changing TTB code |

---

## 🎯 Quick Start by Role

### For Developers

**Implementing a new TTB feature? Read in this order:**

1. **[TTB_FORM_5110_28_MAPPING.md](TTB_FORM_5110_28_MAPPING.md)** → Understand the form and calculations
2. **[TTB_COMPLIANCE_GUIDE.md](TTB_COMPLIANCE_GUIDE.md)** → Follow development workflow (Section "Development Workflow")
3. **[TTB_TESTING_VALIDATION_GUIDE.md](TTB_TESTING_VALIDATION_GUIDE.md)** → Write tests (Section "Unit Test Scenarios")
4. **[TTB_COMPLIANCE_GUIDE.md](TTB_COMPLIANCE_GUIDE.md)** → Code review checklist (Section "Compliance Checklist for Pull Requests")

**Got an error? Read:**
- **[TTB_ERROR_RESOLUTION.md](TTB_ERROR_RESOLUTION.md)** → Find your error type and resolution steps

### For AI Agents

**Before generating code for ANY TTB task:**

1. Read **[TTB_COMPLIANCE_GUIDE.md](TTB_COMPLIANCE_GUIDE.md)** (Section "For AI Agents: Required Context")
2. Include compliance context in every prompt (see template in guide)
3. Reference **[TTB_FORM_5110_28_MAPPING.md](TTB_FORM_5110_28_MAPPING.md)** for calculations
4. Follow exact formulas—NO approximations or shortcuts

**Your prompts MUST include:**
```
🚨 COMPLIANCE REQUIREMENT:
REQUIRED READING: docs/TTB_FORM_5110_28_MAPPING.md, docs/TTB_COMPLIANCE_GUIDE.md
REGULATORY AUTHORITY: 27 CFR Part 19 Subpart V
[... rest of task]
```

### For Compliance Officers

**Monthly reporting workflow:**

1. **[TTB_FORM_5110_28_MAPPING.md](TTB_FORM_5110_28_MAPPING.md)** → Review form structure and calculations
2. **[TTB_TESTING_VALIDATION_GUIDE.md](TTB_TESTING_VALIDATION_GUIDE.md)** → Manual validation procedures (Section "Manual Validation Procedures")
3. **[TTB_ERROR_RESOLUTION.md](TTB_ERROR_RESOLUTION.md)** → If discrepancies found
4. **[TTB_AUDIT_CHANGE_CONTROL.md](TTB_AUDIT_CHANGE_CONTROL.md)** → Document retention and submission records

**Preparing for TTB audit:**
- **[TTB_AUDIT_CHANGE_CONTROL.md](TTB_AUDIT_CHANGE_CONTROL.md)** → Section "TTB Inspection Preparation"

### For Operations Managers

**Running monthly close:**

1. **[TTB_TESTING_VALIDATION_GUIDE.md](TTB_TESTING_VALIDATION_GUIDE.md)** → Procedure 1: Monthly Report Review
2. **[TTB_ERROR_RESOLUTION.md](TTB_ERROR_RESOLUTION.md)** → If balance doesn't match
3. **[TTB_AUDIT_CHANGE_CONTROL.md](TTB_AUDIT_CHANGE_CONTROL.md)** → Document retention (save all supporting docs)

**Quarterly reconciliation:**
- **[TTB_TESTING_VALIDATION_GUIDE.md](TTB_TESTING_VALIDATION_GUIDE.md)** → Procedure 2: Quarterly Reconciliation

---

## 📖 Document Details

### 1. TTB Form 5110.28 Mapping

**File:** [TTB_FORM_5110_28_MAPPING.md](TTB_FORM_5110_28_MAPPING.md) (927 lines)

**Contents:**
- **Form Structure:** All parts (I-VI), line items, data types
- **Data Model Mapping:** `ttb_inventory_snapshots`, `ttb_transactions`, `barrel`, `batch`, `order` tables
- **Calculation Formulas:**
  - Proof Gallons = Wine Gallons × (ABV × 2) / 100
  - Closing = Opening + Production + Transfers In - Transfers Out - Losses
- **Edge Cases:** Angel's share, spillage, transfers, tax determinations, zero activity months
- **Database Schema Recommendations:** New tables needed for full compliance
- **Testing Checklist:** Validation requirements

**Key Sections:**
- Section "Calculation Formulas" → Core TTB equations
- Section "Data Model Mapping" → Database queries
- Section "Edge Cases" → Special scenarios
- Section "Implementation Guide" → Step-by-step setup

---

### 2. TTB Compliance Guide for Developers

**File:** [TTB_COMPLIANCE_GUIDE.md](TTB_COMPLIANCE_GUIDE.md) (540+ lines)

**Contents:**
- **Critical Compliance Rules:** Non-negotiable formulas, constants, equations
- **Development Workflow:** 6-step process from review to validation
- **Code Examples:** Good vs. bad practices
- **Code Review Checklist:** Pre-PR requirements
- **AI Agent Prompt Templates:** Required compliance context
- **What NOT to Do:** Common mistakes and why they're dangerous

**Key Sections:**
- Section "Critical Compliance Rules" → Must-follow regulations
- Section "Development Workflow for TTB Features" → Step-by-step process
- Section "For AI Agents: Required Context" → Prompt templates
- Section "Compliance Checklist for Pull Requests" → Pre-merge verification

**Critical Constants:**
```csharp
public const decimal STANDARD_BARREL_WINE_GALLONS = 53m;  // DO NOT CHANGE
public const decimal SnapshotTolerance = 0.01m;            // TTB regulation
```

---

### 3. TTB Testing & Validation Guide

**File:** [TTB_TESTING_VALIDATION_GUIDE.md](TTB_TESTING_VALIDATION_GUIDE.md) (900+ lines)

**Contents:**
- **Pre-Implementation Validation:** Paper calculations before coding
- **Unit Test Scenarios:** Proof gallons, multipliers, balance equation
- **Integration Test Scenarios:** Full month lifecycle, transfers, multiple spirits
- **End-to-End Test Scenarios:** Complete quarter of operations
- **Regression Test Suite:** Tests that must NEVER break
- **Manual Validation Procedures:** Monthly report review, quarterly reconciliation
- **Test Data Sets:** Sample data for testing

**Key Sections:**
- Section "Unit Test Scenarios" → Copy/paste test code
- Section "Manual Validation Procedures" → Monthly review checklist
- Section "Regression Test Suite" → Critical tests
- Section "Test Data Sets" → Sample data for testing

**Example Test:**
```csharp
[Fact]
public void REGRESSION_ProofGallonFormula_MustNotChange()
{
    // This is the official TTB formula. DO NOT MODIFY.
    var result = TtbVolumeCalculator.CalculateProofGallons(100m, 50m);
    Assert.Equal(100m, result);  // 100 WG at 50% ABV = 100 PG
}
```

---

### 4. TTB Related Forms Reference

**File:** [TTB_RELATED_FORMS.md](TTB_RELATED_FORMS.md) (400+ lines)

**Contents:**
- **The Three Monthly Reports:** Forms 5110.40 (Production), 5110.11 (Storage), 5110.28 (Processing)
- **Transfer Documents:** Form 5100.11 (Transfer in Bond)
- **Tax Returns:** Form 5000.24 (Excise Tax)
- **Cross-Form Validation:** How forms relate and must match
- **Filing Calendar:** Due dates and frequencies
- **Implementation Priority:** Which forms to implement first

**Key Sections:**
- Section "The Three Monthly Operational Reports" → Overview and relationships
- Section "Cross-Form Validation Matrix" → What must match between forms
- Section "Form-to-Caskr Mapping" → Implementation status

---

### 5. TTB Error Resolution Procedures

**File:** [TTB_ERROR_RESOLUTION.md](TTB_ERROR_RESOLUTION.md) (600+ lines)

**Contents:**
- **Common Errors:** Balance mismatch, proof gallon errors, negative inventory, wrong classification
- **Diagnostic Procedures:** Full month audit, transaction trace
- **Resolution Steps:** Identify, fix, re-generate, re-calculate, document
- **When to Contact TTB:** Escalation criteria
- **Emergency Procedures:** Production failure, systematic errors
- **Prevention:** Best practices to avoid errors

**Key Sections:**
- Section "Common Errors" → Quick troubleshooting
- Section "Diagnostic Procedures" → SQL queries and investigation steps
- Section "Emergency Procedures" → Critical incident response

**Common Error Example:**
```
Error: "Inventory Balance Does Not Match"
Calculated: 662.50 PG vs Snapshot: 660.00 PG

Diagnosis:
1. Check for missing transactions (angel's share not logged)
2. Verify ABV in product metadata
3. Check for duplicate transactions

Resolution:
- If ≤ 0.01 PG: Acceptable (rounding tolerance)
- If > 0.01 PG: Investigate and log adjustment
```

---

### 6. TTB Audit & Change Control

**File:** [TTB_AUDIT_CHANGE_CONTROL.md](TTB_AUDIT_CHANGE_CONTROL.md) (600+ lines)

**Contents:**
- **Audit Requirements:** What TTB auditors will request (3 years back)
- **Change Control Policy:** What requires approval, process, forms
- **Document Retention:** 3-year retention requirements, file naming, backups
- **Audit Trail Logging:** Database triggers, application logging
- **TTB Inspection Preparation:** 30-day checklist, inspection protocol
- **Historical Data Integrity:** Immutability requirements, migration safety
- **Incident Response:** Severity levels, response protocols

**Key Sections:**
- Section "TTB Inspection Preparation" → 30-day prep checklist
- Section "Change Control Policy" → Approval process
- Section "Document Retention" → What to keep and where
- Section "Incident Response" → Critical error procedures

**Retention Requirements:**
| Document | Retention | Storage |
|----------|-----------|---------|
| Monthly Reports | 3 years | `/TTB_Compliance/Reports/{YYYY}/{MM}/` |
| Transaction Logs | 3 years | Database + backup |
| Change Control Forms | 3 years | `/TTB_Compliance/ChangeControl/{YYYY}/` |

---

## 🔧 Technical Implementation

### Current Caskr Implementation (Task TTB-001)

**Services:**
- `TtbReportCalculatorService.cs` - Calculates monthly reports
- `TtbInventorySnapshotCalculator.cs` - Daily inventory snapshots
- `TtbVolumeCalculator.cs` - Proof gallon calculations
- `TtbTransactionLoggerService.cs` - Logs compliance transactions
- `TtbProductMetadataCatalog.cs` - Spirit metadata (ABV, classification)

**Database Tables:**
- `ttb_monthly_reports` - Generated monthly reports
- `ttb_inventory_snapshots` - Daily inventory state
- `ttb_transactions` - All TTB compliance events
- `ttb_audit_log` - Change tracking

**Models:**
- `TtbMonthlyReport.cs` - Report entity
- `TtbMonthlyReportData.cs` - Report data structure
- `TtbInventorySnapshot.cs` - Snapshot entity
- `TtbTransaction.cs` - Transaction entity
- `TtbEnums.cs` - SpiritsType, TransactionType, TaxStatus

### Code References

All TTB service files include compliance headers:

```csharp
/// <summary>
/// Calculates TTB Form 5110.28 (Monthly Report of Processing Operations) data.
///
/// COMPLIANCE REFERENCE: docs/TTB_FORM_5110_28_MAPPING.md
/// REGULATORY AUTHORITY: 27 CFR Part 19 Subpart V - Records and Reports
///
/// This service implements the official TTB inventory balance equation:
///   Closing Inventory = Opening Inventory + Production + Transfers In - Transfers Out - Losses
/// </summary>
```

---

## 🚀 Getting Started

### For New Developers

1. **Read the mapping document:**
   ```bash
   cat docs/TTB_FORM_5110_28_MAPPING.md
   ```

2. **Understand the compliance rules:**
   ```bash
   cat docs/TTB_COMPLIANCE_GUIDE.md | less
   ```

3. **Review existing code:**
   ```bash
   cat Caskr.Server/Services/TtbReportCalculatorService.cs
   cat Caskr.Server/Services/TtbVolumeCalculator.cs
   ```

4. **Run the tests:**
   ```bash
   dotnet test --filter "Category=TTB"
   ```

### For AI Agents

**Include this in EVERY TTB-related prompt:**

```
🚨 COMPLIANCE REQUIREMENT:
This task involves TTB regulatory compliance.

REQUIRED READING:
- docs/TTB_FORM_5110_28_MAPPING.md (form structure and calculations)
- docs/TTB_COMPLIANCE_GUIDE.md (development standards)

REGULATORY AUTHORITY: 27 CFR Part 19 Subpart V

COMPLIANCE RULES:
1. Use EXACT formulas from mapping document (no approximations)
2. Proof Gallons = Wine Gallons × (ABV × 2) / 100
3. Inventory must balance: Closing = Opening + Additions - Removals
4. Validate calculations within 0.01 proof gallon tolerance
5. Include form line references in code comments

[YOUR TASK HERE]
```

---

## 📞 Support & Resources

### TTB Official Resources

- **Forms:** [https://www.ttb.gov/forms](https://www.ttb.gov/forms)
- **Form 5110.28 Page:** [https://www.ttb.gov/ttb-form-511028](https://www.ttb.gov/ttb-form-511028)
- **Regulations (27 CFR 19):** [https://www.ecfr.gov/current/title-27/chapter-I/subchapter-A/part-19](https://www.ecfr.gov/current/title-27/chapter-I/subchapter-A/part-19)
- **Industry Circulars:** [https://www.ttb.gov/regulations-and-rulings/industry-circulars](https://www.ttb.gov/regulations-and-rulings/industry-circulars)

### Contact TTB

- **Email:** [alfd.nrc@ttb.gov](mailto:alfd.nrc@ttb.gov)
- **Phone:** 877-882-3277 (National Revenue Center)
- **Hours:** Monday-Friday, 8:00 AM - 5:00 PM ET

### Internal Support

**Questions about implementation?**
- Review `TTB_FORM_5110_28_MAPPING.md` first
- Check `TTB_ERROR_RESOLUTION.md` for troubleshooting
- Consult compliance officer for regulatory interpretation

**Found a bug?**
- Follow procedures in `TTB_ERROR_RESOLUTION.md`
- Document in `CHANGELOG_TTB.md` (to be created)
- Create incident report per `TTB_AUDIT_CHANGE_CONTROL.md`

---

## ✅ Compliance Checklist

### Before Every Commit

- [ ] Read relevant sections of `TTB_FORM_5110_28_MAPPING.md`
- [ ] Followed `TTB_COMPLIANCE_GUIDE.md` workflow
- [ ] Used exact formulas (no approximations)
- [ ] Added code comments with form line references
- [ ] Unit tests pass
- [ ] Regression tests pass

### Before Every Pull Request

- [ ] Completed code review checklist from `TTB_COMPLIANCE_GUIDE.md`
- [ ] All tests pass (unit + integration + regression)
- [ ] Manual validation performed
- [ ] Edge cases handled
- [ ] Documentation updated
- [ ] No magic numbers in code

### Before Production Deployment

- [ ] Full test suite passes
- [ ] Manual monthly report validation completed
- [ ] Quarterly reconciliation done (if applicable)
- [ ] Rollback plan documented
- [ ] Compliance officer approval obtained

---

## 🔄 Document Maintenance

### Review Schedule

| Document | Review Frequency | Trigger |
|----------|------------------|---------|
| All TTB docs | Quarterly | End of quarter |
| `TTB_FORM_5110_28_MAPPING.md` | When TTB form updates | TTB publishes new form version |
| `TTB_COMPLIANCE_GUIDE.md` | Annually | January 1 |
| `TTB_RELATED_FORMS.md` | When implementing new forms | New form development |
| `TTB_ERROR_RESOLUTION.md` | As needed | New error types discovered |
| `TTB_AUDIT_CHANGE_CONTROL.md` | Annually | January 1 |

### Update Process

1. Identify change needed (regulation update, TTB form revision, bug discovery)
2. Create change control request (per `TTB_AUDIT_CHANGE_CONTROL.md`)
3. Obtain compliance officer approval
4. Update documentation
5. Update code if needed
6. Test changes
7. Document in `CHANGELOG_TTB.md`
8. Commit with detailed message citing regulatory basis

---

## 📝 Version History

| Version | Date | Changes | Author |
|---------|------|---------|--------|
| 1.0 | 2025-11-28 | Initial compliance framework created | Claude AI (Task TTB-001) |

---

## 📋 Document Index (Alphabetical)

1. **[TTB_AUDIT_CHANGE_CONTROL.md](TTB_AUDIT_CHANGE_CONTROL.md)** - Audit preparation, change control, retention
2. **[TTB_COMPLIANCE_GUIDE.md](TTB_COMPLIANCE_GUIDE.md)** - Development standards, workflow, checklists
3. **[TTB_ERROR_RESOLUTION.md](TTB_ERROR_RESOLUTION.md)** - Troubleshooting, diagnostics, resolutions
4. **[TTB_FORM_5110_28_MAPPING.md](TTB_FORM_5110_28_MAPPING.md)** - Form structure, calculations, mappings
5. **[TTB_README.md](TTB_README.md)** - This document (navigation and overview)
6. **[TTB_RELATED_FORMS.md](TTB_RELATED_FORMS.md)** - Forms 5110.11, 5110.40, 5100.11 reference
7. **[TTB_TESTING_VALIDATION_GUIDE.md](TTB_TESTING_VALIDATION_GUIDE.md)** - Test scenarios, validation procedures

---

**Remember:** TTB compliance is not optional. When in doubt, consult the documentation, ask compliance officer, or contact TTB directly. Never guess on federal regulations.

**Last Updated:** 2025-11-28
**Next Review:** 2026-01-01 (quarterly)
