# Caskr Great Demo! Structure

Based on "Great Demo!: How To Create And Execute Stunning Software Demonstrations" by Peter Cohan

---

## The Great Demo! Philosophy

**Core Principle: "Do the Last Thing First"**

Traditional demos start with login, navigation, data entry, and eventually show the result. Great demos invert this - show the finished, stunning outcome immediately, then peel back layers only as needed.

**Key Concepts:**
- **Situation Slide**: Brief context about the prospect's specific problem
- **Illustration**: The "Wow!" moment - show the end result first
- **Pathway**: Minimal steps to show how you get there
- **Harbor Tour**: Technical deep-dives only when specifically requested
- **The Delta**: Focus on what's different/better vs. their current solution

---

## Caskr Demo Structure

### Pre-Demo: The Situation Slide (60 seconds)

Before touching the software, establish context with the prospect:

> "You mentioned your team spends [X hours/month] on TTB reporting, and you're always worried about compliance errors that could risk your federal license. Let me show you what your life looks like with Caskr..."

**Key pain points to reference:**
- $5K-$10K/month in staff time on manual TTB reporting
- Fear of federal penalties or license suspension
- Disconnected systems (spreadsheets, paper, QuickBooks)
- No real-time visibility into production status

---

### Act 1: The Illustration (The "Wow!") - 3 Minutes

**START HERE - Show the finished outcome first!**

#### Scene 1.1: The Completed TTB Report (90 seconds)
Navigate directly to: **TTB Compliance > Reports**

Show a **completed, approved Form 5110.28** ready to submit:
- Point out all fields are auto-calculated
- Show the proof gallon calculations done automatically
- Highlight the validation checkmarks (inventory balances verified)
- Click "Preview PDF" to show the actual TTB form, filled out perfectly

**Say:** *"This is your monthly TTB submission, ready to go. Every calculation verified, every barrel accounted for. This used to take your team 3 days - Caskr generates it automatically."*

#### Scene 1.2: The Compliance Dashboard (60 seconds)
Show the **TTB Compliance Dashboard** overview:
- Green status indicators for current month
- Audit trail ready for TTB inspection
- Historical submissions archived

**Say:** *"All your compliance history in one place. When the TTB auditor comes, you hand them this - not boxes of paper."*

#### Scene 1.3: The Operations Dashboard (30 seconds)
Quick pan to **Main Dashboard**:
- All orders with progress percentages
- Tasks assigned and tracked
- Real-time production visibility

**Say:** *"And this is your production floor - every order, every task, every barrel - at a glance."*

**PAUSE HERE.** Ask: *"Is this the kind of visibility you're looking for?"*

---

### Act 2: The Pathway (How We Got There) - 5 Minutes

**Only proceed here after getting buy-in on the Illustration.**

Now peel back ONE layer to show how easy it is to achieve that outcome.

#### Scene 2.1: Logging a Transaction (2 minutes)
Navigate to: **TTB Compliance > Transactions**

**Live demo:** Add a production transaction
1. Click "Add Transaction"
2. Select "Production"
3. Enter barrel count, ABV
4. System auto-calculates proof gallons
5. Save - transaction appears instantly

**Say:** *"Your team logs production as it happens. One click, the math is done for you. No spreadsheets, no calculators."*

#### Scene 2.2: Order & Task Management (2 minutes)
Navigate to: **Dashboard**

Show how an order flows:
1. Create new order (whiskey batch)
2. Tasks auto-populate based on spirit type
3. Assign to team members with dropdown
4. Mark task complete with checkbox
5. Progress bar updates in real-time

**Say:** *"Your production team sees exactly what they need to do. Check it off, move on. Management sees the big picture."*

#### Scene 2.3: The Auto-Generated Report (1 minute)
Navigate back to: **TTB Compliance > Reports**

Click "Generate Report" for current month:
- System aggregates all transactions
- Calculates all required fields
- Validates inventory balances
- Generates PDF preview

**Say:** *"End of month: one click. Caskr does in 30 seconds what used to take your team 3 days."*

---

### Act 3: Harbor Tours (Technical Deep-Dives) - As Requested Only

**These sections are ONLY shown if the prospect asks specific questions.** Never volunteer these unless asked.

#### Harbor Tour A: QuickBooks Integration
If asked about accounting:
- Navigate to Accounting Settings
- Show bidirectional sync configuration
- Show Sync History with audit trail
- Explain automatic journal entries

#### Harbor Tour B: Barrel Tracking & Forecasting
If asked about inventory details:
- Navigate to Barrels page
- Show individual barrel tracking
- Demo the Forecasting tool (age-at-date projections)

#### Harbor Tour C: Custom Reports
If asked about analytics:
- Navigate to Report Builder
- Show drag-and-drop report creation
- Demo export to CSV/PDF

#### Harbor Tour D: Gauge Records
If asked about physical measurements:
- Navigate to TTB Gauge Records
- Show proof, temperature, volume tracking
- Explain how this feeds into TTB forms

#### Harbor Tour E: Webhook Integration
If asked about system integration:
- Explain real-time event notifications
- Show how external systems can subscribe to Caskr events

---

## Demo Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                        SITUATION SLIDE                               │
│                    (60 sec - Establish pain)                         │
└─────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│                     ACT 1: THE ILLUSTRATION                          │
│                        (3 min - The "Wow!")                          │
│  ┌───────────────┐  ┌───────────────┐  ┌───────────────┐            │
│  │ Completed TTB │→ │  Compliance   │→ │  Operations   │            │
│  │    Report     │  │   Dashboard   │  │   Dashboard   │            │
│  │   (Ready!)    │  │  (All Green)  │  │  (Progress)   │            │
│  └───────────────┘  └───────────────┘  └───────────────┘            │
│                                                                      │
│              ★ STOP AND ASK: "Is this what you need?" ★             │
└─────────────────────────────────────────────────────────────────────┘
                                   │
                          (If yes, continue)
                                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      ACT 2: THE PATHWAY                              │
│                    (5 min - One layer deep)                          │
│  ┌───────────────┐  ┌───────────────┐  ┌───────────────┐            │
│  │     Log a     │→ │   Order &     │→ │   Generate    │            │
│  │  Transaction  │  │    Tasks      │  │    Report     │            │
│  │  (So Easy!)   │  │  (Assign!)    │  │  (One Click!) │            │
│  └───────────────┘  └───────────────┘  └───────────────┘            │
└─────────────────────────────────────────────────────────────────────┘
                                   │
                     (Only if specific questions)
                                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    ACT 3: HARBOR TOURS                               │
│                  (As requested - Deep dives)                         │
│                                                                      │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐   │
│  │ QuickBooks  │ │   Barrel    │ │   Custom    │ │    Gauge    │   │
│  │ Integration │ │ Forecasting │ │   Reports   │ │   Records   │   │
│  └─────────────┘ └─────────────┘ └─────────────┘ └─────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Recommended UI Reorganization for Demo Optimization

To make the demo flow seamlessly with "Do the Last Thing First," the UI should be reorganized:

### Navigation Changes

**Current Navigation:**
```
Dashboard | Orders | Barrels | TTB Compliance | Products | Reports | Report Builder | Accounting | Sync History
```

**Proposed Demo-Optimized Navigation:**
```
Compliance Center | Production | Inventory | Analytics | Settings
       │                │            │           │           │
       │                │            │           │           └── Accounting, Sync History
       │                │            │           └── Reports, Report Builder
       │                │            └── Barrels, Products
       │                └── Dashboard, Orders
       └── TTB Reports, Transactions, Gauge Records, Auto-Report Preview
```

### Specific UI Changes Required

#### 1. Create "Compliance Center" as First Navigation Item
Move TTB Compliance to be the **first** item in navigation. This is the "Wow!" - it should be front and center.

#### 2. Add Compliance Status Banner to Dashboard
Add a prominent banner at the top of the Dashboard showing:
- Current month compliance status (Green/Yellow/Red)
- "TTB Report Ready" indicator when all transactions logged
- Quick link to "View Ready Report"

#### 3. Create "Demo Mode" Landing Page
A special landing page that shows:
- Completed TTB report preview (the end result)
- Key metrics (proof gallons tracked, transactions logged)
- "How We Got Here" expandable sections

#### 4. Reorganize TTB Compliance Page Tabs
Current: `Reports | Audit Trail`

Proposed: `Ready Reports | In Progress | Transactions | Gauge Records | Audit Trail`

Make "Ready Reports" the first and default tab - show completed work first.

#### 5. Add "Generate Now" Quick Action
Add a floating action button on the TTB page that shows:
- "Your October 2025 Report is Ready - Generate Now"
- One-click to completed PDF

---

## Demo Data Requirements

For an effective demo, pre-populate:

1. **3 months of completed TTB reports** (showing history)
2. **Current month with all transactions logged** (ready to generate)
3. **5-10 active orders** with varied progress percentages
4. **50+ barrels** across different ages
5. **Tasks at various stages** (assigned, in-progress, completed)
6. **QuickBooks connected** with sync history

---

## Anti-Patterns to Avoid

From "Great Demo!" - things that kill demos:

| Anti-Pattern | Why It's Bad | Instead Do |
|--------------|--------------|------------|
| Starting with login | Shows friction first | Start logged in, on the result |
| Showing data entry first | Boring, everyone has data entry | Show the outcome of data entry |
| Feature dump | Overwhelming, unfocused | Show only what solves THEIR problem |
| "Let me show you one more thing" | Dilutes the impact | Stop when you have buy-in |
| Technical jargon | Confuses, alienates | Use their language (barrels, not "inventory entities") |
| Apologizing for bugs/gaps | Undermines confidence | Focus on what works brilliantly |

---

## Tailoring the Demo

### For CFO/Finance Buyer
Emphasize:
- QuickBooks integration (Harbor Tour A becomes Act 2)
- Cost savings ($5K-$10K/month)
- Audit readiness
- Excise tax calculations

### For Operations Manager
Emphasize:
- Task management and assignment
- Production visibility
- Barrel forecasting
- Real-time progress tracking

### For Warehouse Staff
Emphasize:
- Simple task completion workflow
- Mobile-friendly interface (when available)
- Clear instructions per task
- Barcode scanning (when available)

---

## The 3-Minute Demo (Trade Show / Quick Pitch)

If you only have 3 minutes:

1. **30 sec**: "You spend [X] on TTB compliance. Let me show you your new reality..."
2. **60 sec**: Show completed TTB report, click PDF preview
3. **60 sec**: Show Dashboard with all orders tracked
4. **30 sec**: "One platform. Compliance automated. Production visible. Questions?"

---

## Post-Demo Actions

After the demo:
1. Send PDF of their would-be TTB report (mocked with their data if possible)
2. Share 2-minute video recap of key moments
3. Offer pilot program with real data import
4. Schedule technical deep-dive only if requested

---

## Summary: The Great Demo! Checklist

Before every demo:

- [ ] Know the prospect's specific pain (pre-call research)
- [ ] Situation slide prepared with THEIR numbers
- [ ] Demo environment has impressive completed data
- [ ] Start on the finished TTB report, not Dashboard
- [ ] Practice the 3-minute version
- [ ] Know which Harbor Tours might be relevant to THIS prospect
- [ ] Have "stop points" to ask for feedback
- [ ] Prepare for "Can you show me...?" requests
- [ ] End with clear next steps

---

*"The goal is not to show everything the software can do. The goal is to show the prospect their life is better with your software."* - Peter Cohan
