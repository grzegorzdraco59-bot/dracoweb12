-- =============================================================================
-- KROK 2 – Ujednolicenie nazw: standardowe kolumny w ofertypozycje
-- =============================================================================
-- Kolumna ilosc już istnieje (dawniej Sztuki – zmiana nazwy). cena_netto, rabat, stawka_vat_dec – backfill z Cena, Rabat, stawka_vat (VARCHAR).
-- Starych kolumn (Cena, Rabat, stawka_vat VARCHAR) NIE USUWAĆ – legacy.
-- MariaDB 10.5.2+: ADD COLUMN IF NOT EXISTS. Dla starszych: uruchomić pojedynczo, pominąć błędy "duplicate column".
-- =============================================================================

-- 1) Dodanie kolumn standardowych (jeśli nie istnieją). ilosc – już w tabeli (dawniej Sztuki).
ALTER TABLE ofertypozycje ADD COLUMN IF NOT EXISTS cena_netto   DECIMAL(18,4) NULL;
ALTER TABLE ofertypozycje ADD COLUMN IF NOT EXISTS rabat        DECIMAL(9,2)  NULL;
ALTER TABLE ofertypozycje ADD COLUMN IF NOT EXISTS stawka_vat_dec DECIMAL(9,2) NULL COMMENT 'Stawka VAT % – z parsowania kolumny stawka_vat (VARCHAR)';

-- 2) Backfill: przepisanie z kolumn legacy (stawka_vat VARCHAR -> liczba: zamiana ',' na '.', usunięcie '%' i spacji)
UPDATE ofertypozycje
SET
  cena_netto     = COALESCE(Cena, 0),
  rabat          = COALESCE(Rabat, 0),
  stawka_vat_dec = COALESCE(
    CAST(REPLACE(REPLACE(TRIM(REPLACE(IFNULL(stawka_vat, '0'), '%', '')), ',', '.'), ' ', '') AS DECIMAL(9,2)),
    0
  );
