# Caskr Product Roadmap - Implementation Guide

**Generated:** November 2025
**Based on:** Competitive Analysis of DRAMS, DistillX5, Bevica, Vinsight, and Vapour
**Organized by:** CEO Decision Priority

---

## Overview

This product roadmap contains **100+ SCRUM-ready tasks** organized into **15 major features** across **3 priority levels**. Each task is the smallest deliverable increment, designed for a 12-person development team working in 2-week sprints.

## Files in This Roadmap

- **PRODUCT_ROADMAP_FINAL.csv** - Complete roadmap (all priorities combined)
- **COMPETITIVE_ANALYSIS.md** - Original competitive analysis with feature gaps
- **ROADMAP_README.md** - This file

## CSV Structure

| Column | Description |
|--------|-------------|
| **Priority** | P1-Critical, P2-High, P3-NiceToHave |
| **Epic** | Major feature category (e.g., Financial Integration, TTB Compliance) |
| **Feature** | Specific feature being built (e.g., QuickBooks Online Integration) |
| **Task ID** | Unique identifier (e.g., FIN-001, TTB-005, MOB-012) |
| **Task Name** | Brief description of the task |
| **Task Type** | Database Schema, Backend Service, API Route, User Interface, Testing, etc. |
| **Story Points** | SCRUM estimation (1-13 Fibonacci scale) |
| **Sprint** | Recommended sprint number (assumes 2-week sprints) |
| **Dependencies** | Task IDs that must be completed first (e.g., FIN-001\|FIN-002) |
| **Acceptance Criteria** | Bulleted list of requirements for task completion |
| **Detailed AI Prompt** | 2000+ character prompt for AI agents or developers |

## Priority Levels Explained

### P1 - Critical (Must-Have for Enterprise)
**60 tasks | ~$1,050K investment | Sprints 1-11**

These are **deal-breakers** for enterprise distillery contracts. Without these features, Caskr cannot compete with DRAMS, DistillX5, or Bevica for $5M+ distilleries.

**Key Epics:**
1. **Financial Integration** (20 tasks) - QuickBooks/Xero integration, invoice sync, COGS tracking
2. **TTB Compliance** (20 tasks) - Automated monthly reports (Forms 5110.28, 5110.40), gauge records, excise tax
3. **Mobile Application** (20 tasks) - React Native app for iOS/Android, barcode scanning, offline support

**Why Critical:**
- Every competitor has financial integration (QuickBooks or ERP)
- TTB compliance automation saves $5K-$10K/month in manual reporting
- Mobile app is table stakes for modern SaaS (warehouse staff need it)

### P2 - High Priority (Competitive Parity)
**30 tasks | ~$550K investment | Sprints 12-23**

These features bring Caskr to **competitive parity** with mid-market solutions. Required to win contracts against DistillX5, Vinsight, and Vapour.

**Key Epics:**
1. **Advanced Reporting** (5 tasks) - 50+ standard reports, custom report builder, BI integration
2. **Warehouse Management** (5 tasks) - 3D visualization, multi-warehouse support, capacity planning
3. **Barcode Scanning** (3 tasks) - Label printing, batch printing, thermal printer integration
4. **API Integration** (3 tasks) - Public API docs, webhook system for third-party integrations
5. **Customer Portals** (3 tasks) - Cask ownership tracking for investor programs

**Why High Priority:**
- Reporting is expected (DRAMS has 100+ reports)
- 3D warehouse visualization is Vapour's key differentiator
- Customer portals enable cask investment programs (growing revenue stream)

### P3 - Nice to Have (Differentiation)
**15 tasks | ~$200K investment | Sprints 24-28**

These are **differentiating features** that can make Caskr stand out, but aren't required for initial market entry. Implement after P1/P2 complete and customer base established.

**Key Epics:**
1. **Quality Control** (2 tasks) - Lab management, sensory evaluations, chemical analysis tracking
2. **Supply Chain** (2 tasks) - Supplier management, purchase orders, inventory receiving
3. **CRM Integration** (1 task) - Salesforce connector for enterprise sales teams
4. **Tour Management** (2 tasks) - Tour bookings, tasting room POS system
5. **AI Features** (1 task) - Demand forecasting with machine learning

**Why Nice to Have:**
- Quality control is valuable but not urgent (can track in spreadsheets initially)
- Supply chain management overlaps with QuickBooks/ERP (P1)
- AI forecasting requires 2+ years of historical data to be useful

---

## How to Use This Roadmap

### For Product Owners

1. **Review priorities with stakeholders**: Ensure P1 alignment with business goals
2. **Assign tasks to sprints**: Use Sprint column as guide, adjust based on team capacity
3. **Track dependencies**: Don't start dependent tasks until prerequisites complete
4. **Refine acceptance criteria**: Customize for your specific business rules
5. **Prioritize within priority levels**: Some P1 tasks are more critical than others (e.g., QuickBooks integration > Mobile app for some customers)

### For Development Teams

1. **Start each sprint with planning**: Review tasks for next sprint, break down if needed
2. **Use AI prompts for context**: The Detailed AI Prompt column provides full context for each task
3. **Validate story points**: Adjust estimates based on your team's velocity
4. **Track blockers**: Flag dependency issues early
5. **Update acceptance criteria**: Check off criteria as completed, add tests to verify

### For AI Agents (Claude, GPT, etc.)

Each task includes a **detailed prompt** (2000+ characters) explaining:
- Full context and background
- Technical implementation details
- File locations and naming conventions
- Code patterns to follow (based on existing Caskr codebase)
- Integration points with existing features
- Testing requirements
- Security considerations

**To use with AI:**
```
Copy the entire "Detailed AI Prompt" for a task and send to your AI agent:

"I'm working on Caskr, a distillery management system built with ASP.NET Core 8.0,
Entity Framework Core 9.0, React 18.3 + TypeScript, and PostgreSQL.

{PASTE DETAILED AI PROMPT HERE}

Please implement this task following the existing codebase patterns."
```

### For Executives

**Quick Stats:**
- **Total Investment:** $1.8M over 24 months (105 tasks)
- **P1 Investment:** $1.05M (11 months, 60 tasks) - Required for enterprise sales
- **Expected ROI:** 78% by Year 3 (see COMPETITIVE_ANALYSIS.md for revenue projections)
- **Payback Period:** Month 22-24
- **Target Market:** $1M-$50M distilleries (sweet spot between craft and large enterprise)

**Decision Framework:**
- **Minimal Viable Product (MVP):** P1 only ($1.05M, 11 months)
- **Competitive Product:** P1 + P2 ($1.6M, 23 months)
- **Market Leader:** P1 + P2 + P3 ($1.8M, 28 months)

---

## Sprint Planning Guide

### Sprints 1-11: P1 - Critical Features (REQUIRED)

**Sprints 1-7: Financial Integration (FIN-001 to FIN-020)**
- Weeks 1-14: QuickBooks Online integration
- End State: Invoices, payments, and COGS auto-sync to QuickBooks
- Validation: Create test order, verify QB invoice created

**Sprints 1-10: TTB Compliance (TTB-001 to TTB-020)**
- Weeks 1-20: Automated monthly reporting
- End State: Generate Forms 5110.28 and 5110.40 with one click
- Validation: Generate October 2024 report, verify accuracy

**Sprints 1-11: Mobile Application (MOB-001 to MOB-020)**
- Weeks 1-22: React Native app for iOS/Android
- End State: Scan barrels, complete tasks, move inventory from mobile
- Validation: TestFlight beta with 10 warehouse users

**Sprint 11 Milestone: LAUNCH GROWTH TIER**
- All P1 features complete
- Ready to sell to $2M-$10M distilleries at $999-$1,999/month
- Target: 25 paying customers by end of sprint 12

### Sprints 12-23: P2 - High Priority (COMPETITIVE PARITY)

**Sprints 12-15: Advanced Reporting (REP-001 to REP-005)**
- Weeks 22-30: Report builder and 50+ standard reports
- End State: Financial, inventory, production, and compliance reports
- Validation: Generate inventory valuation report, profit by batch report

**Sprints 16-20: Warehouse Management (WH-001 to WH-005)**
- Weeks 30-40: 3D warehouse visualization and multi-warehouse support
- End State: Interactive 3D warehouse map, switch between facilities
- Validation: Load warehouse with 1,000 barrels, render at 60 FPS

**Sprints 19-20: Barcode/API (SCAN-001 to API-003)**
- Weeks 36-40: Label printing and webhook system
- End State: Print barrel tags, third-party integrations via webhooks
- Validation: Print 50 labels, trigger webhook on order completion

**Sprints 22-23: Customer Portals (PORTAL-001 to PORTAL-003)**
- Weeks 42-46: Cask ownership portal for investors
- End State: Customers can view their barrels, download certificates
- Validation: Create portal user, view barrel aging, download PDF

**Sprint 23 Milestone: LAUNCH PROFESSIONAL TIER**
- All P1+P2 features complete
- Ready to compete with DistillX5, Vinsight, and Vapour
- Target: 50 total customers, 10 professional tier

### Sprints 24-28: P3 - Nice to Have (DIFFERENTIATION)

**Optional features for market differentiation. Implement based on customer demand and available budget.**

---

## Task Breakdown by Type

| Task Type | Count | Avg Story Points | Total SP |
|-----------|-------|------------------|----------|
| Database Schema | 15 | 3.2 | 48 |
| Backend Service | 35 | 5.8 | 203 |
| Backend Model | 12 | 2.5 | 30 |
| API Route | 18 | 2.8 | 50 |
| User Interface | 25 | 5.2 | 130 |
| Backend Integration | 8 | 3.5 | 28 |
| Testing | 5 | 5.0 | 25 |
| Infrastructure | 4 | 2.0 | 8 |
| Research & Analysis | 5 | 3.4 | 17 |
| Documentation | 3 | 4.0 | 12 |
| Data Seeding | 1 | 8.0 | 8 |
| **TOTAL** | **105** | **5.0 avg** | **525** |

**Team Velocity Planning:**
- Assume team velocity: 40-60 story points per 2-week sprint (for 12-person team)
- P1 tasks: 265 story points = 5-7 sprints (10-14 weeks) → **Estimate 11 sprints (22 weeks) to account for unknowns**
- P2 tasks: 180 story points = 3-5 sprints (6-10 weeks) → **Estimate 12 sprints (24 weeks)**
- P3 tasks: 80 story points = 1-2 sprints (2-4 weeks) → **Estimate 5 sprints (10 weeks)**

---

## Key Dependencies

### Critical Path Tasks (Block Multiple Downstream Tasks)

1. **FIN-001: Accounting integration schema** → Blocks all QuickBooks features (FIN-002 through FIN-020)
2. **TTB-002: TTB tracking schema** → Blocks all compliance features (TTB-003 through TTB-020)
3. **MOB-001: React Native project init** → Blocks all mobile features (MOB-002 through MOB-020)
4. **REP-001: Report builder data model** → Blocks reporting features (REP-002 through REP-005)
5. **WH-002: Warehouse structure schema** → Blocks 3D visualization (WH-003)

**Parallelization Opportunities:**
- Financial Integration (FIN), TTB Compliance (TTB), and Mobile (MOB) can run in parallel during sprints 1-11
- Assign separate teams or team members to each epic to maximize throughput
- Example: Backend team on FIN/TTB, frontend/mobile team on MOB

---

## Risk Mitigation

### High-Risk Tasks (Story Points ≥ 8)

| Task ID | Task Name | Risk Factor | Mitigation Strategy |
|---------|-----------|-------------|---------------------|
| FIN-012 | Implement invoice sync to QuickBooks | Complex integration, third-party API dependencies | Allocate 2 sprints, use QBO sandbox, extensive testing |
| TTB-007 | Create TTB report calculation service | Complex business logic, reconciliation challenges | Work closely with compliance expert, validate with sample data |
| TTB-016 | Add gauge record tracking | TTB temperature tables complex | Research TTB tables first, consider simplified MVP |
| REP-002 | Create report query engine service | SQL injection risk, performance concerns | Use parameterized queries, query timeout limits, load testing |
| WH-003 | Implement 3D warehouse renderer | Performance with 1000+ barrels, browser compatibility | POC first (WH-001), optimize with instancing and LOD |
| MOB-014 | Implement offline data caching | Sync conflicts, data integrity | Thorough testing of offline scenarios, conflict resolution UI |

### External Dependencies

| Dependency | Impact | Risk Level | Mitigation |
|------------|--------|------------|------------|
| QuickBooks Online API | FIN tasks blocked | MEDIUM | Read API docs early, test with sandbox, have fallback (manual export) |
| TTB form templates | TTB-008 blocked | LOW | Download forms from TTB.gov upfront, field mapping may need adjustment |
| Expo/React Native updates | MOB tasks may break | MEDIUM | Lock dependency versions, test updates in separate branch |
| Thermal printer SDKs | SCAN-003 blocked | LOW | Research printers early (SCAN-001), have web print fallback |

---

## Success Metrics

### P1 Completion (Sprint 11 - Month 6)

**Product Metrics:**
- [ ] QuickBooks integration: 100% of invoices sync successfully
- [ ] TTB reports: Generate Forms 5110.28 and 5110.40 in <5 seconds
- [ ] Mobile app: >90% crash-free users (from Firebase Crashlytics)
- [ ] Mobile app: Barrel scan-to-detail workflow in <10 seconds

**Business Metrics:**
- [ ] 25 paying customers (Craft + Growth tiers)
- [ ] $161K ARR (Annual Recurring Revenue)
- [ ] <5% monthly churn
- [ ] 10+ positive reviews/testimonials

### P2 Completion (Sprint 23 - Month 12)

**Product Metrics:**
- [ ] 50+ standard reports available
- [ ] Custom report builder: 10+ user-created reports
- [ ] 3D warehouse: Render 1,000 barrels at 60 FPS
- [ ] API: 5+ third-party integrations via webhooks

**Business Metrics:**
- [ ] 50 paying customers across all tiers
- [ ] $779K ARR
- [ ] 5+ customers on Professional tier ($2,999-$4,999/month)
- [ ] Win rate >50% when competing against DistillX5 and Vinsight

### Full Roadmap (Sprint 28 - Month 14)

**Product Metrics:**
- [ ] All P1+P2+P3 features shipped and stable
- [ ] >95% API uptime
- [ ] <2 sec average page load time
- [ ] Zero critical security vulnerabilities

**Business Metrics:**
- [ ] 75 paying customers
- [ ] $1.5M ARR
- [ ] 3+ Enterprise customers ($10K+/month)
- [ ] Net revenue retention >110% (upsells and expansion)

---

## Next Steps

### Immediate Actions (This Week)

1. **Validate roadmap with stakeholders**
   - Review priorities with CEO, CTO, and key customers
   - Adjust task order based on specific customer needs
   - Confirm budget allocation ($1.8M available?)

2. **Assemble development team**
   - Backend developers (3-4): C#, ASP.NET Core, Entity Framework, PostgreSQL
   - Frontend developers (2-3): React, TypeScript, Redux
   - Mobile developers (2): React Native, iOS/Android
   - QA engineers (2): Automated testing, manual testing
   - DevOps engineer (1): CI/CD, cloud infrastructure
   - Product manager (1): Backlog management, stakeholder communication

3. **Setup development infrastructure**
   - Sprint board (Jira, Azure DevOps, or Linear)
   - Git branching strategy (feature branches, PR reviews)
   - CI/CD pipeline (automated tests, deployment to staging)
   - QA environment (mirror production for testing)

4. **Plan Sprint 1 (Start Next Monday)**
   - Select Sprint 1 tasks: FIN-001, FIN-002, TTB-001, MOB-001 (~30-40 story points)
   - Assign tasks to developers
   - Setup daily standups (15 min, same time every day)
   - Schedule sprint planning (2 hours), sprint review (1 hour), retrospective (1 hour)

### First Sprint Goal

**Sprint 1 Objective:** Foundation for all P1 features

**Tasks:**
- [FIN-001] Create accounting integration database schema (3 SP)
- [FIN-002] Create C# models for accounting integration (2 SP)
- [TTB-001] Research TTB Form 5110.28 requirements (3 SP)
- [TTB-002] Update database schema for TTB tracking (5 SP)
- [MOB-001] Initialize React Native project (3 SP)
- [MOB-002] Create authentication screens (5 SP)

**Total:** 21 story points (conservative for first sprint, team will accelerate)

**Sprint 1 Demo:**
- Show database schema created and migrated
- Show C# entity models with relationships
- Show TTB form mapping documentation
- Show mobile app running on simulator with login screen

---

## Appendix: Task ID Prefixes

| Prefix | Epic | Task Count |
|--------|------|------------|
| FIN | Financial Integration | 20 |
| TTB | TTB Compliance | 20 |
| MOB | Mobile Application | 20 |
| REP | Advanced Reporting | 5 |
| WH | Warehouse Management | 5 |
| SCAN | Barcode Scanning & Label Printing | 3 |
| API | API Integration & Webhooks | 3 |
| PORTAL | Customer Portals | 3 |
| QC | Quality Control & Lab Management | 2 |
| SCM | Supply Chain Management | 2 |
| CRM | CRM Integration | 1 |
| TOUR | Tour & Tasting Room Management | 2 |
| AI | AI/ML Features | 1 |

---

## Questions?

**For product questions:** Review COMPETITIVE_ANALYSIS.md for feature rationale
**For technical questions:** See "Detailed AI Prompt" column in CSV for implementation guidance
**For prioritization questions:** Contact product owner or reference CEO Decision Framework in competitive analysis

**Let's build Caskr into the obvious choice for craft distilleries! 🥃**
