-- Skrypt do utworzenia tabeli operator_login
-- Tabela przechowuje dane logowania użytkowników (operatorów)
CREATE TABLE IF NOT EXISTS operator_login (
    id INT(15) AUTO_INCREMENT PRIMARY KEY,
    id_operatora INT(15) NOT NULL,
    login VARCHAR(100) NOT NULL UNIQUE,
    haslohash VARCHAR(255) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (id_operatora) REFERENCES operator(id_operatora) ON DELETE CASCADE,
    INDEX idx_login (login),
    INDEX idx_id_operatora (id_operatora)
);
