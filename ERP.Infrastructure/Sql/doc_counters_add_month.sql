-- KROK 1: Numeracja miesięczna – doc_counters: dodanie month, nowy PK (company_id, doc_type, year, month).
-- Uruchomić po create_doc_counters.sql. Dla istniejących wierszy month=1.

-- 1) Dodanie kolumny month (AFTER year)
ALTER TABLE doc_counters
  ADD COLUMN month INT NOT NULL DEFAULT 1 AFTER year;

-- 2) Usunięcie starego PK i dodanie nowego (company_id, doc_type, year, month)
ALTER TABLE doc_counters
  DROP PRIMARY KEY,
  ADD PRIMARY KEY (company_id, doc_type, year, month);
