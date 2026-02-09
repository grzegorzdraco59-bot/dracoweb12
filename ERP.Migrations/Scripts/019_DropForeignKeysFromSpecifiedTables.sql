-- =============================================================================
-- 019: Usunięcie FOREIGN KEY z tabel: towary, operator, firmy, faktury, dostawcy
-- Baza: locbd (MariaDB/MySQL)
-- =============================================================================

-- =============================================================================
-- A) RAPORT: Wszystkie FK z tych 5 tabel
-- =============================================================================

SELECT
    rc.CONSTRAINT_SCHEMA AS baza,
    rc.TABLE_NAME AS tabela,
    rc.CONSTRAINT_NAME AS nazwa_fk,
    kcu.COLUMN_NAME AS kolumna,
    rc.REFERENCED_TABLE_NAME AS tabela_referencyjna,
    kcu.REFERENCED_COLUMN_NAME AS kolumna_referencyjna
FROM information_schema.REFERENTIAL_CONSTRAINTS rc
JOIN information_schema.KEY_COLUMN_USAGE kcu
    ON rc.CONSTRAINT_SCHEMA = kcu.CONSTRAINT_SCHEMA
   AND rc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
   AND rc.TABLE_NAME = kcu.TABLE_NAME
WHERE rc.CONSTRAINT_SCHEMA = 'locbd'
  AND rc.TABLE_NAME IN ('towary', 'operator', 'firmy', 'faktury', 'dostawcy')
ORDER BY rc.TABLE_NAME, rc.CONSTRAINT_NAME, kcu.ORDINAL_POSITION;

-- Podsumowanie: które tabele mają FK, które nie
SELECT t.tabela,
       COALESCE(COUNT(rc.CONSTRAINT_NAME), 0) AS liczba_fk,
       IF(COUNT(rc.CONSTRAINT_NAME) = 0, 'BRAK FK', CONCAT('ma ', COUNT(rc.CONSTRAINT_NAME), ' FK')) AS status
FROM (
  SELECT 'towary' AS tabela UNION SELECT 'operator' UNION SELECT 'firmy'
  UNION SELECT 'faktury' UNION SELECT 'dostawcy'
) t
LEFT JOIN information_schema.REFERENTIAL_CONSTRAINTS rc
  ON rc.CONSTRAINT_SCHEMA = 'locbd' AND rc.TABLE_NAME = t.tabela
GROUP BY t.tabela
ORDER BY t.tabela;


-- =============================================================================
-- B) GENERATOR: ALTER TABLE DROP FOREIGN KEY (jeden wiersz = jedna komenda)
-- =============================================================================

SELECT CONCAT(
  'ALTER TABLE ', rc.TABLE_NAME, ' DROP FOREIGN KEY ', rc.CONSTRAINT_NAME, ';'
) AS drop_fk_sql
FROM information_schema.REFERENTIAL_CONSTRAINTS rc
WHERE rc.CONSTRAINT_SCHEMA = 'locbd'
  AND rc.TABLE_NAME IN ('towary', 'operator', 'firmy', 'faktury', 'dostawcy')
ORDER BY rc.TABLE_NAME, rc.CONSTRAINT_NAME;


-- =============================================================================
-- B) SKRYPT: Usunięcie FK (wklej wynik z GENERATOR między SET)
-- =============================================================================

SET foreign_key_checks = 0;

-- ALTER TABLE towary DROP FOREIGN KEY <nazwa>;
-- ALTER TABLE operator DROP FOREIGN KEY <nazwa>;
-- ALTER TABLE firmy DROP FOREIGN KEY <nazwa>;
-- ALTER TABLE faktury DROP FOREIGN KEY <nazwa>;
-- ALTER TABLE dostawcy DROP FOREIGN KEY <nazwa>;

SET foreign_key_checks = 1;


-- =============================================================================
-- C) WERYFIKACJA: Brak FK w tych tabelach po wykonaniu
-- =============================================================================

SELECT
    rc.CONSTRAINT_SCHEMA AS baza,
    rc.TABLE_NAME AS tabela,
    rc.CONSTRAINT_NAME AS nazwa_fk
FROM information_schema.REFERENTIAL_CONSTRAINTS rc
WHERE rc.CONSTRAINT_SCHEMA = 'locbd'
  AND rc.TABLE_NAME IN ('towary', 'operator', 'firmy', 'faktury', 'dostawcy')
ORDER BY rc.TABLE_NAME;

-- Oczekiwany wynik: 0 wierszy (brak FK)
