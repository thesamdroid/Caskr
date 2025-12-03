--
-- Seed data: Initial pricing tiers, features, and FAQs
-- Date: 2025-12-03
-- Description: Populates pricing tables with initial data for Craft, Growth, Professional, and Enterprise tiers
--

-- Insert pricing features by category
-- Compliance features
INSERT INTO public.pricing_features (name, description, category, sort_order, is_active) VALUES
('TTB Forms (5110.28, 5110.40)', 'Core monthly production and operations reports required by TTB', 'Compliance', 1, true),
('Full TTB Automation', 'Automated generation and submission of all TTB compliance forms', 'Compliance', 2, true),
('TTB Compliance Automation', 'Complete automation of TTB regulatory compliance workflow', 'Compliance', 3, true);

-- Inventory features
INSERT INTO public.pricing_features (name, description, category, sort_order, is_active) VALUES
('Production Tracking', 'Track production batches from grain to bottle', 'Inventory', 1, true),
('Barrel Management', 'Full barrel inventory tracking with rickhouse location', 'Inventory', 2, true),
('Production Planning', 'Advanced production planning and scheduling tools', 'Inventory', 3, true);

-- Reporting features
INSERT INTO public.pricing_features (name, description, category, sort_order, is_active) VALUES
('Basic Reporting', 'Standard production and inventory reports', 'Reporting', 1, true),
('Advanced Reports + Custom Builder', 'Full report library with custom report builder', 'Reporting', 2, true),
('Custom Dashboards', 'Build custom dashboards with real-time metrics', 'Reporting', 3, true);

-- Integration features
INSERT INTO public.pricing_features (name, description, category, sort_order, is_active) VALUES
('QuickBooks Integration', 'Two-way sync with QuickBooks Online for accounting', 'Integration', 1, true),
('API Access', 'REST API access for custom integrations', 'Integration', 2, true),
('Webhooks', 'Real-time event notifications to external systems', 'Integration', 3, true),
('SSO/SAML', 'Single sign-on with your identity provider', 'Integration', 4, true),
('Custom Integrations', 'Dedicated integration support for enterprise systems', 'Integration', 5, true);

-- Access features
INSERT INTO public.pricing_features (name, description, category, sort_order, is_active) VALUES
('Users', 'Number of user accounts included', 'Access', 1, true),
('Production Limit', 'Annual production volume limit (cases/year)', 'Access', 2, true),
('Mobile Web Access', 'Access Caskr from mobile devices via web browser', 'Access', 3, true),
('Customer Portals', 'Branded customer-facing portals for cask owners', 'Access', 4, true),
('On-Premise Option', 'Deploy Caskr on your own infrastructure', 'Access', 5, true);

-- Support features
INSERT INTO public.pricing_features (name, description, category, sort_order, is_active) VALUES
('Email Support', 'Support via email during business hours', 'Support', 1, true),
('Priority Support', 'Expedited support with faster response times', 'Support', 2, true),
('Dedicated Success Manager', 'Personal account manager for your organization', 'Support', 3, true),
('24/7 Phone Support', 'Round-the-clock phone support for critical issues', 'Support', 4, true),
('Uptime SLA', 'Guaranteed service availability level', 'Support', 5, true);

-- Insert pricing tiers
INSERT INTO public.pricing_tiers (name, slug, tagline, monthly_price_cents, annual_price_cents, annual_discount_percent, is_popular, is_custom_pricing, cta_text, cta_url, sort_order, is_active) VALUES
('Craft', 'craft', 'Perfect for craft distilleries', 29900, 287040, 20, false, false, 'Start Free Trial', '/signup?plan=craft', 1, true),
('Growth', 'growth', 'Scale your operations confidently', 149900, 1439040, 20, true, false, 'Start Free Trial', '/signup?plan=growth', 2, true),
('Professional', 'professional', 'Enterprise-grade for large operations', 399900, 3839040, 20, false, false, 'Start Free Trial', '/signup?plan=professional', 3, true),
('Enterprise', 'enterprise', 'Custom solutions for the largest distilleries', NULL, NULL, 0, false, true, 'Contact Sales', '/contact?plan=enterprise', 4, true);

-- Get tier IDs for feature mapping
-- We'll use subqueries to reference the IDs

-- Craft Tier Features
INSERT INTO public.pricing_tier_features (tier_id, feature_id, is_included, limit_value, limit_description)
SELECT t.id, f.id, true, '10,000 cases/year', NULL
FROM public.pricing_tiers t, public.pricing_features f
WHERE t.slug = 'craft' AND f.name = 'Production Limit';

INSERT INTO public.pricing_tier_features (tier_id, feature_id, is_included, limit_value, limit_description)
SELECT t.id, f.id, true, '5 users', NULL
FROM public.pricing_tiers t, public.pricing_features f
WHERE t.slug = 'craft' AND f.name = 'Users';

INSERT INTO public.pricing_tier_features (tier_id, feature_id, is_included, limit_value, limit_description)
SELECT t.id, f.id, true, NULL, NULL
FROM public.pricing_tiers t, public.pricing_features f
WHERE t.slug = 'craft' AND f.name = 'TTB Forms (5110.28, 5110.40)';

INSERT INTO public.pricing_tier_features (tier_id, feature_id, is_included, limit_value, limit_description)
SELECT t.id, f.id, true, '10 reports', NULL
FROM public.pricing_tiers t, public.pricing_features f
WHERE t.slug = 'craft' AND f.name = 'Basic Reporting';

INSERT INTO public.pricing_tier_features (tier_id, feature_id, is_included, limit_value, limit_description)
SELECT t.id, f.id, true, NULL, NULL
FROM public.pricing_tiers t, public.pricing_features f
WHERE t.slug = 'craft' AND f.name = 'Production Tracking';

INSERT INTO public.pricing_tier_features (tier_id, feature_id, is_included, limit_value, limit_description)
SELECT t.id, f.id, true, NULL, NULL
FROM public.pricing_tiers t, public.pricing_features f
WHERE t.slug = 'craft' AND f.name = 'Barrel Management';

INSERT INTO public.pricing_tier_features (tier_id, feature_id, is_included, limit_value, limit_description)
SELECT t.id, f.id, true, NULL, NULL
FROM public.pricing_tiers t, public.pricing_features f
WHERE t.slug = 'craft' AND f.name = 'Email Support';

INSERT INTO public.pricing_tier_features (tier_id, feature_id, is_included, limit_value, limit_description)
SELECT t.id, f.id, true, '99.5%', NULL
FROM public.pricing_tiers t, public.pricing_features f
WHERE t.slug = 'craft' AND f.name = 'Uptime SLA';

-- Features NOT included in Craft
INSERT INTO public.pricing_tier_features (tier_id, feature_id, is_included, limit_value, limit_description)
SELECT t.id, f.id, false, NULL, NULL
FROM public.pricing_tiers t, public.pricing_features f
WHERE t.slug = 'craft' AND f.name IN ('Full TTB Automation', 'QuickBooks Integration', 'Advanced Reports + Custom Builder', 'Mobile Web Access', 'Priority Support', 'Production Planning', 'Customer Portals', 'API Access', 'Webhooks', 'Dedicated Success Manager');


-- Growth Tier Features
INSERT INTO public.pricing_tier_features (tier_id, feature_id, is_included, limit_value, limit_description)
SELECT t.id, f.id, true, '100,000 cases/year', NULL
FROM public.pricing_tiers t, public.pricing_features f
WHERE t.slug = 'growth' AND f.name = 'Production Limit';

INSERT INTO public.pricing_tier_features (tier_id, feature_id, is_included, limit_value, limit_description)
SELECT t.id, f.id, true, '25 users', NULL
FROM public.pricing_tiers t, public.pricing_features f
WHERE t.slug = 'growth' AND f.name = 'Users';

INSERT INTO public.pricing_tier_features (tier_id, feature_id, is_included, limit_value, limit_description)
SELECT t.id, f.id, true, NULL, NULL
FROM public.pricing_tiers t, public.pricing_features f
WHERE t.slug = 'growth' AND f.name IN ('TTB Forms (5110.28, 5110.40)', 'Full TTB Automation', 'Production Tracking', 'Barrel Management', 'QuickBooks Integration', 'Mobile Web Access', 'Email Support');

INSERT INTO public.pricing_tier_features (tier_id, feature_id, is_included, limit_value, limit_description)
SELECT t.id, f.id, true, '30+ reports', NULL
FROM public.pricing_tiers t, public.pricing_features f
WHERE t.slug = 'growth' AND f.name = 'Advanced Reports + Custom Builder';

INSERT INTO public.pricing_tier_features (tier_id, feature_id, is_included, limit_value, limit_description)
SELECT t.id, f.id, true, '4hr response', NULL
FROM public.pricing_tiers t, public.pricing_features f
WHERE t.slug = 'growth' AND f.name = 'Priority Support';

INSERT INTO public.pricing_tier_features (tier_id, feature_id, is_included, limit_value, limit_description)
SELECT t.id, f.id, true, '99.9%', NULL
FROM public.pricing_tiers t, public.pricing_features f
WHERE t.slug = 'growth' AND f.name = 'Uptime SLA';

-- Features NOT included in Growth
INSERT INTO public.pricing_tier_features (tier_id, feature_id, is_included, limit_value, limit_description)
SELECT t.id, f.id, false, NULL, NULL
FROM public.pricing_tiers t, public.pricing_features f
WHERE t.slug = 'growth' AND f.name IN ('Production Planning', 'Customer Portals', 'API Access', 'Webhooks', 'Dedicated Success Manager', 'SSO/SAML', 'Custom Integrations', '24/7 Phone Support', 'On-Premise Option', 'Custom Dashboards');


-- Professional Tier Features
INSERT INTO public.pricing_tier_features (tier_id, feature_id, is_included, limit_value, limit_description)
SELECT t.id, f.id, true, '500,000 cases/year', NULL
FROM public.pricing_tiers t, public.pricing_features f
WHERE t.slug = 'professional' AND f.name = 'Production Limit';

INSERT INTO public.pricing_tier_features (tier_id, feature_id, is_included, limit_value, limit_description)
SELECT t.id, f.id, true, '100 users', NULL
FROM public.pricing_tiers t, public.pricing_features f
WHERE t.slug = 'professional' AND f.name = 'Users';

INSERT INTO public.pricing_tier_features (tier_id, feature_id, is_included, limit_value, limit_description)
SELECT t.id, f.id, true, NULL, NULL
FROM public.pricing_tiers t, public.pricing_features f
WHERE t.slug = 'professional' AND f.name IN ('TTB Forms (5110.28, 5110.40)', 'Full TTB Automation', 'TTB Compliance Automation', 'Production Tracking', 'Barrel Management', 'Production Planning', 'QuickBooks Integration', 'Mobile Web Access', 'Customer Portals', 'API Access', 'Webhooks', 'Email Support', 'Dedicated Success Manager');

INSERT INTO public.pricing_tier_features (tier_id, feature_id, is_included, limit_value, limit_description)
SELECT t.id, f.id, true, 'Unlimited', NULL
FROM public.pricing_tiers t, public.pricing_features f
WHERE t.slug = 'professional' AND f.name = 'Advanced Reports + Custom Builder';

INSERT INTO public.pricing_tier_features (tier_id, feature_id, is_included, limit_value, limit_description)
SELECT t.id, f.id, true, '1hr response', NULL
FROM public.pricing_tiers t, public.pricing_features f
WHERE t.slug = 'professional' AND f.name = 'Priority Support';

INSERT INTO public.pricing_tier_features (tier_id, feature_id, is_included, limit_value, limit_description)
SELECT t.id, f.id, true, '99.95%', NULL
FROM public.pricing_tiers t, public.pricing_features f
WHERE t.slug = 'professional' AND f.name = 'Uptime SLA';

-- Features NOT included in Professional
INSERT INTO public.pricing_tier_features (tier_id, feature_id, is_included, limit_value, limit_description)
SELECT t.id, f.id, false, NULL, NULL
FROM public.pricing_tiers t, public.pricing_features f
WHERE t.slug = 'professional' AND f.name IN ('SSO/SAML', 'Custom Integrations', '24/7 Phone Support', 'On-Premise Option', 'Custom Dashboards');


-- Enterprise Tier Features (all features included with "Unlimited" or "Custom")
INSERT INTO public.pricing_tier_features (tier_id, feature_id, is_included, limit_value, limit_description)
SELECT t.id, f.id, true, 'Unlimited', NULL
FROM public.pricing_tiers t, public.pricing_features f
WHERE t.slug = 'enterprise' AND f.name IN ('Production Limit', 'Users', 'Advanced Reports + Custom Builder');

INSERT INTO public.pricing_tier_features (tier_id, feature_id, is_included, limit_value, limit_description)
SELECT t.id, f.id, true, 'Custom', NULL
FROM public.pricing_tiers t, public.pricing_features f
WHERE t.slug = 'enterprise' AND f.name = 'Uptime SLA';

INSERT INTO public.pricing_tier_features (tier_id, feature_id, is_included, limit_value, limit_description)
SELECT t.id, f.id, true, NULL, NULL
FROM public.pricing_tiers t, public.pricing_features f
WHERE t.slug = 'enterprise' AND f.name IN ('TTB Forms (5110.28, 5110.40)', 'Full TTB Automation', 'TTB Compliance Automation', 'Production Tracking', 'Barrel Management', 'Production Planning', 'QuickBooks Integration', 'Mobile Web Access', 'Customer Portals', 'API Access', 'Webhooks', 'SSO/SAML', 'Custom Integrations', 'Custom Dashboards', 'Email Support', 'Priority Support', 'Dedicated Success Manager', '24/7 Phone Support', 'On-Premise Option');


-- Insert pricing FAQs
INSERT INTO public.pricing_faqs (question, answer, sort_order, is_active) VALUES
('What is included in the free trial?', 'Our 14-day free trial includes full access to all features in the Growth tier. No credit card required to start. At the end of your trial, you can choose any tier that fits your needs.', 1, true),
('Can I change my plan later?', 'Yes! You can upgrade or downgrade your plan at any time. When upgrading, you''ll have immediate access to new features. When downgrading, changes take effect at the start of your next billing cycle.', 2, true),
('How does annual billing work?', 'Annual billing gives you a 20% discount compared to monthly billing. You pay for 12 months upfront and save the equivalent of over 2 months. Annual plans can be canceled anytime with a prorated refund for unused months.', 3, true),
('What payment methods do you accept?', 'We accept all major credit cards (Visa, MasterCard, American Express, Discover) and ACH bank transfers for annual plans. Enterprise customers can also pay via invoice with NET-30 terms.', 4, true),
('Is my data secure?', 'Absolutely. Caskr uses industry-standard encryption (AES-256 at rest, TLS 1.3 in transit), SOC 2 Type II certified infrastructure, and regular security audits. Your TTB compliance data is stored in secure, US-based data centers.', 5, true),
('Do you offer implementation support?', 'Yes! All plans include onboarding support. Growth and Professional plans include guided setup sessions, and Enterprise plans include dedicated implementation specialists and custom training programs.', 6, true),
('What happens if I exceed my production limit?', 'We''ll notify you when you reach 80% of your limit. If you exceed your limit, you can upgrade to a higher tier or pay for additional capacity on a per-case basis. We never interrupt your service.', 7, true),
('Can I export my data?', 'Yes. You can export all your data at any time in standard formats (CSV, PDF, Excel). Enterprise customers also have API access for automated data extraction and backup.', 8, true),
('Do you integrate with my existing systems?', 'Caskr integrates with QuickBooks Online, popular CRM systems, and offers API access (Professional and Enterprise) for custom integrations. Contact us to discuss specific integration needs.', 9, true),
('What compliance reports are included?', 'All plans include TTB Form 5110.28 (Monthly Report of Production Operations) and Form 5110.40 (Monthly Report of Storage Operations). Higher tiers include automated submission, form history, and audit trail features.', 10, true);


-- Insert sample promotion (for testing)
INSERT INTO public.pricing_promotions (code, description, discount_type, discount_value, applies_to_tiers, valid_from, valid_until, max_redemptions, is_active) VALUES
('WELCOME20', '20% off your first year', 'Percentage', 20, NULL, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP + INTERVAL '1 year', 1000, true),
('EARLYBIRD', '2 free months when you sign up for annual billing', 'FreeMonths', 2, NULL, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP + INTERVAL '6 months', 500, true);

-- Add comment for seed data
COMMENT ON TABLE public.pricing_tiers IS 'Seeded with Craft ($299/mo), Growth ($1,499/mo), Professional ($3,999/mo), and Enterprise (custom) tiers';
