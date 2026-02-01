-- =============================================================================
-- DRY-RUN: ile rekordów będzie zmienionych (bez wykonywania UPDATE)
-- Uruchomić PRZED backfill_pozycje_faktur.sql i backfill_naglowki_faktur.sql
-- =============================================================================

-- 1) Pozycje faktur – ile pozycji ma netto_poz NULL lub 0 (będą przeliczone)
SELECT 'Pozycje faktur do uzupełnienia (netto_poz IS NULL OR netto_poz = 0)' AS opis,
       COUNT(*) AS liczba
FROM pozycjefaktury
WHERE netto_poz IS NULL OR netto_poz = 0;

-- 2) Faktury – ile nagłówków ma sum_netto/sum_vat/sum_brutto NULL lub 0 (będą przeliczone)
SELECT 'Nagłówki faktur do uzupełnienia (sum_netto IS NULL OR sum_netto = 0)' AS opis,
       COUNT(*) AS liczba
FROM faktury f
WHERE f.sum_netto IS NULL OR f.sum_netto = 0;

-- 3) Oferty – ile ma sum_brutto NULL lub 0 (opcjonalny backfill)
SELECT 'Oferty do uzupełnienia sum_brutto (sum_brutto IS NULL OR sum_brutto = 0)' AS opis,
       COUNT(*) AS liczba
FROM oferty o
WHERE o.sum_brutto IS NULL OR o.sum_brutto = 0;

-- 4) Przykładowe 10 pozycji PRZED (do weryfikacji algorytmu)
SELECT id_pozycji_faktury, id_faktury, ilosc, cena_netto, rabat, stawka_vat,
       netto_poz AS netto_poz_przed, vat_poz AS vat_poz_przed, brutto_poz AS brutto_poz_przed,
       ROUND(ilosc * cena_netto * (1 - IFNULL(rabat,0)/100), 2) AS netto_obliczone,
       ROUND(ROUND(ilosc * cena_netto * (1 - IFNULL(rabat,0)/100), 2) *
             COALESCE(CAST(REPLACE(REPLACE(TRIM(REPLACE(IFNULL(stawka_vat,'0'), '%', '')), ',', '.'), ' ', '') AS DECIMAL(10,4)), 0) / 100, 2) AS vat_obliczone
FROM pozycjefaktury
WHERE netto_poz IS NULL OR netto_poz = 0
LIMIT 10;

-- 5) Przykładowe 10 nagłówków faktur PRZED
SELECT Id_faktury, id_firmy, sum_netto AS sum_netto_przed, sum_vat AS sum_vat_przed, sum_brutto AS sum_brutto_przed,
       (SELECT COALESCE(SUM(netto_poz), 0) FROM pozycjefaktury p WHERE p.id_faktury = f.Id_faktury) AS sum_netto_obliczone,
       (SELECT COALESCE(SUM(vat_poz), 0) FROM pozycjefaktury p WHERE p.id_faktury = f.Id_faktury) AS sum_vat_obliczone,
       (SELECT COALESCE(SUM(brutto_poz), 0) FROM pozycjefaktury p WHERE p.id_faktury = f.Id_faktury) AS sum_brutto_obliczone
FROM faktury f
WHERE f.sum_netto IS NULL OR f.sum_netto = 0
LIMIT 10;

-- =============================================================================
-- Po wykonaniu backfillu – weryfikacja (10 rekordów PO)
-- =============================================================================
-- 6) Przykładowe 10 pozycji PO (po backfill_pozycje_faktur.sql)
-- SELECT id_pozycji_faktury, id_faktury, ilosc, cena_netto, rabat, stawka_vat,
--        netto_poz, vat_poz, brutto_poz
-- FROM pozycjefaktury
-- WHERE netto_poz IS NOT NULL AND netto_poz <> 0
-- ORDER BY id_pozycji_faktury DESC
-- LIMIT 10;

-- 7) Przykładowe 10 nagłówków PO (po backfill_naglowki_faktur.sql)
-- SELECT Id_faktury, id_firmy, sum_netto, sum_vat, sum_brutto
-- FROM faktury
-- WHERE sum_netto IS NOT NULL AND sum_netto <> 0
-- ORDER BY Id_faktury DESC
-- LIMIT 10;
