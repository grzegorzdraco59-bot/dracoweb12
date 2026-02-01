-- ETAP 1 – DOPISANIE PÓL DO ROZLICZANIA ZALICZEK (FVZ -> FV)
-- MariaDB. Pola do rozliczeń zaliczek na fakturze końcowej FV.
--
-- WYMAGANIE: FVZ (zaliczki) i FV (końcowa) łączą się przez root_doc_id = FPF.id.
-- FV ma pokazywać: sum_zaliczek_brutto, do_zaplaty_brutto = sum_brutto - sum_zaliczek_brutto.
--
-- ZAKŁADAMY: W tabeli faktury są już kolumny sum_brutto, sum_netto, sum_vat
--            (z faktury_add_sum_fields.sql lub migrate_add_missing_erp_fields.sql).
-- Nie usuwamy żadnych pól, tylko dodajemy brakujące.

-- 1) Dodanie kolumn (ADD COLUMN IF NOT EXISTS – MariaDB 10.5.2+)
ALTER TABLE faktury
  ADD COLUMN IF NOT EXISTS sum_zaliczek_brutto DECIMAL(18,2) NOT NULL DEFAULT 0.00,
  ADD COLUMN IF NOT EXISTS do_zaplaty_brutto   DECIMAL(18,2) NOT NULL DEFAULT 0.00;

-- 2) Opcjonalnie: ustawienie do_zaplaty_brutto dla istniejących wierszy
--    (gdy sum_brutto już jest uzupełnione: do_zaplaty = sum_brutto - sum_zaliczek_brutto)
-- Odkomentować po pierwszym uruchomieniu ALTER, jeśli chcesz backfill:
-- UPDATE faktury
-- SET do_zaplaty_brutto = GREATEST(0, COALESCE(sum_brutto, 0) - COALESCE(sum_zaliczek_brutto, 0))
-- WHERE do_zaplaty_brutto = 0 AND (sum_brutto IS NOT NULL AND sum_brutto <> 0);

-- Potwierdzenie: pola sum_zaliczek_brutto i do_zaplaty_brutto są gotowe pod rozliczenie zaliczek.
