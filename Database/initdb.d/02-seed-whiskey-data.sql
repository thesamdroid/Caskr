-- Seed whiskey-themed test data

-- User types
INSERT INTO public.user_type (id, name) VALUES
    (1, 'Distiller'),
    (2, 'Distributor'),
    (3, 'Retailer');

-- Order status values
INSERT INTO public.status (id, name) VALUES
    (1, 'Research & Development'),
    (2, 'Asset Creation'),
    (3, 'TTB Approval'),
    (4, 'Ordering'),
    (5, 'OHLQ Listing'),
    (6, 'National Listing');

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
    (1, 'Alice Johnson', 'alice.johnson.147@example.invalid', 1, 4),
    (2, 'Bob Smith', 'bob.smith.938@example.invalid', 1, 3),
    (3, 'Carol Williams', 'carol.williams.259@example.invalid', 2, 1),
    (4, 'David Brown', 'david.brown.740@example.invalid', 3, 2),
    (5, 'Eve Davis', 'eve.davis.572@example.invalid', 1, 1),
    (6, 'Frank Miller', 'frank.miller.863@example.invalid', 2, 2),
    (7, 'Grace Wilson', 'grace.wilson.314@example.invalid', 3, 3),
    (8, 'Hank Moore', 'hank.moore.485@example.invalid', 1, 4),
    (9, 'Ivy Taylor', 'ivy.taylor.697@example.invalid', 2, 1),
    (10, 'Jake Anderson', 'jake.anderson.208@example.invalid', 3, 2),
    (11, 'Laura Thomas', 'laura.thomas.559@example.invalid', 1, 3),
    (12, 'Mike Jackson', 'mike.jackson.631@example.invalid', 2, 4),
    (13, 'Nina White', 'nina.white.942@example.invalid', 3, 1),
    (14, 'Oscar Harris', 'oscar.harris.173@example.invalid', 1, 2),
    (15, 'Paula Martin', 'paula.martin.384@example.invalid', 2, 3),
    (16, 'Quincy Lee', 'quincy.lee.795@example.invalid', 3, 4),
    (17, 'Rachel Perez', 'rachel.perez.516@example.invalid', 1, 1),
    (18, 'Steve Clark', 'steve.clark.627@example.invalid', 2, 2),
    (19, 'Tina Lewis', 'tina.lewis.838@example.invalid', 3, 3),
    (20, 'Umar Walker', 'umar.walker.249@example.invalid', 1, 4),
    (21, 'Victor Hall', 'victor.hall.460@example.invalid', 2, 1),
    (22, 'Wendy Young', 'wendy.young.571@example.invalid', 3, 2),
    (23, 'Xavier King', 'xavier.king.682@example.invalid', 1, 3),
    (24, 'Yvonne Scott', 'yvonne.scott.793@example.invalid', 2, 4);

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

-- Orders in units of barrels
INSERT INTO public.orders (name, owner_id, status_id) VALUES
    ('Sinatra 2', 1, 1),
    ('Knob 25', 2, 2),
    ('Cameron Mitchel', 3, 3),
    ('92', 4, 4);

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

-- Sync sequences with inserted ids
SELECT pg_catalog.setval('"Users_id_seq"', (SELECT MAX(id) FROM public.users));
SELECT pg_catalog.setval('"Orders_id_seq"', (SELECT MAX(id) FROM public.orders));
SELECT pg_catalog.setval('"Orders_owner_id_seq"', (SELECT MAX(owner_id) FROM public.orders));
SELECT pg_catalog.setval('"Orders_status_id_seq"', (SELECT MAX(status_id) FROM public.orders));
SELECT pg_catalog.setval('"Status_id_seq"', (SELECT MAX(id) FROM public.status));
SELECT pg_catalog.setval('"StatusTask_id_seq"', (SELECT MAX(id) FROM public.status_task));
SELECT pg_catalog.setval('"Tasks_id_seq"', (SELECT MAX(id) FROM public.tasks));
SELECT pg_catalog.setval('"Company_id_seq"', (SELECT MAX(id) FROM public.company));
