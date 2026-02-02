-- =============================================================================
-- Aktualizacja sum nagłówka oferty (sum_netto, sum_vat, sum_brutto) z pozycji
-- dla jednej oferty o podanym id.
-- Wymagane: oferty – sum_netto, sum_vat, sum_brutto; ofertypozycje – netto_poz, vat_poz, brutto_poz.
-- Jeśli oferty nie ma kolumn sum_netto/sum_vat: dodaj je (np. ALTER TABLE oferty ADD COLUMN sum_netto DECIMAL(18,2) NULL, ADD COLUMN sum_vat DECIMAL(18,2) NULL;).
-- =============================================================================

SET @ofertaId := 1234;

UPDATE oferty o
JOIN (
  SELECT
    oferta_id,
    COALESCE(SUM(netto_poz), 0)  AS s_netto,
    COALESCE(SUM(vat_poz), 0)   AS s_vat,
    COALESCE(SUM(brutto_poz), 0) AS s_brutto
  FROM ofertypozycje
  WHERE oferta_id = @ofertaId
  GROUP BY oferta_id
) x ON x.oferta_id = o.id
SET
  o.sum_netto  = x.s_netto,
  o.sum_vat    = x.s_vat,
  o.sum_brutto = x.s_brutto
WHERE o.id = @ofertaId;
