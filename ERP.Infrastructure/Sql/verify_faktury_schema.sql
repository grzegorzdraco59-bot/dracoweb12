-- =============================================================================
-- ETAP 1–2: Weryfikacja schematu faktury (nagłówek + pozycje)
-- MariaDB/MySQL. Uruchomić w kliencie SQL (HeidiSQL, DBeaver, mysql).
-- =============================================================================
-- Wykryta nazwa tabeli pozycji: pozycjefaktury (<FAKTURY_POZYCJE>)
-- Tabela nagłówka: faktury
-- =============================================================================

-- ETAP 1 – wykrycie tabel
SHOW TABLES;
SHOW TABLES LIKE '%fakt%poz%';
SHOW TABLES LIKE '%poz%fakt%';

-- ETAP 2 – kolumny nagłówka i pozycji
SHOW COLUMNS FROM faktury;
SHOW COLUMNS FROM pozycjefaktury;

-- Wymagane na pozycjach: ilosc, cena_netto, rabat, stawka_vat (wejściowe);
-- netto_poz, vat_poz, brutto_poz (wyliczane). Nagłówek: sum_netto, sum_vat, sum_brutto.
