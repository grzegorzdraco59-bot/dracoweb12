-- =============================================================================
-- ETAP 2 – Migracja PK → id (KLASA B: incoming_fk=0, outgoing_fk>0)
-- Baza: locbd (MariaDB/MySQL)
-- NIE WYKONUJ automatycznie! Wykonuj blok po bloku i waliduj.
-- =============================================================================
-- Metoda: DROP FK → CHANGE COLUMN (zachowaj typ/NOT NULL/AUTO_INCREMENT) → ADD FK
-- UWAGA: Uruchom etap2_analiza_klasa_B.sql PRZED migracją – zweryfikuj FK, PK i incoming_fk.
-- =============================================================================
--
-- Lista tabel KLASA B (z raportu RAPORT_FK_PK_KLASY.txt):
--   doc_counters, ofertypozycje, operator_table_permissions, pozycjefaktury
--
-- UWAGA: Tabela pozycji ofert może nazywać się apozycjeoferty lub ofertypozycje.
--        Uruchom analizę i dostosuj nazwy w skrypcie.
-- =============================================================================


-- =============================================================================
-- SEKCJA: TABELE POMINIĘTE
-- =============================================================================
--
-- doc_counters – composite PK (company_id, doc_type, year, month) → POMIŃ
--   Zasady: tabele z composite PK lub brakiem PK → pomiń i opisz w raporcie.
--
-- operator_table_permissions – PK już = id → tylko walidacja, brak zmian
--
-- Jeśli tabela ma incoming_fk>0 (sprzeczność z klasą B) → oznacz jako KLASA C, przerwij.
-- =============================================================================


-- =============================================================================
-- KROK 0: WERYFIKACJA incoming_fk (przed migracją)
-- =============================================================================
-- Uruchom etap2_analiza_klasa_B.sql sekcja 3.
-- Dla każdej tabeli incoming_fk musi być 0. Jeśli > 0 → KLASA C, nie migruj.
-- =============================================================================


-- =============================================================================
-- TABELA: ofertypozycje (lub apozycjeoferty – sprawdź nazwę: SHOW TABLES LIKE '%pozycje%oferty%')
-- =============================================================================
-- a) PK: ID_pozycja_oferty (int(15)) lub id – jeśli już id, pominąć blok migracji
-- b) FK wychodzące: ID_oferta → aoferty(ID_oferta) lub oferta_id → oferty(id)
--    CONSTRAINT: fk_apozycjeoferty_aoferty lub fk_ofertypozycje_oferty (z analizy)
-- =============================================================================

-- -----------------------------------------------------------------------------
-- ofertypozycje / apozycjeoferty: ID_pozycja_oferty -> id (pomiń jeśli PK już = id)
-- -----------------------------------------------------------------------------
-- Komentarz: tabela ofertypozycje (lub apozycjeoferty), stary_pk ID_pozycja_oferty -> id
-- UWAGA: Jeśli tabela nazywa się apozycjeoferty, zamień wszystkie "ofert typozycje" na "apozycjeoferty"

-- Walidacja PRZED:
SELECT COUNT(*) AS cnt_przed FROM ofertypozycje;

-- Sprawdź aktualny PK (jeśli COLUMN_NAME='id' → pominąć migrację):
-- SELECT k.COLUMN_NAME, c.COLUMN_TYPE, c.EXTRA FROM information_schema.KEY_COLUMN_USAGE k
-- JOIN information_schema.COLUMNS c ON c.TABLE_SCHEMA=k.TABLE_SCHEMA AND c.TABLE_NAME=k.TABLE_NAME AND c.COLUMN_NAME=k.COLUMN_NAME
-- WHERE k.TABLE_SCHEMA=DATABASE() AND k.TABLE_NAME='ofert typozycje' AND k.CONSTRAINT_NAME='PRIMARY';

-- Pobierz nazwy FK (uruchom przed migracją, wpisz wynik do DROP):
-- SELECT CONSTRAINT_NAME, COLUMN_NAME, REFERENCED_TABLE_NAME, REFERENCED_COLUMN_NAME
-- FROM information_schema.KEY_COLUMN_USAGE
-- WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ofert typozycje' AND REFERENCED_TABLE_NAME IS NOT NULL;

START TRANSACTION;

-- Krok 1: Usuń FK wychodzące (nazwa z analizy – typowo: fk_apozycjeoferty_aoferty)
ALTER TABLE ofertypozycje DROP FOREIGN KEY fk_apozycjeoferty_aoferty;
-- Wariant jeśli tabela po rename: fk_ofertypozycje_oferty
-- Jeśli błąd "check that it exists": SHOW CREATE TABLE ofertypozycje;

-- Krok 2: Zmień nazwę PK na id (zachowaj typ, NOT NULL, AUTO_INCREMENT)
ALTER TABLE ofertypozycje CHANGE COLUMN ID_pozycja_oferty id int(15) NOT NULL AUTO_INCREMENT;

-- Krok 3: Odtwórz FK (dostosuj do stanu bazy: aoferty→oferty, ID_oferta→oferta_id jeśli rename wykonany)
ALTER TABLE ofertypozycje
  ADD CONSTRAINT fk_ofertypozycje_oferty
  FOREIGN KEY (ID_oferta) REFERENCES aoferty(ID_oferta) ON DELETE CASCADE ON UPDATE CASCADE;
-- Wariant po rename aoferty→oferty: FOREIGN KEY (oferta_id) REFERENCES oferty(id) ON DELETE CASCADE ON UPDATE CASCADE

COMMIT;

-- Walidacja PO:
SELECT COUNT(*) AS cnt_po FROM ofertypozycje;

-- Sprawdzenie: PK jest na id
SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ofert typozycje' AND COLUMN_NAME = 'id';

-- Sprawdzenie: FK istnieją
SELECT CONSTRAINT_NAME, COLUMN_NAME, REFERENCED_TABLE_NAME, REFERENCED_COLUMN_NAME
FROM information_schema.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ofert typozycje' AND REFERENCED_TABLE_NAME IS NOT NULL;


-- =============================================================================
-- TABELA: operator_table_permissions
-- =============================================================================
-- a) PK: id (już poprawny) – BRAK ZMIAN
-- b) FK wychodzące: id_operatora → operator(id) ON DELETE CASCADE
-- =============================================================================

-- -----------------------------------------------------------------------------
-- operator_table_permissions: PK już = id → tylko walidacja
-- -----------------------------------------------------------------------------
-- Komentarz: tabela operator_table_permissions, było_pk id -> id (bez zmian)

-- Walidacja:
SELECT COUNT(*) AS cnt_operator_table_permissions FROM operator_table_permissions;

-- Sprawdzenie PK:
SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'operator_table_permissions' AND COLUMN_NAME = 'id';
-- Oczekiwane: COLUMN_KEY=PRI, EXTRA=auto_increment

-- Sprawdzenie FK:
SELECT CONSTRAINT_NAME, COLUMN_NAME, REFERENCED_TABLE_NAME, REFERENCED_COLUMN_NAME
FROM information_schema.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'operator_table_permissions' AND REFERENCED_TABLE_NAME IS NOT NULL;


-- =============================================================================
-- TABELA: pozycjefaktury
-- =============================================================================
-- a) PK: id_pozycji_faktury (int(15)) lub id – jeśli już id, pominąć blok migracji
-- b) FK wychodzące: (np. id_faktury→faktury, id_firmy→firmy, id_oferty→oferty) – z analizy
-- =============================================================================

-- -----------------------------------------------------------------------------
-- pozycjefaktury: id_pozycji_faktury -> id (pomiń jeśli PK już = id)
-- -----------------------------------------------------------------------------
-- Komentarz: tabela pozycjefaktury, stary_pk id_pozycji_faktury -> id

-- Sprawdź aktualny PK (jeśli COLUMN_NAME='id' → pominąć migrację):
-- SELECT k.COLUMN_NAME FROM information_schema.KEY_COLUMN_USAGE k
-- WHERE k.TABLE_SCHEMA=DATABASE() AND k.TABLE_NAME='pozycjefaktury' AND k.CONSTRAINT_NAME='PRIMARY';

-- Walidacja PRZED:
SELECT COUNT(*) AS cnt_przed_pozycjefaktury FROM pozycjefaktury;

-- Pobierz nazwy FK (uruchom przed migracją, wpisz wyniki do DROP i ADD):
-- SELECT rc.CONSTRAINT_NAME, GROUP_CONCAT(kcu.COLUMN_NAME ORDER BY kcu.ORDINAL_POSITION) AS kolumny_fk,
--        rc.REFERENCED_TABLE_NAME, GROUP_CONCAT(kcu.REFERENCED_COLUMN_NAME ORDER BY kcu.ORDINAL_POSITION) AS kolumny_ref,
--        rc.DELETE_RULE, rc.UPDATE_RULE
-- FROM information_schema.REFERENTIAL_CONSTRAINTS rc
-- JOIN information_schema.KEY_COLUMN_USAGE kcu ON rc.CONSTRAINT_SCHEMA=kcu.CONSTRAINT_SCHEMA AND rc.TABLE_NAME=kcu.TABLE_NAME AND rc.CONSTRAINT_NAME=kcu.CONSTRAINT_NAME
-- WHERE rc.CONSTRAINT_SCHEMA=DATABASE() AND rc.TABLE_NAME='pozycjefaktury'
-- GROUP BY rc.CONSTRAINT_NAME, rc.REFERENCED_TABLE_NAME, rc.DELETE_RULE, rc.UPDATE_RULE;

START TRANSACTION;

-- Krok 1: Usuń FK wychodzące (wpisz nazwy z analizy – może być kilka)
-- Przykład (dostosuj do wyniku etap2_analiza_klasa_B.sql sekcja 4):
-- ALTER TABLE pozycjefaktury DROP FOREIGN KEY fk_pozycjefaktury_faktury;
-- ALTER TABLE pozycjefaktury DROP FOREIGN KEY fk_pozycjefaktury_firmy;
-- ALTER TABLE pozycjefaktury DROP FOREIGN KEY fk_pozycjefaktury_oferty;
-- Jeśli pozycjefaktury NIE MA zdefiniowanych FK w bazie (tylko kolumny bez CONSTRAINT) → pomiń DROP i ADD, wykonaj tylko Krok 2.
-- UWAGA: Odkomentuj i dostosuj poniższe po analizie:
-- ALTER TABLE pozycjefaktury DROP FOREIGN KEY <CONSTRAINT_NAME_1>;
-- ALTER TABLE pozycjefaktury DROP FOREIGN KEY <CONSTRAINT_NAME_2>;

-- Krok 2: Zmień nazwę PK na id (pomiń jeśli PK już = id)
ALTER TABLE pozycjefaktury CHANGE COLUMN id_pozycji_faktury id int(15) NOT NULL AUTO_INCREMENT;

-- Krok 3: Odtwórz FK (dostosuj do wyniku analizy – kolumny, tabela docelowa, ON DELETE/UPDATE)
-- Przykład (dostosuj do analizy):
-- ALTER TABLE pozycjefaktury ADD CONSTRAINT fk_pozycjefaktury_faktury
--   FOREIGN KEY (id_faktury) REFERENCES faktury(Id_faktury) ON DELETE CASCADE ON UPDATE CASCADE;
-- ALTER TABLE pozycjefaktury ADD CONSTRAINT fk_pozycjefaktury_firmy
--   FOREIGN KEY (id_firmy) REFERENCES firmy(id) ON DELETE RESTRICT ON UPDATE CASCADE;
-- ALTER TABLE pozycjefaktury ADD CONSTRAINT fk_pozycjefaktury_oferty
--   FOREIGN KEY (id_oferty) REFERENCES oferty(id) ON DELETE SET NULL ON UPDATE CASCADE;

COMMIT;

-- Walidacja PO:
SELECT COUNT(*) AS cnt_po_pozycjefaktury FROM pozycjefaktury;

-- Sprawdzenie: PK jest na id
SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'pozycjefaktury' AND COLUMN_NAME = 'id';

-- Sprawdzenie: FK istnieją
SELECT CONSTRAINT_NAME, COLUMN_NAME, REFERENCED_TABLE_NAME, REFERENCED_COLUMN_NAME
FROM information_schema.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'pozycjefaktury' AND REFERENCED_TABLE_NAME IS NOT NULL;


-- =============================================================================
-- RAPORT KOŃCOWY (wypełnij po migracji)
-- =============================================================================
/*
| Tabela                    | Było PK              | Jest | FK usunięto | FK odtworzono | Uwagi                    |
|---------------------------|----------------------|------|-------------|---------------|--------------------------|
| ofertypozycje             | ID_pozycja_oferty    | id   | 1           | 1             |                          |
| operator_table_permissions| id                   | id   | 0           | 0             | Bez zmian (już id)       |
| pozycjefaktury            | id_pozycji_faktury   | id   | ?           | ?             | Dostosuj do analizy      |
| doc_counters              | (composite)          | –    | –           | –             | POMINIĘTO (composite PK) |

Tabele pominięte:
- doc_counters: composite PK (company_id, doc_type, year, month)

Tabele oznaczone jako KLASA C (incoming_fk>0 – sprzeczność):
- (brak, jeśli analiza OK)
*/
