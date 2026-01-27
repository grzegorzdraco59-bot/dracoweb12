-- Skrypt do utworzenia systemu uprawnień operatorów do poszczególnych tabel
-- Ten skrypt tworzy tabelę do przechowywania uprawnień oraz procedury do zarządzania nimi

-- Krok 1: Utworzenie tabeli do przechowywania uprawnień operatorów do tabel
CREATE TABLE IF NOT EXISTS operator_table_permissions (
    id INT(15) AUTO_INCREMENT PRIMARY KEY,
    id_operatora INT(15) NOT NULL,
    table_name VARCHAR(100) NOT NULL,
    can_select TINYINT(1) NOT NULL DEFAULT 0,
    can_insert TINYINT(1) NOT NULL DEFAULT 0,
    can_update TINYINT(1) NOT NULL DEFAULT 0,
    can_delete TINYINT(1) NOT NULL DEFAULT 0,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (id_operatora) REFERENCES operator(id_operatora) ON DELETE CASCADE,
    UNIQUE KEY unique_operator_table (id_operatora, table_name),
    INDEX idx_id_operatora (id_operatora),
    INDEX idx_table_name (table_name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Krok 2: Procedura do dodawania/aktualizacji uprawnień operatora do tabeli
DELIMITER //

CREATE PROCEDURE IF NOT EXISTS sp_SetOperatorTablePermission(
    IN p_id_operatora INT(15),
    IN p_table_name VARCHAR(100),
    IN p_can_select TINYINT(1),
    IN p_can_insert TINYINT(1),
    IN p_can_update TINYINT(1),
    IN p_can_delete TINYINT(1)
)
BEGIN
    INSERT INTO operator_table_permissions 
        (id_operatora, table_name, can_select, can_insert, can_update, can_delete)
    VALUES 
        (p_id_operatora, p_table_name, p_can_select, p_can_insert, p_can_update, p_can_delete)
    ON DUPLICATE KEY UPDATE
        can_select = p_can_select,
        can_insert = p_can_insert,
        can_update = p_can_update,
        can_delete = p_can_delete,
        UpdatedAt = CURRENT_TIMESTAMP;
END //

DELIMITER ;

-- Krok 3: Procedura do sprawdzania uprawnień operatora do tabeli
DELIMITER //

CREATE PROCEDURE IF NOT EXISTS sp_CheckOperatorTablePermission(
    IN p_id_operatora INT(15),
    IN p_table_name VARCHAR(100),
    IN p_permission_type VARCHAR(20), -- 'SELECT', 'INSERT', 'UPDATE', 'DELETE'
    OUT p_has_permission TINYINT(1)
)
BEGIN
    DECLARE v_permission TINYINT(1) DEFAULT 0;
    
    CASE p_permission_type
        WHEN 'SELECT' THEN
            SELECT can_select INTO v_permission
            FROM operator_table_permissions
            WHERE id_operatora = p_id_operatora AND table_name = p_table_name;
        WHEN 'INSERT' THEN
            SELECT can_insert INTO v_permission
            FROM operator_table_permissions
            WHERE id_operatora = p_id_operatora AND table_name = p_table_name;
        WHEN 'UPDATE' THEN
            SELECT can_update INTO v_permission
            FROM operator_table_permissions
            WHERE id_operatora = p_id_operatora AND table_name = p_table_name;
        WHEN 'DELETE' THEN
            SELECT can_delete INTO v_permission
            FROM operator_table_permissions
            WHERE id_operatora = p_id_operatora AND table_name = p_table_name;
    END CASE;
    
    SET p_has_permission = IFNULL(v_permission, 0);
END //

DELIMITER ;

-- Krok 4: Procedura do pobierania wszystkich uprawnień operatora
DELIMITER //

CREATE PROCEDURE IF NOT EXISTS sp_GetOperatorPermissions(
    IN p_id_operatora INT(15)
)
BEGIN
    SELECT 
        table_name,
        can_select,
        can_insert,
        can_update,
        can_delete,
        CreatedAt,
        UpdatedAt
    FROM operator_table_permissions
    WHERE id_operatora = p_id_operatora
    ORDER BY table_name;
END //

DELIMITER ;

-- Krok 5: Procedura do usuwania uprawnień operatora do tabeli
DELIMITER //

CREATE PROCEDURE IF NOT EXISTS sp_RemoveOperatorTablePermission(
    IN p_id_operatora INT(15),
    IN p_table_name VARCHAR(100)
)
BEGIN
    DELETE FROM operator_table_permissions
    WHERE id_operatora = p_id_operatora AND table_name = p_table_name;
END //

DELIMITER ;

-- Krok 6: Widok do łatwego przeglądania uprawnień wszystkich operatorów
CREATE OR REPLACE VIEW v_operator_permissions_summary AS
SELECT 
    o.id_operatora,
    o.imie_nazwisko,
    otp.table_name,
    otp.can_select,
    otp.can_insert,
    otp.can_update,
    otp.can_delete,
    otp.UpdatedAt
FROM operator o
LEFT JOIN operator_table_permissions otp ON o.id_operatora = otp.id_operatora
ORDER BY o.imie_nazwisko, otp.table_name;

-- Przykładowe użycie:
-- Ustawienie uprawnień dla operatora ID=1 do tabeli Odbiorcy (tylko SELECT i INSERT)
-- CALL sp_SetOperatorTablePermission(1, 'Odbiorcy', 1, 1, 0, 0);

-- Sprawdzenie czy operator ma uprawnienie SELECT do tabeli Odbiorcy
-- CALL sp_CheckOperatorTablePermission(1, 'Odbiorcy', 'SELECT', @has_permission);
-- SELECT @has_permission;

-- Pobranie wszystkich uprawnień operatora ID=1
-- CALL sp_GetOperatorPermissions(1);

-- Wyświetlenie podsumowania uprawnień
-- SELECT * FROM v_operator_permissions_summary;
