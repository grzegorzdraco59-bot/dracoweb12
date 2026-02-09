-- =============================================================================
-- 025: Tabela id_sequences – naprawia błąd "Table 'locbd.id_sequences' doesn't exist"
-- Uruchom na bazie locbd: USE locbd; następnie ten skrypt.
-- Używana przez IdGeneratorService przy INSERT i kopiowaniu parent+children.
-- =============================================================================

USE locbd;

CREATE TABLE IF NOT EXISTS id_sequences (
  table_name VARCHAR(64) NOT NULL PRIMARY KEY,
  next_id BIGINT NOT NULL
) ENGINE=InnoDB;

-- Init dla aoferty (opcjonalnie – IdGeneratorService sam utworzy wpis jeśli brak)
INSERT IGNORE INTO id_sequences (table_name, next_id) VALUES ('aoferty', 1);
