-- =============================================================================
-- ETAP 3 – Analiza tabel KLASA C (incoming_fk>0, rdzeń/rodzice)
-- Baza: locbd (MariaDB/MySQL)
-- Uruchom PRZED migracją – wynik użyj do weryfikacji i uzupełnienia etap3_klasa_C_pk_do_id.sql
-- =============================================================================

SET @schema = COALESCE(DATABASE(), 'locbd');

-- =============================================================================
-- 1) Lista tabel KLASA C (incoming_fk>0)
-- =============================================================================
SELECT '=== 1) TABELE KLASA C (rdzeń/rodzice) ===' AS sekcja;

SELECT DISTINCT rc.REFERENCED_TABLE_NAME AS tabela_rodzic
FROM information_schema.REFERENTIAL_CONSTRAINTS rc
WHERE rc.CONSTRAINT_SCHEMA = @schema
  AND rc.REFERENCED_TABLE_NAME IS NOT NULL
ORDER BY rc.REFERENCED_TABLE_NAME;


-- =============================================================================
-- 2) Dla każdej tabeli KLASA C: PK (kolumna, typ, auto_increment), liczba kolumn PK
-- =============================================================================
SELECT '=== 2) PRIMARY KEY tabel klasy C ===' AS sekcja;

SELECT 
    t.TABLE_NAME AS tabela,
    k.COLUMN_NAME AS pk_kolumna,
    c.COLUMN_TYPE AS pelny_typ,
    IF(c.EXTRA LIKE '%auto_increment%', 'TAK', 'NIE') AS auto_increment,
    (SELECT COUNT(*) FROM information_schema.KEY_COLUMN_USAGE k2 
     WHERE k2.TABLE_SCHEMA = t.TABLE_SCHEMA AND k2.TABLE_NAME = t.TABLE_NAME 
     AND k2.CONSTRAINT_NAME = 'PRIMARY') AS liczba_kolumn_pk
FROM information_schema.TABLES t
JOIN information_schema.KEY_COLUMN_USAGE k 
    ON t.TABLE_SCHEMA = k.TABLE_SCHEMA AND t.TABLE_NAME = k.TABLE_NAME
    AND k.CONSTRAINT_NAME = 'PRIMARY'
JOIN information_schema.COLUMNS c 
    ON c.TABLE_SCHEMA = k.TABLE_SCHEMA AND c.TABLE_NAME = k.TABLE_NAME AND c.COLUMN_NAME = k.COLUMN_NAME
WHERE t.TABLE_SCHEMA = @schema AND t.TABLE_TYPE = 'BASE TABLE'
  AND t.TABLE_NAME IN (
    SELECT DISTINCT REFERENCED_TABLE_NAME FROM information_schema.REFERENTIAL_CONSTRAINTS 
    WHERE CONSTRAINT_SCHEMA = @schema AND REFERENCED_TABLE_NAME IS NOT NULL
  )
ORDER BY t.TABLE_NAME;


-- =============================================================================
-- 3) Tabele dzieci (incoming FK) – pełna definicja
-- =============================================================================
SELECT '=== 3) TABELE DZIECI – FK wskazujące na rodziców klasy C ===' AS sekcja;

SELECT 
    rc.REFERENCED_TABLE_NAME AS tabela_rodzic,
    rc.TABLE_NAME AS tabela_dziecka,
    rc.CONSTRAINT_NAME,
    kcu.COLUMN_NAME AS kolumna_fk,
    kcu.REFERENCED_COLUMN_NAME AS kolumna_ref_rodzic,
    rc.DELETE_RULE,
    rc.UPDATE_RULE
FROM information_schema.REFERENTIAL_CONSTRAINTS rc
JOIN information_schema.KEY_COLUMN_USAGE kcu 
    ON rc.CONSTRAINT_SCHEMA = kcu.CONSTRAINT_SCHEMA 
    AND rc.TABLE_NAME = kcu.TABLE_NAME 
    AND rc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
WHERE rc.CONSTRAINT_SCHEMA = @schema
  AND rc.REFERENCED_TABLE_NAME IN (
    SELECT DISTINCT REFERENCED_TABLE_NAME FROM information_schema.REFERENTIAL_CONSTRAINTS 
    WHERE CONSTRAINT_SCHEMA = @schema AND REFERENCED_TABLE_NAME IS NOT NULL
  )
ORDER BY rc.REFERENCED_TABLE_NAME, rc.TABLE_NAME, rc.CONSTRAINT_NAME;


-- =============================================================================
-- 4) Tabele do POMINIĘCIA (composite PK, brak PK)
-- =============================================================================
SELECT '=== 4) TABELE KLASY C DO POMINIĘCIA (composite PK lub brak PK) ===' AS sekcja;

-- Composite PK w klasie C
SELECT t.TABLE_NAME AS tabela, 'composite PK' AS powod
FROM information_schema.TABLES t
WHERE t.TABLE_SCHEMA = @schema AND t.TABLE_TYPE = 'BASE TABLE'
  AND t.TABLE_NAME IN (
    SELECT DISTINCT REFERENCED_TABLE_NAME FROM information_schema.REFERENTIAL_CONSTRAINTS 
    WHERE CONSTRAINT_SCHEMA = @schema AND REFERENCED_TABLE_NAME IS NOT NULL
  )
  AND EXISTS (
    SELECT 1 FROM information_schema.KEY_COLUMN_USAGE k 
    WHERE k.TABLE_SCHEMA = t.TABLE_SCHEMA AND k.TABLE_NAME = t.TABLE_NAME AND k.CONSTRAINT_NAME = 'PRIMARY'
    GROUP BY k.TABLE_NAME HAVING COUNT(*) > 1
  );


-- =============================================================================
-- 5) Wykrywanie cykli FK i self-FK
-- =============================================================================
SELECT '=== 5) CYKLE FK / SELF-FK ===' AS sekcja;

-- Self-FK (tabela referencjonuje samą siebie)
SELECT TABLE_NAME AS tabela, CONSTRAINT_NAME, COLUMN_NAME, REFERENCED_COLUMN_NAME
FROM information_schema.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = @schema AND REFERENCED_TABLE_NAME IS NOT NULL
  AND TABLE_NAME = REFERENCED_TABLE_NAME;


-- =============================================================================
-- 6) Podsumowanie: rodzic -> dzieci (do planu migracji)
-- =============================================================================
SELECT '=== 6) PODSUMOWANIE: rodzic -> lista dzieci (tabela_dziecka, kolumna_fk, constraint) ===' AS sekcja;

SELECT 
    rc.REFERENCED_TABLE_NAME AS rodzic,
    GROUP_CONCAT(
        CONCAT(rc.TABLE_NAME, ':', kcu.COLUMN_NAME, ':', rc.CONSTRAINT_NAME)
        ORDER BY rc.TABLE_NAME SEPARATOR ' | '
    ) AS dzieci_fk
FROM information_schema.REFERENTIAL_CONSTRAINTS rc
JOIN information_schema.KEY_COLUMN_USAGE kcu 
    ON rc.CONSTRAINT_SCHEMA = kcu.CONSTRAINT_SCHEMA 
    AND rc.TABLE_NAME = kcu.TABLE_NAME 
    AND rc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
WHERE rc.CONSTRAINT_SCHEMA = @schema
  AND rc.REFERENCED_TABLE_NAME IN (
    SELECT DISTINCT REFERENCED_TABLE_NAME FROM information_schema.REFERENTIAL_CONSTRAINTS 
    WHERE CONSTRAINT_SCHEMA = @schema AND REFERENCED_TABLE_NAME IS NOT NULL
  )
GROUP BY rc.REFERENCED_TABLE_NAME
ORDER BY rc.REFERENCED_TABLE_NAME;
