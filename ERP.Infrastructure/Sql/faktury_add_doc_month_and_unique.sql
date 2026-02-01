-- KROK 2: Tabela faktury – doc_month + unikat per (id_firmy, doc_type, doc_year, doc_month, doc_no).
-- Wymaga: doc_type, doc_year, doc_no, doc_full_no (np. po faktury_add_doc_numbering.sql i ewent. doc_type).

-- 1) Dodanie kolumny doc_month (DEFAULT 1 dla istniejących wierszy)
ALTER TABLE faktury
  ADD COLUMN doc_month INT NOT NULL DEFAULT 1 AFTER doc_year;

-- 2) Usunięcie starego unikatu (jeśli istnieje)
ALTER TABLE faktury DROP INDEX uq_faktury_company_year_no;

-- 3) Unikat per firma + typ + rok + miesiąc + numer (id_firmy = company_id w tabeli faktury)
ALTER TABLE faktury
  ADD UNIQUE KEY uq_faktury_doc_m (id_firmy, doc_type, doc_year, doc_month, doc_no);
