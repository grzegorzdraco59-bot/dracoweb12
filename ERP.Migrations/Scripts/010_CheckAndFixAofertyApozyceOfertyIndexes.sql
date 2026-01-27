-- Skrypt do sprawdzenia i naprawy indeksów oraz kluczy w tabelach aoferty i apozycjeoferty
-- Sprawdza PRIMARY KEY, FOREIGN KEY oraz indeksy

USE locbd;

-- ============================================
-- TABELA: aoferty
-- ============================================

-- Sprawdzenie PRIMARY KEY na ID_oferta
SET @pk_aoferty_exists = (
    SELECT COUNT(*) 
    FROM information_schema.KEY_COLUMN_USAGE 
    WHERE TABLE_SCHEMA = 'locbd' 
    AND TABLE_NAME = 'aoferty' 
    AND COLUMN_NAME = 'ID_oferta' 
    AND CONSTRAINT_NAME = 'PRIMARY'
);

-- Sprawdzenie AUTO_INCREMENT na ID_oferta
SET @autoinc_aoferty_exists = (
    SELECT COUNT(*) 
    FROM information_schema.COLUMNS 
    WHERE TABLE_SCHEMA = 'locbd' 
    AND TABLE_NAME = 'aoferty' 
    AND COLUMN_NAME = 'ID_oferta' 
    AND EXTRA LIKE '%auto_increment%'
);

-- Sprawdzenie indeksu na id_firmy
SET @idx_aoferty_id_firmy_exists = (
    SELECT COUNT(*) 
    FROM information_schema.STATISTICS 
    WHERE TABLE_SCHEMA = 'locbd' 
    AND TABLE_NAME = 'aoferty' 
    AND INDEX_NAME = 'idx_aoferty_id_firmy'
);

-- Naprawa PRIMARY KEY i AUTO_INCREMENT dla aoferty
SET @sql_aoferty_pk = IF(@pk_aoferty_exists = 0 AND @autoinc_aoferty_exists = 0,
    'ALTER TABLE aoferty MODIFY COLUMN ID_oferta INT(15) NOT NULL AUTO_INCREMENT PRIMARY KEY',
    IF(@pk_aoferty_exists = 0 AND @autoinc_aoferty_exists > 0,
        'ALTER TABLE aoferty MODIFY COLUMN ID_oferta INT(15) NOT NULL AUTO_INCREMENT, ADD PRIMARY KEY (ID_oferta)',
        IF(@pk_aoferty_exists > 0 AND @autoinc_aoferty_exists = 0,
            'ALTER TABLE aoferty MODIFY COLUMN ID_oferta INT(15) NOT NULL AUTO_INCREMENT',
            'SELECT "PRIMARY KEY i AUTO_INCREMENT dla aoferty już skonfigurowane" AS message'
        )
    )
);

PREPARE stmt_aoferty_pk FROM @sql_aoferty_pk;
EXECUTE stmt_aoferty_pk;
DEALLOCATE PREPARE stmt_aoferty_pk;

-- Dodanie indeksu na id_firmy dla aoferty (jeśli nie istnieje)
SET @sql_aoferty_idx = IF(@idx_aoferty_id_firmy_exists = 0,
    'CREATE INDEX idx_aoferty_id_firmy ON aoferty(id_firmy)',
    'SELECT "Indeks idx_aoferty_id_firmy już istnieje" AS message'
);

PREPARE stmt_aoferty_idx FROM @sql_aoferty_idx;
EXECUTE stmt_aoferty_idx;
DEALLOCATE PREPARE stmt_aoferty_idx;

-- ============================================
-- TABELA: apozycjeoferty
-- ============================================

-- Sprawdzenie PRIMARY KEY na ID_pozycja_oferty
SET @pk_apozycje_exists = (
    SELECT COUNT(*) 
    FROM information_schema.KEY_COLUMN_USAGE 
    WHERE TABLE_SCHEMA = 'locbd' 
    AND TABLE_NAME = 'apozycjeoferty' 
    AND COLUMN_NAME = 'ID_pozycja_oferty' 
    AND CONSTRAINT_NAME = 'PRIMARY'
);

-- Sprawdzenie AUTO_INCREMENT na ID_pozycja_oferty
SET @autoinc_apozycje_exists = (
    SELECT COUNT(*) 
    FROM information_schema.COLUMNS 
    WHERE TABLE_SCHEMA = 'locbd' 
    AND TABLE_NAME = 'apozycjeoferty' 
    AND COLUMN_NAME = 'ID_pozycja_oferty' 
    AND EXTRA LIKE '%auto_increment%'
);

-- Sprawdzenie FOREIGN KEY fk_apozycjeoferty_aoferty
SET @fk_apozycje_exists = (
    SELECT COUNT(*) 
    FROM information_schema.KEY_COLUMN_USAGE 
    WHERE TABLE_SCHEMA = 'locbd' 
    AND TABLE_NAME = 'apozycjeoferty' 
    AND COLUMN_NAME = 'ID_oferta' 
    AND REFERENCED_TABLE_NAME = 'aoferty'
    AND REFERENCED_COLUMN_NAME = 'ID_oferta'
);

-- Sprawdzenie indeksu na ID_oferta
SET @idx_apozycje_ID_oferta_exists = (
    SELECT COUNT(*) 
    FROM information_schema.STATISTICS 
    WHERE TABLE_SCHEMA = 'locbd' 
    AND TABLE_NAME = 'apozycjeoferty' 
    AND INDEX_NAME = 'idx_apozycjeoferty_ID_oferta'
);

-- Sprawdzenie indeksu na id_firmy
SET @idx_apozycje_id_firmy_exists = (
    SELECT COUNT(*) 
    FROM information_schema.STATISTICS 
    WHERE TABLE_SCHEMA = 'locbd' 
    AND TABLE_NAME = 'apozycjeoferty' 
    AND INDEX_NAME = 'idx_apozycjeoferty_id_firmy'
);

-- Naprawa PRIMARY KEY i AUTO_INCREMENT dla apozycjeoferty
SET @sql_apozycje_pk = IF(@pk_apozycje_exists = 0 AND @autoinc_apozycje_exists = 0,
    'ALTER TABLE apozycjeoferty MODIFY COLUMN ID_pozycja_oferty INT(15) NOT NULL AUTO_INCREMENT PRIMARY KEY',
    IF(@pk_apozycje_exists = 0 AND @autoinc_apozycje_exists > 0,
        'ALTER TABLE apozycjeoferty MODIFY COLUMN ID_pozycja_oferty INT(15) NOT NULL AUTO_INCREMENT, ADD PRIMARY KEY (ID_pozycja_oferty)',
        IF(@pk_apozycje_exists > 0 AND @autoinc_apozycje_exists = 0,
            'ALTER TABLE apozycjeoferty MODIFY COLUMN ID_pozycja_oferty INT(15) NOT NULL AUTO_INCREMENT',
            'SELECT "PRIMARY KEY i AUTO_INCREMENT dla apozycjeoferty już skonfigurowane" AS message'
        )
    )
);

PREPARE stmt_apozycje_pk FROM @sql_apozycje_pk;
EXECUTE stmt_apozycje_pk;
DEALLOCATE PREPARE stmt_apozycje_pk;

-- Dodanie FOREIGN KEY (jeśli nie istnieje)
SET @sql_apozycje_fk = IF(@fk_apozycje_exists = 0,
    'ALTER TABLE apozycjeoferty 
     ADD CONSTRAINT fk_apozycjeoferty_aoferty 
     FOREIGN KEY (ID_oferta) REFERENCES aoferty(ID_oferta) 
     ON DELETE CASCADE 
     ON UPDATE CASCADE',
    'SELECT "Foreign key fk_apozycjeoferty_aoferty już istnieje" AS message'
);

PREPARE stmt_apozycje_fk FROM @sql_apozycje_fk;
EXECUTE stmt_apozycje_fk;
DEALLOCATE PREPARE stmt_apozycje_fk;

-- Dodanie indeksu na ID_oferta (jeśli nie istnieje)
SET @sql_apozycje_idx_oferta = IF(@idx_apozycje_ID_oferta_exists = 0,
    'CREATE INDEX idx_apozycjeoferty_ID_oferta ON apozycjeoferty(ID_oferta)',
    'SELECT "Indeks idx_apozycjeoferty_ID_oferta już istnieje" AS message'
);

PREPARE stmt_apozycje_idx_oferta FROM @sql_apozycje_idx_oferta;
EXECUTE stmt_apozycje_idx_oferta;
DEALLOCATE PREPARE stmt_apozycje_idx_oferta;

-- Dodanie indeksu na id_firmy (jeśli nie istnieje)
SET @sql_apozycje_idx_firmy = IF(@idx_apozycje_id_firmy_exists = 0,
    'CREATE INDEX idx_apozycjeoferty_id_firmy ON apozycjeoferty(id_firmy)',
    'SELECT "Indeks idx_apozycjeoferty_id_firmy już istnieje" AS message'
);

PREPARE stmt_apozycje_idx_firmy FROM @sql_apozycje_idx_firmy;
EXECUTE stmt_apozycje_idx_firmy;
DEALLOCATE PREPARE stmt_apozycje_idx_firmy;

-- ============================================
-- RAPORT KOŃCOWY - Wyświetlenie struktury
-- ============================================

SELECT '=== STRUKTURA TABELI: aoferty ===' AS info;
DESCRIBE aoferty;

SELECT '=== INDEKSY TABELI: aoferty ===' AS info;
SHOW INDEXES FROM aoferty;

SELECT '=== STRUKTURA TABELI: apozycjeoferty ===' AS info;
DESCRIBE apozycjeoferty;

SELECT '=== INDEKSY TABELI: apozycjeoferty ===' AS info;
SHOW INDEXES FROM apozycjeoferty;

-- Sprawdzenie FOREIGN KEY dla apozycjeoferty
SELECT '=== FOREIGN KEYS TABELI: apozycjeoferty ===' AS info;
SELECT 
    CONSTRAINT_NAME,
    TABLE_NAME,
    COLUMN_NAME,
    REFERENCED_TABLE_NAME,
    REFERENCED_COLUMN_NAME
FROM information_schema.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = 'locbd'
AND TABLE_NAME = 'apozycjeoferty'
AND REFERENCED_TABLE_NAME IS NOT NULL;
