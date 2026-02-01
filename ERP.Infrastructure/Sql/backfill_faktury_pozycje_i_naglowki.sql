-- =============================================================================
-- BACKFILL: pozycjefaktury (netto_poz, vat_poz, brutto_poz) + faktury (sum_*)
-- Jedna transakcja – wszystkie rekordy. MariaDB/MySQL.
-- =============================================================================
-- ETAP 3: Pozycje – wzory (zaokrąglenie na pozycji):
--   netto_poz = ROUND( ilosc * cena_netto * (1 - IFNULL(rabat,0)/100), 2)
--   vat_poz   = ROUND( netto_poz * (stawka_vat_num/100), 2)  -- stawka_vat może być tekst "23%"
--   brutto_poz = netto_poz + vat_poz
-- ETAP 4: Nagłówki – sum_netto/sum_vat/sum_brutto = SUM(pozycje) po id_faktury.
-- =============================================================================

START TRANSACTION;

-- 1) Przelicz WSZYSTKIE pozycje (netto_poz, vat_poz, brutto_poz)
-- stawka_vat w DB może być tekst (np. "23%", "23") – parsowanie do DECIMAL
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
      );

-- 2) Przelicz nagłówki faktur (sum_netto, sum_vat, sum_brutto z pozycji)
UPDATE faktury f
INNER JOIN (
  SELECT COALESCE(faktura_id, id_faktury) AS fid,
         COALESCE(SUM(netto_poz), 0)  AS s_netto,
         COALESCE(SUM(vat_poz), 0)    AS s_vat,
         COALESCE(SUM(brutto_poz), 0) AS s_brutto
  FROM pozycjefaktury
  GROUP BY COALESCE(faktura_id, id_faktury)
) agg ON f.id = agg.fid
SET f.sum_netto = agg.s_netto,
    f.sum_vat   = agg.s_vat,
    f.sum_brutto = agg.s_brutto;

-- 3) do_zaplaty_brutto (opcjonalnie): uruchomić tylko gdy tabela faktury ma kolumny
--    sum_zaliczek_brutto, do_zaplaty_brutto (fix_faktury_do_zaplaty_brutto.sql).
-- UPDATE faktury f
-- SET f.do_zaplaty_brutto = GREATEST(COALESCE(f.sum_brutto, 0) - COALESCE(f.sum_zaliczek_brutto, 0), 0)
-- WHERE f.sum_brutto IS NOT NULL;

COMMIT;
