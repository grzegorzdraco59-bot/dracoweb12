-- ETAP 1: Nagłówek faktury – sumy z pozycji (zapis w DB, nie liczenie w UI).
-- Sumy: sum_netto, sum_vat, sum_brutto.

ALTER TABLE faktury
  ADD COLUMN sum_netto   DECIMAL(18,2) NULL,
  ADD COLUMN sum_vat     DECIMAL(18,2) NULL,
  ADD COLUMN sum_brutto  DECIMAL(18,2) NULL;
