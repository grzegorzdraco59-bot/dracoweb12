-- Skrypt do synchronizacji wszystkich tabel z localbddraco do locbd
-- Automatycznie wykrywa wszystkie tabele i synchronizuje brakujące rekordy
-- Używa PRIMARY KEY do porównania rekordów

-- Tabele systemowe do pominięcia
SET @tables_to_skip = 'information_schema,mysql,performance_schema,sys';

-- Tymczasowa tabela do przechowywania wyników synchronizacji
CREATE TEMPORARY TABLE IF NOT EXISTS sync_results (
    table_name VARCHAR(255),
    records_to_add INT,
    records_added INT,
    status VARCHAR(50),
    error_message TEXT
);

-- Procedura do synchronizacji pojedynczej tabeli
DELIMITER $$

DROP PROCEDURE IF EXISTS SyncTableFromLocalbddraco$$

CREATE PROCEDURE SyncTableFromLocalbddraco(IN table_name_param VARCHAR(255))
BEGIN
    DECLARE done INT DEFAULT FALSE;
    DECLARE col_name VARCHAR(255);
    DECLARE col_type VARCHAR(255);
    DECLARE is_nullable VARCHAR(3);
    DECLARE column_key VARCHAR(3);
    DECLARE extra_info VARCHAR(255);
    
    DECLARE pk_column VARCHAR(255) DEFAULT NULL;
    DECLARE columns_list TEXT DEFAULT '';
    DECLARE select_list TEXT DEFAULT '';
    DECLARE where_clause TEXT DEFAULT '';
    DECLARE insert_sql TEXT;
    DECLARE records_to_add INT DEFAULT 0;
    DECLARE records_added INT DEFAULT 0;
    DECLARE error_msg TEXT DEFAULT NULL;
    
    DECLARE col_cursor CURSOR FOR
        SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_KEY, EXTRA
        FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = 'localbddraco'
        AND TABLE_NAME = table_name_param
        ORDER BY ORDINAL_POSITION;
    
    DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = TRUE;
    
    -- Sprawdź czy tabela istnieje w obu bazach
    SET @src_exists = (
        SELECT COUNT(*) 
        FROM information_schema.TABLES 
        WHERE TABLE_SCHEMA = 'localbddraco' 
        AND TABLE_NAME = table_name_param
    );
    
    SET @dest_exists = (
        SELECT COUNT(*) 
        FROM information_schema.TABLES 
        WHERE TABLE_SCHEMA = 'locbd' 
        AND TABLE_NAME = table_name_param
    );
    
    IF @src_exists = 0 THEN
        INSERT INTO sync_results VALUES (table_name_param, 0, 0, 'SKIPPED', 'Tabela nie istnieje w localbddraco');
    ELSEIF @dest_exists = 0 THEN
        INSERT INTO sync_results VALUES (table_name_param, 0, 0, 'SKIPPED', 'Tabela nie istnieje w locbd');
    ELSE
        -- Znajdź PRIMARY KEY
        SELECT COLUMN_NAME INTO pk_column
        FROM information_schema.KEY_COLUMN_USAGE
        WHERE TABLE_SCHEMA = 'localbddraco'
        AND TABLE_NAME = table_name_param
        AND CONSTRAINT_NAME = 'PRIMARY'
        LIMIT 1;
        
        IF pk_column IS NULL THEN
            INSERT INTO sync_results VALUES (table_name_param, 0, 0, 'SKIPPED', 'Brak PRIMARY KEY');
        ELSE
            -- Zbuduj listę kolumn
            SET done = FALSE;
            OPEN col_cursor;
            
            read_loop: LOOP
                FETCH col_cursor INTO col_name, col_type, is_nullable, column_key, extra_info;
                
                IF done THEN
                    LEAVE read_loop;
                END IF;
                
                -- Pomijamy kolumny AUTO_INCREMENT (będą ustawione automatycznie)
                IF extra_info NOT LIKE '%auto_increment%' THEN
                    IF columns_list != '' THEN
                        SET columns_list = CONCAT(columns_list, ', ');
                        SET select_list = CONCAT(select_list, ', ');
                    END IF;
                    SET columns_list = CONCAT(columns_list, '`', col_name, '`');
                    SET select_list = CONCAT(select_list, 'src.`', col_name, '`');
                END IF;
            END LOOP;
            
            CLOSE col_cursor;
            
            IF columns_list = '' THEN
                INSERT INTO sync_results VALUES (table_name_param, 0, 0, 'SKIPPED', 'Brak kolumn do synchronizacji');
            ELSE
                -- Sprawdź ile rekordów do dodania
                SET @count_sql = CONCAT(
                    'SELECT COUNT(*) INTO @rec_count FROM localbddraco.`', table_name_param, '` src ',
                    'WHERE NOT EXISTS (SELECT 1 FROM locbd.`', table_name_param, '` dest WHERE dest.`', pk_column, '` = src.`', pk_column, '`)'
                );
                PREPARE stmt FROM @count_sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                SET records_to_add = @rec_count;
                
                -- Wykonaj synchronizację
                IF records_to_add > 0 THEN
                    BEGIN
                        DECLARE CONTINUE HANDLER FOR SQLEXCEPTION
                        BEGIN
                            GET DIAGNOSTICS CONDITION 1
                                @sqlstate = RETURNED_SQLSTATE, 
                                @errno = MYSQL_ERRNO, 
                                @text = MESSAGE_TEXT;
                            SET error_msg = CONCAT(@sqlstate, ': ', @text);
                        END;
                        
                        SET insert_sql = CONCAT(
                            'INSERT INTO locbd.`', table_name_param, '` (', columns_list, ') ',
                            'SELECT ', select_list, ' ',
                            'FROM localbddraco.`', table_name_param, '` src ',
                            'WHERE NOT EXISTS (SELECT 1 FROM locbd.`', table_name_param, '` dest WHERE dest.`', pk_column, '` = src.`', pk_column, '`)'
                        );
                        
                        SET @sql = insert_sql;
                        PREPARE stmt FROM @sql;
                        EXECUTE stmt;
                        DEALLOCATE PREPARE stmt;
                        
                        SET records_added = ROW_COUNT();
                        
                        -- Jeśli tabela ma AUTO_INCREMENT, zaktualizuj wartość
                        SET @has_autoinc = (
                            SELECT COUNT(*) 
                            FROM information_schema.COLUMNS 
                            WHERE TABLE_SCHEMA = 'locbd' 
                            AND TABLE_NAME = table_name_param 
                            AND EXTRA LIKE '%auto_increment%'
                        );
                        
                        IF @has_autoinc > 0 THEN
                            SET @max_id_sql = CONCAT('SELECT COALESCE(MAX(`', pk_column, '`), 0) INTO @max_id FROM locbd.`', table_name_param, '`');
                            PREPARE stmt FROM @max_id_sql;
                            EXECUTE stmt;
                            DEALLOCATE PREPARE stmt;
                            
                            SET @autoinc_sql = CONCAT('ALTER TABLE locbd.`', table_name_param, '` AUTO_INCREMENT = ', @max_id + 1);
                            PREPARE stmt FROM @autoinc_sql;
                            EXECUTE stmt;
                            DEALLOCATE PREPARE stmt;
                        END IF;
                    END;
                END IF;
                
                -- Zapisz wynik
                IF error_msg IS NOT NULL THEN
                    INSERT INTO sync_results VALUES (table_name_param, records_to_add, records_added, 'ERROR', error_msg);
                ELSE
                    INSERT INTO sync_results VALUES (table_name_param, records_to_add, records_added, 'SUCCESS', NULL);
                END IF;
            END IF;
        END IF;
    END IF;
END$$

DELIMITER ;

-- Główna pętla synchronizacji wszystkich tabel
DELIMITER $$

DROP PROCEDURE IF EXISTS SyncAllTablesFromLocalbddraco$$

CREATE PROCEDURE SyncAllTablesFromLocalbddraco()
BEGIN
    DECLARE done INT DEFAULT FALSE;
    DECLARE table_name_var VARCHAR(255);
    
    DECLARE table_cursor CURSOR FOR
        SELECT TABLE_NAME
        FROM information_schema.TABLES
        WHERE TABLE_SCHEMA = 'localbddraco'
        AND TABLE_TYPE = 'BASE TABLE'
        ORDER BY TABLE_NAME;
    
    DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = TRUE;
    
    -- Wyczyść wyniki
    TRUNCATE TABLE sync_results;
    
    OPEN table_cursor;
    
    read_loop: LOOP
        FETCH table_cursor INTO table_name_var;
        
        IF done THEN
            LEAVE read_loop;
        END IF;
        
        -- Wywołaj synchronizację dla każdej tabeli
        CALL SyncTableFromLocalbddraco(table_name_var);
        
    END LOOP;
    
    CLOSE table_cursor;
    
    -- Wyświetl wyniki
    SELECT * FROM sync_results ORDER BY table_name;
    
    -- Podsumowanie
    SELECT 
        COUNT(*) AS total_tables,
        SUM(CASE WHEN status = 'SUCCESS' THEN 1 ELSE 0 END) AS successful,
        SUM(CASE WHEN status = 'ERROR' THEN 1 ELSE 0 END) AS errors,
        SUM(CASE WHEN status = 'SKIPPED' THEN 1 ELSE 0 END) AS skipped,
        SUM(records_added) AS total_records_added
    FROM sync_results;
END$$

DELIMITER ;

-- Uruchom synchronizację wszystkich tabel
CALL SyncAllTablesFromLocalbddraco();

-- Opcjonalnie: usuń procedury po użyciu
-- DROP PROCEDURE IF EXISTS SyncAllTablesFromLocalbddraco;
-- DROP PROCEDURE IF EXISTS SyncTableFromLocalbddraco;
DracoERP"