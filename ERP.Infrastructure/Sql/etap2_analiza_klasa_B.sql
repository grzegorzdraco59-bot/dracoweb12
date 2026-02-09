-- =============================================================================
-- ETAP 2 – Analiza tabel KLASA B (incoming_fk=0, outgoing_fk>0)
-- Baza: locbd (MariaDB/MySQL)
-- Uruchom PRZED migracją – wynik użyj do weryfikacji i uzupełnienia etap2_klasa_B_pk_do_id.sql
-- =============================================================================

SET @schema = COALESCE(DATABASE(), 'locbd');

-- =============================================================================
-- 1) Lista tabel KLASA B (incoming=0, outgoing>0)
-- =============================================================================
SELECT '=== 1) TABELE KLASA B ===' AS sekcja;

SELECT t.TABLE_NAME AS tabela
FROM information_schema.TABLES t
WHERE t.TABLE_SCHEMA = @schema AND t.TABLE_TYPE = 'BASE TABLE'
  AND NOT EXISTS (SELECT 1 FROM information_schema.REFERENTIAL_CONSTRAINTS rc 
                  WHERE rc.CONSTRAINT_SCHEMA = @schema AND rc.REFERENCED_TABLE_NAME = t.TABLE_NAME)
  AND EXISTS (SELECT 1 FROM information_schema.REFERENTIAL_CONSTRAINTS rc 
              WHERE rc.CONSTRAINT_SCHEMA = @schema AND rc.TABLE_NAME = t.TABLE_NAME)
ORDER BY t.TABLE_NAME;


-- =============================================================================
-- 2) Dla każdej tabeli KLASA B: PK (kolumna, typ, auto_increment), liczba kolumn PK
-- =============================================================================
SELECT '=== 2) PRIMARY KEY tabel klasy B ===' AS sekcja;

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
  AND NOT EXISTS (SELECT 1 FROM information_schema.REFERENTIAL_CONSTRAINTS rc 
                  WHERE rc.CONSTRAINT_SCHEMA = @schema AND rc.REFERENCED_TABLE_NAME = t.TABLE_NAME)
  AND EXISTS (SELECT 1 FROM information_schema.REFERENTIAL_CONSTRAINTS rc 
              WHERE rc.CONSTRAINT_SCHEMA = @schema AND rc.TABLE_NAME = t.TABLE_NAME)
ORDER BY t.TABLE_NAME;


-- =============================================================================
-- 3) SPRAWDZENIE: czy któraś tabela klasy B ma incoming_fk (sprzeczność → klasa C)
-- =============================================================================
SELECT '=== 3) WERYFIKACJA: incoming_fk (powinno być 0) ===' AS sekcja;

SELECT t.TABLE_NAME AS tabela,
       (SELECT COUNT(*) FROM information_schema.REFERENTIAL_CONSTRAINTS rc 
        WHERE rc.CONSTRAINT_SCHEMA = @schema AND rc.REFERENCED_TABLE_NAME = t.TABLE_NAME) AS incoming_fk
FROM information_schema.TABLES t
WHERE t.TABLE_SCHEMA = @schema AND t.TABLE_TYPE = 'BASE TABLE'
  AND NOT EXISTS (SELECT 1 FROM information_schema.REFERENTIAL_CONSTRAINTS rc 
                  WHERE rc.CONSTRAINT_SCHEMA = @schema AND rc.REFERENCED_TABLE_NAME = t.TABLE_NAME)
  AND EXISTS (SELECT 1 FROM information_schema.REFERENTIAL_CONSTRAINTS rc 
              WHERE rc.CONSTRAINT_SCHEMA = @schema AND rc.TABLE_NAME = t.TABLE_NAME)
ORDER BY t.TABLE_NAME;


-- =============================================================================
-- 4) FK wychodzące – pełna definicja (CONSTRAINT_NAME, kolumny, tabela docelowa, ON DELETE/UPDATE)
-- =============================================================================
SELECT '=== 4) FK WYCHODZĄCE – definicje do odtworzenia ===' AS sekcja;

SELECT 
    rc.TABLE_NAME AS tabela_zrodlowa,
    rc.CONSTRAINT_NAME,
    rc.REFERENCED_TABLE_NAME AS tabela_docelowa,
    GROUP_CONCAT(kcu.COLUMN_NAME ORDER BY kcu.ORDINAL_POSITION) AS kolumny_fk,
    GROUP_CONCAT(kcu.REFERENCED_COLUMN_NAME ORDER BY kcu.ORDINAL_POSITION) AS kolumny_ref,
    rc.DELETE_RULE,
    rc.UPDATE_RULE
FROM information_schema.REFERENTIAL_CONSTRAINTS rc
JOIN information_schema.KEY_COLUMN_USAGE kcu 
    ON rc.CONSTRAINT_SCHEMA = kcu.CONSTRAINT_SCHEMA 
    AND rc.TABLE_NAME = kcu.TABLE_NAME 
    AND rc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
WHERE rc.CONSTRAINT_SCHEMA = @schema
  AND rc.TABLE_NAME IN (
    SELECT t.TABLE_NAME FROM information_schema.TABLES t
    WHERE t.TABLE_SCHEMA = @schema AND t.TABLE_TYPE = 'BASE TABLE'
      AND NOT EXISTS (SELECT 1 FROM information_schema.REFERENTIAL_CONSTRAINTS r 
                      WHERE r.CONSTRAINT_SCHEMA = @schema AND r.REFERENCED_TABLE_NAME = t.TABLE_NAME)
      AND EXISTS (SELECT 1 FROM information_schema.REFERENTIAL_CONSTRAINTS r 
                   WHERE r.CONSTRAINT_SCHEMA = @schema AND r.TABLE_NAME = t.TABLE_NAME)
  )
GROUP BY rc.TABLE_NAME, rc.CONSTRAINT_NAME, rc.REFERENCED_TABLE_NAME, rc.DELETE_RULE, rc.UPDATE_RULE
ORDER BY rc.TABLE_NAME, rc.CONSTRAINT_NAME;


-- =============================================================================
-- 5) Tabele do POMINIĘCIA (composite PK, brak PK)
-- =============================================================================
SELECT '=== 5) TABELE DO POMINIĘCIA (composite PK lub brak PK) ===' AS sekcja;

-- Composite PK
SELECT TABLE_NAME AS tabela, 'composite PK' AS powod
FROM information_schema.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = @schema AND CONSTRAINT_NAME = 'PRIMARY'
GROUP BY TABLE_NAME HAVING COUNT(*) > 1
  AND TABLE_NAME IN (
    SELECT t.TABLE_NAME FROM information_schema.TABLES t
    WHERE t.TABLE_SCHEMA = @schema AND t.TABLE_TYPE = 'BASE TABLE'
      AND NOT EXISTS (SELECT 1 FROM information_schema.REFERENTIAL_CONSTRAINTS r 
                      WHERE r.CONSTRAINT_SCHEMA = @schema AND r.REFERENCED_TABLE_NAME = t.TABLE_NAME)
      AND EXISTS (SELECT 1 FROM information_schema.REFERENTIAL_CONSTRAINTS r 
                   WHERE r.CONSTRAINT_SCHEMA = @schema AND r.TABLE_NAME = t.TABLE_NAME)
  );


-- =============================================================================
-- 6) GENEROWANE KOMENDY: DROP FOREIGN KEY i ADD CONSTRAINT (do skopiowania)
-- =============================================================================
SELECT '=== 6) KOMENDY DROP/ADD FK (skopiuj do etap2_klasa_B_pk_do_id.sql) ===' AS sekcja;

SELECT CONCAT(
  '-- DROP: ', rc.TABLE_NAME, '\n',
  'ALTER TABLE ', rc.TABLE_NAME, ' DROP FOREIGN KEY ', rc.CONSTRAINT_NAME, ';\n',
  '-- ADD (po CHANGE COLUMN):\n',
  'ALTER TABLE ', rc.TABLE_NAME, ' ADD CONSTRAINT ', rc.CONSTRAINT_NAME, ' ',
  'FOREIGN KEY (', GROUP_CONCAT(kcu.COLUMN_NAME ORDER BY kcu.ORDINAL_POSITION), ') ',
  'REFERENCES ', rc.REFERENCED_TABLE_NAME, '(', GROUP_CONCAT(kcu.REFERENCED_COLUMN_NAME ORDER BY kcu.ORDINAL_POSITION), ') ',
  'ON DELETE ', rc.DELETE_RULE, ' ON UPDATE ', rc.UPDATE_RULE, ';\n'
) AS sql_do_skopiowania
FROM information_schema.REFERENTIAL_CONSTRAINTS rc
JOIN information_schema.KEY_COLUMN_USAGE kcu 
  ON rc.CONSTRAINT_SCHEMA = kcu.CONSTRAINT_SCHEMA 
  AND rc.TABLE_NAME = kcu.TABLE_NAME 
  AND rc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
WHERE rc.CONSTRAINT_SCHEMA = @schema
  AND rc.TABLE_NAME IN (
    SELECT t.TABLE_NAME FROM information_schema.TABLES t
    WHERE t.TABLE_SCHEMA = @schema AND t.TABLE_TYPE = 'BASE TABLE'
      AND NOT EXISTS (SELECT 1 FROM information_schema.REFERENTIAL_CONSTRAINTS r 
                      WHERE r.CONSTRAINT_SCHEMA = @schema AND r.REFERENCED_TABLE_NAME = t.TABLE_NAME)
      AND EXISTS (SELECT 1 FROM information_schema.REFERENTIAL_CONSTRAINTS r 
                   WHERE r.CONSTRAINT_SCHEMA = @schema AND r.TABLE_NAME = t.TABLE_NAME)
  )
GROUP BY rc.TABLE_NAME, rc.CONSTRAINT_NAME, rc.REFERENCED_TABLE_NAME, rc.DELETE_RULE, rc.UPDATE_RULE
ORDER BY rc.TABLE_NAME, rc.CONSTRAINT_NAME;
