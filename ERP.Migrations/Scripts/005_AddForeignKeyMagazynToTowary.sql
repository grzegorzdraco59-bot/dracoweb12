-- Skrypt do dodania klucza obcego między magazyn.id_towaru a towary.id
-- UWAGA: Upewnij się, że tabele towary i magazyn istnieją przed uruchomieniem

-- Sprawdzenie czy foreign key już istnieje
SET @foreign_key_exists = (
    SELECT COUNT(*) 
    FROM information_schema.KEY_COLUMN_USAGE 
    WHERE TABLE_SCHEMA = 'locbd' 
    AND TABLE_NAME = 'magazyn' 
    AND COLUMN_NAME = 'id_towaru' 
    AND REFERENCED_TABLE_NAME = 'towary'
);

-- Dodanie foreign key jeśli nie istnieje
SET @sql = IF(@foreign_key_exists = 0,
    'ALTER TABLE magazyn 
     ADD CONSTRAINT fk_magazyn_towary 
     FOREIGN KEY (id_towaru) REFERENCES towary(id) 
     ON DELETE SET NULL 
     ON UPDATE CASCADE',
    'SELECT "Foreign key fk_magazyn_towary już istnieje" AS message'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Dodanie indeksu dla lepszej wydajności (jeśli nie istnieje)
CREATE INDEX IF NOT EXISTS idx_magazyn_id_towaru ON magazyn(id_towaru);
CREATE INDEX IF NOT EXISTS idx_magazyn_id_firmy ON magazyn(id_firmy);
CREATE INDEX IF NOT EXISTS idx_towary_id_firmy ON towary(id_firmy);
