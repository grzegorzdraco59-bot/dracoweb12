-- =============================================================================
-- 021: Usunięcie FOREIGN KEY z tabel: oferty, ofertypozycje, operator_table_permissions
-- Baza: locbd (MariaDB/MySQL)
-- =============================================================================

-- =============================================================================
-- A) RAPORT: FK w tabelach oferty, ofertypozycje, operator_table_permissions
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
  AND rc.TABLE_NAME IN ('oferty', 'ofertypozycje', 'operator_table_permissions')
ORDER BY rc.TABLE_NAME, rc.CONSTRAINT_NAME, kcu.ORDINAL_POSITION;


-- =============================================================================
-- B) FINALNY SKRYPT: DROP FOREIGN KEY (skopiuj wynik final_script i wykonaj)
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
     WHERE CONSTRAINT_SCHEMA = 'locbd'
       AND TABLE_NAME IN ('oferty', 'ofertypozycje', 'operator_table_permissions')),
    ''
  ),
  '\nSET foreign_key_checks = 1;'
) AS final_script;


-- =============================================================================
-- B) SKRYPT GOTOWY (wklej wynik z SELECT final_script powyżej i wykonaj)
-- =============================================================================

SET foreign_key_checks = 0;

-- ALTER TABLE `oferty` DROP FOREIGN KEY `<nazwa>`;
-- ALTER TABLE `ofertypozycje` DROP FOREIGN KEY `<nazwa>`;
-- ALTER TABLE `operator_table_permissions` DROP FOREIGN KEY `<nazwa>`;

SET foreign_key_checks = 1;


-- =============================================================================
-- C) WERYFIKACJA: Brak FK w tych tabelach
-- =============================================================================

SELECT COUNT(*) AS fk_count
FROM information_schema.TABLE_CONSTRAINTS
WHERE CONSTRAINT_SCHEMA = 'locbd'
  AND CONSTRAINT_TYPE = 'FOREIGN KEY'
  AND TABLE_NAME IN ('oferty', 'ofertypozycje', 'operator_table_permissions');

-- Oczekiwany wynik po wykonaniu skryptu: fk_count = 0
