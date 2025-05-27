--
-- PostgreSQL database dump
--

-- Dumped from database version 17.2 (Debian 17.2-1.pgdg120+1)
-- Dumped by pg_dump version 17.2 (Debian 17.2-1.pgdg120+1)

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
-- Name: clean_completed_trades(); Type: FUNCTION; Schema: public; Owner: toukage
--

CREATE FUNCTION public.clean_completed_trades() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    DELETE FROM Trades WHERE status = FALSE;
    RETURN NEW;
END;
$$;


ALTER FUNCTION public.clean_completed_trades() OWNER TO toukage;

--
-- Name: create_deck_entry(); Type: FUNCTION; Schema: public; Owner: toukage
--

CREATE FUNCTION public.create_deck_entry() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO Decks (user_id, card_1, card_2, card_3, card_4) 
    VALUES (NEW.id, NULL, NULL, NULL, NULL);
    RETURN NEW;
END;
$$;


ALTER FUNCTION public.create_deck_entry() OWNER TO toukage;

--
-- Name: create_stack_entry(); Type: FUNCTION; Schema: public; Owner: toukage
--

CREATE FUNCTION public.create_stack_entry() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO Stacks (user_id) 
    VALUES (NEW.id);
    RETURN NEW;
END;
$$;


ALTER FUNCTION public.create_stack_entry() OWNER TO toukage;

--
-- Name: create_stats_entry(); Type: FUNCTION; Schema: public; Owner: toukage
--

CREATE FUNCTION public.create_stats_entry() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO Stats (user_id, wins, losses, elo) 
    VALUES (NEW.id, 0, 0, 100);
    RETURN NEW;
END;
$$;


ALTER FUNCTION public.create_stats_entry() OWNER TO toukage;

--
-- Name: create_user_profile_entry(); Type: FUNCTION; Schema: public; Owner: toukage
--

CREATE FUNCTION public.create_user_profile_entry() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO UserProfile (user_id, name, bio, image) 
    VALUES (NEW.id, '', '', '');
    RETURN NEW;
END;
$$;


ALTER FUNCTION public.create_user_profile_entry() OWNER TO toukage;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: cards; Type: TABLE; Schema: public; Owner: toukage
--

CREATE TABLE public.cards (
    card_id text NOT NULL,
    name character varying(50),
    card_type character varying(50),
    card_monster character varying(50),
    card_element character varying(50),
    card_dmg double precision
);


ALTER TABLE public.cards OWNER TO toukage;

--
-- Name: decks; Type: TABLE; Schema: public; Owner: toukage
--

CREATE TABLE public.decks (
    deck_id integer NOT NULL,
    user_id integer,
    card_1 text,
    card_2 text,
    card_3 text,
    card_4 text
);


ALTER TABLE public.decks OWNER TO toukage;

--
-- Name: decks_deck_id_seq; Type: SEQUENCE; Schema: public; Owner: toukage
--

CREATE SEQUENCE public.decks_deck_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.decks_deck_id_seq OWNER TO toukage;

--
-- Name: decks_deck_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: toukage
--

ALTER SEQUENCE public.decks_deck_id_seq OWNED BY public.decks.deck_id;


--
-- Name: packages; Type: TABLE; Schema: public; Owner: toukage
--

CREATE TABLE public.packages (
    package_id integer NOT NULL,
    card_1 text,
    card_2 text,
    card_3 text,
    card_4 text,
    card_5 text
);


ALTER TABLE public.packages OWNER TO toukage;

--
-- Name: packages_package_id_seq; Type: SEQUENCE; Schema: public; Owner: toukage
--

CREATE SEQUENCE public.packages_package_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.packages_package_id_seq OWNER TO toukage;

--
-- Name: packages_package_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: toukage
--

ALTER SEQUENCE public.packages_package_id_seq OWNED BY public.packages.package_id;


--
-- Name: stackcards; Type: TABLE; Schema: public; Owner: toukage
--

CREATE TABLE public.stackcards (
    stack_id integer NOT NULL,
    card_id text NOT NULL
);


ALTER TABLE public.stackcards OWNER TO toukage;

--
-- Name: stacks; Type: TABLE; Schema: public; Owner: toukage
--

CREATE TABLE public.stacks (
    stack_id integer NOT NULL,
    user_id integer NOT NULL
);


ALTER TABLE public.stacks OWNER TO toukage;

--
-- Name: stacks_stack_id_seq; Type: SEQUENCE; Schema: public; Owner: toukage
--

CREATE SEQUENCE public.stacks_stack_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.stacks_stack_id_seq OWNER TO toukage;

--
-- Name: stacks_stack_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: toukage
--

ALTER SEQUENCE public.stacks_stack_id_seq OWNED BY public.stacks.stack_id;


--
-- Name: stats; Type: TABLE; Schema: public; Owner: toukage
--

CREATE TABLE public.stats (
    user_id integer NOT NULL,
    wins integer DEFAULT 0,
    losses integer DEFAULT 0,
    draws integer DEFAULT 0,
    total_games integer GENERATED ALWAYS AS ((wins + losses)) STORED,
    elo integer DEFAULT 100
);


ALTER TABLE public.stats OWNER TO toukage;

--
-- Name: tokens; Type: TABLE; Schema: public; Owner: toukage
--

CREATE TABLE public.tokens (
    user_id integer NOT NULL,
    token character varying(255) NOT NULL
);


ALTER TABLE public.tokens OWNER TO toukage;

--
-- Name: trades; Type: TABLE; Schema: public; Owner: toukage
--

CREATE TABLE public.trades (
    trade_id text NOT NULL,
    user_id integer NOT NULL,
    card_id text NOT NULL,
    card_type character varying(50) NOT NULL,
    min_dmg double precision NOT NULL,
    status boolean DEFAULT true NOT NULL
);


ALTER TABLE public.trades OWNER TO toukage;

--
-- Name: userprofile; Type: TABLE; Schema: public; Owner: toukage
--

CREATE TABLE public.userprofile (
    user_id integer NOT NULL,
    name character varying(50),
    bio text,
    image text
);


ALTER TABLE public.userprofile OWNER TO toukage;

--
-- Name: users; Type: TABLE; Schema: public; Owner: toukage
--

CREATE TABLE public.users (
    id integer NOT NULL,
    username character varying(50) NOT NULL,
    password character varying(255) NOT NULL,
    coins integer DEFAULT 20,
    admin boolean DEFAULT false
);


ALTER TABLE public.users OWNER TO toukage;

--
-- Name: users_id_seq; Type: SEQUENCE; Schema: public; Owner: toukage
--

CREATE SEQUENCE public.users_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.users_id_seq OWNER TO toukage;

--
-- Name: users_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: toukage
--

ALTER SEQUENCE public.users_id_seq OWNED BY public.users.id;


--
-- Name: decks deck_id; Type: DEFAULT; Schema: public; Owner: toukage
--

ALTER TABLE ONLY public.decks ALTER COLUMN deck_id SET DEFAULT nextval('public.decks_deck_id_seq'::regclass);


--
-- Name: packages package_id; Type: DEFAULT; Schema: public; Owner: toukage
--

ALTER TABLE ONLY public.packages ALTER COLUMN package_id SET DEFAULT nextval('public.packages_package_id_seq'::regclass);


--
-- Name: stacks stack_id; Type: DEFAULT; Schema: public; Owner: toukage
--

ALTER TABLE ONLY public.stacks ALTER COLUMN stack_id SET DEFAULT nextval('public.stacks_stack_id_seq'::regclass);


--
-- Name: users id; Type: DEFAULT; Schema: public; Owner: toukage
--

ALTER TABLE ONLY public.users ALTER COLUMN id SET DEFAULT nextval('public.users_id_seq'::regclass);


--
-- Name: cards cards_pkey; Type: CONSTRAINT; Schema: public; Owner: toukage
--

ALTER TABLE ONLY public.cards
    ADD CONSTRAINT cards_pkey PRIMARY KEY (card_id);


--
-- Name: decks decks_pkey; Type: CONSTRAINT; Schema: public; Owner: toukage
--

ALTER TABLE ONLY public.decks
    ADD CONSTRAINT decks_pkey PRIMARY KEY (deck_id);


--
-- Name: packages packages_pkey; Type: CONSTRAINT; Schema: public; Owner: toukage
--

ALTER TABLE ONLY public.packages
    ADD CONSTRAINT packages_pkey PRIMARY KEY (package_id);


--
-- Name: stackcards stackcards_pkey; Type: CONSTRAINT; Schema: public; Owner: toukage
--

ALTER TABLE ONLY public.stackcards
    ADD CONSTRAINT stackcards_pkey PRIMARY KEY (stack_id, card_id);


--
-- Name: stacks stacks_pkey; Type: CONSTRAINT; Schema: public; Owner: toukage
--

ALTER TABLE ONLY public.stacks
    ADD CONSTRAINT stacks_pkey PRIMARY KEY (stack_id);


--
-- Name: stats stats_pkey; Type: CONSTRAINT; Schema: public; Owner: toukage
--

ALTER TABLE ONLY public.stats
    ADD CONSTRAINT stats_pkey PRIMARY KEY (user_id);


--
-- Name: tokens tokens_pkey; Type: CONSTRAINT; Schema: public; Owner: toukage
--

ALTER TABLE ONLY public.tokens
    ADD CONSTRAINT tokens_pkey PRIMARY KEY (user_id);


--
-- Name: trades trades_pkey; Type: CONSTRAINT; Schema: public; Owner: toukage
--

ALTER TABLE ONLY public.trades
    ADD CONSTRAINT trades_pkey PRIMARY KEY (trade_id);


--
-- Name: users users_pkey; Type: CONSTRAINT; Schema: public; Owner: toukage
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_pkey PRIMARY KEY (id);


--
-- Name: users users_username_key; Type: CONSTRAINT; Schema: public; Owner: toukage
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_username_key UNIQUE (username);


--
-- Name: users create_deck_on_user; Type: TRIGGER; Schema: public; Owner: toukage
--

CREATE TRIGGER create_deck_on_user AFTER INSERT ON public.users FOR EACH ROW EXECUTE FUNCTION public.create_deck_entry();


--
-- Name: users create_stack_on_user; Type: TRIGGER; Schema: public; Owner: toukage
--

CREATE TRIGGER create_stack_on_user AFTER INSERT ON public.users FOR EACH ROW EXECUTE FUNCTION public.create_stack_entry();


--
-- Name: users create_stats_on_user; Type: TRIGGER; Schema: public; Owner: toukage
--

CREATE TRIGGER create_stats_on_user AFTER INSERT ON public.users FOR EACH ROW EXECUTE FUNCTION public.create_stats_entry();


--
-- Name: users create_user_profile_on_user; Type: TRIGGER; Schema: public; Owner: toukage
--

CREATE TRIGGER create_user_profile_on_user AFTER INSERT ON public.users FOR EACH ROW EXECUTE FUNCTION public.create_user_profile_entry();


--
-- Name: stackcards fk_card; Type: FK CONSTRAINT; Schema: public; Owner: toukage
--

ALTER TABLE ONLY public.stackcards
    ADD CONSTRAINT fk_card FOREIGN KEY (card_id) REFERENCES public.cards(card_id) ON DELETE CASCADE;


--
-- Name: packages fk_card1; Type: FK CONSTRAINT; Schema: public; Owner: toukage
--

ALTER TABLE ONLY public.packages
    ADD CONSTRAINT fk_card1 FOREIGN KEY (card_1) REFERENCES public.cards(card_id) ON DELETE CASCADE;


--
-- Name: decks fk_card1; Type: FK CONSTRAINT; Schema: public; Owner: toukage
--

ALTER TABLE ONLY public.decks
    ADD CONSTRAINT fk_card1 FOREIGN KEY (card_1) REFERENCES public.cards(card_id) ON DELETE CASCADE;


--
-- Name: packages fk_card2; Type: FK CONSTRAINT; Schema: public; Owner: toukage
--

ALTER TABLE ONLY public.packages
    ADD CONSTRAINT fk_card2 FOREIGN KEY (card_2) REFERENCES public.cards(card_id) ON DELETE CASCADE;


--
-- Name: decks fk_card2; Type: FK CONSTRAINT; Schema: public; Owner: toukage
--

ALTER TABLE ONLY public.decks
    ADD CONSTRAINT fk_card2 FOREIGN KEY (card_2) REFERENCES public.cards(card_id) ON DELETE CASCADE;


--
-- Name: packages fk_card3; Type: FK CONSTRAINT; Schema: public; Owner: toukage
--

ALTER TABLE ONLY public.packages
    ADD CONSTRAINT fk_card3 FOREIGN KEY (card_3) REFERENCES public.cards(card_id) ON DELETE CASCADE;


--
-- Name: decks fk_card3; Type: FK CONSTRAINT; Schema: public; Owner: toukage
--

ALTER TABLE ONLY public.decks
    ADD CONSTRAINT fk_card3 FOREIGN KEY (card_3) REFERENCES public.cards(card_id) ON DELETE CASCADE;


--
-- Name: packages fk_card4; Type: FK CONSTRAINT; Schema: public; Owner: toukage
--

ALTER TABLE ONLY public.packages
    ADD CONSTRAINT fk_card4 FOREIGN KEY (card_4) REFERENCES public.cards(card_id) ON DELETE CASCADE;


--
-- Name: decks fk_card4; Type: FK CONSTRAINT; Schema: public; Owner: toukage
--

ALTER TABLE ONLY public.decks
    ADD CONSTRAINT fk_card4 FOREIGN KEY (card_4) REFERENCES public.cards(card_id) ON DELETE CASCADE;


--
-- Name: packages fk_card5; Type: FK CONSTRAINT; Schema: public; Owner: toukage
--

ALTER TABLE ONLY public.packages
    ADD CONSTRAINT fk_card5 FOREIGN KEY (card_5) REFERENCES public.cards(card_id) ON DELETE CASCADE;


--
-- Name: stackcards fk_stack; Type: FK CONSTRAINT; Schema: public; Owner: toukage
--

ALTER TABLE ONLY public.stackcards
    ADD CONSTRAINT fk_stack FOREIGN KEY (stack_id) REFERENCES public.stacks(stack_id) ON DELETE CASCADE;


--
-- Name: stacks fk_user; Type: FK CONSTRAINT; Schema: public; Owner: toukage
--

ALTER TABLE ONLY public.stacks
    ADD CONSTRAINT fk_user FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;


--
-- Name: decks fk_user; Type: FK CONSTRAINT; Schema: public; Owner: toukage
--

ALTER TABLE ONLY public.decks
    ADD CONSTRAINT fk_user FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;


--
-- Name: userprofile fk_user; Type: FK CONSTRAINT; Schema: public; Owner: toukage
--

ALTER TABLE ONLY public.userprofile
    ADD CONSTRAINT fk_user FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;


--
-- Name: stats fk_user_stats; Type: FK CONSTRAINT; Schema: public; Owner: toukage
--

ALTER TABLE ONLY public.stats
    ADD CONSTRAINT fk_user_stats FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;


--
-- Name: tokens fk_user_token; Type: FK CONSTRAINT; Schema: public; Owner: toukage
--

ALTER TABLE ONLY public.tokens
    ADD CONSTRAINT fk_user_token FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;


--
-- Name: trades trades_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: toukage
--

ALTER TABLE ONLY public.trades
    ADD CONSTRAINT trades_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

