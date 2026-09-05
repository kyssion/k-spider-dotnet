--
-- PostgreSQL database dump
--

-- Dumped from database version 15.12 (Debian 15.12-1.pgdg120+1)
-- Dumped by pg_dump version 17.2

-- Started on 2025-04-02 12:30:18

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

--
-- TOC entry 230 (class 1255 OID 16485)
-- Name: update_time_func(); Type: FUNCTION; Schema: public; Owner: spider
--

CREATE FUNCTION public.update_time_func() RETURNS trigger
    LANGUAGE plpgsql
    AS $$BEGIN
    NEW.update_time := current_timestamp;
    RETURN NEW;
END;$$;


ALTER FUNCTION public.update_time_func() OWNER TO postgres;

--
-- TOC entry 3451 (class 0 OID 0)
-- Dependencies: 230
-- Name: FUNCTION update_time_func(); Type: COMMENT; Schema: public; Owner: spider
--

COMMENT ON FUNCTION public.update_time_func() IS '更新代码的时候添加的时间戳';


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- TOC entry 215 (class 1259 OID 17619)
-- Name: spider_news_content; Type: TABLE; Schema: public; Owner: spider
--

CREATE TABLE public.spider_news_content (
    id bigint NOT NULL,
    create_time timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    update_time timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    news_url character varying(300),
    news_title character varying(300),
    news_summary text,
    news_from character varying(100),
    news_time timestamp without time zone,
    news_keyword text,
    news_content_json text,
    news_content_text text
);


ALTER TABLE public.spider_news_content OWNER TO postgres;

--
-- TOC entry 3452 (class 0 OID 0)
-- Dependencies: 215
-- Name: TABLE spider_news_content; Type: COMMENT; Schema: public; Owner: spider
--

COMMENT ON TABLE public.spider_news_content IS '新闻信息详情表';


--
-- TOC entry 214 (class 1259 OID 17618)
-- Name: spider_news_content_id_seq; Type: SEQUENCE; Schema: public; Owner: spider
--

CREATE SEQUENCE public.spider_news_content_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.spider_news_content_id_seq OWNER TO postgres;

--
-- TOC entry 3453 (class 0 OID 0)
-- Dependencies: 214
-- Name: spider_news_content_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: spider
--

ALTER SEQUENCE public.spider_news_content_id_seq OWNED BY public.spider_news_content.id;


--
-- TOC entry 219 (class 1259 OID 294686)
-- Name: spider_news_content_origin; Type: TABLE; Schema: public; Owner: spider
--

CREATE TABLE public.spider_news_content_origin (
    id bigint NOT NULL,
    create_time timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    update_time timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    news_url character varying(300) NOT NULL,
    news_origin_content text,
    news_origin_type integer DEFAULT 0 NOT NULL,
    status integer DEFAULT 0 NOT NULL,
    message text
);


ALTER TABLE public.spider_news_content_origin OWNER TO postgres;

--
-- TOC entry 218 (class 1259 OID 294685)
-- Name: spider_news_content_origin_id_seq; Type: SEQUENCE; Schema: public; Owner: spider
--

CREATE SEQUENCE public.spider_news_content_origin_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.spider_news_content_origin_id_seq OWNER TO postgres;

--
-- TOC entry 3454 (class 0 OID 0)
-- Dependencies: 218
-- Name: spider_news_content_origin_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: spider
--

ALTER SEQUENCE public.spider_news_content_origin_id_seq OWNED BY public.spider_news_content_origin.id;


--
-- TOC entry 217 (class 1259 OID 17771)
-- Name: spider_news_image_list; Type: TABLE; Schema: public; Owner: spider
--

CREATE TABLE public.spider_news_image_list (
    id bigint NOT NULL,
    create_time timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    update_time timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    news_url character varying(300),
    image_resource_url character varying(400),
    image_name character varying(100)
);


ALTER TABLE public.spider_news_image_list OWNER TO postgres;

--
-- TOC entry 3455 (class 0 OID 0)
-- Dependencies: 217
-- Name: TABLE spider_news_image_list; Type: COMMENT; Schema: public; Owner: spider
--

COMMENT ON TABLE public.spider_news_image_list IS '爬虫详情中的图片信息记录';


--
-- TOC entry 216 (class 1259 OID 17770)
-- Name: spider_news_image_list_id_seq; Type: SEQUENCE; Schema: public; Owner: spider
--

CREATE SEQUENCE public.spider_news_image_list_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.spider_news_image_list_id_seq OWNER TO postgres;

--
-- TOC entry 3456 (class 0 OID 0)
-- Dependencies: 216
-- Name: spider_news_image_list_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: spider
--

ALTER SEQUENCE public.spider_news_image_list_id_seq OWNED BY public.spider_news_image_list.id;


--
-- TOC entry 221 (class 1259 OID 365486)
-- Name: spider_news_list; Type: TABLE; Schema: public; Owner: spider
--

CREATE TABLE public.spider_news_list (
    id bigint NOT NULL,
    create_time timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    update_time timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    from_media integer,
    news_url character varying(300),
    news_title character varying(300),
    news_summary text,
    news_from character varying(30),
    news_time timestamp without time zone,
    news_download_time timestamp without time zone,
    category integer DEFAULT 0 NOT NULL,
    download_status_code integer DEFAULT 0 NOT NULL
);


ALTER TABLE public.spider_news_list OWNER TO postgres;

--
-- TOC entry 3457 (class 0 OID 0)
-- Dependencies: 221
-- Name: TABLE spider_news_list; Type: COMMENT; Schema: public; Owner: spider
--

COMMENT ON TABLE public.spider_news_list IS '排重抓取信息信息列表';


--
-- TOC entry 3458 (class 0 OID 0)
-- Dependencies: 221
-- Name: COLUMN spider_news_list.category; Type: COMMENT; Schema: public; Owner: spider
--

COMMENT ON COLUMN public.spider_news_list.category IS '新闻类型';


--
-- TOC entry 3459 (class 0 OID 0)
-- Dependencies: 221
-- Name: COLUMN spider_news_list.download_status_code; Type: COMMENT; Schema: public; Owner: spider
--

COMMENT ON COLUMN public.spider_news_list.download_status_code IS '详情数据是否下载 0 没有下载 1 已下载';


--
-- TOC entry 220 (class 1259 OID 365485)
-- Name: spider_news_list_id_seq; Type: SEQUENCE; Schema: public; Owner: spider
--

CREATE SEQUENCE public.spider_news_list_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.spider_news_list_id_seq OWNER TO postgres;

--
-- TOC entry 3460 (class 0 OID 0)
-- Dependencies: 220
-- Name: spider_news_list_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: spider
--

ALTER SEQUENCE public.spider_news_list_id_seq OWNED BY public.spider_news_list.id;


--
-- TOC entry 223 (class 1259 OID 681829)
-- Name: stock_cn_introduction; Type: TABLE; Schema: public; Owner: spider
--

CREATE TABLE public.stock_cn_introduction (
    id bigint NOT NULL,
    create_time timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    update_time timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    stock_id character varying(200),
    stock_name character varying(200),
    exchange_channel integer
);


ALTER TABLE public.stock_cn_introduction OWNER TO postgres;

--
-- TOC entry 225 (class 1259 OID 741805)
-- Name: stock_cn_level1_archived_daily_origin; Type: TABLE; Schema: public; Owner: spider
--

CREATE TABLE public.stock_cn_level1_archived_daily_origin (
    id bigint NOT NULL,
    create_time timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    update_time timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    stock_id character varying(200) NOT NULL,
    exchange_channel integer NOT NULL,
    date date NOT NULL,
    archived text,
    data_from integer
);


ALTER TABLE public.stock_cn_level1_archived_daily_origin OWNER TO postgres;

--
-- TOC entry 3461 (class 0 OID 0)
-- Dependencies: 225
-- Name: TABLE stock_cn_level1_archived_daily_origin; Type: COMMENT; Schema: public; Owner: spider
--

COMMENT ON TABLE public.stock_cn_level1_archived_daily_origin IS '中国股市信息天级别表归档';


--
-- TOC entry 224 (class 1259 OID 741804)
-- Name: stock_cn_level1_archived_daliy_id_seq; Type: SEQUENCE; Schema: public; Owner: spider
--

CREATE SEQUENCE public.stock_cn_level1_archived_daliy_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.stock_cn_level1_archived_daliy_id_seq OWNER TO postgres;

--
-- TOC entry 3462 (class 0 OID 0)
-- Dependencies: 224
-- Name: stock_cn_level1_archived_daliy_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: spider
--

ALTER SEQUENCE public.stock_cn_level1_archived_daliy_id_seq OWNED BY public.stock_cn_level1_archived_daily_origin.id;


--
-- TOC entry 227 (class 1259 OID 741833)
-- Name: stock_hk_level1_archived_daily_origin; Type: TABLE; Schema: public; Owner: spider
--

CREATE TABLE public.stock_hk_level1_archived_daily_origin (
    id bigint NOT NULL,
    create_time timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    update_time timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    stock_id character varying(200) NOT NULL,
    exchange_channel integer NOT NULL,
    date date NOT NULL,
    archived text,
    data_from integer
);


ALTER TABLE public.stock_hk_level1_archived_daily_origin OWNER TO postgres;

--
-- TOC entry 3463 (class 0 OID 0)
-- Dependencies: 227
-- Name: TABLE stock_hk_level1_archived_daily_origin; Type: COMMENT; Schema: public; Owner: spider
--

COMMENT ON TABLE public.stock_hk_level1_archived_daily_origin IS '香港股市信息天级别level1原始数据';


--
-- TOC entry 226 (class 1259 OID 741832)
-- Name: stock_hk_level1_archived_daliy_id_seq; Type: SEQUENCE; Schema: public; Owner: spider
--

CREATE SEQUENCE public.stock_hk_level1_archived_daliy_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.stock_hk_level1_archived_daliy_id_seq OWNER TO postgres;

--
-- TOC entry 3464 (class 0 OID 0)
-- Dependencies: 226
-- Name: stock_hk_level1_archived_daliy_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: spider
--

ALTER SEQUENCE public.stock_hk_level1_archived_daliy_id_seq OWNED BY public.stock_hk_level1_archived_daily_origin.id;


--
-- TOC entry 222 (class 1259 OID 681828)
-- Name: stock_introduction_id_seq; Type: SEQUENCE; Schema: public; Owner: spider
--

CREATE SEQUENCE public.stock_introduction_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.stock_introduction_id_seq OWNER TO postgres;

--
-- TOC entry 3465 (class 0 OID 0)
-- Dependencies: 222
-- Name: stock_introduction_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: spider
--

ALTER SEQUENCE public.stock_introduction_id_seq OWNED BY public.stock_cn_introduction.id;


--
-- TOC entry 229 (class 1259 OID 741846)
-- Name: stock_usa_level1_archived_daliy_origin; Type: TABLE; Schema: public; Owner: spider
--

CREATE TABLE public.stock_usa_level1_archived_daliy_origin (
    id bigint NOT NULL,
    create_time timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    update_time timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    stock_id character varying(200) NOT NULL,
    exchange_channel integer NOT NULL,
    date date NOT NULL,
    archived text,
    data_from integer
);


ALTER TABLE public.stock_usa_level1_archived_daliy_origin OWNER TO postgres;

--
-- TOC entry 3466 (class 0 OID 0)
-- Dependencies: 229
-- Name: TABLE stock_usa_level1_archived_daliy_origin; Type: COMMENT; Schema: public; Owner: spider
--

COMMENT ON TABLE public.stock_usa_level1_archived_daliy_origin IS '美股股市信息天级别level1归档原始数据';


--
-- TOC entry 228 (class 1259 OID 741845)
-- Name: stock_usa_level1_archived_daliy_id_seq; Type: SEQUENCE; Schema: public; Owner: spider
--

CREATE SEQUENCE public.stock_usa_level1_archived_daliy_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.stock_usa_level1_archived_daliy_id_seq OWNER TO postgres;

--
-- TOC entry 3467 (class 0 OID 0)
-- Dependencies: 228
-- Name: stock_usa_level1_archived_daliy_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: spider
--

ALTER SEQUENCE public.stock_usa_level1_archived_daliy_id_seq OWNED BY public.stock_usa_level1_archived_daliy_origin.id;


--
-- TOC entry 3235 (class 2604 OID 17622)
-- Name: spider_news_content id; Type: DEFAULT; Schema: public; Owner: spider
--

ALTER TABLE ONLY public.spider_news_content ALTER COLUMN id SET DEFAULT nextval('public.spider_news_content_id_seq'::regclass);


--
-- TOC entry 3241 (class 2604 OID 294689)
-- Name: spider_news_content_origin id; Type: DEFAULT; Schema: public; Owner: spider
--

ALTER TABLE ONLY public.spider_news_content_origin ALTER COLUMN id SET DEFAULT nextval('public.spider_news_content_origin_id_seq'::regclass);


--
-- TOC entry 3238 (class 2604 OID 17774)
-- Name: spider_news_image_list id; Type: DEFAULT; Schema: public; Owner: spider
--

ALTER TABLE ONLY public.spider_news_image_list ALTER COLUMN id SET DEFAULT nextval('public.spider_news_image_list_id_seq'::regclass);


--
-- TOC entry 3246 (class 2604 OID 365489)
-- Name: spider_news_list id; Type: DEFAULT; Schema: public; Owner: spider
--

ALTER TABLE ONLY public.spider_news_list ALTER COLUMN id SET DEFAULT nextval('public.spider_news_list_id_seq'::regclass);


--
-- TOC entry 3251 (class 2604 OID 681832)
-- Name: stock_cn_introduction id; Type: DEFAULT; Schema: public; Owner: spider
--

ALTER TABLE ONLY public.stock_cn_introduction ALTER COLUMN id SET DEFAULT nextval('public.stock_introduction_id_seq'::regclass);


--
-- TOC entry 3254 (class 2604 OID 741808)
-- Name: stock_cn_level1_archived_daily_origin id; Type: DEFAULT; Schema: public; Owner: spider
--

ALTER TABLE ONLY public.stock_cn_level1_archived_daily_origin ALTER COLUMN id SET DEFAULT nextval('public.stock_cn_level1_archived_daliy_id_seq'::regclass);


--
-- TOC entry 3257 (class 2604 OID 741836)
-- Name: stock_hk_level1_archived_daily_origin id; Type: DEFAULT; Schema: public; Owner: spider
--

ALTER TABLE ONLY public.stock_hk_level1_archived_daily_origin ALTER COLUMN id SET DEFAULT nextval('public.stock_hk_level1_archived_daliy_id_seq'::regclass);


--
-- TOC entry 3260 (class 2604 OID 741849)
-- Name: stock_usa_level1_archived_daliy_origin id; Type: DEFAULT; Schema: public; Owner: spider
--

ALTER TABLE ONLY public.stock_usa_level1_archived_daliy_origin ALTER COLUMN id SET DEFAULT nextval('public.stock_usa_level1_archived_daliy_id_seq'::regclass);


--
-- TOC entry 3272 (class 2606 OID 294696)
-- Name: spider_news_content_origin spider_news_content_origin_pkey; Type: CONSTRAINT; Schema: public; Owner: spider
--

ALTER TABLE ONLY public.spider_news_content_origin
    ADD CONSTRAINT spider_news_content_origin_pkey PRIMARY KEY (id);


--
-- TOC entry 3264 (class 2606 OID 17628)
-- Name: spider_news_content spider_news_content_pkey; Type: CONSTRAINT; Schema: public; Owner: spider
--

ALTER TABLE ONLY public.spider_news_content
    ADD CONSTRAINT spider_news_content_pkey PRIMARY KEY (id);


--
-- TOC entry 3268 (class 2606 OID 17778)
-- Name: spider_news_image_list spider_news_image_list_pkey; Type: CONSTRAINT; Schema: public; Owner: spider
--

ALTER TABLE ONLY public.spider_news_image_list
    ADD CONSTRAINT spider_news_image_list_pkey PRIMARY KEY (id);


--
-- TOC entry 3276 (class 2606 OID 365497)
-- Name: spider_news_list spider_news_list_pkey; Type: CONSTRAINT; Schema: public; Owner: spider
--

ALTER TABLE ONLY public.spider_news_list
    ADD CONSTRAINT spider_news_list_pkey PRIMARY KEY (id);


--
-- TOC entry 3280 (class 2606 OID 681836)
-- Name: stock_cn_introduction stock_cn_introduction_pkey; Type: CONSTRAINT; Schema: public; Owner: spider
--

ALTER TABLE ONLY public.stock_cn_introduction
    ADD CONSTRAINT stock_cn_introduction_pkey PRIMARY KEY (id);


--
-- TOC entry 3285 (class 2606 OID 741814)
-- Name: stock_cn_level1_archived_daily_origin stock_cn_level1_archived_daliy_pkey; Type: CONSTRAINT; Schema: public; Owner: spider
--

ALTER TABLE ONLY public.stock_cn_level1_archived_daily_origin
    ADD CONSTRAINT stock_cn_level1_archived_daliy_pkey PRIMARY KEY (id);


--
-- TOC entry 3290 (class 2606 OID 741842)
-- Name: stock_hk_level1_archived_daily_origin stock_hk_level1_archived_daliy_pkey; Type: CONSTRAINT; Schema: public; Owner: spider
--

ALTER TABLE ONLY public.stock_hk_level1_archived_daily_origin
    ADD CONSTRAINT stock_hk_level1_archived_daliy_pkey PRIMARY KEY (id);


--
-- TOC entry 3294 (class 2606 OID 741855)
-- Name: stock_usa_level1_archived_daliy_origin stock_usa_level1_archived_daliy_pkey; Type: CONSTRAINT; Schema: public; Owner: spider
--

ALTER TABLE ONLY public.stock_usa_level1_archived_daliy_origin
    ADD CONSTRAINT stock_usa_level1_archived_daliy_pkey PRIMARY KEY (id);


--
-- TOC entry 3287 (class 2606 OID 741816)
-- Name: stock_cn_level1_archived_daily_origin uk_cn_daily_stock; Type: CONSTRAINT; Schema: public; Owner: spider
--

ALTER TABLE ONLY public.stock_cn_level1_archived_daily_origin
    ADD CONSTRAINT uk_cn_daily_stock UNIQUE (date, stock_id);


--
-- TOC entry 3292 (class 2606 OID 741844)
-- Name: stock_hk_level1_archived_daily_origin uk_hk_daily_stock; Type: CONSTRAINT; Schema: public; Owner: spider
--

ALTER TABLE ONLY public.stock_hk_level1_archived_daily_origin
    ADD CONSTRAINT uk_hk_daily_stock UNIQUE (date, stock_id);


--
-- TOC entry 3274 (class 2606 OID 294698)
-- Name: spider_news_content_origin uk_news_content_origin_url; Type: CONSTRAINT; Schema: public; Owner: spider
--

ALTER TABLE ONLY public.spider_news_content_origin
    ADD CONSTRAINT uk_news_content_origin_url UNIQUE (news_url);


--
-- TOC entry 3266 (class 2606 OID 118231)
-- Name: spider_news_content uk_news_content_test_url; Type: CONSTRAINT; Schema: public; Owner: spider
--

ALTER TABLE ONLY public.spider_news_content
    ADD CONSTRAINT uk_news_content_test_url UNIQUE (news_url);


--
-- TOC entry 3468 (class 0 OID 0)
-- Dependencies: 3266
-- Name: CONSTRAINT uk_news_content_test_url ON spider_news_content; Type: COMMENT; Schema: public; Owner: spider
--

COMMENT ON CONSTRAINT uk_news_content_test_url ON public.spider_news_content IS '新闻详情来源地址唯一索引';


--
-- TOC entry 3270 (class 2606 OID 206360)
-- Name: spider_news_image_list uk_news_image_url; Type: CONSTRAINT; Schema: public; Owner: spider
--

ALTER TABLE ONLY public.spider_news_image_list
    ADD CONSTRAINT uk_news_image_url UNIQUE (image_resource_url);


--
-- TOC entry 3278 (class 2606 OID 365499)
-- Name: spider_news_list uk_news_url; Type: CONSTRAINT; Schema: public; Owner: spider
--

ALTER TABLE ONLY public.spider_news_list
    ADD CONSTRAINT uk_news_url UNIQUE (news_url);


--
-- TOC entry 3469 (class 0 OID 0)
-- Dependencies: 3278
-- Name: CONSTRAINT uk_news_url ON spider_news_list; Type: COMMENT; Schema: public; Owner: spider
--

COMMENT ON CONSTRAINT uk_news_url ON public.spider_news_list IS '新闻url地址链接全局唯一';


--
-- TOC entry 3282 (class 2606 OID 681838)
-- Name: stock_cn_introduction uk_stock_cn_introduction; Type: CONSTRAINT; Schema: public; Owner: spider
--

ALTER TABLE ONLY public.stock_cn_introduction
    ADD CONSTRAINT uk_stock_cn_introduction UNIQUE (stock_id);


--
-- TOC entry 3296 (class 2606 OID 741857)
-- Name: stock_usa_level1_archived_daliy_origin uk_usa_daily_stock; Type: CONSTRAINT; Schema: public; Owner: spider
--

ALTER TABLE ONLY public.stock_usa_level1_archived_daliy_origin
    ADD CONSTRAINT uk_usa_daily_stock UNIQUE (date, stock_id);


--
-- TOC entry 3283 (class 1259 OID 1033306)
-- Name: stock_cn_id_date; Type: INDEX; Schema: public; Owner: spider
--

CREATE UNIQUE INDEX stock_cn_id_date ON public.stock_cn_level1_archived_daily_origin USING btree (stock_id, date) WITH (deduplicate_items='true');


--
-- TOC entry 3288 (class 1259 OID 1033307)
-- Name: stock_hk_id_date; Type: INDEX; Schema: public; Owner: spider
--

CREATE UNIQUE INDEX stock_hk_id_date ON public.stock_hk_level1_archived_daily_origin USING btree (stock_id, date) WITH (deduplicate_items='true');


--
-- TOC entry 3302 (class 2620 OID 774159)
-- Name: stock_hk_level1_archived_daily_origin update_function; Type: TRIGGER; Schema: public; Owner: spider
--

CREATE TRIGGER update_function BEFORE UPDATE ON public.stock_hk_level1_archived_daily_origin FOR EACH ROW EXECUTE FUNCTION public.update_time_func();


--
-- TOC entry 3297 (class 2620 OID 17708)
-- Name: spider_news_content update_modified_column; Type: TRIGGER; Schema: public; Owner: spider
--

CREATE TRIGGER update_modified_column BEFORE UPDATE ON public.spider_news_content FOR EACH ROW EXECUTE FUNCTION public.update_time_func();


--
-- TOC entry 3470 (class 0 OID 0)
-- Dependencies: 3297
-- Name: TRIGGER update_modified_column ON spider_news_content; Type: COMMENT; Schema: public; Owner: spider
--

COMMENT ON TRIGGER update_modified_column ON public.spider_news_content IS '更新时候自动更新时间';


--
-- TOC entry 3299 (class 2620 OID 294699)
-- Name: spider_news_content_origin update_modified_column; Type: TRIGGER; Schema: public; Owner: spider
--

CREATE TRIGGER update_modified_column BEFORE UPDATE ON public.spider_news_content_origin FOR EACH ROW EXECUTE FUNCTION public.update_time_func();


--
-- TOC entry 3298 (class 2620 OID 17781)
-- Name: spider_news_image_list update_modified_column; Type: TRIGGER; Schema: public; Owner: spider
--

CREATE TRIGGER update_modified_column BEFORE UPDATE ON public.spider_news_image_list FOR EACH ROW EXECUTE FUNCTION public.update_time_func();


--
-- TOC entry 3471 (class 0 OID 0)
-- Dependencies: 3298
-- Name: TRIGGER update_modified_column ON spider_news_image_list; Type: COMMENT; Schema: public; Owner: spider
--

COMMENT ON TRIGGER update_modified_column ON public.spider_news_image_list IS '更新时候自动更新时间';


--
-- TOC entry 3300 (class 2620 OID 365500)
-- Name: spider_news_list update_modified_column; Type: TRIGGER; Schema: public; Owner: spider
--

CREATE TRIGGER update_modified_column BEFORE UPDATE ON public.spider_news_list FOR EACH ROW EXECUTE FUNCTION public.update_time_func();


--
-- TOC entry 3472 (class 0 OID 0)
-- Dependencies: 3300
-- Name: TRIGGER update_modified_column ON spider_news_list; Type: COMMENT; Schema: public; Owner: spider
--

COMMENT ON TRIGGER update_modified_column ON public.spider_news_list IS '更新时候自动更新时间';


--
-- TOC entry 3301 (class 2620 OID 773954)
-- Name: stock_cn_level1_archived_daily_origin update_modified_column; Type: TRIGGER; Schema: public; Owner: spider
--

CREATE TRIGGER update_modified_column BEFORE UPDATE ON public.stock_cn_level1_archived_daily_origin FOR EACH ROW EXECUTE FUNCTION public.update_time_func();


--
-- TOC entry 3303 (class 2620 OID 774160)
-- Name: stock_usa_level1_archived_daliy_origin update_time; Type: TRIGGER; Schema: public; Owner: spider
--

CREATE TRIGGER update_time BEFORE UPDATE ON public.stock_usa_level1_archived_daliy_origin FOR EACH ROW EXECUTE FUNCTION public.update_time_func();


-- Completed on 2025-04-02 12:30:21

--
-- PostgreSQL database dump complete
--

