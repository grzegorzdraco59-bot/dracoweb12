-- =============================================================================
-- BACKFILL: ofertypozycje (netto_poz, vat_poz, brutto_poz) oraz oferty.sum_brutto
-- =============================================================================
-- Algorytm pozycji: rabat w %, netto_poz = ROUND(ilosc*cena*(1-rabat/100),2), vat_poz = ROUND(netto_poz*stawka_vat/100,2), brutto_poz = netto_poz+vat_poz.
-- Kolumny w ofertypozycje: Sztuki=ilosc, Cena=cena_netto, Rabat=rabat, stawka_vat.
-- Wykonanie: najpierw pozycje, potem nagłówki. Transakcje, partiami (LIMIT 500).
-- =============================================================================

-- 1) DRY-RUN: ile pozycji do uzupełnienia
-- SELECT COUNT(*) FROM ofertypozycje WHERE netto_poz IS NULL OR netto_poz = 0;
-- SELECT COUNT(*) FROM oferty WHERE sum_brutto IS NULL OR sum_brutto = 0;

-- 2) Pozycje ofert (partia 500)
START TRANSACTION;

UPDATE ofertypozycje p
SET
  p.netto_poz = ROUND(p.Sztuki * p.Cena * (1 - IFNULL(p.Rabat, 0) / 100), 2),
  p.vat_poz   = ROUND(
    ROUND(p.Sztuki * p.Cena * (1 - IFNULL(p.Rabat, 0) / 100), 2)
    * COALESCE(CAST(REPLACE(REPLACE(TRIM(REPLACE(IFNULL(p.stawka_vat, '0'), '%', '')), ',', '.'), ' ', '') AS DECIMAL(10,4)), 0) / 100,
    2
  ),
  p.brutto_poz = ROUND(p.Sztuki * p.Cena * (1 - IFNULL(p.Rabat, 0) / 100), 2)
    + ROUND(
        ROUND(p.Sztuki * p.Cena * (1 - IFNULL(p.Rabat, 0) / 100), 2)
        * COALESCE(CAST(REPLACE(REPLACE(TRIM(REPLACE(IFNULL(p.stawka_vat, '0'), '%', '')), ',', '.'), ' ', '') AS DECIMAL(10,4)), 0) / 100,
        2
      )
WHERE p.netto_poz IS NULL OR p.netto_poz = 0
LIMIT 500;

COMMIT;

-- 3) Nagłówki ofert – sum_brutto = SUM(brutto_poz) (partia 500). Powtarzać do ROW_COUNT() = 0.
START TRANSACTION;

UPDATE oferty o
INNER JOIN (
  SELECT oferta_id, COALESCE(SUM(brutto_poz), 0) AS s_brutto
  FROM ofertypozycje
  GROUP BY oferta_id
) agg ON o.id = agg.oferta_id
SET o.sum_brutto = agg.s_brutto
WHERE o.sum_brutto IS NULL OR o.sum_brutto = 0
LIMIT 500;

COMMIT;
