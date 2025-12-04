--
-- Seed data: Initial pricing tiers, features, and FAQs for US domestic MVP launch
-- Date: 2025-12-04
-- Description: Populates pricing tables with Starter ($699/mo), Growth ($1,699/mo),
--              Professional ($2,999/mo), and Enterprise (custom) tiers
--
-- This replaces the application-level PricingDataSeeder.cs - all seeding is now handled via SQL
--

-- Clear existing data to ensure clean seed (use with caution in production)
-- This ensures idempotent seeding behavior
TRUNCATE TABLE public.pricing_tier_features CASCADE;
TRUNCATE TABLE public.pricing_faqs CASCADE;
TRUNCATE TABLE public.pricing_promotions CASCADE;
TRUNCATE TABLE public.pricing_tiers CASCADE;
TRUNCATE TABLE public.pricing_features CASCADE;

-- Reset sequences
ALTER SEQUENCE pricing_features_id_seq RESTART WITH 1;
ALTER SEQUENCE pricing_tiers_id_seq RESTART WITH 1;
ALTER SEQUENCE pricing_tier_features_id_seq RESTART WITH 1;
ALTER SEQUENCE pricing_faqs_id_seq RESTART WITH 1;
ALTER SEQUENCE pricing_promotions_id_seq RESTART WITH 1;

-- ============================================================================
-- PRICING FEATURES (27 features across 8 categories)
-- ============================================================================

-- Core Features (1-4)
INSERT INTO public.pricing_features (id, name, description, category, sort_order, is_active, created_at, updated_at) VALUES
(1, 'Barrel Inventory', 'Complete barrel lifecycle tracking', 'Core', 1, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(2, 'Barrel Limit', 'Maximum barrels you can track', 'Core', 2, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(3, 'Users', 'Team member accounts', 'Core', 3, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(4, 'Locations', 'Warehouse/facility locations', 'Core', 4, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- TTB Compliance Features (5-8)
INSERT INTO public.pricing_features (id, name, description, category, sort_order, is_active, created_at, updated_at) VALUES
(5, 'TTB Compliance', 'Full TTB form automation (5110.28, 5110.40, 5100.16)', 'Compliance', 1, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(6, 'Gauge Records', 'Temperature-corrected proof gallon calculations', 'Compliance', 2, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(7, 'Excise Tax Calculation', 'Federal excise tax with reduced rate tracking', 'Compliance', 3, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(8, 'Audit Trail', '31+ field comprehensive audit logging', 'Compliance', 4, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Financial Features (9-11)
INSERT INTO public.pricing_features (id, name, description, category, sort_order, is_active, created_at, updated_at) VALUES
(9, 'QuickBooks Integration', 'Bi-directional sync with QuickBooks Online', 'Financial', 1, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(10, 'Invoice Sync', 'Automatic invoice creation in QuickBooks', 'Financial', 2, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(11, 'COGS Tracking', 'Cost of goods sold calculation and journal entries', 'Financial', 3, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Reporting Features (12-14)
INSERT INTO public.pricing_features (id, name, description, category, sort_order, is_active, created_at, updated_at) VALUES
(12, 'Standard Reports', 'Pre-built financial, inventory, and compliance reports', 'Reporting', 1, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(13, 'Custom Report Builder', 'Drag-and-drop report creation with 20+ tables', 'Reporting', 2, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(14, 'Export Options', 'Export to CSV and PDF', 'Reporting', 3, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Investor Portal Features (15-18)
INSERT INTO public.pricing_features (id, name, description, category, sort_order, is_active, created_at, updated_at) VALUES
(15, 'Investor Portal', 'Customer-facing portal for cask ownership', 'Portal', 1, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(16, 'Investor Limit', 'Maximum investor accounts', 'Portal', 2, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(17, 'Document Management', 'Ownership certificates, photos, invoices', 'Portal', 3, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(18, 'Maturation Tracking', 'Age and progress tracking for investors', 'Portal', 4, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Mobile & Operations Features (19-21)
INSERT INTO public.pricing_features (id, name, description, category, sort_order, is_active, created_at, updated_at) VALUES
(19, 'Mobile Access', 'PWA mobile experience', 'Operations', 1, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(20, 'Barcode Scanning', 'Web-based QR and barcode scanning (5 formats)', 'Operations', 2, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(21, 'Offline Support', 'Work offline with automatic sync', 'Operations', 3, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Integration Features (22-23)
INSERT INTO public.pricing_features (id, name, description, category, sort_order, is_active, created_at, updated_at) VALUES
(22, 'Webhooks', '12 event types for integrations', 'Integration', 1, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(23, 'API Access', 'Full REST API with documentation', 'Integration', 2, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Production Features (24)
INSERT INTO public.pricing_features (id, name, description, category, sort_order, is_active, created_at, updated_at) VALUES
(24, 'Production Planning', 'Scheduling and capacity management', 'Production', 1, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Support Features (25-27)
INSERT INTO public.pricing_features (id, name, description, category, sort_order, is_active, created_at, updated_at) VALUES
(25, 'Support Response', 'Support response time SLA', 'Support', 1, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(26, 'Onboarding', 'Implementation assistance', 'Support', 2, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(27, 'Dedicated Account Manager', 'Named account manager', 'Support', 3, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Update sequence to next available ID
SELECT setval('pricing_features_id_seq', 27, true);

-- ============================================================================
-- PRICING TIERS (4 tiers)
-- ============================================================================

INSERT INTO public.pricing_tiers (id, name, slug, tagline, monthly_price_cents, annual_price_cents, annual_discount_percent, is_popular, is_custom_pricing, cta_text, cta_url, sort_order, is_active, created_at, updated_at) VALUES
-- Starter: $699/mo, $5,592/yr (20% annual discount = $559.20/mo)
(1, 'Starter', 'starter', 'For emerging craft distilleries', 69900, 671040, 20, false, false, 'Start Free Trial', '/signup?plan=starter', 1, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Growth: $1,699/mo, $13,592/yr (20% annual discount = $1,359.20/mo) - MOST POPULAR
(2, 'Growth', 'growth', 'For growing craft distilleries', 169900, 1631040, 20, true, false, 'Start Free Trial', '/signup?plan=growth', 2, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Professional: $2,999/mo, $23,992/yr (20% annual discount = $2,399.20/mo)
(3, 'Professional', 'professional', 'For established multi-location distilleries', 299900, 2879040, 20, false, false, 'Start Free Trial', '/signup?plan=professional', 3, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Enterprise: Custom pricing
(4, 'Enterprise', 'enterprise', 'Custom solutions for large operations', NULL, NULL, 0, false, true, 'Contact Sales', '/contact?plan=enterprise', 4, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Update sequence to next available ID
SELECT setval('pricing_tiers_id_seq', 4, true);

-- ============================================================================
-- PRICING TIER FEATURES (108 mappings: 27 features x 4 tiers)
-- ============================================================================

-- Starter Tier (ID: 1) - 27 features
INSERT INTO public.pricing_tier_features (tier_id, feature_id, is_included, limit_value, limit_description) VALUES
-- Core
(1, 1, true, NULL, NULL),
(1, 2, true, '500', 'barrels'),
(1, 3, true, '3', 'users'),
(1, 4, true, '1', 'location'),
-- Compliance
(1, 5, true, NULL, NULL),
(1, 6, true, NULL, NULL),
(1, 7, true, NULL, NULL),
(1, 8, true, NULL, NULL),
-- Financial
(1, 9, true, NULL, NULL),
(1, 10, true, NULL, NULL),
(1, 11, true, NULL, NULL),
-- Reporting
(1, 12, true, '15', 'reports'),
(1, 13, false, NULL, NULL),
(1, 14, true, NULL, NULL),
-- Portal (not included in Starter)
(1, 15, false, NULL, NULL),
(1, 16, false, NULL, NULL),
(1, 17, false, NULL, NULL),
(1, 18, false, NULL, NULL),
-- Operations
(1, 19, true, NULL, NULL),
(1, 20, true, NULL, NULL),
(1, 21, true, NULL, NULL),
-- Integration (not included in Starter)
(1, 22, false, NULL, NULL),
(1, 23, false, NULL, NULL),
-- Production (not included in Starter)
(1, 24, false, NULL, NULL),
-- Support
(1, 25, true, '48h', 'response'),
(1, 26, true, 'Self-serve', NULL),
(1, 27, false, NULL, NULL);

-- Growth Tier (ID: 2) - 27 features
INSERT INTO public.pricing_tier_features (tier_id, feature_id, is_included, limit_value, limit_description) VALUES
-- Core
(2, 1, true, NULL, NULL),
(2, 2, true, '2,500', 'barrels'),
(2, 3, true, '10', 'users'),
(2, 4, true, '2', 'locations'),
-- Compliance
(2, 5, true, NULL, NULL),
(2, 6, true, NULL, NULL),
(2, 7, true, NULL, NULL),
(2, 8, true, NULL, NULL),
-- Financial
(2, 9, true, NULL, NULL),
(2, 10, true, NULL, NULL),
(2, 11, true, NULL, NULL),
-- Reporting
(2, 12, true, '30+', 'reports'),
(2, 13, true, NULL, NULL),
(2, 14, true, NULL, NULL),
-- Portal
(2, 15, true, NULL, NULL),
(2, 16, true, '50', 'investors'),
(2, 17, true, NULL, NULL),
(2, 18, true, NULL, NULL),
-- Operations
(2, 19, true, NULL, NULL),
(2, 20, true, NULL, NULL),
(2, 21, true, NULL, NULL),
-- Integration
(2, 22, true, NULL, NULL),
(2, 23, true, NULL, NULL),
-- Production (not included in Growth)
(2, 24, false, NULL, NULL),
-- Support
(2, 25, true, '24h', 'response'),
(2, 26, true, 'Guided (2h)', NULL),
(2, 27, false, NULL, NULL);

-- Professional Tier (ID: 3) - 27 features
INSERT INTO public.pricing_tier_features (tier_id, feature_id, is_included, limit_value, limit_description) VALUES
-- Core
(3, 1, true, NULL, NULL),
(3, 2, true, '10,000', 'barrels'),
(3, 3, true, '25', 'users'),
(3, 4, true, '5', 'locations'),
-- Compliance
(3, 5, true, NULL, NULL),
(3, 6, true, NULL, NULL),
(3, 7, true, NULL, NULL),
(3, 8, true, NULL, NULL),
-- Financial
(3, 9, true, NULL, NULL),
(3, 10, true, NULL, NULL),
(3, 11, true, NULL, NULL),
-- Reporting
(3, 12, true, '30+', 'reports'),
(3, 13, true, NULL, NULL),
(3, 14, true, NULL, NULL),
-- Portal
(3, 15, true, NULL, NULL),
(3, 16, true, '200', 'investors'),
(3, 17, true, NULL, NULL),
(3, 18, true, NULL, NULL),
-- Operations
(3, 19, true, NULL, NULL),
(3, 20, true, NULL, NULL),
(3, 21, true, NULL, NULL),
-- Integration
(3, 22, true, NULL, NULL),
(3, 23, true, NULL, NULL),
-- Production
(3, 24, true, NULL, NULL),
-- Support
(3, 25, true, '4h', 'response'),
(3, 26, true, 'White-glove (8h)', NULL),
(3, 27, false, NULL, NULL);

-- Enterprise Tier (ID: 4) - 27 features (all included, most unlimited)
INSERT INTO public.pricing_tier_features (tier_id, feature_id, is_included, limit_value, limit_description) VALUES
-- Core
(4, 1, true, NULL, NULL),
(4, 2, true, 'Unlimited', NULL),
(4, 3, true, 'Unlimited', NULL),
(4, 4, true, 'Unlimited', NULL),
-- Compliance
(4, 5, true, NULL, NULL),
(4, 6, true, NULL, NULL),
(4, 7, true, NULL, NULL),
(4, 8, true, NULL, NULL),
-- Financial
(4, 9, true, NULL, NULL),
(4, 10, true, NULL, NULL),
(4, 11, true, NULL, NULL),
-- Reporting
(4, 12, true, '30+', 'reports'),
(4, 13, true, NULL, NULL),
(4, 14, true, NULL, NULL),
-- Portal
(4, 15, true, NULL, NULL),
(4, 16, true, 'Unlimited', NULL),
(4, 17, true, NULL, NULL),
(4, 18, true, NULL, NULL),
-- Operations
(4, 19, true, NULL, NULL),
(4, 20, true, NULL, NULL),
(4, 21, true, NULL, NULL),
-- Integration
(4, 22, true, NULL, NULL),
(4, 23, true, NULL, NULL),
-- Production
(4, 24, true, NULL, NULL),
-- Support
(4, 25, true, '1h', 'response'),
(4, 26, true, 'Custom', NULL),
(4, 27, true, NULL, NULL);

-- ============================================================================
-- PRICING FAQS (8 questions)
-- ============================================================================

INSERT INTO public.pricing_faqs (id, question, answer, sort_order, is_active, created_at, updated_at) VALUES
(1, 'What is included in the free trial?',
   'All features of the **Growth tier** are included in our **14-day free trial**. No credit card required. You''ll have full access to TTB compliance, QuickBooks integration, custom reporting, and the investor portal.',
   1, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

(2, 'How does TTB compliance automation work?',
   'Caskr automatically generates **Forms 5110.28** (Processing Operations) and **5110.40** (Storage Operations) based on your barrel transactions. Temperature-corrected proof gallon calculations, gauge records, and excise tax calculations are all handled automatically with a comprehensive audit trail.',
   2, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

(3, 'Is there a discount for annual billing?',
   'Yes, annual billing saves you **20%** compared to monthly billing. This is automatically applied when you select annual billing during signup.',
   3, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

(4, 'Can I upgrade or downgrade my plan?',
   'Yes, you can change your plan at any time. Upgrades take effect immediately with prorated billing. Downgrades take effect at the start of your next billing cycle.',
   4, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

(5, 'How does the QuickBooks integration work?',
   'Caskr connects to **QuickBooks Online** via OAuth 2.0. Invoices sync bi-directionally, and COGS journal entries are created automatically when batches complete. We support 8 account types and provide real-time sync status monitoring.',
   5, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

(6, 'What is the investor portal?',
   'The investor portal allows your cask ownership program participants to log in and view their barrel investments. They can see maturation progress, download ownership certificates, view photos, and track their cask''s journey from fill to bottle.',
   6, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

(7, 'Do you offer discounts for early adopters?',
   'Yes! Our first 25 customers receive **25% off** their first year. Contact sales with code **EARLYBIRD25** to claim this offer.',
   7, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

(8, 'How long does implementation take?',
   'Most distilleries are up and running within **2-4 weeks**. Self-serve onboarding takes about a day for basic setup. Guided onboarding (Growth tier) includes 2 hours of implementation support. White-glove onboarding (Professional tier) includes 8 hours of hands-on assistance.',
   8, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Update sequence to next available ID
SELECT setval('pricing_faqs_id_seq', 8, true);

-- ============================================================================
-- PRICING PROMOTIONS (sample promotions)
-- ============================================================================

INSERT INTO public.pricing_promotions (code, description, discount_type, discount_value, applies_to_tiers, valid_from, valid_until, max_redemptions, is_active, created_at, updated_at) VALUES
('EARLYBIRD25', '25% off first year for early adopters', 'Percentage', 25, NULL, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP + INTERVAL '6 months', 25, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('WELCOME20', '20% off your first year', 'Percentage', 20, NULL, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP + INTERVAL '1 year', 1000, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('ANNUAL2FREE', '2 free months when you sign up for annual billing', 'FreeMonths', 2, NULL, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP + INTERVAL '6 months', 500, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- ============================================================================
-- DOCUMENTATION
-- ============================================================================

COMMENT ON TABLE public.pricing_tiers IS 'US domestic MVP launch pricing: Starter ($699/mo), Growth ($1,699/mo), Professional ($2,999/mo), Enterprise (custom). All include 20% annual discount.';
COMMENT ON TABLE public.pricing_features IS '27 features across 8 categories: Core, Compliance, Financial, Reporting, Portal, Operations, Integration, Production, Support';
COMMENT ON TABLE public.pricing_tier_features IS 'Feature matrix mapping 27 features to 4 tiers with inclusion status and limits';
COMMENT ON TABLE public.pricing_faqs IS '8 FAQs covering trial, compliance, billing, integrations, and implementation';

-- Log successful completion
DO $$
DECLARE
    v_feature_count INTEGER;
    v_tier_count INTEGER;
    v_mapping_count INTEGER;
    v_faq_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO v_feature_count FROM public.pricing_features;
    SELECT COUNT(*) INTO v_tier_count FROM public.pricing_tiers;
    SELECT COUNT(*) INTO v_mapping_count FROM public.pricing_tier_features;
    SELECT COUNT(*) INTO v_faq_count FROM public.pricing_faqs;

    RAISE NOTICE 'Pricing seed completed: % features, % tiers, % mappings, % FAQs',
        v_feature_count, v_tier_count, v_mapping_count, v_faq_count;
END $$;
