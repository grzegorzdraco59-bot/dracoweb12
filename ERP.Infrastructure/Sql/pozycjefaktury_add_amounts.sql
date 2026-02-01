-- ETAP 1: Pozycje faktury – pola wyliczone (netto, VAT, brutto) – zaokrąglenia do 2 miejsc.
-- Rabat = procent. Liczenie: netto_poz = ilosc * cena_netto * (1 - rabat/100), vat_poz, brutto_poz.

ALTER TABLE pozycjefaktury
  ADD COLUMN netto_poz  DECIMAL(18,2) NULL,
  ADD COLUMN vat_poz    DECIMAL(18,2) NULL,
  ADD COLUMN brutto_poz DECIMAL(18,2) NULL;
