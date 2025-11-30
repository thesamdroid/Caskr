# TTB Compliance User Guide

> **Version:** 1.0
> **Last Updated:** November 2025
> **Audience:** Distillery Operators, Compliance Officers, Administrators

---

## Table of Contents

1. [Introduction](#1-introduction)
   - [What is TTB Compliance?](#11-what-is-ttb-compliance)
   - [Understanding TTB Forms](#12-understanding-ttb-forms)
   - [Key Terminology](#13-key-terminology)
2. [Setup and Configuration](#2-setup-and-configuration)
   - [Enabling TTB Compliance](#21-enabling-ttb-compliance)
   - [Configuring Your TTB Permit Number](#22-configuring-your-ttb-permit-number)
   - [Setting Up Automatic Report Generation](#23-setting-up-automatic-report-generation)
   - [Assigning Compliance Roles](#24-assigning-compliance-roles)
3. [Daily Operations](#3-daily-operations)
   - [Automatic Transaction Logging](#31-automatic-transaction-logging)
   - [Manual Transaction Entry](#32-manual-transaction-entry)
   - [Gauge Record Entry for Barrels](#33-gauge-record-entry-for-barrels)
4. [Monthly Reporting](#4-monthly-reporting)
   - [Generating Draft Reports](#41-generating-draft-reports)
   - [Reviewing Report Data](#42-reviewing-report-data)
   - [Understanding Validation Warnings](#43-understanding-validation-warnings)
   - [Correcting Errors Before Submission](#44-correcting-errors-before-submission)
   - [Approval Workflow](#45-approval-workflow)
5. [Submitting to TTB](#5-submitting-to-ttb)
   - [Downloading the Final PDF](#51-downloading-the-final-pdf)
   - [Where to Submit](#52-where-to-submit)
   - [Entering Confirmation Number](#53-entering-confirmation-number)
   - [What Happens After Submission](#54-what-happens-after-submission)
6. [Corrections and Amendments](#6-corrections-and-amendments)
   - [Handling Errors After Submission](#61-handling-errors-after-submission)
   - [When to File Amended Reports](#62-when-to-file-amended-reports)
   - [Adding Manual Transactions for Prior Periods](#63-adding-manual-transactions-for-prior-periods)
7. [Audit Trail](#7-audit-trail)
   - [Viewing the Audit Trail](#71-viewing-the-audit-trail)
   - [Exporting Audit Logs](#72-exporting-audit-logs)
   - [Understanding Immutability Rules](#73-understanding-immutability-rules)
8. [Federal Excise Tax](#8-federal-excise-tax)
   - [Understanding Tax Rates](#81-understanding-tax-rates)
   - [Calculating Tax on Removals](#82-calculating-tax-on-removals)
   - [QuickBooks Integration](#83-quickbooks-integration)
9. [Frequently Asked Questions](#9-frequently-asked-questions)
10. [Troubleshooting](#10-troubleshooting)
    - [Common Errors and Solutions](#101-common-errors-and-solutions)
    - [Getting Help](#102-getting-help)

---

## 1. Introduction

### 1.1 What is TTB Compliance?

The **Alcohol and Tobacco Tax and Trade Bureau (TTB)** is the federal agency responsible for regulating the distilled spirits industry in the United States. All Distilled Spirits Plants (DSPs) are required to:

- Maintain accurate records of all spirits produced, stored, and removed
- File monthly operations reports
- Pay federal excise taxes on spirits removed for consumption
- Retain records for a minimum of 3 years for TTB inspection

**Caskr's TTB Compliance module** automates much of this record-keeping and reporting, ensuring your distillery stays in compliance with federal regulations (27 CFR Part 19).

> **Important:** While Caskr helps you generate accurate reports, the ultimate responsibility for TTB compliance rests with the distillery. Always review reports carefully before submission.

### 1.2 Understanding TTB Forms

Caskr supports the two primary monthly reporting forms:

#### TTB Form 5110.28 - Monthly Report of Processing Operations

This form reports all **processing activities** including:
- Production of distilled spirits
- Bottling operations
- Transfers received and sent
- Losses and gains
- Tax-paid removals

**Who files:** All DSPs with production or processing operations

#### TTB Form 5110.40 - Monthly Report of Storage Operations

This form reports all **storage activities** including:
- Opening and closing inventory in bonded storage
- Receipts into storage
- Removals from storage
- Warehouse-specific barrel counts

**Who files:** DSPs with bonded storage operations (warehouses)

```
┌─────────────────────────────────────────────────────────────────┐
│                    TTB FORM DECISION GUIDE                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Do you produce or process spirits?                             │
│           │                                                     │
│     ┌─────┴─────┐                                              │
│     │           │                                              │
│    YES          NO                                              │
│     │           │                                              │
│     ▼           │                                              │
│  File Form      │                                              │
│  5110.28        │                                              │
│                 │                                              │
│  Do you have bonded storage warehouses?                        │
│           │                                                     │
│     ┌─────┴─────┐                                              │
│     │           │                                              │
│    YES          NO                                              │
│     │           │                                              │
│     ▼           ▼                                              │
│  Also File    File Only                                         │
│  Form 5110.40  5110.28                                         │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 1.3 Key Terminology

| Term | Definition |
|------|------------|
| **Proof Gallon (PG)** | The standard unit for measuring spirits. One proof gallon = 1 gallon of liquid at 100 proof (50% ABV). Formula: `PG = Wine Gallons × (ABV × 2) / 100` |
| **Wine Gallon (WG)** | The actual liquid volume in gallons, regardless of proof |
| **Proof** | Twice the alcohol by volume (ABV). 80 proof = 40% ABV |
| **Bonded** | Spirits held in storage without tax paid (tax-deferred) |
| **Tax Paid** | Spirits on which federal excise tax has been paid |
| **Tax Determination** | The event when tax liability is established (usually at removal) |
| **DSP** | Distilled Spirits Plant - your licensed distillery premises |
| **Gauge** | A physical measurement of a barrel's contents (proof, volume, temperature) |

---

## 2. Setup and Configuration

### 2.1 Enabling TTB Compliance

TTB Compliance features must be enabled for your company before you can use them.

**To enable TTB Compliance:**

1. Navigate to **Settings** → **Company Settings**
2. Scroll to the **Compliance** section
3. Toggle **Enable TTB Compliance** to ON
4. Click **Save Changes**

```
┌─────────────────────────────────────────────────────────────────┐
│                    COMPANY SETTINGS                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Company Information                                            │
│  ──────────────────────                                        │
│  Company Name: [Acme Distilling Co.          ]                 │
│                                                                 │
│  Compliance                                                     │
│  ──────────────────────                                        │
│  [✓] Enable TTB Compliance                                     │
│                                                                 │
│  TTB Permit Number: [DSP-XX-#####            ]                 │
│                                                                 │
│  Reduced Rate Eligible: [✓] Yes                                │
│  (Under 100,000 proof gallons annual production)               │
│                                                                 │
│                              [ Cancel ]  [ Save Changes ]       │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

> **Note:** Only users with Admin or SuperAdmin roles can enable TTB Compliance.

### 2.2 Configuring Your TTB Permit Number

Your TTB permit number is required on all reports and must be configured correctly.

**To configure your permit number:**

1. Navigate to **Settings** → **Company Settings**
2. Enter your **TTB Permit Number** in the format `DSP-XX-#####`
   - `DSP` = Distilled Spirits Plant
   - `XX` = State abbreviation (e.g., KY, TN, TX)
   - `#####` = Your permit number
3. Click **Save Changes**

**Example:** `DSP-KY-12345`

> **Warning:** Reports cannot be submitted without a valid TTB permit number. You will see a validation error if this is missing.

### 2.3 Setting Up Automatic Report Generation

Caskr can automatically generate draft TTB reports at the end of each month.

**To configure automatic report generation:**

1. Navigate to **TTB Compliance** → **Settings**
2. Under **Automatic Report Generation**:
   - Toggle **Auto-Generate Monthly Reports** to ON
   - Select **Report Generation Day** (1-28, typically 1st of month for previous month)
   - Choose which forms to generate:
     - [✓] Form 5110.28 (Processing Operations)
     - [✓] Form 5110.40 (Storage Operations)
3. Configure **Notification Settings**:
   - Enter email addresses for report notifications
   - Select notification triggers:
     - [✓] Draft report generated
     - [✓] Validation warnings found
     - [✓] Report approved
4. Click **Save Settings**

```
┌─────────────────────────────────────────────────────────────────┐
│              AUTO REPORT GENERATION SETTINGS                    │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  [✓] Enable Automatic Report Generation                        │
│                                                                 │
│  Generation Schedule                                            │
│  ──────────────────────                                        │
│  Generate on day: [ 1 ▼] of each month                         │
│  For the previous month's data                                 │
│                                                                 │
│  Forms to Generate                                              │
│  ──────────────────────                                        │
│  [✓] Form 5110.28 - Processing Operations                      │
│  [✓] Form 5110.40 - Storage Operations                         │
│                                                                 │
│  Notifications                                                  │
│  ──────────────────────                                        │
│  Send notifications to:                                         │
│  [compliance@acmedistilling.com                     ]          │
│                                                                 │
│                              [ Cancel ]  [ Save Settings ]      │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 2.4 Assigning Compliance Roles

TTB reports require a multi-level approval workflow. You must assign appropriate roles to team members.

**Available Compliance Roles:**

| Role | Permissions |
|------|-------------|
| **Operator** | View reports, enter daily transactions and gauge records |
| **Compliance Manager** | Review and approve reports, manage transactions |
| **Admin** | Full compliance access, configure settings |
| **SuperAdmin** | All permissions across all companies |

**To assign compliance roles:**

1. Navigate to **Settings** → **User Management**
2. Select the user you want to configure
3. In the **Role** dropdown, select the appropriate role
4. Click **Save**

**Best Practice:** Assign at least two users with approval capability (Compliance Manager or Admin) to ensure reports can be approved even when one person is unavailable.

---

## 3. Daily Operations

### 3.1 Automatic Transaction Logging

Caskr automatically creates TTB transaction records when you perform standard operations. You do **not** need to manually enter these transactions:

| Operation in Caskr | TTB Transaction Created |
|-------------------|------------------------|
| Complete a production batch | **Production** transaction logged with proof gallons |
| Transfer spirits to another DSP | **Transfer Out** transaction logged |
| Receive spirits from another DSP | **Transfer In** transaction logged |
| Complete a bottling run | **Bottling** transaction logged |
| Ship a tax-paid order | **Tax Determination** transaction logged |

**How it works:**

1. When you mark a batch as **Complete**, Caskr calculates:
   - Total wine gallons produced
   - Average proof/ABV
   - Proof gallons (using formula: `PG = WG × (ABV × 2) / 100`)
2. A TTB Production transaction is automatically created
3. The transaction appears in your TTB Transactions list

```
┌─────────────────────────────────────────────────────────────────┐
│                    AUTOMATIC TTB LOGGING                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────┐      ┌─────────────┐      ┌─────────────┐     │
│  │   Complete  │ ───► │   Caskr     │ ───► │    TTB      │     │
│  │    Batch    │      │  Calculates │      │ Transaction │     │
│  │             │      │  Proof Gal  │      │   Created   │     │
│  └─────────────┘      └─────────────┘      └─────────────┘     │
│                                                                 │
│  Example:                                                       │
│  • Batch #B-2024-0423 completed                                │
│  • 500 wine gallons at 120 proof                               │
│  • Auto-calculated: 600 proof gallons                          │
│  • Transaction type: PRODUCTION                                 │
│  • Product type: Whiskey - Under 190 Proof                     │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 3.2 Manual Transaction Entry

Some transactions must be entered manually because they cannot be automatically detected:

**When to enter manual transactions:**

- **Losses** - Breakage, leakage, evaporation beyond normal
- **Gains** - Unexpected increases (rare, requires explanation)
- **Corrections** - Adjusting prior errors
- **Destructions** - Voluntary destruction of spirits

**To enter a manual transaction:**

1. Navigate to **TTB Compliance** → **Transactions**
2. Click **+ New Transaction**
3. Fill in the transaction details:

```
┌─────────────────────────────────────────────────────────────────┐
│                    NEW TTB TRANSACTION                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Transaction Date: [11/15/2025        📅]                      │
│                                                                 │
│  Transaction Type: [ Loss                     ▼]               │
│                                                                 │
│  Product Type:     [ Whiskey                  ▼]               │
│                                                                 │
│  Spirits Type:     [ Under 190 Proof          ▼]               │
│                                                                 │
│  Tax Status:       [ Bonded                   ▼]               │
│                                                                 │
│  Proof Gallons:    [25.50                      ]               │
│                                                                 │
│  Wine Gallons:     [21.25                      ]               │
│                                                                 │
│  Notes/Explanation:                                             │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ Barrel #B-1523 discovered with leak during monthly     │   │
│  │ inventory. Cooperage repair scheduled.                  │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│                              [ Cancel ]  [ Save Transaction ]   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

4. Click **Save Transaction**

> **Important:** All manual transactions must include a note explaining the reason. This is required for TTB compliance and audit purposes.

### 3.3 Gauge Record Entry for Barrels

Gauge records document the physical measurement of barrel contents and are required by TTB regulations.

**Types of Gauge Records:**

| Type | When Required |
|------|--------------|
| **Fill Gauge** | When filling a new barrel |
| **Storage Gauge** | Periodic checks during aging (at least annually) |
| **Removal Gauge** | When emptying a barrel for bottling or transfer |

**To enter a gauge record:**

1. Navigate to **TTB Compliance** → **Gauge Records**
2. Click **+ New Gauge Record**
3. Enter the gauge details:

```
┌─────────────────────────────────────────────────────────────────┐
│                    NEW GAUGE RECORD                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Barrel: [B-2024-0156 - Rye Whiskey             🔍]            │
│                                                                 │
│  Gauge Date: [11/15/2025        📅]                            │
│                                                                 │
│  Gauge Type: [ Storage Gauge              ▼]                   │
│                                                                 │
│  Measurements                                                   │
│  ──────────────────────                                        │
│  Proof:            [125.4                   ]                  │
│  Temperature (°F): [68.0                    ]                  │
│  Wine Gallons:     [49.5                    ]                  │
│                                                                 │
│  Calculated Values (auto-filled)                               │
│  ──────────────────────                                        │
│  Correction Factor: 0.998                                       │
│  Corrected Proof Gallons: 61.85                                │
│                                                                 │
│  Notes:                                                         │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ Annual inventory gauge - barrel in good condition       │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│                              [ Cancel ]  [ Save Record ]        │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

4. Click **Save Record**

**Temperature Correction:**

Caskr automatically applies TTB Table 7 correction factors based on temperature and proof. The correction adjusts for the expansion/contraction of spirits at temperatures other than the standard 60°F.

| Temperature Range | Typical Correction Factor |
|------------------|--------------------------|
| Below 40°F | 1.016 - 1.026 |
| 40-60°F | 1.000 - 1.016 |
| 60°F (standard) | 1.000 |
| 60-80°F | 0.984 - 1.000 |
| Above 80°F | 0.960 - 0.984 |

---

## 4. Monthly Reporting

### 4.1 Generating Draft Reports

At the end of each month, you need to generate TTB reports for the previous month's operations.

**To generate a report manually:**

1. Navigate to **TTB Compliance** → **Reports**
2. Click **+ Generate Report**
3. Select the report parameters:

```
┌─────────────────────────────────────────────────────────────────┐
│                    GENERATE TTB REPORT                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Report Period                                                  │
│  ──────────────────────                                        │
│  Month: [ October      ▼]  Year: [ 2025 ▼]                     │
│                                                                 │
│  Form Type                                                      │
│  ──────────────────────                                        │
│  ( ) Form 5110.28 - Processing Operations                      │
│  ( ) Form 5110.40 - Storage Operations                         │
│  (●) Both Forms                                                 │
│                                                                 │
│  Options                                                        │
│  ──────────────────────                                        │
│  [✓] Include validation warnings in output                     │
│  [✓] Auto-calculate closing inventory                          │
│                                                                 │
│                              [ Cancel ]  [ Generate Report ]    │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

4. Click **Generate Report**
5. The system will:
   - Calculate opening inventory from the previous month's closing
   - Sum all production transactions
   - Sum all transfer transactions
   - Calculate losses and gains
   - Compute closing inventory
   - Validate the inventory balance equation
   - Generate a draft PDF

**Automatic Generation:**

If you've configured automatic report generation (see Section 2.3), draft reports are created automatically on the configured day. You'll receive an email notification when drafts are ready for review.

### 4.2 Reviewing Report Data

Before submitting a report, carefully review all data for accuracy.

**To review a report:**

1. Navigate to **TTB Compliance** → **Reports**
2. Find the report in the list and click **View Details**
3. Review each section:

```
┌─────────────────────────────────────────────────────────────────┐
│                TTB REPORT - OCTOBER 2025                        │
│                     Form 5110.28                                │
├─────────────────────────────────────────────────────────────────┤
│  Status: DRAFT                  Generated: 11/01/2025          │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  SECTION: OPENING INVENTORY                                     │
│  ──────────────────────────────────────────────────────────────│
│  Product Type        Spirits Type      Tax Status    Proof Gal │
│  ──────────────────────────────────────────────────────────────│
│  Whiskey             Under 190 Proof   Bonded        12,450.00 │
│  Bourbon             Under 190 Proof   Bonded         8,234.50 │
│  Vodka               190 or More       Bonded         3,120.00 │
│  ──────────────────────────────────────────────────────────────│
│  TOTAL OPENING INVENTORY                             23,804.50 │
│                                                                 │
│  SECTION: PRODUCTION                                            │
│  ──────────────────────────────────────────────────────────────│
│  Product Type        Spirits Type      Tax Status    Proof Gal │
│  ──────────────────────────────────────────────────────────────│
│  Whiskey             Under 190 Proof   Bonded         2,150.00 │
│  Vodka               190 or More       Bonded         1,800.00 │
│  ──────────────────────────────────────────────────────────────│
│  TOTAL PRODUCTION                                     3,950.00 │
│                                                                 │
│  SECTION: TRANSFERS IN                                          │
│  ──────────────────────────────────────────────────────────────│
│  (No transfers received this period)                     0.00  │
│                                                                 │
│  SECTION: TRANSFERS OUT                                         │
│  ──────────────────────────────────────────────────────────────│
│  Product Type        Spirits Type      Tax Status    Proof Gal │
│  ──────────────────────────────────────────────────────────────│
│  Whiskey             Under 190 Proof   Tax Paid       1,250.00 │
│  ──────────────────────────────────────────────────────────────│
│  TOTAL TRANSFERS OUT                                  1,250.00 │
│                                                                 │
│  SECTION: LOSSES                                                │
│  ──────────────────────────────────────────────────────────────│
│  Product Type        Spirits Type      Tax Status    Proof Gal │
│  ──────────────────────────────────────────────────────────────│
│  Whiskey             Under 190 Proof   Bonded            45.25 │
│  ──────────────────────────────────────────────────────────────│
│  TOTAL LOSSES                                            45.25 │
│                                                                 │
│  SECTION: CLOSING INVENTORY (Calculated)                        │
│  ──────────────────────────────────────────────────────────────│
│  Opening + Production + In - Out - Losses = Closing            │
│  23,804.50 + 3,950.00 + 0.00 - 1,250.00 - 45.25 = 26,459.25   │
│  ──────────────────────────────────────────────────────────────│
│  TOTAL CLOSING INVENTORY                             26,459.25 │
│                                                                 │
│  VALIDATION STATUS: ✅ PASSED                                   │
│                                                                 │
│  [ Download PDF ]  [ Submit for Review ]  [ Edit Transactions ] │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

**Key items to verify:**

- [ ] Opening inventory matches last month's closing inventory
- [ ] All production batches are accounted for
- [ ] Transfers in/out match your transfer documentation
- [ ] Losses are reasonable and documented
- [ ] Closing inventory calculation is correct
- [ ] Tax status is correct for all line items

### 4.3 Understanding Validation Warnings

Caskr performs automatic validation checks and displays warnings/errors that need attention.

**Validation Error Types:**

| Type | Icon | Meaning | Action Required |
|------|------|---------|----------------|
| **Error** | ❌ | Critical issue - cannot submit | Must fix before submission |
| **Warning** | ⚠️ | Potential issue - review recommended | Review but can still submit |
| **Info** | ℹ️ | Informational note | No action required |

**Common Validation Messages:**

```
┌─────────────────────────────────────────────────────────────────┐
│                    VALIDATION RESULTS                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ❌ ERROR: Missing TTB Permit Number                            │
│     Your company profile does not have a TTB permit number      │
│     configured. Go to Settings → Company Settings to add it.   │
│                                                                 │
│  ❌ ERROR: Inventory Imbalance Detected                         │
│     Calculated closing inventory (26,459.25 PG) does not match │
│     physical inventory (26,485.00 PG). Difference: 25.75 PG    │
│     exceeds tolerance of 0.1%.                                  │
│                                                                 │
│  ⚠️ WARNING: High Loss Percentage                               │
│     Losses of 1.2% exceed the typical threshold of 1%.         │
│     Review loss transactions for accuracy.                      │
│                                                                 │
│  ⚠️ WARNING: Negative Inventory Line Item                       │
│     Bourbon - Under 190 Proof shows negative closing inventory │
│     of -15.00 PG. This may indicate missing production records.│
│                                                                 │
│  ℹ️ INFO: First Report of Year                                  │
│     This is your first report for 2025. Opening inventory      │
│     should match 2024 December closing inventory.              │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 4.4 Correcting Errors Before Submission

If validation finds errors, you must correct them before submitting the report.

**To correct errors:**

1. **Review the error message** to understand what needs to be fixed
2. **Navigate to the relevant section:**
   - For transaction errors: **TTB Compliance** → **Transactions**
   - For gauge record errors: **TTB Compliance** → **Gauge Records**
   - For company settings: **Settings** → **Company Settings**
3. **Make the correction:**
   - Add missing transactions
   - Edit incorrect values
   - Delete duplicate entries
4. **Regenerate the report** to reflect the changes
5. **Review validation** again to confirm the error is resolved

**Common corrections:**

| Error | Solution |
|-------|----------|
| Missing production | Add Production transaction for the batch |
| Inventory imbalance | Add Loss or Gain transaction to reconcile |
| Missing permit number | Update Company Settings |
| Negative inventory | Review transactions for errors or add production |

### 4.5 Approval Workflow

TTB reports must go through an approval workflow before submission to TTB.

**Workflow States:**

```
┌─────────┐    Submit for    ┌─────────────┐    Approve    ┌──────────┐
│  DRAFT  │ ──────────────►  │ PENDING     │ ────────────► │ APPROVED │
│         │     Review       │ REVIEW      │               │          │
└─────────┘                  └─────────────┘               └──────────┘
     ▲                             │                             │
     │                             │ Reject                      │ Submit
     │                             ▼                             │ to TTB
     │                       ┌─────────┐                         ▼
     └───────────────────────│ DRAFT   │                   ┌──────────┐
          (with notes)       └─────────┘                   │SUBMITTED │
                                                           └──────────┘
                                                                │
                                                                │ Archive
                                                                ▼
                                                           ┌──────────┐
                                                           │ ARCHIVED │
                                                           └──────────┘
```

**Step 1: Submit for Review**

1. Ensure all validation errors are resolved
2. Click **Submit for Review**
3. Optionally add notes for the reviewer
4. The report status changes to **Pending Review**
5. Assigned reviewers receive an email notification

**Step 2: Manager Review and Approval**

1. Reviewer navigates to **TTB Compliance** → **Reports**
2. Filter by status: **Pending Review**
3. Click on the report to review
4. After reviewing, click either:
   - **Approve** - advances to Approved status
   - **Reject** - returns to Draft status with notes

```
┌─────────────────────────────────────────────────────────────────┐
│                    REPORT REVIEW                                │
├─────────────────────────────────────────────────────────────────┤
│  Report: Form 5110.28 - October 2025                           │
│  Submitted by: John Smith on 11/02/2025                        │
│  Status: PENDING REVIEW                                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Review Notes:                                                  │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ Verified production records against batch logs.         │   │
│  │ Loss amount confirmed with warehouse manager.           │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  [ View Full Report ]  [ View Transactions ]  [ View Audit ]   │
│                                                                 │
│                              [ Reject ]  [ Approve Report ]     │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 5. Submitting to TTB

### 5.1 Downloading the Final PDF

Once a report is approved, download the final PDF for submission.

**To download the PDF:**

1. Navigate to **TTB Compliance** → **Reports**
2. Find the approved report
3. Click **Download PDF**
4. The PDF is saved to your Downloads folder

**PDF Contents:**

The downloaded PDF is formatted according to TTB specifications and includes:
- Form header with your DSP permit number
- All inventory and transaction data
- Signature block (sign before submitting)
- Date fields

> **Important:** Review the PDF carefully before submission. Once submitted to TTB, corrections require filing an amended report.

### 5.2 Where to Submit

TTB reports are submitted through the **TTB Pay.gov Portal**.

**Submission Steps:**

1. Go to [https://www.pay.gov/public/form/start/677045601](https://www.pay.gov/public/form/start/677045601)
2. Log in with your TTB.gov credentials
3. Select the appropriate form type
4. Upload your PDF or enter data manually
5. Pay any applicable excise tax
6. Submit the form
7. **Save the confirmation number**

```
┌─────────────────────────────────────────────────────────────────┐
│                    TTB SUBMISSION LINKS                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Pay.gov Portal:                                                │
│  https://www.pay.gov                                           │
│                                                                 │
│  TTB.gov Account:                                               │
│  https://www.ttb.gov/services/permits-online                   │
│                                                                 │
│  Form Instructions:                                             │
│  • 5110.28: https://www.ttb.gov/forms/f511028                  │
│  • 5110.40: https://www.ttb.gov/forms/f511040                  │
│                                                                 │
│  Help Desk: 1-866-927-3833                                     │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 5.3 Entering Confirmation Number

After submitting to TTB, record the confirmation number in Caskr.

**To enter the confirmation number:**

1. Navigate to **TTB Compliance** → **Reports**
2. Find the approved report you just submitted
3. Click **Record Submission**
4. Enter the confirmation number from Pay.gov
5. Click **Save**

```
┌─────────────────────────────────────────────────────────────────┐
│                    RECORD TTB SUBMISSION                        │
├─────────────────────────────────────────────────────────────────┤
│  Report: Form 5110.28 - October 2025                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  TTB Confirmation Number:                                       │
│  [PAY-2025-1102-XXXXX                        ]                 │
│                                                                 │
│  Submission Date: [11/05/2025        📅]                       │
│                                                                 │
│  Notes:                                                         │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ Submitted by Jane Doe. Tax payment of $16,875.00       │   │
│  │ included with submission.                               │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│                              [ Cancel ]  [ Submit to TTB ]      │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 5.4 What Happens After Submission

Once you record the TTB submission, several important things occur:

1. **Report Status Changes to SUBMITTED**
   - The report can no longer be edited
   - All associated transactions are locked

2. **Month Data Becomes Immutable**
   - Transactions for that month cannot be modified
   - Any changes require an amended report

3. **Audit Log Entry Created**
   - The submission is recorded in the audit trail
   - Includes who submitted, when, and the confirmation number

4. **Opening Inventory Locked**
   - Next month's opening inventory is set from this closing
   - Creates continuity for compliance

> **Warning:** Once marked as submitted, you cannot undo this action. Only mark as submitted after you have received a confirmation number from TTB.

---

## 6. Corrections and Amendments

### 6.1 Handling Errors After Submission

If you discover an error after submitting a report to TTB, you have options depending on the severity:

**Minor Errors (no amendment needed):**
- Typos that don't affect values
- Minor rounding differences (< 0.1 PG)
- Formatting issues

**Errors Requiring Amendment:**
- Incorrect proof gallon totals (> 1 PG difference)
- Missing transactions
- Wrong category classifications
- Incorrect permit information

### 6.2 When to File Amended Reports

File an amended report when:

| Situation | Action |
|-----------|--------|
| Discovered error within same month | File amended report |
| Error affects inventory balance | File amended report |
| Missing production/transfers | File amended report |
| Incorrect tax calculation | File amended report AND contact TTB |
| Minor clerical error | Document internally, no amendment needed |

**To create an amended report in Caskr:**

1. Navigate to **TTB Compliance** → **Reports**
2. Find the submitted report
3. Click **Create Amendment**
4. Make necessary corrections
5. Generate the amended PDF
6. Submit through Pay.gov as an amended report

### 6.3 Adding Manual Transactions for Prior Periods

If you need to add a transaction for a prior period that has already been submitted:

**Option 1: Include in Current Month's Report**

1. Create a new transaction dated in the prior period
2. Add a note: "Correction for [Month/Year] - [reason]"
3. Include in your current month's report
4. The prior period's data remains unchanged but is documented

**Option 2: File Amended Report**

1. Create the amended report (see 6.2)
2. Add the missing transaction
3. Regenerate calculations
4. Submit the amendment

> **Best Practice:** Consult with your compliance advisor or TTB directly for guidance on significant corrections affecting multiple periods.

---

## 7. Audit Trail

### 7.1 Viewing the Audit Trail

Caskr maintains a complete audit trail of all TTB-related activities for compliance inspections.

**To view the audit trail:**

1. Navigate to **TTB Compliance** → **Audit Trail**
2. Use filters to narrow results:
   - Date range
   - Entity type (Report, Transaction, Gauge Record)
   - Action type (Create, Update, Delete)
   - User

```
┌─────────────────────────────────────────────────────────────────┐
│                    TTB AUDIT TRAIL                              │
├─────────────────────────────────────────────────────────────────┤
│  Filters: [Date: Last 30 Days ▼] [Type: All ▼] [Action: All ▼] │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Timestamp           User          Entity       Action          │
│  ──────────────────────────────────────────────────────────────│
│  11/05/2025 14:32   jane.doe     Report #42   SUBMIT_TO_TTB    │
│  11/05/2025 14:30   jane.doe     Report #42   APPROVE          │
│  11/04/2025 09:15   john.smith   Report #42   SUBMIT_REVIEW    │
│  11/02/2025 16:45   john.smith   Transaction  CREATE           │
│  11/02/2025 16:40   john.smith   Transaction  UPDATE           │
│  11/01/2025 08:00   SYSTEM       Report #42   CREATE (Auto)    │
│  10/28/2025 11:20   john.smith   Gauge Rec    CREATE           │
│                                                                 │
│  Showing 1-7 of 156 entries        [ < Previous ] [ Next > ]   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

**Clicking on an entry shows full details:**

```
┌─────────────────────────────────────────────────────────────────┐
│                    AUDIT LOG DETAIL                             │
├─────────────────────────────────────────────────────────────────┤
│  Log ID: AUD-2025-11-0542                                      │
│  Timestamp: 11/02/2025 16:40:23 UTC                            │
│  User: john.smith (John Smith)                                 │
│  IP Address: 192.168.1.45                                      │
│  User Agent: Chrome/119.0 Windows                              │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Entity: TTB Transaction #1847                                  │
│  Action: UPDATE                                                 │
│                                                                 │
│  Changes:                                                       │
│  ┌────────────────┬────────────────┬────────────────┐          │
│  │ Field          │ Old Value      │ New Value      │          │
│  ├────────────────┼────────────────┼────────────────┤          │
│  │ ProofGallons   │ 24.50          │ 25.50          │          │
│  │ Notes          │ (empty)        │ Corrected...   │          │
│  └────────────────┴────────────────┴────────────────┘          │
│                                                                 │
│  Description: Updated proof gallons from 24.50 to 25.50        │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 7.2 Exporting Audit Logs

Export audit logs for TTB inspections or internal review.

**To export audit logs:**

1. Navigate to **TTB Compliance** → **Audit Trail**
2. Set your date range filters
3. Click **Export to CSV**
4. Choose export options:
   - Include all fields
   - Include change details
   - Date format preference
5. Click **Download**

**Export includes:**
- Timestamp
- User name and ID
- Entity type and ID
- Action type
- Old and new values
- IP address
- Change description

> **For TTB Inspections:** Export the full audit trail for the inspection period. Provide in CSV format or print for the inspector.

### 7.3 Understanding Immutability Rules

Caskr enforces immutability rules to maintain audit integrity:

**Immutable Once Submitted:**
- Reports marked as "Submitted" cannot be modified
- Transactions in submitted months cannot be changed
- Gauge records in submitted months are locked

**What CAN be changed:**
- Draft reports (before submission)
- Transactions in the current (non-submitted) month
- Company settings (except permit number on submitted reports)

**What CANNOT be changed:**
- Audit log entries (ever)
- Submitted reports
- Transactions in submitted periods

> **Important:** This immutability is by design to maintain compliance integrity. If you need to make corrections to submitted data, file an amended report.

---

## 8. Federal Excise Tax

### 8.1 Understanding Tax Rates

Federal excise tax is due when spirits are removed from bond for consumption.

**Current Tax Rates (as of 2025):**

| Rate Type | Rate per Proof Gallon | Eligibility |
|-----------|----------------------|-------------|
| **Standard Rate** | $13.50 | All DSPs |
| **Reduced Rate** | $13.34 | DSPs producing < 100,000 PG/year |

**Craft Beverage Modernization Act (CBMA) Benefits:**

Small distilleries qualify for the reduced rate on the first 100,000 proof gallons removed each calendar year.

```
┌─────────────────────────────────────────────────────────────────┐
│                    TAX RATE CALCULATION                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Example: 500 proof gallons removed                            │
│                                                                 │
│  If eligible for reduced rate:                                  │
│  500 PG × $13.34 = $6,670.00                                   │
│                                                                 │
│  If NOT eligible (or over 100,000 PG):                         │
│  500 PG × $13.50 = $6,750.00                                   │
│                                                                 │
│  Savings with reduced rate: $80.00                             │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 8.2 Calculating Tax on Removals

When spirits are removed from bond (sold, sampled, used), excise tax is calculated.

**To view tax calculations:**

1. Navigate to **Orders** → select an order
2. View the **Tax** tab
3. See calculated tax based on proof gallons

**Or from TTB Compliance:**

1. Navigate to **TTB Compliance** → **Excise Tax**
2. View tax calculations by period
3. See year-to-date totals and remaining reduced rate allocation

```
┌─────────────────────────────────────────────────────────────────┐
│                    EXCISE TAX REPORT - 2025                     │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Annual Summary                                                 │
│  ──────────────────────                                        │
│  Total Proof Gallons Removed (YTD):     45,230.50 PG           │
│  Reduced Rate Remaining:                54,769.50 PG           │
│  Total Tax Liability (YTD):             $603,022.87            │
│                                                                 │
│  Monthly Breakdown                                              │
│  ──────────────────────                                        │
│  Month       PG Removed   Rate      Tax Due      Status        │
│  ──────────────────────────────────────────────────────────────│
│  January     4,520.00    Reduced    $60,276.80   PAID          │
│  February    3,890.00    Reduced    $51,892.60   PAID          │
│  March       5,120.00    Reduced    $68,300.80   PAID          │
│  ...                                                            │
│  October     4,850.25    Reduced    $64,702.34   PENDING       │
│                                                                 │
│  [ Export Report ]  [ View Details ]                            │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 8.3 QuickBooks Integration

Caskr can post excise tax liabilities to QuickBooks for accounting purposes.

**To post tax to QuickBooks:**

1. Navigate to **TTB Compliance** → **Excise Tax**
2. Find the tax determination you want to post
3. Click **Post to QuickBooks**
4. Verify the journal entry details
5. Click **Confirm**

**Journal Entry Created:**

| Account | Debit | Credit |
|---------|-------|--------|
| Federal Excise Tax Expense | $X,XXX.XX | |
| Excise Tax Payable | | $X,XXX.XX |

---

## 9. Frequently Asked Questions

### General Questions

**Q: What if my inventory doesn't reconcile?**

A: Inventory imbalances typically occur due to:
1. **Missing transactions** - Check for unreported production, transfers, or losses
2. **Measurement errors** - Verify gauge records are accurate
3. **Rounding differences** - Small differences (< 0.1%) are acceptable
4. **Actual losses** - Angel's share, leakage, or breakage not recorded

**Solution:** Add a Loss or Gain transaction to reconcile, with a detailed explanation. For significant discrepancies (> 1%), investigate thoroughly before adjusting.

---

**Q: How do I report evaporation losses (angel's share)?**

A: Evaporation losses should be recorded as Loss transactions:

1. Navigate to **TTB Compliance** → **Transactions**
2. Click **+ New Transaction**
3. Select **Transaction Type: Loss**
4. Enter the proof gallons lost
5. In notes, specify: "Evaporation/angel's share - [period]"

**Best Practice:** Record evaporation losses monthly based on gauge record comparisons, or annually during inventory.

---

**Q: What's the difference between proof gallons and wine gallons?**

A:
- **Wine Gallons (WG):** The actual liquid volume in gallons
- **Proof Gallons (PG):** The taxable volume, adjusted for alcohol content

**Formula:** `Proof Gallons = Wine Gallons × (Proof / 100)`

**Example:**
- 100 wine gallons at 80 proof (40% ABV)
- Proof Gallons = 100 × (80/100) = 80 PG
- 100 wine gallons at 120 proof (60% ABV)
- Proof Gallons = 100 × (120/100) = 120 PG

---

**Q: When are TTB reports due?**

A: Monthly TTB reports are due by the **15th of the following month**.

- October report due: November 15
- November report due: December 15
- December report due: January 15

**Exception:** If the 15th falls on a weekend or holiday, the due date is the next business day.

---

**Q: Can I submit reports early?**

A: Yes, you can submit reports as soon as you have complete data for the month. Many distilleries submit within the first week of the following month.

---

**Q: What happens if I miss a filing deadline?**

A: Late filing can result in:
- Penalties from TTB
- Interest on unpaid taxes
- Potential audit triggers

**If you're going to be late:** Contact TTB proactively to explain the situation.

---

**Q: Do I need to keep paper records?**

A: TTB regulations require record retention for 3 years. Caskr stores all records digitally, which is accepted by TTB. However, you may want to:
- Keep PDF copies of submitted reports
- Export and backup audit logs periodically
- Maintain physical copies of particularly important documents

---

**Q: How do I handle spirits received from another DSP?**

A: Record as a **Transfer In** transaction:

1. When you receive spirits, enter a Transfer In transaction
2. Include the source DSP permit number in notes
3. Verify proof gallons against the shipping documents
4. The spirits should appear in your inventory for the month received

---

**Q: What if I make a production run that spans two months?**

A: Record the production in the month when the batch is **completed** (gauged and ready for storage). The TTB cares about when spirits enter your inventory, not when production started.

---

### Technical Questions

**Q: Why is my report showing validation errors?**

A: Common causes:
- Missing TTB permit number in company settings
- Inventory imbalance exceeding tolerance
- Negative inventory values
- Missing required transactions

See [Section 4.3](#43-understanding-validation-warnings) for detailed explanations.

---

**Q: How accurate do my measurements need to be?**

A: TTB requires:
- **Proof:** Accurate to 0.1 proof
- **Volume:** Accurate to 0.1 gallon
- **Temperature:** Accurate to 1°F (for correction)
- **Proof Gallons:** Accurate to 0.01 PG (rounded)

---

**Q: Can I edit a submitted report?**

A: No, submitted reports are locked. To make corrections, you must file an amended report. See [Section 6](#6-corrections-and-amendments).

---

**Q: How do I set up automatic reports for multiple forms?**

A: Configure both forms in auto-generation settings:
1. Go to **TTB Compliance** → **Settings**
2. Check both Form 5110.28 and Form 5110.40
3. Both will generate on your scheduled day

---

## 10. Troubleshooting

### 10.1 Common Errors and Solutions

#### Error: "Missing TTB Permit Number"

**Cause:** Company profile doesn't have TTB permit configured.

**Solution:**
1. Go to **Settings** → **Company Settings**
2. Enter your TTB permit number (format: DSP-XX-#####)
3. Save and regenerate the report

---

#### Error: "Inventory Balance Does Not Reconcile"

**Cause:** Closing inventory doesn't match the calculation:
`Opening + Production + Transfers In - Transfers Out - Losses = Closing`

**Solution:**
1. Review all transactions for the month
2. Check for missing production batches
3. Verify transfers match documentation
4. Add a Loss or Gain transaction if needed
5. Regenerate the report

---

#### Error: "Negative Inventory Detected"

**Cause:** More was removed than was available in inventory.

**Solution:**
1. Check for missing production records
2. Verify opening inventory is correct
3. Check for duplicate removal transactions
4. Add missing production if applicable

---

#### Error: "Cannot Submit - Report Has Validation Errors"

**Cause:** Critical errors exist that prevent submission.

**Solution:**
1. Review all errors in the validation panel
2. Fix each error as described
3. Regenerate the report
4. Confirm all errors are resolved
5. Submit for review

---

#### Error: "Permission Denied - Cannot Approve Report"

**Cause:** Your user role doesn't have approval permissions.

**Solution:**
1. Contact your administrator
2. Request Compliance Manager or Admin role
3. Or have an authorized user approve the report

---

#### Error: "Cannot Modify - Month is Locked"

**Cause:** A report for this month has already been submitted to TTB.

**Solution:**
1. You cannot modify transactions for submitted months
2. For corrections, file an amended report
3. Or include the correction in the current month with notes

---

#### Warning: "Loss Percentage Exceeds Threshold"

**Cause:** Losses are higher than expected (typically > 1-2%).

**Solution:**
1. This is a warning, not an error - you can still submit
2. Review losses to ensure they're accurate
3. Document unusual losses thoroughly
4. Consider if there's an operational issue to address

---

#### Warning: "First Report of Year - Verify Opening Inventory"

**Cause:** January report needs careful opening inventory verification.

**Solution:**
1. Verify opening inventory matches December's closing
2. If different, investigate the discrepancy
3. Document any adjustments

---

### 10.2 Getting Help

**In-App Support:**
- Click the **?** icon in the top navigation
- Search the knowledge base
- Submit a support ticket

**TTB Resources:**
- TTB Help Desk: 1-866-927-3833
- TTB Website: https://www.ttb.gov
- Form Instructions: https://www.ttb.gov/forms

**Caskr Support:**
- Email: support@caskr.com
- Documentation: https://docs.caskr.com
- Status Page: https://status.caskr.com

---

## Appendix A: TTB Form Field Mapping

### Form 5110.28 Sections

| Section | Description | Caskr Data Source |
|---------|-------------|-------------------|
| Part I | Plant Identification | Company Settings |
| Part II | Spirits Produced | Production Transactions |
| Part III | Spirits Received | Transfer In Transactions |
| Part IV | Spirits on Hand | Inventory Snapshots |
| Part V | Spirits Removed | Transfer Out + Tax Paid |
| Part VI | Losses | Loss Transactions |

### Form 5110.40 Sections

| Section | Description | Caskr Data Source |
|---------|-------------|-------------------|
| Part I | Warehouse Identification | Company Settings |
| Part II | Opening Inventory | Previous Month Closing |
| Part III | Receipts | Transfer In + Production |
| Part IV | Removals | Transfer Out + Bottling |
| Part V | Closing Inventory | Calculated |
| Part VI | Barrel Counts | Active Barrels |

---

## Appendix B: Glossary

| Term | Definition |
|------|------------|
| **ABV** | Alcohol By Volume - the percentage of alcohol in a liquid |
| **Angel's Share** | Spirits lost to evaporation during barrel aging |
| **Bonded** | Spirits held in storage without tax paid |
| **CBMA** | Craft Beverage Modernization Act - provides reduced tax rates for small producers |
| **CFR** | Code of Federal Regulations |
| **DSP** | Distilled Spirits Plant - licensed distillery premises |
| **Excise Tax** | Federal tax on distilled spirits based on proof gallons |
| **Gauge** | Physical measurement of barrel contents |
| **Proof** | Measure of alcohol content; twice the ABV percentage |
| **Proof Gallon** | One gallon at 100 proof; the taxable unit |
| **TTB** | Alcohol and Tobacco Tax and Trade Bureau |
| **Wine Gallon** | Actual liquid volume in gallons |

---

## Appendix C: Quick Reference Card

```
┌─────────────────────────────────────────────────────────────────┐
│                TTB COMPLIANCE QUICK REFERENCE                   │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  KEY FORMULA:                                                   │
│  Proof Gallons = Wine Gallons × (Proof / 100)                  │
│                                                                 │
│  INVENTORY EQUATION:                                            │
│  Closing = Opening + Production + In - Out - Losses            │
│                                                                 │
│  TAX RATES (2025):                                             │
│  Standard: $13.50/PG  |  Reduced: $13.34/PG                    │
│                                                                 │
│  REPORT DUE DATES:                                              │
│  Monthly reports due by 15th of following month                │
│                                                                 │
│  WORKFLOW:                                                      │
│  Draft → Submit for Review → Approve → Submit to TTB           │
│                                                                 │
│  HELP:                                                          │
│  TTB: 1-866-927-3833  |  Caskr: support@caskr.com             │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

*This guide is for informational purposes. Consult with your compliance advisor and TTB for official guidance on specific situations.*
