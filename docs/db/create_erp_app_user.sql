-- Skrypt tworzący użytkownika MySQL/MariaDB dla aplikacji ERP (WPF/Web).
-- Uruchomić jako użytkownik z uprawnieniami CREATE USER i GRANT (np. root).
-- Dostosuj nazwę bazy jeśli inna niż locbd.

CREATE USER IF NOT EXISTS 'erp_app'@'localhost' IDENTIFIED BY 'StrongPasswordHere';
GRANT SELECT, INSERT, UPDATE, DELETE, EXECUTE ON locbd.* TO 'erp_app'@'localhost';
FLUSH PRIVILEGES;
