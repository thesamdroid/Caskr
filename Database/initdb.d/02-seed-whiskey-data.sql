-- Seed whiskey-themed test data

-- User types
INSERT INTO public.user_type (id, name) VALUES
    (1, 'SuperAdmin'),
    (2, 'Admin'),
    (3, 'Distiller'),
    (4, 'Distributor'),
    (5, 'Retailer');

-- Order status values
INSERT INTO public.status (id, name) VALUES
    (1, 'Research & Development'),
    (2, 'Asset Creation'),
    (3, 'TTB Approval'),
    (4, 'Ordering'),
    (5, 'OHLQ Listing'),
    (6, 'National Listing');

-- Spirit types
INSERT INTO public.spirit_type (id, name) VALUES
    (1, 'Bourbon'),
    (2, 'Vodka'),
    (3, 'Gin'),
    (4, 'Tequila');

-- Status tasks
INSERT INTO public.status_task (id, status_id, name) VALUES
    (1, 1, 'Determination of Spirit'),
    (2, 1, 'Determination of Style'),
    (3, 1, 'Infusion or Secondary Cask'),
    (4, 1, 'Proof'),
    (5, 1, 'Bottle'),
    (6, 1, 'Product Finalization'),
    (7, 1, 'Pricing'),
    (8, 2, 'UPC'),
    (9, 2, 'Bottle Labels'),
    (10, 2, 'SCC'),
    (11, 2, 'Case Labels'),
    (12, 2, 'NABCA Label'),
    (13, 2, 'Shipping Cases'),
    (14, 3, 'Formula'),
    (15, 3, 'COLA'),
    (16, 4, 'Glass'),
    (17, 4, 'Corrugated'),
    (18, 4, 'Labels'),
    (19, 5, 'Product Listing'),
    (20, 6, 'Product Listing');

-- Users
INSERT INTO public.users (id, name, email, user_type_id, company_id) VALUES
    (1, 'Alice Johnson', 'alice.johnson.147@example.invalid', 3, 4),
    (2, 'Bob Smith', 'bob.smith.938@example.invalid', 3, 3),
    (3, 'Carol Williams', 'carol.williams.259@example.invalid', 4, 1),
    (4, 'David Brown', 'david.brown.740@example.invalid', 5, 2),
    (5, 'Eve Davis', 'eve.davis.572@example.invalid', 3, 1),
    (6, 'Frank Miller', 'frank.miller.863@example.invalid', 4, 2),
    (7, 'Grace Wilson', 'grace.wilson.314@example.invalid', 5, 3),
    (8, 'Hank Moore', 'hank.moore.485@example.invalid', 3, 4),
    (9, 'Ivy Taylor', 'ivy.taylor.697@example.invalid', 4, 1),
    (10, 'Jake Anderson', 'jake.anderson.208@example.invalid', 5, 2),
    (11, 'Laura Thomas', 'laura.thomas.559@example.invalid', 3, 3),
    (12, 'Mike Jackson', 'mike.jackson.631@example.invalid', 4, 4),
    (13, 'Nina White', 'nina.white.942@example.invalid', 5, 1),
    (14, 'Oscar Harris', 'oscar.harris.173@example.invalid', 3, 2),
    (15, 'Paula Martin', 'paula.martin.384@example.invalid', 4, 3),
    (16, 'Quincy Lee', 'quincy.lee.795@example.invalid', 5, 4),
    (17, 'Rachel Perez', 'rachel.perez.516@example.invalid', 3, 1),
    (18, 'Steve Clark', 'steve.clark.627@example.invalid', 4, 2),
    (19, 'Tina Lewis', 'tina.lewis.838@example.invalid', 5, 3),
    (20, 'Umar Walker', 'umar.walker.249@example.invalid', 3, 4),
    (21, 'Victor Hall', 'victor.hall.460@example.invalid', 4, 1),
    (22, 'Wendy Young', 'wendy.young.571@example.invalid', 5, 2),
    (23, 'Xavier King', 'xavier.king.682@example.invalid', 3, 3),
    (24, 'Yvonne Scott', 'yvonne.scott.793@example.invalid', 4, 4),
    (125, 'Super Admin', 'admin@example.invalid', 1, 1),
    (126, 'Shaw', 'shaw@caskr.co', 1, 1);

-- Companies
INSERT INTO public.company (company_name, primary_contact_id, renewal_date) VALUES
    ('Middle West', 3, CURRENT_TIMESTAMP + INTERVAL '1 year'),
    ('Makers Mark', 4, CURRENT_TIMESTAMP + INTERVAL '1 year'),
    ('Jim Beam', 2, CURRENT_TIMESTAMP + INTERVAL '1 year'),
    ('Jack Daniels', 1, CURRENT_TIMESTAMP + INTERVAL '1 year');

-- Products
INSERT INTO public.products (id, owner_id, notes) VALUES
    (1, 1, 'Tennessee Whiskey'),
    (2, 2, 'Kentucky Straight Bourbon'),
    (3, 3, 'Irish Whiskey');

-- Components
INSERT INTO public.component (id, batch_id, name, percentage) VALUES
    (1, 1, 'Corn', 70),
    (2, 1, 'Rye', 20),
    (3, 1, 'Barley', 10),
    (4, 1, 'Wheat', 5);

-- Mash bills
INSERT INTO public.mash_bill (id, company_id, name, component_ids) VALUES
    (1, 4, 'Standard', ARRAY[1,2,3]),
    (2, 3, 'Standard', ARRAY[1,2,3]),
    (3, 1, 'Standard', ARRAY[1,2,3]),
    (4, 2, 'Standard', ARRAY[1,2,3]);

-- Batches
INSERT INTO public.batch (id, company_id, mash_bill_id) VALUES
    (1, 4, 1),
    (1, 3, 2),
    (1, 1, 3),
    (1, 2, 4);

-- Orders in units of barrels
INSERT INTO public.orders (name, owner_id, status_id, spirit_type_id, batch_id, quantity, company_id) VALUES
    ('Sinatra 2', 1, 1, 1, 1, 100, 4),
    ('Knob 25', 2, 2, 2, 1, 150, 3),
    ('Cameron Mitchel', 3, 3, 3, 1, 200, 1),
    ('92', 4, 4, 4, 1, 250, 2);

-- Tasks for orders
INSERT INTO public.tasks (order_id, name, completed_date) VALUES
    -- Sinatra 2 (Research & Development)
    (1, 'Determination of Spirit', CURRENT_TIMESTAMP),
    (1, 'Proof', NULL),
    -- Knob 25 (Asset Creation)
    (2, 'UPC', CURRENT_TIMESTAMP),
    (2, 'Bottle Labels', NULL),
    -- Cameron Mitchel (TTB Approval)
    (3, 'Formula', CURRENT_TIMESTAMP),
    (3, 'COLA', NULL),
    -- 92 (Ordering)
    (4, 'Glass', CURRENT_TIMESTAMP),
    (4, 'Labels', NULL);

-- Additional bulk test data for comprehensive coverage

-- Generate extra users
INSERT INTO public.users (name, email, user_type_id, company_id)
SELECT CONCAT('Test User ', gs),
       CONCAT('test.user.', gs, '@example.invalid'),
       (gs % 3) + 3,
       (gs % 4) + 1
FROM generate_series(25, 124) AS gs;

-- Generate additional products
INSERT INTO public.products (owner_id, notes)
SELECT ((gs % 124) + 1), CONCAT('Bulk product ', gs)
FROM generate_series(4, 103) AS gs;

-- Generate a large number of orders
INSERT INTO public.orders (name, owner_id, status_id, spirit_type_id, batch_id, quantity, company_id)
SELECT CONCAT('Bulk Order ', gs),
       ((gs % 124) + 1),
       (gs % 6) + 1,
       (gs % 4) + 1,
       1,
       gs * 10,
       (gs % 4) + 1
FROM generate_series(5, 54) AS gs;

-- Tasks for all orders
INSERT INTO public.tasks (order_id, name, completed_date)
SELECT o.id,
       CONCAT('Auto Task ', gs),
       CASE WHEN gs % 2 = 0 THEN CURRENT_TIMESTAMP ELSE NULL END
FROM public.orders o
CROSS JOIN generate_series(1,3) AS gs;

-- Rickhouses per company
INSERT INTO public.rickhouse (company_id, name, address)
SELECT c.id,
       CONCAT('Rickhouse ', c.id, '-', gs),
       CONCAT('Address ', c.id, '-', gs)
FROM public.company c
CROSS JOIN generate_series(1,3) AS gs;

-- Barrels for each order
INSERT INTO public.barrel (company_id, sku, batch_id, order_id, rickhouse_id)
SELECT o.company_id,
       CONCAT('SKU', o.id, '-', gs),
       o.batch_id,
       o.id,
       (SELECT r.id FROM public.rickhouse r WHERE r.company_id = o.company_id ORDER BY r.id LIMIT 1)
FROM public.orders o
CROSS JOIN generate_series(1,5) AS gs;

-- Sync sequences with inserted ids
SELECT pg_catalog.setval('"Users_id_seq"', (SELECT MAX(id) FROM public.users));
SELECT pg_catalog.setval('"Orders_id_seq"', (SELECT MAX(id) FROM public.orders));
SELECT pg_catalog.setval('"Orders_owner_id_seq"', (SELECT MAX(owner_id) FROM public.orders));
SELECT pg_catalog.setval('"Orders_status_id_seq"', (SELECT MAX(status_id) FROM public.orders));
SELECT pg_catalog.setval('"Orders_spirit_type_id_seq"', (SELECT MAX(spirit_type_id) FROM public.orders));
SELECT pg_catalog.setval('"Status_id_seq"', (SELECT MAX(id) FROM public.status));
SELECT pg_catalog.setval('"SpiritType_id_seq"', (SELECT MAX(id) FROM public.spirit_type));
SELECT pg_catalog.setval('"StatusTask_id_seq"', (SELECT MAX(id) FROM public.status_task));
SELECT pg_catalog.setval('"Tasks_id_seq"', (SELECT MAX(id) FROM public.tasks));
SELECT pg_catalog.setval('"Company_id_seq"', (SELECT MAX(id) FROM public.company));
