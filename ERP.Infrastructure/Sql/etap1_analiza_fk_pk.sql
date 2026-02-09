-- =============================================================================
-- ETAP 1 – Analiza tabel pod kątem migracji PK → id
-- Baza: locbd (MariaDB/MySQL)
-- Warunek: tabela BEZ FK (ani wychodzących, ani przychodzących)
-- =============================================================================

-- 1) Wszystkie tabele z ich PRIMARY KEY (kolumna, typ, auto_increment)
SELECT 
    t.TABLE_NAME AS tabela,
    k.COLUMN_NAME AS pk_kolumna,
    k.DATA_TYPE AS typ_danych,
    k.COLUMN_TYPE AS pelny_typ,
    IF(k.EXTRA LIKE '%auto_increment%', 'TAK', 'NIE') AS auto_increment,
    (SELECT COUNT(*) FROM information_schema.KEY_COLUMN_USAGE k2 
     WHERE k2.TABLE_SCHEMA = t.TABLE_SCHEMA AND k2.TABLE_NAME = t.TABLE_NAME 
     AND k2.CONSTRAINT_NAME = 'PRIMARY') AS liczba_kolumn_pk
FROM information_schema.TABLES t
JOIN information_schema.KEY_COLUMN_USAGE k 
    ON t.TABLE_SCHEMA = k.TABLE_SCHEMA AND t.TABLE_NAME = k.TABLE_NAME
    AND k.CONSTRAINT_NAME = 'PRIMARY'
WHERE t.TABLE_SCHEMA = 'locbd'
  AND t.TABLE_TYPE = 'BASE TABLE'
ORDER BY t.TABLE_NAME;

-- 2) Wszystkie FK – tabele referencjonujące (mają FK wychodzące)
SELECT DISTINCT
    rc.TABLE_NAME AS tabela_z_fk,
    rc.REFERENCED_TABLE_NAME AS referencjonuje_tabele
FROM information_schema.REFERENTIAL_CONSTRAINTS rc
WHERE rc.CONSTRAINT_SCHEMA = 'locbd'
ORDER BY rc.TABLE_NAME, rc.REFERENCED_TABLE_NAME;

-- 3) Wszystkie FK – tabele referencjonowane (mają FK przychodzące)
SELECT DISTINCT
    rc.REFERENCED_TABLE_NAME AS tabela_referencjonowana,
    rc.TABLE_NAME AS przez_tabele
FROM information_schema.REFERENTIAL_CONSTRAINTS rc
WHERE rc.CONSTRAINT_SCHEMA = 'locbd'
ORDER BY rc.REFERENCED_TABLE_NAME;

-- 4) Tabele BEZ żadnych FK (kandydaci na ETAP 1)
-- Użyj wyników 1-3 do wykluczenia tabel z FK
