-- KROK 1: Kolumny numeracji dokumentów w tabeli faktury + unikat per (id_firmy, doc_year, doc_no).
-- Dla istniejących wierszy: doc_year z data_faktury (Clarion), doc_no = Id_faktury (unikalność), doc_full_no = FV/year/Id.

-- 1) Dodanie kolumn (najpierw NULL, żeby móc uzupełnić istniejące wiersze).
-- Przy ponownym uruchomieniu: pominąć ten ALTER, jeśli kolumny już istnieją.
ALTER TABLE faktury
  ADD COLUMN doc_year INT NULL,
  ADD COLUMN doc_no INT NULL,
  ADD COLUMN doc_full_no VARCHAR(64) NULL;

-- 2) Uzupełnienie istniejących wierszy (data_faktury = dni od 28.12.1800 w Clarion)
UPDATE faktury
SET doc_year = YEAR(DATE_ADD('1800-12-28', INTERVAL COALESCE(data_faktury, 0) DAY)),
    doc_no   = Id_faktury,
    doc_full_no = CONCAT('FV/', YEAR(DATE_ADD('1800-12-28', INTERVAL COALESCE(data_faktury, 0) DAY)), '/', LPAD(Id_faktury, 6, '0'))
WHERE doc_year IS NULL OR doc_no IS NULL OR doc_full_no IS NULL;

-- 3) Dla wierszy bez data_faktury (NULL/0) ustaw rok 1900
UPDATE faktury SET doc_year = 1900, doc_no = Id_faktury, doc_full_no = CONCAT('FV/1900/', LPAD(Id_faktury, 6, '0'))
WHERE doc_year IS NULL;

-- 4) Zmiana na NOT NULL
ALTER TABLE faktury
  MODIFY COLUMN doc_year INT NOT NULL,
  MODIFY COLUMN doc_no INT NOT NULL,
  MODIFY COLUMN doc_full_no VARCHAR(64) NOT NULL;

-- 5) Unikat per firma + rok + numer
ALTER TABLE faktury
  ADD UNIQUE KEY uq_faktury_company_year_no (id_firmy, doc_year, doc_no);
