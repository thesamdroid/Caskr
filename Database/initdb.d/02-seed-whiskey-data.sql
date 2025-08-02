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

-- Users
INSERT INTO public.users (id, name, email, user_type_id) VALUES
    (1, 'Jack Daniels', 'shaw.samuelj+caskrtest@gmail.com', 1),
    (2, 'Jim Beam', 'shaw.samuelj+caskrtest@gmail.com', 1),
    (3, 'Jameson', 'shaw.samuelj+caskrtest@gmail.com', 2),
    (4, 'Makers Mark', 'shaw.samuelj+caskrtest@gmail.com', 3);

-- Products
INSERT INTO public.products (id, owner_id, notes) VALUES
    (1, 1, 'Tennessee Whiskey'),
    (2, 2, 'Kentucky Straight Bourbon'),
    (3, 3, 'Irish Whiskey');

-- Orders in units of barrels
INSERT INTO public.orders (name, owner_id, status_id) VALUES
    ('Order 3 barrels of Jack Daniels', 4, 1),
    ('Order 2 barrels of Jim Beam', 4, 2),
    ('Order 5 barrels of Jameson', 4, 3);

-- Sync sequences with inserted ids
SELECT pg_catalog.setval('"Users_id_seq"', (SELECT MAX(id) FROM public.users));
SELECT pg_catalog.setval('"Orders_id_seq"', (SELECT MAX(id) FROM public.orders));
SELECT pg_catalog.setval('"Orders_owner_id_seq"', (SELECT MAX(owner_id) FROM public.orders));
SELECT pg_catalog.setval('"Orders_status_id_seq"', (SELECT MAX(status_id) FROM public.orders));
SELECT pg_catalog.setval('"Status_id_seq"', (SELECT MAX(id) FROM public.status));
