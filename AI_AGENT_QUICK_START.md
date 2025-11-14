# Using This Roadmap with AI Agents - Quick Start Guide

This roadmap is designed to be used with AI coding assistants (Claude, GPT-4, etc.). Each of the 86 tasks includes a **detailed 2000+ character prompt** that gives the AI full context.

---

## How to Use with Claude Code / Claude

### Step 1: Open the CSV

Open `PRODUCT_ROADMAP.csv` in Excel, Google Sheets, or a text editor.

### Step 2: Find Your Task

Navigate to the task you want to work on. For example, Sprint 1 Task: **FIN-001**.

### Step 3: Copy the Detailed Prompt

Copy the entire content from the **"Detailed AI Prompt"** column for that task.

### Step 4: Send to AI with Context

```
I'm working on Caskr, a distillery management system with the following tech stack:
- Backend: ASP.NET Core 8.0, Entity Framework Core 9.0.1, PostgreSQL
- Frontend: React 18.3 + TypeScript + Redux Toolkit
- Auth: Keycloak SSO with JWT
- Existing features: Order management, barrel tracking, batch management, task system

Current codebase structure:
- Caskr.Server/ (Backend - C# .NET)
  - Controllers/ (API endpoints)
  - Models/ (Entity models)
  - Services/ (Business logic)
  - Database/initdb.d/ (SQL migrations)
- caskr.client/ (Frontend - React + TypeScript)
  - src/components/ (Reusable components)
  - src/pages/ (Page components)
  - src/store/ (Redux slices)
  - src/services/ (API clients)

Task to implement:
{PASTE THE DETAILED AI PROMPT HERE}

Please implement this task following the existing codebase patterns. Show me the complete code for all files that need to be created or modified.
```

### Step 5: Review and Test

The AI will generate code following your existing patterns. Review the code, test it, and iterate if needed.

---

## Example: Implementing FIN-001

**Task:** Create accounting integration database schema

**Detailed Prompt from CSV:**
```
Create a database migration for QuickBooks Online integration. The Caskr application uses PostgreSQL with Entity Framework Core 9.0.1. Create SQL migration files in the Database/initdb.d/ directory following the existing pattern (using IF NOT EXISTS for idempotent migrations). Include three new tables: 1) accounting_integrations to store OAuth tokens and provider configuration (supports multiple providers like QuickBooks, Xero, NetSuite), 2) accounting_sync_logs to track all sync operations with status and error messages for audit trail, 3) chart_of_accounts_mapping to map Caskr internal account types (COGS, WIP, Finished Goods, etc.) to external accounting system account IDs. Ensure all sensitive fields like access_token and refresh_token have '_encrypted' suffix to indicate encryption requirement. Add appropriate indexes for company_id lookups and sync_status filtering. Include updated_at timestamps for change tracking.
```

**Send to Claude:**
```
I'm working on Caskr, a distillery management system (ASP.NET Core 8.0, EF Core 9.0.1, PostgreSQL).

Task: Create a database migration for QuickBooks Online integration. The Caskr application uses PostgreSQL with Entity Framework Core 9.0.1. Create SQL migration files in the Database/initdb.d/ directory following the existing pattern (using IF NOT EXISTS for idempotent migrations). Include three new tables: 1) accounting_integrations to store OAuth tokens and provider configuration (supports multiple providers like QuickBooks, Xero, NetSuite), 2) accounting_sync_logs to track all sync operations with status and error messages for audit trail, 3) chart_of_accounts_mapping to map Caskr internal account types (COGS, WIP, Finished Goods, etc.) to external accounting system account IDs. Ensure all sensitive fields like access_token and refresh_token have '_encrypted' suffix to indicate encryption requirement. Add appropriate indexes for company_id lookups and sync_status filtering. Include updated_at timestamps for change tracking.

Please create the SQL migration file: Database/initdb.d/07-migration-accounting-integration.sql
```

**Claude's Response:**
Claude will generate a complete SQL file with all three tables, indexes, and proper formatting.

---

## Tips for Best Results

### 1. Include Codebase Context

Always mention:
- Tech stack (ASP.NET Core, React, PostgreSQL)
- Current file being modified (if editing existing code)
- Related files (if task depends on other components)

### 2. Reference Existing Patterns

If the task says "follow existing patterns," you can help the AI by:
- Showing an example of an existing similar file
- Mentioning naming conventions used in your codebase
- Pointing to a similar feature already implemented

Example:
```
The existing LabelsService.cs uses iText7 for PDF generation.
Please follow the same pattern for TransfersService.cs.
Here's the LabelsService structure: [paste relevant code]
```

### 3. Break Down Large Tasks

If a task has 8+ story points, consider breaking it into smaller prompts:

**Large Task:** FIN-012 (Implement invoice sync to QuickBooks - 8 SP)

**Break into:**
1. "First, create the invoice sync service interface"
2. "Now implement the customer creation logic"
3. "Now implement the invoice creation logic"
4. "Now add error handling and retry logic"
5. "Now add logging"

### 4. Ask for Explanation

After getting code, ask:
```
Please explain how this implementation handles [specific concern]:
- Token refresh when expired
- Idempotent sync (preventing duplicates)
- Error handling for network failures
```

### 5. Request Tests

```
Now please create unit tests for this service:
- Test successful invoice sync
- Test with expired token (should refresh)
- Test with invalid customer (should create new)
- Test network failure (should retry)
```

---

## Multi-Agent Workflow (Advanced)

If you have access to multiple AI agents or Claude Code with parallel execution:

### Sprint Planning

**Agent 1 (Backend):** Implement FIN-001, FIN-002, FIN-003 (Database + Models + Package)
**Agent 2 (Backend):** Implement TTB-001, TTB-002, TTB-003 (Research + Database + Models)
**Agent 3 (Mobile):** Implement MOB-001, MOB-002 (React Native Init + Auth Screens)

All agents can work in parallel since these tasks don't depend on each other.

### Code Review Agent

After implementation:
```
Please review this implementation for:
1. Security vulnerabilities (SQL injection, XSS, token exposure)
2. Performance issues (N+1 queries, missing indexes)
3. Code quality (follows C# conventions, proper error handling)
4. Test coverage (are critical paths tested?)
```

---

## Common Patterns in This Roadmap

### Database Migrations

**Pattern:** All migrations use `IF NOT EXISTS` for idempotency

**Template:**
```sql
-- File: Database/initdb.d/##-migration-{feature-name}.sql

-- Create table only if it doesn't exist
CREATE TABLE IF NOT EXISTS {table_name} (
    id SERIAL PRIMARY KEY,
    company_id INTEGER NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    FOREIGN KEY (company_id) REFERENCES companies(id) ON DELETE CASCADE
);

-- Create index only if it doesn't exist
CREATE INDEX IF NOT EXISTS idx_{table}_{column} ON {table}({column});
```

### C# Entity Models

**Pattern:** Entity models match database tables with snake_case column names

**Template:**
```csharp
// File: Caskr.Server/Models/{EntityName}.cs

using System.ComponentModel.DataAnnotations;

namespace Caskr.server.Models;

public class {EntityName}
{
    public int Id { get; set; }

    [Required]
    public int CompanyId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Company Company { get; set; } = null!;
}

// In CaskrDbContext.cs:
public DbSet<{EntityName}> {EntityNames} { get; set; }

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<{EntityName}>(entity =>
    {
        entity.ToTable("{table_name}");
        entity.Property(e => e.CompanyId).HasColumnName("company_id");
        entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
    });
}
```

### API Controllers

**Pattern:** Controllers extend AuthorizedApiControllerBase and use dependency injection

**Template:**
```csharp
// File: Caskr.Server/Controllers/{Feature}Controller.cs

using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Caskr.server.Controllers;

public class {Feature}Controller(
    I{Feature}Service {feature}Service,
    ILogger<{Feature}Controller> logger) : AuthorizedApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int companyId)
    {
        try
        {
            var items = await {feature}Service.GetAllAsync(companyId);
            return Ok(items);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching {feature}s");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
```

### React Components

**Pattern:** Functional components with TypeScript, hooks, and Material-UI

**Template:**
```typescript
// File: caskr.client/src/components/{ComponentName}.tsx

import React, { useState, useEffect } from 'react';
import { useAppSelector, useAppDispatch } from '../hooks';
import { authorizedFetch } from '../api/authorizedFetch';

type Props = {
  isOpen: boolean;
  onClose: () => void;
  // ... other props
};

const {ComponentName} = ({ isOpen, onClose }: Props) => {
  const [data, setData] = useState<DataType[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (isOpen) {
      fetchData();
    }
  }, [isOpen]);

  const fetchData = async () => {
    setLoading(true);
    try {
      const response = await authorizedFetch('/api/endpoint');
      if (!response.ok) throw new Error('Failed to fetch');
      const result = await response.json();
      setData(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setLoading(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div className="modal-overlay">
      {/* Component content */}
    </div>
  );
};

export default {ComponentName};
```

---

## Troubleshooting

### "AI doesn't understand the codebase structure"

**Solution:** Provide more context. Show the AI an existing similar file:

```
Here's an existing service for reference:

[paste LabelsService.cs]

Please create TransfersService.cs following the same pattern.
```

### "AI generates code that doesn't match our style"

**Solution:** Be explicit about conventions:

```
Important conventions:
- Database columns use snake_case (created_at, not CreatedAt)
- C# properties use PascalCase (CreatedAt in model)
- Use HasColumnName("created_at") in OnModelCreating
- All API endpoints under AuthorizedApiControllerBase
- All error responses include { error: string, message: string }
```

### "AI generates insecure code"

**Solution:** Add security requirements to every prompt:

```
Security requirements:
- Use parameterized queries (no string concatenation in SQL)
- Validate user has access to company_id before any operation
- Encrypt sensitive fields (access_token, refresh_token) before database storage
- Add rate limiting to authentication endpoints
- Log all failed attempts
```

---

## Using with GitHub Copilot

If you use GitHub Copilot in VS Code:

1. **Open the task description** in a comment:
```csharp
// Task FIN-001: Create accounting integration database schema
// {paste detailed prompt here}

// Expected file: Database/initdb.d/07-migration-accounting-integration.sql
```

2. **Let Copilot suggest** the implementation based on the comment

3. **Review and accept** or modify suggestions

---

## Estimating Time

**Story Point to Time conversion** (approximate for experienced developers):

| Story Points | Time Estimate | Complexity |
|--------------|---------------|------------|
| 1-2 | 2-4 hours | Simple (CRUD, basic UI) |
| 3-5 | 1-2 days | Medium (integration, complex logic) |
| 8-13 | 3-5 days | Complex (new feature, research needed) |

**Team velocity:** With 12 developers, expect 40-60 story points per 2-week sprint.

---

## Success Metrics

**You're using this roadmap effectively if:**

✅ Each task takes ≤ estimated story points (in actual hours)
✅ AI-generated code requires minimal manual fixes (<30% editing)
✅ Tests pass on first run (or second run after minor fixes)
✅ Code reviews find no major issues
✅ Dependencies between tasks are respected (no blocked work)

---

## Resources

- **Caskr Tech Stack:** ASP.NET Core 8.0, EF Core 9.0.1, PostgreSQL, React 18.3, TypeScript
- **Existing Codebase:** Review existing files to understand patterns before asking AI
- **COMPETITIVE_ANALYSIS.md:** Context for why each feature exists
- **ROADMAP_README.md:** Full implementation guide for teams

---

**Ready to start? Pick a task from Sprint 1 and copy its Detailed AI Prompt!**

**Sprint 1 Tasks:**
- FIN-001: Create accounting integration database schema (3 SP)
- FIN-002: Create C# models for accounting integration (2 SP)
- TTB-001: Research TTB Form 5110.28 requirements (3 SP)
- TTB-002: Update database schema for TTB tracking (5 SP)
- MOB-001: Initialize React Native project (3 SP)
- MOB-002: Create authentication screens (5 SP)

**Total Sprint 1:** 21 story points (~1 week for experienced team)

---

Good luck building Caskr! 🥃
