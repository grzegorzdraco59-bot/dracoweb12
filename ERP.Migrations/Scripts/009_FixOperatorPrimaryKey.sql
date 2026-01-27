-- Skrypt do naprawy tabeli operator - dodanie PRIMARY KEY i AUTO_INCREMENT do kolumny id_operatora
-- Ten skrypt dodaje PRIMARY KEY i AUTO_INCREMENT jeśli ich brakuje

-- Krok 1: Sprawdź czy PRIMARY KEY istnieje
SET @pk_exists = (
    SELECT COUNT(*) 
    FROM information_schema.KEY_COLUMN_USAGE 
    WHERE TABLE_SCHEMA = 'locbd' 
    AND TABLE_NAME = 'operator' 
    AND CONSTRAINT_NAME = 'PRIMARY'
);

-- Krok 2: Sprawdź czy AUTO_INCREMENT jest ustawiony
SET @autoinc_exists = (
    SELECT COUNT(*) 
    FROM information_schema.COLUMNS 
    WHERE TABLE_SCHEMA = 'locbd' 
    AND TABLE_NAME = 'operator' 
    AND COLUMN_NAME = 'id_operatora' 
    AND EXTRA LIKE '%auto_increment%'
);

-- Krok 3: Jeśli PRIMARY KEY nie istnieje i AUTO_INCREMENT nie istnieje, dodaj oba
SET @sql_both = IF(@pk_exists = 0 AND @autoinc_exists = 0,
    'ALTER TABLE operator MODIFY COLUMN id_operatora INT(15) NOT NULL AUTO_INCREMENT PRIMARY KEY',
    IF(@pk_exists = 0 AND @autoinc_exists > 0,
        'ALTER TABLE operator MODIFY COLUMN id_operatora INT(15) NOT NULL AUTO_INCREMENT, ADD PRIMARY KEY (id_operatora)',
        IF(@pk_exists > 0 AND @autoinc_exists = 0,
            'ALTER TABLE operator MODIFY COLUMN id_operatora INT(15) NOT NULL AUTO_INCREMENT',
            'SELECT "PRIMARY KEY i AUTO_INCREMENT już skonfigurowane" AS message'
        )
    )
);

PREPARE stmt_both FROM @sql_both;
EXECUTE stmt_both;
DEALLOCATE PREPARE stmt_both;

-- Krok 4: Upewnij się, że kolumna id_operatora jest NOT NULL i ma AUTO_INCREMENT
ALTER TABLE operator MODIFY COLUMN id_operatora INT(15) NOT NULL AUTO_INCREMENT;

-- Sprawdź strukturę tabeli po zmianach
DESCRIBE operator;
