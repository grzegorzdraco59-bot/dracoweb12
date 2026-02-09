-- =============================================================================
-- 028: Procedura sp_next_id – jednolity mechanizm nadawania ID (migracja Clarion)
-- Używana przez IIdGenerator w C#. Zastępuje MAX(id)+1.
-- Wymaga: tabela id_sequences (025)
-- =============================================================================

USE locbd;

DELIMITER $$

DROP PROCEDURE IF EXISTS sp_next_id$$

CREATE PROCEDURE sp_next_id(IN p_table_name VARCHAR(64))
BEGIN
  DECLARE v_next BIGINT DEFAULT NULL;
  DECLARE v_new_id BIGINT;

  -- SELECT next_id FOR UPDATE (blokada wiersza)
  SELECT next_id INTO v_next
  FROM id_sequences
  WHERE table_name = p_table_name
  FOR UPDATE;

  IF v_next IS NULL THEN
    -- Brak rekordu: utwórz z next_id=2 (zwrócimy 1)
    INSERT INTO id_sequences (table_name, next_id) VALUES (p_table_name, 2);
    SET v_new_id = 1;
  ELSE
    SET v_new_id = v_next;
    UPDATE id_sequences SET next_id = next_id + 1 WHERE table_name = p_table_name;
  END IF;

  SELECT v_new_id AS new_id;
END$$

DELIMITER ;

-- Init dla tabel używanych w C# (jeśli brak w id_sequences)
INSERT IGNORE INTO id_sequences (table_name, next_id) VALUES ('aoferty', 1);
INSERT IGNORE INTO id_sequences (table_name, next_id) VALUES ('apozycjeoferty', 1);
INSERT IGNORE INTO id_sequences (table_name, next_id) VALUES ('faktury', 1);
INSERT IGNORE INTO id_sequences (table_name, next_id) VALUES ('pozycjefaktury', 1);
INSERT IGNORE INTO id_sequences (table_name, next_id) VALUES ('zamowienia', 1);
INSERT IGNORE INTO id_sequences (table_name, next_id) VALUES ('pozycjezamowienia', 1);
INSERT IGNORE INTO id_sequences (table_name, next_id) VALUES ('zlecenia', 1);
INSERT IGNORE INTO id_sequences (table_name, next_id) VALUES ('pozycjezlecenia', 1);
INSERT IGNORE INTO id_sequences (table_name, next_id) VALUES ('zamowieniahala', 1);
