-- =============================================================================
-- KROK 4 & 5 – BACKFILL: ofertypozycje (netto_poz, vat_poz, brutto_poz) oraz oferty.sum_brutto
-- =============================================================================
-- Wymagane kolumny: ofertypozycje – ilosc, cena_netto, Rabat, stawka_vat (VARCHAR), netto_poz, vat_poz, brutto_poz;
--                 oferty – sum_brutto.
-- Jeśli brak netto_poz/vat_poz/brutto_poz: najpierw uruchomić oferty_ofertypozycje_add_columns.sql.
--
-- Formuła (rabat w % 0..100, NULL = 0):
--   netto_poz = ROUND( ilosc * cena_netto * (1 - IFNULL(rabat,0)/100), 2 )
--   vat_poz   = ROUND( netto_poz * (IFNULL(stawka_vat,0)/100), 2 )
--   brutto_poz = netto_poz + vat_poz
-- Kolumna ilosc (dawniej Sztuki). stawka_vat z VARCHAR (parsowanie: ',' -> '.', bez '%').
-- =============================================================================

-- DRY-RUN (odkomentować):
-- SELECT COUNT(*) AS pozycje_do_uzupelnienia FROM ofertypozycje WHERE netto_poz IS NULL OR (netto_poz = 0 AND (ilosc IS NOT NULL AND cena_netto IS NOT NULL AND (ilosc <> 0 OR cena_netto <> 0)));
-- SELECT COUNT(*) AS oferty_do_uzupelnienia FROM oferty o WHERE (o.sum_brutto IS NULL OR o.sum_brutto = 0) AND EXISTS (SELECT 1 FROM ofertypozycje p WHERE p.oferta_id = o.id);

-- ----- 1) Pozycje: wyliczenie netto_poz, vat_poz, brutto_poz (COALESCE dla NULL ilosc/Cena) -----
START TRANSACTION;

UPDATE ofertypozycje p
SET
  p.netto_poz = ROUND(COALESCE(p.ilosc, 0) * COALESCE(p.cena_netto, 0) * (1 - IFNULL(p.Rabat, 0) / 100), 2),
  p.vat_poz   = ROUND(
    ROUND(COALESCE(p.ilosc, 0) * COALESCE(p.cena_netto, 0) * (1 - IFNULL(p.Rabat, 0) / 100), 2)
    * COALESCE(CAST(REPLACE(REPLACE(TRIM(REPLACE(IFNULL(p.stawka_vat, '0'), '%', '')), ',', '.'), ' ', '') AS DECIMAL(10,4)), 0) / 100,
    2
  ),
  p.brutto_poz = ROUND(COALESCE(p.ilosc, 0) * COALESCE(p.cena_netto, 0) * (1 - IFNULL(p.Rabat, 0) / 100), 2)
    + ROUND(
        ROUND(COALESCE(p.ilosc, 0) * COALESCE(p.cena_netto, 0) * (1 - IFNULL(p.Rabat, 0) / 100), 2)
        * COALESCE(CAST(REPLACE(REPLACE(TRIM(REPLACE(IFNULL(p.stawka_vat, '0'), '%', '')), ',', '.'), ' ', '') AS DECIMAL(10,4)), 0) / 100,
        2
      )
WHERE p.netto_poz IS NULL OR p.netto_poz = 0;

COMMIT;

-- ----- 2) Nagłówki ofert: sum_brutto = SUM(brutto_poz) po oferta_id -----
START TRANSACTION;

UPDATE oferty o
INNER JOIN (
  SELECT oferta_id, COALESCE(SUM(brutto_poz), 0) AS s_brutto
  FROM ofertypozycje
  WHERE oferta_id IS NOT NULL
  GROUP BY oferta_id
) agg ON o.id = agg.oferta_id
SET o.sum_brutto = agg.s_brutto
WHERE o.sum_brutto IS NULL OR o.sum_brutto = 0;

COMMIT;

-- ----- 3) RAPORT (wykonać po backfillzie) -----
-- Pozycje z wyliczonym brutto_poz:
SELECT COUNT(*) AS pozycje_z_przeliczonym_brutto FROM ofertypozycje WHERE netto_poz IS NOT NULL AND brutto_poz IS NOT NULL;
-- Oferty z sum_brutto > 0 (mające pozycje):
SELECT COUNT(*) AS oferty_z_sum_brutto FROM oferty o
WHERE (o.sum_brutto IS NOT NULL AND o.sum_brutto <> 0)
  AND EXISTS (SELECT 1 FROM ofertypozycje p WHERE p.oferta_id = o.id);

-- Dla bardzo dużych tabel: wersja partiami (pozycje LIMIT 500, oferty LIMIT 500) – powtarzać do 0 wierszy.
-- UPDATE ofertypozycje p SET ... WHERE ... LIMIT 500;
-- UPDATE oferty o INNER JOIN (...) SET ... LIMIT 500;
