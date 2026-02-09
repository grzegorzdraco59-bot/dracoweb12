-- =============================================================================
-- 022: Usunięcie FOREIGN KEY z tabeli operator_table_permissions (locbd)
-- MariaDB/MySQL
-- =============================================================================

-- 1) Sprawdzenie nazwy FK (uruchom przed DROP)
SELECT CONSTRAINT_NAME
FROM information_schema.REFERENTIAL_CONSTRAINTS
WHERE CONSTRAINT_SCHEMA = 'locbd'
  AND TABLE_NAME = 'operator_table_permissions';


-- 2) Usunięcie FK (typowa nazwa: operator_table_permissions_ibfk_1)
SET foreign_key_checks = 0;

ALTER TABLE `operator_table_permissions` DROP FOREIGN KEY `operator_table_permissions_ibfk_1`;

SET foreign_key_checks = 1;


-- 3) Weryfikacja
SELECT COUNT(*) AS fk_count
FROM information_schema.TABLE_CONSTRAINTS
WHERE CONSTRAINT_SCHEMA = 'locbd'
  AND CONSTRAINT_TYPE = 'FOREIGN KEY'
  AND TABLE_NAME = 'operator_table_permissions';
-- Oczekiwany wynik: fk_count = 0
