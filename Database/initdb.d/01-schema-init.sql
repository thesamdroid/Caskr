--
-- PostgreSQL database dump
--

-- Dumped from database version 17.3
-- Dumped by pg_dump version 17.1

-- Started on 2025-02-16 19:36:39

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- TOC entry 217 (class 1259 OID 16389)
-- Name: orders; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.orders (
    id integer NOT NULL,
    name text NOT NULL,
    owner_id integer NOT NULL,
    created_date timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_date timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    status_id integer NOT NULL
);


ALTER TABLE public.orders OWNER TO postgres;

--
-- TOC entry 218 (class 1259 OID 16395)
-- Name: Orders_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public."Orders_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."Orders_id_seq" OWNER TO postgres;

--
-- TOC entry 3443 (class 0 OID 0)
-- Dependencies: 218
-- Name: Orders_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public."Orders_id_seq" OWNED BY public.orders.id;


--
-- TOC entry 219 (class 1259 OID 16396)
-- Name: Orders_owner_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public."Orders_owner_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."Orders_owner_id_seq" OWNER TO postgres;

--
-- TOC entry 3444 (class 0 OID 0)
-- Dependencies: 219
-- Name: Orders_owner_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public."Orders_owner_id_seq" OWNED BY public.orders.owner_id;


--
-- TOC entry 220 (class 1259 OID 16397)
-- Name: Orders_status_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public."Orders_status_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."Orders_status_id_seq" OWNER TO postgres;

--
-- TOC entry 3445 (class 0 OID 0)
-- Dependencies: 220
-- Name: Orders_status_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public."Orders_status_id_seq" OWNED BY public.orders.status_id;


--
-- TOC entry 221 (class 1259 OID 16398)
-- Name: status; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.status (
    id integer NOT NULL,
    name text NOT NULL
);


ALTER TABLE public.status OWNER TO postgres;

--
-- TOC entry 222 (class 1259 OID 16403)
-- Name: Status_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public."Status_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."Status_id_seq" OWNER TO postgres;

--
-- TOC entry 3446 (class 0 OID 0)
-- Dependencies: 222
-- Name: Status_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public."Status_id_seq" OWNED BY public.status.id;


--
-- Name: status_task; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.status_task (
    id integer NOT NULL,
    status_id integer NOT NULL,
    name text NOT NULL
);


ALTER TABLE public.status_task OWNER TO postgres;

--
-- Name: StatusTask_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public."StatusTask_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."StatusTask_id_seq" OWNER TO postgres;

--
-- Name: StatusTask_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public."StatusTask_id_seq" OWNED BY public.status_task.id;


--
-- TOC entry 223 (class 1259 OID 16404)
-- Name: users; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.users (
    id integer NOT NULL,
    name text NOT NULL,
    email text NOT NULL,
    user_type_id integer NOT NULL
);


ALTER TABLE public.users OWNER TO postgres;

--
-- TOC entry 224 (class 1259 OID 16409)
-- Name: Users_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public."Users_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."Users_id_seq" OWNER TO postgres;

--
-- TOC entry 3447 (class 0 OID 0)
-- Dependencies: 224
-- Name: Users_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public."Users_id_seq" OWNED BY public.users.id;


--
-- TOC entry 226 (class 1259 OID 16443)
-- Name: products; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.products (
    id integer NOT NULL,
    owner_id integer NOT NULL,
    notes text,
    created_date timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_date timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


ALTER TABLE public.products OWNER TO postgres;

--
-- TOC entry 225 (class 1259 OID 16431)
-- Name: user_type; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.user_type (
    id integer NOT NULL,
    name text
);


ALTER TABLE public.user_type OWNER TO postgres;

--
-- TOC entry 3273 (class 2604 OID 16410)
-- Name: orders id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.orders ALTER COLUMN id SET DEFAULT nextval('public."Orders_id_seq"'::regclass);


--
-- TOC entry 3274 (class 2604 OID 16411)
-- Name: orders owner_id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.orders ALTER COLUMN owner_id SET DEFAULT nextval('public."Orders_owner_id_seq"'::regclass);


--
-- TOC entry 3276 (class 2604 OID 16412)
-- Name: orders status_id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.orders ALTER COLUMN status_id SET DEFAULT nextval('public."Orders_status_id_seq"'::regclass);


--
-- TOC entry 3277 (class 2604 OID 16413)
-- Name: status id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.status ALTER COLUMN id SET DEFAULT nextval('public."Status_id_seq"'::regclass);

--
-- Name: status_task id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.status_task ALTER COLUMN id SET DEFAULT nextval('public."StatusTask_id_seq"'::regclass);


--
-- TOC entry 3278 (class 2604 OID 16414)
-- Name: users id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.users ALTER COLUMN id SET DEFAULT nextval('public."Users_id_seq"'::regclass);


--
-- TOC entry 3280 (class 2606 OID 16416)
-- Name: orders Orders_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.orders
    ADD CONSTRAINT "Orders_pkey" PRIMARY KEY (id);


--
-- TOC entry 3282 (class 2606 OID 16418)
-- Name: status Status_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.status
    ADD CONSTRAINT "Status_pkey" PRIMARY KEY (id);

--
-- Name: status_task StatusTask_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.status_task
    ADD CONSTRAINT "StatusTask_pkey" PRIMARY KEY (id);


--
-- TOC entry 3284 (class 2606 OID 16420)
-- Name: users Users_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT "Users_pkey" PRIMARY KEY (id);


--
-- TOC entry 3288 (class 2606 OID 16449)
-- Name: products products_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.products
    ADD CONSTRAINT products_pkey PRIMARY KEY (id);


--
-- TOC entry 3286 (class 2606 OID 16437)
-- Name: user_type user_type_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.user_type
    ADD CONSTRAINT user_type_pkey PRIMARY KEY (id);


--
-- TOC entry 3292 (class 2606 OID 16450)
-- Name: products fk_owner_users; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.products
    ADD CONSTRAINT fk_owner_users FOREIGN KEY (owner_id) REFERENCES public.users(id);

--
-- Name: status_task fk_statustask_statusid; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.status_task
    ADD CONSTRAINT fk_statustask_statusid FOREIGN KEY (status_id) REFERENCES public.status(id);


--
-- TOC entry 3289 (class 2606 OID 16421)
-- Name: orders fk_ownerid_userid; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.orders
    ADD CONSTRAINT fk_ownerid_userid FOREIGN KEY (owner_id) REFERENCES public.users(id);


--
-- TOC entry 3290 (class 2606 OID 16426)
-- Name: orders fk_statusid_stautusid; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.orders
    ADD CONSTRAINT fk_statusid_stautusid FOREIGN KEY (status_id) REFERENCES public.status(id) NOT VALID;


--
-- TOC entry 3291 (class 2606 OID 16438)
-- Name: users fk_user_userstatus; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT fk_user_userstatus FOREIGN KEY (user_type_id) REFERENCES public.user_type(id) NOT VALID;

--
-- Name: task; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.task (
    id integer NOT NULL,
    order_id integer NOT NULL,
    name text NOT NULL,
    created_date timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_date timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    completed_date timestamp with time zone
);


ALTER TABLE public.task OWNER TO postgres;

--
-- Name: Task_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public."Task_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."Task_id_seq" OWNER TO postgres;

--
-- Name: Task_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public."Task_id_seq" OWNED BY public.task.id;

--
-- Name: task id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.task ALTER COLUMN id SET DEFAULT nextval('public."Task_id_seq"'::regclass);

--
-- Name: task Task_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.task
    ADD CONSTRAINT "Task_pkey" PRIMARY KEY (id);

--
-- Name: task fk_task_orderid; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.task
    ADD CONSTRAINT fk_task_orderid FOREIGN KEY (order_id) REFERENCES public.orders(id);


-- Completed on 2025-02-16 19:36:39

--
-- PostgreSQL database dump complete
--

