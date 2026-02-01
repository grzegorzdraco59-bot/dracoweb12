-- =============================================================================
-- BACKFILL: faktury – sum_netto, sum_vat, sum_brutto z SUM(pozycjefaktury)
-- =============================================================================
-- sum_netto  = SUM(netto_poz), sum_vat = SUM(vat_poz), sum_brutto = SUM(brutto_poz)
-- Uruchomić PO backfill_pozycje_faktur.sql (żeby pozycje miały uzupełnione netto_poz/vat_poz/brutto_poz).
-- Partiami po 500 faktur. Najpierw backfill_dry_run.sql.
-- =============================================================================

-- Partia 500 nagłówków (uruchomić wielokrotnie aż ROW_COUNT() = 0)
START TRANSACTION;

UPDATE faktury f
INNER JOIN (
  SELECT id_faktury,
         COALESCE(SUM(netto_poz), 0)  AS s_netto,
         COALESCE(SUM(vat_poz), 0)    AS s_vat,
         COALESCE(SUM(brutto_poz), 0) AS s_brutto
  FROM pozycjefaktury
  GROUP BY id_faktury
) agg ON f.Id_faktury = agg.id_faktury
SET f.sum_netto = agg.s_netto,
    f.sum_vat   = agg.s_vat,
    f.sum_brutto = agg.s_brutto
WHERE f.sum_netto IS NULL OR f.sum_netto = 0
LIMIT 500;

COMMIT;

-- =============================================================================
-- Wariant: jedna transakcja, wszystkie faktury (dla małych baz)
-- =============================================================================
-- START TRANSACTION;
-- UPDATE faktury f
-- INNER JOIN (
--   SELECT id_faktury,
--          COALESCE(SUM(netto_poz), 0) AS s_netto,
--          COALESCE(SUM(vat_poz), 0)   AS s_vat,
--          COALESCE(SUM(brutto_poz), 0) AS s_brutto
--   FROM pozycjefaktury
--   GROUP BY id_faktury
-- ) agg ON f.Id_faktury = agg.id_faktury
-- SET f.sum_netto = agg.s_netto, f.sum_vat = agg.s_vat, f.sum_brutto = agg.s_brutto
-- WHERE f.sum_netto IS NULL OR f.sum_netto = 0;
-- COMMIT;
