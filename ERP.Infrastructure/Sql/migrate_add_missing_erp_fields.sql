-- =============================================================================
-- Migracja: BRAKUJĄCE POLA I INDEKSY DLA ERP (faktury, pozycje, oferty, numeracja, drzewko)
-- =============================================================================
-- Zasady:
--   NIE usuwać istniejących kolumn ani indeksów.
--   NIE zmieniać nazw ani znaczenia istniejących danych.
--   TYLKO dodawać brakujące pola/indeksy.
--   Jeśli pole/indeks już istnieje – pominąć (MariaDB: ADD COLUMN/INDEX IF NOT EXISTS).
--
-- Wymagania: MariaDB 10.0.2+ (ADD COLUMN IF NOT EXISTS).
-- Przy ponownym uruchomieniu: skrypt jest idempotentny (IF NOT EXISTS).
-- MySQL (bez IF NOT EXISTS): uruchamiać każdy ALTER osobno; przy "Duplicate column name" pominąć.
-- =============================================================================

-- -----------------------------------------------------------------------------
-- TABELA: faktury
-- -----------------------------------------------------------------------------
-- Kolumny numeracji i drzewka dokumentów; sumy z nagłówka.
-- company_id w tabeli faktury = id_firmy (nie zmieniamy nazwy).
-- -----------------------------------------------------------------------------

ALTER TABLE faktury
  ADD COLUMN IF NOT EXISTS doc_type        VARCHAR(8) NULL,
  ADD COLUMN IF NOT EXISTS doc_year        INT NULL,
  ADD COLUMN IF NOT EXISTS doc_month       INT NULL,
  ADD COLUMN IF NOT EXISTS doc_no          INT NULL,
  ADD COLUMN IF NOT EXISTS doc_full_no     VARCHAR(64) NULL,
  ADD COLUMN IF NOT EXISTS source_offer_id BIGINT NULL,
  ADD COLUMN IF NOT EXISTS parent_doc_id   BIGINT NULL,
  ADD COLUMN IF NOT EXISTS root_doc_id     BIGINT NULL,
  ADD COLUMN IF NOT EXISTS sum_netto       DECIMAL(18,2) NULL DEFAULT 0.00,
  ADD COLUMN IF NOT EXISTS sum_vat         DECIMAL(18,2) NULL DEFAULT 0.00,
  ADD COLUMN IF NOT EXISTS sum_brutto      DECIMAL(18,2) NULL DEFAULT 0.00;

-- Indeksy dla drzewka i wyszukiwania po ofercie.
-- Przy ponownym uruchomieniu: jeśli błąd "Duplicate key name" – pominąć dany ALTER.
-- UNIQUE: numeracja dokumentów (id_firmy = company_id)
ALTER TABLE faktury
  ADD UNIQUE KEY uq_faktury_doc_m (id_firmy, doc_type, doc_year, doc_month, doc_no);

ALTER TABLE faktury ADD INDEX idx_faktury_source_offer (id_firmy, source_offer_id);
ALTER TABLE faktury ADD INDEX idx_faktury_parent_doc   (id_firmy, parent_doc_id);
ALTER TABLE faktury ADD INDEX idx_faktury_root_doc     (id_firmy, root_doc_id);


-- -----------------------------------------------------------------------------
-- TABELA: pozycjefaktury (faktury_pozycje)
-- -----------------------------------------------------------------------------
-- Pola wyliczone: netto_poz, vat_poz, brutto_poz. Nie zmieniamy: ilosc, cena_netto, rabat, stawka_vat.
-- -----------------------------------------------------------------------------

ALTER TABLE pozycjefaktury
  ADD COLUMN IF NOT EXISTS netto_poz  DECIMAL(18,2) NULL,
  ADD COLUMN IF NOT EXISTS vat_poz    DECIMAL(18,2) NULL,
  ADD COLUMN IF NOT EXISTS brutto_poz DECIMAL(18,2) NULL;


-- -----------------------------------------------------------------------------
-- TABELA: oferty (dawniej aoferty)
-- -----------------------------------------------------------------------------
-- Suma brutto w nagłówku (np. do drzewka dokumentów). total_brutto pozostaje bez zmian.
-- -----------------------------------------------------------------------------

ALTER TABLE oferty
  ADD COLUMN IF NOT EXISTS sum_brutto DECIMAL(18,2) NULL DEFAULT 0.00;


-- -----------------------------------------------------------------------------
-- TABELA: doc_counters
-- -----------------------------------------------------------------------------
-- Upewnij się, że tabela ma: company_id, doc_type, year, month, last_no
-- oraz PRIMARY KEY (company_id, doc_type, year, month).
-- Jeśli tabela nie istnieje – utwórz ją skryptem create_doc_counters.sql.
-- -----------------------------------------------------------------------------

-- Kolumna month (jeśli brak) – dla starych baz bez miesięcznej numeracji
ALTER TABLE doc_counters
  ADD COLUMN IF NOT EXISTS month INT NOT NULL DEFAULT 1 AFTER year;

-- Korekta PRIMARY KEY tylko gdy doc_counters miał stary PK bez month.
-- Uruchomić tylko raz; przy ponownym uruchomieniu: "Duplicate primary key" – pominąć.
-- ALTER TABLE doc_counters
--   DROP PRIMARY KEY,
--   ADD PRIMARY KEY (company_id, doc_type, year, month);


-- =============================================================================
-- KONIEC MIGRACJI
-- =============================================================================
-- Lista dodanych pól (jeśli wcześniej brakowały):
--
-- faktury:        doc_type, doc_year, doc_month, doc_no, doc_full_no,
--                 source_offer_id, parent_doc_id, root_doc_id,
--                 sum_netto, sum_vat, sum_brutto
-- pozycjefaktury: netto_poz, vat_poz, brutto_poz
-- oferty:         sum_brutto
-- doc_counters:   month (jeśli brak)
--
-- Indeksy faktury: uq_faktury_doc_m, idx_faktury_source_offer,
--                  idx_faktury_parent_doc, idx_faktury_root_doc
--
-- Nic nie zostało usunięte ani zmienione (tylko dodane).
-- =============================================================================
