-- KROK 2: Tabela faktury – doc_type, doc_month, unikat per (id_firmy, doc_type, doc_year, doc_month, doc_no).
-- Uruchomić gdy faktury ma już doc_year, doc_no, doc_full_no (np. po faktury_add_doc_numbering.sql).

-- 1) Dodanie kolumn doc_type i doc_month (jeśli brak)
ALTER TABLE faktury
  ADD COLUMN doc_type VARCHAR(16) NULL,
  ADD COLUMN doc_month INT NULL;

-- 2) Backfill: doc_type z skrot_nazwa_faktury, doc_month z data_faktury (Clarion)
UPDATE faktury
SET doc_type = COALESCE(NULLIF(TRIM(skrot_nazwa_faktury), ''), 'FV'),
    doc_month = MONTH(DATE_ADD('1800-12-28', INTERVAL COALESCE(data_faktury, 0) DAY))
WHERE doc_type IS NULL OR doc_month IS NULL;

-- 3) Dla wierszy bez sensownej daty
UPDATE faktury SET doc_type = 'FV', doc_month = 1 WHERE doc_type IS NULL OR doc_month IS NULL;

-- 4) NOT NULL
ALTER TABLE faktury
  MODIFY COLUMN doc_type VARCHAR(16) NOT NULL,
  MODIFY COLUMN doc_month INT NOT NULL;

-- 5) Usunięcie starego unikatu (jeśli istnieje) i dodanie nowego
ALTER TABLE faktury DROP INDEX uq_faktury_company_year_no;
ALTER TABLE faktury
  ADD UNIQUE KEY uq_faktury_company_type_year_month_no (id_firmy, doc_type, doc_year, doc_month, doc_no);
