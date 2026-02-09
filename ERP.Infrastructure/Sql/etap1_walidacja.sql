-- =============================================================================
-- ETAP 1 – Walidacja po migracji PK → id
-- Uruchom dla każdej tabeli po wykonaniu migracji
-- =============================================================================

-- Szablon – zamień 'tabela' na nazwę tabeli:

-- 1) Liczba rekordów (przed = po)
SELECT COUNT(*) AS liczba_rekordow FROM tabela;

-- 2) Sprawdzenie kolumny id (PK, AUTO_INCREMENT)
SELECT COLUMN_NAME, COLUMN_KEY, EXTRA, COLUMN_TYPE 
FROM information_schema.COLUMNS 
WHERE TABLE_SCHEMA = 'locbd' AND TABLE_NAME = 'tabela' AND COLUMN_NAME = 'id';
-- Oczekiwane: COLUMN_KEY=PRI, EXTRA=auto_increment

-- 3) Brak NULL w id
SELECT COUNT(*) AS null_count FROM tabela WHERE id IS NULL;
-- Oczekiwane: 0
