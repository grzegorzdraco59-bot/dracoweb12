-- Skrypt do dodania klucza obcego między apozycjeoferty.ID_oferta a aoferty.ID_oferta
-- UWAGA: Upewnij się, że tabele aoferty i apozycjeoferty istnieją przed uruchomieniem

-- Sprawdzenie czy foreign key już istnieje
SET @foreign_key_exists = (
    SELECT COUNT(*) 
    FROM information_schema.KEY_COLUMN_USAGE 
    WHERE TABLE_SCHEMA = 'locbd' 
    AND TABLE_NAME = 'apozycjeoferty' 
    AND COLUMN_NAME = 'ID_oferta' 
    AND REFERENCED_TABLE_NAME = 'aoferty'
);

-- Dodanie foreign key jeśli nie istnieje
SET @sql = IF(@foreign_key_exists = 0,
    'ALTER TABLE apozycjeoferty 
     ADD CONSTRAINT fk_apozycjeoferty_aoferty 
     FOREIGN KEY (ID_oferta) REFERENCES aoferty(ID_oferta) 
     ON DELETE CASCADE 
     ON UPDATE CASCADE',
    'SELECT "Foreign key fk_apozycjeoferty_aoferty już istnieje" AS message'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Dodanie indeksu dla lepszej wydajności (jeśli nie istnieje)
CREATE INDEX IF NOT EXISTS idx_apozycjeoferty_ID_oferta ON apozycjeoferty(ID_oferta);
CREATE INDEX IF NOT EXISTS idx_apozycjeoferty_id_firmy ON apozycjeoferty(id_firmy);
CREATE INDEX IF NOT EXISTS idx_aoferty_id_firmy ON aoferty(id_firmy);
