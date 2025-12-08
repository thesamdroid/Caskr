--
-- Demo Seed Data: Caskr Company with 10-Year History
-- Date: 2025-12-08
-- Description: Comprehensive demo data for Caskr company showing full platform usage
--              with shaw@caskr.co user, 10 years of barrel history (~100 barrels/year),
--              TTB compliance data, suppliers, customers, production runs, and more.
--

-- ============================================================================
-- 1. CASKR COMPANY SETUP
-- ============================================================================

-- Insert Caskr company with full extended fields
INSERT INTO public.company (
    id,
    company_name,
    created_date,
    renewal_date,
    address_line1,
    address_line2,
    city,
    state,
    postal_code,
    country,
    phone_number,
    website,
    ttb_permit_number,
    is_active,
    auto_generate_ttb_reports,
    ttb_auto_report_cadence,
    ttb_auto_report_hour_utc,
    ttb_auto_report_day_of_month,
    ttb_auto_report_day_of_week
) VALUES (
    100,
    'Caskr',
    '2014-06-15'::timestamptz,
    CURRENT_TIMESTAMP + INTERVAL '1 year',
    '1234 Distillery Way',
    'Suite 100',
    'Louisville',
    'Kentucky',
    '40202',
    'USA',
    '(502) 555-CASK',
    'https://caskr.co',
    'DSP-KY-20014',
    true,
    true,
    'Monthly',
    8,
    5,
    'Monday'
) ON CONFLICT (id) DO UPDATE SET
    company_name = EXCLUDED.company_name,
    address_line1 = EXCLUDED.address_line1,
    city = EXCLUDED.city,
    state = EXCLUDED.state,
    postal_code = EXCLUDED.postal_code,
    ttb_permit_number = EXCLUDED.ttb_permit_number;

-- ============================================================================
-- 2. USER SETUP - SHAW AND TEAM
-- ============================================================================

-- Main user: Shaw (Admin/Owner)
INSERT INTO public.users (
    id, name, email, user_type_id, company_id, is_primary_contact, is_ttb_contact
) VALUES (
    200, 'Shaw', 'shaw@caskr.co', 1, 100, true, true
) ON CONFLICT (id) DO UPDATE SET
    email = EXCLUDED.email,
    company_id = EXCLUDED.company_id,
    is_primary_contact = EXCLUDED.is_primary_contact,
    is_ttb_contact = EXCLUDED.is_ttb_contact;

-- Team members
INSERT INTO public.users (id, name, email, user_type_id, company_id, is_primary_contact, is_ttb_contact) VALUES
    (201, 'Sarah Mitchell', 'sarah@caskr.co', 2, 100, false, false),
    (202, 'James Cooper', 'james@caskr.co', 3, 100, false, true),
    (203, 'Emily Watson', 'emily@caskr.co', 3, 100, false, false),
    (204, 'Michael Brown', 'michael@caskr.co', 3, 100, false, false),
    (205, 'Jennifer Davis', 'jennifer@caskr.co', 4, 100, false, false),
    (206, 'Robert Wilson', 'robert@caskr.co', 5, 100, false, false)
ON CONFLICT (id) DO UPDATE SET
    company_id = EXCLUDED.company_id;

-- ============================================================================
-- 3. WAREHOUSES - MULTIPLE TYPES
-- ============================================================================

-- Create diverse warehouse types for Caskr
INSERT INTO public.warehouses (id, company_id, name, warehouse_type, address_line1, city, state, postal_code, country, total_capacity, length_feet, width_feet, height_feet, is_active, notes, created_by_user_id) VALUES
    (100, 100, 'Main Rickhouse A', 'Rickhouse', '1234 Distillery Way', 'Louisville', 'Kentucky', '40202', 'USA', 500, 200.00, 100.00, 40.00, true, 'Primary aging facility - 7 tier traditional rickhouse', 200),
    (101, 100, 'Main Rickhouse B', 'Rickhouse', '1234 Distillery Way', 'Louisville', 'Kentucky', '40202', 'USA', 450, 180.00, 90.00, 40.00, true, 'Secondary aging facility - newer construction 2018', 200),
    (102, 100, 'West Campus Rickhouse', 'Rickhouse', '5678 Bourbon Trail', 'Bardstown', 'Kentucky', '40004', 'USA', 600, 220.00, 110.00, 45.00, true, 'Expansion facility opened 2020 - climate controlled lower levels', 200),
    (103, 100, 'Palletized Storage 1', 'Palletized', '1234 Distillery Way', 'Louisville', 'Kentucky', '40202', 'USA', 800, 150.00, 200.00, 25.00, true, 'Modern palletized storage for overflow and young whiskey', 200),
    (104, 100, 'Tank Farm', 'Tank_Farm', '1234 Distillery Way', 'Louisville', 'Kentucky', '40202', 'USA', 200, 100.00, 100.00, 30.00, true, 'Stainless steel tanks for white dog and blending', 200),
    (105, 100, 'Outdoor Yard', 'Outdoor', '1234 Distillery Way', 'Louisville', 'Kentucky', '40202', 'USA', 150, 300.00, 150.00, 0.00, true, 'Seasonal outdoor aging experimental program', 200)
ON CONFLICT (id) DO UPDATE SET
    company_id = EXCLUDED.company_id,
    name = EXCLUDED.name;

-- Also create rickhouse entries for backward compatibility
INSERT INTO public.rickhouse (id, company_id, name, address) VALUES
    (100, 100, 'Main Rickhouse A', '1234 Distillery Way, Louisville, KY 40202'),
    (101, 100, 'Main Rickhouse B', '1234 Distillery Way, Louisville, KY 40202'),
    (102, 100, 'West Campus Rickhouse', '5678 Bourbon Trail, Bardstown, KY 40004'),
    (103, 100, 'Palletized Storage 1', '1234 Distillery Way, Louisville, KY 40202'),
    (104, 100, 'Tank Farm', '1234 Distillery Way, Louisville, KY 40202'),
    (105, 100, 'Outdoor Yard', '1234 Distillery Way, Louisville, KY 40202')
ON CONFLICT (id) DO UPDATE SET
    company_id = EXCLUDED.company_id;

-- ============================================================================
-- 4. MASH BILLS AND BATCHES
-- ============================================================================

-- Create Caskr's mash bills (recipes)
INSERT INTO public.mash_bill (id, company_id, name, component_ids) VALUES
    (100, 100, 'Traditional Bourbon', ARRAY[100,101,102]),
    (101, 100, 'High Rye Bourbon', ARRAY[103,104,105]),
    (102, 100, 'Wheated Bourbon', ARRAY[106,107,108]),
    (103, 100, 'Single Malt', ARRAY[109]),
    (104, 100, 'Rye Whiskey', ARRAY[110,111,112])
ON CONFLICT (id) DO UPDATE SET
    company_id = EXCLUDED.company_id,
    name = EXCLUDED.name;

-- Create components for mash bills
INSERT INTO public.component (id, batch_id, name, percentage) VALUES
    -- Traditional Bourbon (75% corn, 15% rye, 10% malted barley)
    (100, 1, 'Corn', 75),
    (101, 1, 'Rye', 15),
    (102, 1, 'Malted Barley', 10),
    -- High Rye Bourbon (60% corn, 30% rye, 10% malted barley)
    (103, 1, 'Corn', 60),
    (104, 1, 'Rye', 30),
    (105, 1, 'Malted Barley', 10),
    -- Wheated Bourbon (70% corn, 20% wheat, 10% malted barley)
    (106, 1, 'Corn', 70),
    (107, 1, 'Wheat', 20),
    (108, 1, 'Malted Barley', 10),
    -- Single Malt (100% malted barley)
    (109, 1, 'Malted Barley', 100),
    -- Rye Whiskey (95% rye, 5% malted barley)
    (110, 1, 'Rye', 51),
    (111, 1, 'Corn', 39),
    (112, 1, 'Malted Barley', 10)
ON CONFLICT (id) DO UPDATE SET
    name = EXCLUDED.name;

-- Create batches for 10 years of production (2015-2024, ~10 batches per year)
INSERT INTO public.batch (id, company_id, mash_bill_id)
SELECT
    batch_id,
    100,
    CASE (batch_id % 5)
        WHEN 0 THEN 100  -- Traditional Bourbon
        WHEN 1 THEN 100  -- Traditional Bourbon (most common)
        WHEN 2 THEN 101  -- High Rye
        WHEN 3 THEN 102  -- Wheated
        WHEN 4 THEN 104  -- Rye
    END
FROM generate_series(100, 199) AS batch_id
ON CONFLICT (id, company_id) DO NOTHING;

-- ============================================================================
-- 5. ORDERS - 10 YEARS OF PRODUCTION
-- ============================================================================

-- Generate orders for 10 years (2015-2024) - approximately 10 production runs per year
INSERT INTO public.orders (
    id, name, owner_id, status_id, spirit_type_id, batch_id, quantity, company_id,
    created_date, updated_date
)
SELECT
    1000 + gs AS id,
    CASE (gs % 10)
        WHEN 0 THEN 'Spring Release ' || (2015 + (gs / 10))
        WHEN 1 THEN 'Summer Small Batch ' || (2015 + (gs / 10))
        WHEN 2 THEN 'Fall Reserve ' || (2015 + (gs / 10))
        WHEN 3 THEN 'Winter Cask Strength ' || (2015 + (gs / 10))
        WHEN 4 THEN 'Single Barrel Select ' || (2015 + (gs / 10))
        WHEN 5 THEN 'Master Distiller Series ' || (2015 + (gs / 10))
        WHEN 6 THEN 'Limited Edition ' || (2015 + (gs / 10))
        WHEN 7 THEN 'Barrel Proof ' || (2015 + (gs / 10))
        WHEN 8 THEN 'Vintage Collection ' || (2015 + (gs / 10))
        WHEN 9 THEN 'Anniversary Blend ' || (2015 + (gs / 10))
    END AS name,
    200 AS owner_id,
    CASE
        WHEN gs < 80 THEN 6  -- National Listing (completed)
        WHEN gs < 90 THEN 5  -- OHLQ Listing
        WHEN gs < 95 THEN 4  -- Ordering
        ELSE (gs % 3) + 1    -- Mix of early stages
    END AS status_id,
    CASE (gs % 4)
        WHEN 0 THEN 1  -- Bourbon
        WHEN 1 THEN 1  -- Bourbon (most common)
        WHEN 2 THEN 1  -- Bourbon
        WHEN 3 THEN 4  -- Tequila (using as Rye placeholder)
    END AS spirit_type_id,
    100 + (gs % 100) AS batch_id,
    8 + (gs % 5) * 2 AS quantity,  -- 8-16 barrels per order
    100 AS company_id,
    ('2015-01-01'::date + ((gs * 36) || ' days')::interval)::timestamptz AS created_date,
    ('2015-01-01'::date + ((gs * 36) || ' days')::interval)::timestamptz AS updated_date
FROM generate_series(0, 99) AS gs
ON CONFLICT (id) DO UPDATE SET
    company_id = EXCLUDED.company_id;

-- ============================================================================
-- 6. BARRELS - 10 YEARS (~1000 BARRELS)
-- ============================================================================

-- Generate ~1000 barrels over 10 years (100 per year average)
INSERT INTO public.barrel (id, company_id, sku, batch_id, order_id, rickhouse_id, warehouse_id)
SELECT
    1000 + gs AS id,
    100 AS company_id,
    'CASKR-' || LPAD((2015 + (gs / 100))::text, 4, '0') || '-' || LPAD((gs % 1000)::text, 4, '0') AS sku,
    100 + ((gs / 10) % 100) AS batch_id,
    1000 + (gs / 10) AS order_id,
    CASE
        WHEN gs % 6 = 0 THEN 100
        WHEN gs % 6 = 1 THEN 101
        WHEN gs % 6 = 2 THEN 102
        WHEN gs % 6 = 3 THEN 103
        WHEN gs % 6 = 4 THEN 100
        ELSE 101
    END AS rickhouse_id,
    CASE
        WHEN gs % 6 = 0 THEN 100
        WHEN gs % 6 = 1 THEN 101
        WHEN gs % 6 = 2 THEN 102
        WHEN gs % 6 = 3 THEN 103
        WHEN gs % 6 = 4 THEN 100
        ELSE 101
    END AS warehouse_id
FROM generate_series(0, 999) AS gs
ON CONFLICT (id) DO UPDATE SET
    company_id = EXCLUDED.company_id;

-- ============================================================================
-- 7. TTB GAUGE RECORDS - FOR ALL BARRELS
-- ============================================================================

-- Generate initial fill gauge records for all barrels
INSERT INTO public.ttb_gauge_records (
    barrel_id, gauge_date, gauge_type, proof, temperature, wine_gallons, proof_gallons, gauged_by_user_id, notes
)
SELECT
    1000 + gs AS barrel_id,
    ('2015-01-15'::date + ((gs * 3.65) || ' days')::interval)::timestamptz AS gauge_date,
    'Fill' AS gauge_type,
    125.0 + (random() * 10 - 5)::numeric(5,2) AS proof,  -- 120-130 proof typical fill
    65.0 + (random() * 20 - 10)::numeric(5,2) AS temperature,  -- 55-75F
    53.0 + (random() * 2 - 1)::numeric(10,2) AS wine_gallons,  -- ~53 gallon barrel
    CASE (gs % 4) WHEN 0 THEN 200 WHEN 1 THEN 202 WHEN 2 THEN 203 ELSE 204 END AS gauged_by_user_id,
    'Initial fill gauge - new American white oak barrel, #4 char'
FROM generate_series(0, 999) AS gs
ON CONFLICT DO NOTHING;

-- Generate storage gauge records (annual checks) for older barrels
INSERT INTO public.ttb_gauge_records (
    barrel_id, gauge_date, gauge_type, proof, temperature, wine_gallons, proof_gallons, gauged_by_user_id, notes
)
SELECT
    1000 + barrel_gs AS barrel_id,
    ('2016-01-15'::date + ((barrel_gs * 3.65) || ' days')::interval + (year_gs || ' years')::interval)::timestamptz AS gauge_date,
    'Storage' AS gauge_type,
    -- Proof increases over time due to evaporation (angel's share)
    127.0 + (year_gs * 1.5) + (random() * 4 - 2)::numeric(5,2) AS proof,
    68.0 + (random() * 15 - 7)::numeric(5,2) AS temperature,
    -- Volume decreases ~3-5% per year due to angel's share
    (53.0 - (year_gs * 2.0) + (random() * 2 - 1))::numeric(10,2) AS wine_gallons,
    CASE (barrel_gs % 4) WHEN 0 THEN 200 WHEN 1 THEN 202 WHEN 2 THEN 203 ELSE 204 END AS gauged_by_user_id,
    'Annual storage gauge - inventory verification'
FROM generate_series(0, 799) AS barrel_gs
CROSS JOIN generate_series(1, LEAST(9, 9 - (barrel_gs / 100))) AS year_gs
WHERE (barrel_gs / 100) + year_gs <= 9
ON CONFLICT DO NOTHING;

-- ============================================================================
-- 8. TTB MONTHLY REPORTS - 10 YEARS OF COMPLIANCE
-- ============================================================================

-- Generate TTB monthly reports for 10 years (120 reports)
INSERT INTO public.ttb_monthly_reports (
    company_id, report_month, report_year, status, generated_at, submitted_at,
    ttb_confirmation_number, pdf_path, created_by_user_id,
    submitted_for_review_by_user_id, submitted_for_review_at,
    reviewed_by_user_id, reviewed_at,
    approved_by_user_id, approved_at
)
SELECT
    100 AS company_id,
    ((gs % 12) + 1)::smallint AS report_month,
    (2015 + (gs / 12))::integer AS report_year,
    CASE
        WHEN gs < 116 THEN 'Submitted'  -- All past reports submitted
        WHEN gs = 116 THEN 'Approved'   -- Current month approved
        WHEN gs = 117 THEN 'PendingReview'  -- Next month in review
        ELSE 'Draft'  -- Future months draft
    END AS status,
    ('2015-01-05'::date + ((gs + 1) || ' months')::interval)::timestamptz AS generated_at,
    CASE WHEN gs < 117 THEN ('2015-01-10'::date + ((gs + 1) || ' months')::interval)::timestamptz ELSE NULL END AS submitted_at,
    CASE WHEN gs < 117 THEN 'TTB-' || (2015 + (gs / 12)) || '-' || LPAD(((gs % 12) + 1)::text, 2, '0') || '-' || LPAD((10000 + gs)::text, 6, '0') ELSE NULL END AS ttb_confirmation_number,
    '/reports/ttb/' || (2015 + (gs / 12)) || '/' || LPAD(((gs % 12) + 1)::text, 2, '0') || '/form-5110-28.pdf' AS pdf_path,
    200 AS created_by_user_id,
    CASE WHEN gs < 118 THEN 202 ELSE NULL END AS submitted_for_review_by_user_id,
    CASE WHEN gs < 118 THEN ('2015-01-03'::date + ((gs + 1) || ' months')::interval)::timestamptz ELSE NULL END AS submitted_for_review_at,
    CASE WHEN gs < 117 THEN 201 ELSE NULL END AS reviewed_by_user_id,
    CASE WHEN gs < 117 THEN ('2015-01-04'::date + ((gs + 1) || ' months')::interval)::timestamptz ELSE NULL END AS reviewed_at,
    CASE WHEN gs < 117 THEN 200 ELSE NULL END AS approved_by_user_id,
    CASE WHEN gs < 117 THEN ('2015-01-05'::date + ((gs + 1) || ' months')::interval)::timestamptz ELSE NULL END AS approved_at
FROM generate_series(0, 119) AS gs
ON CONFLICT DO NOTHING;

-- ============================================================================
-- 9. TTB TRANSACTIONS - PRODUCTION AND OPERATIONS
-- ============================================================================

-- Generate production transactions (when barrels are filled)
INSERT INTO public.ttb_transactions (
    company_id, transaction_date, transaction_type, product_type, spirits_type,
    proof_gallons, wine_gallons, source_entity_type, source_entity_id, notes
)
SELECT
    100 AS company_id,
    ('2015-01-15'::date + ((gs * 3.65) || ' days')::interval)::date AS transaction_date,
    'Production' AS transaction_type,
    'Whiskey' AS product_type,
    'Under190Proof' AS spirits_type,
    (53.0 * 1.25)::numeric(12,2) AS proof_gallons,  -- ~53 gallons at ~125 proof
    53.0 AS wine_gallons,
    'Barrel' AS source_entity_type,
    1000 + gs AS source_entity_id,
    'New barrel filled - production run'
FROM generate_series(0, 999) AS gs
ON CONFLICT DO NOTHING;

-- Generate some transfer transactions (warehouse moves)
INSERT INTO public.ttb_transactions (
    company_id, transaction_date, transaction_type, product_type, spirits_type,
    proof_gallons, wine_gallons, source_entity_type, notes
)
SELECT
    100 AS company_id,
    ('2018-06-01'::date + ((gs * 30) || ' days')::interval)::date AS transaction_date,
    CASE WHEN gs % 2 = 0 THEN 'Transfer_Out' ELSE 'Transfer_In' END AS transaction_type,
    'Whiskey' AS product_type,
    'Under190Proof' AS spirits_type,
    (48.0 * 1.30 * (5 + (gs % 10)))::numeric(12,2) AS proof_gallons,
    (48.0 * (5 + (gs % 10)))::numeric(12,2) AS wine_gallons,
    'Transfer' AS source_entity_type,
    'Inter-warehouse transfer - ' || CASE WHEN gs % 2 = 0 THEN 'to West Campus' ELSE 'from Main Rickhouse' END
FROM generate_series(0, 49) AS gs
ON CONFLICT DO NOTHING;

-- Generate tax determination transactions (when whiskey is removed for bottling)
INSERT INTO public.ttb_transactions (
    company_id, transaction_date, transaction_type, product_type, spirits_type,
    proof_gallons, wine_gallons, source_entity_type, notes
)
SELECT
    100 AS company_id,
    ('2019-01-15'::date + ((gs * 45) || ' days')::interval)::date AS transaction_date,
    'TaxDetermination' AS transaction_type,
    'Whiskey' AS product_type,
    'Under190Proof' AS spirits_type,
    (45.0 * 1.35 * (3 + (gs % 5)))::numeric(12,2) AS proof_gallons,
    (45.0 * (3 + (gs % 5)))::numeric(12,2) AS wine_gallons,
    'Order' AS source_entity_type,
    'Tax determination for bottling - ' || CASE (gs % 4)
        WHEN 0 THEN 'Spring Release'
        WHEN 1 THEN 'Small Batch'
        WHEN 2 THEN 'Single Barrel'
        ELSE 'Reserve Collection'
    END
FROM generate_series(0, 39) AS gs
ON CONFLICT DO NOTHING;

-- ============================================================================
-- 10. TTB INVENTORY SNAPSHOTS - MONTHLY
-- ============================================================================

-- Generate monthly inventory snapshots
INSERT INTO public.ttb_inventory_snapshots (
    company_id, snapshot_date, product_type, spirits_type, proof_gallons, wine_gallons, tax_status
)
SELECT
    100 AS company_id,
    ('2015-01-31'::date + ((gs) || ' months')::interval)::date AS snapshot_date,
    'Whiskey' AS product_type,
    'Under190Proof' AS spirits_type,
    -- Inventory grows over time as production exceeds bottling
    (10000 + (gs * 400) + (random() * 500 - 250))::numeric(12,2) AS proof_gallons,
    (8000 + (gs * 300) + (random() * 400 - 200))::numeric(12,2) AS wine_gallons,
    'Bonded' AS tax_status
FROM generate_series(0, 119) AS gs
ON CONFLICT DO NOTHING;

-- ============================================================================
-- 11. SUPPLIERS
-- ============================================================================

INSERT INTO public.suppliers (id, company_id, supplier_name, supplier_type, contact_person, email, phone, address, website, payment_terms, is_active, notes) VALUES
    (100, 100, 'Kentucky Grain Co', 'Grain', 'John Miller', 'john@kygrain.example.com', '(502) 555-1001', '100 Farm Road, Lexington, KY 40505', 'https://kygrain.example.com', 'Net 30', true, 'Primary corn and rye supplier - certified non-GMO'),
    (101, 100, 'Blue Grass Cooperage', 'Cooperage', 'Mary Thompson', 'mary@bgcooperage.example.com', '(502) 555-1002', '200 Barrel Lane, Louisville, KY 40210', 'https://bgcooperage.example.com', 'Net 45', true, 'Premium American white oak barrels - #3 and #4 char'),
    (102, 100, 'Bluegrass Bottles', 'Bottles', 'Robert Chen', 'robert@bgbottles.example.com', '(502) 555-1003', '300 Glass Street, Louisville, KY 40220', 'https://bgbottles.example.com', 'Net 30', true, 'Custom bourbon bottles and closures'),
    (103, 100, 'Heritage Label Co', 'Labels', 'Lisa Garcia', 'lisa@heritagelabel.example.com', '(502) 555-1004', '400 Print Ave, Cincinnati, OH 45202', 'https://heritagelabel.example.com', 'Net 30', true, 'Premium labels and packaging materials'),
    (104, 100, 'Distillery Equipment Inc', 'Equipment', 'David Park', 'david@distequip.example.com', '(502) 555-1005', '500 Industrial Blvd, Nashville, TN 37210', 'https://distequip.example.com', 'Net 60', true, 'Stills, fermenters, and distillery equipment'),
    (105, 100, 'CleanChem Solutions', 'Chemicals', 'Jennifer White', 'jennifer@cleanchem.example.com', '(502) 555-1006', '600 Chemical Way, Indianapolis, IN 46220', 'https://cleanchem.example.com', 'Net 30', true, 'Cleaning and sanitation chemicals'),
    (106, 100, 'Midwest Malt House', 'Grain', 'William Brown', 'william@midwestmalt.example.com', '(502) 555-1007', '700 Malt Road, Madison, WI 53701', 'https://midwestmalt.example.com', 'Net 30', true, 'Specialty malted barley and wheat')
ON CONFLICT (id) DO UPDATE SET company_id = EXCLUDED.company_id;

-- ============================================================================
-- 12. SUPPLIER PRODUCTS
-- ============================================================================

INSERT INTO public.supplier_products (id, supplier_id, product_name, product_category, sku, unit_of_measure, current_price, currency, lead_time_days, minimum_order_quantity, is_active) VALUES
    -- Grain suppliers
    (100, 100, 'Yellow Dent Corn #2', 'Grain', 'YDC-002', 'bushel', 7.50, 'USD', 7, 500, true),
    (101, 100, 'Winter Rye', 'Grain', 'WRY-001', 'bushel', 9.25, 'USD', 7, 200, true),
    (102, 106, 'Two-Row Malted Barley', 'Grain', 'TRMB-001', 'lb', 0.85, 'USD', 14, 2000, true),
    (103, 106, 'Soft Red Winter Wheat', 'Grain', 'SRWW-001', 'bushel', 8.00, 'USD', 7, 300, true),
    -- Cooperage
    (104, 101, '53 Gallon Bourbon Barrel #4 Char', 'Cooperage', 'BB53-4', 'each', 275.00, 'USD', 30, 25, true),
    (105, 101, '53 Gallon Bourbon Barrel #3 Char', 'Cooperage', 'BB53-3', 'each', 265.00, 'USD', 30, 25, true),
    (106, 101, '30 Gallon Quarter Cask', 'Cooperage', 'QC30-4', 'each', 185.00, 'USD', 21, 10, true),
    -- Bottles
    (107, 102, '750ml Bourbon Bottle - Classic', 'Bottles', 'BTL750C', 'case', 48.00, 'USD', 14, 50, true),
    (108, 102, '750ml Bourbon Bottle - Premium', 'Bottles', 'BTL750P', 'case', 72.00, 'USD', 21, 25, true),
    (109, 102, 'Cork Closure - Natural', 'Closures', 'CORK-N', 'case', 85.00, 'USD', 14, 20, true),
    -- Labels
    (110, 103, 'Front Label - Standard', 'Labels', 'LBL-STD', 'roll', 125.00, 'USD', 10, 10, true),
    (111, 103, 'Back Label - Standard', 'Labels', 'LBL-BCK', 'roll', 95.00, 'USD', 10, 10, true),
    (112, 103, 'Neck Label - Foil', 'Labels', 'LBL-NCK', 'roll', 150.00, 'USD', 14, 5, true),
    -- Equipment
    (113, 104, 'Barrel Rack - 4 Tier', 'Equipment', 'RACK-4T', 'each', 450.00, 'USD', 45, 5, true),
    (114, 104, 'Digital Hydrometer', 'Equipment', 'HYD-DIG', 'each', 325.00, 'USD', 14, 1, true),
    -- Chemicals
    (115, 105, 'CIP Alkaline Cleaner', 'Chemicals', 'CIP-ALK', 'gallon', 35.00, 'USD', 7, 10, true),
    (116, 105, 'Sanitizer Solution', 'Chemicals', 'SAN-001', 'gallon', 28.00, 'USD', 7, 10, true)
ON CONFLICT (id) DO UPDATE SET supplier_id = EXCLUDED.supplier_id;

-- ============================================================================
-- 13. PURCHASE ORDERS
-- ============================================================================

-- Generate purchase orders over 10 years
INSERT INTO public.purchase_orders (
    id, company_id, supplier_id, po_number, order_date, expected_delivery_date,
    status, total_amount, currency, payment_status, notes, created_by_user_id
)
SELECT
    100 + gs AS id,
    100 AS company_id,
    CASE (gs % 7)
        WHEN 0 THEN 100  -- Grain
        WHEN 1 THEN 101  -- Cooperage
        WHEN 2 THEN 102  -- Bottles
        WHEN 3 THEN 103  -- Labels
        WHEN 4 THEN 100  -- Grain
        WHEN 5 THEN 101  -- Cooperage
        ELSE 106         -- Malt
    END AS supplier_id,
    'PO-' || LPAD((2015 + (gs / 15))::text, 4, '0') || '-' || LPAD((gs + 1)::text, 4, '0') AS po_number,
    ('2015-01-10'::date + ((gs * 24) || ' days')::interval)::date AS order_date,
    ('2015-01-10'::date + ((gs * 24 + 21) || ' days')::interval)::date AS expected_delivery_date,
    CASE
        WHEN gs < 140 THEN 'Received'::purchase_order_status
        WHEN gs < 145 THEN 'Partial_Received'::purchase_order_status
        WHEN gs < 148 THEN 'Confirmed'::purchase_order_status
        ELSE 'Sent'::purchase_order_status
    END AS status,
    (1000 + (gs % 50) * 200 + random() * 500)::numeric(12,2) AS total_amount,
    'USD' AS currency,
    CASE WHEN gs < 135 THEN 'Paid'::payment_status WHEN gs < 145 THEN 'Partial'::payment_status ELSE 'Unpaid'::payment_status END AS payment_status,
    'Regular supply order - ' || CASE (gs % 7)
        WHEN 0 THEN 'Corn and Rye'
        WHEN 1 THEN 'New Barrels'
        WHEN 2 THEN 'Bottle order'
        WHEN 3 THEN 'Labels'
        WHEN 4 THEN 'Grain restock'
        WHEN 5 THEN 'Barrel reorder'
        ELSE 'Malt order'
    END,
    CASE (gs % 3) WHEN 0 THEN 200 WHEN 1 THEN 201 ELSE 202 END AS created_by_user_id
FROM generate_series(0, 149) AS gs
ON CONFLICT (id) DO UPDATE SET company_id = EXCLUDED.company_id;

-- ============================================================================
-- 14. CUSTOMERS
-- ============================================================================

INSERT INTO public.customers (
    id, company_id, customer_name, customer_type, email, phone, website,
    address_line1, city, state, postal_code, country, assigned_user_id, is_active, notes
) VALUES
    (100, 100, 'Louisville Liquors', 'OffPremise', 'orders@louisvilleliquors.example.com', '(502) 555-2001', 'https://louisvilleliquors.example.com', '123 Main St', 'Louisville', 'Kentucky', '40202', 'USA', 205, true, 'Premium retail partner - flagship store'),
    (101, 100, 'The Bourbon Room', 'OnPremise', 'bar@bourbonroom.example.com', '(502) 555-2002', 'https://bourbonroom.example.com', '456 Whiskey Row', 'Louisville', 'Kentucky', '40202', 'USA', 205, true, 'High-end bourbon bar - exclusive pours'),
    (102, 100, 'Kentucky Distributors Inc', 'Distributor', 'orders@kydist.example.com', '(502) 555-2003', 'https://kydist.example.com', '789 Distribution Way', 'Louisville', 'Kentucky', '40210', 'USA', 205, true, 'Primary state distributor'),
    (103, 100, 'Bluegrass Wine & Spirits', 'Distributor', 'sales@bluegrasswine.example.com', '(859) 555-2004', 'https://bluegrasswine.example.com', '321 Commerce Dr', 'Lexington', 'Kentucky', '40505', 'USA', 205, true, 'Regional distributor - Central KY'),
    (104, 100, 'The Whiskey Exchange', 'OffPremise', 'buyer@whiskeyexchange.example.com', '(312) 555-2005', 'https://whiskeyexchange.example.com', '555 State St', 'Chicago', 'Illinois', '60601', 'USA', 205, true, 'National retailer - single barrel program'),
    (105, 100, 'Proof Bar & Restaurant', 'OnPremise', 'manager@proofbar.example.com', '(502) 555-2006', 'https://proofbar.example.com', '777 4th Street', 'Louisville', 'Kentucky', '40202', 'USA', 205, true, 'Farm-to-table restaurant - cocktail focus'),
    (106, 100, 'Capitol Spirits', 'OffPremise', 'orders@capitolspirits.example.com', '(202) 555-2007', 'https://capitolspirits.example.com', '999 K Street NW', 'Washington', 'District of Columbia', '20001', 'USA', 205, true, 'DC flagship retailer'),
    (107, 100, 'Southern Wine & Spirits', 'Distributor', 'orders@swsdist.example.com', '(305) 555-2008', 'https://swsdist.example.com', '1200 Distribution Blvd', 'Miami', 'Florida', '33101', 'USA', 205, true, 'Southeast distributor - FL, GA, SC'),
    (108, 100, 'Highland Investment Group', 'Investor', 'investments@highland.example.com', '(415) 555-2009', 'https://highland.example.com', '100 California St', 'San Francisco', 'California', '94111', 'USA', 200, true, 'Cask investment partner - 50 barrel program'),
    (109, 100, 'Oak & Grain Investments', 'Investor', 'casks@oakgrain.example.com', '(212) 555-2010', 'https://oakgrain.example.com', '350 Park Ave', 'New York', 'New York', '10022', 'USA', 200, true, 'Private cask investment club'),
    (110, 100, 'Westside Wine Merchants', 'OffPremise', 'orders@westsidewine.example.com', '(310) 555-2011', 'https://westsidewine.example.com', '8500 Santa Monica Blvd', 'West Hollywood', 'California', '90069', 'USA', 205, true, 'LA premium spirits retailer'),
    (111, 100, 'The Still House', 'OnPremise', 'info@stillhouse.example.com', '(615) 555-2012', 'https://stillhouse.example.com', '200 Broadway', 'Nashville', 'Tennessee', '37201', 'USA', 205, true, 'Nashville bourbon bar'),
    (112, 100, 'Premium Spirits USA', 'Distributor', 'national@premiumspirits.example.com', '(646) 555-2013', 'https://premiumspirits.example.com', '1 World Trade Center', 'New York', 'New York', '10007', 'USA', 205, true, 'National craft spirits distributor')
ON CONFLICT (id) DO UPDATE SET company_id = EXCLUDED.company_id;

-- ============================================================================
-- 15. PORTAL USERS - INVESTORS
-- ============================================================================

INSERT INTO public.portal_users (
    id, email, password_hash, first_name, last_name, phone, company_id,
    is_active, email_verified, is_cask_investor
) VALUES
    (100, 'james.wellington@example.com', '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/X4.TqWXpBq8kqvKqy', 'James', 'Wellington', '(415) 555-3001', 100, true, true, true),
    (101, 'sophia.chen@example.com', '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/X4.TqWXpBq8kqvKqy', 'Sophia', 'Chen', '(212) 555-3002', 100, true, true, true),
    (102, 'marcus.thompson@example.com', '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/X4.TqWXpBq8kqvKqy', 'Marcus', 'Thompson', '(310) 555-3003', 100, true, true, true),
    (103, 'emma.rodriguez@example.com', '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/X4.TqWXpBq8kqvKqy', 'Emma', 'Rodriguez', '(617) 555-3004', 100, true, true, true),
    (104, 'william.park@example.com', '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/X4.TqWXpBq8kqvKqy', 'William', 'Park', '(206) 555-3005', 100, true, true, true),
    (105, 'olivia.jackson@example.com', '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/X4.TqWXpBq8kqvKqy', 'Olivia', 'Jackson', '(303) 555-3006', 100, true, true, true),
    (106, 'ethan.miller@example.com', '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/X4.TqWXpBq8kqvKqy', 'Ethan', 'Miller', '(512) 555-3007', 100, true, true, true),
    (107, 'ava.wilson@example.com', '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/X4.TqWXpBq8kqvKqy', 'Ava', 'Wilson', '(404) 555-3008', 100, true, true, true),
    (108, 'alexander.brown@example.com', '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/X4.TqWXpBq8kqvKqy', 'Alexander', 'Brown', '(312) 555-3009', 100, true, true, true),
    (109, 'isabella.martinez@example.com', '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/X4.TqWXpBq8kqvKqy', 'Isabella', 'Martinez', '(786) 555-3010', 100, true, true, true)
ON CONFLICT (id) DO UPDATE SET company_id = EXCLUDED.company_id;

-- ============================================================================
-- 16. CASK OWNERSHIPS - INVESTOR BARRELS
-- ============================================================================

-- Link investors to barrels they own
INSERT INTO public.cask_ownerships (
    portal_user_id, barrel_id, purchase_date, purchase_price, ownership_percentage,
    certificate_number, status, notes
)
SELECT
    100 + (gs % 10) AS portal_user_id,
    1000 + (500 + gs) AS barrel_id,  -- More recent barrels for investors
    ('2020-01-15'::date + ((gs * 15) || ' days')::interval)::date AS purchase_date,
    (4500 + (gs % 10) * 250 + random() * 500)::numeric(10,2) AS purchase_price,
    100.00 AS ownership_percentage,
    'CASKR-INV-' || LPAD((2020 + (gs / 50))::text, 4, '0') || '-' || LPAD((gs + 1)::text, 4, '0') AS certificate_number,
    CASE
        WHEN gs < 30 THEN 'Active'
        WHEN gs < 35 THEN 'Matured'
        ELSE 'Active'
    END AS status,
    'Private cask investment - ' || CASE (gs % 5)
        WHEN 0 THEN 'Traditional Bourbon'
        WHEN 1 THEN 'High Rye Bourbon'
        WHEN 2 THEN 'Wheated Bourbon'
        WHEN 3 THEN 'Single Barrel Select'
        ELSE 'Cask Strength'
    END
FROM generate_series(0, 49) AS gs
ON CONFLICT DO NOTHING;

-- ============================================================================
-- 17. PORTAL DOCUMENTS
-- ============================================================================

-- Generate ownership certificates for cask investors
INSERT INTO public.portal_documents (
    cask_ownership_id, document_type, file_name, file_path, file_size_bytes,
    mime_type, uploaded_by_user_id
)
SELECT
    co.id AS cask_ownership_id,
    'Ownership_Certificate' AS document_type,
    'Certificate-' || co.certificate_number || '.pdf' AS file_name,
    '/documents/certificates/' || EXTRACT(YEAR FROM co.purchase_date) || '/' || co.certificate_number || '.pdf' AS file_path,
    125000 + (random() * 25000)::bigint AS file_size_bytes,
    'application/pdf' AS mime_type,
    200 AS uploaded_by_user_id
FROM public.cask_ownerships co
WHERE co.barrel_id >= 1500
ON CONFLICT DO NOTHING;

-- ============================================================================
-- 18. PORTAL NOTIFICATIONS
-- ============================================================================

-- Generate welcome and milestone notifications for investors
INSERT INTO public.portal_notifications (
    portal_user_id, notification_type, title, message, related_barrel_id, is_read, sent_at
)
SELECT
    pu.id AS portal_user_id,
    'Account_Update' AS notification_type,
    'Welcome to Caskr Cask Investment Program' AS title,
    'Welcome to the Caskr family! Your cask investment account has been activated. You can now view your barrel details, track maturation progress, and access ownership documents.' AS message,
    NULL AS related_barrel_id,
    true AS is_read,
    pu.created_at + interval '1 hour' AS sent_at
FROM public.portal_users pu
WHERE pu.company_id = 100
ON CONFLICT DO NOTHING;

-- Barrel milestone notifications
INSERT INTO public.portal_notifications (
    portal_user_id, notification_type, title, message, related_barrel_id, is_read, sent_at
)
SELECT
    co.portal_user_id,
    'Barrel_Milestone' AS notification_type,
    'Your Barrel Has Reached ' || (EXTRACT(YEAR FROM CURRENT_DATE) - EXTRACT(YEAR FROM co.purchase_date))::int || ' Years!' AS title,
    'Congratulations! Your cask (#' || b.sku || ') has been aging beautifully. Our master distiller notes excellent flavor development with notes of caramel, vanilla, and oak.' AS message,
    co.barrel_id AS related_barrel_id,
    CASE WHEN random() > 0.3 THEN true ELSE false END AS is_read,
    (co.purchase_date + interval '1 year')::timestamptz AS sent_at
FROM public.cask_ownerships co
JOIN public.barrel b ON co.barrel_id = b.id
WHERE b.company_id = 100
ON CONFLICT DO NOTHING;

-- ============================================================================
-- 19. INTER-WAREHOUSE TRANSFERS
-- ============================================================================

-- Create some inter-warehouse transfers
INSERT INTO public.inter_warehouse_transfers (
    id, from_warehouse_id, to_warehouse_id, transfer_date, barrels_count,
    proof_gallons, status, initiated_by_user_id, completed_at, notes
)
SELECT
    100 + gs AS id,
    CASE (gs % 3) WHEN 0 THEN 100 WHEN 1 THEN 101 ELSE 103 END AS from_warehouse_id,
    CASE (gs % 3) WHEN 0 THEN 102 WHEN 1 THEN 100 ELSE 101 END AS to_warehouse_id,
    ('2018-06-01'::date + ((gs * 60) || ' days')::interval)::date AS transfer_date,
    5 + (gs % 10) AS barrels_count,
    ((5 + (gs % 10)) * 48.0 * 1.30)::numeric(12,2) AS proof_gallons,
    CASE WHEN gs < 35 THEN 'Completed'::warehouse_transfer_status ELSE 'In_Transit'::warehouse_transfer_status END AS status,
    CASE (gs % 3) WHEN 0 THEN 200 WHEN 1 THEN 202 ELSE 203 END AS initiated_by_user_id,
    CASE WHEN gs < 35 THEN ('2018-06-01'::date + ((gs * 60 + 3) || ' days')::interval)::timestamptz ELSE NULL END AS completed_at,
    'Rotation transfer - ' || CASE (gs % 4)
        WHEN 0 THEN 'Seasonal rotation for even aging'
        WHEN 1 THEN 'Capacity optimization'
        WHEN 2 THEN 'Quality management relocation'
        ELSE 'Maturation program movement'
    END AS notes
FROM generate_series(0, 39) AS gs
ON CONFLICT (id) DO UPDATE SET from_warehouse_id = EXCLUDED.from_warehouse_id;

-- ============================================================================
-- 20. WAREHOUSE CAPACITY SNAPSHOTS
-- ============================================================================

-- Generate daily capacity snapshots for the past year
INSERT INTO public.warehouse_capacity_snapshots (
    warehouse_id, snapshot_date, total_capacity, occupied_positions, occupancy_percentage
)
SELECT
    w.id AS warehouse_id,
    ('2024-01-01'::date + ((day_gs) || ' days')::interval)::date AS snapshot_date,
    w.total_capacity AS total_capacity,
    LEAST(w.total_capacity, (w.total_capacity * (0.6 + random() * 0.3))::int) AS occupied_positions,
    LEAST(100, ((0.6 + random() * 0.3) * 100)::numeric(5,2)) AS occupancy_percentage
FROM public.warehouses w
CROSS JOIN generate_series(0, 340) AS day_gs
WHERE w.company_id = 100
ON CONFLICT (warehouse_id, snapshot_date) DO UPDATE SET
    occupied_positions = EXCLUDED.occupied_positions,
    occupancy_percentage = EXCLUDED.occupancy_percentage;

-- ============================================================================
-- 21. TASKS FOR ORDERS
-- ============================================================================

-- Generate tasks for orders
INSERT INTO public.tasks (order_id, name, completed_date)
SELECT
    o.id AS order_id,
    task_name,
    CASE
        WHEN o.status_id >= task_status THEN o.created_date + ((task_status * 14) || ' days')::interval
        ELSE NULL
    END AS completed_date
FROM public.orders o
CROSS JOIN (VALUES
    (1, 'Determination of Spirit'),
    (1, 'Determination of Style'),
    (1, 'Proof Target'),
    (2, 'UPC Assignment'),
    (2, 'Bottle Label Design'),
    (2, 'Case Label Design'),
    (3, 'TTB Formula Submission'),
    (3, 'COLA Application'),
    (4, 'Glass Order'),
    (4, 'Label Printing'),
    (5, 'OHLQ Product Listing'),
    (6, 'National Distribution Setup')
) AS tasks(task_status, task_name)
WHERE o.company_id = 100 AND o.id >= 1000
ON CONFLICT DO NOTHING;

-- ============================================================================
-- 22. ACCOUNTING INTEGRATIONS
-- ============================================================================

INSERT INTO public.accounting_integrations (
    company_id, provider, realm_id, is_active
) VALUES (
    100, 'QuickBooksOnline', 'realm-caskr-123456', true
) ON CONFLICT DO NOTHING;

-- ============================================================================
-- 23. CRM INTEGRATIONS
-- ============================================================================

INSERT INTO public.crm_integrations (
    company_id, provider, instance_url, organization_id, is_active, connection_status,
    connected_by_user_id, connected_at, last_sync_at
) VALUES (
    100, 'Salesforce', 'https://caskr.my.salesforce.com', '00D5f000000example', true, 'Connected',
    200, '2023-01-15'::timestamptz, CURRENT_TIMESTAMP - interval '2 hours'
) ON CONFLICT (company_id, provider) DO UPDATE SET
    connection_status = EXCLUDED.connection_status,
    last_sync_at = EXCLUDED.last_sync_at;

-- ============================================================================
-- 24. SYNC SEQUENCES
-- ============================================================================

-- Update sequences to avoid conflicts with explicit IDs
SELECT pg_catalog.setval('"Company_id_seq"', GREATEST((SELECT MAX(id) FROM public.company), 100));
SELECT pg_catalog.setval('"Users_id_seq"', GREATEST((SELECT MAX(id) FROM public.users), 206));
SELECT pg_catalog.setval('public.warehouses_id_seq', GREATEST((SELECT MAX(id) FROM public.warehouses), 105));
SELECT pg_catalog.setval('"Rickhouse_id_seq"', GREATEST((SELECT MAX(id) FROM public.rickhouse), 105));
SELECT pg_catalog.setval('"Barrel_id_seq"', GREATEST((SELECT MAX(id) FROM public.barrel), 1999));
SELECT pg_catalog.setval('"Orders_id_seq"', GREATEST((SELECT MAX(id) FROM public.orders), 1099));
SELECT pg_catalog.setval('public.suppliers_id_seq', GREATEST((SELECT MAX(id) FROM public.suppliers), 106));
SELECT pg_catalog.setval('public.supplier_products_id_seq', GREATEST((SELECT MAX(id) FROM public.supplier_products), 116));
SELECT pg_catalog.setval('public.purchase_orders_id_seq', GREATEST((SELECT MAX(id) FROM public.purchase_orders), 249));
SELECT pg_catalog.setval('public.customers_id_seq', GREATEST((SELECT MAX(id) FROM public.customers), 112));
SELECT pg_catalog.setval('public.portal_users_id_seq', GREATEST((SELECT MAX(id) FROM public.portal_users), 109));
SELECT pg_catalog.setval('public.inter_warehouse_transfers_id_seq', GREATEST((SELECT MAX(id) FROM public.inter_warehouse_transfers), 139));

-- ============================================================================
-- SEED DATA COMPLETE
-- ============================================================================
-- Summary:
-- - 1 Company: Caskr (ID: 100) with full TTB and contact details
-- - 7 Users: Shaw + 6 team members
-- - 6 Warehouses: 3 Rickhouses, 1 Palletized, 1 Tank Farm, 1 Outdoor
-- - 5 Mash Bills: Traditional, High Rye, Wheated, Single Malt, Rye
-- - 100 Batches: For 10 years of production
-- - 100 Orders: 10 years of production runs (2015-2024)
-- - 1000 Barrels: ~100 per year over 10 years
-- - 1000+ TTB Gauge Records: Fill and storage gauges
-- - 120 TTB Monthly Reports: 10 years of compliance
-- - 1000+ TTB Transactions: Production, transfers, tax determinations
-- - 120 TTB Inventory Snapshots: Monthly inventory tracking
-- - 7 Suppliers: Grain, Cooperage, Bottles, Labels, Equipment, Chemicals
-- - 17 Supplier Products: Full product catalog
-- - 150 Purchase Orders: 10 years of procurement
-- - 13 Customers: Distributors, retailers, bars, investors
-- - 10 Portal Users: Cask investors
-- - 50 Cask Ownerships: Investor barrel assignments
-- - Portal Documents and Notifications
-- - 40 Inter-Warehouse Transfers
-- - 365 Warehouse Capacity Snapshots
-- - QuickBooks and Salesforce integrations configured
