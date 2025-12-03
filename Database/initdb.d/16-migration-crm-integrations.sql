--
-- Migration: Salesforce CRM Integration Tables
-- Date: 2025-12-03
-- Task ID: CRM-001
-- Description: Adds CRM integration infrastructure including:
--              - customers table for customer master data
--              - crm_integrations for OAuth tokens and connection state
--              - crm_sync_logs for audit trail
--              - crm_entity_mappings for Caskr ↔ Salesforce ID relationships
--              - crm_field_mappings for customizable field mapping
--              - crm_sync_preferences for sync configuration
--              - crm_sync_conflicts for manual conflict resolution
--

-- ============================================================================
-- 1. Enum Types for CRM Integration
-- ============================================================================

-- Sync direction enum
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'crm_sync_direction') THEN
        CREATE TYPE crm_sync_direction AS ENUM ('Inbound', 'Outbound', 'Bidirectional');
    END IF;
END $$;

-- Sync status enum
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'crm_sync_status') THEN
        CREATE TYPE crm_sync_status AS ENUM ('Pending', 'InProgress', 'Success', 'Failed', 'Conflict');
    END IF;
END $$;

-- Conflict resolution status enum
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'crm_conflict_status') THEN
        CREATE TYPE crm_conflict_status AS ENUM ('Pending', 'Resolved_Caskr', 'Resolved_Salesforce', 'Merged');
    END IF;
END $$;

-- Customer type enum
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'customer_type') THEN
        CREATE TYPE customer_type AS ENUM ('OnPremise', 'OffPremise', 'Distributor', 'Direct', 'Investor');
    END IF;
END $$;

-- ============================================================================
-- 2. Customers Table (Master Customer Data)
-- ============================================================================
-- Centralized customer table to support CRM sync and order management
CREATE TABLE IF NOT EXISTS public.customers (
    id BIGSERIAL PRIMARY KEY,
    company_id INTEGER NOT NULL,
    customer_name VARCHAR(200) NOT NULL,
    customer_type customer_type NOT NULL DEFAULT 'Direct',
    -- Contact information
    email VARCHAR(200),
    phone VARCHAR(50),
    website VARCHAR(255),
    -- Address information
    address_line1 VARCHAR(200),
    address_line2 VARCHAR(200),
    city VARCHAR(100),
    state VARCHAR(100),
    postal_code VARCHAR(20),
    country VARCHAR(100) DEFAULT 'USA',
    -- Salesforce integration fields
    salesforce_account_id VARCHAR(18),
    salesforce_last_sync_at TIMESTAMPTZ,
    -- Ownership and tracking
    assigned_user_id INTEGER,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    notes TEXT,
    -- Timestamps
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    -- Foreign keys
    CONSTRAINT fk_customers_company
        FOREIGN KEY (company_id) REFERENCES public.company(id),
    CONSTRAINT fk_customers_assigned_user
        FOREIGN KEY (assigned_user_id) REFERENCES public.users(id),
    -- Unique constraint for Salesforce ID per company
    CONSTRAINT uq_customers_salesforce_account
        UNIQUE (company_id, salesforce_account_id)
);

-- Indexes for customers
CREATE INDEX IF NOT EXISTS idx_customers_company_id
    ON public.customers(company_id);

CREATE INDEX IF NOT EXISTS idx_customers_customer_name
    ON public.customers(company_id, customer_name);

CREATE INDEX IF NOT EXISTS idx_customers_salesforce_account_id
    ON public.customers(salesforce_account_id)
    WHERE salesforce_account_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_customers_customer_type
    ON public.customers(company_id, customer_type);

CREATE INDEX IF NOT EXISTS idx_customers_is_active
    ON public.customers(company_id, is_active);

-- ============================================================================
-- 3. CRM Integrations Table (OAuth & Connection State)
-- ============================================================================
-- Stores OAuth tokens and connection state for each CRM provider per company
-- Pattern based on accounting_integrations table
CREATE TABLE IF NOT EXISTS public.crm_integrations (
    id BIGSERIAL PRIMARY KEY,
    company_id INTEGER NOT NULL,
    provider TEXT NOT NULL DEFAULT 'Salesforce',
    -- Salesforce-specific fields
    instance_url TEXT,                          -- e.g., https://na1.salesforce.com
    organization_id VARCHAR(18),                -- Salesforce Org ID (15 or 18 char)
    -- OAuth tokens (encrypted)
    access_token_encrypted TEXT,
    refresh_token_encrypted TEXT,
    token_expires_at TIMESTAMPTZ,
    -- Connection state
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    connection_status TEXT NOT NULL DEFAULT 'Disconnected',
    last_error_message TEXT,
    last_error_at TIMESTAMPTZ,
    -- Tracking
    connected_by_user_id INTEGER,
    connected_at TIMESTAMPTZ,
    last_sync_at TIMESTAMPTZ,
    -- Timestamps
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    -- Foreign keys
    CONSTRAINT fk_crm_integrations_company
        FOREIGN KEY (company_id) REFERENCES public.company(id),
    CONSTRAINT fk_crm_integrations_connected_by
        FOREIGN KEY (connected_by_user_id) REFERENCES public.users(id),
    -- One integration per provider per company
    CONSTRAINT uq_crm_integrations_company_provider
        UNIQUE (company_id, provider),
    -- Valid connection status
    CONSTRAINT chk_crm_integrations_connection_status
        CHECK (connection_status IN ('Connected', 'Disconnected', 'Error', 'TokenExpired'))
);

-- Indexes for crm_integrations
CREATE INDEX IF NOT EXISTS idx_crm_integrations_company_id
    ON public.crm_integrations(company_id);

CREATE INDEX IF NOT EXISTS idx_crm_integrations_provider
    ON public.crm_integrations(provider);

CREATE INDEX IF NOT EXISTS idx_crm_integrations_is_active
    ON public.crm_integrations(is_active);

-- ============================================================================
-- 4. CRM Sync Logs Table (Audit Trail)
-- ============================================================================
-- Logs all sync operations for audit and troubleshooting
-- Pattern based on accounting_sync_logs table
CREATE TABLE IF NOT EXISTS public.crm_sync_logs (
    id BIGSERIAL PRIMARY KEY,
    company_id INTEGER NOT NULL,
    provider TEXT NOT NULL DEFAULT 'Salesforce',
    -- Entity identification
    entity_type TEXT NOT NULL,                  -- Account, Opportunity, Contact
    caskr_entity_id TEXT,                       -- ID in Caskr (customers.id, orders.id)
    caskr_entity_type TEXT,                     -- Customer, Order, PortalUser
    salesforce_id VARCHAR(18),                  -- Salesforce record ID (15 or 18 char)
    -- Sync details
    sync_direction crm_sync_direction NOT NULL,
    sync_status crm_sync_status NOT NULL,
    sync_action TEXT,                           -- Create, Update, Delete
    -- Error handling
    error_message TEXT,
    error_code TEXT,
    retry_count INTEGER NOT NULL DEFAULT 0,
    -- Payload logging (for debugging)
    request_payload JSONB,
    response_payload JSONB,
    -- Timing
    sync_started_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_completed_at TIMESTAMPTZ,
    synced_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    -- Foreign keys
    CONSTRAINT fk_crm_sync_logs_company
        FOREIGN KEY (company_id) REFERENCES public.company(id),
    -- Valid sync actions
    CONSTRAINT chk_crm_sync_logs_action
        CHECK (sync_action IS NULL OR sync_action IN ('Create', 'Update', 'Delete', 'Upsert'))
);

-- Indexes for crm_sync_logs
CREATE INDEX IF NOT EXISTS idx_crm_sync_logs_company_id
    ON public.crm_sync_logs(company_id);

CREATE INDEX IF NOT EXISTS idx_crm_sync_logs_sync_status
    ON public.crm_sync_logs(sync_status);

CREATE INDEX IF NOT EXISTS idx_crm_sync_logs_synced_at
    ON public.crm_sync_logs(synced_at);

CREATE INDEX IF NOT EXISTS idx_crm_sync_logs_entity_type
    ON public.crm_sync_logs(entity_type);

CREATE INDEX IF NOT EXISTS idx_crm_sync_logs_salesforce_id
    ON public.crm_sync_logs(salesforce_id)
    WHERE salesforce_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_crm_sync_logs_caskr_entity
    ON public.crm_sync_logs(caskr_entity_type, caskr_entity_id)
    WHERE caskr_entity_id IS NOT NULL;

-- Composite index for recent sync queries
CREATE INDEX IF NOT EXISTS idx_crm_sync_logs_company_status_time
    ON public.crm_sync_logs(company_id, sync_status, synced_at DESC);

-- ============================================================================
-- 5. CRM Entity Mappings Table (Caskr ↔ Salesforce ID Links)
-- ============================================================================
-- Tracks the relationship between Caskr entities and Salesforce records
CREATE TABLE IF NOT EXISTS public.crm_entity_mappings (
    id BIGSERIAL PRIMARY KEY,
    company_id INTEGER NOT NULL,
    provider TEXT NOT NULL DEFAULT 'Salesforce',
    -- Salesforce entity info
    salesforce_entity_type TEXT NOT NULL,       -- Account, Opportunity, Contact
    salesforce_id VARCHAR(18) NOT NULL,         -- Salesforce record ID
    -- Caskr entity info
    caskr_entity_type TEXT NOT NULL,            -- Customer, Order, PortalUser
    caskr_entity_id TEXT NOT NULL,              -- ID in Caskr
    -- Sync tracking for conflict detection
    last_sync_at TIMESTAMPTZ,
    caskr_last_modified TIMESTAMPTZ,
    salesforce_last_modified TIMESTAMPTZ,
    -- Status
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    -- Timestamps
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    -- Foreign keys
    CONSTRAINT fk_crm_entity_mappings_company
        FOREIGN KEY (company_id) REFERENCES public.company(id),
    -- Unique constraints to prevent duplicate mappings
    CONSTRAINT uq_crm_entity_mappings_caskr
        UNIQUE (company_id, provider, caskr_entity_type, caskr_entity_id),
    CONSTRAINT uq_crm_entity_mappings_salesforce
        UNIQUE (company_id, provider, salesforce_entity_type, salesforce_id)
);

-- Indexes for crm_entity_mappings
CREATE INDEX IF NOT EXISTS idx_crm_entity_mappings_company_id
    ON public.crm_entity_mappings(company_id);

CREATE INDEX IF NOT EXISTS idx_crm_entity_mappings_salesforce_id
    ON public.crm_entity_mappings(salesforce_id);

CREATE INDEX IF NOT EXISTS idx_crm_entity_mappings_caskr_entity
    ON public.crm_entity_mappings(caskr_entity_type, caskr_entity_id);

CREATE INDEX IF NOT EXISTS idx_crm_entity_mappings_is_active
    ON public.crm_entity_mappings(is_active);

-- ============================================================================
-- 6. CRM Field Mappings Table (Customizable Mapping)
-- ============================================================================
-- Allows per-company customization of field mappings between systems
CREATE TABLE IF NOT EXISTS public.crm_field_mappings (
    id BIGSERIAL PRIMARY KEY,
    company_id INTEGER NOT NULL,
    provider TEXT NOT NULL DEFAULT 'Salesforce',
    -- Entity type this mapping applies to
    salesforce_entity_type TEXT NOT NULL,       -- Account, Opportunity, Contact
    caskr_entity_type TEXT NOT NULL,            -- Customer, Order, PortalUser
    -- Field mapping
    salesforce_field TEXT NOT NULL,             -- Salesforce API field name
    caskr_field TEXT NOT NULL,                  -- Caskr entity field name
    -- Transformation options
    transformation_rule TEXT,                   -- Optional: UPPERCASE, LOWERCASE, DATE_FORMAT, etc.
    default_value TEXT,                         -- Default if source is null
    -- Configuration
    sync_direction crm_sync_direction NOT NULL DEFAULT 'Inbound',
    is_required BOOLEAN NOT NULL DEFAULT FALSE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    -- Timestamps
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    -- Foreign keys
    CONSTRAINT fk_crm_field_mappings_company
        FOREIGN KEY (company_id) REFERENCES public.company(id),
    -- Unique field mapping per company/entity
    CONSTRAINT uq_crm_field_mappings
        UNIQUE (company_id, provider, salesforce_entity_type, salesforce_field)
);

-- Indexes for crm_field_mappings
CREATE INDEX IF NOT EXISTS idx_crm_field_mappings_company_id
    ON public.crm_field_mappings(company_id);

CREATE INDEX IF NOT EXISTS idx_crm_field_mappings_entity_type
    ON public.crm_field_mappings(salesforce_entity_type);

-- ============================================================================
-- 7. CRM Sync Preferences Table (Configuration per Entity)
-- ============================================================================
-- Configures sync behavior for each entity type per company
CREATE TABLE IF NOT EXISTS public.crm_sync_preferences (
    id BIGSERIAL PRIMARY KEY,
    company_id INTEGER NOT NULL,
    provider TEXT NOT NULL DEFAULT 'Salesforce',
    -- Entity type this preference applies to
    entity_type TEXT NOT NULL,                  -- Account, Opportunity, Contact
    -- Sync direction
    sync_direction crm_sync_direction NOT NULL DEFAULT 'Inbound',
    -- Sync methods
    webhook_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    polling_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    polling_interval_minutes INTEGER NOT NULL DEFAULT 15,
    -- Behavior options
    auto_create_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    auto_update_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    auto_delete_enabled BOOLEAN NOT NULL DEFAULT FALSE,
    -- Conflict resolution
    conflict_resolution TEXT NOT NULL DEFAULT 'LastWriteWins',
    -- Tracking
    last_polling_at TIMESTAMPTZ,
    last_webhook_at TIMESTAMPTZ,
    -- Status
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    -- Timestamps
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    -- Foreign keys
    CONSTRAINT fk_crm_sync_preferences_company
        FOREIGN KEY (company_id) REFERENCES public.company(id),
    -- One preference set per entity type per company
    CONSTRAINT uq_crm_sync_preferences
        UNIQUE (company_id, provider, entity_type),
    -- Valid conflict resolution strategies
    CONSTRAINT chk_crm_sync_preferences_conflict_resolution
        CHECK (conflict_resolution IN ('LastWriteWins', 'CaskrWins', 'SalesforceWins', 'Manual')),
    -- Valid polling intervals (1 minute to 24 hours)
    CONSTRAINT chk_crm_sync_preferences_polling_interval
        CHECK (polling_interval_minutes >= 1 AND polling_interval_minutes <= 1440)
);

-- Indexes for crm_sync_preferences
CREATE INDEX IF NOT EXISTS idx_crm_sync_preferences_company_id
    ON public.crm_sync_preferences(company_id);

-- ============================================================================
-- 8. CRM Sync Conflicts Table (Manual Resolution Queue)
-- ============================================================================
-- Stores conflicts that require manual review and resolution
CREATE TABLE IF NOT EXISTS public.crm_sync_conflicts (
    id BIGSERIAL PRIMARY KEY,
    company_id INTEGER NOT NULL,
    provider TEXT NOT NULL DEFAULT 'Salesforce',
    -- Entity identification
    salesforce_entity_type TEXT NOT NULL,
    salesforce_id VARCHAR(18) NOT NULL,
    caskr_entity_type TEXT NOT NULL,
    caskr_entity_id TEXT NOT NULL,
    -- Conflict details
    field_name TEXT NOT NULL,
    caskr_value TEXT,
    salesforce_value TEXT,
    caskr_modified_at TIMESTAMPTZ,
    salesforce_modified_at TIMESTAMPTZ,
    -- Resolution
    resolution_status crm_conflict_status NOT NULL DEFAULT 'Pending',
    resolved_value TEXT,
    resolved_by_user_id INTEGER,
    resolved_at TIMESTAMPTZ,
    resolution_notes TEXT,
    -- Timestamps
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    -- Foreign keys
    CONSTRAINT fk_crm_sync_conflicts_company
        FOREIGN KEY (company_id) REFERENCES public.company(id),
    CONSTRAINT fk_crm_sync_conflicts_resolved_by
        FOREIGN KEY (resolved_by_user_id) REFERENCES public.users(id)
);

-- Indexes for crm_sync_conflicts
CREATE INDEX IF NOT EXISTS idx_crm_sync_conflicts_company_id
    ON public.crm_sync_conflicts(company_id);

CREATE INDEX IF NOT EXISTS idx_crm_sync_conflicts_status
    ON public.crm_sync_conflicts(resolution_status);

CREATE INDEX IF NOT EXISTS idx_crm_sync_conflicts_pending
    ON public.crm_sync_conflicts(company_id, resolution_status)
    WHERE resolution_status = 'Pending';

CREATE INDEX IF NOT EXISTS idx_crm_sync_conflicts_created_at
    ON public.crm_sync_conflicts(created_at DESC);

-- ============================================================================
-- 9. Add Salesforce Integration Fields to Existing Tables
-- ============================================================================

-- Add Salesforce fields to orders table
ALTER TABLE public.orders
    ADD COLUMN IF NOT EXISTS customer_id BIGINT,
    ADD COLUMN IF NOT EXISTS salesforce_opportunity_id VARCHAR(18),
    ADD COLUMN IF NOT EXISTS salesforce_last_sync_at TIMESTAMPTZ,
    ADD COLUMN IF NOT EXISTS order_date DATE,
    ADD COLUMN IF NOT EXISTS total_amount DECIMAL(12, 2),
    ADD COLUMN IF NOT EXISTS order_notes TEXT;

-- Add foreign key for customer_id if it doesn't exist
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints
        WHERE constraint_name = 'fk_orders_customer'
        AND table_name = 'orders'
    ) THEN
        ALTER TABLE public.orders
            ADD CONSTRAINT fk_orders_customer
                FOREIGN KEY (customer_id) REFERENCES public.customers(id);
    END IF;
END $$;

-- Index for Salesforce opportunity ID
CREATE INDEX IF NOT EXISTS idx_orders_salesforce_opportunity_id
    ON public.orders(salesforce_opportunity_id)
    WHERE salesforce_opportunity_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_orders_customer_id
    ON public.orders(customer_id)
    WHERE customer_id IS NOT NULL;

-- Add Salesforce fields to portal_users table
ALTER TABLE public.portal_users
    ADD COLUMN IF NOT EXISTS salesforce_contact_id VARCHAR(18),
    ADD COLUMN IF NOT EXISTS salesforce_last_sync_at TIMESTAMPTZ,
    ADD COLUMN IF NOT EXISTS linked_customer_id BIGINT,
    ADD COLUMN IF NOT EXISTS is_cask_investor BOOLEAN NOT NULL DEFAULT FALSE;

-- Add foreign key for linked_customer_id if it doesn't exist
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints
        WHERE constraint_name = 'fk_portal_users_customer'
        AND table_name = 'portal_users'
    ) THEN
        ALTER TABLE public.portal_users
            ADD CONSTRAINT fk_portal_users_customer
                FOREIGN KEY (linked_customer_id) REFERENCES public.customers(id);
    END IF;
END $$;

-- Index for Salesforce contact ID
CREATE INDEX IF NOT EXISTS idx_portal_users_salesforce_contact_id
    ON public.portal_users(salesforce_contact_id)
    WHERE salesforce_contact_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_portal_users_linked_customer_id
    ON public.portal_users(linked_customer_id)
    WHERE linked_customer_id IS NOT NULL;

-- ============================================================================
-- 10. Table Comments for Documentation
-- ============================================================================

COMMENT ON TABLE public.customers IS 'Master customer table for CRM integration. Stores customer/account data synced from Salesforce.';
COMMENT ON COLUMN public.customers.salesforce_account_id IS 'Salesforce Account ID (15 or 18 character format)';
COMMENT ON COLUMN public.customers.customer_type IS 'Customer classification: OnPremise (bars/restaurants), OffPremise (retail), Distributor, Direct, Investor';

COMMENT ON TABLE public.crm_integrations IS 'OAuth tokens and connection state for CRM providers. Similar pattern to accounting_integrations.';
COMMENT ON COLUMN public.crm_integrations.access_token_encrypted IS 'OAuth access token encrypted using ASP.NET Core Data Protection';
COMMENT ON COLUMN public.crm_integrations.refresh_token_encrypted IS 'OAuth refresh token encrypted using ASP.NET Core Data Protection';
COMMENT ON COLUMN public.crm_integrations.instance_url IS 'Salesforce instance URL (e.g., https://na1.salesforce.com)';

COMMENT ON TABLE public.crm_sync_logs IS 'Audit trail for all CRM sync operations. Used for troubleshooting and compliance.';
COMMENT ON COLUMN public.crm_sync_logs.request_payload IS 'JSON payload sent to/from Salesforce (for debugging)';
COMMENT ON COLUMN public.crm_sync_logs.retry_count IS 'Number of retry attempts for failed sync operations';

COMMENT ON TABLE public.crm_entity_mappings IS 'Links Caskr entities to their Salesforce counterparts. Used for update detection and conflict resolution.';
COMMENT ON COLUMN public.crm_entity_mappings.caskr_last_modified IS 'Timestamp of last Caskr modification for conflict detection';
COMMENT ON COLUMN public.crm_entity_mappings.salesforce_last_modified IS 'Timestamp of last Salesforce modification for conflict detection';

COMMENT ON TABLE public.crm_field_mappings IS 'Customizable field mappings between Salesforce and Caskr entities. Allows per-company configuration.';
COMMENT ON COLUMN public.crm_field_mappings.transformation_rule IS 'Optional transformation: UPPERCASE, LOWERCASE, DATE_FORMAT, TRIM, etc.';

COMMENT ON TABLE public.crm_sync_preferences IS 'Per-entity sync configuration. Controls sync direction, frequency, and conflict resolution.';
COMMENT ON COLUMN public.crm_sync_preferences.conflict_resolution IS 'Strategy: LastWriteWins, CaskrWins, SalesforceWins, or Manual';

COMMENT ON TABLE public.crm_sync_conflicts IS 'Queue for conflicts requiring manual resolution. Shows side-by-side values for user decision.';
COMMENT ON COLUMN public.crm_sync_conflicts.resolution_status IS 'Pending, Resolved_Caskr (Caskr value kept), Resolved_Salesforce (SF value kept), or Merged';

-- ============================================================================
-- 11. Seed Default Field Mappings
-- ============================================================================
-- Insert default field mappings for all existing companies
-- This runs once during migration; new companies get mappings via application code

INSERT INTO public.crm_field_mappings (
    company_id, provider, salesforce_entity_type, caskr_entity_type,
    salesforce_field, caskr_field, is_required, sync_direction
)
SELECT
    c.id,
    'Salesforce',
    mapping.sf_entity,
    mapping.caskr_entity,
    mapping.sf_field,
    mapping.caskr_field,
    mapping.is_required,
    'Inbound'::crm_sync_direction
FROM public.company c
CROSS JOIN (VALUES
    -- Account → Customer mappings
    ('Account', 'Customer', 'Id', 'salesforce_account_id', true),
    ('Account', 'Customer', 'Name', 'customer_name', true),
    ('Account', 'Customer', 'BillingStreet', 'address_line1', false),
    ('Account', 'Customer', 'BillingCity', 'city', false),
    ('Account', 'Customer', 'BillingState', 'state', false),
    ('Account', 'Customer', 'BillingPostalCode', 'postal_code', false),
    ('Account', 'Customer', 'BillingCountry', 'country', false),
    ('Account', 'Customer', 'Phone', 'phone', false),
    ('Account', 'Customer', 'Website', 'website', false),
    -- Opportunity → Order mappings
    ('Opportunity', 'Order', 'Id', 'salesforce_opportunity_id', true),
    ('Opportunity', 'Order', 'AccountId', 'customer_id', true),
    ('Opportunity', 'Order', 'Amount', 'total_amount', false),
    ('Opportunity', 'Order', 'CloseDate', 'order_date', true),
    ('Opportunity', 'Order', 'Name', 'order_notes', false),
    -- Contact → PortalUser mappings
    ('Contact', 'PortalUser', 'Id', 'salesforce_contact_id', true),
    ('Contact', 'PortalUser', 'Email', 'email', true),
    ('Contact', 'PortalUser', 'FirstName', 'first_name', false),
    ('Contact', 'PortalUser', 'LastName', 'last_name', true),
    ('Contact', 'PortalUser', 'Phone', 'phone', false)
) AS mapping(sf_entity, caskr_entity, sf_field, caskr_field, is_required)
ON CONFLICT (company_id, provider, salesforce_entity_type, salesforce_field) DO NOTHING;

-- ============================================================================
-- 12. Seed Default Sync Preferences
-- ============================================================================
-- Insert default sync preferences for all existing companies

INSERT INTO public.crm_sync_preferences (
    company_id, provider, entity_type, sync_direction,
    webhook_enabled, polling_enabled, polling_interval_minutes,
    auto_create_enabled, auto_update_enabled, conflict_resolution
)
SELECT
    c.id,
    'Salesforce',
    pref.entity_type,
    pref.sync_direction::crm_sync_direction,
    pref.webhook_enabled,
    pref.polling_enabled,
    pref.polling_interval,
    pref.auto_create,
    pref.auto_update,
    pref.conflict_resolution
FROM public.company c
CROSS JOIN (VALUES
    -- Account sync preferences
    ('Account', 'Inbound', true, true, 15, true, true, 'LastWriteWins'),
    -- Opportunity sync preferences (real-time via webhook)
    ('Opportunity', 'Inbound', true, true, 15, true, true, 'LastWriteWins'),
    -- Contact sync preferences
    ('Contact', 'Inbound', true, true, 60, true, true, 'LastWriteWins')
) AS pref(entity_type, sync_direction, webhook_enabled, polling_enabled,
          polling_interval, auto_create, auto_update, conflict_resolution)
ON CONFLICT (company_id, provider, entity_type) DO NOTHING;
