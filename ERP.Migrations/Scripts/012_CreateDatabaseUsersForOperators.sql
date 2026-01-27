-- Skrypt do tworzenia użytkowników bazy danych dla operatorów z uprawnieniami do tabel
-- UWAGA: Ten skrypt wymaga uprawnień administratora bazy danych (root)
-- Uruchom ten skrypt jako użytkownik z uprawnieniami CREATE USER i GRANT

-- Krok 1: Procedura do tworzenia użytkownika MySQL dla operatora
DELIMITER //

CREATE PROCEDURE IF NOT EXISTS sp_CreateOperatorDatabaseUser(
    IN p_id_operatora INT(15),
    IN p_username VARCHAR(100),
    IN p_password VARCHAR(255)
)
BEGIN
    DECLARE v_user_exists INT DEFAULT 0;
    DECLARE v_sql TEXT;
    
    -- Sprawdź czy użytkownik już istnieje
    SELECT COUNT(*) INTO v_user_exists
    FROM mysql.user
    WHERE User = p_username AND Host = 'localhost';
    
    -- Utwórz użytkownika jeśli nie istnieje
    IF v_user_exists = 0 THEN
        SET v_sql = CONCAT('CREATE USER ''', p_username, '''@''localhost'' IDENTIFIED BY ''', p_password, '''');
        SET @sql = v_sql;
        PREPARE stmt FROM @sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;
    
    -- Nadaj podstawowe uprawnienia do bazy danych locbd
    SET v_sql = CONCAT('GRANT USAGE ON locbd.* TO ''', p_username, '''@''localhost''');
    SET @sql = v_sql;
    PREPARE stmt FROM @sql;
    EXECUTE stmt;
    DEALLOCATE PREPARE stmt;
END //

DELIMITER ;

-- Krok 2: Procedura do nadawania uprawnień do konkretnej tabeli na podstawie operator_table_permissions
DELIMITER //

CREATE PROCEDURE IF NOT EXISTS sp_GrantTablePermissionsToOperator(
    IN p_id_operatora INT(15),
    IN p_username VARCHAR(100)
)
BEGIN
    DECLARE done INT DEFAULT FALSE;
    DECLARE v_table_name VARCHAR(100);
    DECLARE v_can_select TINYINT(1);
    DECLARE v_can_insert TINYINT(1);
    DECLARE v_can_update TINYINT(1);
    DECLARE v_can_delete TINYINT(1);
    DECLARE v_permissions TEXT DEFAULT '';
    DECLARE v_sql TEXT;
    
    -- Kursor do iteracji po uprawnieniach operatora
    DECLARE cur_permissions CURSOR FOR
        SELECT table_name, can_select, can_insert, can_update, can_delete
        FROM operator_table_permissions
        WHERE id_operatora = p_id_operatora;
    
    DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = TRUE;
    
    -- Najpierw odbierz wszystkie uprawnienia (REVOKE ALL)
    SET v_sql = CONCAT('REVOKE ALL PRIVILEGES ON locbd.* FROM ''', p_username, '''@''localhost''');
    SET @sql = v_sql;
    PREPARE stmt FROM @sql;
    EXECUTE stmt;
    DEALLOCATE PREPARE stmt;
    
    -- Nadaj uprawnienia do każdej tabeli zgodnie z operator_table_permissions
    OPEN cur_permissions;
    
    read_loop: LOOP
        FETCH cur_permissions INTO v_table_name, v_can_select, v_can_insert, v_can_update, v_can_delete;
        
        IF done THEN
            LEAVE read_loop;
        END IF;
        
        -- Zbuduj listę uprawnień
        SET v_permissions = '';
        
        IF v_can_select = 1 THEN
            SET v_permissions = CONCAT(v_permissions, IF(v_permissions = '', 'SELECT', ', SELECT'));
        END IF;
        
        IF v_can_insert = 1 THEN
            SET v_permissions = CONCAT(v_permissions, IF(v_permissions = '', 'INSERT', ', INSERT'));
        END IF;
        
        IF v_can_update = 1 THEN
            SET v_permissions = CONCAT(v_permissions, IF(v_permissions = '', 'UPDATE', ', UPDATE'));
        END IF;
        
        IF v_can_delete = 1 THEN
            SET v_permissions = CONCAT(v_permissions, IF(v_permissions = '', 'DELETE', ', DELETE'));
        END IF;
        
        -- Nadaj uprawnienia jeśli są jakieś do nadania
        IF v_permissions != '' THEN
            SET v_sql = CONCAT('GRANT ', v_permissions, ' ON locbd.', v_table_name, ' TO ''', p_username, '''@''localhost''');
            SET @sql = v_sql;
            PREPARE stmt FROM @sql;
            EXECUTE stmt;
            DEALLOCATE PREPARE stmt;
        END IF;
    END LOOP;
    
    CLOSE cur_permissions;
    
    -- Zastosuj zmiany
    FLUSH PRIVILEGES;
END //

DELIMITER ;

-- Krok 3: Procedura do usunięcia użytkownika MySQL dla operatora
DELIMITER //

CREATE PROCEDURE IF NOT EXISTS sp_DropOperatorDatabaseUser(
    IN p_username VARCHAR(100)
)
BEGIN
    DECLARE v_sql TEXT;
    
    -- Usuń użytkownika
    SET v_sql = CONCAT('DROP USER IF EXISTS ''', p_username, '''@''localhost''');
    SET @sql = v_sql;
    PREPARE stmt FROM @sql;
    EXECUTE stmt;
    DEALLOCATE PREPARE stmt;
    
    -- Zastosuj zmiany
    FLUSH PRIVILEGES;
END //

DELIMITER ;

-- Krok 4: Widok do przeglądania użytkowników MySQL powiązanych z operatorami
CREATE OR REPLACE VIEW v_operator_database_users AS
SELECT 
    o.id_operatora,
    o.imie_nazwisko,
    ol.login AS database_username,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM mysql.user 
            WHERE User = ol.login AND Host = 'localhost'
        ) THEN 'TAK'
        ELSE 'NIE'
    END AS user_exists
FROM operator o
INNER JOIN operator_login ol ON o.id_operatora = ol.id_operatora;

-- Przykładowe użycie:
-- 1. Utworzenie użytkownika MySQL dla operatora (użyj login z tabeli operator_login)
-- CALL sp_CreateOperatorDatabaseUser(1, 'operator1', 'bezpieczne_haslo_123');

-- 2. Nadanie uprawnień do tabel na podstawie operator_table_permissions
-- CALL sp_GrantTablePermissionsToOperator(1, 'operator1');

-- 3. Usunięcie użytkownika MySQL
-- CALL sp_DropOperatorDatabaseUser('operator1');

-- 4. Sprawdzenie użytkowników powiązanych z operatorami
-- SELECT * FROM v_operator_database_users;
