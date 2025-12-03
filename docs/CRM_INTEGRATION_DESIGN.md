# Salesforce CRM Integration Design

## Overview

The Caskr Salesforce CRM Integration enables bidirectional synchronization between Caskr and Salesforce, allowing distilleries to maintain unified customer data, automate order creation from sales opportunities, and provide cask investors with portal access. This integration follows proven patterns established by the QuickBooks integration (FIN-001 through FIN-005).

### Business Value

- **Sales Team Efficiency**: Salesforce Opportunities automatically create Caskr orders when closed-won
- **Customer Data Consistency**: Single source of truth for customer information
- **Investor Portal Access**: Salesforce Contacts become Caskr portal users for cask ownership tracking
- **Reduced Manual Entry**: Eliminate duplicate data entry between systems

### Integration Scope

| Feature | Phase 1 | Phase 2 |
|---------|---------|---------|
| Salesforce Account → Caskr Customer | Sync | Bidirectional |
| Salesforce Opportunity → Caskr Order | Sync | Sync |
| Salesforce Contact → Caskr Portal User | Sync | Bidirectional |
| Caskr Order Status → Salesforce Opportunity | - | Sync |
| Caskr Customer Updates → Salesforce Account | - | Sync |

---

## Architecture

### Integration Pattern Decision

**Recommendation**: Start with **one-way sync (Salesforce → Caskr)** in Phase 1, then add bidirectional sync in Phase 2.

| Pattern | Pros | Cons | Use Case |
|---------|------|------|----------|
| **One-way (SF → Caskr)** | Simpler, lower risk, faster to implement | Manual updates in Caskr don't propagate | Phase 1: Sales-driven workflow |
| **One-way (Caskr → SF)** | Order fulfillment visible to sales | Requires Caskr as primary system | Fulfillment tracking |
| **Bidirectional** | Complete data synchronization | Complex conflict resolution, higher risk | Phase 2: Full integration |

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              SALESFORCE                                      │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐                          │
│  │   Account   │  │ Opportunity │  │   Contact   │                          │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘                          │
│         │                │                │                                  │
│         │    Outbound Message / Platform Events / Polling                   │
└─────────┼────────────────┼────────────────┼─────────────────────────────────┘
          │                │                │
          ▼                ▼                ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                        CASKR SALESFORCE CONNECTOR                            │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │                    SalesforceWebhookController                        │   │
│  │  POST /api/salesforce/webhook/account                                 │   │
│  │  POST /api/salesforce/webhook/opportunity                             │   │
│  │  POST /api/salesforce/webhook/contact                                 │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
│                                    │                                         │
│                                    ▼                                         │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │                     SalesforceSyncService                             │   │
│  │  - ValidatePayload()                                                  │   │
│  │  - MapAccountToCustomer()                                             │   │
│  │  - MapOpportunityToOrder()                                            │   │
│  │  - MapContactToPortalUser()                                           │   │
│  │  - HandleConflict()                                                   │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
│                                    │                                         │
│                                    ▼                                         │
│  ┌────────────────┐  ┌────────────────┐  ┌─────────────────────────────┐    │
│  │ CrmSyncLogs    │  │ CrmIntegration │  │  SalesforcePollingService   │    │
│  │ (Audit Trail)  │  │ (OAuth Tokens) │  │  (Fallback/Reconciliation)  │    │
│  └────────────────┘  └────────────────┘  └─────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────────────┘
          │                │                │
          ▼                ▼                ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                              CASKR DOMAIN                                    │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐                          │
│  │  Customer   │  │    Order    │  │ Portal User │                          │
│  └─────────────┘  └─────────────┘  └─────────────┘                          │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Data Mapping

### Salesforce Account → Caskr Customer

| Salesforce Field | Caskr Field | Mapping Rule | Notes |
|------------------|-------------|--------------|-------|
| `Id` | `salesforce_account_id` | Direct | External ID for linking |
| `Name` | `customer_name` | Direct | Required |
| `BillingStreet` | `address_line1` | Direct | |
| `BillingCity` | `city` | Direct | |
| `BillingState` | `state` | Direct | |
| `BillingPostalCode` | `postal_code` | Direct | |
| `BillingCountry` | `country` | Direct, default "USA" | |
| `Phone` | `phone` | Direct | |
| `Website` | `website` | Direct | |
| `Industry` | `customer_type` | Mapped | See industry mapping table |
| `OwnerId` | `assigned_user_id` | Lookup by email | Salesforce user → Caskr user |
| `CreatedDate` | `created_at` | Direct | |
| `LastModifiedDate` | `updated_at` | Direct | Used for conflict resolution |

**Industry Mapping**:
| Salesforce Industry | Caskr Customer Type |
|--------------------|--------------------|
| Restaurants | On-Premise |
| Hospitality | On-Premise |
| Retail | Off-Premise |
| Wholesale | Distributor |
| *Other* | Direct |

### Salesforce Opportunity → Caskr Order

| Salesforce Field | Caskr Field | Mapping Rule | Notes |
|------------------|-------------|--------------|-------|
| `Id` | `salesforce_opportunity_id` | Direct | External ID |
| `AccountId` | `customer_id` | Lookup | Find/create customer first |
| `Name` | `order_notes` | Direct | |
| `Amount` | `total_amount` | Direct | |
| `CloseDate` | `order_date` | Direct | |
| `StageName` | `status` | Mapped | See stage mapping |
| `Description` | `order_notes` | Append | |
| `LastModifiedDate` | `updated_at` | Direct | Conflict resolution |

**Stage Mapping**:
| Salesforce Stage | Caskr Status | Trigger Action |
|------------------|--------------|----------------|
| Closed Won | Confirmed | Create Order |
| Closed Lost | *No sync* | Ignore |
| Negotiation | *No sync* | Ignore |
| Prospecting | *No sync* | Ignore |

**Order Line Items** (from OpportunityLineItem):
| Salesforce Field | Caskr Field | Mapping Rule |
|------------------|-------------|--------------|
| `ProductCode` | `product_sku` | Lookup by SKU |
| `Quantity` | `quantity` | Direct |
| `UnitPrice` | `unit_price` | Direct |
| `TotalPrice` | `line_total` | Calculated |

### Salesforce Contact → Caskr Portal User

| Salesforce Field | Caskr Field | Mapping Rule | Notes |
|------------------|-------------|--------------|-------|
| `Id` | `salesforce_contact_id` | Direct | External ID |
| `Email` | `email` | Direct | Required, unique |
| `FirstName` | `first_name` | Direct | |
| `LastName` | `last_name` | Direct | |
| `Phone` | `phone` | Direct | |
| `AccountId` | `linked_customer_id` | Lookup | Customer association |
| `Cask_Investor__c` | `is_cask_investor` | Direct | Custom field |
| `Portal_Access__c` | `portal_enabled` | Direct | Custom field |

---

## Sync Methods

### 1. Webhook (Real-Time) - Primary Method

Salesforce Outbound Messages or Platform Events trigger Caskr webhooks for real-time sync.

**Advantages**:
- Immediate synchronization
- No polling overhead
- Event-driven architecture

**Configuration Required in Salesforce**:
1. Create Outbound Message for Account, Opportunity, Contact
2. Configure endpoint: `https://api.caskr.com/api/salesforce/webhook/{entity}`
3. Include session ID for callback queries

**Webhook Payload Example**:
```json
{
  "event_type": "opportunity.closed_won",
  "organization_id": "00D5f000000XXXX",
  "timestamp": "2024-11-15T14:30:00Z",
  "data": {
    "Id": "0065f00000XXXXXX",
    "AccountId": "0015f00000YYYYYY",
    "Name": "Bourbon Private Label - Q4 2024",
    "Amount": 25000.00,
    "StageName": "Closed Won",
    "CloseDate": "2024-11-15"
  }
}
```

### 2. Polling (Fallback) - Secondary Method

Scheduled background job queries Salesforce for updated records.

**Frequency**:
| Sync Type | Frequency | Use Case |
|-----------|-----------|----------|
| Critical Events | Webhooks (real-time) | Opportunity closed |
| Account Updates | Every 15 minutes | Customer data changes |
| Full Reconciliation | Daily (2 AM) | Catch missed changes |

**Polling Query (SOQL)**:
```sql
SELECT Id, Name, BillingStreet, BillingCity, BillingState,
       BillingPostalCode, Phone, Industry, LastModifiedDate
FROM Account
WHERE LastModifiedDate > :lastSyncTimestamp
  AND IsDeleted = false
ORDER BY LastModifiedDate ASC
LIMIT 200
```

### 3. Sync Service Architecture

```csharp
// Interface definition following QuickBooks pattern
public interface ISalesforceSyncService
{
    Task<SyncResult> SyncAccountAsync(int companyId, SalesforceAccount account);
    Task<SyncResult> SyncOpportunityAsync(int companyId, SalesforceOpportunity opportunity);
    Task<SyncResult> SyncContactAsync(int companyId, SalesforceContact contact);
    Task<IEnumerable<SyncResult>> RunFullReconciliationAsync(int companyId);
}
```

---

## Sync Frequency Configuration

### Default Sync Schedule

| Entity | Webhook | Polling | Reconciliation |
|--------|---------|---------|----------------|
| Account | On update | 15 min | Daily |
| Opportunity (Closed) | On stage change | 15 min | Daily |
| Contact | On update | 1 hour | Daily |

### Configurable via crm_sync_preferences Table

```sql
INSERT INTO crm_sync_preferences (company_id, provider, entity_type, sync_method, polling_interval, is_active)
VALUES
    (1, 'Salesforce', 'Account', 'Webhook', NULL, true),
    (1, 'Salesforce', 'Account', 'Polling', '15 minutes', true),
    (1, 'Salesforce', 'Opportunity', 'Webhook', NULL, true),
    (1, 'Salesforce', 'Contact', 'Polling', '1 hour', true);
```

---

## Conflict Resolution Strategy

### Last-Write-Wins (Default)

For most fields, the system with the most recent `LastModifiedDate` wins.

```csharp
public class ConflictResolver
{
    public T ResolveConflict<T>(T caskrRecord, T salesforceRecord)
        where T : ISyncableEntity
    {
        if (salesforceRecord.LastModifiedDate > caskrRecord.UpdatedAt)
            return salesforceRecord;
        return caskrRecord;
    }
}
```

### Critical Field Manual Resolution

For fields marked as "critical", conflicts require manual resolution:

| Critical Fields | Resolution Method |
|-----------------|-------------------|
| `customer_name` | Manual review queue |
| `total_amount` (Order) | Manual review queue |
| `email` (Portal User) | Manual review queue |

**Manual Conflict UI**:
- Queue pending conflicts in `crm_sync_conflicts` table
- Admin dashboard shows side-by-side comparison
- User selects winning value or merges

### Conflict Detection Query

```sql
SELECT c.*, sf.last_modified_date as sf_updated
FROM customers c
JOIN crm_sync_mappings m ON c.id = m.caskr_entity_id
WHERE c.updated_at > m.last_sync_at
  AND m.salesforce_last_modified > m.last_sync_at
  AND m.entity_type = 'Account';
```

---

## Error Handling

### Retry Policy

Following the QuickBooks pattern (QuickBooksConstants.cs):

```csharp
public static class SalesforceConstants
{
    public static class RetryPolicy
    {
        public const int MaxRetryCount = 3;
        public static readonly TimeSpan InitialRetryDelay = TimeSpan.FromSeconds(2);
        public const double BackoffMultiplier = 2.0;
        // Delays: 2s, 4s, 8s
    }
}
```

### Error Categories

| Error Type | Retry | Action |
|------------|-------|--------|
| Network timeout | Yes (3x) | Exponential backoff |
| 401 Unauthorized | Yes (1x) | Refresh OAuth token |
| 403 Forbidden | No | Log, alert admin |
| 404 Not Found | No | Mark as deleted in Caskr |
| 429 Rate Limited | Yes | Wait for rate limit reset |
| 500 Server Error | Yes (3x) | Exponential backoff |
| Validation Error | No | Log, queue for review |

### Sync Log Entry

Every sync operation is logged to `crm_sync_logs`:

```sql
INSERT INTO crm_sync_logs (
    company_id, provider, entity_type, entity_id,
    salesforce_id, sync_direction, sync_status,
    error_message, retry_count, synced_at
) VALUES (
    1, 'Salesforce', 'Account', '123',
    '0015f00000YYYYYY', 'Inbound', 'Success',
    NULL, 0, NOW()
);
```

### Alert Thresholds

| Condition | Alert Level | Action |
|-----------|-------------|--------|
| 3 consecutive failures | Warning | Email admin |
| 10 failures in 1 hour | Critical | Email + Slack |
| OAuth token expired | Critical | Email admin |
| Full reconciliation mismatch > 5% | Warning | Review queue |

---

## Security

### OAuth 2.0 Authentication

Following the QuickBooks OAuth pattern:

```
┌───────────────────────────────────────────────────────────────────┐
│                    SALESFORCE OAUTH 2.0 FLOW                       │
├───────────────────────────────────────────────────────────────────┤
│                                                                    │
│  1. User clicks "Connect Salesforce"                              │
│     └─► Caskr redirects to Salesforce login                       │
│                                                                    │
│  2. User authorizes Caskr application                             │
│     └─► Salesforce redirects to Caskr callback with auth code     │
│                                                                    │
│  3. Caskr exchanges auth code for tokens                          │
│     └─► Store encrypted access_token and refresh_token            │
│                                                                    │
│  4. API calls include Bearer token                                │
│     └─► Refresh token when access_token expires                   │
│                                                                    │
└───────────────────────────────────────────────────────────────────┘
```

### Token Storage

Tokens encrypted using ASP.NET Core Data Protection (same as QuickBooks):

```csharp
public class SalesforceAuthService : ISalesforceAuthService
{
    private readonly IDataProtector _tokenProtector;

    public SalesforceAuthService(IDataProtectionProvider dataProtectionProvider)
    {
        _tokenProtector = dataProtectionProvider.CreateProtector(
            SalesforceConstants.OAuth.TokenProtectorPurpose);
    }

    public string EncryptToken(string token) => _tokenProtector.Protect(token);
    public string DecryptToken(string encryptedToken) => _tokenProtector.Unprotect(encryptedToken);
}
```

### Webhook Security

**HMAC Signature Verification** (for inbound webhooks from Salesforce):

```csharp
public bool VerifySalesforceSignature(string payload, string signature, string secret)
{
    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
    var expectedSignature = Convert.ToBase64String(
        hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
    return CryptographicOperations.FixedTimeEquals(
        Encoding.UTF8.GetBytes(signature),
        Encoding.UTF8.GetBytes(expectedSignature));
}
```

---

## Database Schema

### Migration: 16-migration-crm-integrations.sql

```sql
--
-- Migration: Salesforce CRM integration tables
-- Date: 2025-02-20
-- Task: CRM-001
-- Description: Adds crm_integrations, crm_sync_logs, crm_field_mappings,
--              crm_sync_preferences, and crm_sync_conflicts tables
--

-- Enum for sync direction
CREATE TYPE crm_sync_direction AS ENUM ('Inbound', 'Outbound', 'Bidirectional');

-- Enum for sync status
CREATE TYPE crm_sync_status AS ENUM ('Pending', 'InProgress', 'Success', 'Failed', 'Conflict');

-- Enum for conflict resolution status
CREATE TYPE crm_conflict_status AS ENUM ('Pending', 'Resolved_Caskr', 'Resolved_Salesforce', 'Merged');

-- Main integration configuration table (similar to accounting_integrations)
CREATE TABLE IF NOT EXISTS public.crm_integrations (
    id BIGSERIAL PRIMARY KEY,
    company_id INTEGER NOT NULL,
    provider TEXT NOT NULL DEFAULT 'Salesforce',
    instance_url TEXT,                          -- Salesforce instance URL
    access_token_encrypted TEXT,
    refresh_token_encrypted TEXT,
    organization_id TEXT,                       -- Salesforce Org ID
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    connected_by_user_id INTEGER,
    connected_at TIMESTAMPTZ,
    last_sync_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_crm_integrations_company
        FOREIGN KEY (company_id) REFERENCES public.company(id),
    CONSTRAINT fk_crm_integrations_connected_by
        FOREIGN KEY (connected_by_user_id) REFERENCES public.users(id),
    CONSTRAINT uq_crm_integrations_company_provider
        UNIQUE (company_id, provider)
);

CREATE INDEX IF NOT EXISTS idx_crm_integrations_company_id
    ON public.crm_integrations(company_id);

-- Sync logs for audit trail (similar to accounting_sync_logs)
CREATE TABLE IF NOT EXISTS public.crm_sync_logs (
    id BIGSERIAL PRIMARY KEY,
    company_id INTEGER NOT NULL,
    provider TEXT NOT NULL DEFAULT 'Salesforce',
    entity_type TEXT NOT NULL,                  -- Account, Opportunity, Contact
    caskr_entity_id TEXT,                       -- ID in Caskr (customers.id, orders.id)
    salesforce_id TEXT,                         -- Salesforce record ID
    sync_direction crm_sync_direction NOT NULL,
    sync_status crm_sync_status NOT NULL,
    error_message TEXT,
    error_code TEXT,
    retry_count INTEGER NOT NULL DEFAULT 0,
    request_payload JSONB,
    response_payload JSONB,
    synced_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_crm_sync_logs_company
        FOREIGN KEY (company_id) REFERENCES public.company(id)
);

CREATE INDEX IF NOT EXISTS idx_crm_sync_logs_company_id
    ON public.crm_sync_logs(company_id);
CREATE INDEX IF NOT EXISTS idx_crm_sync_logs_sync_status
    ON public.crm_sync_logs(sync_status);
CREATE INDEX IF NOT EXISTS idx_crm_sync_logs_synced_at
    ON public.crm_sync_logs(synced_at);
CREATE INDEX IF NOT EXISTS idx_crm_sync_logs_entity_type
    ON public.crm_sync_logs(entity_type);
CREATE INDEX IF NOT EXISTS idx_crm_sync_logs_salesforce_id
    ON public.crm_sync_logs(salesforce_id);

-- Entity mapping table for tracking Caskr ↔ Salesforce relationships
CREATE TABLE IF NOT EXISTS public.crm_entity_mappings (
    id BIGSERIAL PRIMARY KEY,
    company_id INTEGER NOT NULL,
    provider TEXT NOT NULL DEFAULT 'Salesforce',
    entity_type TEXT NOT NULL,                  -- Account, Opportunity, Contact
    caskr_entity_id TEXT NOT NULL,              -- ID in Caskr
    salesforce_id TEXT NOT NULL,                -- Salesforce record ID
    last_sync_at TIMESTAMPTZ,
    caskr_last_modified TIMESTAMPTZ,
    salesforce_last_modified TIMESTAMPTZ,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_crm_entity_mappings_company
        FOREIGN KEY (company_id) REFERENCES public.company(id),
    CONSTRAINT uq_crm_entity_mappings_caskr
        UNIQUE (company_id, provider, entity_type, caskr_entity_id),
    CONSTRAINT uq_crm_entity_mappings_salesforce
        UNIQUE (company_id, provider, entity_type, salesforce_id)
);

CREATE INDEX IF NOT EXISTS idx_crm_entity_mappings_company_id
    ON public.crm_entity_mappings(company_id);
CREATE INDEX IF NOT EXISTS idx_crm_entity_mappings_salesforce_id
    ON public.crm_entity_mappings(salesforce_id);

-- Customizable field mappings per company
CREATE TABLE IF NOT EXISTS public.crm_field_mappings (
    id BIGSERIAL PRIMARY KEY,
    company_id INTEGER NOT NULL,
    provider TEXT NOT NULL DEFAULT 'Salesforce',
    entity_type TEXT NOT NULL,                  -- Account, Opportunity, Contact
    salesforce_field TEXT NOT NULL,             -- Salesforce API field name
    caskr_field TEXT NOT NULL,                  -- Caskr entity field name
    transformation_rule TEXT,                   -- Optional transformation (e.g., 'UPPERCASE', 'DATE_FORMAT')
    is_required BOOLEAN NOT NULL DEFAULT FALSE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_crm_field_mappings_company
        FOREIGN KEY (company_id) REFERENCES public.company(id),
    CONSTRAINT uq_crm_field_mappings
        UNIQUE (company_id, provider, entity_type, salesforce_field)
);

CREATE INDEX IF NOT EXISTS idx_crm_field_mappings_company_id
    ON public.crm_field_mappings(company_id);

-- Sync preferences per entity type
CREATE TABLE IF NOT EXISTS public.crm_sync_preferences (
    id BIGSERIAL PRIMARY KEY,
    company_id INTEGER NOT NULL,
    provider TEXT NOT NULL DEFAULT 'Salesforce',
    entity_type TEXT NOT NULL,                  -- Account, Opportunity, Contact
    sync_direction crm_sync_direction NOT NULL DEFAULT 'Inbound',
    webhook_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    polling_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    polling_interval_minutes INTEGER NOT NULL DEFAULT 15,
    auto_create_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    auto_update_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    conflict_resolution TEXT NOT NULL DEFAULT 'LastWriteWins',
    last_polling_at TIMESTAMPTZ,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_crm_sync_preferences_company
        FOREIGN KEY (company_id) REFERENCES public.company(id),
    CONSTRAINT uq_crm_sync_preferences
        UNIQUE (company_id, provider, entity_type)
);

CREATE INDEX IF NOT EXISTS idx_crm_sync_preferences_company_id
    ON public.crm_sync_preferences(company_id);

-- Conflict tracking for manual resolution
CREATE TABLE IF NOT EXISTS public.crm_sync_conflicts (
    id BIGSERIAL PRIMARY KEY,
    company_id INTEGER NOT NULL,
    provider TEXT NOT NULL DEFAULT 'Salesforce',
    entity_type TEXT NOT NULL,
    caskr_entity_id TEXT NOT NULL,
    salesforce_id TEXT NOT NULL,
    field_name TEXT NOT NULL,
    caskr_value TEXT,
    salesforce_value TEXT,
    caskr_modified_at TIMESTAMPTZ,
    salesforce_modified_at TIMESTAMPTZ,
    resolution_status crm_conflict_status NOT NULL DEFAULT 'Pending',
    resolved_value TEXT,
    resolved_by_user_id INTEGER,
    resolved_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_crm_sync_conflicts_company
        FOREIGN KEY (company_id) REFERENCES public.company(id),
    CONSTRAINT fk_crm_sync_conflicts_resolved_by
        FOREIGN KEY (resolved_by_user_id) REFERENCES public.users(id)
);

CREATE INDEX IF NOT EXISTS idx_crm_sync_conflicts_company_id
    ON public.crm_sync_conflicts(company_id);
CREATE INDEX IF NOT EXISTS idx_crm_sync_conflicts_status
    ON public.crm_sync_conflicts(resolution_status);

-- Add Salesforce ID columns to existing entities
ALTER TABLE public.customers
    ADD COLUMN IF NOT EXISTS salesforce_account_id TEXT,
    ADD COLUMN IF NOT EXISTS salesforce_last_sync_at TIMESTAMPTZ;

CREATE INDEX IF NOT EXISTS idx_customers_salesforce_account_id
    ON public.customers(salesforce_account_id) WHERE salesforce_account_id IS NOT NULL;

ALTER TABLE public.orders
    ADD COLUMN IF NOT EXISTS salesforce_opportunity_id TEXT,
    ADD COLUMN IF NOT EXISTS salesforce_last_sync_at TIMESTAMPTZ;

CREATE INDEX IF NOT EXISTS idx_orders_salesforce_opportunity_id
    ON public.orders(salesforce_opportunity_id) WHERE salesforce_opportunity_id IS NOT NULL;

ALTER TABLE public.portal_users
    ADD COLUMN IF NOT EXISTS salesforce_contact_id TEXT,
    ADD COLUMN IF NOT EXISTS salesforce_last_sync_at TIMESTAMPTZ;

CREATE INDEX IF NOT EXISTS idx_portal_users_salesforce_contact_id
    ON public.portal_users(salesforce_contact_id) WHERE salesforce_contact_id IS NOT NULL;

-- Seed default field mappings for standard Salesforce → Caskr mapping
INSERT INTO public.crm_field_mappings (company_id, provider, entity_type, salesforce_field, caskr_field, is_required)
SELECT
    c.id,
    'Salesforce',
    mapping.entity_type,
    mapping.sf_field,
    mapping.caskr_field,
    mapping.is_required
FROM public.company c
CROSS JOIN (VALUES
    -- Account → Customer mappings
    ('Account', 'Id', 'salesforce_account_id', true),
    ('Account', 'Name', 'customer_name', true),
    ('Account', 'BillingStreet', 'address_line1', false),
    ('Account', 'BillingCity', 'city', false),
    ('Account', 'BillingState', 'state', false),
    ('Account', 'BillingPostalCode', 'postal_code', false),
    ('Account', 'BillingCountry', 'country', false),
    ('Account', 'Phone', 'phone', false),
    ('Account', 'Website', 'website', false),
    -- Opportunity → Order mappings
    ('Opportunity', 'Id', 'salesforce_opportunity_id', true),
    ('Opportunity', 'AccountId', 'customer_id', true),
    ('Opportunity', 'Amount', 'total_amount', false),
    ('Opportunity', 'CloseDate', 'order_date', true),
    ('Opportunity', 'Name', 'order_notes', false),
    -- Contact → Portal User mappings
    ('Contact', 'Id', 'salesforce_contact_id', true),
    ('Contact', 'Email', 'email', true),
    ('Contact', 'FirstName', 'first_name', false),
    ('Contact', 'LastName', 'last_name', true),
    ('Contact', 'Phone', 'phone', false)
) AS mapping(entity_type, sf_field, caskr_field, is_required)
ON CONFLICT DO NOTHING;
```

---

## Sequence Diagrams

### Opportunity Closed Won → Order Creation

```
┌─────────┐          ┌───────────┐          ┌─────────────────┐          ┌──────────┐
│Salesforce│          │  Caskr    │          │SalesforceSyncSvc│          │ Database │
│   User  │          │ Webhook   │          │                 │          │          │
└────┬────┘          └─────┬─────┘          └────────┬────────┘          └────┬─────┘
     │                     │                         │                        │
     │ Close Opportunity   │                         │                        │
     │ (Stage=Closed Won)  │                         │                        │
     ├────────────────────►│                         │                        │
     │                     │                         │                        │
     │                     │  POST /webhook/opportunity                       │
     │                     │  {event: closed_won}    │                        │
     │                     ├────────────────────────►│                        │
     │                     │                         │                        │
     │                     │                         │  Lookup Customer by    │
     │                     │                         │  AccountId             │
     │                     │                         ├───────────────────────►│
     │                     │                         │                        │
     │                     │                         │  Customer (or create)  │
     │                     │                         │◄───────────────────────┤
     │                     │                         │                        │
     │                     │                         │  Create Order          │
     │                     │                         ├───────────────────────►│
     │                     │                         │                        │
     │                     │                         │  Order Created         │
     │                     │                         │◄───────────────────────┤
     │                     │                         │                        │
     │                     │                         │  Log Sync (Success)    │
     │                     │                         ├───────────────────────►│
     │                     │                         │                        │
     │                     │  200 OK                 │                        │
     │                     │◄────────────────────────┤                        │
     │                     │                         │                        │
```

### OAuth 2.0 Connection Flow

```
┌─────────┐     ┌──────────────┐     ┌───────────────────┐     ┌───────────┐
│  Admin  │     │    Caskr     │     │SalesforceAuthSvc  │     │ Salesforce│
└────┬────┘     └──────┬───────┘     └─────────┬─────────┘     └─────┬─────┘
     │                 │                       │                     │
     │ Click "Connect" │                       │                     │
     ├────────────────►│                       │                     │
     │                 │                       │                     │
     │                 │ Get Auth URL          │                     │
     │                 ├──────────────────────►│                     │
     │                 │                       │                     │
     │                 │ Redirect URL          │                     │
     │                 │◄──────────────────────┤                     │
     │                 │                       │                     │
     │ Redirect to Salesforce Login            │                     │
     │◄────────────────┤                       │                     │
     │                 │                       │                     │
     │ Login + Authorize                       │                     │
     ├─────────────────┼───────────────────────┼────────────────────►│
     │                 │                       │                     │
     │ Redirect with auth_code                 │                     │
     │◄────────────────┼───────────────────────┼─────────────────────┤
     │                 │                       │                     │
     │ Callback with code                      │                     │
     ├────────────────►│                       │                     │
     │                 │                       │                     │
     │                 │ Exchange code for tokens                    │
     │                 ├──────────────────────►│                     │
     │                 │                       │                     │
     │                 │                       │ Token Request       │
     │                 │                       ├────────────────────►│
     │                 │                       │                     │
     │                 │                       │ Access + Refresh    │
     │                 │                       │◄────────────────────┤
     │                 │                       │                     │
     │                 │                       │ Encrypt & Store     │
     │                 │                       ├──────────────────►DB│
     │                 │                       │                     │
     │                 │ Connection Success    │                     │
     │                 │◄──────────────────────┤                     │
     │                 │                       │                     │
     │ "Connected!"    │                       │                     │
     │◄────────────────┤                       │                     │
```

---

## Service Implementation

### Constants Class

```csharp
// File: Caskr.Server/Services/SalesforceConstants.cs
namespace Caskr.Server.Services;

public static class SalesforceConstants
{
    public static class EntityTypes
    {
        public const string Account = "Account";
        public const string Opportunity = "Opportunity";
        public const string Contact = "Contact";
    }

    public static class RetryPolicy
    {
        public const int MaxRetryCount = 3;
        public static readonly TimeSpan InitialRetryDelay = TimeSpan.FromSeconds(2);
        public const double BackoffMultiplier = 2.0;
    }

    public static class CacheConfiguration
    {
        public static readonly TimeSpan FieldMappingsCacheDuration = TimeSpan.FromHours(1);
        public const string FieldMappingsCacheKeyPrefix = "Salesforce.FieldMappings";
    }

    public static class SyncConfiguration
    {
        public static readonly TimeSpan DefaultPollingInterval = TimeSpan.FromMinutes(15);
        public static readonly TimeSpan ReconciliationInterval = TimeSpan.FromHours(24);
        public const int MaxRecordsPerPollingBatch = 200;
    }

    public static class OAuth
    {
        public const string TokenProtectorPurpose = "Caskr.Server.Services.SalesforceAuthService.Tokens";
        public static readonly TimeSpan TokenExpirySkew = TimeSpan.FromMinutes(5);
        public const string AuthorizationEndpoint = "https://login.salesforce.com/services/oauth2/authorize";
        public const string TokenEndpoint = "https://login.salesforce.com/services/oauth2/token";
        public const string SandboxAuthorizationEndpoint = "https://test.salesforce.com/services/oauth2/authorize";
        public const string SandboxTokenEndpoint = "https://test.salesforce.com/services/oauth2/token";
    }

    public static class ConfigurationKeys
    {
        public const string ClientId = "Salesforce:ClientId";
        public const string ClientSecret = "Salesforce:ClientSecret";
        public const string RedirectUri = "Salesforce:RedirectUri";
        public const string ConnectSuccessRedirectUrl = "Salesforce:ConnectSuccessRedirectUrl";
        public const string Environment = "Salesforce:Environment";
        public const string DefaultEnvironment = "sandbox";
    }

    public static class OpportunityStages
    {
        public const string ClosedWon = "Closed Won";
        public const string ClosedLost = "Closed Lost";
    }

    public static class ConflictResolution
    {
        public const string LastWriteWins = "LastWriteWins";
        public const string CaskrWins = "CaskrWins";
        public const string SalesforceWins = "SalesforceWins";
        public const string Manual = "Manual";
    }
}
```

---

## Testing Strategy

### Unit Tests

| Test Class | Coverage |
|------------|----------|
| `SalesforceAuthServiceTests` | OAuth token exchange, refresh, encryption |
| `SalesforceSyncServiceTests` | Entity mapping, conflict resolution |
| `SalesforcePollingServiceTests` | SOQL query generation, batch processing |
| `SalesforceWebhookControllerTests` | Payload validation, signature verification |

### Integration Tests

| Test Scenario | Validation |
|---------------|------------|
| Account sync (create) | New customer created in Caskr |
| Account sync (update) | Existing customer updated |
| Opportunity closed-won | Order created with line items |
| Contact sync | Portal user created with access |
| Token refresh | Expired token refreshed automatically |
| Webhook signature | Invalid signature rejected |

### Test Data Seeding

```csharp
// Test helper for seeding Salesforce test data
public static class SalesforceTestData
{
    public static SalesforceAccount CreateTestAccount() => new()
    {
        Id = "0015f00000TESTXX",
        Name = "Test Distillery Co",
        BillingStreet = "123 Barrel Lane",
        BillingCity = "Louisville",
        BillingState = "KY",
        BillingPostalCode = "40202",
        Phone = "502-555-1234",
        Industry = "Wholesale",
        LastModifiedDate = DateTime.UtcNow
    };
}
```

---

## Implementation Phases

### Phase 1: Foundation (CRM-001)
- [ ] Database migration for CRM tables
- [ ] SalesforceConstants class
- [ ] SalesforceAuthService (OAuth 2.0)
- [ ] Basic Account → Customer sync
- [ ] CRM sync logging
- [ ] Unit tests

### Phase 2: Opportunity Sync (CRM-002)
- [ ] Opportunity → Order mapping
- [ ] Webhook endpoint for closed-won
- [ ] Order line item sync
- [ ] Integration tests

### Phase 3: Portal Integration (CRM-003)
- [ ] Contact → Portal User sync
- [ ] Cask investor flag handling
- [ ] Portal access provisioning

### Phase 4: Bidirectional Sync (CRM-004)
- [ ] Caskr → Salesforce outbound sync
- [ ] Conflict detection and resolution
- [ ] Manual resolution UI
- [ ] Full reconciliation job

---

## Configuration

### appsettings.json

```json
{
  "Salesforce": {
    "ClientId": "your-connected-app-client-id",
    "ClientSecret": "your-connected-app-client-secret",
    "RedirectUri": "https://api.caskr.com/api/salesforce/oauth/callback",
    "ConnectSuccessRedirectUrl": "/crm/salesforce/success",
    "Environment": "sandbox",
    "PollingIntervalMinutes": 15,
    "ReconciliationHour": 2
  }
}
```

### Salesforce Connected App Requirements

1. **OAuth Settings**:
   - Enable OAuth Settings: Yes
   - Callback URL: `https://api.caskr.com/api/salesforce/oauth/callback`
   - Selected OAuth Scopes: `api`, `refresh_token`, `offline_access`

2. **Outbound Message Configuration**:
   - Workflow Rule: When Opportunity Stage = "Closed Won"
   - Endpoint URL: `https://api.caskr.com/api/salesforce/webhook/opportunity`

---

## Monitoring & Observability

### Key Metrics

| Metric | Description | Alert Threshold |
|--------|-------------|-----------------|
| `crm_sync_success_rate` | % of successful syncs | < 95% |
| `crm_sync_latency_ms` | Average sync time | > 5000ms |
| `crm_pending_conflicts` | Unresolved conflicts | > 10 |
| `crm_oauth_token_refreshes` | Token refresh count | > 10/hour |

### Dashboard Queries

```sql
-- Sync success rate (last 24 hours)
SELECT
    entity_type,
    COUNT(*) FILTER (WHERE sync_status = 'Success') * 100.0 / COUNT(*) as success_rate,
    COUNT(*) as total_syncs
FROM crm_sync_logs
WHERE synced_at > NOW() - INTERVAL '24 hours'
GROUP BY entity_type;

-- Pending conflicts
SELECT entity_type, COUNT(*) as pending
FROM crm_sync_conflicts
WHERE resolution_status = 'Pending'
GROUP BY entity_type;
```

---

## Related Documentation

- [WEBHOOK_SYSTEM_DESIGN.md](./WEBHOOK_SYSTEM_DESIGN.md) - Webhook architecture patterns
- QuickBooks Integration (FIN-001 through FIN-005) - OAuth and sync patterns
- [Customer Portal](./TTB_COMPLIANCE_USER_GUIDE.md) - Portal user management

---

## Revision History

| Date | Version | Author | Changes |
|------|---------|--------|---------|
| 2025-02-20 | 1.0 | CRM-001 | Initial design document |
