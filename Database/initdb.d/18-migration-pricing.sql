--
-- Migration: Pricing data model for public pricing page
-- Date: 2025-12-03
-- Description: Adds pricing_tiers, pricing_features, pricing_tier_features, pricing_faqs,
--              pricing_promotions, and pricing_audit_logs tables for data-driven pricing management
--

-- Pricing tiers table (Craft, Growth, Professional, Enterprise)
CREATE TABLE IF NOT EXISTS public.pricing_tiers (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    slug VARCHAR(50) NOT NULL,
    tagline VARCHAR(200),
    monthly_price_cents INTEGER,
    annual_price_cents INTEGER,
    annual_discount_percent INTEGER NOT NULL DEFAULT 0,
    is_popular BOOLEAN NOT NULL DEFAULT FALSE,
    is_custom_pricing BOOLEAN NOT NULL DEFAULT FALSE,
    cta_text VARCHAR(50),
    cta_url VARCHAR(200),
    sort_order INTEGER NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pricing_tiers_slug_key UNIQUE (slug)
);

-- Indexes for pricing_tiers
CREATE INDEX IF NOT EXISTS idx_pricing_tiers_sort_order
    ON public.pricing_tiers(sort_order);

CREATE INDEX IF NOT EXISTS idx_pricing_tiers_is_active
    ON public.pricing_tiers(is_active);

-- Pricing features table
CREATE TABLE IF NOT EXISTS public.pricing_features (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    description VARCHAR(500),
    category VARCHAR(50),
    sort_order INTEGER NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Indexes for pricing_features
CREATE INDEX IF NOT EXISTS idx_pricing_features_category
    ON public.pricing_features(category);

CREATE INDEX IF NOT EXISTS idx_pricing_features_sort_order
    ON public.pricing_features(sort_order);

CREATE INDEX IF NOT EXISTS idx_pricing_features_is_active
    ON public.pricing_features(is_active);

-- Pricing tier features junction table
CREATE TABLE IF NOT EXISTS public.pricing_tier_features (
    id SERIAL PRIMARY KEY,
    tier_id INTEGER NOT NULL,
    feature_id INTEGER NOT NULL,
    is_included BOOLEAN NOT NULL DEFAULT TRUE,
    limit_value VARCHAR(50),
    limit_description VARCHAR(100),
    CONSTRAINT uq_pricing_tier_features UNIQUE (tier_id, feature_id),
    CONSTRAINT fk_pricing_tier_features_tier
        FOREIGN KEY (tier_id) REFERENCES public.pricing_tiers(id) ON DELETE CASCADE,
    CONSTRAINT fk_pricing_tier_features_feature
        FOREIGN KEY (feature_id) REFERENCES public.pricing_features(id) ON DELETE CASCADE
);

-- Indexes for pricing_tier_features
CREATE INDEX IF NOT EXISTS idx_pricing_tier_features_tier_id
    ON public.pricing_tier_features(tier_id);

CREATE INDEX IF NOT EXISTS idx_pricing_tier_features_feature_id
    ON public.pricing_tier_features(feature_id);

-- Pricing FAQs table
CREATE TABLE IF NOT EXISTS public.pricing_faqs (
    id SERIAL PRIMARY KEY,
    question VARCHAR(500) NOT NULL,
    answer TEXT NOT NULL,
    sort_order INTEGER NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Indexes for pricing_faqs
CREATE INDEX IF NOT EXISTS idx_pricing_faqs_sort_order
    ON public.pricing_faqs(sort_order);

CREATE INDEX IF NOT EXISTS idx_pricing_faqs_is_active
    ON public.pricing_faqs(is_active);

-- Pricing promotions table
CREATE TABLE IF NOT EXISTS public.pricing_promotions (
    id SERIAL PRIMARY KEY,
    code VARCHAR(50) NOT NULL,
    description VARCHAR(200),
    discount_type VARCHAR(20) NOT NULL,
    discount_value INTEGER NOT NULL DEFAULT 0,
    applies_to_tiers JSONB,
    valid_from TIMESTAMPTZ,
    valid_until TIMESTAMPTZ,
    max_redemptions INTEGER,
    current_redemptions INTEGER NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pricing_promotions_code_key UNIQUE (code),
    CONSTRAINT chk_pricing_promotions_discount_type
        CHECK (discount_type IN ('Percentage', 'FixedAmount', 'FreeMonths'))
);

-- Indexes for pricing_promotions
CREATE INDEX IF NOT EXISTS idx_pricing_promotions_is_active
    ON public.pricing_promotions(is_active);

CREATE INDEX IF NOT EXISTS idx_pricing_promotions_validity
    ON public.pricing_promotions(valid_from, valid_until);

CREATE INDEX IF NOT EXISTS idx_pricing_promotions_code_active
    ON public.pricing_promotions(code, is_active);

-- Pricing audit logs table
CREATE TABLE IF NOT EXISTS public.pricing_audit_logs (
    id SERIAL PRIMARY KEY,
    entity_type VARCHAR(50) NOT NULL,
    entity_id INTEGER NOT NULL,
    action VARCHAR(20) NOT NULL,
    changed_by_user_id INTEGER NOT NULL,
    change_timestamp TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    old_values JSONB,
    new_values JSONB,
    ip_address VARCHAR(45),
    user_agent TEXT,
    change_description TEXT,
    CONSTRAINT fk_pricing_audit_logs_user
        FOREIGN KEY (changed_by_user_id) REFERENCES public.users(id),
    CONSTRAINT chk_pricing_audit_logs_action
        CHECK (action IN ('Create', 'Update', 'Delete', 'Activate', 'Deactivate'))
);

-- Indexes for pricing_audit_logs
CREATE INDEX IF NOT EXISTS idx_pricing_audit_logs_entity_type
    ON public.pricing_audit_logs(entity_type);

CREATE INDEX IF NOT EXISTS idx_pricing_audit_logs_entity_id
    ON public.pricing_audit_logs(entity_id);

CREATE INDEX IF NOT EXISTS idx_pricing_audit_logs_timestamp
    ON public.pricing_audit_logs(change_timestamp);

CREATE INDEX IF NOT EXISTS idx_pricing_audit_logs_user_id
    ON public.pricing_audit_logs(changed_by_user_id);

-- Composite index for entity lookup
CREATE INDEX IF NOT EXISTS idx_pricing_audit_logs_entity_lookup
    ON public.pricing_audit_logs(entity_type, entity_id, change_timestamp DESC);

-- Comments for documentation
COMMENT ON TABLE public.pricing_tiers IS 'Stores pricing tier configurations (e.g., Craft, Growth, Professional, Enterprise)';
COMMENT ON TABLE public.pricing_features IS 'Stores individual features that can be included in pricing tiers';
COMMENT ON TABLE public.pricing_tier_features IS 'Junction table linking tiers to features with limit values';
COMMENT ON TABLE public.pricing_faqs IS 'Stores frequently asked questions displayed on the pricing page';
COMMENT ON TABLE public.pricing_promotions IS 'Stores promotional discount codes that can be applied to pricing';
COMMENT ON TABLE public.pricing_audit_logs IS 'Audit trail for all admin changes to pricing data';

COMMENT ON COLUMN public.pricing_tiers.slug IS 'URL-friendly identifier for the tier (e.g., "craft", "growth")';
COMMENT ON COLUMN public.pricing_tiers.monthly_price_cents IS 'Monthly price in cents (NULL for custom pricing tiers)';
COMMENT ON COLUMN public.pricing_tiers.annual_price_cents IS 'Annual price in cents (NULL for custom pricing tiers)';
COMMENT ON COLUMN public.pricing_tiers.is_popular IS 'Whether to highlight this tier as "Most Popular"';
COMMENT ON COLUMN public.pricing_tiers.is_custom_pricing IS 'Whether this tier requires custom pricing (e.g., Enterprise)';

COMMENT ON COLUMN public.pricing_features.category IS 'Category grouping (e.g., Compliance, Inventory, Reporting, Support)';

COMMENT ON COLUMN public.pricing_tier_features.is_included IS 'Whether the feature is included (TRUE = checkmark, FALSE = X)';
COMMENT ON COLUMN public.pricing_tier_features.limit_value IS 'Limit value display (e.g., "5 users", "Unlimited")';

COMMENT ON COLUMN public.pricing_faqs.answer IS 'Answer text supporting markdown formatting';

COMMENT ON COLUMN public.pricing_promotions.discount_type IS 'Type of discount: Percentage, FixedAmount (cents), or FreeMonths';
COMMENT ON COLUMN public.pricing_promotions.discount_value IS 'Discount value (percentage points, cents, or months based on type)';
COMMENT ON COLUMN public.pricing_promotions.applies_to_tiers IS 'JSON array of tier IDs (NULL = applies to all tiers)';
