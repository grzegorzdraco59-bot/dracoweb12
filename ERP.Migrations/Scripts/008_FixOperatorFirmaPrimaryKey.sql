-- Skrypt do naprawy tabeli operatorfirma - usunięcie rekordów z NULL id i dodanie PRIMARY KEY
-- Ten skrypt usuwa rekordy z pustym id i dodaje PRIMARY KEY jeśli go brakuje

-- Krok 1: Usuń rekordy z NULL id (będą one miały niepoprawne dane)
DELETE FROM operatorfirma WHERE id IS NULL;

-- Krok 2: Sprawdź czy kolumna id ma AUTO_INCREMENT i PRIMARY KEY
-- Najpierw sprawdź czy PRIMARY KEY istnieje
SET @pk_exists = (
    SELECT COUNT(*) 
    FROM information_schema.KEY_COLUMN_USAGE 
    WHERE TABLE_SCHEMA = 'locbd' 
    AND TABLE_NAME = 'operatorfirma' 
    AND CONSTRAINT_NAME = 'PRIMARY'
);

-- Jeśli PRIMARY KEY nie istnieje, dodaj go
SET @sql_pk = IF(@pk_exists = 0,
    'ALTER TABLE operatorfirma MODIFY COLUMN id INT(15) NOT NULL AUTO_INCREMENT PRIMARY KEY',
    'SELECT "PRIMARY KEY już istnieje" AS message'
);

PREPARE stmt_pk FROM @sql_pk;
EXECUTE stmt_pk;
DEALLOCATE PREPARE stmt_pk;

-- Krok 3: Upewnij się, że kolumna id ma AUTO_INCREMENT (jeśli PRIMARY KEY już istniał, ale nie miał AUTO_INCREMENT)
-- Sprawdź czy AUTO_INCREMENT jest ustawiony
SET @autoinc_exists = (
    SELECT COUNT(*) 
    FROM information_schema.COLUMNS 
    WHERE TABLE_SCHEMA = 'locbd' 
    AND TABLE_NAME = 'operatorfirma' 
    AND COLUMN_NAME = 'id' 
    AND EXTRA LIKE '%auto_increment%'
);

-- Jeśli AUTO_INCREMENT nie istnieje, dodaj go (ale tylko jeśli PRIMARY KEY istnieje)
SET @sql_autoinc = IF(@autoinc_exists = 0 AND @pk_exists > 0,
    'ALTER TABLE operatorfirma MODIFY COLUMN id INT(15) NOT NULL AUTO_INCREMENT',
    IF(@autoinc_exists = 0 AND @pk_exists = 0,
        'ALTER TABLE operatorfirma MODIFY COLUMN id INT(15) NOT NULL AUTO_INCREMENT PRIMARY KEY',
        'SELECT "AUTO_INCREMENT już skonfigurowany" AS message'
    )
);

PREPARE stmt_autoinc FROM @sql_autoinc;
EXECUTE stmt_autoinc;
DEALLOCATE PREPARE stmt_autoinc;

-- Krok 4: Upewnij się, że kolumny id_operatora i id_firmy są NOT NULL
ALTER TABLE operatorfirma MODIFY COLUMN id_operatora INT(15) NOT NULL;
ALTER TABLE operatorfirma MODIFY COLUMN id_firmy INT(15) NOT NULL;

-- Sprawdź strukturę tabeli po zmianach
DESCRIBE operatorfirma;
