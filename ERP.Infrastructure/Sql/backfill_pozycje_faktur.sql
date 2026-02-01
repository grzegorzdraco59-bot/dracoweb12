-- =============================================================================
-- BACKFILL: pozycjefaktury – netto_poz, vat_poz, brutto_poz
-- =============================================================================
-- Algorytm (rabat w %, zaokrąglenia na poziomie pozycji):
--   1) netto0 = ilosc * cena_netto
--   2) netto_po_rabacie = netto0 * (1 - rabat/100)   -- rabat NULL = 0
--   3) netto_poz = ROUND(netto_po_rabacie, 2)
--   4) vat_poz   = ROUND(netto_poz * stawka_vat/100, 2)  -- stawka_vat NULL/zw = 0
--   5) brutto_poz = netto_poz + vat_poz
--
-- Wykonanie: w transakcji, partiami (np. 500–1000). Powtarzać do 0 zmienionych.
-- Najpierw uruchomić backfill_dry_run.sql.
-- =============================================================================

-- Partia 500 rekordów (uruchomić wielokrotnie aż ROW_COUNT() = 0)
START TRANSACTION;

UPDATE pozycjefaktury p
SET
  p.netto_poz = ROUND(p.ilosc * p.cena_netto * (1 - IFNULL(p.rabat, 0) / 100), 2),
  p.vat_poz   = ROUND(
    ROUND(p.ilosc * p.cena_netto * (1 - IFNULL(p.rabat, 0) / 100), 2)
    * COALESCE(CAST(REPLACE(REPLACE(TRIM(REPLACE(IFNULL(p.stawka_vat, '0'), '%', '')), ',', '.'), ' ', '') AS DECIMAL(10,4)), 0) / 100,
    2
  ),
  p.brutto_poz = ROUND(p.ilosc * p.cena_netto * (1 - IFNULL(p.rabat, 0) / 100), 2)
    + ROUND(
        ROUND(p.ilosc * p.cena_netto * (1 - IFNULL(p.rabat, 0) / 100), 2)
        * COALESCE(CAST(REPLACE(REPLACE(TRIM(REPLACE(IFNULL(p.stawka_vat, '0'), '%', '')), ',', '.'), ' ', '') AS DECIMAL(10,4)), 0) / 100,
        2
      )
WHERE p.netto_poz IS NULL OR p.netto_poz = 0
LIMIT 500;

-- Sprawdź: SELECT ROW_COUNT(); jeśli 0 – koniec. Powtarzać do 0.
COMMIT;

-- =============================================================================
-- Wariant: jedna transakcja, wszystkie rekordy (dla małych baz)
-- =============================================================================
-- START TRANSACTION;
-- UPDATE pozycjefaktury p
-- SET p.netto_poz = ..., p.vat_poz = ..., p.brutto_poz = ... (jak wyżej)
-- WHERE p.netto_poz IS NULL OR p.netto_poz = 0;
-- COMMIT;
