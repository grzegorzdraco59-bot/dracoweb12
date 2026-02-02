-- =============================================================================
-- Kolumny sum_netto, sum_vat w tabeli oferty (obok istniejącego sum_brutto).
-- Używane przez RecalcOfferTotals – sumy z ofertypozycje (netto_poz, vat_poz, brutto_poz).
-- Wymagane przed uruchomieniem WPF (OfferRepository odczytuje te kolumny).
-- Dla MySQL < 8.0.12 / MariaDB < 10.5 (brak IF NOT EXISTS): wykonaj ręcznie:
--   ALTER TABLE oferty ADD COLUMN sum_netto DECIMAL(18,2) NULL DEFAULT 0.00;
--   ALTER TABLE oferty ADD COLUMN sum_vat   DECIMAL(18,2) NULL DEFAULT 0.00;
-- =============================================================================

ALTER TABLE oferty
  ADD COLUMN IF NOT EXISTS sum_netto DECIMAL(18,2) NULL DEFAULT 0.00,
  ADD COLUMN IF NOT EXISTS sum_vat   DECIMAL(18,2) NULL DEFAULT 0.00;
