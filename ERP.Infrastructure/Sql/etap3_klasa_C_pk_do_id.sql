-- =============================================================================
-- ETAP 3 – Migracja PK → id (KLASA C: incoming_fk>0, rdzeń/rodzice)
-- Baza: locbd (MariaDB/MySQL)
-- NIE WYKONUJ automatycznie! Wykonuj blok po bloku i waliduj.
-- =============================================================================
-- Metoda: DROP FK w dzieciach → CHANGE COLUMN PK w rodzicu → ADD FK w dzieciach (REFERENCED=id)
-- UWAGA: Uruchom etap3_analiza_klasa_C.sql PRZED migracją.
-- =============================================================================
--
-- Tabele klasy C: faktury, firmy, oferty, operator
-- Tylko operator wymaga migracji (PK=id_operatora → id).
-- faktury, firmy, oferty – PK już = id, pomijamy.
--
-- =============================================================================


-- =============================================================================
-- SEKCJA: TABELE POMINIĘTE (PK już = id) – tylko walidacja
-- =============================================================================

-- faktury – PK już = id
SELECT COUNT(*) AS cnt_faktury FROM faktury;

SELECT COLUMN_NAME, COLUMN_KEY FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'faktury' AND COLUMN_NAME = 'id';

SELECT COUNT(*) AS cnt_pozycjefaktury FROM pozycjefaktury;

-- firmy – PK już = id
SELECT COUNT(*) AS cnt_firmy FROM firmy;

SELECT COLUMN_NAME, COLUMN_KEY FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'firmy' AND COLUMN_NAME = 'id';

SELECT COUNT(*) AS cnt_doc_counters FROM doc_counters;

-- oferty – PK już = id
SELECT COUNT(*) AS cnt_oferty FROM oferty;

SELECT COLUMN_NAME, COLUMN_KEY FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'oferty' AND COLUMN_NAME = 'id';

SELECT COUNT(*) AS cnt_ofert_typozycje FROM ofertypozycje;

-- =============================================================================


-- =============================================================================
-- TABELA: operator
-- =============================================================================
-- a) PK: id_operatora (int) → id
-- b) Tabele dzieci: operator_table_permissions (id_operatora → id_operatora)
--    CONSTRAINT: operator_table_permissions_ibfk_1
-- c) Kolumna FK w dziecku: id_operatora (NIE ZMIENIAMY – zostawiamy jak jest)
-- =============================================================================

-- -----------------------------------------------------------------------------
-- operator: id_operatora -> id
-- -----------------------------------------------------------------------------
-- Komentarz: tabela operator, stary_pk id_operatora -> id

-- Walidacja PRZED:
SELECT COUNT(*) AS cnt_przed_operator FROM operator;

SELECT COUNT(*) AS cnt_przed_operator_table_permissions FROM operator_table_permissions;

-- Sprawdź aktualny PK:
SELECT k.COLUMN_NAME, c.COLUMN_TYPE, c.EXTRA
FROM information_schema.KEY_COLUMN_USAGE k
JOIN information_schema.COLUMNS c ON c.TABLE_SCHEMA=k.TABLE_SCHEMA AND c.TABLE_NAME=k.TABLE_NAME AND c.COLUMN_NAME=k.COLUMN_NAME
WHERE k.TABLE_SCHEMA = DATABASE() AND k.TABLE_NAME = 'operator' AND k.CONSTRAINT_NAME = 'PRIMARY';

-- Sprawdź FK w dziecku:
SELECT CONSTRAINT_NAME, COLUMN_NAME, REFERENCED_TABLE_NAME, REFERENCED_COLUMN_NAME
FROM information_schema.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'operator_table_permissions' AND REFERENCED_TABLE_NAME IS NOT NULL;

START TRANSACTION;

-- Krok a) DROP FK w dzieciach
ALTER TABLE operator_table_permissions DROP FOREIGN KEY operator_table_permissions_ibfk_1;

-- Krok b) Zmiana PK w rodzicu (zachowaj typ, NOT NULL, AUTO_INCREMENT)
ALTER TABLE operator CHANGE COLUMN id_operatora id int(15) NOT NULL AUTO_INCREMENT;

-- Krok c) Odtworzenie FK w dzieciach (REFERENCED_COLUMN = id)
-- Kolumna FK w dziecku: id_operatora (bez zmiany)
ALTER TABLE operator_table_permissions
  ADD CONSTRAINT fk_operator_table_permissions_operator
  FOREIGN KEY (id_operatora) REFERENCES operator(id) ON DELETE CASCADE ON UPDATE RESTRICT;

COMMIT;

-- Walidacja PO:
SELECT COUNT(*) AS cnt_po_operator FROM operator;

SELECT COUNT(*) AS cnt_po_operator_table_permissions FROM operator_table_permissions;

-- Sprawdzenie PK w rodzicu:
SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'operator' AND COLUMN_NAME = 'id';

-- Sprawdzenie FK w dzieciach:
SELECT CONSTRAINT_NAME, COLUMN_NAME, REFERENCED_TABLE_NAME, REFERENCED_COLUMN_NAME
FROM information_schema.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'operator_table_permissions' AND REFERENCED_TABLE_NAME IS NOT NULL;


-- =============================================================================
-- RAPORT KOŃCOWY (wypełnij po migracji)
-- =============================================================================
/*
| Tabela   | Było PK     | Jest | Dzieci (FK usunięto/odtworzono) |
|----------|-------------|------|----------------------------------|
| faktury  | id          | id   | 0 (bez zmian)                    |
| firmy    | id          | id   | 0 (bez zmian)                    |
| oferty   | id          | id   | 0 (bez zmian)                    |
| operator | id_operatora| id   | 1 (operator_table_permissions)    |

Tabele pominięte (PK już = id): faktury, firmy, oferty
Tabele pominięte (composite/brak/nietypowy): (brak)
Cykle FK / self-FK: (brak)
*/
