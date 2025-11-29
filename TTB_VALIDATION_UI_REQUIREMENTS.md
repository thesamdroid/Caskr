# TTB Report Validation - UI Requirements

## Overview
This document describes the UI requirements for displaying TTB report validation errors and warnings. The backend implementation is complete and the API endpoints are ready. The client UI (located in `caskr.client`) needs to be updated to consume and display validation results.

## Backend API Changes

### TtbReportSummaryResponse
The `/api/ttb/reports` endpoint now returns validation data:

```json
{
  "id": 1,
  "companyId": 1,
  "reportMonth": 10,
  "reportYear": 2024,
  "formType": "Form5110_28",
  "status": "ValidationFailed",
  "generatedAt": "2024-10-31T12:00:00Z",
  "validationErrors": "[\"Inventory reconciliation failed for Whiskey/Under190Proof. Check transaction logs.\"]",
  "validationWarnings": "[\"Loss percentage (16.00%) is unusually high for Whiskey/Under190Proof. Review loss entries.\"]"
}
```

### New Report Status
- `ValidationFailed` (value: 4) - Report has validation errors that must be fixed before submission

## UI Implementation Requirements

### 1. Reports List Page (TtbReportsPage)

#### Display Validation Status
- Show a warning icon (⚠️) next to reports with `status === "ValidationFailed"`
- Show an info icon (ℹ️) next to reports that have warnings but are valid
- Color code by status:
  - `ValidationFailed`: Red/danger color
  - `Draft` with warnings: Yellow/warning color
  - `Draft` without issues: Normal color

#### Add Validation Column
Add a "Validation" column to the reports table showing:
- "✓ Valid" - No errors, no warnings
- "⚠️ N Warnings" - No errors, N warnings
- "❌ N Errors" - N errors (may also have warnings)

### 2. View Validation Report Button

Add a "View Validation Report" button that:
- Is visible for all reports (enabled when `validationErrors` or `validationWarnings` are present)
- Opens a modal/dialog showing detailed validation results
- Displays separately:
  - **Errors** (must fix before submission) - in red
  - **Warnings** (should review) - in yellow/orange

Example modal:
```
┌─────────────────────────────────────────┐
│ Validation Report - October 2024        │
├─────────────────────────────────────────┤
│ ❌ Errors (2)                           │
│                                         │
│ • Inventory reconciliation failed for   │
│   Whiskey/Under190Proof. Check          │
│   transaction logs.                     │
│                                         │
│ • TTB permit number is missing. Please  │
│   configure the permit number in        │
│   company settings.                     │
│                                         │
│ ⚠️ Warnings (1)                         │
│                                         │
│ • Loss percentage (16.00%) is unusually │
│   high for Whiskey/Under190Proof.       │
│   Review loss entries.                  │
│                                         │
│         [Close]  [View Report Data]     │
└─────────────────────────────────────────┘
```

### 3. Submission Prevention

Block the "Submit to TTB" action when:
- `status === "ValidationFailed"`
- Show a message: "This report has validation errors that must be fixed before submission. Click 'View Validation Report' to see details."

Allow submission when:
- `status === "Draft"` (even if warnings present)
- Show a warning confirmation if warnings exist: "This report has N warnings. Are you sure you want to submit?"

### 4. Automatic Validation on Generation

After generating a report:
- Check the returned status
- If `ValidationFailed`, automatically show the validation modal
- Display a toast/notification:
  - Error: "Report generated with validation errors. Please review."
  - Warning: "Report generated with validation warnings. Please review."
  - Success: "Report generated successfully."

## API Endpoint Reference

### GET /api/ttb/reports
Query Parameters:
- `companyId` (required)
- `year` (required)
- `formType` (optional)
- `status` (optional)

Response includes `validationErrors` and `validationWarnings` as JSON-serialized string arrays.

### POST /api/ttb/reports/generate
Returns the same validation data in the created report record.

## Data Parsing

The validation fields are JSON-serialized arrays:
```javascript
const errors = report.validationErrors
  ? JSON.parse(report.validationErrors)
  : [];
const warnings = report.validationWarnings
  ? JSON.parse(report.validationWarnings)
  : [];
```

## Testing Scenarios

### Test Case 1: Valid Report
- No errors, no warnings
- Status: `Draft`
- Should allow submission

### Test Case 2: Report with Warnings Only
- No errors, has warnings
- Status: `Draft`
- Should show warning icon
- Should allow submission with confirmation

### Test Case 3: Report with Errors
- Has errors
- Status: `ValidationFailed`
- Should show error icon/state
- Should block submission
- Should auto-show validation modal

### Test Case 4: Report with Errors and Warnings
- Has both
- Status: `ValidationFailed`
- Should show both in validation modal
- Errors listed first, then warnings

## Notes for Implementation

1. The validation happens server-side during report generation
2. Validation errors include:
   - Inventory reconciliation failures
   - Missing TTB permit number
   - Negative inventory values
3. Validation warnings include:
   - Loss percentage > 15%
4. All validation is based on TTB compliance requirements
5. The validation cannot be skipped or bypassed
