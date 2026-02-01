-- =============================================================================
-- Migracja: zmiana nazw kolumn w ofertypozycje (bez utraty danych)
-- =============================================================================
-- Wykonaj: SHOW COLUMNS FROM ofertypozycje; – potwierdź istnienie kolumn.
-- Docelowe nazwy: cena_netto, netto_poz, vat_poz, brutto_poz.
-- Uwaga: jeśli kolumny netto_poz, vat_poz, brutto_poz zostały wcześniej DODANE
-- (np. przez oferty_ofertypozycje_add_columns.sql), to:
-- 1) Najpierw uruchom backfill (netto_poz/vat_poz/brutto_poz z formuły).
-- 2) Potem: ALTER TABLE ofertypozycje DROP COLUMN cena_po_rabacie_i_sztukach;
-- 3) Nie zmieniaj vat ani cena_brutto (już są vat_poz, brutto_poz).
-- Poniżej wersja dla tabeli z oryginalnymi nazwami (bez wcześniej dodanych kolumn).
-- =============================================================================

-- 1) cena -> cena_netto (cena jednostkowa netto przed rabatem)
ALTER TABLE ofertypozycje
  CHANGE COLUMN Cena cena_netto DECIMAL(18,4) NULL;

-- 2) vat -> vat_poz (kwota VAT pozycji)
ALTER TABLE ofertypozycje
  CHANGE COLUMN vat vat_poz DECIMAL(18,2) NULL;

-- 3) cena_brutto -> brutto_poz (brutto pozycji)
ALTER TABLE ofertypozycje
  CHANGE COLUMN cena_brutto brutto_poz DECIMAL(18,2) NULL;

-- 4) cena_po_rabacie_i_sztukach -> netto_poz (netto pozycji po rabacie)
--    Wykonaj tylko jeśli NIE ma jeszcze kolumny netto_poz (np. z add_columns).
--    Jeśli netto_poz już istnieje: zamiast tego zrób backfill netto_poz, potem DROP cena_po_rabacie_i_sztukach.
ALTER TABLE ofertypozycje
  CHANGE COLUMN Cena_po_rabacie_i_sztukach netto_poz DECIMAL(18,2) NULL;

-- Kolumny rabat / stawka_vat: jeśli brak (np. tylko stawka_vat VARCHAR), dodaj tylko rabat:
-- ALTER TABLE ofertypozycje ADD COLUMN IF NOT EXISTS rabat DECIMAL(9,2) NULL;
-- stawka_vat: w wielu bazach już jest (VARCHAR). Dla wersji numerycznej: stawka_vat_dec (ADD IF NOT EXISTS).

-- =============================================================================
-- WARIANT B: gdy netto_poz, vat_poz, brutto_poz JUŻ ISTNIEJĄ (np. z add_columns)
-- =============================================================================
-- Wtedy: tylko zmień cena -> cena_netto; potem usuń stare kolumny (po backfillzie).
-- ALTER TABLE ofertypozycje CHANGE COLUMN Cena cena_netto DECIMAL(18,4) NULL;
-- (Backfill netto_poz, vat_poz, brutto_poz z formuły – backfill_ofertypozycje_oferty.sql)
-- ALTER TABLE ofertypozycje DROP COLUMN Cena_po_rabacie_i_sztukach;
-- ALTER TABLE ofertypozycje DROP COLUMN vat;
-- ALTER TABLE ofertypozycje DROP COLUMN cena_brutto;
