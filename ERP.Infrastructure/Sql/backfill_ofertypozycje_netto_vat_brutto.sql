-- =============================================================================
-- Backfill: ofertypozycje – netto_poz, vat_poz, brutto_poz
-- =============================================================================
-- Wymaga kolumn: ilosc, cena_netto, rabat (%), stawka_vat (%).
-- Uwaga: jeśli rabat w DB ma nazwę Rabat (wielka litera), zamień p.rabat na p.Rabat.
--        jeśli stawka_vat jest VARCHAR (np. '23%'), użyj skryptu z parsowaniem
--        (backfill_ofertypozycje_oferty.sql).
-- =============================================================================

START TRANSACTION;

UPDATE ofertypozycje p
SET
  p.netto_poz = ROUND(IFNULL(p.ilosc,0) * IFNULL(p.cena_netto,0) * (1 - IFNULL(p.rabat,0)/100), 2),
  p.vat_poz   = ROUND(ROUND(IFNULL(p.ilosc,0) * IFNULL(p.cena_netto,0) * (1 - IFNULL(p.rabat,0)/100), 2) * (IFNULL(p.stawka_vat,0)/100), 2),
  p.brutto_poz = ROUND(IFNULL(p.ilosc,0) * IFNULL(p.cena_netto,0) * (1 - IFNULL(p.rabat,0)/100), 2)
              + ROUND(ROUND(IFNULL(p.ilosc,0) * IFNULL(p.cena_netto,0) * (1 - IFNULL(p.rabat,0)/100), 2) * (IFNULL(p.stawka_vat,0)/100), 2);

COMMIT;
