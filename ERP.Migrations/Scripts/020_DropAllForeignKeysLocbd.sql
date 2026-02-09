-- =============================================================================
-- 020: Usunięcie WSZYSTKICH FOREIGN KEY z bazy locbd
-- MariaDB/MySQL – gotowe do wklejenia w HeidiSQL / DBeaver / phpMyAdmin
-- =============================================================================

-- =============================================================================
-- A) RAPORT: Wszystkie FK w locbd
-- =============================================================================

SELECT
    rc.TABLE_NAME AS TABLE_NAME,
    rc.CONSTRAINT_NAME AS CONSTRAINT_NAME,
    kcu.COLUMN_NAME AS COLUMN_NAME,
    rc.REFERENCED_TABLE_NAME AS REFERENCED_TABLE_NAME,
    kcu.REFERENCED_COLUMN_NAME AS REFERENCED_COLUMN_NAME,
    rc.UPDATE_RULE AS UPDATE_RULE,
    rc.DELETE_RULE AS DELETE_RULE
FROM information_schema.REFERENTIAL_CONSTRAINTS rc
JOIN information_schema.KEY_COLUMN_USAGE kcu
    ON rc.CONSTRAINT_SCHEMA = kcu.CONSTRAINT_SCHEMA
   AND rc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
   AND rc.TABLE_NAME = kcu.TABLE_NAME
WHERE rc.CONSTRAINT_SCHEMA = 'locbd'
ORDER BY rc.TABLE_NAME, rc.CONSTRAINT_NAME, kcu.ORDINAL_POSITION;


-- =============================================================================
-- B) FINALNY SKRYPT: DROP wszystkich FK (uruchom po skopiowaniu wyniku generatora)
--    LUB uruchom poniższy SELECT i skopiuj wartość kolumny final_script
-- =============================================================================

SELECT CONCAT(
  'SET foreign_key_checks = 0;\n',
  IFNULL(
    (SELECT GROUP_CONCAT(
       CONCAT('ALTER TABLE `', TABLE_NAME, '` DROP FOREIGN KEY `', CONSTRAINT_NAME, '`;')
       ORDER BY TABLE_NAME, CONSTRAINT_NAME
       SEPARATOR '\n'
     )
     FROM information_schema.REFERENTIAL_CONSTRAINTS
     WHERE CONSTRAINT_SCHEMA = 'locbd'),
    ''
  ),
  '\nSET foreign_key_checks = 1;'
) AS final_script;


-- =============================================================================
-- B) SKRYPT GOTOWY (wklej poniżej do klienta SQL i wykonaj)
-- =============================================================================

SET foreign_key_checks = 0;

-- Wklej tutaj wygenerowane ALTER TABLE ... DROP FOREIGN KEY ...;
-- (wynik z SELECT final_script powyżej)

SET foreign_key_checks = 1;


-- =============================================================================
-- C) WERYFIKACJA: Brak FK w locbd
-- =============================================================================

SELECT COUNT(*) AS fk_count
FROM information_schema.TABLE_CONSTRAINTS
WHERE CONSTRAINT_SCHEMA = 'locbd'
  AND CONSTRAINT_TYPE = 'FOREIGN KEY';

-- Oczekiwany wynik po wykonaniu skryptu: fk_count = 0
