-- Skrypt do dodania kluczy obcych dla operatorfirma
-- operatorfirma.id_operatora -> operator.id_operatora
-- operatorfirma.id_firmy -> firmy.ID_FIRMY

-- Sprawdzenie czy foreign key operatorfirma -> operator już istnieje
SET @foreign_key_operator_exists = (
    SELECT COUNT(*) 
    FROM information_schema.KEY_COLUMN_USAGE 
    WHERE TABLE_SCHEMA = 'locbd' 
    AND TABLE_NAME = 'operatorfirma' 
    AND COLUMN_NAME = 'id_operatora' 
    AND REFERENCED_TABLE_NAME = 'operator'
);

-- Sprawdzenie czy foreign key operatorfirma -> firmy już istnieje
SET @foreign_key_firma_exists = (
    SELECT COUNT(*) 
    FROM information_schema.KEY_COLUMN_USAGE 
    WHERE TABLE_SCHEMA = 'locbd' 
    AND TABLE_NAME = 'operatorfirma' 
    AND COLUMN_NAME = 'id_firmy' 
    AND REFERENCED_TABLE_NAME = 'firmy'
);

-- Dodanie foreign key operatorfirma -> operator jeśli nie istnieje
SET @sql_operator = IF(@foreign_key_operator_exists = 0,
    'ALTER TABLE operatorfirma 
     ADD CONSTRAINT fk_operatorfirma_operator 
     FOREIGN KEY (id_operatora) REFERENCES operator(id_operatora) 
     ON DELETE CASCADE 
     ON UPDATE CASCADE',
    'SELECT "Foreign key fk_operatorfirma_operator już istnieje" AS message'
);

PREPARE stmt_operator FROM @sql_operator;
EXECUTE stmt_operator;
DEALLOCATE PREPARE stmt_operator;

-- Dodanie foreign key operatorfirma -> firmy jeśli nie istnieje
SET @sql_firma = IF(@foreign_key_firma_exists = 0,
    'ALTER TABLE operatorfirma 
     ADD CONSTRAINT fk_operatorfirma_firmy 
     FOREIGN KEY (id_firmy) REFERENCES firmy(ID_FIRMY) 
     ON DELETE CASCADE 
     ON UPDATE CASCADE',
    'SELECT "Foreign key fk_operatorfirma_firmy już istnieje" AS message'
);

PREPARE stmt_firma FROM @sql_firma;
EXECUTE stmt_firma;
DEALLOCATE PREPARE stmt_firma;

-- Dodanie indeksów dla lepszej wydajności (jeśli nie istnieją)
CREATE INDEX IF NOT EXISTS idx_operatorfirma_id_operatora ON operatorfirma(id_operatora);
CREATE INDEX IF NOT EXISTS idx_operatorfirma_id_firmy ON operatorfirma(id_firmy);
