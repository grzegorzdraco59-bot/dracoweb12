-- =============================================================================
-- BACKFILL (opcjonalny): oferty.sum_brutto z total_brutto
-- =============================================================================
-- Preferowane: backfill_ofertypozycje_oferty.sql (SUM(brutto_poz) z ofertypozycje).
-- Alternatywa: skopiuj total_brutto do sum_brutto.
-- =============================================================================
START TRANSACTION;

UPDATE oferty o
SET o.sum_brutto = COALESCE(o.total_brutto, 0)
WHERE o.sum_brutto IS NULL OR o.sum_brutto = 0;

COMMIT;
